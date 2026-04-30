// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Helpers;

using SQLitePCL;
using static SQLitePCL.raw;

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// A connection implementation that interacts directly with SQLite using the SQLitePCL.raw library.
/// This implementation avoids using any Object-Relational Mapping (ORM) or expression-tree translation
/// to minimize overhead. Each SQL statement is prepared once and cached for reuse. Parameters are
/// bound positionally, ensuring that the critical execution path remains efficient and minimizes
/// memory allocations to only the necessary serialized payload and results.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Ordering Rules",
    "SA1204:Static elements should appear before instance elements",
    Justification = "Methods are grouped by functional purpose (public API first, then shared private helpers including the static legacy-V10 probes). Enforcing strict static-first ordering would scatter cohesive code.")]
internal sealed class SqlitePclRawConnection : IAkavacheConnection
{
    /// <summary>
    /// The SQL command used to create the CacheEntry table if it does not already exist.
    /// The table structure is designed to be compatible with previous versions of Akavache
    /// that used sqlite-net, allowing older databases to be opened seamlessly.
    /// </summary>
    private const string SchemaSql =
        "CREATE TABLE IF NOT EXISTS \"CacheEntry\" (" +
        "\"Id\" TEXT PRIMARY KEY NOT NULL, " +
        "\"CreatedAt\" INTEGER NOT NULL, " +
        "\"ExpiresAt\" INTEGER NULL, " +
        "\"TypeName\" TEXT NULL, " +
        "\"Value\" BLOB NULL);" +
        "CREATE INDEX IF NOT EXISTS \"CacheEntry_ExpiresAt\" ON \"CacheEntry\"(\"ExpiresAt\");" +
        "CREATE INDEX IF NOT EXISTS \"CacheEntry_TypeName\" ON \"CacheEntry\"(\"TypeName\");";

    /// <summary>The list of columns retrieved in CacheEntry selection queries.</summary>
    private const string SelectColumns = "\"Id\", \"CreatedAt\", \"ExpiresAt\", \"TypeName\", \"Value\"";

    /// <summary>A filtering clause that excludes expired cache entries. It expects a single parameter representing the current time in UTC ticks.</summary>
    private const string UnexpiredClause = "(\"ExpiresAt\" IS NULL OR \"ExpiresAt\" > ?)";

    /// <summary>
    /// A filtering clause for bulk operations that uses the SQLite json_each function.
    /// This allows the SQL statement to remain static and reusable regardless of how many
    /// keys are being processed. The input parameter should be a JSON array of keys.
    /// </summary>
    private const string JsonKeyInClause = "\"Id\" IN (SELECT value FROM json_each(?))";

    /// <summary>Single-key read (no type filter).</summary>
    private const string SqlGetOne =
        "SELECT " + SelectColumns + " FROM \"CacheEntry\" WHERE \"Id\" = ? AND " + UnexpiredClause;

    /// <summary>Single-key read with type discriminator.</summary>
    private const string SqlGetOneTyped =
        "SELECT " + SelectColumns + " FROM \"CacheEntry\" WHERE \"Id\" = ? AND " + UnexpiredClause + " AND \"TypeName\" = ?";

    /// <summary>Bulk key read using a JSON-array parameter.</summary>
    private const string SqlGetMany =
        "SELECT " + SelectColumns + " FROM \"CacheEntry\" WHERE " + JsonKeyInClause + " AND " + UnexpiredClause;

    /// <summary>Bulk key read with type discriminator.</summary>
    private const string SqlGetManyTyped =
        "SELECT " + SelectColumns + " FROM \"CacheEntry\" WHERE " + JsonKeyInClause + " AND " + UnexpiredClause + " AND \"TypeName\" = ?";

    /// <summary>Full-scan read of unexpired rows.</summary>
    private const string SqlGetAll =
        "SELECT " + SelectColumns + " FROM \"CacheEntry\" WHERE " + UnexpiredClause;

    /// <summary>Full-scan read of unexpired rows matching a type discriminator.</summary>
    private const string SqlGetAllTyped =
        "SELECT " + SelectColumns + " FROM \"CacheEntry\" WHERE " + UnexpiredClause + " AND \"TypeName\" = ?";

    /// <summary>Full-scan read of unexpired keys only.</summary>
    private const string SqlGetAllKeys =
        "SELECT \"Id\" FROM \"CacheEntry\" WHERE " + UnexpiredClause;

    /// <summary>Full-scan read of unexpired keys matching a type discriminator.</summary>
    private const string SqlGetAllKeysTyped =
        "SELECT \"Id\" FROM \"CacheEntry\" WHERE " + UnexpiredClause + " AND \"TypeName\" = ?";

    /// <summary>Upsert of a single CacheEntry row.</summary>
    private const string SqlUpsert =
        "INSERT OR REPLACE INTO \"CacheEntry\" (\"Id\", \"CreatedAt\", \"ExpiresAt\", \"TypeName\", \"Value\") VALUES (?, ?, ?, ?, ?)";

    /// <summary>Delete a single key.</summary>
    private const string SqlInvalidateOne = "DELETE FROM \"CacheEntry\" WHERE \"Id\" = ?";

    /// <summary>Delete a single key scoped to a type discriminator.</summary>
    private const string SqlInvalidateOneTyped = "DELETE FROM \"CacheEntry\" WHERE \"Id\" = ? AND \"TypeName\" = ?";

    /// <summary>Wipe every row in the CacheEntry table.</summary>
    private const string SqlInvalidateAll = "DELETE FROM \"CacheEntry\"";

    /// <summary>Wipe every row matching a type discriminator.</summary>
    private const string SqlInvalidateAllTyped = "DELETE FROM \"CacheEntry\" WHERE \"TypeName\" = ?";

    /// <summary>Update the expiry of a single key.</summary>
    private const string SqlSetExpiry = "UPDATE \"CacheEntry\" SET \"ExpiresAt\" = ? WHERE \"Id\" = ?";

    /// <summary>Update the expiry of a single key scoped to a type discriminator.</summary>
    private const string SqlSetExpiryTyped = "UPDATE \"CacheEntry\" SET \"ExpiresAt\" = ? WHERE \"Id\" = ? AND \"TypeName\" = ?";

    /// <summary>Delete every expired row.</summary>
    private const string SqlVacuumExpired = "DELETE FROM \"CacheEntry\" WHERE \"ExpiresAt\" IS NOT NULL AND \"ExpiresAt\" < ?";

    /// <summary>A query to check for the existence of a specific table in the SQLite database schema.</summary>
    private const string SqlTableExists = "SELECT 1 FROM \"sqlite_master\" WHERE \"type\" = 'table' AND \"name\" = ?";

