// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq.Expressions;

using SQLite;

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// SQLite implementation of <see cref="IAkavacheConnection"/> that delegates to
/// sqlite-net's <see cref="SQLiteAsyncConnection"/>.
/// </summary>
internal sealed class SqliteAkavacheConnection : IAkavacheConnection
{
    /// <summary>The underlying sqlite-net asynchronous connection.</summary>
    private readonly SQLiteAsyncConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteAkavacheConnection"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    public SqliteAkavacheConnection(SQLiteConnectionString connectionString) =>
        _connection = new SQLiteAsyncConnection(connectionString);

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteAkavacheConnection"/> class
    /// with explicit open flags (used for read-only V10 migration connections).
    /// </summary>
    /// <param name="databasePath">The path to the database file.</param>
    /// <param name="openFlags">The SQLite open flags.</param>
    public SqliteAkavacheConnection(string databasePath, SQLiteOpenFlags openFlags) =>
        _connection = new SQLiteAsyncConnection(databasePath, openFlags);

    /// <inheritdoc/>
    public Task CreateTableAsync<T>()
        where T : new() =>
        _connection.CreateTableAsync<T>();

    /// <inheritdoc/>
    public Task<List<T>> QueryAsync<T>(Expression<Func<T, bool>> predicate)
        where T : new() =>
        _connection.Table<T>().Where(predicate).ToListAsync();

    /// <inheritdoc/>
    public Task<List<T>> QueryAsync<T>(string sql, params object[] args)
        where T : new() =>
        _connection.QueryAsync<T>(sql, args);

    /// <inheritdoc/>
    public async Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate)
        where T : new() =>
        await _connection.Table<T>().Where(predicate).FirstOrDefaultAsync().ConfigureAwait(false);

    /// <inheritdoc/>
    public Task InsertOrReplaceAsync<T>(T entity)
        where T : new() =>
        _connection.InsertOrReplaceAsync(entity);

    /// <inheritdoc/>
    public Task ExecuteAsync(string sql, params object[] args) =>
        _connection.ExecuteAsync(sql, args);

    /// <inheritdoc/>
    public Task<T> ExecuteScalarAsync<T>(string sql, params object[] args) =>
        _connection.ExecuteScalarAsync<T>(sql, args);

    /// <inheritdoc/>
    public Task RunInTransactionAsync(Action<IAkavacheTransaction> body) =>
        _connection.RunInTransactionAsync(syncConnection =>
            body(new SqliteAkavacheTransaction(syncConnection)));

    /// <inheritdoc/>
    public async Task<bool> TableExistsAsync(string tableName)
    {
        var tableInfo = await _connection.GetTableInfoAsync(tableName).ConfigureAwait(false);
        return tableInfo.Count > 0;
    }

    /// <inheritdoc/>
    public Task CheckpointAsync(CheckpointMode mode = CheckpointMode.Passive)
    {
        // wal_checkpoint returns a 3-column row (busy, log, checkpointed); use a typed query
        // so the result set is drained instead of flowing through ExecuteAsync, which treats
        // any row return as an error.
        var pragma = mode switch
        {
            CheckpointMode.Full => "PRAGMA wal_checkpoint(FULL)",
            CheckpointMode.Truncate => "PRAGMA wal_checkpoint(TRUNCATE)",
            _ => "PRAGMA wal_checkpoint(PASSIVE)",
        };
        return _connection.QueryAsync<WalCheckpointRow>(pragma);
    }

    /// <inheritdoc/>
    public Task CompactAsync() =>
        _connection.ExecuteAsync("VACUUM");

    /// <summary>
    /// Releases auxiliary SQLite resources by switching the journal mode to <c>DELETE</c>,
    /// which removes the <c>-wal</c> and <c>-shm</c> files. The PRAGMA returns a row
    /// containing the new mode name, so it is drained via a typed query rather than
    /// <c>ExecuteAsync</c> (which would treat the returned row as an error).
    /// </summary>
    /// <returns>A task that completes when the journal mode switch is done.</returns>
    public Task ReleaseAuxiliaryResourcesAsync() =>
        _connection.QueryAsync<JournalModeRow>("PRAGMA journal_mode=DELETE");

    /// <inheritdoc/>
    public async Task<byte[]?> TryReadLegacyV10ValueAsync(string key, DateTimeOffset now, Type? type)
    {
        // V10 schema columns: Key (varchar), TypeName (varchar), Value (BLOB), Expiration (bigint), CreatedAt (bigint).
        // Expiration is ticks (bigint). NULL or 0 means unexpired; otherwise require Expiration > nowTicks.
        var nowTicks = now.UtcTicks;
        const string expiryPredicate = "(Expiration IS NULL OR Expiration = 0 OR Expiration > ?)";

        var sqlStatements = new List<(string Sql, object[] Args)>(3);
        if (type is not null)
        {
            if (!string.IsNullOrWhiteSpace(type.AssemblyQualifiedName))
            {
                sqlStatements.Add((
                    $"SELECT Value FROM CacheElement WHERE Key = ? AND {expiryPredicate} AND TypeName = ?",
                    [key, nowTicks, type.AssemblyQualifiedName!]));
            }

            sqlStatements.Add((
                $"SELECT Value FROM CacheElement WHERE Key = ? AND {expiryPredicate} AND TypeName = ?",
                [key, nowTicks, type.FullName!]));
        }

        sqlStatements.Add((
            $"SELECT Value FROM CacheElement WHERE Key = ? AND {expiryPredicate}",
            [key, nowTicks]));

        foreach (var (sql, args) in sqlStatements)
        {
            try
            {
                var value = await _connection.ExecuteScalarAsync<byte[]?>(sql, args).ConfigureAwait(false);
                if (value != null)
                {
                    return value;
                }
            }
            catch
            {
                // Try next form — table/columns may not exist in newer DBs.
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public Task CloseAsync() =>
        _connection.CloseAsync();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() =>
        await CloseAsync().ConfigureAwait(false);

    /// <summary>
    /// Row shape used to drain the result of <c>PRAGMA wal_checkpoint(...)</c>, which returns
    /// (busy, log, checkpointed).
    /// </summary>
    private sealed class WalCheckpointRow
    {
        /// <summary>Gets or sets the busy flag returned by the checkpoint pragma.</summary>
        [Column("busy")]
        public int Busy { get; set; }

        /// <summary>Gets or sets the number of frames in the WAL log at checkpoint time.</summary>
        [Column("log")]
        public int Log { get; set; }

        /// <summary>Gets or sets the number of frames successfully checkpointed.</summary>
        [Column("checkpointed")]
        public int Checkpointed { get; set; }
    }

    /// <summary>
    /// Row shape used to drain the result of <c>PRAGMA journal_mode</c>, which returns the
    /// current (or newly-set) journal mode name.
    /// </summary>
    private sealed class JournalModeRow
    {
        /// <summary>Gets or sets the journal mode name returned by the pragma.</summary>
        [Column("journal_mode")]
        public string JournalMode { get; set; } = string.Empty;
    }
}
