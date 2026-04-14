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
    /// <summary>Default timeout applied to each cache operation in this fixture.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Verifies that InvalidateAll removes all untyped items and they cannot be retrieved afterwards.
    /// </summary>
    /// <returns>A task to await.</returns>
    [Test]
    public async Task InvalidateAll_ShouldRemove_AllItems()
    {
        SystemJsonSerializer serializer = new();
        await using SqliteBlobCache cache = new(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);

        // Arrange
        await cache.Insert("a", [1]).Timeout(Timeout).FirstAsync();
        await cache.Insert("b", [2]).Timeout(Timeout).FirstAsync();
        await cache.Insert("c", [3]).Timeout(Timeout).FirstAsync();

        var keysBefore = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();
        await Assert.That(keysBefore).Count().IsEqualTo(3);

        // Act
        await cache.InvalidateAll().Timeout(Timeout).FirstAsync();

        // Assert
        var keysAfter = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();
        await Assert.That(keysAfter).IsEmpty();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("a").Timeout(Timeout).FirstAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("b").Timeout(Timeout).FirstAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("c").Timeout(Timeout).FirstAsync());
    }

    /// <summary>
    /// Verifies that InvalidateAll removes both typed and untyped items.
    /// </summary>
    /// <returns>A task to await.</returns>
    [Test]
    public async Task InvalidateAll_ShouldRemove_TypedAndUntypedItems()
    {
        SystemJsonSerializer serializer = new();
        await using SqliteBlobCache cache = new(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);

        // Arrange: mix typed and untyped entries
        await cache.Insert("u1", [1]).Timeout(Timeout).FirstAsync();
        await cache.Insert("u2", [2]).Timeout(Timeout).FirstAsync();

        var userType = typeof(string);
        await cache.Insert("t1", [10], userType).Timeout(Timeout).FirstAsync();
        await cache.Insert("t2", [20], userType).Timeout(Timeout).FirstAsync();

        var keysBefore = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();
        await Assert.That(keysBefore).Count().IsEqualTo(4);

        // Act
        await cache.InvalidateAll().Timeout(Timeout).FirstAsync();

        // Assert
        var keysAfter = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();
        await Assert.That(keysAfter).IsEmpty();

        // Both typed and untyped should be gone
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("u1").Timeout(Timeout).FirstAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("u2").Timeout(Timeout).FirstAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("t1", userType).Timeout(Timeout).FirstAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("t2", userType).Timeout(Timeout).FirstAsync());
    }

    /// <summary>
    /// Verifies that InvalidateAll clears all items even when some entries are expired and filtered from GetAllKeys.
    /// </summary>
    /// <returns>A task to await.</returns>
    [Test]
    public async Task InvalidateAll_ShouldIgnore_ExpiredEntriesButStillClearAll()
    {
        SystemJsonSerializer serializer = new();
        await using SqliteBlobCache cache = new(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);

        // Arrange: one expired, one not
        await cache.Insert("live", [1], DateTimeOffset.Now.AddMinutes(5)).Timeout(Timeout).FirstAsync();
        await cache.Insert("expired", [2], DateTimeOffset.Now.AddMilliseconds(200)).Timeout(Timeout).FirstAsync();

        // wait for expiration
        await Task.Delay(300);

        var keysBefore = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();

        // live remains, expired filtered out by GetAllKeys � keysBefore may be 1
        await Assert.That(keysBefore).Count().IsLessThanOrEqualTo(1);

        // Act
        await cache.InvalidateAll().Timeout(Timeout).FirstAsync();

        // Assert
        var keysAfter = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();
        await Assert.That(keysAfter).IsEmpty();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("live").Timeout(Timeout).FirstAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("expired").Timeout(Timeout).FirstAsync());
    }
}