    /// <summary>Legacy v10 single-row read. Missing table / column errors are swallowed at the caller.</summary>
    private const string SqlLegacyV10 =
        "SELECT \"Value\" FROM \"CacheElement\" WHERE \"Key\" = ? AND (\"Expiration\" IS NULL OR \"Expiration\" = 0 OR \"Expiration\" > ?)";

    /// <summary>Legacy v10 single-row read with type discriminator.</summary>
    private const string SqlLegacyV10Typed =
        "SELECT \"Value\" FROM \"CacheElement\" WHERE \"Key\" = ? AND (\"Expiration\" IS NULL OR \"Expiration\" = 0 OR \"Expiration\" > ?) AND \"TypeName\" = ?";

    /// <summary>Bulk v10 row read used by the migration service to drain an entire v10 file.</summary>
    private const string SqlLegacyV10ReadAll =
        "SELECT \"Key\", \"TypeName\", \"Value\", \"Expiration\", \"CreatedAt\" FROM \"CacheElement\"";

    // ── CacheEntry SELECT column ordinals (matches SelectColumns) ──────────

    /// <summary>Column ordinal for the <c>Id</c> column in CacheEntry SELECT results.</summary>
    private const int ColId = 0;

    /// <summary>Column ordinal for the <c>CreatedAt</c> column in CacheEntry SELECT results.</summary>
    private const int ColCreatedAt = 1;

    /// <summary>Column ordinal for the <c>ExpiresAt</c> column in CacheEntry SELECT results.</summary>
    private const int ColExpiresAt = 2;

    /// <summary>Column ordinal for the <c>TypeName</c> column in CacheEntry SELECT results.</summary>
    private const int ColTypeName = 3;

    /// <summary>Column ordinal for the <c>Value</c> column in CacheEntry SELECT results.</summary>
    private const int ColValue = 4;

    // ── Upsert bind-parameter positions (matches SqlUpsert "?, ?, ?, ?, ?") ──

    /// <summary>Bind position for the <c>Id</c> parameter in the upsert statement.</summary>
    private const int UpsertParamId = 1;

    /// <summary>Bind position for the <c>CreatedAt</c> parameter in the upsert statement.</summary>
    private const int UpsertParamCreatedAt = 2;

    /// <summary>Bind position for the <c>ExpiresAt</c> parameter in the upsert statement.</summary>
    private const int UpsertParamExpiresAt = 3;

    /// <summary>Bind position for the <c>TypeName</c> parameter in the upsert statement.</summary>
    private const int UpsertParamTypeName = 4;

    /// <summary>Bind position for the <c>Value</c> parameter in the upsert statement.</summary>
    private const int UpsertParamValue = 5;

    // ── Common query bind-parameter positions ──────────────────────────────

    /// <summary>Bind position for the key (or JSON key array) parameter in most query statements.</summary>
    private const int QueryParamKey = 1;

    /// <summary>Bind position for the <c>nowUtcTicks</c> expiry-check parameter in most query statements.</summary>
    private const int QueryParamNow = 2;

    /// <summary>Bind position for the optional type-name discriminator parameter in typed query statements.</summary>
    private const int QueryParamTypeName = 3;

    // ── Legacy v10 read-all column ordinals (matches SqlLegacyV10ReadAll) ──

    /// <summary>Column ordinal for the <c>Key</c> column in v10 read-all results.</summary>
    private const int V10ColKey = 0;

    /// <summary>Column ordinal for the <c>TypeName</c> column in v10 read-all results.</summary>
    private const int V10ColTypeName = 1;

    /// <summary>Column ordinal for the <c>Value</c> column in v10 read-all results.</summary>
    private const int V10ColValue = 2;

    /// <summary>Column ordinal for the <c>Expiration</c> column in v10 read-all results.</summary>
    private const int V10ColExpiration = 3;

    /// <summary>Column ordinal for the <c>CreatedAt</c> column in v10 read-all results.</summary>
    private const int V10ColCreatedAt = 4;

    // ── Legacy v10 single-row read column ordinal ──────────────────────────

    /// <summary>Column ordinal for the <c>Value</c> column in the single-row v10 query (only column selected).</summary>
    private const int V10SingleColValue = 0;

    // ── Invalidate bind-parameter positions ────────────────────────────────

    /// <summary>Bind position for the key parameter in invalidate statements.</summary>
    private const int InvalidateParamKey = 1;

    /// <summary>Bind position for the type-name parameter in typed invalidate statements.</summary>
    private const int InvalidateParamTypeName = 2;

    // ── SetExpiry bind-parameter positions ──────────────────────────────────

    /// <summary>Bind position for the <c>ExpiresAt</c> parameter in set-expiry statements.</summary>
    private const int SetExpiryParamExpiresAt = 1;

    /// <summary>Bind position for the key parameter in set-expiry statements.</summary>
    private const int SetExpiryParamKey = 2;

    /// <summary>Bind position for the type-name parameter in typed set-expiry statements.</summary>
    private const int SetExpiryParamTypeName = 3;

    // ── GetAllKeys column ordinal ──────────────────────────────────────────

    /// <summary>Column ordinal for the single <c>Id</c> column returned by GetAllKeys queries.</summary>
    private const int KeysColId = 0;

    // ── JSON builder heuristics ────────────────────────────────────────────

    /// <summary>Estimated average bytes per key used to pre-size the JSON builder in <see cref="SerializeKeysAsJson"/>.</summary>
    private const int EstimatedBytesPerKey = 8;

    /// <summary>First non-control Unicode code point. Characters below this are escaped as <c>\uXXXX</c> in JSON strings.</summary>
    private const char FirstPrintableChar = (char)0x20;

    /// <summary>The cached prepared statement for retrieving a single cache entry.</summary>
    private sqlite3_stmt? _stmtGetOne;

    /// <summary>The cached prepared statement for retrieving a single cache entry with a type discriminator.</summary>
    private sqlite3_stmt? _stmtGetOneTyped;

    /// <summary>The cached prepared statement for retrieving multiple cache entries.</summary>
    private sqlite3_stmt? _stmtGetMany;

    /// <summary>The cached prepared statement for retrieving multiple cache entries with a type discriminator.</summary>
    private sqlite3_stmt? _stmtGetManyTyped;

    /// <summary>The cached prepared statement for retrieving all cache entries.</summary>
    private sqlite3_stmt? _stmtGetAll;

    /// <summary>The cached prepared statement for retrieving all cache entries with a type discriminator.</summary>
    private sqlite3_stmt? _stmtGetAllTyped;

    /// <summary>The cached prepared statement for retrieving all cache keys.</summary>
    private sqlite3_stmt? _stmtGetAllKeys;

