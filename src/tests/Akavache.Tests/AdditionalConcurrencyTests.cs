// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// System first
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Additional concurrency stress tests.
/// </summary>
[Category("Concurrency")]
public class AdditionalConcurrencyTests
{
    /// <summary>
    /// Concurrent writes and reads should not corrupt entries.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InMemoryCache_ConcurrentWrites_DoNotCorrupt()
    {
        var serializer = new SystemJsonSerializer();
        await using var cache = new InMemoryBlobCache(serializer);
        var writeTasks = Enumerable.Range(0, 50)
            .Select(async i => await cache.InsertObject($"user_{i}", new Mocks.UserObject { Name = $"User{i}" }).FirstAsync());
        await Task.WhenAll(writeTasks);
        var readTasks = Enumerable.Range(0, 50)
            .Select(async i => await cache.GetObject<Mocks.UserObject>($"user_{i}").FirstAsync());
        var results = await Task.WhenAll(readTasks);
        await Assert.That(results.All(r => r.Name!.StartsWith("User", StringComparison.Ordinal))).IsTrue();
    }
}
