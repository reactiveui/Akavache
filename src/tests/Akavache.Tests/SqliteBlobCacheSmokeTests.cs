// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// End-to-end smoke tests for <see cref="SqliteBlobCache"/> that exercise the full stack —
/// <see cref="SqliteBlobCache"/> composing with the real <see cref="SqliteAkavacheConnection"/>
/// against an actual SQLite database file on disk. Kept deliberately small: the bulk of
/// logic coverage lives in <c>SqliteBlobCacheDirectTests</c> (in-memory) and SQL translation
/// coverage lives in <c>SqliteAkavacheConnectionTests</c>. These smoke tests validate that
/// the two layers compose correctly under real SQLite semantics.
/// </summary>
[Category("Akavache")]
[NotInParallel("NativeSqlite")]
public class SqliteBlobCacheSmokeTests
{
    /// <summary>
    /// Verifies that a simple insert + get round-trip works against a real SQLite database.
    /// This is the canonical end-to-end smoke test.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertAndGetRoundTripAgainstRealDatabase()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "smoke.db");
            SqliteBlobCache cache = new(dbPath, new SystemJsonSerializer());
            try
            {
                await cache.Insert("k", [1, 2, 3]).ToTask();
                var data = await cache.Get("k").ToTask();

                await Assert.That(data).IsNotNull();
                await Assert.That(data!).IsEquivalentTo(new byte[] { 1, 2, 3 });
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Verifies that data written by one <see cref="SqliteBlobCache"/> instance is visible to
    /// a second instance opened against the same database file — proving the durable
    /// checkpoint/dispose path actually persists data to disk, not just to an in-process WAL.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DataPersistsAcrossCacheInstancesOnSamePath()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "durable.db");

            SqliteBlobCache writer = new(dbPath, new SystemJsonSerializer());
            try
            {
                await writer.Insert("persisted", [9, 8, 7]).ToTask();
            }
            finally
            {
                await writer.DisposeAsync();
            }

            SqliteBlobCache reader = new(dbPath, new SystemJsonSerializer());
            try
            {
                var data = await reader.Get("persisted").ToTask();
                await Assert.That(data).IsNotNull();
                await Assert.That(data!).IsEquivalentTo(new byte[] { 9, 8, 7 });
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Verifies that expired entries are filtered out of query results against a real SQLite
    /// database, and that <see cref="SqliteBlobCache.Vacuum"/> completes successfully against
    /// a real backing file.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumAgainstRealDatabaseShouldSucceedAndFilterExpired()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "vacuum.db");
            SqliteBlobCache cache = new(dbPath, new SystemJsonSerializer());
            try
            {
                await cache.Insert("expired", [1], DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await cache.Insert("valid", [2], DateTimeOffset.UtcNow.AddDays(1)).ToTask();

                await cache.Vacuum().ToTask();

                var keys = await cache.GetAllKeys().ToList().ToTask();
                await Assert.That(keys.Count).IsEqualTo(1);
                await Assert.That(keys[0]).IsEqualTo("valid");
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }
}