    /// <summary>The cached prepared statement for retrieving all cache keys with a type discriminator.</summary>
    private sqlite3_stmt? _stmtGetAllKeysTyped;

    /// <summary>The cached prepared statement for inserting or updating cache entries.</summary>
    private sqlite3_stmt? _stmtUpsert;

    /// <summary>The cached prepared statement for invalidating a single cache entry.</summary>
    private sqlite3_stmt? _stmtInvalidateOne;

    /// <summary>The cached prepared statement for invalidating a single cache entry with a type discriminator.</summary>
    private sqlite3_stmt? _stmtInvalidateOneTyped;

    /// <summary>The cached prepared statement for invalidating all cache entries.</summary>
    private sqlite3_stmt? _stmtInvalidateAll;

    /// <summary>The cached prepared statement for invalidating all cache entries with a type discriminator.</summary>
    private sqlite3_stmt? _stmtInvalidateAllTyped;

    /// <summary>The cached prepared statement for updating the expiration time of a cache entry.</summary>
    private sqlite3_stmt? _stmtSetExpiry;

    /// <summary>The cached prepared statement for updating the expiration time of a cache entry with a type discriminator.</summary>
    private sqlite3_stmt? _stmtSetExpiryTyped;

    /// <summary>The cached prepared statement for deleting expired cache entries.</summary>
    private sqlite3_stmt? _stmtVacuumExpired;

    /// <summary>The cached prepared statement for checking if a table exists.</summary>
    private sqlite3_stmt? _stmtTableExists;

    /// <summary>The cached prepared statement for reading from the legacy version 10 table.</summary>
    private sqlite3_stmt? _stmtLegacyV10;

    /// <summary>The cached prepared statement for reading from the legacy version 10 table with a type discriminator.</summary>
    private sqlite3_stmt? _stmtLegacyV10Typed;

    /// <summary>Tracks whether the connection has already been closed or disposed. Read
    /// via <see cref="Volatile"/> in <see cref="CloseCore"/>, written via
    /// <see cref="Volatile"/> inside the shutdown callback so the store is visible to
    /// subsequent <see cref="CloseCore"/> callers on all architectures.</summary>
    private int _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlitePclRawConnection"/> class.
    /// </summary>
    /// <param name="databasePath">The full file system path to the SQLite database.</param>
    /// <param name="password">An optional password for database encryption. If provided, the key is applied immediately after the database is opened.</param>
    /// <param name="readOnly">Whether to open the database in read-only mode. This is typically used for legacy database probes.</param>
    public SqlitePclRawConnection(string databasePath, string? password, bool readOnly)
    {
        ArgumentExceptionHelper.ThrowIfNull(databasePath);

        // Shared gate across all SQLite-backed Akavache assemblies.
        // See SqliteProviderGate for details on why this is necessary.
        if (SqliteProviderGate.TryClaimInit())
        {
            Batteries_V2.Init();
        }

        var openFlags = readOnly
            ? SQLITE_OPEN_READONLY
            : SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE;

        // Quote the password by doubling any single-quotes.
        var quotedPassword = !string.IsNullOrEmpty(password)
            ? "'" + password!.Replace("'", "''") + "'"
            : null;

        CheckRc(sqlite3_open_v2(databasePath, out var db, openFlags, null), db, "open " + databasePath);
        Db = db;

        if (quotedPassword is not null)
        {
            ExecuteNonQuery("PRAGMA key = " + quotedPassword);
        }

        if (!readOnly)
        {
            // PRAGMA journal_mode=WAL verifies the encryption key.
            // Failure surfaces here as SQLITE_NOTADB if the key is invalid.
            try
            {
                ExecuteNonQuery("PRAGMA journal_mode=WAL");
                ExecuteNonQuery("PRAGMA synchronous=NORMAL");
            }
#if ENCRYPTED
            catch (AkavacheSqliteException ex) when (quotedPassword is not null && ex.ResultCode == SQLITE_NOTADB)
            {
                // The file isn't readable with SQLite3MC's default cipher. Most likely it
                // was written by Akavache 11.x, which used the SQLCipher-4 bundle. Re-open
                // with SQLite3MC's SQLCipher-4 compatibility mode, then re-encrypt forward
                // to the modern cipher so subsequent opens take the fast native path.
                Db.Dispose();
                Db = OpenLegacySqlCipher4(databasePath, openFlags, quotedPassword);
                try
                {
                    // Switch the page cipher back to the SQLite3MC default and rewrite every
                    // page in place. Use the same password so callers don't need to know.
                    ExecuteNonQuery("PRAGMA cipher = 'chacha20'");
                    ExecuteNonQuery("PRAGMA rekey = " + quotedPassword);
                    ExecuteNonQuery("PRAGMA journal_mode=WAL");
                    ExecuteNonQuery("PRAGMA synchronous=NORMAL");
                }
                catch
                {
                    Db.Dispose();
                    throw;
                }
            }
#endif
            catch
            {
                Db.Dispose();
                throw;
            }
        }

        // Start the background worker after all constructor-time native calls.
        // All subsequent database interactions must go through the queue.
        Queue = new(this, $"Akavache.Sqlite3[{Path.GetFileName(databasePath)}]");
    }

    /// <summary>Gets the native SQLite database handle.</summary>
#if ENCRYPTED
    // Reassigned by the constructor when the SQLCipher-4 → modern-cipher
    // backward-compatibility retry path opens a second handle.
#pragma warning disable RCS1170 // Use read-only auto-implemented property — setter is used by the retry path above.
    internal sqlite3 Db { get; private set; }
#pragma warning restore RCS1170
#else
    internal sqlite3 Db { get; }
#endif

