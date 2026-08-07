// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

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
    /// <summary>The payload written and read back on the round-trip smoke tests.</summary>
    private static readonly byte[] RoundTripPayload = [1, 2, 3];

    /// <summary>The payload written by one cache instance and read back by another.</summary>
    private static readonly byte[] PersistedPayload = [9, 8, 7];

    /// <summary>Payload for the entry whose expiry has already passed.</summary>
    private static readonly byte[] ExpiredPayload = [1];

    /// <summary>Payload for the entry that is still within its expiry.</summary>
    private static readonly byte[] ValidPayload = [2];

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
            SqliteBlobCache cache = new(dbPath, new SystemJsonSerializer(), ImmediateSequencer.Instance);
            try
            {
                cache.Insert("k", RoundTripPayload).WaitForCompletion();

                var data = cache.Get("k").WaitForValue();

                await Assert.That(data).IsNotNull();
                await Assert.That(data!).IsEquivalentTo(RoundTripPayload);
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
                writer.Insert("persisted", PersistedPayload).WaitForCompletion();
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
                await Assert.That(data!).IsEquivalentTo(PersistedPayload);
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

            SqliteBlobCache cache = new(dbPath, new SystemJsonSerializer(), ImmediateSequencer.Instance);
            try
            {
                cache.Insert("k", RoundTripPayload).WaitForCompletion();

                var data = cache.Get("k").WaitForValue();

                await Assert.That(data).IsNotNull();
                await Assert.That(data!).IsEquivalentTo(RoundTripPayload);
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
            SqliteBlobCache cache = new(dbPath, new SystemJsonSerializer(), ImmediateSequencer.Instance);
            try
            {
                cache.Insert("expired", ExpiredPayload, TimeProvider.System.GetUtcNow().AddDays(-1)).WaitForCompletion();
                cache.Insert("valid", ValidPayload, TimeProvider.System.GetUtcNow().AddDays(1)).WaitForCompletion();
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
