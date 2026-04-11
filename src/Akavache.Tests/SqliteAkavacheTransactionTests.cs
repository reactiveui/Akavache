// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;
using Akavache.Tests.Helpers;
using Akavache.Tests.Mocks;
using SQLite;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="IAkavacheTransaction"/> implementations: the real
/// <see cref="SqliteAkavacheTransaction"/> (against an actual SQLite file inside
/// <c>RunInTransactionAsync</c>) and the dictionary-backed
/// <see cref="InMemoryAkavacheTransaction"/>. Each test runs against both implementations
/// to prove behavioural parity at the transaction-handle abstraction.
/// </summary>
[Category("Akavache")]
public class SqliteAkavacheTransactionTests
{
    /// <summary>
    /// Verifies <see cref="IAkavacheTransaction.InsertOrReplace{T}"/> writes a row that is
    /// readable post-commit, on both backends.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertOrReplaceShouldPersistEntry()
    {
        await foreach (var connection in CreateConnections())
        {
            try
            {
                await connection.CreateTableAsync<CacheEntry>();

                await connection.RunInTransactionAsync(tx =>
                    tx.InsertOrReplace(new CacheEntry { Id = "k", Value = [1, 2] }));

                var rows = await connection.QueryAsync<CacheEntry>(_ => true);
                await Assert.That(rows.Count).IsEqualTo(1);
                await Assert.That(rows[0].Id).IsEqualTo("k");
                await Assert.That(rows[0].Value!).IsEquivalentTo(new byte[] { 1, 2 });
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Verifies <see cref="IAkavacheTransaction.Delete{T}"/> removes a row by primary key
    /// on both backends.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeleteShouldRemoveEntry()
    {
        await foreach (var connection in CreateConnections())
        {
            try
            {
                await connection.CreateTableAsync<CacheEntry>();
                await connection.InsertOrReplaceAsync(new CacheEntry { Id = "k", Value = [1] });

                await connection.RunInTransactionAsync(tx => tx.Delete<CacheEntry>("k"));

                var rows = await connection.QueryAsync<CacheEntry>(_ => true);
                await Assert.That(rows).IsEmpty();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Verifies <see cref="IAkavacheTransaction.Query{T}"/> returns rows matching the
    /// supplied predicate on both backends.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task QueryShouldReturnFilteredRows()
    {
        await foreach (var connection in CreateConnections())
        {
            try
            {
                await connection.CreateTableAsync<CacheEntry>();
                await connection.InsertOrReplaceAsync(new CacheEntry { Id = "a", Value = [1], TypeName = "T1" });
                await connection.InsertOrReplaceAsync(new CacheEntry { Id = "b", Value = [2], TypeName = "T1" });
                await connection.InsertOrReplaceAsync(new CacheEntry { Id = "c", Value = [3], TypeName = "T2" });

                List<CacheEntry>? collected = null;
                await connection.RunInTransactionAsync(tx => collected = tx.Query<CacheEntry>(x => x.TypeName == "T1"));

                await Assert.That(collected).IsNotNull();
                await Assert.That(collected!.Count).IsEqualTo(2);
                await Assert.That(collected!.Any(x => x.Id == "a")).IsTrue();
                await Assert.That(collected!.Any(x => x.Id == "b")).IsTrue();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Verifies <see cref="IAkavacheTransaction.SetExpiry"/> updates a single entry's
    /// <c>ExpiresAt</c> column when no type filter is supplied, on both backends.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetExpiryWithoutTypeShouldUpdateRow()
    {
        await foreach (var connection in CreateConnections())
        {
            try
            {
                await connection.CreateTableAsync<CacheEntry>();
                await connection.InsertOrReplaceAsync(new CacheEntry { Id = "k", Value = [1], ExpiresAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

                var newExpiry = new DateTime(2040, 6, 15, 12, 0, 0, DateTimeKind.Utc);
                await connection.RunInTransactionAsync(tx => tx.SetExpiry("k", null, newExpiry));

                var row = await connection.FirstOrDefaultAsync<CacheEntry>(x => x.Id == "k");
                await Assert.That(row).IsNotNull();
                await Assert.That(row!.ExpiresAt!.Value.Year).IsEqualTo(2040);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Verifies <see cref="IAkavacheTransaction.SetExpiry"/> with a matching <c>typeFullName</c>
    /// updates the row, and with a mismatching <c>typeFullName</c> leaves it untouched, on
    /// both backends.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetExpiryWithTypeFilterShouldRespectTypeName()
    {
        await foreach (var connection in CreateConnections())
        {
            try
            {
                await connection.CreateTableAsync<CacheEntry>();
                await connection.InsertOrReplaceAsync(new CacheEntry
                {
                    Id = "k",
                    Value = [1],
                    TypeName = "MatchingType",
                    ExpiresAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });

                // Mismatching type — no-op.
                await connection.RunInTransactionAsync(tx =>
                    tx.SetExpiry("k", "WrongType", new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

                var row = await connection.FirstOrDefaultAsync<CacheEntry>(x => x.Id == "k");
                await Assert.That(row!.ExpiresAt!.Value.Year).IsEqualTo(2025);

                // Matching type — updates.
                await connection.RunInTransactionAsync(tx =>
                    tx.SetExpiry("k", "MatchingType", new DateTime(2050, 3, 1, 0, 0, 0, DateTimeKind.Utc)));

                row = await connection.FirstOrDefaultAsync<CacheEntry>(x => x.Id == "k");
                await Assert.That(row!.ExpiresAt!.Value.Year).IsEqualTo(2050);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Verifies <see cref="IAkavacheTransaction.Execute"/> runs raw SQL inside a transaction
    /// on both backends. The in-memory implementation is a no-op, so it does not throw and
    /// the row remains; the SQLite backend honours the DELETE statement.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteShouldRunWithoutThrowing()
    {
        await foreach (var connection in CreateConnections())
        {
            try
            {
                await connection.CreateTableAsync<CacheEntry>();
                await connection.InsertOrReplaceAsync(new CacheEntry { Id = "k", Value = [1] });

                // Should not throw on either backend.
                await connection.RunInTransactionAsync(tx =>
                    tx.Execute("DELETE FROM CacheEntry WHERE Id = ?", "k"));
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Verifies <see cref="IAkavacheTransaction.IsValid"/> reports <c>true</c> for a fresh,
    /// open transaction on both backends.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsValidShouldBeTrueForFreshTransaction()
    {
        await foreach (var connection in CreateConnections())
        {
            try
            {
                await connection.CreateTableAsync<CacheEntry>();

                var observed = false;
                await connection.RunInTransactionAsync(tx => observed = tx.IsValid);

                await Assert.That(observed).IsTrue();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Verifies that the in-memory transaction reports <see cref="IAkavacheTransaction.IsValid"/>
    /// as <c>false</c> when the parent connection is configured to simulate an invalid
    /// underlying handle.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryIsValidShouldRespectSimulateNullConnection()
    {
        await using var connection = new InMemoryAkavacheConnection { SimulateNullConnection = true };
        await connection.CreateTableAsync<CacheEntry>();

        var observed = true;
        await connection.RunInTransactionAsync(tx => observed = tx.IsValid);

        await Assert.That(observed).IsFalse();
    }

    /// <summary>
    /// Verifies that <see cref="SqliteAkavacheTransaction"/>'s constructor throws
    /// <see cref="ArgumentNullException"/> when passed a null connection. This covers the
    /// last unhit branch in the SQLite transaction implementation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SqliteTransactionConstructorShouldThrowOnNullConnection() =>
        await Assert.That(() => new SqliteAkavacheTransaction(null!)).Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies the encrypted compilation's <c>SqliteAkavacheTransaction</c> constructor
    /// throws <see cref="ArgumentNullException"/> when passed a null connection — closes
    /// the matching branch in the encrypted assembly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedSqliteTransactionConstructorShouldThrowOnNullConnection() =>
        await Assert.That(() => new Akavache.EncryptedSqlite3.SqliteAkavacheTransaction(null!)).Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies that the in-memory transaction's <c>IsValidTrueCallsRemaining</c> counter
    /// flips <see cref="IAkavacheTransaction.IsValid"/> to <c>false</c> after the configured
    /// number of true reads, used to drive mid-loop guard tests in
    /// <c>SqliteBlobCache.Insert</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryIsValidShouldFlipAfterConfiguredCallCount()
    {
        await using var connection = new InMemoryAkavacheConnection
        {
            TransactionIsValidTrueCallsRemaining = 1,
        };
        await connection.CreateTableAsync<CacheEntry>();

        var first = false;
        var second = false;
        await connection.RunInTransactionAsync(tx =>
        {
            first = tx.IsValid;
            second = tx.IsValid;
        });

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsFalse();
    }

    private static async IAsyncEnumerable<IAkavacheConnection> CreateConnections()
    {
        // In-memory backend.
        yield return new InMemoryAkavacheConnection();

        // Real SQLite backend wrapped in a temp file. Note: yielded connections are owned
        // by the caller (each test disposes via `await connection.DisposeAsync()`).
        var sqliteTempDir = Path.Combine(Path.GetTempPath(), $"akavache_tx_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sqliteTempDir);

        var sqlite = new SqliteAkavacheConnection(new SQLiteConnectionString(Path.Combine(sqliteTempDir, "tx.db"), true));
        try
        {
            yield return sqlite;
        }
        finally
        {
            try
            {
                Utility.DeleteDirectory(sqliteTempDir);
            }
            catch
            {
                // Best effort cleanup.
            }
        }

        // Encrypted SQLite (SQLCipher) backend — exercises the same source files compiled
        // into the Akavache.EncryptedSqlite3 assembly so its IL gets coverage.
        var encryptedTempDir = Path.Combine(Path.GetTempPath(), $"akavache_tx_enc_{Guid.NewGuid():N}");
        Directory.CreateDirectory(encryptedTempDir);

        var encrypted = new Akavache.EncryptedSqlite3.SqliteAkavacheConnection(
            new SQLite.SQLiteConnectionString(Path.Combine(encryptedTempDir, "tx.db"), true, key: "test123"));
        try
        {
            yield return encrypted;
        }
        finally
        {
            try
            {
                Utility.DeleteDirectory(encryptedTempDir);
            }
            catch
            {
                // Best effort cleanup.
            }
        }

        await Task.CompletedTask;
    }
}
