// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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
/// global <see cref="AppLocator"/> state via <see cref="AkavacheTestExecutor"/>; running
/// them in parallel with each other (or with other AppLocator-touching tests) would race.
/// </remarks>
[Category("Akavache")]
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
        cache.Dispose();

        var error = cache.Insert([new("k", [1])]).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Insert(KeyValuePairs) throws on null input.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertKeyValuePairsShouldThrowOnNull()
    {
        using var cache = CreateCache();
        await Assert.That(() => cache.Insert(null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests Insert(string, bytes) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertSingleShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.Insert("key", [1, 2, 3]).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Get(string) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.Get("key").SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Get(keys) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetMultipleShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.Get(["k1", "k2"]).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetAllKeys throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.GetAllKeys().ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetCreatedAt throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.GetCreatedAt("key").SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Invalidate(key) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.Invalidate("key").SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Invalidate(keys) throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateMultipleShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.Invalidate(["k1", "k2"]).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests InvalidateAll throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.InvalidateAll().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Vacuum throws on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.Vacuum().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, expiration) errors on null/whitespace key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldErrorOnEmptyKey()
    {
        using var cache = CreateCache();
        var error = cache.UpdateExpiration(string.Empty, DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, expiration) errors on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldErrorOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.UpdateExpiration("key", DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type, expiration) errors on null/whitespace key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationTypeShouldErrorOnEmptyKey()
    {
        using var cache = CreateCache();
        var error = cache.UpdateExpiration(string.Empty, typeof(string), DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type, expiration) errors on null type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationTypeShouldErrorOnNullType()
    {
        using var cache = CreateCache();
        var error = cache.UpdateExpiration("key", null!, DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type, expiration) errors on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationTypeShouldErrorOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.UpdateExpiration("key", typeof(string), DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, expiration) errors on null keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldErrorOnNullKeys()
    {
        using var cache = CreateCache();
        var error = cache.UpdateExpiration((IEnumerable<string>)null!, DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, expiration) errors on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldErrorOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.UpdateExpiration(["k1"], DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type, expiration) errors on null keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysTypeShouldErrorOnNullKeys()
    {
        using var cache = CreateCache();
        var error = cache.UpdateExpiration((IEnumerable<string>)null!, typeof(string), DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type, expiration) errors on null type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysTypeShouldErrorOnNullType()
    {
        using var cache = CreateCache();
        var error = cache.UpdateExpiration(["key"], null!, DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ArgumentNullException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type, expiration) errors on disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysTypeShouldErrorOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.UpdateExpiration(["k1"], typeof(string), DateTimeOffset.Now).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration successfully updates expiration on existing entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldUpdateExistingEntry()
    {
        using var cache = CreateCache();
        cache.Insert("key1", [1, 2, 3]).SubscribeAndComplete();
        cache.UpdateExpiration("key1", DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

        var data = cache.Get("key1").SubscribeGetValue();
        await Assert.That(data).IsNotNull();
    }

    /// <summary>
    /// Tests UpdateExpiration on multiple keys updates all matching entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldUpdateMultiple()
    {
        using var cache = CreateCache();
        cache.Insert("k1", [1]).SubscribeAndComplete();
        cache.Insert("k2", [2]).SubscribeAndComplete();

        cache.UpdateExpiration(["k1", "k2"], DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

        var d1 = cache.Get("k1").SubscribeGetValue();
        var d2 = cache.Get("k2").SubscribeGetValue();
        await Assert.That(d1).IsNotNull();
        await Assert.That(d2).IsNotNull();
    }

    /// <summary>
    /// Tests Vacuum removes expired entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldRemoveExpiredEntries()
    {
        using var cache = CreateCache();

        // Insert with already-expired timestamp
        cache.Insert("expired", [1], DateTimeOffset.Now.AddSeconds(-10)).SubscribeAndComplete();
        cache.Insert("valid", [2], DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

        cache.Vacuum().SubscribeAndComplete();

        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys).Contains("valid");
        await Assert.That(keys).DoesNotContain("expired");
    }

    /// <summary>
    /// Tests ForcedDateTimeKind setter propagates to the serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindSetterShouldPropagate()
    {
        using var cache = CreateCache();
        cache.ForcedDateTimeKind = DateTimeKind.Utc;
        await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);

        cache.ForcedDateTimeKind = null;
        await Assert.That(cache.ForcedDateTimeKind).IsNull();
    }

    /// <summary>
    /// Tests InsertObject and GetObject round-trip using the instance overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectAndGetObjectShouldRoundTrip()
    {
        using var cache = CreateCache();
        UserObject user = new() { Name = "Alice", Bio = "Dev", Blog = "https://example.com" };
        cache.InsertObject("user-1", user).SubscribeAndComplete();

        var result = cache.GetObject<UserObject>("user-1").SubscribeGetValue();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Alice");
    }

    /// <summary>
    /// Tests InsertObject with an absolute expiration stores and retrieves the value before expiration.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectWithExpirationShouldStoreValue()
    {
        using var cache = CreateCache();
        cache.InsertObject("k", new UserObject { Name = "Bob" }, DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();
        var result = cache.GetObject<UserObject>("k").SubscribeGetValue();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Bob");
    }

    /// <summary>
    /// Tests InsertObject throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.InsertObject("k", new UserObject()).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetObject throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.GetObject<UserObject>("k").SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetAllObjects returns all stored instances of the requested type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllObjectsShouldReturnAllItemsForType()
    {
        using var cache = CreateCache();
        cache.InsertObject("a", new UserObject { Name = "A" }).SubscribeAndComplete();
        cache.InsertObject("b", new UserObject { Name = "B" }).SubscribeAndComplete();
        cache.InsertObject("c", new UserObject { Name = "C" }).SubscribeAndComplete();

        var all = cache.GetAllObjects<UserObject>().SubscribeGetValue();
        var list = all!.ToList();
        await Assert.That(list.Count).IsEqualTo(3);
    }

    /// <summary>
    /// Tests GetAllObjects throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllObjectsShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.GetAllObjects<UserObject>().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetObjectCreatedAt returns the created timestamp for a typed entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldReturnTimestamp()
    {
        using var cache = CreateCache();
        cache.InsertObject("k", new UserObject { Name = "x" }).SubscribeAndComplete();
        var createdAt = cache.GetObjectCreatedAt<UserObject>("k").SubscribeGetValue();
        await Assert.That(createdAt).IsNotNull();
    }

    /// <summary>
    /// Tests InvalidateObject removes a typed entry so subsequent GetObject fails.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectShouldRemoveEntry()
    {
        using var cache = CreateCache();
        cache.InsertObject("k", new UserObject { Name = "x" }).SubscribeAndComplete();
        cache.InvalidateObject<UserObject>("k").SubscribeAndComplete();

        var error = cache.GetObject<UserObject>("k").SubscribeGetError();
        await Assert.That(error).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests InvalidateAllObjects removes all entries for a type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllObjectsShouldRemoveTypedEntries()
    {
        using var cache = CreateCache();
        cache.InsertObject("a", new UserObject { Name = "A" }).SubscribeAndComplete();
        cache.InsertObject("b", new UserObject { Name = "B" }).SubscribeAndComplete();

        cache.InvalidateAllObjects<UserObject>().SubscribeAndComplete();

        var all = cache.GetAllObjects<UserObject>().SubscribeGetValue();
        await Assert.That(all!.Count()).IsEqualTo(0);
    }

    /// <summary>
    /// Tests InvalidateAll(type) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllTypeShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.InvalidateAll(typeof(UserObject)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Insert(keyValuePairs, type) stores entries in the type index.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertKeyValuePairsWithTypeShouldPopulateTypeIndex()
    {
        using var cache = CreateCache();
        KeyValuePair<string, byte[]>[] pairs =
        [
            new("k1", [1, 2, 3]),
            new("k2", [4, 5, 6])
        ];
        cache.Insert(pairs, typeof(UserObject)).SubscribeAndComplete();

        var keys = cache.GetAllKeys(typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(keys).Contains("k1");
        await Assert.That(keys).Contains("k2");
    }

    /// <summary>
    /// Tests Insert(keyValuePairs, type) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertKeyValuePairsWithTypeShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.Insert([new("k", [1])], typeof(UserObject)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Insert(key, data, type) stores the entry with the type index.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertWithTypeShouldPopulateTypeIndex()
    {
        using var cache = CreateCache();
        cache.Insert("k", [1, 2, 3], typeof(UserObject)).SubscribeAndComplete();
        var keys = cache.GetAllKeys(typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(keys).Contains("k");
    }

    /// <summary>
    /// Tests Insert(key, data, type) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertWithTypeShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.Insert("k", [1], typeof(UserObject)).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Get(key, type) delegates to Get(key) and returns the stored value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetWithTypeShouldReturnValue()
    {
        using var cache = CreateCache();
        cache.Insert("k", [7, 8, 9], typeof(UserObject)).SubscribeAndComplete();
        var data = cache.Get("k", typeof(UserObject)).SubscribeGetValue();
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Length).IsEqualTo(3);
    }

    /// <summary>
    /// Tests Get(keys, type) returns the stored values for existing keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetMultipleWithTypeShouldReturnValues()
    {
        using var cache = CreateCache();
        cache.Insert("k1", [1], typeof(UserObject)).SubscribeAndComplete();
        cache.Insert("k2", [2], typeof(UserObject)).SubscribeAndComplete();

        var results = cache.Get(["k1", "k2", "missing"], typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(results!.Count).IsEqualTo(2);
    }

    /// <summary>
    /// Tests GetAll(type) returns stored entries and removes expired ones.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllByTypeShouldReturnValidAndRemoveExpired()
    {
        using var cache = CreateCache();
        cache.Insert("valid", [1], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();
        cache.Insert("expired", [2], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-10)).SubscribeAndComplete();

        var all = cache.GetAll(typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(all!.Count).IsEqualTo(1);
        await Assert.That(all[0].Key).IsEqualTo("valid");
    }

    /// <summary>
    /// Tests GetAll(type) returns an empty sequence when no entries exist for the type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllByTypeShouldBeEmptyWhenTypeMissing()
    {
        using var cache = CreateCache();
        var all = cache.GetAll(typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(all!.Count).IsEqualTo(0);
    }

    /// <summary>
    /// Tests GetAll(type) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllByTypeShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.GetAll(typeof(UserObject)).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetAllKeys(type) returns valid keys and removes expired ones.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysByTypeShouldReturnValidAndRemoveExpired()
    {
        using var cache = CreateCache();
        cache.Insert("valid", [1], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();
        cache.Insert("expired", [2], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-5)).SubscribeAndComplete();

        var keys = cache.GetAllKeys(typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(keys).Contains("valid");
        await Assert.That(keys).DoesNotContain("expired");
    }

    /// <summary>
    /// Tests GetAllKeys(type) returns empty when no type entries exist.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysByTypeShouldBeEmptyWhenTypeMissing()
    {
        using var cache = CreateCache();
        var keys = cache.GetAllKeys(typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(keys!.Count).IsEqualTo(0);
    }

    /// <summary>
    /// Tests GetAllKeys(type) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysByTypeShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.GetAllKeys(typeof(UserObject)).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetCreatedAt(keys) returns timestamps for known keys and null for missing ones.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtKeysShouldReturnTimestampsAndNulls()
    {
        using var cache = CreateCache();
        cache.Insert("k1", [1]).SubscribeAndComplete();

        var results = cache.GetCreatedAt(["k1", "missing"]).ToList().SubscribeGetValue();
        await Assert.That(results!.Count).IsEqualTo(2);

        var (_, time) = results.First(static r => r.Key == "k1");
        var (_, dateTimeOffset) = results.First(static r => r.Key == "missing");
        await Assert.That(time).IsNotNull();
        await Assert.That(dateTimeOffset).IsNull();
    }

    /// <summary>
    /// Tests GetCreatedAt(keys) throws on a disposed cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtKeysShouldThrowOnDisposed()
    {
        var cache = CreateCache();
        cache.Dispose();

        var error = cache.GetCreatedAt(["k"]).ToList().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetCreatedAt(keys, type) returns the same timestamps as the non-type overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtKeysWithTypeShouldReturnTimestamps()
    {
        using var cache = CreateCache();
        cache.Insert("k1", [1], typeof(UserObject)).SubscribeAndComplete();
        var results = cache.GetCreatedAt(["k1"], typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(results!.Count).IsEqualTo(1);
        await Assert.That(results[0].Time).IsNotNull();
    }

    /// <summary>
    /// Tests GetCreatedAt(key, type) returns the created timestamp.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtWithTypeShouldReturnTimestamp()
    {
        using var cache = CreateCache();
        cache.Insert("k1", [1], typeof(UserObject)).SubscribeAndComplete();
        var created = cache.GetCreatedAt("k1", typeof(UserObject)).SubscribeGetValue();
        await Assert.That(created).IsNotNull();
    }

    /// <summary>
    /// Tests Flush() completes successfully.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FlushShouldComplete()
    {
        using var cache = CreateCache();
        var result = cache.Flush().SubscribeGetValue();
        await Assert.That(result).IsEqualTo(Unit.Default);
    }

    /// <summary>
    /// Tests Flush(type) completes successfully.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FlushTypeShouldComplete()
    {
        using var cache = CreateCache();
        var result = cache.Flush(typeof(UserObject)).SubscribeGetValue();
        await Assert.That(result).IsEqualTo(Unit.Default);
    }

    /// <summary>
    /// Tests Invalidate(key, type) removes the entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateWithTypeShouldRemoveEntry()
    {
        using var cache = CreateCache();
        cache.Insert("k", [1], typeof(UserObject)).SubscribeAndComplete();
        cache.Invalidate("k", typeof(UserObject)).SubscribeAndComplete();

        var error = cache.Get("k").SubscribeGetError();
        await Assert.That(error).IsTypeOf<KeyNotFoundException>();
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
            cache.Insert("k1", [1], typeof(UserObject)).SubscribeAndComplete();
            cache.Insert("k2", [2], typeof(UserObject)).SubscribeAndComplete();

            cache.Invalidate(["k1", "k2"], typeof(UserObject)).SubscribeAndComplete();

            var remaining = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(remaining!.Count).IsEqualTo(0);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests Get(key) returns KeyNotFoundException and cleans up when the entry has expired.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetShouldRemoveAndThrowWhenEntryExpired()
    {
        using var cache = CreateCache();
        cache.Insert("expired", [1], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-5)).SubscribeAndComplete();

        var error = cache.Get("expired").SubscribeGetError();
        await Assert.That(error).IsTypeOf<KeyNotFoundException>();

        // After the failed Get, the key should be removed from the type index as well.
        var keys = cache.GetAllKeys(typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(keys).DoesNotContain("expired");
    }

    /// <summary>
    /// Tests Get(key) throws KeyNotFoundException for a missing key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetShouldThrowWhenKeyMissing()
    {
        using var cache = CreateCache();
        var error = cache.Get("missing").SubscribeGetError();
        await Assert.That(error).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type) updates entries matching the type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationWithTypeShouldUpdateMatching()
    {
        using var cache = CreateCache();
        cache.Insert("k", [1], typeof(UserObject)).SubscribeAndComplete();
        cache.UpdateExpiration("k", typeof(UserObject), DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

        var value = cache.Get("k").SubscribeGetValue();
        await Assert.That(value).IsNotNull();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type) is a no-op when the stored entry has a different type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationWithTypeShouldIgnoreMismatchedType()
    {
        using var cache = CreateCache();
        cache.Insert("k", [1], typeof(UserObject)).SubscribeAndComplete();

        // Should not throw; simply no update performed since type mismatches.
        cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

        var value = cache.Get("k").SubscribeGetValue();
        await Assert.That(value).IsNotNull();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type) updates entries with matching type only.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithTypeShouldUpdateMatching()
    {
        using var cache = CreateCache();
        cache.Insert("k1", [1], typeof(UserObject)).SubscribeAndComplete();
        cache.Insert("k2", [2], typeof(UserObject)).SubscribeAndComplete();

        cache.UpdateExpiration(["k1", "k2"], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

        var v1 = cache.Get("k1").SubscribeGetValue();
        var v2 = cache.Get("k2").SubscribeGetValue();
        await Assert.That(v1).IsNotNull();
        await Assert.That(v2).IsNotNull();
    }

    /// <summary>
    /// Tests GetAllKeys cleans up expired entries during enumeration.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysShouldRemoveExpired()
    {
        using var cache = CreateCache();
        cache.Insert("valid", [1], DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();
        cache.Insert("expired", [2], DateTimeOffset.Now.AddSeconds(-5)).SubscribeAndComplete();

        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys).Contains("valid");
        await Assert.That(keys).DoesNotContain("expired");
    }

    /// <summary>
    /// Tests InvalidateAll() clears every entry from the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllShouldClearAllEntries()
    {
        using var cache = CreateCache();
        cache.Insert("k1", [1]).SubscribeAndComplete();
        cache.Insert("k2", [2], typeof(UserObject)).SubscribeAndComplete();

        cache.InvalidateAll().SubscribeAndComplete();

        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys!.Count).IsEqualTo(0);
        var typedKeys = cache.GetAllKeys(typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(typedKeys!.Count).IsEqualTo(0);
    }

    /// <summary>
    /// Tests the single-argument constructor uses the default task pool scheduler.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SingleArgConstructorShouldUseTaskpoolScheduler()
    {
        using InMemoryBlobCache cache = new(new SystemJsonSerializer());
        await Assert.That(cache.Scheduler).IsNotNull();
        await Assert.That(cache.Scheduler).IsEqualTo(CacheDatabase.TaskpoolScheduler);
        await Assert.That(cache.Serializer).IsNotNull();
    }

    /// <summary>
    /// Tests ForcedDateTimeKind setter propagates the value to the cache's own serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindSetterShouldUpdateAppLocatorSerializer()
    {
        using var cache = CreateCache();
        var cacheSerializer = cache.Serializer;
        await Assert.That(cacheSerializer).IsNotNull();

        cache.ForcedDateTimeKind = DateTimeKind.Utc;

        await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(cacheSerializer.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);

        cache.ForcedDateTimeKind = null;
        await Assert.That(cacheSerializer.ForcedDateTimeKind).IsNull();
    }

    /// <summary>
    /// Verifies the <see cref="InMemoryBlobCacheBase.ForcedDateTimeKind"/> setter propagates
    /// the new value to a serializer registered with <see cref="AppLocator"/>. Closes
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
        RecordingForcedKindSerializer registered = new();
        AppLocator.CurrentMutable.RegisterConstant(registered, typeof(ISerializer));

        // Sanity check: confirm the registration is what AppLocator returns. If a parallel
        // test has polluted state despite the executor reset, we want to fail early with a
        // clear message rather than masking the failure as a setter-propagation issue.
        var resolved = AppLocator.Current.GetService<ISerializer>();
        await Assert.That(resolved).IsSameReferenceAs(registered);

        using var cache = CreateCache();
        cache.ForcedDateTimeKind = DateTimeKind.Utc;

        await Assert.That(registered.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(registered.LastSetKind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Verifies the <see cref="InMemoryBlobCacheBase.ForcedDateTimeKind"/> setter tolerates
    /// the case where no <see cref="ISerializer"/> is registered with <see cref="AppLocator"/>.
    /// Closes the false branch of the AppLocator-null check inside the setter.
    /// The <see cref="AkavacheTestExecutor"/> resets AppLocator state around the test, so the
    /// pre-test reset guarantees no serializer is registered when the test body runs.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task ForcedDateTimeKindSetterShouldTolerateMissingAppLocatorSerializer()
    {
        using var cache = CreateCache();

        // Setter must not throw even when AppLocator has no registered ISerializer.
        cache.ForcedDateTimeKind = DateTimeKind.Utc;
        await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
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
        using var cache = CreateCache();

        // Insert untyped entries only — the type index never gets populated.
        cache.Insert("expired", [1, 2, 3], DateTimeOffset.Now.AddSeconds(-5)).SubscribeAndComplete();
        cache.Insert("valid", [4, 5, 6], DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

        cache.Vacuum().SubscribeAndComplete();

        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys).DoesNotContain("expired");
        await Assert.That(keys).Contains("valid");
    }

    /// <summary>
    /// Tests Vacuum removes expired typed entries from the type index.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldRemoveExpiredEntryFromTypeIndex()
    {
        using var cache = CreateCache();
        cache.Insert("expiredTyped", [1, 2, 3], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-5)).SubscribeAndComplete();
        cache.Insert("validTyped", [4, 5, 6], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

        var typedKeysBefore = cache.GetAllKeys(typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(typedKeysBefore).Contains("validTyped");

        cache.Vacuum().SubscribeAndComplete();

        var typedKeysAfter = cache.GetAllKeys(typeof(UserObject)).ToList().SubscribeGetValue();
        await Assert.That(typedKeysAfter).DoesNotContain("expiredTyped");
        await Assert.That(typedKeysAfter).Contains("validTyped");
    }

    /// <summary>
    /// Tests that constructing InMemoryBlobCache with a null scheduler throws ArgumentNullException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowOnNullScheduler() =>
        await Assert.That(static () => new InMemoryBlobCache(null!, new SystemJsonSerializer()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that constructing InMemoryBlobCache with a null ISerializer throws ArgumentNullException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowOnNullSerializer() =>
        await Assert.That(static () => new InMemoryBlobCache(ImmediateScheduler.Instance, null))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that the single-arg ISerializer constructor throws on null serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SingleArgSerializerConstructorShouldThrowOnNull() =>
        await Assert.That(static () => new InMemoryBlobCache((ISerializer)null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that the string constructor throws when the serializer type cannot be resolved.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task StringConstructorShouldThrowWhenSerializerNotRegistered() =>
        await Assert.That(static () => new InMemoryBlobCache("NonExistentSerializerContract"))
            .ThrowsException();

    /// <summary>
    /// Tests GetObject returns default(T) when the stored byte array is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectShouldReturnDefaultWhenStoredDataIsNull()
    {
        using var cache = CreateCache();

        // Insert a null byte array directly via the raw Insert method.
        cache.Insert("nulldata", null!, typeof(UserObject)).SubscribeAndComplete();

        var received = false;
        var result = cache.GetObject<UserObject>("nulldata").Do(_ => received = true).SubscribeGetValue();
        await Assert.That(received).IsTrue();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type) is a no-op when the key is not found in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationWithTypeShouldNoopWhenKeyMissing()
    {
        using var cache = CreateCache();

        // Should complete without error even though the key does not exist.
        cache.UpdateExpiration("missing", typeof(UserObject), DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type) is a no-op when the stored entry has a different type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithTypeShouldIgnoreMismatchedType()
    {
        using var cache = CreateCache();
        cache.Insert("k", [1], typeof(UserObject)).SubscribeAndComplete();

        // Update with a different type should be a no-op.
        cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

        var value = cache.Get("k").SubscribeGetValue();
        await Assert.That(value).IsNotNull();
    }

    /// <summary>
    /// Tests UpdateExpiration(key) is a no-op when the key is not found in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldNoopWhenKeyMissing()
    {
        using var cache = CreateCache();

        // Should complete without error even though the key does not exist.
        cache.UpdateExpiration("missing", DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests UpdateExpiration(keys) is a no-op for keys not in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldNoopWhenKeysMissing()
    {
        using var cache = CreateCache();
        cache.UpdateExpiration(["missing1", "missing2"], DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type) is a no-op for keys not in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithTypeShouldNoopWhenKeysMissing()
    {
        using var cache = CreateCache();
        cache.UpdateExpiration(["missing"], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.CollectExpiredKeys"/> returns the keys
    /// of every entry whose <c>ExpiresAt</c> is at or before the supplied <c>now</c> cutoff.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CollectExpiredKeysShouldReturnEntriesAtOrBeforeNow()
    {
        DateTimeOffset now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Dictionary<string, CacheEntry> cache = new()
        {
            ["expired"] = new("expired", TypeName: null, Value: null, default, now.AddMinutes(-5).UtcDateTime),
            ["expiringNow"] = new("expiringNow", TypeName: null, Value: null, default, now.UtcDateTime),
            ["future"] = new("future", TypeName: null, Value: null, default, now.AddHours(1).UtcDateTime),
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
        DateTimeOffset now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Dictionary<string, CacheEntry> cache = new()
        {
            ["k1"] = new("k1", TypeName: null, Value: null, default, now.AddHours(1).UtcDateTime),
            ["k2"] = new("k2", TypeName: null, Value: null, default, ExpiresAt: null),
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
        Dictionary<Type, HashSet<string>> typeIndex = new()
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
        Dictionary<Type, HashSet<string>> typeIndex = [];

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
        DateTimeOffset now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Dictionary<string, CacheEntry> cache = new()
        {
            ["expired"] = new("expired", "T1", Value: null, default, now.AddMinutes(-1).UtcDateTime),
            ["valid"] = new("valid", "T1", Value: null, default, now.AddHours(1).UtcDateTime),
        };
        Dictionary<Type, HashSet<string>> typeIndex = new()
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
        DateTimeOffset now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Dictionary<string, CacheEntry> cache = new()
        {
            ["valid"] = new("valid", TypeName: null, Value: null, default, now.AddHours(1).UtcDateTime),
        };
        Dictionary<Type, HashSet<string>> typeIndex = new()
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
        DateTimeOffset now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Dictionary<string, CacheEntry> cache = new()
        {
            ["expired"] = new("expired", TypeName: null, Value: null, default, now.AddMinutes(-1).UtcDateTime),
        };
        Dictionary<Type, HashSet<string>> typeIndex = [];

        InMemoryBlobCacheBase.VacuumExpiredEntries(cache, typeIndex, now);

        await Assert.That(cache).DoesNotContainKey("expired");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.RemoveKeyFromTypeIndexFast"/> removes the key
    /// from the single type bucket tracked by <c>keyToType</c> and clears the
    /// reverse-map entry, without touching unrelated buckets.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveKeyFromTypeIndexFastShouldRemoveFromTrackedBucketOnly()
    {
        Dictionary<Type, HashSet<string>> typeIndex = new()
        {
            [typeof(string)] = ["k1", "k2"],
            [typeof(int)] = ["k3"],
        };
        Dictionary<string, Type> keyToType = new(StringComparer.Ordinal)
        {
            ["k1"] = typeof(string),
            ["k2"] = typeof(string),
            ["k3"] = typeof(int),
        };

        InMemoryBlobCacheBase.RemoveKeyFromTypeIndexFast(typeIndex, keyToType, "k1");

        await Assert.That(typeIndex[typeof(string)]).DoesNotContain("k1");
        await Assert.That(typeIndex[typeof(string)]).Contains("k2");
        await Assert.That(typeIndex[typeof(int)]).Contains("k3");
        await Assert.That(keyToType).DoesNotContainKey("k1");
        await Assert.That(keyToType).ContainsKey("k2");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.RemoveKeyFromTypeIndexFast"/> is a no-op when
    /// the key was never tracked by the reverse map (e.g. it was inserted via an untyped <c>Insert</c>).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveKeyFromTypeIndexFastShouldNoOpForUntrackedKey()
    {
        Dictionary<Type, HashSet<string>> typeIndex = new()
        {
            [typeof(string)] = ["tracked"],
        };
        Dictionary<string, Type> keyToType = new(StringComparer.Ordinal)
        {
            ["tracked"] = typeof(string),
        };

        InMemoryBlobCacheBase.RemoveKeyFromTypeIndexFast(typeIndex, keyToType, "untracked");

        await Assert.That(typeIndex[typeof(string)]).Contains("tracked");
        await Assert.That(keyToType).ContainsKey("tracked");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.RemoveKeyFromTypeIndexFast"/> tolerates the
    /// case where the reverse map points to a type that has already been evicted from
    /// <c>typeIndex</c> — it still cleans the reverse-map entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveKeyFromTypeIndexFastShouldClearReverseMapWhenTypeBucketMissing()
    {
        Dictionary<Type, HashSet<string>> typeIndex = [];
        Dictionary<string, Type> keyToType = new(StringComparer.Ordinal)
        {
            ["k1"] = typeof(string),
        };

        InMemoryBlobCacheBase.RemoveKeyFromTypeIndexFast(typeIndex, keyToType, "k1");

        await Assert.That(keyToType).DoesNotContainKey("k1");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.VacuumExpiredEntriesFast"/> removes expired
    /// entries from the cache, prunes the corresponding entries from both <c>typeIndex</c>
    /// and <c>keyToType</c>, and preserves valid entries untouched.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumExpiredEntriesFastShouldRemoveExpiredAndPruneIndexes()
    {
        DateTimeOffset now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Dictionary<string, CacheEntry> cache = new(StringComparer.Ordinal)
        {
            ["expired"] = new("expired", "System.String", Value: null, default, now.AddMinutes(-1).UtcDateTime),
            ["valid"] = new("valid", "System.String", Value: null, default, now.AddHours(1).UtcDateTime),
        };
        Dictionary<Type, HashSet<string>> typeIndex = new()
        {
            [typeof(string)] = new(StringComparer.Ordinal) { "expired", "valid" },
        };
        Dictionary<string, Type> keyToType = new(StringComparer.Ordinal)
        {
            ["expired"] = typeof(string),
            ["valid"] = typeof(string),
        };

        InMemoryBlobCacheBase.VacuumExpiredEntriesFast(cache, typeIndex, keyToType, now);

        await Assert.That(cache).DoesNotContainKey("expired");
        await Assert.That(cache).ContainsKey("valid");
        await Assert.That(typeIndex[typeof(string)]).DoesNotContain("expired");
        await Assert.That(typeIndex[typeof(string)]).Contains("valid");
        await Assert.That(keyToType).DoesNotContainKey("expired");
        await Assert.That(keyToType).ContainsKey("valid");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.VacuumExpiredEntriesFast"/> is a no-op when
    /// nothing is expired — cache, type-index, and reverse-map are all left untouched.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumExpiredEntriesFastShouldBeNoOpWhenNothingExpired()
    {
        DateTimeOffset now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Dictionary<string, CacheEntry> cache = new(StringComparer.Ordinal)
        {
            ["valid"] = new("valid", TypeName: null, Value: null, default, now.AddHours(1).UtcDateTime),
        };
        Dictionary<Type, HashSet<string>> typeIndex = new()
        {
            [typeof(string)] = new(StringComparer.Ordinal) { "valid" },
        };
        Dictionary<string, Type> keyToType = new(StringComparer.Ordinal)
        {
            ["valid"] = typeof(string),
        };

        InMemoryBlobCacheBase.VacuumExpiredEntriesFast(cache, typeIndex, keyToType, now);

        await Assert.That(cache).ContainsKey("valid");
        await Assert.That(typeIndex[typeof(string)]).Contains("valid");
        await Assert.That(keyToType).ContainsKey("valid");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.VacuumExpiredEntriesFast"/> handles expired
    /// entries whose keys were never typed (never tracked in <c>keyToType</c>) —
    /// the cache row is still removed and no exception is thrown.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumExpiredEntriesFastShouldRemoveUntrackedExpiredEntries()
    {
        DateTimeOffset now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Dictionary<string, CacheEntry> cache = new(StringComparer.Ordinal)
        {
            ["expired-untyped"] = new("expired-untyped", TypeName: null, Value: null, default, now.AddMinutes(-1).UtcDateTime),
        };
        Dictionary<Type, HashSet<string>> typeIndex = [];
        Dictionary<string, Type> keyToType = new(StringComparer.Ordinal);

        InMemoryBlobCacheBase.VacuumExpiredEntriesFast(cache, typeIndex, keyToType, now);

        await Assert.That(cache).DoesNotContainKey("expired-untyped");
    }

    /// <summary>
    /// Verifies the untyped <see cref="InMemoryBlobCacheBase.Insert(IEnumerable{KeyValuePair{string, byte[]}}, DateTimeOffset?)"/>
    /// overload short-circuits to the cached unit observable when handed an empty
    /// <see cref="ICollection{T}"/>, without scheduling any work on the thread pool.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertIEnumerableShouldEarlyExitOnEmptyCollection()
    {
        using var cache = CreateCache();

        await cache.Insert([]);

        var keys = await cache.GetAllKeys().ToList();
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Verifies the typed <see cref="InMemoryBlobCacheBase.Insert(IEnumerable{KeyValuePair{string, byte[]}}, Type, DateTimeOffset?)"/>
    /// overload short-circuits to the cached unit observable on empty input.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypedInsertIEnumerableShouldEarlyExitOnEmptyCollection()
    {
        using var cache = CreateCache();

        await cache.Insert([], typeof(string));

        var typedKeys = await cache.GetAllKeys(typeof(string)).ToList();
        await Assert.That(typedKeys).IsEmpty();
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.Invalidate(IEnumerable{string})"/> short-circuits
    /// on an empty key set — the cache stays untouched and no observable start work runs.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateIEnumerableShouldEarlyExitOnEmptyCollection()
    {
        using var cache = CreateCache();
        await cache.Insert("survivor", [9]);

        await cache.Invalidate([]);

        var keys = await cache.GetAllKeys().ToList();
        await Assert.That(keys).Contains("survivor");
    }

    /// <summary>
    /// Verifies <see cref="InMemoryBlobCacheBase.Invalidate(IEnumerable{string})"/> still
    /// removes entries when handed an iterator-based <see cref="IEnumerable{T}"/> that is not
    /// an <see cref="ICollection{T}"/> — drives the false branch of the fast-path
    /// <c>ICollection</c> type-pattern guard and exercises the regular materialisation loop.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateIEnumerableShouldHandleNonICollectionSource()
    {
        using var cache = CreateCache();
        await cache.Insert("drop-1", [1]);
        await cache.Insert("drop-2", [2]);
        await cache.Insert("keep", [3]);

        await cache.Invalidate(IteratorKeys());

        var keys = (await cache.GetAllKeys().ToList()).ToList();
        await Assert.That(keys).Contains("keep");
        await Assert.That(keys).DoesNotContain("drop-1");
        await Assert.That(keys).DoesNotContain("drop-2");

        static IEnumerable<string> IteratorKeys()
        {
            yield return "drop-1";
            yield return "drop-2";
        }
    }

    /// <summary>
    /// Verifies the bulk typed <see cref="InMemoryBlobCacheBase.Insert(IEnumerable{KeyValuePair{string, byte[]}}, Type, DateTimeOffset?)"/>
    /// evicts a key from its previous type bucket when it is re-inserted under a new type,
    /// preserving the "one type per key" invariant that the O(1) reverse index depends on.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BulkTypedInsertShouldEvictKeyFromPreviousTypeBucket()
    {
        using var cache = CreateCache();

        // Arrange: land "k1" in the string bucket first.
        await cache.Insert([new("k1", [1])], typeof(string));
        await Assert.That((await cache.GetAllKeys(typeof(string)).ToList()).ToList()).Contains("k1");

        // Act: re-insert the same key under a different type, in the bulk path.
        await cache.Insert([new("k1", [2])], typeof(int));

        // Assert: k1 is now only in the int bucket.
        await Assert.That((await cache.GetAllKeys(typeof(string)).ToList()).ToList()).DoesNotContain("k1");
        await Assert.That((await cache.GetAllKeys(typeof(int)).ToList()).ToList()).Contains("k1");
    }

    /// <summary>
    /// Verifies the single-key typed <see cref="InMemoryBlobCacheBase.Insert(string, byte[], Type, DateTimeOffset?)"/>
    /// evicts a key from its previous type bucket when it is re-inserted under a new type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SingleKeyTypedInsertShouldEvictKeyFromPreviousTypeBucket()
    {
        using var cache = CreateCache();

        await cache.Insert("k1", [1], typeof(string));
        await Assert.That((await cache.GetAllKeys(typeof(string)).ToList()).ToList()).Contains("k1");

        await cache.Insert("k1", [2], typeof(int));

        await Assert.That((await cache.GetAllKeys(typeof(string)).ToList()).ToList()).DoesNotContain("k1");
        await Assert.That((await cache.GetAllKeys(typeof(int)).ToList()).ToList()).Contains("k1");
    }

    /// <summary>
    /// Verifies the single-key typed <see cref="InMemoryBlobCacheBase.Insert(string, byte[], Type, DateTimeOffset?)"/>
    /// is a no-op for the "same type, same key" path — the reverse index remains consistent
    /// and the key stays in its existing bucket.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SingleKeyTypedInsertReplayingSameTypeShouldRetainBucketMembership()
    {
        using var cache = CreateCache();

        await cache.Insert("k1", [1], typeof(string));
        await cache.Insert("k1", [2], typeof(string));

        var keys = await cache.GetAllKeys(typeof(string)).ToList();
        await Assert.That(keys.ToList()).Contains("k1");
    }

    /// <summary>
    /// The string-based constructor throws <see cref="InvalidOperationException"/> when
    /// no serializer is registered for the given contract (line 26 null-coalescing throw).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Ctor_WithUnregisteredSerializerType_ThrowsInvalidOperation() =>
        await Assert.That(static () => _ = new InMemoryBlobCache("NonExistentSerializer"))
            .ThrowsException();

    /// <summary>
    /// Creates a new <see cref="InMemoryBlobCache"/> using the immediate scheduler and System.Text.Json serializer.
    /// </summary>
    /// <returns>A new in-memory blob cache instance.</returns>
    private static InMemoryBlobCache CreateCache() =>
        new(ImmediateScheduler.Instance, new SystemJsonSerializer());

    /// <summary>
    /// A minimal <see cref="ISerializer"/> stub used to verify the
    /// <see cref="InMemoryBlobCacheBase.ForcedDateTimeKind"/> setter propagates kind updates
    /// to a serializer registered with <see cref="AppLocator"/>. Records the most
    /// recent setter invocation so the test can assert the value reached this instance.
    /// </summary>
    private sealed class RecordingForcedKindSerializer : ISerializer
    {
        /// <summary>
        /// Gets the last value assigned to <see cref="ForcedDateTimeKind"/>.
        /// </summary>
        public DateTimeKind? LastSetKind { get; private set; }

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind
        {
            get;
            set
            {
                field = value;
                LastSetKind = value;
            }
        }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) => default;

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => [];
    }
}
