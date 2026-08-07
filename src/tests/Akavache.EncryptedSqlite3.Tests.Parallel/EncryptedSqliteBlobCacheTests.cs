// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>
/// Tests for the <see cref="EncryptedSqliteBlobCache"/> class. The class also pulls in the
/// inherited <see cref="BlobCacheTestsBase"/> suite which exercises the encrypted backend
/// against a real SQLCipher database, plus an additional set of direct tests that use
/// <see cref="InMemoryAkavacheConnection"/> as the storage backend so that the encrypted
/// assembly's compiled <c>SqliteBlobCache</c> code paths are exercised without needing the
/// SQLCipher native runtime for every test.
/// </summary>
[InheritsTests]
public class EncryptedSqliteBlobCacheTests : BlobCacheTestsBase
{
    /// <summary>Key that exists only in the connection's legacy V10 store.</summary>
    private const string LegacyKey = "legacyKey";

    /// <summary>Key whose seeded row carries a null Id, which the post-query filters must drop.</summary>
    private const string NullIdKey = "nullId";

    /// <summary>Key whose seeded row carries a null payload, which the value filters must drop.</summary>
    private const string NullValueKey = "nullValue";

    /// <summary>How many entries the typed-flow test files under <see cref="string"/>.</summary>
    private const int StringTypedEntryCount = 2;

    /// <summary>How many keys the bulk overloads are asked for at once.</summary>
    private const int BulkKeyCount = 2;

    /// <summary>How many entries the non-typed flow test writes.</summary>
    private const int NonTypedEntryCount = 3;

    /// <summary>How many seeded rows keep a non-null Id and therefore survive the post-query filters.</summary>
    private const int NonNullIdEntryCount = 2;

    /// <summary>Payload used to prove a blob round-trips through the cache unchanged.</summary>
    private static readonly byte[] RoundTripPayload = [1, 2, 3];

    /// <summary>Payload seeded into the connection's legacy V10 store.</summary>
    private static readonly byte[] LegacyPayload = [9, 8, 7];

    /// <summary>Payload for the second key, kept distinct so entries can be told apart.</summary>
    private static readonly byte[] SecondEntryPayload = [2];

    /// <summary>Payload for the third key, kept distinct so entries can be told apart.</summary>
    private static readonly byte[] ThirdEntryPayload = [3];

    /// <summary>Payload for the fourth key, kept distinct so entries can be told apart.</summary>
    private static readonly byte[] FourthEntryPayload = [4];

    /// <summary>Payload of the one seeded row that passes every post-query filter.</summary>
    private static readonly byte[] SurvivingEntryPayload = [9];

    /// <summary>
    /// Verifies the <see cref="EncryptedSqliteBlobCache(IAkavacheConnection, ISerializer, ISequencer)"/>
    /// constructor accepts an <see cref="InMemoryAkavacheConnection"/> and round-trips data —
    /// exercises the constructor + <c>Insert</c> + <c>Get</c> code paths in the encrypted
    /// assembly's compiled <c>SqliteBlobCache</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionInsertAndGetShouldRoundTrip()
    {
        InMemoryAkavacheConnection connection = new();
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        cache.Insert("k", RoundTripPayload).WaitForCompletion();
        var data = cache.Get("k").SubscribeGetValue();
        await Assert.That(data).IsEquivalentTo(RoundTripPayload);
    }

