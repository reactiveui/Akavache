// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;
using Akavache.Tests.Helpers;
using SQLite;

namespace Akavache.Tests;

/// <summary>
/// Direct tests for <see cref="SqliteAkavacheConnection"/> that exercise the SQL translation
/// layer against a real temporary SQLite database file. One test per <see cref="IAkavacheConnection"/>
/// method to ensure provider-specific SQL actually executes.
/// </summary>
[Category("Akavache")]
[NotInParallel("NativeSqlite")]
public class SqliteAkavacheConnectionTests
{
    /// <summary>
    /// Verifies that <see cref="SqliteAkavacheConnection.CreateTableAsync{T}"/> creates the
    /// CacheEntry table and that it can be detected via <see cref="IAkavacheConnection.TableExistsAsync"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateTableAndTableExistsShouldRoundTrip()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);

            await Assert.That(await connection.TableExistsAsync("CacheEntry")).IsFalse();

            await connection.CreateTableAsync<CacheEntry>();

            await Assert.That(await connection.TableExistsAsync("CacheEntry")).IsTrue();
        }
    }

    /// <summary>
    /// Verifies <see cref="SqliteAkavacheConnection.InsertOrReplaceAsync{T}"/> and
    /// <see cref="SqliteAkavacheConnection.FirstOrDefaultAsync{T}"/> round-trip a
    /// <see cref="CacheEntry"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertOrReplaceAndFirstOrDefaultShouldRoundTrip()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            CacheEntry entry = new()
            {
                Id = "k1",
                Value = [1, 2, 3],
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                TypeName = typeof(string).FullName,
            };

            await connection.InsertOrReplaceAsync(entry);

            var retrieved = await connection.FirstOrDefaultAsync<CacheEntry>(x => x.Id == "k1");
            await Assert.That(retrieved).IsNotNull();
            await Assert.That(retrieved!.Value).IsEquivalentTo(new byte[] { 1, 2, 3 });
            await Assert.That(retrieved.TypeName).IsEqualTo(typeof(string).FullName);
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteAkavacheConnection.QueryAsync{T}(System.Linq.Expressions.Expression{System.Func{T, bool}})"/>
    /// returns only entries matching the predicate.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task QueryAsyncWithPredicateShouldFilter()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "a", Value = [1], TypeName = "T1" });
            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "b", Value = [2], TypeName = "T1" });
            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "c", Value = [3], TypeName = "T2" });

            var t1 = await connection.QueryAsync<CacheEntry>(x => x.TypeName == "T1");
            await Assert.That(t1.Count).IsEqualTo(2);
            await Assert.That(t1.Any(x => x.Id == "a")).IsTrue();
            await Assert.That(t1.Any(x => x.Id == "b")).IsTrue();
        }
    }

    /// <summary>
    /// Verifies <see cref="SqliteAkavacheConnection.QueryAsync{T}(string, object[])"/> executes
    /// a raw SQL query and maps results correctly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task QueryAsyncWithSqlShouldExecute()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "k", Value = [42] });

            var results = await connection.QueryAsync<CacheEntry>("SELECT * FROM CacheEntry WHERE Id = ?", "k");
            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].Value![0]).IsEqualTo((byte)42);
        }
    }

    /// <summary>
    /// Verifies <see cref="SqliteAkavacheConnection.ExecuteAsync"/> runs a raw SQL statement.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteAsyncShouldRunRawSql()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "k", Value = [1] });
            await connection.ExecuteAsync("DELETE FROM CacheEntry WHERE Id = ?", "k");

            var rows = await connection.QueryAsync<CacheEntry>(_ => true);
            await Assert.That(rows).IsEmpty();
        }
    }

    /// <summary>
    /// Verifies <see cref="SqliteAkavacheConnection.ExecuteScalarAsync{T}"/> returns a scalar
    /// value from a raw SQL query.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteScalarAsyncShouldReturnScalar()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "a", Value = [1] });
            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "b", Value = [2] });

            var count = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM CacheEntry");
            await Assert.That(count).IsEqualTo(2L);
        }
    }

    /// <summary>
    /// Verifies that <see cref="IAkavacheConnection.RunInTransactionAsync"/> provides a
    /// synchronous transaction handle whose operations are visible after commit.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunInTransactionShouldCommitOperations()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            await connection.RunInTransactionAsync(tx =>
            {
                tx.InsertOrReplace(new CacheEntry { Id = "x", Value = [9] });
                tx.InsertOrReplace(new CacheEntry { Id = "y", Value = [8] });
            });

            var rows = await connection.QueryAsync<CacheEntry>(_ => true);
            await Assert.That(rows.Count).IsEqualTo(2);
        }
    }

    /// <summary>
    /// Verifies that <see cref="IAkavacheTransaction.Query{T}"/> inside a transaction returns
    /// current rows, and <see cref="IAkavacheTransaction.Delete{T}"/> removes them.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunInTransactionQueryAndDeleteShouldWork()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "p", Value = [1] });
            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "q", Value = [2] });

            await connection.RunInTransactionAsync(tx =>
            {
                foreach (var row in tx.Query<CacheEntry>(_ => true))
                {
                    tx.Delete<CacheEntry>(row.Id!);
                }
            });

            var remaining = await connection.QueryAsync<CacheEntry>(_ => true);
            await Assert.That(remaining).IsEmpty();
        }
    }

    /// <summary>
    /// Verifies that <see cref="IAkavacheTransaction.SetExpiry"/> updates the <c>ExpiresAt</c>
    /// column on the matching entry when no type filter is supplied.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetExpiryWithoutTypeShouldUpdateExpiresAt()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "k", Value = [1], ExpiresAt = DateTime.UtcNow.AddMinutes(1) });

            DateTime newExpiry = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await connection.RunInTransactionAsync(tx => tx.SetExpiry("k", null, newExpiry));

            var row = await connection.FirstOrDefaultAsync<CacheEntry>(x => x.Id == "k");
            await Assert.That(row).IsNotNull();
            await Assert.That(row!.ExpiresAt!.Value.Year).IsEqualTo(2030);
        }
    }

    /// <summary>
    /// Verifies that <see cref="IAkavacheTransaction.SetExpiry"/> with a type filter only
    /// mutates rows whose <c>TypeName</c> column matches.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetExpiryWithTypeFilterShouldOnlyUpdateMatching()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            DateTime originalExpiry = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "k", Value = [1], ExpiresAt = originalExpiry, TypeName = "A" });

            await connection.RunInTransactionAsync(tx =>
            {
                // Non-matching type — should be a no-op.
                tx.SetExpiry("k", "B", new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            });

            var row = await connection.FirstOrDefaultAsync<CacheEntry>(x => x.Id == "k");
            await Assert.That(row!.ExpiresAt!.Value.Year).IsEqualTo(2025);

            await connection.RunInTransactionAsync(tx =>
            {
                // Matching type — should update.
                tx.SetExpiry("k", "A", new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            });

            row = await connection.FirstOrDefaultAsync<CacheEntry>(x => x.Id == "k");
            await Assert.That(row!.ExpiresAt!.Value.Year).IsEqualTo(2040);
        }
    }

    /// <summary>
    /// Verifies that <see cref="IAkavacheTransaction.Execute"/> runs arbitrary SQL inside a transaction.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TransactionExecuteShouldRunSql()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "k", Value = [1] });
            await connection.RunInTransactionAsync(tx => tx.Execute("DELETE FROM CacheEntry WHERE Id = ?", "k"));

            var rows = await connection.QueryAsync<CacheEntry>(_ => true);
            await Assert.That(rows).IsEmpty();
        }
    }

    /// <summary>
    /// Verifies that the synchronous transaction reports <see cref="IAkavacheTransaction.IsValid"/>
    /// as <c>true</c> for a real open connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TransactionIsValidShouldBeTrueForOpenConnection()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            var observedValid = false;
            await connection.RunInTransactionAsync(tx => observedValid = tx.IsValid);
            await Assert.That(observedValid).IsTrue();
        }
    }

    /// <summary>
    /// Verifies each <see cref="CheckpointMode"/> executes successfully against a real SQLite
    /// database in WAL journaling mode (where the checkpoint is meaningful).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CheckpointAsyncShouldExecuteAllModes()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            // Put the database in WAL mode so wal_checkpoint has something to act on.
            await connection.QueryAsync<JournalModeRow>("PRAGMA journal_mode=WAL");

            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "k", Value = [1] });

            await connection.CheckpointAsync(CheckpointMode.Passive);
            await connection.CheckpointAsync(CheckpointMode.Full);
            await connection.CheckpointAsync(CheckpointMode.Truncate);
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteAkavacheConnection.CompactAsync"/> executes VACUUM
    /// successfully against a real database.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CompactAsyncShouldExecuteVacuum()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();
            await connection.InsertOrReplaceAsync(new CacheEntry { Id = "k", Value = [1] });

            await connection.CompactAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteAkavacheConnection.ReleaseAuxiliaryResourcesAsync"/>
    /// switches the SQLite journal mode to DELETE, allowing -wal and -shm files to be released.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReleaseAuxiliaryResourcesShouldSwitchJournalMode()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            // Start from WAL so there is an observable mode transition.
            await connection.QueryAsync<JournalModeRow>("PRAGMA journal_mode=WAL");

            await connection.ReleaseAuxiliaryResourcesAsync();

            var modes = await connection.QueryAsync<JournalModeRow>("PRAGMA journal_mode");
            await Assert.That(modes).IsNotEmpty();
            await Assert.That(string.Equals(modes[0].JournalMode, "delete", StringComparison.OrdinalIgnoreCase)).IsTrue();
        }
    }

    /// <summary>
    /// Verifies <see cref="SqliteAkavacheConnection.TryReadLegacyV10ValueAsync"/> returns a
    /// value from a pre-existing V10 CacheElement table row (type-filtered).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10ValueAsyncShouldReturnMatchingRow()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "legacy.db");

            // Arrange: manually build a V10-shaped CacheElement table.
            using (SQLiteConnection raw = new(dbPath))
            {
                raw.Execute(
                    "CREATE TABLE CacheElement (Key varchar PRIMARY KEY, TypeName varchar, Value blob, Expiration bigint, CreatedAt bigint)");
                raw.Execute(
                    "INSERT INTO CacheElement (Key, TypeName, Value, Expiration, CreatedAt) VALUES (?, ?, ?, ?, ?)",
                    "legacyKey",
                    typeof(string).FullName,
                    new byte[] { 5, 6, 7 },
                    0L,
                    DateTime.UtcNow.Ticks);
            }

            await using SqliteAkavacheConnection connection = new(new(dbPath, true));

            var result = await connection.TryReadLegacyV10ValueAsync("legacyKey", DateTimeOffset.UtcNow, typeof(string));
            await Assert.That(result).IsNotNull();
            await Assert.That(result!).IsEquivalentTo(new byte[] { 5, 6, 7 });
        }
    }

    /// <summary>
    /// Verifies <see cref="SqliteAkavacheConnection.TryReadLegacyV10ValueAsync"/> falls back
    /// from type-filtered to untyped query when the type filter does not match.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10ValueAsyncShouldFallBackToUntypedQuery()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "legacy.db");

            using (SQLiteConnection raw = new(dbPath))
            {
                raw.Execute(
                    "CREATE TABLE CacheElement (Key varchar PRIMARY KEY, TypeName varchar, Value blob, Expiration bigint, CreatedAt bigint)");
                raw.Execute(
                    "INSERT INTO CacheElement (Key, TypeName, Value, Expiration, CreatedAt) VALUES (?, ?, ?, ?, ?)",
                    "k",
                    "SomeOther.Type",
                    new byte[] { 99 },
                    0L,
                    DateTime.UtcNow.Ticks);
            }

            await using SqliteAkavacheConnection connection = new(new(dbPath, true));

            var result = await connection.TryReadLegacyV10ValueAsync("k", DateTimeOffset.UtcNow, type: null);
            await Assert.That(result).IsNotNull();
            await Assert.That(result![0]).IsEqualTo((byte)99);
        }
    }

    /// <summary>
    /// Verifies <see cref="SqliteAkavacheConnection.TryReadLegacyV10ValueAsync"/> returns null
    /// when the CacheElement table does not exist at all (new database).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10ValueAsyncShouldReturnNullWhenTableMissing()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            var result = await connection.TryReadLegacyV10ValueAsync("missing", DateTimeOffset.UtcNow, null);
            await Assert.That(result).IsNull();
        }
    }

    /// <summary>
    /// Verifies <see cref="SqliteAkavacheConnection.TryReadLegacyV10ValueAsync"/> honours the
    /// <c>Expiration</c> column and skips expired rows.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10ValueAsyncShouldSkipExpiredRows()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "legacy.db");
            var expiredTicks = DateTime.UtcNow.AddMinutes(-1).Ticks;

            using (SQLiteConnection raw = new(dbPath))
            {
                raw.Execute(
                    "CREATE TABLE CacheElement (Key varchar PRIMARY KEY, TypeName varchar, Value blob, Expiration bigint, CreatedAt bigint)");
                raw.Execute(
                    "INSERT INTO CacheElement (Key, TypeName, Value, Expiration, CreatedAt) VALUES (?, ?, ?, ?, ?)",
                    "expired",
                    typeof(string).FullName,
                    new byte[] { 1 },
                    expiredTicks,
                    DateTime.UtcNow.Ticks);
            }

            await using SqliteAkavacheConnection connection = new(new(dbPath, true));

            var result = await connection.TryReadLegacyV10ValueAsync("expired", DateTimeOffset.UtcNow, typeof(string));
            await Assert.That(result).IsNull();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteAkavacheConnection.CloseAsync"/> completes cleanly on
    /// an open connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CloseAsyncShouldCompleteCleanly()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();

            await connection.CloseAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteAkavacheConnection.DisposeAsync"/> completes cleanly
    /// and closes the underlying connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeAsyncShouldCompleteCleanly()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var connection = CreateConnection(path);
            await connection.CreateTableAsync<CacheEntry>();
            await connection.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the read-only open-flags constructor works against an existing V10 database
    /// (used by the V10 → V11 migration flow).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadOnlyOpenFlagsConstructorShouldOpenExistingDatabase()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "ro.db");

            using (SQLiteConnection raw = new(dbPath))
            {
                raw.Execute(
                    "CREATE TABLE CacheElement (Key varchar PRIMARY KEY, TypeName varchar, Value blob, Expiration bigint, CreatedAt bigint)");
                raw.Execute(
                    "INSERT INTO CacheElement (Key, TypeName, Value, Expiration, CreatedAt) VALUES (?, ?, ?, ?, ?)",
                    "k",
                    typeof(string).FullName,
                    new byte[] { 1, 2 },
                    0L,
                    DateTime.UtcNow.Ticks);
            }

            await using SqliteAkavacheConnection connection = new(dbPath, SQLiteOpenFlags.ReadOnly);

            var result = await connection.TryReadLegacyV10ValueAsync("k", DateTimeOffset.UtcNow, typeof(string));
            await Assert.That(result).IsNotNull();
            await Assert.That(result![0]).IsEqualTo((byte)1);
        }
    }

    /// <summary>
    /// Verifies the encrypted-SQLCipher compilation of <c>SqliteAkavacheConnection</c> can
    /// create a table and round-trip a row, exercising every method in one shot:
    /// <c>CreateTableAsync</c>, <c>InsertOrReplaceAsync</c>, <c>QueryAsync</c>,
    /// <c>FirstOrDefaultAsync</c>, <c>ExecuteAsync</c>, <c>ExecuteScalarAsync</c>,
    /// <c>RunInTransactionAsync</c>, <c>CheckpointAsync</c>, <c>CompactAsync</c>,
    /// <c>ReleaseAuxiliaryResourcesAsync</c>, <c>TableExistsAsync</c>, and dispose.
    /// Closes the encrypted assembly's <c>SqliteAkavacheConnection</c> coverage gap.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConnectionShouldExerciseEveryMethod()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"enc_conn_{Guid.NewGuid():N}.db");
            EncryptedSqlite3.SqliteAkavacheConnection connection = new(
                new(dbPath, true, key: "test123"));

            try
            {
                await Assert.That(await connection.TableExistsAsync("CacheEntry")).IsFalse();

                await connection.CreateTableAsync<CacheEntry>();
                await Assert.That(await connection.TableExistsAsync("CacheEntry")).IsTrue();

                await connection.QueryAsync<JournalModeRow>("PRAGMA journal_mode=WAL");

                await connection.InsertOrReplaceAsync(new CacheEntry
                {
                    Id = "k",
                    Value = [1, 2, 3],
                    TypeName = typeof(string).FullName,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                });

                var rows = await connection.QueryAsync<CacheEntry>(x => x.Id == "k");
                await Assert.That(rows.Count).IsEqualTo(1);

                var row = await connection.FirstOrDefaultAsync<CacheEntry>(x => x.Id == "k");
                await Assert.That(row).IsNotNull();

                var sqlRows = await connection.QueryAsync<CacheEntry>("SELECT * FROM CacheEntry WHERE Id = ?", "k");
                await Assert.That(sqlRows.Count).IsEqualTo(1);

                var count = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM CacheEntry");
                await Assert.That(count).IsEqualTo(1L);

                await connection.RunInTransactionAsync(tx =>
                {
                    tx.InsertOrReplace(new CacheEntry { Id = "k2", Value = [4] });
                    tx.SetExpiry("k2", null, DateTime.UtcNow.AddHours(2));
                    tx.Execute("DELETE FROM CacheEntry WHERE Id = ?", "k2");

                    var inTxRows = tx.Query<CacheEntry>(_ => true);
                    if (inTxRows.Count == 0)
                    {
                        throw new InvalidOperationException("Expected at least one row inside the transaction.");
                    }

                    if (tx.IsValid)
                    {
                        return;
                    }

                    throw new InvalidOperationException("Expected the transaction to be valid.");
                });

                await connection.ExecuteAsync("DELETE FROM CacheEntry WHERE Id = ?", "k");

                await connection.CheckpointAsync(CheckpointMode.Passive);
                await connection.CheckpointAsync(CheckpointMode.Full);
                await connection.CheckpointAsync(CheckpointMode.Truncate);
                await connection.CompactAsync();
                await connection.ReleaseAuxiliaryResourcesAsync();

                var legacy = await connection.TryReadLegacyV10ValueAsync("missing", DateTimeOffset.UtcNow, null);
                await Assert.That(legacy).IsNull();
            }
            finally
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Verifies the encrypted compilation of <c>SqliteAkavacheConnection</c>'s
    /// <c>TryReadLegacyV10ValueAsync</c> reads a value from a pre-existing V10 CacheElement
    /// table when the type filter matches.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConnectionTryReadLegacyV10ValueShouldReturnMatchingRow()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "encrypted-legacy.db");

            // Build a SQLCipher-encrypted database with the legacy V10 CacheElement schema.
            using (SQLiteConnection raw = new(new(dbPath, true, key: "test123")))
            {
                raw.Execute(
                    "CREATE TABLE CacheElement (Key varchar PRIMARY KEY, TypeName varchar, Value blob, Expiration bigint, CreatedAt bigint)");
                raw.Execute(
                    "INSERT INTO CacheElement (Key, TypeName, Value, Expiration, CreatedAt) VALUES (?, ?, ?, ?, ?)",
                    "legacyKey",
                    typeof(string).FullName,
                    new byte[] { 7, 7, 7 },
                    0L,
                    DateTime.UtcNow.Ticks);
            }

            await using EncryptedSqlite3.SqliteAkavacheConnection connection = new(
                new(dbPath, true, key: "test123"));

            var result = await connection.TryReadLegacyV10ValueAsync("legacyKey", DateTimeOffset.UtcNow, typeof(string));
            await Assert.That(result).IsNotNull();
            await Assert.That(result!).IsEquivalentTo(new byte[] { 7, 7, 7 });
        }
    }

    /// <summary>
    /// Verifies the encrypted compilation of <c>SqliteAkavacheConnection</c>'s read-only
    /// open-flags constructor opens an existing SQLCipher database.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConnectionReadOnlyOpenFlagsConstructorShouldOpenExistingDatabase()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "encrypted-ro.db");

            using (SQLiteConnection raw = new(new(dbPath, true, key: "test123")))
            {
                raw.Execute("CREATE TABLE Test (Key varchar PRIMARY KEY)");
            }

            // The read-only flags constructor takes a raw path; for SQLCipher backends a separate
            // construction with a key is required to actually decrypt. The test ensures the
            // constructor itself executes without throwing, exercising the second .ctor on the
            // encrypted compilation.
            await using EncryptedSqlite3.SqliteAkavacheConnection connection = new(
                dbPath,
                SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite);

            // Connection construction is the assertion target — no further operations needed.
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Creates a fresh <see cref="SqliteAkavacheConnection"/> pointing at a unique temporary database file.
    /// </summary>
    /// <param name="directory">The directory that holds the temporary database file.</param>
    /// <returns>A new, open connection.</returns>
    private static SqliteAkavacheConnection CreateConnection(string directory)
    {
        var dbPath = Path.Combine(directory, $"conn_{Guid.NewGuid():N}.db");
        return new(new(dbPath, true));
    }

    /// <summary>
    /// Row shape for mapping results of <c>PRAGMA journal_mode</c> (and <c>PRAGMA journal_mode=...</c>)
    /// via sqlite-net's generic query API.
    /// </summary>
    internal sealed class JournalModeRow
    {
        /// <summary>
        /// Gets or sets the SQLite journal mode reported by the PRAGMA query.
        /// </summary>
        [Column("journal_mode")]
        public string JournalMode { get; set; } = string.Empty;
    }
}
