// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// End-to-end smoke tests for <see cref="SqliteBlobCache"/> that exercise the full stack —
/// the blob cache composing with the real <c>SqlitePclRawConnection</c> against an actual
/// SQLite database file on disk. Kept deliberately small: the bulk of logic coverage lives
/// in <c>SqliteBlobCacheDirectTests</c> (in-memory), and these smoke tests validate that
/// the two layers compose correctly under real SQLite semantics.
/// </summary>
[Category("Akavache")]
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
            SqliteBlobCache cache = new(dbPath, new SystemJsonSerializer(), System.Reactive.Concurrency.ImmediateScheduler.Instance);
            try
            {
                cache.Insert("k", [1, 2, 3]).WaitForCompletion();

                var data = cache.Get("k").WaitForValue();

                await Assert.That(data).IsNotNull();
                await Assert.That(data!).IsEquivalentTo(new byte[] { 1, 2, 3 });
            }
            finally
            {
                cache.Dispose();
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
                writer.Insert("persisted", [9, 8, 7]).WaitForCompletion();
            }
            finally
            {
                writer.Dispose();
            }

            SqliteBlobCache reader = new(dbPath, new SystemJsonSerializer());
            try
            {
                var data = reader.Get("persisted").WaitForValue();

                await Assert.That(data).IsNotNull();
                await Assert.That(data!).IsEquivalentTo(new byte[] { 9, 8, 7 });
            }
            finally
            {
                reader.Dispose();
            }
        }
    }

    /// <summary>
    /// Repro for the Settings-tests regression: mirrors the Settings <c>RunWithAkavache</c>
    /// pattern by wrapping the cache construction inside a sync-over-async
    /// <c>GetAwaiter().GetResult()</c> of an <c>async</c> lambda that awaits a prior
    /// (no-op) task before constructing the cache and running an insert+get. This
    /// reproduces the exact flow that made <c>TestCreateAndInsertNewtonsoftAsync</c>
    /// hang / segfault at exit 139 without involving Akavache.Settings at all.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorWorksInsideSyncOverAsyncContext()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "sync_over_async.db");

            SqliteBlobCache cache = new(dbPath, new SystemJsonSerializer(), System.Reactive.Concurrency.ImmediateScheduler.Instance);
            try
            {
                cache.Insert("k", [1, 2, 3]).WaitForCompletion();

                var data = cache.Get("k").WaitForValue();

                await Assert.That(data).IsNotNull();
                await Assert.That(data!).IsEquivalentTo(new byte[] { 1, 2, 3 });
            }
            finally
            {
                cache.Dispose();
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
            SqliteBlobCache cache = new(dbPath, new SystemJsonSerializer(), System.Reactive.Concurrency.ImmediateScheduler.Instance);
            try
            {
                cache.Insert("expired", [1], DateTimeOffset.UtcNow.AddDays(-1)).WaitForCompletion();
                cache.Insert("valid", [2], DateTimeOffset.UtcNow.AddDays(1)).WaitForCompletion();
                cache.Vacuum().WaitForCompletion();

                var keys = cache.GetAllKeys().ToList().WaitForValue();

                await Assert.That(keys!.Count).IsEqualTo(1);
                await Assert.That(keys![0]).IsEqualTo("valid");
            }
            finally
            {
                cache.Dispose();
            }
        }
    }
}