    /// <summary>Gets the operation queue that serializes all database interactions.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2213:Disposable fields should be disposed",
        Justification = "Close()/CloseAsync() call Queue.ShutdownAndWait, which joins the worker thread and releases the queue's own native resources.")]
    internal SqliteOperationQueue Queue { get; }

    /// <summary>Gets or sets a value indicating whether an ambient transaction is active.</summary>
    internal bool InTransaction { get; set; }

    /// <inheritdoc/>
    public IObservable<Unit> CreateSchema() =>
        Queue.Enqueue(static conn =>
        {
            conn.ExecuteNonQuery(SchemaSql);
            return Unit.Default;
        });

    /// <inheritdoc/>
    public IObservable<bool> TableExists(string tableName) =>
        Queue.Enqueue(conn =>
        {
            var statement = conn.EnsurePrepared(ref conn._stmtTableExists, SqlTableExists);
            try
            {
                sqlite3_bind_text(statement, QueryParamKey, tableName);
                return sqlite3_step(statement) == SQLITE_ROW;
            }
            finally
            {
                sqlite3_reset(statement);
                sqlite3_clear_bindings(statement);
            }
        });

    /// <inheritdoc/>
    public IObservable<CacheEntry?> Get(string key, string? typeFullName, DateTimeOffset now) =>
        Queue.Enqueue<CacheEntry?>(conn =>
        {
            var nowUtcTicks = now.UtcTicks;
            sqlite3_stmt statement;
            if (typeFullName is null)
            {
                statement = conn.EnsurePrepared(ref conn._stmtGetOne, SqlGetOne);
                sqlite3_bind_text(statement, QueryParamKey, key);
                sqlite3_bind_int64(statement, QueryParamNow, nowUtcTicks);
            }
            else
            {
                statement = conn.EnsurePrepared(ref conn._stmtGetOneTyped, SqlGetOneTyped);
                sqlite3_bind_text(statement, QueryParamKey, key);
                sqlite3_bind_int64(statement, QueryParamNow, nowUtcTicks);
                sqlite3_bind_text(statement, QueryParamTypeName, typeFullName);
            }

            try
            {
                if (sqlite3_step(statement) == SQLITE_ROW)
                {
                    return ReadCacheEntry(statement);
                }

                return null;
            }
            finally
            {
                sqlite3_reset(statement);
                sqlite3_clear_bindings(statement);
            }
        });

    /// <inheritdoc/>
    public IObservable<CacheEntry> GetMany(IReadOnlyList<string> keys, string? typeFullName, DateTimeOffset now)
    {
        ArgumentExceptionHelper.ThrowIfNull(keys);
        return Queue.EnqueueRowStream<CacheEntry>((conn, onNext, isCancelled) =>
        {
            if (keys.Count == 0)
            {
                return;
            }

            var keysJson = SerializeKeysAsJson(keys);
            var nowUtcTicks = now.UtcTicks;

            sqlite3_stmt statement;
            if (typeFullName is null)
            {
                statement = conn.EnsurePrepared(ref conn._stmtGetMany, SqlGetMany);
                sqlite3_bind_text(statement, QueryParamKey, keysJson);
                sqlite3_bind_int64(statement, QueryParamNow, nowUtcTicks);
            }
            else
            {
                statement = conn.EnsurePrepared(ref conn._stmtGetManyTyped, SqlGetManyTyped);
                sqlite3_bind_text(statement, QueryParamKey, keysJson);
                sqlite3_bind_int64(statement, QueryParamNow, nowUtcTicks);
                sqlite3_bind_text(statement, QueryParamTypeName, typeFullName);
            }

            try
            {
                ScanRows(statement, static s => ReadCacheEntry(s), onNext, isCancelled);
            }
            finally
            {
                sqlite3_reset(statement);
                sqlite3_clear_bindings(statement);
            }
        });
    }

    /// <inheritdoc/>
    public IObservable<CacheEntry> GetAll(string? typeFullName, DateTimeOffset now) =>
        Queue.EnqueueRowStream<CacheEntry>((conn, onNext, isCancelled) =>
        {
            var nowUtcTicks = now.UtcTicks;

            sqlite3_stmt statement;
            if (typeFullName is null)
            {
                statement = conn.EnsurePrepared(ref conn._stmtGetAll, SqlGetAll);
                sqlite3_bind_int64(statement, QueryParamKey, nowUtcTicks);
            }
            else
            {
                statement = conn.EnsurePrepared(ref conn._stmtGetAllTyped, SqlGetAllTyped);
                sqlite3_bind_int64(statement, QueryParamKey, nowUtcTicks);
                sqlite3_bind_text(statement, QueryParamNow, typeFullName);
            }

            try
            {
                ScanRows(statement, static s => ReadCacheEntry(s), onNext, isCancelled);
            }
            finally
            {
                sqlite3_reset(statement);
                sqlite3_clear_bindings(statement);
            }
        });

    /// <inheritdoc/>
    public IObservable<string> GetAllKeys(string? typeFullName, DateTimeOffset now) =>
        Queue.EnqueueRowStream<string>((conn, onNext, isCancelled) =>
        {
            var nowUtcTicks = now.UtcTicks;

            sqlite3_stmt statement;
            if (typeFullName is null)
            {
                statement = conn.EnsurePrepared(ref conn._stmtGetAllKeys, SqlGetAllKeys);
                sqlite3_bind_int64(statement, QueryParamKey, nowUtcTicks);
            }
            else
            {
                statement = conn.EnsurePrepared(ref conn._stmtGetAllKeysTyped, SqlGetAllKeysTyped);
                sqlite3_bind_int64(statement, QueryParamKey, nowUtcTicks);
                sqlite3_bind_text(statement, QueryParamNow, typeFullName);
            }

            try
            {
                ScanRows(statement, static s => sqlite3_column_text(s, KeysColId).utf8_to_string(), onNext, isCancelled);
            }
            finally
            {
                sqlite3_reset(statement);
                sqlite3_clear_bindings(statement);
            }
        });

    /// <inheritdoc/>
    public IObservable<Unit> Upsert(IReadOnlyList<CacheEntry> entries)
    {
        ArgumentExceptionHelper.ThrowIfNull(entries);
        return Queue.Enqueue(
            conn =>
            {
                if (entries.Count == 0)
                {
                    return Unit.Default;
                }

                RunInOwnedTransaction(conn, () =>
                {
                    var statement = conn.EnsurePrepared(ref conn._stmtUpsert, SqlUpsert);
                    foreach (var cacheEntry in entries)
                    {
                        sqlite3_bind_text(statement, UpsertParamId, cacheEntry.Id ?? string.Empty);
                        sqlite3_bind_int64(statement, UpsertParamCreatedAt, cacheEntry.CreatedAt.UtcTicks);
                        if (cacheEntry.ExpiresAt.HasValue)
                        {
                            sqlite3_bind_int64(statement, UpsertParamExpiresAt, cacheEntry.ExpiresAt.Value.UtcTicks);
                        }
                        else
                        {
                            sqlite3_bind_null(statement, UpsertParamExpiresAt);
                        }

                        if (cacheEntry.TypeName is null)
                        {
                            sqlite3_bind_null(statement, UpsertParamTypeName);
                        }
                        else
                        {
                            sqlite3_bind_text(statement, UpsertParamTypeName, cacheEntry.TypeName);
                        }

                        if (cacheEntry.Value is null)
                        {
                            sqlite3_bind_null(statement, UpsertParamValue);
                        }
                        else
                        {
                            sqlite3_bind_blob(statement, UpsertParamValue, cacheEntry.Value);
                        }

                        StepAndCheck(statement, conn.Db, "upsert step");
                        sqlite3_reset(statement);
                        sqlite3_clear_bindings(statement);
                    }
                });

                return Unit.Default;
            },
            coalescable: true);
    }

    /// <inheritdoc/>
    public IObservable<Unit> Invalidate(IReadOnlyList<string> keys, string? typeFullName)
    {
        ArgumentExceptionHelper.ThrowIfNull(keys);
        return Queue.Enqueue(
            conn =>
            {
                if (keys.Count == 0)
                {
                    return Unit.Default;
                }

                RunInOwnedTransaction(conn, () =>
                {
                    var statement = typeFullName is null
                        ? conn.EnsurePrepared(ref conn._stmtInvalidateOne, SqlInvalidateOne)
                        : conn.EnsurePrepared(ref conn._stmtInvalidateOneTyped, SqlInvalidateOneTyped);

                    foreach (var cacheKey in keys)
                    {
                        sqlite3_bind_text(statement, InvalidateParamKey, cacheKey);
                        if (typeFullName is not null)
                        {
                            sqlite3_bind_text(statement, InvalidateParamTypeName, typeFullName);
                        }

                        StepAndCheck(statement, conn.Db, "invalidate step");
                        sqlite3_reset(statement);
                        sqlite3_clear_bindings(statement);
                    }
                });

                return Unit.Default;
            },
            coalescable: true);
    }

    /// <inheritdoc/>
    public IObservable<Unit> InvalidateAll(string? typeFullName) =>
        Queue.Enqueue(
            conn =>
            {
                sqlite3_stmt statement;
                if (typeFullName is null)
                {
                    statement = conn.EnsurePrepared(ref conn._stmtInvalidateAll, SqlInvalidateAll);
                }
                else
                {
                    statement = conn.EnsurePrepared(ref conn._stmtInvalidateAllTyped, SqlInvalidateAllTyped);
                    sqlite3_bind_text(statement, QueryParamKey, typeFullName);
                }

                try
                {
                    StepAndCheck(statement, conn.Db, "invalidate-all step");
                    return Unit.Default;
                }
                finally
                {
                    sqlite3_reset(statement);
                    sqlite3_clear_bindings(statement);
                }
            },
            coalescable: true);

    /// <inheritdoc/>
    public IObservable<Unit> SetExpiry(string key, string? typeFullName, DateTimeOffset? expiresAt) =>
        Queue.Enqueue(conn =>
        {
            sqlite3_stmt statement;
            if (typeFullName is null)
            {
                statement = conn.EnsurePrepared(ref conn._stmtSetExpiry, SqlSetExpiry);
                BindNullableTicks(statement, SetExpiryParamExpiresAt, expiresAt);
                sqlite3_bind_text(statement, SetExpiryParamKey, key);
            }
            else
            {
                statement = conn.EnsurePrepared(ref conn._stmtSetExpiryTyped, SqlSetExpiryTyped);
                BindNullableTicks(statement, SetExpiryParamExpiresAt, expiresAt);
                sqlite3_bind_text(statement, SetExpiryParamKey, key);
                sqlite3_bind_text(statement, SetExpiryParamTypeName, typeFullName);
            }

            try
            {
                StepAndCheck(statement, conn.Db, "set-expiry step");
                return Unit.Default;
            }
            finally
            {
                sqlite3_reset(statement);
                sqlite3_clear_bindings(statement);
            }
        });

    /// <inheritdoc/>
    public IObservable<Unit> VacuumExpired(DateTimeOffset now) =>
        Queue.Enqueue(conn =>
        {
            var statement = conn.EnsurePrepared(ref conn._stmtVacuumExpired, SqlVacuumExpired);
            sqlite3_bind_int64(statement, QueryParamKey, now.UtcTicks);
            try
            {
                StepAndCheck(statement, conn.Db, "vacuum-expired step");
                return Unit.Default;
            }
            finally
            {
                sqlite3_reset(statement);
                sqlite3_clear_bindings(statement);
            }
        });

    /// <inheritdoc/>
    public IObservable<Unit> Checkpoint(CheckpointMode mode) =>
        Queue.Enqueue(conn =>
        {
            var pragmaCommand = mode switch
            {
                CheckpointMode.Full => "PRAGMA wal_checkpoint(FULL)",
                CheckpointMode.Truncate => "PRAGMA wal_checkpoint(TRUNCATE)",
                _ => "PRAGMA wal_checkpoint(PASSIVE)",
            };
            conn.ExecuteNonQuery(pragmaCommand);
            return Unit.Default;
        });

    /// <inheritdoc/>
    public IObservable<Unit> Compact() =>
        Queue.Enqueue(static conn =>
        {
            // VACUUM requires all prepared statements to be finalized first.
            // They will be automatically re-prepared during the next operation.
            conn.DisposeStatements();
            conn.ExecuteNonQuery("VACUUM");
            return Unit.Default;
        });

    /// <inheritdoc/>
    public void Dispose() => CloseCore();

    /// <inheritdoc/>
    public IObservable<byte[]?> TryReadLegacyV10Value(string key, DateTimeOffset now, Type? type) =>
        Queue.Enqueue<byte[]?>(conn =>
        {
            // Probe the legacy CacheElement table. Attempt typed search first (using
            // AQN then FQN), then fall back to an untyped search. The caller
            //  handles exception details from missing tables or columns.
            var nowUtcTicks = now.UtcTicks;
            var assemblyQualifiedName = type?.AssemblyQualifiedName;
            var typeFullNameMatch = type?.FullName;

            var assemblyQualifiedNameMatch = assemblyQualifiedName is null || string.IsNullOrWhiteSpace(assemblyQualifiedName)
                ? null
                : TryLegacyTyped(conn, key, nowUtcTicks, assemblyQualifiedName);
            if (assemblyQualifiedNameMatch is not null)
            {
                return assemblyQualifiedNameMatch;
            }

            var typeFullNameHit = typeFullNameMatch is null || string.IsNullOrWhiteSpace(typeFullNameMatch)
                ? null
                : TryLegacyTyped(conn, key, nowUtcTicks, typeFullNameMatch);
            return typeFullNameHit ?? TryLegacyUntyped(conn, key, nowUtcTicks);
        });

    /// <summary>
    /// Reads all rows from the legacy version 10 CacheElement table. This is used exclusively
    /// during the migration process from version 10 to version 11 and is not part of the standard connection interface.
    /// </summary>
    /// <returns>An observable sequence containing the data from each legacy row.</returns>
    public IObservable<V10LegacyRow> ReadAllLegacyV10Rows() =>
        Queue.EnqueueRowStream<V10LegacyRow>(static (conn, onNext, isCancelled) =>
        {
            var resultCode = sqlite3_prepare_v2(conn.Db, SqlLegacyV10ReadAll, out var statement);
            CheckRc(resultCode, conn.Db, "prepare v10 read-all");
            try
            {
                ScanRows(statement, static s => ReadV10Row(s), onNext, isCancelled);
            }
            finally
            {
                statement.Dispose();
            }
        });

    /// <summary>
    /// Provides a shared shutdown path for both asynchronous and synchronous closure.
    /// This method is idempotent, ensuring that subsequent calls do not cause errors.
    /// </summary>
    internal void CloseCore()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        Queue.ShutdownAndWait(conn => RunShutdownCleanup(
            ref conn._disposed,
            conn.DisposeStatements,
            () => conn.Db.Dispose()));
    }

    /// <summary>
    /// Attempts to read a value from the legacy version 10 table using a type name discriminator.
    /// Returns null if the legacy table or its columns cannot be found.
    /// </summary>
    /// <param name="conn">The database connection to use.</param>
    /// <param name="key">The key of the cache entry.</param>
    /// <param name="nowUtcTicks">The current time in UTC ticks for expiry validation.</param>
    /// <param name="typeName">The name of the type associated with the entry.</param>
    /// <returns>The data bytes if found and not expired; otherwise, null.</returns>
    internal static byte[]? TryLegacyTyped(SqlitePclRawConnection conn, string key, long nowUtcTicks, string typeName)
    {
        sqlite3_stmt statement;
        try
        {
            statement = conn.EnsurePrepared(ref conn._stmtLegacyV10Typed, SqlLegacyV10Typed);
        }
        catch (AkavacheSqliteException)
        {
            return null;
        }

        sqlite3_bind_text(statement, QueryParamKey, key);
        sqlite3_bind_int64(statement, QueryParamNow, nowUtcTicks);
        sqlite3_bind_text(statement, QueryParamTypeName, typeName);

        try
        {
            if (sqlite3_step(statement) == SQLITE_ROW)
            {
                return sqlite3_column_blob(statement, V10SingleColValue).ToArray();
            }

            return null;
        }
        finally
        {
            sqlite3_reset(statement);
            sqlite3_clear_bindings(statement);
        }
    }

    /// <summary>
    /// Attempts to read a value from the legacy version 10 table.
    /// Returns null if the legacy table or its columns cannot be found.
    /// </summary>
    /// <param name="conn">The database connection to use.</param>
    /// <param name="key">The key of the cache entry.</param>
    /// <param name="nowUtcTicks">The current time in UTC ticks for expiry validation.</param>
    /// <returns>The data bytes if found and not expired; otherwise, null.</returns>
    internal static byte[]? TryLegacyUntyped(SqlitePclRawConnection conn, string key, long nowUtcTicks)
    {
        sqlite3_stmt statement;
        try
        {
            statement = conn.EnsurePrepared(ref conn._stmtLegacyV10, SqlLegacyV10);
        }
        catch (AkavacheSqliteException)
        {
            return null;
        }

        sqlite3_bind_text(statement, QueryParamKey, key);
        sqlite3_bind_int64(statement, QueryParamNow, nowUtcTicks);

        try
        {
            if (sqlite3_step(statement) == SQLITE_ROW)
            {
                return sqlite3_column_blob(statement, V10SingleColValue).ToArray();
            }

            return null;
        }
        finally
        {
            sqlite3_reset(statement);
            sqlite3_clear_bindings(statement);
        }
    }

    /// <summary>
    /// A simple JSON serializer for a list of strings, producing an array format like ["a","b"].
    /// This is used to avoid the overhead of more complex JSON libraries for small lists of keys.
    /// </summary>
    /// <param name="keys">The list of keys to serialize.</param>
    /// <returns>A string representing the JSON array of keys.</returns>
    internal static string SerializeKeysAsJson(IReadOnlyList<string> keys)
    {
        var jsonBuilder = new StringBuilder(keys.Count * EstimatedBytesPerKey);
        jsonBuilder.Append('[');
        for (var i = 0; i < keys.Count; i++)
        {
            if (i > 0)
            {
                jsonBuilder.Append(',');
            }

            AppendJsonString(jsonBuilder, keys[i]);
        }

        jsonBuilder.Append(']');
        return jsonBuilder.ToString();
    }

    /// <summary>
    /// Appends a string to a StringBuilder, applying JSON escaping rules.
    /// </summary>
    /// <param name="jsonBuilder">The target StringBuilder.</param>
    /// <param name="value">The string value to escape and append.</param>
    internal static void AppendJsonString(StringBuilder jsonBuilder, string value)
    {
        jsonBuilder.Append('"');
        foreach (var character in value)
        {
            switch (character)
            {
                case '"':
                    {
                        jsonBuilder.Append("\\\"");
                        break;
                    }

                case '\\':
                    {
                        jsonBuilder.Append("\\\\");
                        break;
                    }

                case '\b':
                    {
                        jsonBuilder.Append("\\b");
                        break;
                    }

                case '\f':
                    {
                        jsonBuilder.Append("\\f");
                        break;
                    }

                case '\n':
                    {
                        jsonBuilder.Append("\\n");
                        break;
                    }

                case '\r':
                    {
                        jsonBuilder.Append("\\r");
                        break;
                    }

                case '\t':
                    {
                        jsonBuilder.Append("\\t");
                        break;
                    }

                default:
                    {
                        if (character < FirstPrintableChar)
                        {
                            jsonBuilder.Append("\\u").Append(((int)character).ToString("X4"));
                        }
                        else
                        {
                            jsonBuilder.Append(character);
                        }

                        break;
                    }
            }
        }

        jsonBuilder.Append('"');
    }

    /// <summary>
    /// Creates a CacheEntry object from the data in the current row of a prepared statement.
    /// The column order is expected to match the SelectColumns constant.
    /// </summary>
    /// <param name="statement">The statement currently positioned on a valid result row.</param>
    /// <returns>A new CacheEntry populated with the row data.</returns>
    internal static CacheEntry ReadCacheEntry(sqlite3_stmt statement)
    {
        var id = sqlite3_column_text(statement, ColId).utf8_to_string();
        var createdAtTicks = sqlite3_column_int64(statement, ColCreatedAt);

        var expiresAt = sqlite3_column_type(statement, ColExpiresAt) == SQLITE_NULL
            ? null
            : (DateTimeOffset?)new DateTimeOffset(sqlite3_column_int64(statement, ColExpiresAt), TimeSpan.Zero);

        var typeName = sqlite3_column_type(statement, ColTypeName) == SQLITE_NULL
            ? null
            : sqlite3_column_text(statement, ColTypeName).utf8_to_string();

        var value = sqlite3_column_type(statement, ColValue) == SQLITE_NULL
            ? null
            : sqlite3_column_blob(statement, ColValue).ToArray();

        return new(id, typeName, value, new DateTimeOffset(createdAtTicks, TimeSpan.Zero), expiresAt);
    }

    /// <summary>
    /// Binds a date and time value to a parameter in a prepared statement.
    /// If the value is null, a SQLite NULL is bound instead of ticks.
    /// </summary>
    /// <param name="statement">The statement to bind the value to.</param>
    /// <param name="parameterIndex">The position of the parameter (starting at 1).</param>
    /// <param name="value">The date and time value to bind.</param>
    internal static void BindNullableTicks(sqlite3_stmt statement, int parameterIndex, DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            sqlite3_bind_int64(statement, parameterIndex, value.Value.UtcTicks);
        }
        else
        {
            sqlite3_bind_null(statement, parameterIndex);
        }
    }

    /// <summary>
    /// Performs a best-effort database rollback if a transaction fails.
    /// Any secondary errors during the rollback are ignored to preserve the original exception.
    /// </summary>
    /// <param name="db">The native SQLite handle.</param>
    internal static void TryRollback(sqlite3 db)
    {
        try
        {
            sqlite3_exec(db, "ROLLBACK");
        }
        catch
        {
            // best-effort rollback after a mid-transaction failure
        }
    }

    /// <summary>
    /// Checks a <c>sqlite3_prepare_v2</c> result code and throws if it indicates failure.
    /// Disposes the partial mapping on error.
    /// </summary>
    /// <param name="resultCode">The SQLite result code.</param>
    /// <param name="preparedMapping">The statement handle (may be null on failure).</param>
    /// <param name="db">The database handle for error messages.</param>
    /// <param name="sql">The SQL text that was being prepared.</param>
    internal static void HandlePrepareResult(int resultCode, sqlite3_stmt? preparedMapping, sqlite3 db, string sql)
    {
        if (resultCode is SQLITE_OK)
        {
            return;
        }

        preparedMapping?.Dispose();
        CheckRc(resultCode, db, "prepare: " + sql);
    }

    /// <summary>
    /// Reads a V10 legacy row from the current statement position.
    /// </summary>
    /// <param name="statement">The prepared statement positioned on a row.</param>
    /// <returns>A <see cref="V10LegacyRow"/>.</returns>
    internal static V10LegacyRow ReadV10Row(sqlite3_stmt statement)
    {
        var cacheKey = sqlite3_column_text(statement, V10ColKey).utf8_to_string();
        var typeName = sqlite3_column_type(statement, V10ColTypeName) == SQLITE_NULL
            ? null
            : sqlite3_column_text(statement, V10ColTypeName).utf8_to_string();
        var value = sqlite3_column_type(statement, V10ColValue) == SQLITE_NULL
            ? null
            : sqlite3_column_blob(statement, V10ColValue).ToArray();
        var expiration = sqlite3_column_int64(statement, V10ColExpiration);
        var createdAt = sqlite3_column_int64(statement, V10ColCreatedAt);
        return new(cacheKey, typeName, value, expiration, createdAt);
    }

    /// <summary>
    /// Steps through a prepared statement, emitting each row via <paramref name="onNext"/>.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="statement">The prepared statement.</param>
    /// <param name="readRow">Reads a row from the current position.</param>
    /// <param name="onNext">Emits each row.</param>
    /// <param name="isCancelled">Returns true when the subscriber has disposed.</param>
    internal static void ScanRows<T>(sqlite3_stmt statement, Func<sqlite3_stmt, T> readRow, Action<T> onNext, Func<bool> isCancelled)
    {
        while (!isCancelled() && sqlite3_step(statement) == SQLITE_ROW)
        {
            onNext(readRow(statement));
        }
    }

    /// <summary>
    /// Executes <c>sqlite3_step</c> on <paramref name="statement"/> and throws via
    /// <see cref="CheckRc"/> if the result is not <c>SQLITE_DONE</c>.
    /// </summary>
    /// <param name="statement">The prepared statement to step.</param>
    /// <param name="db">The database handle for error messages.</param>
    /// <param name="operation">A description for the error message.</param>
    internal static void StepAndCheck(sqlite3_stmt statement, sqlite3 db, string operation)
    {
        var rc = sqlite3_step(statement);
        CheckRc(MapStepResult(rc, db), db, operation);
    }

    /// <summary>
    /// Maps a <c>sqlite3_step</c> return code to a <c>CheckRc</c>-compatible code.
    /// <c>SQLITE_DONE</c> maps to <c>SQLITE_OK</c>; anything else extracts the
    /// extended error code from the database handle.
    /// </summary>
    /// <param name="stepResult">The return value from <c>sqlite3_step</c>.</param>
    /// <param name="db">The database handle for error code extraction.</param>
    /// <returns>A result code suitable for <see cref="CheckRc"/>.</returns>
    internal static int MapStepResult(int stepResult, sqlite3 db) =>
        stepResult == SQLITE_DONE ? SQLITE_OK : sqlite3_errcode(db);

    /// <summary>
    /// Validates a SQLite return code and throws <see cref="AkavacheSqliteException"/>
    /// if it indicates failure.
    /// </summary>
    /// <param name="resultCode">The SQLite result code.</param>
    /// <param name="db">The database handle for error message extraction, or null.</param>
    /// <param name="operation">A description of the operation for the error message.</param>
    internal static void CheckRc(int resultCode, sqlite3? db, string operation)
    {
        if (resultCode is SQLITE_OK or SQLITE_DONE or SQLITE_ROW)
        {
            return;
        }

        string message;
        if (db is null)
        {
            message = $"SQLite error {resultCode} during {operation}";
        }
        else
        {
            var detail = sqlite3_errmsg(db).utf8_to_string();
            message = $"SQLite error {resultCode} during {operation}: {detail}";
        }

        throw new AkavacheSqliteException(resultCode, message);
    }

    /// <summary>
    /// Ensures that an SQL statement is prepared and ready for execution.
    /// If the statement is already cached in the provided slot, it is returned immediately.
    /// Otherwise, the SQL is prepared and stored in the slot for future use.
    /// </summary>
    /// <remarks>
    /// To ensure stability, the preparation result is first stored in a local variable.
    /// This prevents a failed preparation from leaving a partially initialized or invalid
    /// statement in the cache slot, which could lead to segmentation faults during cleanup.
    /// If preparation fails, the slot remains empty, allowing for a clean retry later.
    /// </remarks>
    /// <param name="slot">The memory location where the prepared statement is cached.</param>
    /// <param name="sql">The SQL text to prepare.</param>
    /// <returns>The prepared statement ready for use.</returns>
    internal sqlite3_stmt EnsurePrepared(ref sqlite3_stmt? slot, string sql)
    {
        if (slot is not null)
        {
            return slot;
        }

        var resultCode = sqlite3_prepare_v2(Db, sql, out var preparedMapping);
        HandlePrepareResult(resultCode, preparedMapping, Db, sql);

        slot = preparedMapping;
        return slot!;
    }

    /// <summary>
    /// Executes an SQL command that does not return results, such as a PRAGMA or a schema modification.
    /// </summary>
    /// <param name="sql">The SQL text to execute.</param>
    internal void ExecuteNonQuery(string sql)
    {
        var resultCode = sqlite3_exec(Db, sql);
        CheckRc(resultCode, Db, "exec: " + sql);
    }

    /// <summary>
    /// Opens an ambient <c>BEGIN IMMEDIATE</c> transaction for the operation queue's
    /// commit-coalescing path. Sets <see cref="InTransaction"/> so that individual write
    /// methods skip their own transaction management.
    /// </summary>
    internal void BeginImmediate()
    {
        ExecuteNonQuery("BEGIN IMMEDIATE");
        InTransaction = true;
    }

    /// <summary>
    /// Commits the ambient transaction opened by <see cref="BeginImmediate"/>.
    /// </summary>
    internal void Commit()
    {
        InTransaction = false;
        ExecuteNonQuery("COMMIT");
    }

    /// <summary>
    /// Runs <paramref name="body"/> inside an owned <c>BEGIN IMMEDIATE … COMMIT</c>
    /// transaction when the connection is not already inside an ambient transaction.
    /// On failure, rolls back and re-throws. When an ambient transaction is active,
    /// the body runs directly without extra transaction management.
    /// </summary>
    /// <param name="conn">The connection to run against.</param>
    /// <param name="body">The work to execute inside the transaction.</param>
    internal static void RunInOwnedTransaction(SqlitePclRawConnection conn, Action body)
    {
        var ownsTransaction = !conn.InTransaction;
        if (ownsTransaction)
        {
            conn.ExecuteNonQuery("BEGIN IMMEDIATE");
        }

        try
        {
            body();

            if (ownsTransaction)
            {
                conn.ExecuteNonQuery("COMMIT");
            }
        }
        catch
        {
            if (ownsTransaction)
            {
                TryRollback(conn.Db);
            }

            throw;
        }
    }

    /// <summary>
    /// Best-effort rollback of the ambient transaction. Clears the
    /// transaction flag regardless of outcome.
    /// </summary>
    /// <param name="setInTransaction">Action that sets the transaction flag to false.</param>
    /// <param name="db">The database handle to rollback on.</param>
    internal static void TryRollbackAmbient(Action<bool> setInTransaction, sqlite3 db)
    {
        setInTransaction(false);
        TryRollback(db);
    }

    /// <summary>
    /// Shutdown cleanup: disposes statements and the native handle if not already disposed.
    /// </summary>
    /// <param name="disposed">The disposed flag.</param>
    /// <param name="disposeStatements">Action that finalizes prepared statements.</param>
    /// <param name="disposeHandle">Action that closes the native database handle.</param>
    internal static void RunShutdownCleanup(
        ref int disposed,
        Action disposeStatements,
        Action disposeHandle)
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            return;
        }

        disposeStatements();
        disposeHandle();
        Volatile.Write(ref disposed, 1);
    }

    /// <summary>Finalizes and releases all cached prepared statements.</summary>
    internal void DisposeStatements()
    {
        DisposeStatement(ref _stmtGetOne);
        DisposeStatement(ref _stmtGetOneTyped);
        DisposeStatement(ref _stmtGetMany);
        DisposeStatement(ref _stmtGetManyTyped);
        DisposeStatement(ref _stmtGetAll);
        DisposeStatement(ref _stmtGetAllTyped);
        DisposeStatement(ref _stmtGetAllKeys);
        DisposeStatement(ref _stmtGetAllKeysTyped);
        DisposeStatement(ref _stmtUpsert);
        DisposeStatement(ref _stmtInvalidateOne);
        DisposeStatement(ref _stmtInvalidateOneTyped);
        DisposeStatement(ref _stmtInvalidateAll);
        DisposeStatement(ref _stmtInvalidateAllTyped);
        DisposeStatement(ref _stmtSetExpiry);
        DisposeStatement(ref _stmtSetExpiryTyped);
        DisposeStatement(ref _stmtVacuumExpired);
        DisposeStatement(ref _stmtTableExists);
        DisposeStatement(ref _stmtLegacyV10);
        DisposeStatement(ref _stmtLegacyV10Typed);

        static void DisposeStatement(ref sqlite3_stmt? stmt)
        {
            stmt?.Dispose();
            stmt = null;
        }
    }

