// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Tests.Executors;
using Akavache.Tests.Mocks;
using Splat;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="InMemoryBlobCacheBase"/> covering disposed-state error paths,
/// typed instance overloads, type-aware operations and expiration edge cases.
/// </summary>
/// <remarks>
/// Marked <see cref="NotInParallelAttribute"/> because several tests in this class touch
/// global <see cref="Splat.AppLocator"/> state via <see cref="AkavacheTestExecutor"/>; running
/// them in parallel with each other (or with other AppLocator-touching tests) would race.
/// </remarks>
[Category("Akavache")]
[NotInParallel("CacheDatabaseState")]
public class InMemoryBlobCacheBaseTests
{
    /// <summary>
    /// Tests Insert(KeyValuePairs) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertKeyValuePairsShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])]).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Insert(KeyValuePairs) throws on null input.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertKeyValuePairsShouldThrowOnNull()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => cache.Insert((IEnumerable<KeyValuePair<string, byte[]>>)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Insert(string, bytes) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertSingleShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Insert("key", [1, 2, 3]).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Get(string) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Get("key").ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Get(keys) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetMultipleShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Get(["k1", "k2"]).ToList().ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetAllKeys throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.GetAllKeys().ToList().ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetCreatedAt throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.GetCreatedAt("key").ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Invalidate(key) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Invalidate("key").ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Invalidate(keys) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateMultipleShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Invalidate(["k1", "k2"]).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests InvalidateAll throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.InvalidateAll().ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Vacuum throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Vacuum().ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, expiration) errors on null/whitespace key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldErrorOnEmptyKey()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(async () => await cache.UpdateExpiration(string.Empty, DateTimeOffset.Now).ToTask())
                .Throws<ArgumentException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(key, expiration) errors on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldErrorOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.UpdateExpiration("key", DateTimeOffset.Now).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type, expiration) errors on null/whitespace key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationTypeShouldErrorOnEmptyKey()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(async () => await cache.UpdateExpiration(string.Empty, typeof(string), DateTimeOffset.Now).ToTask())
                .Throws<ArgumentException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type, expiration) errors on null type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationTypeShouldErrorOnNullType()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(async () => await cache.UpdateExpiration("key", null!, DateTimeOffset.Now).ToTask())
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type, expiration) errors on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationTypeShouldErrorOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.UpdateExpiration("key", typeof(string), DateTimeOffset.Now).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, expiration) errors on null keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldErrorOnNullKeys()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(async () => await cache.UpdateExpiration((IEnumerable<string>)null!, DateTimeOffset.Now).ToTask())
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, expiration) errors on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldErrorOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.UpdateExpiration(["k1"], DateTimeOffset.Now).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type, expiration) errors on null keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysTypeShouldErrorOnNullKeys()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(async () => await cache.UpdateExpiration((IEnumerable<string>)null!, typeof(string), DateTimeOffset.Now).ToTask())
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type, expiration) errors on null type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysTypeShouldErrorOnNullType()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(async () => await cache.UpdateExpiration(["k1"], null!, DateTimeOffset.Now).ToTask())
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type, expiration) errors on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysTypeShouldErrorOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.UpdateExpiration(["k1"], typeof(string), DateTimeOffset.Now).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration successfully updates expiration on existing entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldUpdateExistingEntry()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("key1", [1, 2, 3]).ToTask();
            await cache.UpdateExpiration("key1", DateTimeOffset.Now.AddHours(1)).ToTask();

            var data = await cache.Get("key1").ToTask();
            await Assert.That(data).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration on multiple keys updates all matching entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldUpdateMultiple()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k1", [1]).ToTask();
            await cache.Insert("k2", [2]).ToTask();

            await cache.UpdateExpiration(["k1", "k2"], DateTimeOffset.Now.AddHours(1)).ToTask();

            var d1 = await cache.Get("k1").ToTask();
            var d2 = await cache.Get("k2").ToTask();
            await Assert.That(d1).IsNotNull();
            await Assert.That(d2).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Vacuum removes expired entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldRemoveExpiredEntries()
    {
        var cache = CreateCache();
        try
        {
            // Insert with already-expired timestamp
            await cache.Insert("expired", [1], DateTimeOffset.Now.AddSeconds(-10)).ToTask();
            await cache.Insert("valid", [2], DateTimeOffset.Now.AddHours(1)).ToTask();

            await cache.Vacuum().ToTask();

            var keys = await cache.GetAllKeys().ToList().ToTask();
            await Assert.That(keys).Contains("valid");
            await Assert.That(keys).DoesNotContain("expired");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests ForcedDateTimeKind setter propagates to the serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindSetterShouldPropagate()
    {
        var cache = CreateCache();
        try
        {
            cache.ForcedDateTimeKind = DateTimeKind.Utc;
            await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);

            cache.ForcedDateTimeKind = null;
            await Assert.That(cache.ForcedDateTimeKind).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests InsertObject and GetObject round-trip using the instance overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectAndGetObjectShouldRoundTrip()
    {
        var cache = CreateCache();
        try
        {
            var user = new UserObject { Name = "Alice", Bio = "Dev", Blog = "https://example.com" };
            await cache.InsertObject("user-1", user).ToTask();

            var result = await cache.GetObject<UserObject>("user-1").ToTask();
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("Alice");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests InsertObject with an absolute expiration stores and retrieves the value before expiration.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectWithExpirationShouldStoreValue()
    {
        var cache = CreateCache();
        try
        {
            await cache.InsertObject("k", new UserObject { Name = "Bob" }, DateTimeOffset.Now.AddHours(1)).ToTask();
            var result = await cache.GetObject<UserObject>("k").ToTask();
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("Bob");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests InsertObject throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.InsertObject("k", new UserObject()).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetObject throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.GetObject<UserObject>("k").ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetAllObjects returns all stored instances of the requested type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllObjectsShouldReturnAllItemsForType()
    {
        var cache = CreateCache();
        try
        {
            await cache.InsertObject("a", new UserObject { Name = "A" }).ToTask();
            await cache.InsertObject("b", new UserObject { Name = "B" }).ToTask();
            await cache.InsertObject("c", new UserObject { Name = "C" }).ToTask();

            var all = await cache.GetAllObjects<UserObject>().ToTask();
            var list = all.ToList();
            await Assert.That(list.Count).IsEqualTo(3);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetAllObjects throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllObjectsShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.GetAllObjects<UserObject>().ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetObjectCreatedAt returns the created timestamp for a typed entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldReturnTimestamp()
    {
        var cache = CreateCache();
        try
        {
            await cache.InsertObject("k", new UserObject { Name = "x" }).ToTask();
            var createdAt = await cache.GetObjectCreatedAt<UserObject>("k").ToTask();
            await Assert.That(createdAt).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests InvalidateObject removes a typed entry so subsequent GetObject fails.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectShouldRemoveEntry()
    {
        var cache = CreateCache();
        try
        {
            await cache.InsertObject("k", new UserObject { Name = "x" }).ToTask();
            await cache.InvalidateObject<UserObject>("k").ToTask();

            await Assert.That(async () => await cache.GetObject<UserObject>("k").ToTask())
                .Throws<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests InvalidateAllObjects removes all entries for a type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllObjectsShouldRemoveTypedEntries()
    {
        var cache = CreateCache();
        try
        {
            await cache.InsertObject("a", new UserObject { Name = "A" }).ToTask();
            await cache.InsertObject("b", new UserObject { Name = "B" }).ToTask();

            await cache.InvalidateAllObjects<UserObject>().ToTask();

            var all = await cache.GetAllObjects<UserObject>().ToTask();
            await Assert.That(all.Count()).IsEqualTo(0);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests InvalidateAll(type) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllTypeShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.InvalidateAll(typeof(UserObject)).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Insert(keyValuePairs, type) stores entries in the type index.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertKeyValuePairsWithTypeShouldPopulateTypeIndex()
    {
        var cache = CreateCache();
        try
        {
            var pairs = new[]
            {
                new KeyValuePair<string, byte[]>("k1", [1, 2, 3]),
                new KeyValuePair<string, byte[]>("k2", [4, 5, 6]),
            };
            await cache.Insert(pairs, typeof(UserObject)).ToTask();

            var keys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
            await Assert.That(keys).Contains("k1");
            await Assert.That(keys).Contains("k2");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Insert(keyValuePairs, type) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertKeyValuePairsWithTypeShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])], typeof(UserObject)).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Insert(key, data, type) stores the entry with the type index.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertWithTypeShouldPopulateTypeIndex()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k", [1, 2, 3], typeof(UserObject)).ToTask();
            var keys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
            await Assert.That(keys).Contains("k");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Insert(key, data, type) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertWithTypeShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Insert("k", [1], typeof(UserObject)).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Get(key, type) delegates to Get(key) and returns the stored value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetWithTypeShouldReturnValue()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k", [7, 8, 9], typeof(UserObject)).ToTask();
            var data = await cache.Get("k", typeof(UserObject)).ToTask();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!.Length).IsEqualTo(3);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Get(keys, type) returns the stored values for existing keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetMultipleWithTypeShouldReturnValues()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k1", [1], typeof(UserObject)).ToTask();
            await cache.Insert("k2", [2], typeof(UserObject)).ToTask();

            var results = await cache.Get(["k1", "k2", "missing"], typeof(UserObject)).ToList().ToTask();
            await Assert.That(results.Count).IsEqualTo(2);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetAll(type) returns stored entries and removes expired ones.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllByTypeShouldReturnValidAndRemoveExpired()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("valid", [1], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();
            await cache.Insert("expired", [2], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-10)).ToTask();

            var all = await cache.GetAll(typeof(UserObject)).ToList().ToTask();
            await Assert.That(all.Count).IsEqualTo(1);
            await Assert.That(all[0].Key).IsEqualTo("valid");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetAll(type) returns an empty sequence when no entries exist for the type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllByTypeShouldBeEmptyWhenTypeMissing()
    {
        var cache = CreateCache();
        try
        {
            var all = await cache.GetAll(typeof(UserObject)).ToList().ToTask();
            await Assert.That(all.Count).IsEqualTo(0);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetAll(type) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllByTypeShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.GetAll(typeof(UserObject)).ToList().ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetAllKeys(type) returns valid keys and removes expired ones.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysByTypeShouldReturnValidAndRemoveExpired()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("valid", [1], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();
            await cache.Insert("expired", [2], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-5)).ToTask();

            var keys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
            await Assert.That(keys).Contains("valid");
            await Assert.That(keys).DoesNotContain("expired");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetAllKeys(type) returns empty when no type entries exist.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysByTypeShouldBeEmptyWhenTypeMissing()
    {
        var cache = CreateCache();
        try
        {
            var keys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
            await Assert.That(keys.Count).IsEqualTo(0);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetAllKeys(type) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysByTypeShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetCreatedAt(keys) returns timestamps for known keys and null for missing ones.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtKeysShouldReturnTimestampsAndNulls()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k1", [1]).ToTask();

            var results = await cache.GetCreatedAt(["k1", "missing"]).ToList().ToTask();
            await Assert.That(results.Count).IsEqualTo(2);

            var k1 = results.First(r => r.Key == "k1");
            var missing = results.First(r => r.Key == "missing");
            await Assert.That(k1.Time).IsNotNull();
            await Assert.That(missing.Time).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetCreatedAt(keys) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtKeysShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.GetCreatedAt(["k"]).ToList().ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetCreatedAt(keys, type) returns the same timestamps as the non-type overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtKeysWithTypeShouldReturnTimestamps()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k1", [1], typeof(UserObject)).ToTask();
            var results = await cache.GetCreatedAt(["k1"], typeof(UserObject)).ToList().ToTask();
            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].Time).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetCreatedAt(key, type) returns the created timestamp.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtWithTypeShouldReturnTimestamp()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k1", [1], typeof(UserObject)).ToTask();
            var created = await cache.GetCreatedAt("k1", typeof(UserObject)).ToTask();
            await Assert.That(created).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Flush() completes successfully.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FlushShouldComplete()
    {
        var cache = CreateCache();
        try
        {
            var result = await cache.Flush().ToTask();
            await Assert.That(result).IsEqualTo(Unit.Default);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Flush(type) completes successfully.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FlushTypeShouldComplete()
    {
        var cache = CreateCache();
        try
        {
            var result = await cache.Flush(typeof(UserObject)).ToTask();
            await Assert.That(result).IsEqualTo(Unit.Default);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Invalidate(key, type) removes the entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateWithTypeShouldRemoveEntry()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k", [1], typeof(UserObject)).ToTask();
            await cache.Invalidate("k", typeof(UserObject)).ToTask();

            await Assert.That(async () => await cache.Get("k").ToTask())
                .Throws<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Invalidate(keys, type) removes the specified entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateMultipleWithTypeShouldRemoveEntries()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k1", [1], typeof(UserObject)).ToTask();
            await cache.Insert("k2", [2], typeof(UserObject)).ToTask();

            await cache.Invalidate(["k1", "k2"], typeof(UserObject)).ToTask();

            var remaining = await cache.GetAllKeys().ToList().ToTask();
            await Assert.That(remaining.Count).IsEqualTo(0);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Get(key) returns KeyNotFoundException and cleans up when the entry has expired.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetShouldRemoveAndThrowWhenEntryExpired()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("expired", [1], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-5)).ToTask();

            await Assert.That(async () => await cache.Get("expired").ToTask())
                .Throws<KeyNotFoundException>();

            // After the failed Get, the key should be removed from the type index as well.
            var keys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
            await Assert.That(keys).DoesNotContain("expired");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Get(key) throws KeyNotFoundException for a missing key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetShouldThrowWhenKeyMissing()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(async () => await cache.Get("missing").ToTask())
                .Throws<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type) updates entries matching the type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationWithTypeShouldUpdateMatching()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k", [1], typeof(UserObject)).ToTask();
            await cache.UpdateExpiration("k", typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();

            var value = await cache.Get("k").ToTask();
            await Assert.That(value).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type) is a no-op when the stored entry has a different type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationWithTypeShouldIgnoreMismatchedType()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k", [1], typeof(UserObject)).ToTask();

            // Should not throw; simply no update performed since type mismatches.
            await cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now.AddHours(1)).ToTask();

            var value = await cache.Get("k").ToTask();
            await Assert.That(value).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type) updates entries with matching type only.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithTypeShouldUpdateMatching()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k1", [1], typeof(UserObject)).ToTask();
            await cache.Insert("k2", [2], typeof(UserObject)).ToTask();

            await cache.UpdateExpiration(["k1", "k2"], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();

            var v1 = await cache.Get("k1").ToTask();
            var v2 = await cache.Get("k2").ToTask();
            await Assert.That(v1).IsNotNull();
            await Assert.That(v2).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetAllKeys cleans up expired entries during enumeration.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysShouldRemoveExpired()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("valid", [1], DateTimeOffset.Now.AddHours(1)).ToTask();
            await cache.Insert("expired", [2], DateTimeOffset.Now.AddSeconds(-5)).ToTask();

            var keys = await cache.GetAllKeys().ToList().ToTask();
            await Assert.That(keys).Contains("valid");
            await Assert.That(keys).DoesNotContain("expired");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests InvalidateAll() clears every entry from the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllShouldClearAllEntries()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k1", [1]).ToTask();
            await cache.Insert("k2", [2], typeof(UserObject)).ToTask();

            await cache.InvalidateAll().ToTask();

            var keys = await cache.GetAllKeys().ToList().ToTask();
            await Assert.That(keys.Count).IsEqualTo(0);
            var typedKeys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
            await Assert.That(typedKeys.Count).IsEqualTo(0);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests the single-argument constructor uses the default task pool scheduler.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SingleArgConstructorShouldUseTaskpoolScheduler()
    {
        var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        try
        {
            await Assert.That(cache.Scheduler).IsNotNull();
            await Assert.That(cache.Scheduler).IsEqualTo(CacheDatabase.TaskpoolScheduler);
            await Assert.That(cache.Serializer).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests ForcedDateTimeKind setter propagates the value to the cache's own serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindSetterShouldUpdateAppLocatorSerializer()
    {
        var cache = CreateCache();
        try
        {
            var cacheSerializer = cache.Serializer;
            await Assert.That(cacheSerializer).IsNotNull();

            cache.ForcedDateTimeKind = DateTimeKind.Utc;

            await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
            await Assert.That(cacheSerializer.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);

            cache.ForcedDateTimeKind = null;
            await Assert.That(cacheSerializer.ForcedDateTimeKind).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the <see cref="InMemoryBlobCacheBase.ForcedDateTimeKind"/> setter propagates
    /// the new value to a serializer registered with <see cref="Splat.AppLocator"/>. Closes
    /// the true branch of the AppLocator-null check inside the setter.
    /// The <see cref="AkavacheTestExecutor"/> resets AppLocator state around the test so the
    /// global registration doesn't leak between tests.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task ForcedDateTimeKindSetterShouldPropagateToRegisteredAppLocatorSerializer()
    {
        // Register a sentinel serializer so the setter routes its propagation through it.
        // Use the explicit (object, Type) overload to avoid any generic-inference ambiguity
        // that would register the recorder under its concrete type instead of ISerializer.
        var registered = new RecordingForcedKindSerializer();
        AppLocator.CurrentMutable.RegisterConstant(registered, typeof(ISerializer));

        // Sanity check: confirm the registration is what AppLocator returns. If a parallel
        // test has polluted state despite the executor reset, we want to fail early with a
        // clear message rather than masking the failure as a setter-propagation issue.
        var resolved = AppLocator.Current.GetService<ISerializer>();
        await Assert.That(resolved).IsSameReferenceAs(registered);

        var cache = CreateCache();
        try
        {
            cache.ForcedDateTimeKind = DateTimeKind.Utc;

            await Assert.That(registered.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
            await Assert.That(registered.LastSetKind).IsEqualTo(DateTimeKind.Utc);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the <see cref="InMemoryBlobCacheBase.ForcedDateTimeKind"/> setter tolerates
    /// the case where no <see cref="ISerializer"/> is registered with <see cref="Splat.AppLocator"/>.
    /// Closes the false branch of the AppLocator-null check inside the setter.
    /// The <see cref="AkavacheTestExecutor"/> resets AppLocator state around the test, so the
    /// pre-test reset guarantees no serializer is registered when the test body runs.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task ForcedDateTimeKindSetterShouldTolerateMissingAppLocatorSerializer()
    {
        var cache = CreateCache();
        try
        {
            // Setter must not throw even when AppLocator has no registered ISerializer.
            cache.ForcedDateTimeKind = DateTimeKind.Utc;
            await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryBlobCacheBase.Vacuum"/> handles the case where the
    /// type index is empty but there are expired untyped entries. Closes the false branch
    /// of the inner <c>foreach (var kvp in _typeIndex)</c> iterator that only iterates when
    /// the index has entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldHandleExpiredUntypedEntriesWithEmptyTypeIndex()
    {
        var cache = CreateCache();
        try
        {
            // Insert untyped entries only — the type index never gets populated.
            await cache.Insert("expired", [1, 2, 3], DateTimeOffset.Now.AddSeconds(-5)).ToTask();
            await cache.Insert("valid", [4, 5, 6], DateTimeOffset.Now.AddHours(1)).ToTask();

            await cache.Vacuum().ToTask();

            var keys = await cache.GetAllKeys().ToList().ToTask();
            await Assert.That(keys).DoesNotContain("expired");
            await Assert.That(keys).Contains("valid");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Vacuum removes expired typed entries from the type index.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldRemoveExpiredEntryFromTypeIndex()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("expiredTyped", [1, 2, 3], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-5)).ToTask();
            await cache.Insert("validTyped", [4, 5, 6], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();

            var typedKeysBefore = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
            await Assert.That(typedKeysBefore).Contains("validTyped");

            await cache.Vacuum().ToTask();

            var typedKeysAfter = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
            await Assert.That(typedKeysAfter).DoesNotContain("expiredTyped");
            await Assert.That(typedKeysAfter).Contains("validTyped");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that constructing InMemoryBlobCache with a null scheduler throws ArgumentNullException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowOnNullScheduler()
    {
        await Assert.That(static () => new InMemoryBlobCache(null!, new SystemJsonSerializer()))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that constructing InMemoryBlobCache with a null ISerializer throws ArgumentNullException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowOnNullSerializer()
    {
        await Assert.That(static () => new InMemoryBlobCache(ImmediateScheduler.Instance, null))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the single-arg ISerializer constructor throws on null serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SingleArgSerializerConstructorShouldThrowOnNull()
    {
        await Assert.That(static () => new InMemoryBlobCache((ISerializer)null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the string constructor throws when the serializer type cannot be resolved.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task StringConstructorShouldThrowWhenSerializerNotRegistered()
    {
        await Assert.That(static () => new InMemoryBlobCache("NonExistentSerializerContract"))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests GetObject returns default(T) when the stored byte array is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectShouldReturnDefaultWhenStoredDataIsNull()
    {
        var cache = CreateCache();
        try
        {
            // Insert a null byte array directly via the raw Insert method.
            await cache.Insert("nulldata", null!, typeof(UserObject)).ToTask();

            var result = await cache.GetObject<UserObject>("nulldata").ToTask();
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type) is a no-op when the key is not found in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationWithTypeShouldNoopWhenKeyMissing()
    {
        var cache = CreateCache();
        try
        {
            // Should complete without error even though the key does not exist.
            await cache.UpdateExpiration("missing", typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type) is a no-op when the stored entry has a different type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithTypeShouldIgnoreMismatchedType()
    {
        var cache = CreateCache();
        try
        {
            await cache.Insert("k", [1], typeof(UserObject)).ToTask();

            // Update with a different type should be a no-op.
            await cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now.AddHours(1)).ToTask();

            var value = await cache.Get("k").ToTask();
            await Assert.That(value).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(key) is a no-op when the key is not found in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldNoopWhenKeyMissing()
    {
        var cache = CreateCache();
        try
        {
            // Should complete without error even though the key does not exist.
            await cache.UpdateExpiration("missing", DateTimeOffset.Now.AddHours(1)).ToTask();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(keys) is a no-op for keys not in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldNoopWhenKeysMissing()
    {
        var cache = CreateCache();
        try
        {
            await cache.UpdateExpiration(["missing1", "missing2"], DateTimeOffset.Now.AddHours(1)).ToTask();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type) is a no-op for keys not in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithTypeShouldNoopWhenKeysMissing()
    {
        var cache = CreateCache();
        try
        {
            await cache.UpdateExpiration(["missing"], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests HttpService lazy initialization returns a default instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HttpServiceShouldReturnDefaultInstance()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(cache.HttpService).IsNotNull().And.IsTypeOf<HttpService>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests HttpService setter overrides the lazy default.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HttpServiceSetterShouldOverrideDefault()
    {
        var cache = CreateCache();
        try
        {
            // Touch the lazy getter first so the setter is clearly overriding a real instance.
            _ = cache.HttpService;
            var custom = new HttpService();
            cache.HttpService = custom;
            await Assert.That(cache.HttpService).IsSameReferenceAs(custom);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.CollectExpiredKeys"/> returns the keys
    /// of every entry whose <c>ExpiresAt</c> is at or before the supplied <c>now</c> cutoff.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CollectExpiredKeysShouldReturnEntriesAtOrBeforeNow()
    {
        var now = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var cache = new Dictionary<string, CacheEntry>
        {
            ["expired"] = new() { Id = "expired", ExpiresAt = now.AddMinutes(-5).UtcDateTime },
            ["expiringNow"] = new() { Id = "expiringNow", ExpiresAt = now.UtcDateTime },
            ["future"] = new() { Id = "future", ExpiresAt = now.AddHours(1).UtcDateTime },
        };

        var result = InMemoryBlobCacheBase.CollectExpiredKeys(cache, now);

        await Assert.That(result).Contains("expired");
        await Assert.That(result).Contains("expiringNow");
        await Assert.That(result).DoesNotContain("future");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.CollectExpiredKeys"/> returns an empty list
    /// when nothing has expired.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CollectExpiredKeysShouldReturnEmptyWhenNothingExpired()
    {
        var now = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var cache = new Dictionary<string, CacheEntry>
        {
            ["k1"] = new() { Id = "k1", ExpiresAt = now.AddHours(1).UtcDateTime },
            ["k2"] = new() { Id = "k2", ExpiresAt = null },
        };

        var result = InMemoryBlobCacheBase.CollectExpiredKeys(cache, now);

        await Assert.That(result).IsEmpty();
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.CollectExpiredKeys"/> returns an empty list
    /// when the cache itself is empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CollectExpiredKeysShouldReturnEmptyForEmptyCache()
    {
        var result = InMemoryBlobCacheBase.CollectExpiredKeys([], DateTimeOffset.UtcNow);

        await Assert.That(result).IsEmpty();
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.RemoveKeyFromAllTypeIndexes"/> removes the
    /// key from every type's set in the index, and tolerates the key not being present in some.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveKeyFromAllTypeIndexesShouldPruneEverySetThatContainsKey()
    {
        var typeIndex = new Dictionary<Type, HashSet<string>>
        {
            [typeof(string)] = ["k1", "k2"],
            [typeof(int)] = ["k1", "k3"],
            [typeof(double)] = ["k4"],
        };

        InMemoryBlobCacheBase.RemoveKeyFromAllTypeIndexes(typeIndex, "k1");

        await Assert.That(typeIndex[typeof(string)]).DoesNotContain("k1");
        await Assert.That(typeIndex[typeof(string)]).Contains("k2");
        await Assert.That(typeIndex[typeof(int)]).DoesNotContain("k1");
        await Assert.That(typeIndex[typeof(int)]).Contains("k3");
        await Assert.That(typeIndex[typeof(double)]).Contains("k4");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.RemoveKeyFromAllTypeIndexes"/> is a no-op
    /// when the type index is empty (closes the false branch of the inner foreach iterator).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveKeyFromAllTypeIndexesShouldNoOpForEmptyIndex()
    {
        var typeIndex = new Dictionary<Type, HashSet<string>>();

        // Should not throw.
        InMemoryBlobCacheBase.RemoveKeyFromAllTypeIndexes(typeIndex, "anyKey");

        await Assert.That(typeIndex).IsEmpty();
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.VacuumExpiredEntries"/> removes expired
    /// entries from the cache and prunes their keys out of every type-index entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumExpiredEntriesShouldRemoveExpiredAndPruneTypeIndex()
    {
        var now = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var cache = new Dictionary<string, CacheEntry>
        {
            ["expired"] = new() { Id = "expired", ExpiresAt = now.AddMinutes(-1).UtcDateTime, TypeName = "T1" },
            ["valid"] = new() { Id = "valid", ExpiresAt = now.AddHours(1).UtcDateTime, TypeName = "T1" },
        };
        var typeIndex = new Dictionary<Type, HashSet<string>>
        {
            [typeof(string)] = ["expired", "valid"],
        };

        InMemoryBlobCacheBase.VacuumExpiredEntries(cache, typeIndex, now);

        await Assert.That(cache).DoesNotContainKey("expired");
        await Assert.That(cache).ContainsKey("valid");
        await Assert.That(typeIndex[typeof(string)]).DoesNotContain("expired");
        await Assert.That(typeIndex[typeof(string)]).Contains("valid");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.VacuumExpiredEntries"/> is a no-op when
    /// nothing is expired (no cache mutation, no type-index pruning).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumExpiredEntriesShouldBeNoOpWhenNothingExpired()
    {
        var now = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var cache = new Dictionary<string, CacheEntry>
        {
            ["valid"] = new() { Id = "valid", ExpiresAt = now.AddHours(1).UtcDateTime },
        };
        var typeIndex = new Dictionary<Type, HashSet<string>>
        {
            [typeof(string)] = ["valid"],
        };

        InMemoryBlobCacheBase.VacuumExpiredEntries(cache, typeIndex, now);

        await Assert.That(cache).ContainsKey("valid");
        await Assert.That(typeIndex[typeof(string)]).Contains("valid");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.VacuumExpiredEntries"/> handles the case
    /// where there are expired entries but the type index is empty — the entries are still
    /// removed from the cache, and the empty inner foreach is exercised harmlessly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumExpiredEntriesShouldRemoveEntriesEvenWithEmptyTypeIndex()
    {
        var now = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var cache = new Dictionary<string, CacheEntry>
        {
            ["expired"] = new() { Id = "expired", ExpiresAt = now.AddMinutes(-1).UtcDateTime },
        };
        var typeIndex = new Dictionary<Type, HashSet<string>>();

        InMemoryBlobCacheBase.VacuumExpiredEntries(cache, typeIndex, now);

        await Assert.That(cache).DoesNotContainKey("expired");
    }

    /// <summary>
    /// Creates a new <see cref="InMemoryBlobCache"/> using the immediate scheduler and System.Text.Json serializer.
    /// </summary>
    /// <returns>A new in-memory blob cache instance.</returns>
    private static InMemoryBlobCache CreateCache() =>
        new(ImmediateScheduler.Instance, new SystemJsonSerializer());

    /// <summary>
    /// A minimal <see cref="ISerializer"/> stub used to verify the
    /// <see cref="InMemoryBlobCacheBase.ForcedDateTimeKind"/> setter propagates kind updates
    /// to a serializer registered with <see cref="Splat.AppLocator"/>. Records the most
    /// recent setter invocation so the test can assert the value reached this instance.
    /// </summary>
    private sealed class RecordingForcedKindSerializer : ISerializer
    {
        /// <summary>Backing field for <see cref="ForcedDateTimeKind"/>.</summary>
        private DateTimeKind? _kind;

        /// <summary>
        /// Gets the last value assigned to <see cref="ForcedDateTimeKind"/>.
        /// </summary>
        public DateTimeKind? LastSetKind { get; private set; }

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind
        {
            get => _kind;
            set
            {
                _kind = value;
                LastSetKind = value;
            }
        }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) => default;

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => [];
    }
}
