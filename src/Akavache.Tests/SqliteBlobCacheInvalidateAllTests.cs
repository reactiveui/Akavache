// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests focused on SqliteBlobCache.InvalidateAll behavior.
/// </summary>
[NonParallelizable]
[TestFixture]
[Category("Akavache")]
public class SqliteBlobCacheInvalidateAllTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Verifies that InvalidateAll removes all untyped items and they cannot be retrieved afterwards.
    /// </summary>
    /// <returns>A task to await.</returns>
    [Test]
    public async Task InvalidateAll_ShouldRemove_AllItems()
    {
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        await using (var cache = new SqliteBlobCache(Path.Combine(path, "invalidateall-basic.db"), serializer))
        {
            // Arrange
            await cache.Insert("a", [1]).Timeout(Timeout).FirstAsync();
            await cache.Insert("b", [2]).Timeout(Timeout).FirstAsync();
            await cache.Insert("c", [3]).Timeout(Timeout).FirstAsync();

            var keysBefore = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();
            Assert.That(keysBefore, Has.Count.EqualTo(3));

            // Act
            await cache.InvalidateAll().Timeout(Timeout).FirstAsync();

            // Assert
            var keysAfter = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();
            Assert.That(keysAfter, Is.Empty);

            Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("a").Timeout(Timeout).FirstAsync());
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("b").Timeout(Timeout).FirstAsync());
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("c").Timeout(Timeout).FirstAsync());
        }
    }

    /// <summary>
    /// Verifies that InvalidateAll removes both typed and untyped items.
    /// </summary>
    /// <returns>A task to await.</returns>
    [Test]
    public async Task InvalidateAll_ShouldRemove_TypedAndUntypedItems()
    {
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        await using (var cache = new SqliteBlobCache(Path.Combine(path, "invalidateall-mixed.db"), serializer))
        {
            // Arrange: mix typed and untyped entries
            await cache.Insert("u1", [1]).Timeout(Timeout).FirstAsync();
            await cache.Insert("u2", [2]).Timeout(Timeout).FirstAsync();

            var userType = typeof(string);
            await cache.Insert("t1", [10], userType).Timeout(Timeout).FirstAsync();
            await cache.Insert("t2", [20], userType).Timeout(Timeout).FirstAsync();

            var keysBefore = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();
            Assert.That(keysBefore, Has.Count.EqualTo(4));

            // Act
            await cache.InvalidateAll().Timeout(Timeout).FirstAsync();

            // Assert
            var keysAfter = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();
            Assert.That(keysAfter, Is.Empty);

            // Both typed and untyped should be gone
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("u1").Timeout(Timeout).FirstAsync());
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("u2").Timeout(Timeout).FirstAsync());
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("t1", userType).Timeout(Timeout).FirstAsync());
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("t2", userType).Timeout(Timeout).FirstAsync());
        }
    }

    /// <summary>
    /// Verifies that InvalidateAll clears all items even when some entries are expired and filtered from GetAllKeys.
    /// </summary>
    /// <returns>A task to await.</returns>
    [Test]
    public async Task InvalidateAll_ShouldIgnore_ExpiredEntriesButStillClearAll()
    {
        var serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        await using (var cache = new SqliteBlobCache(Path.Combine(path, "invalidateall-expired.db"), serializer))
        {
            // Arrange: one expired, one not
            await cache.Insert("live", [1], DateTimeOffset.Now.AddMinutes(5)).Timeout(Timeout).FirstAsync();
            await cache.Insert("expired", [2], DateTimeOffset.Now.AddMilliseconds(200)).Timeout(Timeout).FirstAsync();

            // wait for expiration
            await Task.Delay(300);

            var keysBefore = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();

            // live remains, expired filtered out by GetAllKeys — keysBefore may be 1
            Assert.That(keysBefore, Has.Count.LessThanOrEqualTo(1));

            // Act
            await cache.InvalidateAll().Timeout(Timeout).FirstAsync();

            // Assert
            var keysAfter = await cache.GetAllKeys().ToList().Timeout(Timeout).FirstAsync();
            Assert.That(keysAfter, Is.Empty);

            Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("live").Timeout(Timeout).FirstAsync());
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("expired").Timeout(Timeout).FirstAsync());
        }
    }
}