#if ENCRYPTED
    /// <summary>
    /// Opens <paramref name="databasePath"/> with SQLite3MC's SQLCipher-4 compatibility
    /// mode engaged so a database produced by the Akavache 11.x encrypted provider
    /// (which used the SQLCipher-4 native bundle) can be read.
    /// </summary>
    /// <param name="databasePath">The full file system path to the SQLite database.</param>
    /// <param name="openFlags">The flags used for <c>sqlite3_open_v2</c>.</param>
    /// <param name="quotedPassword">The password, already wrapped in single-quotes for PRAGMA use.</param>
    /// <returns>An open <see cref="sqlite3"/> handle on which page 1 has been successfully decrypted.</returns>
    private static sqlite3 OpenLegacySqlCipher4(string databasePath, int openFlags, string quotedPassword)
    {
        CheckRc(sqlite3_open_v2(databasePath, out var db, openFlags, null), db, "open " + databasePath + " (sqlcipher-4 compat)");
        try
        {
            CheckRc(sqlite3_exec(db, "PRAGMA cipher = 'sqlcipher'"), db, "exec: PRAGMA cipher = 'sqlcipher'");
            CheckRc(sqlite3_exec(db, "PRAGMA legacy = 4"), db, "exec: PRAGMA legacy = 4");
            CheckRc(sqlite3_exec(db, "PRAGMA key = " + quotedPassword), db, "exec: PRAGMA key (sqlcipher-4)");

            // Touch page 1 to confirm the key is correct in legacy mode. This raises
            // SQLITE_NOTADB if the password is genuinely wrong, distinguishing that
            // case from "wrote with v11" (which now succeeds).
            CheckRc(sqlite3_exec(db, "SELECT count(*) FROM sqlite_master"), db, "verify legacy key");
            return db;
        }
        catch
        {
            db.Dispose();
            throw;
        }
    }
#endif
}
