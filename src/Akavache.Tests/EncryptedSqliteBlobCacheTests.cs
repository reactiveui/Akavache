// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using Akavache.EncryptedSqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;
using Splat;

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
        var connection = new InMemoryAkavacheConnection();
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("k", [1, 2, 3]).ToTask();
            var data = await cache.Get("k").ToTask();
            await Assert.That(data).IsEquivalentTo(new byte[] { 1, 2, 3 });
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies typed insert/get/getAll/invalidate/invalidateAll/keys flow on the encrypted
    /// in-memory-backed cache, exercising the type-aware code paths in the encrypted assembly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionTypedFlowShouldExerciseAllTypeMethods()
    {
        var connection = new InMemoryAkavacheConnection();
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
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
        finally
        {
            await cache.DisposeAsync();
        }
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("a", [1]).ToTask();
            await cache.Insert("b", [2]).ToTask();
            await cache.Insert([new KeyValuePair<string, byte[]>("c", [3])]).ToTask();

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
        finally
        {
            await cache.DisposeAsync();
        }
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Insert("k", [1]).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])]).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Insert("k", [1], typeof(string)).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])], typeof(string)).ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.Get("k").ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Get(["k"]).ToList().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Get("k", typeof(string)).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Get(["k"], typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.GetAllKeys().ToList().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.GetAllKeys(typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.GetAll(typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.GetCreatedAt("k").ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.GetCreatedAt(["k"]).ToList().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.GetCreatedAt("k", typeof(string)).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.GetCreatedAt(["k"], typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.Flush().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Flush(typeof(string)).ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.Invalidate("k").ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Invalidate(["k"]).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Invalidate("k", typeof(string)).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Invalidate(["k"], typeof(string)).ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.InvalidateAll().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.InvalidateAll(typeof(string)).ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.Vacuum().ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.UpdateExpiration("k", DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.UpdateExpiration(["k"], DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.BeforeWriteToDiskFilter([1, 2, 3], ImmediateScheduler.Instance).ToTask()).Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies the encrypted cache surfaces <see cref="ArgumentNullException"/> through the
    /// null-arg validation paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionNullArgsShouldThrow()
    {
        var connection = new InMemoryAkavacheConnection();
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await Assert.That(async () => await cache.Get((IEnumerable<string>)null!).ToList().ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.Get((string)null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.Get("k", (Type)null!).ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.Get((IEnumerable<string>)null!, typeof(string)).ToList().ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.Get(["k"], (Type)null!).ToList().ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.GetAll(null!).ToList().ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.GetAllKeys((Type)null!).ToList().ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.GetCreatedAt((string)null!).ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.GetCreatedAt((IEnumerable<string>)null!).ToList().ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.Insert((IEnumerable<KeyValuePair<string, byte[]>>)null!).ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.Insert("k", (byte[])null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.Insert("k", [1], (Type)null!).ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.Invalidate((IEnumerable<string>)null!).ToTask()).Throws<ArgumentNullException>();
            await Assert.That(async () => await cache.Invalidate("k", (Type)null!).ToTask()).Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the legacy V10 fallback path is exercised on the encrypted compilation
    /// when the cache misses the V11 store and the connection's legacy store has a value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionGetShouldFallBackToLegacyV10Store()
    {
        var connection = new InMemoryAkavacheConnection();
        connection.LegacyV10Store["legacyKey"] = [9, 8, 7];

        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var data = await cache.Get("legacyKey").ToTask();
            await Assert.That(data).IsEquivalentTo(new byte[] { 9, 8, 7 });

            var typedData = await cache.Get("legacyKey", typeof(string)).ToTask();
            await Assert.That(typedData).IsEquivalentTo(new byte[] { 9, 8, 7 });
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that on the encrypted compilation, missing keys throw
    /// <see cref="KeyNotFoundException"/> after exhausting the legacy V10 fallback path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionGetMissingShouldThrowKeyNotFound()
    {
        var connection = new InMemoryAkavacheConnection();
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await Assert.That(async () => await cache.Get("missing").ToTask()).Throws<KeyNotFoundException>();
            await Assert.That(async () => await cache.Get("missing", typeof(string)).ToTask()).Throws<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the encrypted compilation tolerates a checkpoint failure during Flush —
    /// exercises the catch branch around <c>CheckpointAsync</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedInMemoryConnectionFlushSwallowsCheckpointFailure()
    {
        var connection = new InMemoryAkavacheConnection { FailCheckpoint = true };
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Flush().ToTask();
            await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            connection.FailCheckpoint = false;
            await cache.DisposeAsync();
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
        var connection = new InMemoryAkavacheConnection { FailCheckpoint = true };
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        var connection = new InMemoryAkavacheConnection();
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        cache.Dispose();

        // Asserts run inline because the test method must remain synchronous to exercise the sync Dispose path.
        if (connection.LastCheckpointMode != CheckpointMode.Truncate)
        {
            throw new InvalidOperationException("Expected Truncate checkpoint mode after sync Dispose.");
        }

        if (connection.ReleaseAuxiliaryResourcesCount < 1)
        {
            throw new InvalidOperationException("Expected ReleaseAuxiliaryResources to be called at least once.");
        }
    }

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null connection — covers the
    /// null-guard branch on the encrypted compilation's third constructor overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullConnectionShouldThrow() =>
        await Assert.That(() => new EncryptedSqliteBlobCache((IAkavacheConnection)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null connection-string — covers
    /// the null-guard branch on the encrypted compilation's connection-string constructor.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullConnectionStringShouldThrow() =>
        await Assert.That(() => new EncryptedSqliteBlobCache((SQLite.SQLiteConnectionString)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null filename — covers the
    /// file-name null-guard on the file-name + password constructor overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullFileNameShouldThrow() =>
        await Assert.That(() => new EncryptedSqliteBlobCache((string)null!, GetTestPassword(), new SystemJsonSerializer())).Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies the encrypted constructor throws when given a null password — covers the
    /// password null-guard on the file-name + password constructor overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedConstructorWithNullPasswordShouldThrow() =>
        await Assert.That(() => new EncryptedSqliteBlobCache("test.db", (string)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies <see cref="EncryptedSqliteBlobCache.BeforeWriteToDiskFilter"/> returns the
    /// supplied data unchanged when the cache is active — exercises the success path of the
    /// filter on the encrypted compilation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedBeforeWriteToDiskFilterShouldReturnDataWhenNotDisposed()
    {
        var connection = new InMemoryAkavacheConnection();
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var input = new byte[] { 10, 20, 30 };
            var result = await cache.BeforeWriteToDiskFilter(input, ImmediateScheduler.Instance).ToTask();
            await Assert.That(result).IsEquivalentTo(input);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the encrypted compilation's typed Insert outer <c>!tx.IsValid</c> guard
    /// returns early on the first iteration when the transaction is immediately invalid.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedTypedInsertOuterInvalidGuardShouldReturnEarly()
    {
        var connection = new InMemoryAkavacheConnection
        {
            TransactionIsValidTrueCallsRemaining = 0,
        };
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("k", [1], typeof(string)).ToTask();
            await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        }
        finally
        {
            connection.TransactionIsValidTrueCallsRemaining = int.MaxValue;
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the encrypted compilation's typed Insert mid-loop <c>!tx.IsValid</c> guard
    /// returns early once the transaction reports invalid between iterations.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedTypedInsertInnerInvalidGuardShouldReturnEarly()
    {
        var connection = new InMemoryAkavacheConnection
        {
            TransactionIsValidTrueCallsRemaining = 1,
        };
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert(
                [new KeyValuePair<string, byte[]>("a", [1]), new KeyValuePair<string, byte[]>("b", [2])],
                typeof(string)).ToTask();

            await Assert.That(connection.Store.ContainsKey("a")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("b")).IsFalse();
        }
        finally
        {
            connection.TransactionIsValidTrueCallsRemaining = int.MaxValue;
            await cache.DisposeAsync();
        }
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
        var connection = new InMemoryAkavacheConnection
        {
            FailInsertOrReplaceInTransaction = true,
            FailCheckpoint = true,
        };
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("k", [1], typeof(string)).ToTask();
            await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        }
        finally
        {
            connection.FailCheckpoint = false;
            connection.FailInsertOrReplaceInTransaction = false;
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the encrypted compilation's typed Insert swallows a
    /// <c>RunInTransactionAsync</c> failure at the outer try/catch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EncryptedTypedInsertSwallowsOuterTransactionFailure()
    {
        var connection = new InMemoryAkavacheConnection { FailRunInTransaction = true };
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("k", [1], typeof(string)).ToTask();
            await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        }
        finally
        {
            connection.FailRunInTransaction = false;
            await cache.DisposeAsync();
        }
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
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
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the encrypted compilation's synchronous <c>Dispose</c> path tolerates every
    /// teardown call throwing.
    /// </summary>
    [Test]
    public void EncryptedSyncDisposeTolerantOfAllFailures()
    {
        var connection = new InMemoryAkavacheConnection
        {
            FailCheckpoint = true,
            FailReleaseAuxiliaryResources = true,
            FailClose = true,
        };
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        var connection = new InMemoryAkavacheConnection
        {
            FailCheckpoint = true,
            FailCompact = true,
            FailReleaseAuxiliaryResources = true,
            FailClose = true,
        };
        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        var connection = new InMemoryAkavacheConnection { BypassPredicate = true };
        connection.SeedRaw("nullId", new CacheEntry { Id = null, Value = [1], CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName });
        connection.SeedRaw("nullValue", new CacheEntry { Id = "nullValue", Value = null, CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName });
        connection.SeedRaw("good", new CacheEntry { Id = "good", Value = [9], CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName });

        var cache = new EncryptedSqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
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
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer)
    {
        // Create separate database files for each serializer to ensure compatibility.
        var serializerName = AppLocator.Current.GetService<ISerializer>()?.GetType().Name ?? "Unknown";

        // Further separate JSON and BSON formats to prevent cross-contamination.
        var formatType = serializerName.Contains("Bson") ? "bson" : "json";
        var fileName = $"encrypted-test-{serializerName}-{formatType}.db";

        return new EncryptedSqliteBlobCache(Path.Combine(path, fileName), GetTestPassword(), serializer);
    }

    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCacheForPath(string path, ISerializer serializer)
    {
        // For round-trip tests, use a consistent database file name to ensure
        // both cache instances (write and read) use the same database file.
        var fileName = "encrypted-roundtrip-test.db";
        return new EncryptedSqliteBlobCache(Path.Combine(path, fileName), GetTestPassword(), serializer);
    }

    private static string GetTestPassword() => "test123";
}