    /// <summary>
    /// Verifies typed insert/get/getAll/invalidate/invalidateAll/keys flow on the encrypted
    /// in-memory-backed cache, exercising the type-aware code paths in the encrypted assembly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionTypedFlowShouldExerciseAllTypeMethods()
    {
        InMemoryAkavacheConnection connection = new();
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        cache.Insert("a", [1], typeof(string)).WaitForCompletion();
        cache.Insert("b", SecondEntryPayload, typeof(string)).WaitForCompletion();
        cache.Insert("c", ThirdEntryPayload, typeof(int)).WaitForCompletion();

        var typedKeys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(typedKeys!.Count).IsEqualTo(StringTypedEntryCount);

        var typedAll = cache.GetAll(typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(typedAll!.Count).IsEqualTo(StringTypedEntryCount);

        var single = cache.Get("a", typeof(string)).SubscribeGetValue();
        await Assert.That(single).IsNotNull();

        var bulkTyped = cache.Get(["a", "b"], typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(bulkTyped!.Count).IsEqualTo(BulkKeyCount);

        var createdAt = cache.GetCreatedAt("a", typeof(string)).SubscribeGetValue();
        await Assert.That(createdAt).IsNotNull();

        var bulkCreatedAt = cache.GetCreatedAt(["a", "b"], typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(bulkCreatedAt!.Count).IsEqualTo(BulkKeyCount);

        cache.Invalidate("a", typeof(string)).WaitForCompletion();
        cache.Invalidate(["b"], typeof(string)).WaitForCompletion();
        cache.InvalidateAll(typeof(int)).WaitForCompletion();

        var remaining = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(remaining).IsEmpty();
    }

    /// <summary>
    /// Verifies the non-typed flow on the encrypted in-memory-backed cache, including
    /// non-typed insert/get/getAllKeys/getCreatedAt/invalidate/invalidateAll/vacuum/flush
    /// and non-typed UpdateExpiration overloads.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionNonTypedFlowShouldExerciseAllMethods()
    {
        InMemoryAkavacheConnection connection = new();
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        cache.Insert("a", [1]).WaitForCompletion();
        cache.Insert("b", SecondEntryPayload).WaitForCompletion();
        cache.Insert([new("c", ThirdEntryPayload)]).WaitForCompletion();

        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys!.Count).IsEqualTo(NonTypedEntryCount);

        var single = cache.Get("a").SubscribeGetValue();
        await Assert.That(single).IsNotNull();

        var bulk = cache.Get(["a", "b"]).ToList().SubscribeGetValue();
        await Assert.That(bulk!.Count).IsEqualTo(BulkKeyCount);

        var createdAt = cache.GetCreatedAt("a").SubscribeGetValue();
        await Assert.That(createdAt).IsNotNull();

        var bulkCreatedAt = cache.GetCreatedAt(["a", "b"]).ToList().SubscribeGetValue();
        await Assert.That(bulkCreatedAt!.Count).IsEqualTo(BulkKeyCount);

        cache.UpdateExpiration("a", TimeProvider.System.GetUtcNow().AddHours(1)).WaitForCompletion();
        cache.UpdateExpiration("b", typeof(string), TimeProvider.System.GetUtcNow().AddHours(1)).WaitForCompletion();
        cache.UpdateExpiration(["a"], TimeProvider.System.GetUtcNow().AddHours(1)).WaitForCompletion();
        cache.UpdateExpiration(["b"], typeof(string), TimeProvider.System.GetUtcNow().AddHours(1)).WaitForCompletion();

        cache.Flush().WaitForCompletion();
        cache.Flush(typeof(string)).WaitForCompletion();
        cache.Vacuum().WaitForCompletion();

        cache.Invalidate("a").WaitForCompletion();
        cache.Invalidate(["b"]).WaitForCompletion();
        cache.InvalidateAll().WaitForCompletion();

        var remaining = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(remaining).IsEmpty();
    }

    /// <summary>
    /// Verifies the encrypted cache surfaces <see cref="ObjectDisposedException"/> through every
    /// public method after disposal — exercises the disposed-state guards in the encrypted
    /// compilation of <c>SqliteBlobCache</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionDisposedShouldThrowForAllOperations()
    {
        InMemoryAkavacheConnection connection = new();
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        cache.Dispose();

        await AssertDisposedForReadOperations(cache);
        await AssertDisposedForWriteOperations(cache);
    }

    /// <summary>Verifies the encrypted cache surfaces <see cref="ArgumentNullException"/> through the null-arg validation paths.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionNullArgsShouldThrow()
    {
        InMemoryAkavacheConnection connection = new();
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        var error = cache.Get((IEnumerable<string>)null!).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.Get((string)null!, typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.Get("k", null!).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.Get((IEnumerable<string>)null!, typeof(string)).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.Get(["k"], null!).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.GetAll(null!).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.GetAllKeys(null!).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.GetCreatedAt((string)null!).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.GetCreatedAt((IEnumerable<string>)null!).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.Insert(null!).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.Insert("k", null!, typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.Insert("k", [1], (Type)null!).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.Invalidate((IEnumerable<string>)null!).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();

        error = cache.Invalidate("k", null!).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that the legacy V10 fallback path is exercised on the encrypted compilation
    /// when the cache misses the V11 store and the connection's legacy store has a value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionGetShouldFallBackToLegacyV10Store()
    {
        InMemoryAkavacheConnection connection = new();
        connection.LegacyV10Store[LegacyKey] = LegacyPayload;

        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        var data = cache.Get(LegacyKey).SubscribeGetValue();
        await Assert.That(data).IsEquivalentTo(LegacyPayload);

        var typedData = cache.Get(LegacyKey, typeof(string)).SubscribeGetValue();
        await Assert.That(typedData).IsEquivalentTo(LegacyPayload);
    }

    /// <summary>Verifies that on the encrypted compilation, missing keys throw <see cref="KeyNotFoundException"/> after exhausting the legacy V10 fallback path.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionGetMissingShouldThrowKeyNotFound()
    {
        using InMemoryAkavacheConnection connection = new();
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        var error = cache.Get("missing").SubscribeGetError();
        await Assert.That(error).IsTypeOf<KeyNotFoundException>();

        error = cache.Get("missing", typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>
    /// Verifies the encrypted compilation tolerates a checkpoint failure during Flush —
    /// exercises the catch branch around <c>CheckpointAsync</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionFlushSwallowsCheckpointFailure()
    {
        using InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        try
        {
            cache.Flush().WaitForCompletion();
            await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            connection.FailCheckpoint = false;
        }
    }

    /// <summary>
    /// Verifies the encrypted compilation's <c>Dispose</c> falls back to <c>CompactAsync</c>
    /// when the checkpoint throws, then proceeds to release auxiliary resources.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionDisposeShouldFallBackToCompactWhenCheckpointFails()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        cache.Dispose();

        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    /// <summary>
    /// Verifies the encrypted compilation's synchronous <c>Dispose</c> path runs the
    /// best-effort cleanup (passive checkpoint then dispose).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionSyncDisposeRunsCleanupPath()
    {
        InMemoryAkavacheConnection connection = new();
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        cache.Dispose();

        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Full);
    }

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null connection — covers the
    /// null-guard branch on the encrypted compilation's third constructor overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullConnectionShouldThrow() =>
        await Assert.That(static () => new EncryptedSqliteBlobCache((IAkavacheConnection)null!, new SystemJsonSerializer()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null filename — covers the
    /// file-name null-guard on the file-name + password constructor overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullFileNameShouldThrow() =>
        await Assert.That(static () => new EncryptedSqliteBlobCache(null!, "test123", new SystemJsonSerializer()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null password — covers the
    /// password null-guard on the file-name + password constructor overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullPasswordShouldThrow() =>
        await Assert.That(static () => new EncryptedSqliteBlobCache("test.db", null!, new SystemJsonSerializer()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies <see cref="EncryptedSqliteBlobCache.BeforeWriteToDiskFilter"/> returns the
    /// supplied data unchanged when the cache is active — exercises the success path of the
    /// filter on the encrypted compilation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedBeforeWriteToDiskFilterShouldReturnDataWhenNotDisposed()
    {
        InMemoryAkavacheConnection connection = new();
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        byte[] input = [10, 20, 30];
        var result = cache.BeforeWriteToDiskFilter(input, ImmediateSequencer.Instance).SubscribeGetValue();
        await Assert.That(result).IsEquivalentTo(input);
    }

    /// <summary>
    /// Verifies the encrypted compilation's typed Insert reports a failing upsert to the caller
    /// rather than completing as though the write landed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedTypedInsertSurfacesUpsertFailure()
    {
        InMemoryAkavacheConnection connection = new() { FailUpsert = true };
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        var error = cache.Insert("k", [1], typeof(string)).SubscribeGetError();

        await Assert.That(error).IsNotNull();
        await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        connection.FailUpsert = false;
    }

    /// <summary>
    /// Verifies the encrypted compilation's <c>UpdateExpiration</c> overloads route through
    /// <c>SetExpiry</c> and mutate the underlying entry. Exercises every UpdateExpiration arm
    /// (single key, key+type, keys, keys+type).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedUpdateExpirationOverloadsShouldMutateEntries()
    {
        InMemoryAkavacheConnection connection = new();
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);
        cache.Insert("k1", [1]).WaitForCompletion();
        cache.Insert("k2", SecondEntryPayload).WaitForCompletion();
        cache.Insert("k3", ThirdEntryPayload, typeof(string)).WaitForCompletion();
        cache.Insert("k4", FourthEntryPayload, typeof(string)).WaitForCompletion();

        var expiry = TimeProvider.System.GetUtcNow().AddHours(1);
        cache.UpdateExpiration("k1", expiry).WaitForCompletion();
        cache.UpdateExpiration(["k2"], expiry).WaitForCompletion();
        cache.UpdateExpiration("k3", typeof(string), expiry).WaitForCompletion();
        cache.UpdateExpiration(["k4"], typeof(string), expiry).WaitForCompletion();

        await Assert.That(connection.Store["k1"].ExpiresAt!.Value).IsEqualTo(expiry.UtcDateTime);
        await Assert.That(connection.Store["k2"].ExpiresAt!.Value).IsEqualTo(expiry.UtcDateTime);
        await Assert.That(connection.Store["k3"].ExpiresAt!.Value).IsEqualTo(expiry.UtcDateTime);
        await Assert.That(connection.Store["k4"].ExpiresAt!.Value).IsEqualTo(expiry.UtcDateTime);
    }

    /// <summary>Verifies the encrypted compilation's synchronous <c>Dispose</c> path tolerates every teardown call throwing.</summary>
    [Test]
    public void EncryptedSyncDisposeTolerantOfAllFailures()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        // Should not throw.
        cache.Dispose();
    }

    /// <summary>Verifies the encrypted compilation's <c>Dispose</c> tolerates every teardown call throwing.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedDisposeTolerantOfAllTeardownFailures()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true, FailCompact = true };
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        cache.Dispose();
    }

    /// <summary>
    /// Verifies the encrypted compilation's post-query defensive <c>x?.Id is not null</c> filters
    /// in the various Get/GetAll/GetCreatedAt overloads skip entries surfaced with a null Id.
    /// Drives the false branches of those filters.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedPostQueryDefensiveFiltersShouldSkipNullIdEntries()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw(
            NullIdKey,
            new(Id: null, typeof(string).FullName, [1], TimeProvider.System.GetUtcNow().UtcDateTime, ExpiresAt: null));
        connection.SeedRaw(
            NullValueKey,
            new(NullValueKey, typeof(string).FullName, Value: null, TimeProvider.System.GetUtcNow().UtcDateTime, ExpiresAt: null));
        connection.SeedRaw(
            "good",
            new("good", typeof(string).FullName, SurvivingEntryPayload, TimeProvider.System.GetUtcNow().UtcDateTime, ExpiresAt: null));

        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        // Bulk Get/GetAll filter by BOTH null Id and null Value, so only "good" passes.
        var bulk = cache.Get([NullIdKey, NullValueKey, "good"]).ToList().SubscribeGetValue();
        await Assert.That(bulk!.Count).IsEqualTo(1);
        await Assert.That(bulk![0].Key).IsEqualTo("good");

        var bulkTyped = cache.Get([NullIdKey, NullValueKey, "good"], typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(bulkTyped!.Count).IsEqualTo(1);

        var all = cache.GetAll(typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(all!.Count).IsEqualTo(1);

        // GetAllKeys / GetCreatedAt only filter by null Id, so "nullValue" (Id non-null) passes too.
        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys!.Count).IsEqualTo(NonNullIdEntryCount);
        var typedKeys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(typedKeys!.Count).IsEqualTo(NonNullIdEntryCount);

        var createdAt = cache.GetCreatedAt([NullIdKey, NullValueKey, "good"]).ToList().SubscribeGetValue();
        await Assert.That(createdAt!.Count).IsEqualTo(NonNullIdEntryCount);
        var typedCreatedAt = cache.GetCreatedAt([NullIdKey, NullValueKey, "good"], typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(typedCreatedAt!.Count).IsEqualTo(NonNullIdEntryCount);
    }

    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer) =>
        new EncryptedSqliteBlobCache(new InMemoryAkavacheConnection(), serializer, ImmediateSequencer.Instance);

    /// <summary>Asserts every read-shaped operation on a disposed cache surfaces <see cref="ObjectDisposedException"/>.</summary>
    /// <param name="cache">A cache that has already been disposed.</param>
    /// <returns>A task.</returns>
    private static async Task AssertDisposedForReadOperations(EncryptedSqliteBlobCache cache)
    {
        var error = cache.Get("k").SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Get(["k"]).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Get("k", typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Get(["k"], typeof(string)).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.GetAllKeys().ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.GetAll(typeof(string)).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.GetCreatedAt("k").SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.GetCreatedAt(["k"]).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.GetCreatedAt("k", typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.GetCreatedAt(["k"], typeof(string)).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>Asserts every mutating operation on a disposed cache surfaces <see cref="ObjectDisposedException"/>.</summary>
    /// <param name="cache">A cache that has already been disposed.</param>
    /// <returns>A task.</returns>
    private static async Task AssertDisposedForWriteOperations(EncryptedSqliteBlobCache cache)
    {
        var error = cache.Insert("k", [1]).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Insert([new("k", [1])]).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Insert("k", [1], typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Insert([new("k", [1])], typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Flush().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Flush(typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Invalidate("k").SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Invalidate(["k"]).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Invalidate("k", typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Invalidate(["k"], typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.InvalidateAll().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.InvalidateAll(typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Vacuum().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.UpdateExpiration("k", TimeProvider.System.GetLocalNow()).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.UpdateExpiration(["k"], TimeProvider.System.GetLocalNow()).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.UpdateExpiration("k", typeof(string), TimeProvider.System.GetLocalNow()).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.UpdateExpiration(["k"], typeof(string), TimeProvider.System.GetLocalNow()).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.BeforeWriteToDiskFilter(RoundTripPayload, ImmediateSequencer.Instance).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }
}
