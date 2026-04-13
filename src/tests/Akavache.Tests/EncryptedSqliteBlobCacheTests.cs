// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using Akavache.EncryptedSqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;
using SQLite;

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
[NotInParallel("CacheDatabaseState")]
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
        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.Insert("k", [1, 2, 3]).ToTask();
        var data = await cache.Get("k").ToTask();
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
        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.Insert("a", [1], typeof(string)).ToTask();
        await cache.Insert("b", [2], typeof(string)).ToTask();
        await cache.Insert("c", [3], typeof(int)).ToTask();

        var typedKeys = await cache.GetAllKeys(typeof(string)).ToList().ToTask();
        await Assert.That(typedKeys.Count).IsEqualTo(2);

        var typedAll = await cache.GetAll(typeof(string)).ToList().ToTask();
        await Assert.That(typedAll.Count).IsEqualTo(2);

        var single = await cache.Get("a", typeof(string)).ToTask();
        await Assert.That(single).IsNotNull();

        var bulkTyped = await cache.Get(["a", "b"], typeof(string)).ToList().ToTask();
        await Assert.That(bulkTyped.Count).IsEqualTo(2);

        var createdAt = await cache.GetCreatedAt("a", typeof(string)).ToTask();
        await Assert.That(createdAt).IsNotNull();

        var bulkCreatedAt = await cache.GetCreatedAt(["a", "b"], typeof(string)).ToList().ToTask();
        await Assert.That(bulkCreatedAt.Count).IsEqualTo(2);

        await cache.Invalidate("a", typeof(string)).ToTask();
        await cache.Invalidate(["b"], typeof(string)).ToTask();
        await cache.InvalidateAll(typeof(int)).ToTask();

        var remaining = await cache.GetAllKeys().ToList().ToTask();
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
        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.Insert("a", [1]).ToTask();
        await cache.Insert("b", [2]).ToTask();
        await cache.Insert([new("c", [3])]).ToTask();

        var keys = await cache.GetAllKeys().ToList().ToTask();
        await Assert.That(keys.Count).IsEqualTo(3);

        var single = await cache.Get("a").ToTask();
        await Assert.That(single).IsNotNull();

        var bulk = await cache.Get(["a", "b"]).ToList().ToTask();
        await Assert.That(bulk.Count).IsEqualTo(2);

        var createdAt = await cache.GetCreatedAt("a").ToTask();
        await Assert.That(createdAt).IsNotNull();

        var bulkCreatedAt = await cache.GetCreatedAt(["a", "b"]).ToList().ToTask();
        await Assert.That(bulkCreatedAt.Count).IsEqualTo(2);

        await cache.UpdateExpiration("a", DateTimeOffset.UtcNow.AddHours(1)).ToTask();
        await cache.UpdateExpiration("b", typeof(string), DateTimeOffset.UtcNow.AddHours(1)).ToTask();
        await cache.UpdateExpiration(["a"], DateTimeOffset.UtcNow.AddHours(1)).ToTask();
        await cache.UpdateExpiration(["b"], typeof(string), DateTimeOffset.UtcNow.AddHours(1)).ToTask();

        await cache.Flush().ToTask();
        await cache.Flush(typeof(string)).ToTask();
        await cache.Vacuum().ToTask();

        await cache.Invalidate("a").ToTask();
        await cache.Invalidate(["b"]).ToTask();
        await cache.InvalidateAll().ToTask();

        var remaining = await cache.GetAllKeys().ToList().ToTask();
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
        await cache.DisposeAsync();

        await cache.Insert("k", [1]).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Insert([new("k", [1])]).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Insert("k", [1], typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])], typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.Get("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Get(["k"]).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Get("k", typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Get(["k"], typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.GetAllKeys().ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.GetAllKeys(typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.GetAll(typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.GetCreatedAt("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.GetCreatedAt(["k"]).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.GetCreatedAt("k", typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.GetCreatedAt(["k"], typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.Flush().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Flush(typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.Invalidate("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Invalidate(["k"]).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Invalidate("k", typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Invalidate(["k"], typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.InvalidateAll().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.InvalidateAll(typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.Vacuum().ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.UpdateExpiration("k", DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.UpdateExpiration(["k"], DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.BeforeWriteToDiskFilter([1, 2, 3], ImmediateScheduler.Instance).ToTask().ShouldThrowAsync<ObjectDisposedException>();
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
        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.Get((IEnumerable<string>)null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.Get((string)null!, typeof(string)).ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.Get("k", null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.Get((IEnumerable<string>)null!, typeof(string)).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.Get(["k"], null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.GetAll(null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.GetAllKeys(null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.GetCreatedAt((string)null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.GetCreatedAt((IEnumerable<string>)null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.Insert(null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.Insert("k", null!, typeof(string)).ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.Insert("k", [1], (Type)null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.Invalidate((IEnumerable<string>)null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
        await cache.Invalidate("k", null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
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

        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        var data = await cache.Get("legacyKey").ToTask();
        await Assert.That(data).IsEquivalentTo(new byte[] { 9, 8, 7 });

        var typedData = await cache.Get("legacyKey", typeof(string)).ToTask();
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
        await using InMemoryAkavacheConnection connection = new();
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.Get("missing").ToTask().ShouldThrowAsync<KeyNotFoundException>();
        await cache.Get("missing", typeof(string)).ToTask().ShouldThrowAsync<KeyNotFoundException>();
    }

    /// <summary>
    /// Verifies the encrypted compilation tolerates a checkpoint failure during Flush —
    /// exercises the catch branch around <c>CheckpointAsync</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionFlushSwallowsCheckpointFailure()
    {
        await using InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Flush().ToTask();
            await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            connection.FailCheckpoint = false;
        }
    }

    /// <summary>
    /// Verifies the encrypted compilation's <c>DisposeAsync</c> falls back to <c>CompactAsync</c>
    /// when the checkpoint throws, then proceeds to release auxiliary resources.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionDisposeAsyncShouldFallBackToCompactWhenCheckpointFails()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        await cache.DisposeAsync();

        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.CompactCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.ReleaseAuxiliaryResourcesCount).IsGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Verifies the encrypted compilation's synchronous <c>Dispose</c> path runs the
    /// best-effort cleanup (truncate checkpoint + release auxiliary + close).
    /// </summary>
    [Test]
    public void EncryptedInMemoryConnectionSyncDisposeRunsCleanupPath()
    {
        InMemoryAkavacheConnection connection = new();
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        cache.Dispose();

        // Asserts run inline because the test method must remain synchronous to exercise the sync Dispose path.
        if (connection.LastCheckpointMode != CheckpointMode.Truncate)
        {
            throw new InvalidOperationException("Expected Truncate checkpoint mode after sync Dispose.");
        }

        if (connection.ReleaseAuxiliaryResourcesCount >= 1)
        {
            return;
        }

        throw new InvalidOperationException("Expected ReleaseAuxiliaryResources to be called at least once.");
    }

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null connection — covers the
    /// null-guard branch on the encrypted compilation's third constructor overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullConnectionShouldThrow() =>
        await Assert.That(() => new EncryptedSqliteBlobCache((IAkavacheConnection)null!, new SystemJsonSerializer()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null connection-string — covers
    /// the null-guard branch on the encrypted compilation's connection-string constructor.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullConnectionStringShouldThrow() =>
        await Assert.That(() => new EncryptedSqliteBlobCache((SQLiteConnectionString)null!, new SystemJsonSerializer()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null filename — covers the
    /// file-name null-guard on the file-name + password constructor overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullFileNameShouldThrow() =>
        await Assert.That(() => new EncryptedSqliteBlobCache(null!, "test123", new SystemJsonSerializer()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null password — covers the
    /// password null-guard on the file-name + password constructor overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullPasswordShouldThrow() =>
        await Assert.That(() => new EncryptedSqliteBlobCache("test.db", null!, new SystemJsonSerializer()))
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
        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        byte[] input = [10, 20, 30];
        var result = await cache.BeforeWriteToDiskFilter(input, ImmediateScheduler.Instance).ToTask();
        await Assert.That(result).IsEquivalentTo(input);
    }

    /// <summary>
    /// Verifies the encrypted compilation's typed Insert outer <c>!tx.IsValid</c> guard
    /// returns early on the first iteration when the transaction is immediately invalid.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedTypedInsertOuterInvalidGuardShouldReturnEarly()
    {
        InMemoryAkavacheConnection connection = new() { TransactionIsValidTrueCallsRemaining = 0, };
        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.Insert("k", [1], typeof(string)).ToTask();
        await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        connection.TransactionIsValidTrueCallsRemaining = int.MaxValue;
    }

    /// <summary>
    /// Verifies the encrypted compilation's typed Insert mid-loop <c>!tx.IsValid</c> guard
    /// returns early once the transaction reports invalid between iterations.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedTypedInsertInnerInvalidGuardShouldReturnEarly()
    {
        InMemoryAkavacheConnection connection = new() { TransactionIsValidTrueCallsRemaining = 1, };
        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.Insert(
            [new KeyValuePair<string, byte[]>("a", [1]), new KeyValuePair<string, byte[]>("b", [2])],
            typeof(string)).ToTask();

        await Assert.That(connection.Store.ContainsKey("a")).IsFalse();
        await Assert.That(connection.Store.ContainsKey("b")).IsFalse();
        connection.TransactionIsValidTrueCallsRemaining = int.MaxValue;
    }

    /// <summary>
    /// Verifies the encrypted compilation's typed Insert swallows an inner per-entry
    /// <c>InsertOrReplace</c> failure (covers the inner try/catch in the loop) and tolerates
    /// a post-write checkpoint failure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedTypedInsertSwallowsInnerInsertFailure()
    {
        InMemoryAkavacheConnection connection = new()
        {
            FailInsertOrReplaceInTransaction = true, FailCheckpoint = true,
        };
        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.Insert("k", [1], typeof(string)).ToTask();
        await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        connection.FailCheckpoint = false;
        connection.FailInsertOrReplaceInTransaction = false;
    }

    /// <summary>
    /// Verifies the encrypted compilation's typed Insert swallows a
    /// <c>RunInTransactionAsync</c> failure at the outer try/catch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedTypedInsertSwallowsOuterTransactionFailure()
    {
        InMemoryAkavacheConnection connection = new() { FailRunInTransaction = true };
        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.Insert("k", [1], typeof(string)).ToTask();
        await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        connection.FailRunInTransaction = false;
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
        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.Insert("k1", [1]).ToTask();
        await cache.Insert("k2", [2]).ToTask();
        await cache.Insert("k3", [3], typeof(string)).ToTask();
        await cache.Insert("k4", [4], typeof(string)).ToTask();

        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        await cache.UpdateExpiration("k1", expiry).ToTask();
        await cache.UpdateExpiration(["k2"], expiry).ToTask();
        await cache.UpdateExpiration("k3", typeof(string), expiry).ToTask();
        await cache.UpdateExpiration(["k4"], typeof(string), expiry).ToTask();

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
            FailCheckpoint = true, FailReleaseAuxiliaryResources = true, FailClose = true,
        };
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Should not throw.
        cache.Dispose();
    }

    /// <summary>
    /// Verifies the encrypted compilation's <c>DisposeAsync</c> tolerates every teardown call
    /// throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedDisposeAsyncTolerantOfAllTeardownFailures()
    {
        InMemoryAkavacheConnection connection = new()
        {
            FailCheckpoint = true, FailCompact = true, FailReleaseAuxiliaryResources = true, FailClose = true,
        };
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        await cache.DisposeAsync();
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
            new() { Id = null, Value = [1], CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName });
        connection.SeedRaw(
            "nullValue",
            new()
            {
                Id = "nullValue", Value = null, CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName
            });
        connection.SeedRaw(
            "good",
            new()
            {
                Id = "good", Value = [9], CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName
            });

        await using EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Bulk Get/GetAll filter by BOTH null Id and null Value, so only "good" passes.
        var bulk = await cache.Get(["nullId", "nullValue", "good"]).ToList().ToTask();
        await Assert.That(bulk.Count).IsEqualTo(1);
        await Assert.That(bulk[0].Key).IsEqualTo("good");

        var bulkTyped = await cache.Get(["nullId", "nullValue", "good"], typeof(string)).ToList().ToTask();
        await Assert.That(bulkTyped.Count).IsEqualTo(1);

        var all = await cache.GetAll(typeof(string)).ToList().ToTask();
        await Assert.That(all.Count).IsEqualTo(1);

        // GetAllKeys / GetCreatedAt only filter by null Id, so "nullValue" (Id non-null) passes too.
        var keys = await cache.GetAllKeys().ToList().ToTask();
        await Assert.That(keys.Count).IsEqualTo(2);
        var typedKeys = await cache.GetAllKeys(typeof(string)).ToList().ToTask();
        await Assert.That(typedKeys.Count).IsEqualTo(2);

        var ca = await cache.GetCreatedAt(["nullId", "nullValue", "good"]).ToList().ToTask();
        await Assert.That(ca.Count).IsEqualTo(2);
        var caTyped = await cache.GetCreatedAt(["nullId", "nullValue", "good"], typeof(string)).ToList().ToTask();
        await Assert.That(caTyped.Count).IsEqualTo(2);
    }

    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer) =>
        new EncryptedSqliteBlobCache(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);
}
