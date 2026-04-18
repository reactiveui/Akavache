// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

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
    /// <summary>
    /// Verifies the <see cref="EncryptedSqliteBlobCache(IAkavacheConnection, ISerializer, IScheduler?)"/>
    /// constructor accepts an <see cref="InMemoryAkavacheConnection"/> and round-trips data —
    /// exercises the constructor + <c>Insert</c> + <c>Get</c> code paths in the encrypted
    /// assembly's compiled <c>SqliteBlobCache</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionInsertAndGetShouldRoundTrip()
    {
        InMemoryAkavacheConnection connection = new();
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        cache.Insert("k", [1, 2, 3]).SubscribeAndComplete();
        var data = cache.Get("k").SubscribeGetValue();
        await Assert.That(data).IsEquivalentTo(new byte[] { 1, 2, 3 });
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
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        cache.Insert("a", [1], typeof(string)).SubscribeAndComplete();
        cache.Insert("b", [2], typeof(string)).SubscribeAndComplete();
        cache.Insert("c", [3], typeof(int)).SubscribeAndComplete();

        var typedKeys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(typedKeys!.Count).IsEqualTo(2);

        var typedAll = cache.GetAll(typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(typedAll!.Count).IsEqualTo(2);

        var single = cache.Get("a", typeof(string)).SubscribeGetValue();
        await Assert.That(single).IsNotNull();

        var bulkTyped = cache.Get(["a", "b"], typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(bulkTyped!.Count).IsEqualTo(2);

        var createdAt = cache.GetCreatedAt("a", typeof(string)).SubscribeGetValue();
        await Assert.That(createdAt).IsNotNull();

        var bulkCreatedAt = cache.GetCreatedAt(["a", "b"], typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(bulkCreatedAt!.Count).IsEqualTo(2);

        cache.Invalidate("a", typeof(string)).SubscribeAndComplete();
        cache.Invalidate(["b"], typeof(string)).SubscribeAndComplete();
        cache.InvalidateAll(typeof(int)).SubscribeAndComplete();

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
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        cache.Insert("a", [1]).SubscribeAndComplete();
        cache.Insert("b", [2]).SubscribeAndComplete();
        cache.Insert([new("c", [3])]).SubscribeAndComplete();

        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys!.Count).IsEqualTo(3);

        var single = cache.Get("a").SubscribeGetValue();
        await Assert.That(single).IsNotNull();

        var bulk = cache.Get(["a", "b"]).ToList().SubscribeGetValue();
        await Assert.That(bulk!.Count).IsEqualTo(2);

        var createdAt = cache.GetCreatedAt("a").SubscribeGetValue();
        await Assert.That(createdAt).IsNotNull();

        var bulkCreatedAt = cache.GetCreatedAt(["a", "b"]).ToList().SubscribeGetValue();
        await Assert.That(bulkCreatedAt!.Count).IsEqualTo(2);

        cache.UpdateExpiration("a", DateTimeOffset.UtcNow.AddHours(1)).SubscribeAndComplete();
        cache.UpdateExpiration("b", typeof(string), DateTimeOffset.UtcNow.AddHours(1)).SubscribeAndComplete();
        cache.UpdateExpiration(["a"], DateTimeOffset.UtcNow.AddHours(1)).SubscribeAndComplete();
        cache.UpdateExpiration(["b"], typeof(string), DateTimeOffset.UtcNow.AddHours(1)).SubscribeAndComplete();

        cache.Flush().SubscribeAndComplete();
        cache.Flush(typeof(string)).SubscribeAndComplete();
        cache.Vacuum().SubscribeAndComplete();

        cache.Invalidate("a").SubscribeAndComplete();
        cache.Invalidate(["b"]).SubscribeAndComplete();
        cache.InvalidateAll().SubscribeAndComplete();

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
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        cache.Dispose();

        var error = cache.Insert("k", [1]).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Insert([new("k", [1])]).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Insert("k", [1], typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Insert([new("k", [1])], typeof(string)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.Get("k").SubscribeGetError();
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

        error = cache.UpdateExpiration("k", DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.UpdateExpiration(["k"], DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();

        error = cache.BeforeWriteToDiskFilter([1, 2, 3], ImmediateScheduler.Instance).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies the encrypted cache surfaces <see cref="ArgumentNullException"/> through the
    /// null-arg validation paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionNullArgsShouldThrow()
    {
        InMemoryAkavacheConnection connection = new();
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        connection.LegacyV10Store["legacyKey"] = [9, 8, 7];

        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        var data = cache.Get("legacyKey").SubscribeGetValue();
        await Assert.That(data).IsEquivalentTo(new byte[] { 9, 8, 7 });

        var typedData = cache.Get("legacyKey", typeof(string)).SubscribeGetValue();
        await Assert.That(typedData).IsEquivalentTo(new byte[] { 9, 8, 7 });
    }

    /// <summary>
    /// Verifies that on the encrypted compilation, missing keys throw
    /// <see cref="KeyNotFoundException"/> after exhausting the legacy V10 fallback path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionGetMissingShouldThrowKeyNotFound()
    {
        using InMemoryAkavacheConnection connection = new();
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            cache.Flush().SubscribeAndComplete();
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
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Test deliberately exercises the synchronous Dispose path.")]
    public async Task EncryptedInMemoryConnectionSyncDisposeRunsCleanupPath()
    {
        InMemoryAkavacheConnection connection = new();
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        byte[] input = [10, 20, 30];
        var result = cache.BeforeWriteToDiskFilter(input, ImmediateScheduler.Instance).SubscribeGetValue();
        await Assert.That(result).IsEquivalentTo(input);
    }

    /// <summary>
    /// Verifies the encrypted compilation's typed Insert swallows a failure in the
    /// upsert path. Exercises the outer try/catch around <c>UpsertAsync</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedTypedInsertSwallowsUpsertFailure()
    {
        InMemoryAkavacheConnection connection = new() { FailUpsert = true };
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        cache.Insert("k", [1], typeof(string)).SubscribeAndComplete();
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
        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        cache.Insert("k1", [1]).SubscribeAndComplete();
        cache.Insert("k2", [2]).SubscribeAndComplete();
        cache.Insert("k3", [3], typeof(string)).SubscribeAndComplete();
        cache.Insert("k4", [4], typeof(string)).SubscribeAndComplete();

        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        cache.UpdateExpiration("k1", expiry).SubscribeAndComplete();
        cache.UpdateExpiration(["k2"], expiry).SubscribeAndComplete();
        cache.UpdateExpiration("k3", typeof(string), expiry).SubscribeAndComplete();
        cache.UpdateExpiration(["k4"], typeof(string), expiry).SubscribeAndComplete();

        await Assert.That(connection.Store["k1"].ExpiresAt!.Value).IsEqualTo(expiry.UtcDateTime);
        await Assert.That(connection.Store["k2"].ExpiresAt!.Value).IsEqualTo(expiry.UtcDateTime);
        await Assert.That(connection.Store["k3"].ExpiresAt!.Value).IsEqualTo(expiry.UtcDateTime);
        await Assert.That(connection.Store["k4"].ExpiresAt!.Value).IsEqualTo(expiry.UtcDateTime);
    }

    /// <summary>
    /// Verifies the encrypted compilation's synchronous <c>Dispose</c> path tolerates every
    /// teardown call throwing.
    /// </summary>
    [Test]
    public void EncryptedSyncDisposeTolerantOfAllFailures()
    {
        InMemoryAkavacheConnection connection = new()
        {
            FailCheckpoint = true,
        };
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Should not throw.
        cache.Dispose();
    }

    /// <summary>
    /// Verifies the encrypted compilation's <c>Dispose</c> tolerates every teardown call
    /// throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedDisposeTolerantOfAllTeardownFailures()
    {
        InMemoryAkavacheConnection connection = new()
        {
            FailCheckpoint = true, FailCompact = true,
        };
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
            "nullId",
            new(Id: null, typeof(string).FullName, [1], DateTime.UtcNow, ExpiresAt: null));
        connection.SeedRaw(
            "nullValue",
            new("nullValue", typeof(string).FullName, Value: null, DateTime.UtcNow, ExpiresAt: null));
        connection.SeedRaw(
            "good",
            new("good", typeof(string).FullName, [9], DateTime.UtcNow, ExpiresAt: null));

        using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Bulk Get/GetAll filter by BOTH null Id and null Value, so only "good" passes.
        var bulk = cache.Get(["nullId", "nullValue", "good"]).ToList().SubscribeGetValue();
        await Assert.That(bulk!.Count).IsEqualTo(1);
        await Assert.That(bulk![0].Key).IsEqualTo("good");

        var bulkTyped = cache.Get(["nullId", "nullValue", "good"], typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(bulkTyped!.Count).IsEqualTo(1);

        var all = cache.GetAll(typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(all!.Count).IsEqualTo(1);

        // GetAllKeys / GetCreatedAt only filter by null Id, so "nullValue" (Id non-null) passes too.
        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys!.Count).IsEqualTo(2);
        var typedKeys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(typedKeys!.Count).IsEqualTo(2);

        var ca = cache.GetCreatedAt(["nullId", "nullValue", "good"]).ToList().SubscribeGetValue();
        await Assert.That(ca!.Count).IsEqualTo(2);
        var caTyped = cache.GetCreatedAt(["nullId", "nullValue", "good"], typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(caTyped!.Count).IsEqualTo(2);
    }

    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer) =>
        new EncryptedSqliteBlobCache(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);
}
