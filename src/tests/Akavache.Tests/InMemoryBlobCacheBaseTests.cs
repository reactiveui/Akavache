// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
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
        await cache.DisposeAsync();

        await Assert.That(() => cache.Insert([new("k", [1])]).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Insert(KeyValuePairs) throws on null input.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertKeyValuePairsShouldThrowOnNull()
    {
        await using var cache = CreateCache();
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
        await cache.DisposeAsync();

        await Assert.That(() => cache.Insert("key", [1, 2, 3]).ToTask())
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

        await Assert.That(() => cache.Get("key").ToTask())
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

        await cache.Get(["k1", "k2"]).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
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

        await cache.GetAllKeys().ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
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

        await Assert.That(() => cache.GetCreatedAt("key").ToTask())
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

        await Assert.That(() => cache.Invalidate("key").ToTask())
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

        await Assert.That(() => cache.Invalidate(["k1", "k2"]).ToTask())
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

        await Assert.That(() => cache.InvalidateAll().ToTask())
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

        await Assert.That(() => cache.Vacuum().ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, expiration) errors on null/whitespace key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldErrorOnEmptyKey()
    {
        await using var cache = CreateCache();
        await Assert.That(() => cache.UpdateExpiration(string.Empty, DateTimeOffset.Now).ToTask())
            .Throws<ArgumentException>();
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

        await Assert.That(() => cache.UpdateExpiration("key", DateTimeOffset.Now).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type, expiration) errors on null/whitespace key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationTypeShouldErrorOnEmptyKey()
    {
        await using var cache = CreateCache();
        await Assert.That(() => cache.UpdateExpiration(string.Empty, typeof(string), DateTimeOffset.Now).ToTask())
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type, expiration) errors on null type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationTypeShouldErrorOnNullType()
    {
        await using var cache = CreateCache();
        await Assert.That(() => cache.UpdateExpiration("key", null!, DateTimeOffset.Now).ToTask())
            .Throws<ArgumentNullException>();
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

        await Assert.That(() => cache.UpdateExpiration("key", typeof(string), DateTimeOffset.Now).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, expiration) errors on null keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldErrorOnNullKeys()
    {
        await using var cache = CreateCache();
        await Assert.That(() => cache.UpdateExpiration((IEnumerable<string>)null!, DateTimeOffset.Now).ToTask())
            .Throws<ArgumentNullException>();
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

        await Assert.That(() => cache.UpdateExpiration(["k1"], DateTimeOffset.Now).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type, expiration) errors on null keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysTypeShouldErrorOnNullKeys()
    {
        await using var cache = CreateCache();
        await Assert.That(() => cache.UpdateExpiration((IEnumerable<string>)null!, typeof(string), DateTimeOffset.Now).ToTask())
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type, expiration) errors on null type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysTypeShouldErrorOnNullType()
    {
        await using var cache = CreateCache();
        await Assert.That(() => cache.UpdateExpiration(["key"], null!, DateTimeOffset.Now).ToTask())
            .Throws<ArgumentNullException>();
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

        await Assert.That(() => cache.UpdateExpiration(["k1"], typeof(string), DateTimeOffset.Now).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests UpdateExpiration successfully updates expiration on existing entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldUpdateExistingEntry()
    {
        await using var cache = CreateCache();
        await cache.Insert("key1", [1, 2, 3]).ToTask();
        await cache.UpdateExpiration("key1", DateTimeOffset.Now.AddHours(1)).ToTask();

        var data = await cache.Get("key1").ToTask();
        await Assert.That(data).IsNotNull();
    }

    /// <summary>
    /// Tests UpdateExpiration on multiple keys updates all matching entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldUpdateMultiple()
    {
        await using var cache = CreateCache();
        await cache.Insert("k1", [1]).ToTask();
        await cache.Insert("k2", [2]).ToTask();

        await cache.UpdateExpiration(["k1", "k2"], DateTimeOffset.Now.AddHours(1)).ToTask();

        var d1 = await cache.Get("k1").ToTask();
        var d2 = await cache.Get("k2").ToTask();
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
        await using var cache = CreateCache();

        // Insert with already-expired timestamp
        await cache.Insert("expired", [1], DateTimeOffset.Now.AddSeconds(-10)).ToTask();
        await cache.Insert("valid", [2], DateTimeOffset.Now.AddHours(1)).ToTask();

        await cache.Vacuum().ToTask();

        var keys = await cache.GetAllKeys().ToList().ToTask();
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
        await using var cache = CreateCache();
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
        await using var cache = CreateCache();
        UserObject user = new() { Name = "Alice", Bio = "Dev", Blog = "https://example.com" };
        await cache.InsertObject("user-1", user).ToTask();

        var result = await cache.GetObject<UserObject>("user-1").ToTask();
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
        await using var cache = CreateCache();
        await cache.InsertObject("k", new UserObject { Name = "Bob" }, DateTimeOffset.Now.AddHours(1)).ToTask();
        var result = await cache.GetObject<UserObject>("k").ToTask();
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
        await cache.DisposeAsync();

        await Assert.That(() => cache.InsertObject("k", new UserObject()).ToTask())
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

        await Assert.That(() => cache.GetObject<UserObject>("k").ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetAllObjects returns all stored instances of the requested type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllObjectsShouldReturnAllItemsForType()
    {
        await using var cache = CreateCache();
        await cache.InsertObject("a", new UserObject { Name = "A" }).ToTask();
        await cache.InsertObject("b", new UserObject { Name = "B" }).ToTask();
        await cache.InsertObject("c", new UserObject { Name = "C" }).ToTask();

        var all = await cache.GetAllObjects<UserObject>().ToTask();
        var list = all.ToList();
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
        await cache.DisposeAsync();

        await cache.GetAllObjects<UserObject>().ToTask().ShouldThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetObjectCreatedAt returns the created timestamp for a typed entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldReturnTimestamp()
    {
        await using var cache = CreateCache();
        await cache.InsertObject("k", new UserObject { Name = "x" }).ToTask();
        var createdAt = await cache.GetObjectCreatedAt<UserObject>("k").ToTask();
        await Assert.That(createdAt).IsNotNull();
    }

    /// <summary>
    /// Tests InvalidateObject removes a typed entry so subsequent GetObject fails.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectShouldRemoveEntry()
    {
        await using var cache = CreateCache();
        await cache.InsertObject("k", new UserObject { Name = "x" }).ToTask();
        await cache.InvalidateObject<UserObject>("k").ToTask();

        await cache.GetObject<UserObject>("k").ToTask().ShouldThrowAsync<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests InvalidateAllObjects removes all entries for a type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllObjectsShouldRemoveTypedEntries()
    {
        await using var cache = CreateCache();
        await cache.InsertObject("a", new UserObject { Name = "A" }).ToTask();
        await cache.InsertObject("b", new UserObject { Name = "B" }).ToTask();

        await cache.InvalidateAllObjects<UserObject>().ToTask();

        var all = await cache.GetAllObjects<UserObject>().ToTask();
        await Assert.That(all.Count()).IsEqualTo(0);
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

        await Assert.That(() => cache.InvalidateAll(typeof(UserObject)).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Insert(keyValuePairs, type) stores entries in the type index.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertKeyValuePairsWithTypeShouldPopulateTypeIndex()
    {
        await using var cache = CreateCache();
        KeyValuePair<string, byte[]>[] pairs =
        [
            new("k1", [1, 2, 3]),
            new("k2", [4, 5, 6])
        ];
        await cache.Insert(pairs, typeof(UserObject)).ToTask();

        var keys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
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
        await cache.DisposeAsync();

        await Assert.That(() => cache.Insert([new("k", [1])], typeof(UserObject)).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Insert(key, data, type) stores the entry with the type index.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertWithTypeShouldPopulateTypeIndex()
    {
        await using var cache = CreateCache();
        await cache.Insert("k", [1, 2, 3], typeof(UserObject)).ToTask();
        var keys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
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
        await cache.DisposeAsync();

        await Assert.That(() => cache.Insert("k", [1], typeof(UserObject)).ToTask())
            .Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests Get(key, type) delegates to Get(key) and returns the stored value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetWithTypeShouldReturnValue()
    {
        await using var cache = CreateCache();
        await cache.Insert("k", [7, 8, 9], typeof(UserObject)).ToTask();
        var data = await cache.Get("k", typeof(UserObject)).ToTask();
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
        await using var cache = CreateCache();
        await cache.Insert("k1", [1], typeof(UserObject)).ToTask();
        await cache.Insert("k2", [2], typeof(UserObject)).ToTask();

        var results = await cache.Get(["k1", "k2", "missing"], typeof(UserObject)).ToList().ToTask();
        await Assert.That(results.Count).IsEqualTo(2);
    }

    /// <summary>
    /// Tests GetAll(type) returns stored entries and removes expired ones.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllByTypeShouldReturnValidAndRemoveExpired()
    {
        await using var cache = CreateCache();
        await cache.Insert("valid", [1], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();
        await cache.Insert("expired", [2], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-10)).ToTask();

        var all = await cache.GetAll(typeof(UserObject)).ToList().ToTask();
        await Assert.That(all.Count).IsEqualTo(1);
        await Assert.That(all[0].Key).IsEqualTo("valid");
    }

    /// <summary>
    /// Tests GetAll(type) returns an empty sequence when no entries exist for the type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllByTypeShouldBeEmptyWhenTypeMissing()
    {
        await using var cache = CreateCache();
        var all = await cache.GetAll(typeof(UserObject)).ToList().ToTask();
        await Assert.That(all.Count).IsEqualTo(0);
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

        await cache.GetAll(typeof(UserObject)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetAllKeys(type) returns valid keys and removes expired ones.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysByTypeShouldReturnValidAndRemoveExpired()
    {
        await using var cache = CreateCache();
        await cache.Insert("valid", [1], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();
        await cache.Insert("expired", [2], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-5)).ToTask();

        var keys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
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
        await using var cache = CreateCache();
        var keys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
        await Assert.That(keys.Count).IsEqualTo(0);
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

        await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetCreatedAt(keys) returns timestamps for known keys and null for missing ones.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtKeysShouldReturnTimestampsAndNulls()
    {
        await using var cache = CreateCache();
        await cache.Insert("k1", [1]).ToTask();

        var results = await cache.GetCreatedAt(["k1", "missing"]).ToList().ToTask();
        await Assert.That(results.Count).IsEqualTo(2);

        (_, var time) = results.First(static r => r.Key == "k1");
        (_, var dateTimeOffset) = results.First(static r => r.Key == "missing");
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
        await cache.DisposeAsync();

        await cache.GetCreatedAt(["k"]).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests GetCreatedAt(keys, type) returns the same timestamps as the non-type overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtKeysWithTypeShouldReturnTimestamps()
    {
        await using var cache = CreateCache();
        await cache.Insert("k1", [1], typeof(UserObject)).ToTask();
        var results = await cache.GetCreatedAt(["k1"], typeof(UserObject)).ToList().ToTask();
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Time).IsNotNull();
    }

    /// <summary>
    /// Tests GetCreatedAt(key, type) returns the created timestamp.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtWithTypeShouldReturnTimestamp()
    {
        await using var cache = CreateCache();
        await cache.Insert("k1", [1], typeof(UserObject)).ToTask();
        var created = await cache.GetCreatedAt("k1", typeof(UserObject)).ToTask();
        await Assert.That(created).IsNotNull();
    }

    /// <summary>
    /// Tests Flush() completes successfully.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FlushShouldComplete()
    {
        await using var cache = CreateCache();
        var result = await cache.Flush().ToTask();
        await Assert.That(result).IsEqualTo(Unit.Default);
    }

    /// <summary>
    /// Tests Flush(type) completes successfully.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FlushTypeShouldComplete()
    {
        await using var cache = CreateCache();
        var result = await cache.Flush(typeof(UserObject)).ToTask();
        await Assert.That(result).IsEqualTo(Unit.Default);
    }

    /// <summary>
    /// Tests Invalidate(key, type) removes the entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateWithTypeShouldRemoveEntry()
    {
        await using var cache = CreateCache();
        await cache.Insert("k", [1], typeof(UserObject)).ToTask();
        await cache.Invalidate("k", typeof(UserObject)).ToTask();

        await cache.Get("k").ToTask().ShouldThrowAsync<KeyNotFoundException>();
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
        await using var cache = CreateCache();
        await cache.Insert("expired", [1], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-5)).ToTask();

        await cache.Get("expired").ToTask().ShouldThrowAsync<KeyNotFoundException>();

        // After the failed Get, the key should be removed from the type index as well.
        var keys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
        await Assert.That(keys).DoesNotContain("expired");
    }

    /// <summary>
    /// Tests Get(key) throws KeyNotFoundException for a missing key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetShouldThrowWhenKeyMissing()
    {
        await using var cache = CreateCache();
        await Assert.That(() => cache.Get("missing").ToTask())
            .Throws<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type) updates entries matching the type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationWithTypeShouldUpdateMatching()
    {
        await using var cache = CreateCache();
        await cache.Insert("k", [1], typeof(UserObject)).ToTask();
        await cache.UpdateExpiration("k", typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();

        var value = await cache.Get("k").ToTask();
        await Assert.That(value).IsNotNull();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type) is a no-op when the stored entry has a different type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationWithTypeShouldIgnoreMismatchedType()
    {
        await using var cache = CreateCache();
        await cache.Insert("k", [1], typeof(UserObject)).ToTask();

        // Should not throw; simply no update performed since type mismatches.
        await cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now.AddHours(1)).ToTask();

        var value = await cache.Get("k").ToTask();
        await Assert.That(value).IsNotNull();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type) updates entries with matching type only.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithTypeShouldUpdateMatching()
    {
        await using var cache = CreateCache();
        await cache.Insert("k1", [1], typeof(UserObject)).ToTask();
        await cache.Insert("k2", [2], typeof(UserObject)).ToTask();

        await cache.UpdateExpiration(["k1", "k2"], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();

        var v1 = await cache.Get("k1").ToTask();
        var v2 = await cache.Get("k2").ToTask();
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
        await using var cache = CreateCache();
        await cache.Insert("valid", [1], DateTimeOffset.Now.AddHours(1)).ToTask();
        await cache.Insert("expired", [2], DateTimeOffset.Now.AddSeconds(-5)).ToTask();

        var keys = await cache.GetAllKeys().ToList().ToTask();
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
        await using var cache = CreateCache();
        await cache.Insert("k1", [1]).ToTask();
        await cache.Insert("k2", [2], typeof(UserObject)).ToTask();

        await cache.InvalidateAll().ToTask();

        var keys = await cache.GetAllKeys().ToList().ToTask();
        await Assert.That(keys.Count).IsEqualTo(0);
        var typedKeys = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
        await Assert.That(typedKeys.Count).IsEqualTo(0);
    }

    /// <summary>
    /// Tests the single-argument constructor uses the default task pool scheduler.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SingleArgConstructorShouldUseTaskpoolScheduler()
    {
        await using InMemoryBlobCache cache = new(new SystemJsonSerializer());
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
        await using var cache = CreateCache();
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

        await using var cache = CreateCache();
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
        await using var cache = CreateCache();

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
        await using var cache = CreateCache();

        // Insert untyped entries only — the type index never gets populated.
        await cache.Insert("expired", [1, 2, 3], DateTimeOffset.Now.AddSeconds(-5)).ToTask();
        await cache.Insert("valid", [4, 5, 6], DateTimeOffset.Now.AddHours(1)).ToTask();

        await cache.Vacuum().ToTask();

        var keys = await cache.GetAllKeys().ToList().ToTask();
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
        await using var cache = CreateCache();
        await cache.Insert("expiredTyped", [1, 2, 3], typeof(UserObject), DateTimeOffset.Now.AddSeconds(-5)).ToTask();
        await cache.Insert("validTyped", [4, 5, 6], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();

        var typedKeysBefore = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
        await Assert.That(typedKeysBefore).Contains("validTyped");

        await cache.Vacuum().ToTask();

        var typedKeysAfter = await cache.GetAllKeys(typeof(UserObject)).ToList().ToTask();
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
            .Throws<InvalidOperationException>();

    /// <summary>
    /// Tests GetObject returns default(T) when the stored byte array is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectShouldReturnDefaultWhenStoredDataIsNull()
    {
        await using var cache = CreateCache();

        // Insert a null byte array directly via the raw Insert method.
        await cache.Insert("nulldata", null!, typeof(UserObject)).ToTask();

        var result = await cache.GetObject<UserObject>("nulldata").ToTask();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests UpdateExpiration(key, type) is a no-op when the key is not found in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationWithTypeShouldNoopWhenKeyMissing()
    {
        await using var cache = CreateCache();

        // Should complete without error even though the key does not exist.
        await cache.UpdateExpiration("missing", typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type) is a no-op when the stored entry has a different type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithTypeShouldIgnoreMismatchedType()
    {
        await using var cache = CreateCache();
        await cache.Insert("k", [1], typeof(UserObject)).ToTask();

        // Update with a different type should be a no-op.
        await cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now.AddHours(1)).ToTask();

        var value = await cache.Get("k").ToTask();
        await Assert.That(value).IsNotNull();
    }

    /// <summary>
    /// Tests UpdateExpiration(key) is a no-op when the key is not found in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationShouldNoopWhenKeyMissing()
    {
        await using var cache = CreateCache();

        // Should complete without error even though the key does not exist.
        await cache.UpdateExpiration("missing", DateTimeOffset.Now.AddHours(1)).ToTask();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys) is a no-op for keys not in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldNoopWhenKeysMissing()
    {
        await using var cache = CreateCache();
        await cache.UpdateExpiration(["missing1", "missing2"], DateTimeOffset.Now.AddHours(1)).ToTask();
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, type) is a no-op for keys not in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithTypeShouldNoopWhenKeysMissing()
    {
        await using var cache = CreateCache();
        await cache.UpdateExpiration(["missing"], typeof(UserObject), DateTimeOffset.Now.AddHours(1)).ToTask();
    }

    /// <summary>
    /// Tests HttpService lazy initialization returns a default instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HttpServiceShouldReturnDefaultInstance()
    {
        await using var cache = CreateCache();
        await Assert.That(cache.HttpService).IsNotNull().And.IsTypeOf<HttpService>();
    }

    /// <summary>
    /// Tests HttpService setter overrides the lazy default.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HttpServiceSetterShouldOverrideDefault()
    {
        await using var cache = CreateCache();

        // Touch the lazy getter first so the setter is clearly overriding a real instance.
        _ = cache.HttpService;
        HttpService custom = new();
        cache.HttpService = custom;
        await Assert.That(cache.HttpService).IsSameReferenceAs(custom);
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
        DateTimeOffset now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Dictionary<string, CacheEntry> cache = new()
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
            ["expired"] = new() { Id = "expired", ExpiresAt = now.AddMinutes(-1).UtcDateTime, TypeName = "T1" },
            ["valid"] = new() { Id = "valid", ExpiresAt = now.AddHours(1).UtcDateTime, TypeName = "T1" },
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
            ["valid"] = new() { Id = "valid", ExpiresAt = now.AddHours(1).UtcDateTime },
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
            ["expired"] = new() { Id = "expired", ExpiresAt = now.AddMinutes(-1).UtcDateTime },
        };
        Dictionary<Type, HashSet<string>> typeIndex = [];

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
