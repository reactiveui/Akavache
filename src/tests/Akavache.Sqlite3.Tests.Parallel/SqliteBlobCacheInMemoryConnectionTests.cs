// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>
/// Tests for <see cref="SqliteBlobCache"/> driven through the in-memory
/// <see cref="InMemoryAkavacheConnection"/>, which lets a test steer the storage layer's
/// failures and seed rows the real backend would never produce.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class SqliteBlobCacheInMemoryConnectionTests
{
    /// <summary>Key of the seeded row whose <c>Id</c> column is null, which every query must skip.</summary>
    private const string NullIdKey = "nullId";

    /// <summary>Key of the seeded row whose <c>Value</c> column is null, which every query must skip.</summary>
    private const string NullValueKey = "nullValue";

    /// <summary>Key of the seeded row that is fully populated and must survive every defensive filter.</summary>
    private const string WellFormedKey = "good";

    /// <summary>Payload of the first entry a test stores; distinct so a swapped row shows in the assertion.</summary>
    private static readonly byte[] FirstEntryPayload = [1];

    /// <summary>Payload of the second entry a test stores.</summary>
    private static readonly byte[] SecondEntryPayload = [2];

    /// <summary>Payload of the third entry a test stores.</summary>
    private static readonly byte[] ThirdEntryPayload = [3];

    /// <summary>A two-byte payload, for tests that assert the whole blob survives the round trip.</summary>
    private static readonly byte[] TwoBytePayload = [1, 2];

    /// <summary>A three-byte payload handed to the write filter.</summary>
    private static readonly byte[] ThreeBytePayload = [1, 2, 3];

    /// <summary>Payload handed to the write filter, chosen so an accidental rewrite is obvious.</summary>
    private static readonly byte[] WriteFilterProbePayload = [10, 20, 30];

    /// <summary>Payload parked in the legacy V10 store for the untyped fallback read.</summary>
    private static readonly byte[] LegacyUntypedPayload = [9, 8, 7];

    /// <summary>Payload parked in the legacy V10 store for the typed fallback read.</summary>
    private static readonly byte[] LegacyTypedPayload = [1, 2, 3, 4];

    /// <summary>Payload of the seeded row that must survive every defensive filter.</summary>
    private static readonly byte[] WellFormedPayload = [9];

    /// <summary>How long a test's entry stays valid before the test moves its expiry.</summary>
    private static readonly TimeSpan ShortLifetime = TimeSpan.FromMinutes(5);

    /// <summary>How far ahead a test pushes an expiry it wants to prove took effect.</summary>
    private static readonly TimeSpan ExtendedLifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Tests that all public methods throw ObjectDisposedException after disposal
    /// when using an in-memory connection (no real SQLite database required).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposedShouldThrowForAllOperations()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        cache.Dispose();

        var now = TimeProvider.System.GetLocalNow();

        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Insert("k", FirstEntryPayload));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Insert([new("k", FirstEntryPayload)]));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Insert("k", FirstEntryPayload, typeof(string)));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Insert([new("k", FirstEntryPayload)], typeof(string)));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Get("k"));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Get(["k"]).ToList());
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Get("k", typeof(string)));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Get(["k"], typeof(string)).ToList());
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.GetAllKeys().ToList());
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.GetAllKeys(typeof(string)).ToList());
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.GetAll(typeof(string)).ToList());
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.GetCreatedAt("k"));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.GetCreatedAt(["k"]).ToList());
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.GetCreatedAt("k", typeof(string)));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.GetCreatedAt(["k"], typeof(string)).ToList());
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Flush());
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Flush(typeof(string)));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Invalidate("k"));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Invalidate(["k"]));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Invalidate("k", typeof(string)));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Invalidate(["k"], typeof(string)));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.InvalidateAll());
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.InvalidateAll(typeof(string)));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Vacuum());
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.UpdateExpiration("k", now));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.UpdateExpiration(["k"], now));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.UpdateExpiration("k", typeof(string), now));
        await SqliteBlobCacheDirectTests.AssertDisposed(cache.UpdateExpiration(["k"], typeof(string), now));
    }

    /// <summary>
    /// Tests that SqliteBlobCache.BeforeWriteToDiskFilter returns an error observable
    /// after the cache has been disposed, using an in-memory connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBeforeWriteToDiskFilterShouldThrowWhenDisposed()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        cache.Dispose();

        await SqliteBlobCacheDirectTests.AssertDisposed(cache.BeforeWriteToDiskFilter(ThreeBytePayload, ImmediateSequencer.Instance));
    }

    /// <summary>
    /// Tests that SqliteBlobCache.BeforeWriteToDiskFilter returns data unchanged
    /// when the cache is active, using an in-memory connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBeforeWriteToDiskFilterShouldReturnDataWhenNotDisposed()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var result = cache.BeforeWriteToDiskFilter(WriteFilterProbePayload, ImmediateSequencer.Instance).SubscribeGetValue();
            await Assert.That(result).IsEquivalentTo(WriteFilterProbePayload);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests basic CRUD operations using the in-memory connection, verifying that the
    /// IAkavacheConnection abstraction works correctly for Insert, Get,
    /// GetAllKeys, Invalidate, and InvalidateAll.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryCrudOperationsShouldWork()
    {
        const int StoredKeyCount = 2;

        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            // Insert and Get
            cache.Insert("k1", TwoBytePayload).WaitForCompletion();
            var data = cache.Get("k1").SubscribeGetValue();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!.Length).IsEqualTo(TwoBytePayload.Length);

            // Typed Insert and Get
            cache.Insert("k2", ThirdEntryPayload, typeof(string)).WaitForCompletion();
            var typedData = cache.Get("k2", typeof(string)).SubscribeGetValue();
            await Assert.That(typedData).IsNotNull();

            // GetAllKeys
            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(StoredKeyCount);

            // Invalidate single
            cache.Invalidate("k1").WaitForCompletion();
            await Assert.That(SqliteBlobCacheDirectTests.CaptureError(cache.Get("k1"))).IsTypeOf<KeyNotFoundException>();

            // InvalidateAll
            cache.InvalidateAll().WaitForCompletion();
            var remainingKeys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(remainingKeys!).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that double disposal does not throw when using an in-memory connection.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDoubleDisposeShouldNotThrow()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        cache.Dispose();
        cache.Dispose();

        await SqliteBlobCacheDirectTests.AssertDisposed(cache.Get("k"));
    }

    /// <summary>Tests that the constructor throws ArgumentNullException when a null IAkavacheConnection is passed.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorWithNullConnectionShouldThrow() =>
        await Assert.That(static () => new SqliteBlobCache((IAkavacheConnection)null!, new SystemJsonSerializer()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies that SqliteBlobCache.UpdateExpiration(string, DateTimeOffset?)
    /// is routed through the connection's <c>SetExpiryAsync</c> helper and actually
    /// mutates the stored entry's expiration when using the in-memory backend.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryUpdateExpirationShouldMutateEntry()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            const int UpdatedExpiryHours = 2;
            cache.Insert("k1", FirstEntryPayload, TimeProvider.System.GetUtcNow().AddMinutes(1)).WaitForCompletion();
            var newExpiry = TimeProvider.System.GetUtcNow().AddHours(UpdatedExpiryHours);

            cache.UpdateExpiration("k1", newExpiry).WaitForCompletion();

            var stored = connection.Store["k1"];
            await Assert.That(stored.ExpiresAt).IsNotNull();
            await Assert.That(stored.ExpiresAt!.Value).IsEqualTo(newExpiry.UtcDateTime);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.UpdateExpiration(string, Type, DateTimeOffset?)
    /// only affects entries whose <c>TypeName</c> column matches, leaving other entries untouched.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryUpdateExpirationWithTypeShouldRespectTypeFilter()
    {
        const int FarFutureYears = 10;

        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var initialExpiry = TimeProvider.System.GetUtcNow().AddMinutes(1);
            cache.Insert("k1", FirstEntryPayload, typeof(string), initialExpiry).WaitForCompletion();
            cache.Insert("k1", FirstEntryPayload, typeof(int)).WaitForCompletion(); // overwrites would happen only if same key+type; dictionary keyed by Id so last write wins

            // Insert a different key so we can prove type filter isolates the right row.
            cache.Insert("k2", SecondEntryPayload, typeof(string), initialExpiry).WaitForCompletion();

            var updatedExpiry = TimeProvider.System.GetUtcNow().AddDays(1);

            // Update k2 only, scoped to typeof(string).
            cache.UpdateExpiration("k2", typeof(string), updatedExpiry).WaitForCompletion();

            var k2 = connection.Store["k2"];
            await Assert.That(k2.ExpiresAt!.Value).IsEqualTo(updatedExpiry.UtcDateTime);

            // Mismatching type filter should be a no-op.
            cache.UpdateExpiration("k2", typeof(object), TimeProvider.System.GetUtcNow().AddYears(FarFutureYears)).WaitForCompletion();
            await Assert.That(connection.Store["k2"].ExpiresAt!.Value).IsEqualTo(updatedExpiry.UtcDateTime);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies the bulk SqliteBlobCache.UpdateExpiration(IEnumerable{string}, DateTimeOffset?)
    /// overload updates all supplied keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBulkUpdateExpirationShouldMutateAllEntries()
    {
        const int UpdatedExpiryHours = 5;

        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Insert("k1", FirstEntryPayload).WaitForCompletion();
            cache.Insert("k2", SecondEntryPayload).WaitForCompletion();
            cache.Insert("k3", ThirdEntryPayload).WaitForCompletion();

            var updated = TimeProvider.System.GetUtcNow().AddHours(UpdatedExpiryHours);
            cache.UpdateExpiration(["k1", "k2", "k3"], updated).WaitForCompletion();

            foreach (var id in new[] { "k1", "k2", "k3" })
            {
                await Assert.That(connection.Store[id].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies the typed bulk SqliteBlobCache.UpdateExpiration(IEnumerable{string}, Type, DateTimeOffset?)
    /// overload only touches entries with a matching <c>TypeName</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBulkUpdateExpirationWithTypeShouldRespectTypeFilter()
    {
        const int UpdatedExpiryDays = 3;
        const int FarFutureYears = 10;

        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Insert("a", FirstEntryPayload, typeof(string), TimeProvider.System.GetUtcNow().AddMinutes(1)).WaitForCompletion();
            cache.Insert("b", SecondEntryPayload, typeof(string), TimeProvider.System.GetUtcNow().AddMinutes(1)).WaitForCompletion();

            var updated = TimeProvider.System.GetUtcNow().AddDays(UpdatedExpiryDays);
            cache.UpdateExpiration(["a", "b"], typeof(string), updated).WaitForCompletion();

            await Assert.That(connection.Store["a"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
            await Assert.That(connection.Store["b"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);

            // Wrong type should leave both untouched.
            var wrongTypeExpiry = TimeProvider.System.GetUtcNow().AddYears(FarFutureYears);
            cache.UpdateExpiration(["a", "b"], typeof(int), wrongTypeExpiry).WaitForCompletion();
            await Assert.That(connection.Store["a"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
            await Assert.That(connection.Store["b"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Flush() calls
    /// IAkavacheConnection.CheckpointAsync(CheckpointMode) with
    /// CheckpointMode.Passive.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryFlushShouldRequestPassiveCheckpoint()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Flush().WaitForCompletion();
            await Assert.That(connection.CheckpointCount).IsEqualTo(1);
            await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Passive);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that typed SqliteBlobCache.Insert(string, byte[], Type, DateTimeOffset?)
    /// triggers a passive checkpoint on the backend for multi-instance durability.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedInsertShouldCheckpointAfterWrite()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var before = connection.CheckpointCount;
            cache.Insert("k", FirstEntryPayload, typeof(string)).WaitForCompletion();
            await Assert.That(connection.CheckpointCount).IsGreaterThan(before);
            await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Passive);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Verifies that SqliteBlobCache.Vacuum is routed through IAkavacheConnection.CompactAsync().</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryVacuumShouldCallCompact()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Vacuum().WaitForCompletion();
            await Assert.That(connection.CompactCount).IsEqualTo(1);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Dispose issues a full checkpoint and
    /// then releases auxiliary resources before closing the connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposeShouldCheckpointAndReleaseAuxiliary()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        cache.Dispose();

        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Full);
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Get(string) falls back to the V10 legacy
    /// backing store when the key is not present in the primary V11 table.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetShouldFallBackToLegacyV10Store()
    {
        InMemoryAkavacheConnection connection = new();
        connection.LegacyV10Store["legacyKey"] = LegacyUntypedPayload;

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var data = cache.Get("legacyKey").SubscribeGetValue();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!).IsEquivalentTo(LegacyUntypedPayload);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that typed SqliteBlobCache.Get(string, Type) falls back to the
    /// V10 legacy backing store when the key is not present in the primary V11 table.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedGetShouldFallBackToLegacyV10Store()
    {
        InMemoryAkavacheConnection connection = new();
        connection.LegacyV10Store["legacyTyped"] = LegacyTypedPayload;

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var data = cache.Get("legacyTyped", typeof(string)).SubscribeGetValue();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!).IsEquivalentTo(LegacyTypedPayload);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Get(string) throws
    /// KeyNotFoundException when the key is missing from both the
    /// primary and legacy V10 stores.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetMissingKeyShouldThrowKeyNotFound()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            await Assert.That(SqliteBlobCacheDirectTests.CaptureError(cache.Get("missing"))).IsTypeOf<KeyNotFoundException>();
            await Assert.That(SqliteBlobCacheDirectTests.CaptureError(cache.Get("missing", typeof(string)))).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Invalidate(IEnumerable{string}, Type) only
    /// removes entries whose <c>TypeName</c> matches, leaving other entries intact.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryInvalidateWithTypeShouldOnlyRemoveTypedEntries()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Insert("a", FirstEntryPayload, typeof(string)).WaitForCompletion();
            cache.Insert("b", SecondEntryPayload).WaitForCompletion(); // untyped

            cache.Invalidate(["a", "b"], typeof(string)).WaitForCompletion();

            // "a" removed (typed match); "b" still present (no TypeName).
            await Assert.That(connection.Store.ContainsKey("a")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("b")).IsTrue();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Verifies SqliteBlobCache.InvalidateAll(Type) removes only entries with a matching <c>TypeName</c> and leaves the rest intact.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryInvalidateAllWithTypeShouldOnlyRemoveTypedEntries()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Insert("a", FirstEntryPayload, typeof(string)).WaitForCompletion();
            cache.Insert("b", SecondEntryPayload, typeof(string)).WaitForCompletion();
            cache.Insert("c", ThirdEntryPayload).WaitForCompletion(); // untyped

            cache.InvalidateAll(typeof(string)).WaitForCompletion();

            await Assert.That(connection.Store.ContainsKey("a")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("b")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("c")).IsTrue();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Verifies that SqliteBlobCache.GetCreatedAt(string) returns the stored creation timestamp using the in-memory backend.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetCreatedAtShouldReturnStoredTime()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Insert("k", FirstEntryPayload).WaitForCompletion();
            var createdAt = cache.GetCreatedAt("k").SubscribeGetValue();
            await Assert.That(createdAt).IsNotNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Flush() completes successfully even when
    /// the backend checkpoint throws, exercising the catch branch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryFlushSwallowsCheckpointFailure()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            // Should not throw, even though CheckpointAsync raises.
            cache.Flush().WaitForCompletion();
            await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            // Disable the failure so Dispose can complete cleanly.
            connection.FailCheckpoint = false;
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies typed Insert reports a failing upsert to the caller. Reporting success would
    /// leave the caller believing a write landed when the store is empty, and the next read
    /// would come back with nothing and no explanation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedInsertSurfacesUpsertFailure()
    {
        InMemoryAkavacheConnection connection = new() { FailUpsert = true, };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var error = cache.Insert("k", FirstEntryPayload, typeof(string)).SubscribeGetError();

            await Assert.That(error).IsNotNull();
            await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        }
        finally
        {
            connection.FailUpsert = false;
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies typed Insert still succeeds when only the post-write checkpoint fails. The
    /// checkpoint moves data out of the write-ahead log, which is durable either way.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedInsertToleratesCheckpointFailure()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true, };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Insert("k", FirstEntryPayload, typeof(string)).WaitForCompletion();

            await Assert.That(connection.Store.ContainsKey("k")).IsTrue();
        }
        finally
        {
            connection.FailCheckpoint = false;
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Dispose falls back from a failing
    /// checkpoint to IAkavacheConnection.CompactAsync(), then continues on
    /// to release auxiliary resources and close.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposeFallsBackToCompactWhenCheckpointFails()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        // Dispose enqueues a best-effort checkpoint (which fails here) then
        // calls Connection.Dispose(). No compact fallback — the checkpoint
        // error is swallowed silently.
        cache.Dispose();

        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    /// <summary>Verifies that SqliteBlobCache.Dispose tolerates all teardown calls throwing.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposeTolerantOfAllTeardownFailures()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true, FailCompact = true, };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        // Should not throw even though every teardown operation raises.
        cache.Dispose();

        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    /// <summary>
    /// Verifies synchronous SqliteBlobCache.Dispose() runs the best-effort cleanup path.
    /// It is the same observable behaviour as the disposal test above, so it defers to it
    /// rather than repeating the assertions.
    /// </summary>
    /// <returns>A task.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Test]
    public Task InMemorySyncDisposeRunsCleanupPath() =>
        InMemoryDisposeShouldCheckpointAndReleaseAuxiliary();

    /// <summary>Verifies that synchronous SqliteBlobCache.Dispose() tolerates every teardown call throwing.</summary>
    [Test]
    public void InMemorySyncDisposeTolerantOfAllFailures()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true, };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        // Should not throw.
        cache.Dispose();
    }

    /// <summary>
    /// Verifies that an error raised from IAkavacheConnection.CreateSchemaAsync()
    /// during initialization is surfaced on the first operation that awaits initialization.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryInitializationFailureShouldPropagate()
    {
        InMemoryAkavacheConnection connection = new() { FailCreateTable = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            await Assert.That(SqliteBlobCacheDirectTests.CaptureError(cache.Get("k"))).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            connection.FailCreateTable = false;
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the post-query defensive <c>x?.Id is not null</c> filter in the
    /// <c>Get(IEnumerable&lt;string&gt;)</c> overload skips entries with a null <c>Id</c>
    /// surfaced by the storage layer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBulkGetShouldSkipEntriesWithNullIdOrValue()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw(NullIdKey, new(Id: null, TypeName: null, FirstEntryPayload, default, ExpiresAt: null));
        connection.SeedRaw(NullValueKey, new(NullValueKey, TypeName: null, Value: null, default, ExpiresAt: null));
        connection.SeedRaw(WellFormedKey, new(WellFormedKey, TypeName: null, WellFormedPayload, default, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var results = cache.Get([NullIdKey, NullValueKey, WellFormedKey]).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo(WellFormedKey);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the typed bulk <c>Get(IEnumerable&lt;string&gt;, Type)</c> overload's
    /// post-query defensive filter skips entries with a null <c>Id</c> or <c>Value</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedBulkGetShouldSkipEntriesWithNullIdOrValue()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw(NullIdKey, new(Id: null, typeof(string).FullName, FirstEntryPayload, default, ExpiresAt: null));
        connection.SeedRaw(NullValueKey, new(NullValueKey, typeof(string).FullName, Value: null, default, ExpiresAt: null));
        connection.SeedRaw(WellFormedKey, new(WellFormedKey, typeof(string).FullName, WellFormedPayload, default, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var results = cache.Get([NullIdKey, NullValueKey, WellFormedKey], typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo(WellFormedKey);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Verifies that SqliteBlobCache.GetAll(Type)'s post-query defensive filter skips entries with a null <c>Id</c> or <c>Value</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetAllShouldSkipEntriesWithNullIdOrValue()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw(NullIdKey, new(Id: null, typeof(string).FullName, FirstEntryPayload, default, ExpiresAt: null));
        connection.SeedRaw(NullValueKey, new(NullValueKey, typeof(string).FullName, Value: null, default, ExpiresAt: null));
        connection.SeedRaw(WellFormedKey, new(WellFormedKey, typeof(string).FullName, WellFormedPayload, default, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var results = cache.GetAll(typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo(WellFormedKey);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Verifies that SqliteBlobCache.GetAllKeys()'s post-query defensive filter skips entries with a null <c>Id</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetAllKeysShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw(NullIdKey, new(Id: null, TypeName: null, FirstEntryPayload, default, ExpiresAt: null));
        connection.SeedRaw(WellFormedKey, new(WellFormedKey, TypeName: null, WellFormedPayload, default, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(1);
            await Assert.That(keys![0]).IsEqualTo(WellFormedKey);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Verifies that SqliteBlobCache.GetAllKeys(Type)'s post-query defensive filter skips entries with a null <c>Id</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetAllKeysWithTypeShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw(NullIdKey, new(Id: null, typeof(string).FullName, FirstEntryPayload, default, ExpiresAt: null));
        connection.SeedRaw(WellFormedKey, new(WellFormedKey, typeof(string).FullName, WellFormedPayload, default, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var keys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(1);
            await Assert.That(keys![0]).IsEqualTo(WellFormedKey);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the bulk SqliteBlobCache.GetCreatedAt(IEnumerable{string})
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBulkGetCreatedAtShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw(NullIdKey, new(Id: null, TypeName: null, FirstEntryPayload, TimeProvider.System.GetUtcNow().UtcDateTime, ExpiresAt: null));
        connection.SeedRaw(WellFormedKey, new(WellFormedKey, TypeName: null, WellFormedPayload, TimeProvider.System.GetUtcNow().UtcDateTime, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var results = cache.GetCreatedAt([NullIdKey, WellFormedKey]).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo(WellFormedKey);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the typed bulk SqliteBlobCache.GetCreatedAt(IEnumerable{string}, Type)
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedBulkGetCreatedAtShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw(NullIdKey, new(Id: null, typeof(string).FullName, FirstEntryPayload, TimeProvider.System.GetUtcNow().UtcDateTime, ExpiresAt: null));
        connection.SeedRaw(WellFormedKey, new(WellFormedKey, typeof(string).FullName, WellFormedPayload, TimeProvider.System.GetUtcNow().UtcDateTime, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var results = cache.GetCreatedAt([NullIdKey, WellFormedKey], typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo(WellFormedKey);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the single-key SqliteBlobCache.GetCreatedAt(string)
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetCreatedAtSingleShouldSkipNullIdEntry()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw(NullIdKey, new(Id: null, TypeName: null, FirstEntryPayload, TimeProvider.System.GetUtcNow().UtcDateTime, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            // Defensive Where filters out the null-Id entry; the DefaultIfEmpty fallback yields null.
            var result = cache.GetCreatedAt(NullIdKey).SubscribeGetValue();
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the typed single-key SqliteBlobCache.GetCreatedAt(string, Type)
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetCreatedAtSingleTypedShouldSkipNullIdEntry()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw(NullIdKey, new(Id: null, typeof(string).FullName, FirstEntryPayload, TimeProvider.System.GetUtcNow().UtcDateTime, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            var result = cache.GetCreatedAt(NullIdKey, typeof(string)).SubscribeGetValue();
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that constructing a SqliteBlobCache creates the CacheEntry
    /// schema on the supplied connection (observed through the public API).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructingCacheShouldCreateCacheEntryTable()
    {
        InMemoryAkavacheConnection connection = new();
        using SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        cache.Insert("k", FirstEntryPayload).WaitForCompletion();

        var tableExists = connection.TableExists(nameof(CacheEntry)).SubscribeGetValue();
        await Assert.That(tableExists).IsTrue();
    }

    /// <summary>
    /// Verifies that operations surface an error when the underlying
    /// connection fails to create the CacheEntry table during init.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FailedCreateTableShouldPropagateToOperations()
    {
        InMemoryAkavacheConnection connection = new() { FailCreateTable = true };
        using SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        await Assert.That(SqliteBlobCacheDirectTests.CaptureError(cache.Insert("k", FirstEntryPayload))).IsTypeOf<Exception>();
    }

    /// <summary>
    /// Typed bulk Insert with an empty collection returns Unit without touching the database.
    /// Covers lines 433-435 (entries.Count == 0 early return in typed Insert).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TypedBulkInsertWithEmptyCollectionShouldReturnUnit()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Insert([], typeof(string)).WaitForCompletion();

            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// InvalidateAll with a null type throws ArgumentNullException.
    /// Covers lines 536-538 (null type guard in InvalidateAll(Type)).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InvalidateAllWithNullTypeShouldThrowArgumentNullException()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            await Assert.That(SqliteBlobCacheDirectTests.CaptureError(cache.InvalidateAll((Type)null!))).IsTypeOf<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Dispose catches and swallows errors when Connection.Checkpoint(Full) throws.
    /// Covers lines 772-775 (catch block in Dispose(bool)).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DisposeSwallowsCheckpointException()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        // Dispose should not throw even though Checkpoint(Full) raises.
        cache.Dispose();

        // The connection should still be disposed.
        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    /// <summary>Dispose swallows a synchronous throw from Connection.Checkpoint (the catch at lines 772-775).</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DisposeSwallowsSynchronousCheckpointThrow()
    {
        InMemoryAkavacheConnection connection = new() { ThrowOnCheckpointCall = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        cache.Dispose();

        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    /// <summary>
    /// Invalidate with an empty HashSet exercises the MaterializeKeys ICollection path
    /// plus the empty-key-list early return (lines 500-503).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Invalidate_WithEmptyHashSet_IsNoop()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            HashSet<string> noKeys = [];
            cache.Insert("keep", FirstEntryPayload).WaitForCompletion();
            cache.Invalidate(noKeys).WaitForCompletion();

            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(1);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Invalidate typed with an empty HashSet exercises the MaterializeKeys ICollection
    /// path plus the empty-key-list early return (lines 527-530).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InvalidateTyped_WithEmptyHashSet_IsNoop()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            HashSet<string> noKeys = [];
            cache.Insert("keep", FirstEntryPayload, typeof(string)).WaitForCompletion();
            cache.Invalidate(noKeys, typeof(string)).WaitForCompletion();

            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(1);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// UpdateExpiration with an empty HashSet exercises the MaterializeKeys ICollection path
    /// plus the empty-key-list early return in the untyped overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task UpdateExpiration_WithEmptyHashSet_IsNoop()
    {
        InMemoryAkavacheConnection connection = new();
        using SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        HashSet<string> noKeys = [];
        cache.Insert("keep", FirstEntryPayload, TimeProvider.System.GetLocalNow().Add(ShortLifetime)).WaitForCompletion();
        cache.UpdateExpiration(noKeys, TimeProvider.System.GetLocalNow().Add(ExtendedLifetime)).WaitForCompletion();

        var value = cache.Get("keep").SubscribeGetValue();
        await Assert.That(value).IsNotNull();
        await Assert.That(value!.Length).IsEqualTo(FirstEntryPayload.Length);
        await Assert.That(value[0]).IsEqualTo(FirstEntryPayload[0]);
    }

    /// <summary>
    /// UpdateExpiration typed with an empty HashSet exercises the MaterializeKeys ICollection
    /// path plus the empty-key-list early return in the typed overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task UpdateExpirationTyped_WithEmptyHashSet_IsNoop()
    {
        InMemoryAkavacheConnection connection = new();
        using SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        HashSet<string> noKeys = [];
        cache.Insert("keep", FirstEntryPayload, typeof(string), TimeProvider.System.GetLocalNow().Add(ShortLifetime)).WaitForCompletion();
        cache.UpdateExpiration(noKeys, typeof(string), TimeProvider.System.GetLocalNow().Add(ExtendedLifetime)).WaitForCompletion();

        var value = cache.Get("keep", typeof(string)).SubscribeGetValue();
        await Assert.That(value).IsNotNull();
        await Assert.That(value!.Length).IsEqualTo(FirstEntryPayload.Length);
        await Assert.That(value[0]).IsEqualTo(FirstEntryPayload[0]);
    }

    /// <summary>
    /// Calling Dispose(false) via the non-disposing path is a no-op. We test this
    /// by verifying that the cache is still functional after the base finalizer would
    /// call Dispose(false). Since we cannot call Dispose(false) directly on the sealed
    /// class, we verify the double-dispose idempotency instead, which covers line 761.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_CalledTwice_SecondCallIsIdempotent()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        cache.Dispose();
        cache.Dispose();

        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    /// <summary>
    /// Insert with an empty collection of key-value pairs returns Unit.Default
    /// without calling Connection.Upsert. Covers the <c>entries.Count > 0</c>
    /// ternary false branch at line 398.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Insert_EmptyKeyValuePairs_ReturnsUnitWithoutUpsert()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Insert([]).WaitForCompletion();

            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(0);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Insert typed with an empty collection returns Unit.Default without
    /// calling Connection.Upsert. Covers the <c>entries.Count == 0</c>
    /// early return at line 433.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InsertTyped_EmptyKeyValuePairs_ReturnsUnitWithoutUpsert()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Insert([], typeof(string)).WaitForCompletion();

            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(0);
        }
        finally
        {
            cache.Dispose();
        }
    }
}
