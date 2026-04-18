// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests focused on SqliteBlobCache.InvalidateAll behavior.
/// </summary>
[Category("Akavache")]
public class SqliteBlobCacheInvalidateAllTests
{
    /// <summary>
    /// Verifies that InvalidateAll removes all untyped items and they cannot be retrieved afterwards.
    /// </summary>
    /// <returns>A task to await.</returns>
    [Test]
    public async Task InvalidateAll_ShouldRemove_AllItems()
    {
        SystemJsonSerializer serializer = new();
        using SqliteBlobCache cache = new(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);

        // Arrange
        cache.Insert("a", [1]).SubscribeAndComplete();
        cache.Insert("b", [2]).SubscribeAndComplete();
        cache.Insert("c", [3]).SubscribeAndComplete();

        var keysBefore = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keysBefore).Count().IsEqualTo(3);

        // Act
        cache.InvalidateAll().SubscribeAndComplete();

        // Assert
        var keysAfter = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keysAfter).IsEmpty();

        var errorA = cache.Get("a").SubscribeGetError();
        await Assert.That(errorA).IsTypeOf<KeyNotFoundException>();

        var errorB = cache.Get("b").SubscribeGetError();
        await Assert.That(errorB).IsTypeOf<KeyNotFoundException>();

        var errorC = cache.Get("c").SubscribeGetError();
        await Assert.That(errorC).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>
    /// Verifies that InvalidateAll removes both typed and untyped items.
    /// </summary>
    /// <returns>A task to await.</returns>
    [Test]
    public async Task InvalidateAll_ShouldRemove_TypedAndUntypedItems()
    {
        SystemJsonSerializer serializer = new();
        using SqliteBlobCache cache = new(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);

        // Arrange: mix typed and untyped entries
        cache.Insert("u1", [1]).SubscribeAndComplete();
        cache.Insert("u2", [2]).SubscribeAndComplete();

        var userType = typeof(string);
        cache.Insert("t1", [10], userType).SubscribeAndComplete();
        cache.Insert("t2", [20], userType).SubscribeAndComplete();

        var keysBefore = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keysBefore).Count().IsEqualTo(4);

        // Act
        cache.InvalidateAll().SubscribeAndComplete();

        // Assert
        var keysAfter = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keysAfter).IsEmpty();

        // Both typed and untyped should be gone
        var errorU1 = cache.Get("u1").SubscribeGetError();
        await Assert.That(errorU1).IsTypeOf<KeyNotFoundException>();

        var errorU2 = cache.Get("u2").SubscribeGetError();
        await Assert.That(errorU2).IsTypeOf<KeyNotFoundException>();

        var errorT1 = cache.Get("t1", userType).SubscribeGetError();
        await Assert.That(errorT1).IsTypeOf<KeyNotFoundException>();

        var errorT2 = cache.Get("t2", userType).SubscribeGetError();
        await Assert.That(errorT2).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>
    /// Verifies that InvalidateAll clears all items even when some entries are expired and filtered from GetAllKeys.
    /// </summary>
    /// <returns>A task to await.</returns>
    [Test]
    public async Task InvalidateAll_ShouldIgnore_ExpiredEntriesButStillClearAll()
    {
        SystemJsonSerializer serializer = new();
        using SqliteBlobCache cache = new(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);

        // Arrange: one expired, one not
        cache.Insert("live", [1], DateTimeOffset.Now.AddMinutes(5)).SubscribeAndComplete();
        cache.Insert("expired", [2], DateTimeOffset.Now.AddMilliseconds(200)).SubscribeAndComplete();

        // wait for expiration
        await Task.Delay(300);

        var keysBefore = cache.GetAllKeys().ToList().SubscribeGetValue();

        // live remains, expired filtered out by GetAllKeys — keysBefore may be 1
        await Assert.That(keysBefore).Count().IsLessThanOrEqualTo(1);

        // Act
        cache.InvalidateAll().SubscribeAndComplete();

        // Assert
        var keysAfter = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keysAfter).IsEmpty();

        var errorLive = cache.Get("live").SubscribeGetError();
        await Assert.That(errorLive).IsTypeOf<KeyNotFoundException>();

        var errorExpired = cache.Get("expired").SubscribeGetError();
        await Assert.That(errorExpired).IsTypeOf<KeyNotFoundException>();
    }
}
