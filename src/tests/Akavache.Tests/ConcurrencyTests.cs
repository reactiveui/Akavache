// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Threading.Tasks;
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for concurrent operations on InMemoryBlobCache.
/// </summary>
[Category("Akavache")]
public sealed class ConcurrencyTests
{
    /// <summary>
    /// Tests that concurrent InsertObject operations do not cause IndexOutOfRangeException.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InMemoryBlobCache_ConcurrentInsertObject_ShouldNotThrowIndexOutOfRangeException()
    {
        // Arrange
        await using InMemoryBlobCache cache = new(new SystemJsonSerializer());
        const int threadCount = 10;
        const int operationsPerThread = 100;
        ConcurrentBag<Exception> exceptions = [];
        List<Task> tasks = [];

        // Act
        for (var t = 0; t < threadCount; t++)
        {
            var threadId = t;
            var localCache = cache;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < operationsPerThread; i++)
                    {
                        var key = $"key_{threadId}_{i}";
                        TestObject value = new() { Id = i, Name = $"Thread {threadId} Item {i}" };

                        // Perform concurrent InsertObject operations
                        await localCache.InsertObject(key, value);

                        // Occasionally invalidate to trigger cleanup operations
                        if (i % 10 == 0)
                        {
                            await localCache.InvalidateObject<TestObject>($"key_{threadId}_{i - 5}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert
        var indexOutOfRangeExceptions = exceptions
            .Where(ex => ex is IndexOutOfRangeException)
            .ToList();

        if (indexOutOfRangeExceptions.Count > 0)
        {
            throw new AggregateException("IndexOutOfRangeExceptions occurred during concurrent operations", indexOutOfRangeExceptions);
        }

        // Verify no other exceptions occurred
        if (exceptions.IsEmpty)
        {
            return;
        }

        throw new AggregateException("Unexpected exceptions occurred during concurrent operations", exceptions);
    }

    /// <summary>
    /// Tests that high volume stress operations do not cause IndexOutOfRangeException.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InMemoryBlobCache_HighVolumeStressTest_ShouldNotThrowIndexOutOfRangeException()
    {
        // Arrange
        await using InMemoryBlobCache cache = new(new SystemJsonSerializer());
        const int threadCount = 50;
        const int operationsPerThread = 500;
        ConcurrentBag<Exception> exceptions = [];
        List<Task> tasks = [];

        // Act - Create a high-stress scenario with mixed operations
        for (var t = 0; t < threadCount; t++)
        {
            var threadId = t;
            var localCache = cache;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < operationsPerThread; i++)
                    {
                        var key = $"stress_key_{threadId}_{i}";
                        TestObject value = new() { Id = i, Name = $"Stress Thread {threadId} Item {i}" };

                        // Mix of operations to stress the Dictionary and HashSet operations.
                        // Operation choice is derived deterministically from (threadId, i)
                        // — we want reproducible coverage of all four branches without using
                        // System.Random, which CA5394 forbids in non-test contexts and which
                        // adds shared mutable state across threads here.
                        switch ((threadId + i) % 4)
                        {
                            case 0:
                            {
                                await localCache.InsertObject(key, value);
                                break;
                            }

                            case 1:
                            {
                                await GetObjectIgnoringNotFound(localCache, key);
                                break;
                            }

                            case 2:
                            {
                                await localCache.InvalidateObject<TestObject>(key);
                                break;
                            }

                            case 3:
                            {
                                await localCache.Vacuum();
                                break;
                            }
                        }

                        // Add deterministic delays every 20 iterations to create more
                        // realistic timing without depending on System.Random.
                        if (i % 20 == 0)
                        {
                            await Task.Delay(1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert
        var indexOutOfRangeExceptions = exceptions
            .Where(ex => ex is IndexOutOfRangeException)
            .ToList();

        if (indexOutOfRangeExceptions.Count == 0)
        {
            return;
        }

        throw new AggregateException("IndexOutOfRangeExceptions occurred during stress test", indexOutOfRangeExceptions);
    }

    /// <summary>
    /// Concurrent writes followed by concurrent reads round-trip every entry intact.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InMemoryBlobCache_ConcurrentWritesShouldNotCorrupt()
    {
        await using InMemoryBlobCache cache = new(new SystemJsonSerializer());
        var localCache = cache;

        var writeTasks = Enumerable.Range(0, 50)
            .Select(i => localCache.InsertObject($"user_{i}", new TestObject { Id = i, Name = $"User{i}" }).FirstAsync().ToTask());
        await Task.WhenAll(writeTasks);

        var readTasks = Enumerable.Range(0, 50)
            .Select(i => localCache.GetObject<TestObject>($"user_{i}").FirstAsync().ToTask());
        var results = await Task.WhenAll(readTasks);

        await Assert.That(results.All(static r => r is { Name: { } name } && name.StartsWith("User", StringComparison.Ordinal))).IsTrue();
    }

    /// <summary>
    /// Attempts to retrieve an object from the cache using the specified key while ignoring any KeyNotFoundException.
    /// </summary>
    /// <param name="cache">The cache from which the object will be retrieved.</param>
    /// <param name="key">The key associated with the object in the cache.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task GetObjectIgnoringNotFound(IBlobCache cache, string key)
    {
        try
        {
            await cache.GetObject<TestObject>(key);
        }
        catch (KeyNotFoundException)
        {
            // Expected if key doesn't exist
        }
    }

    /// <summary>
    /// A simple object used for testing concurrent operations.
    /// </summary>
    private sealed class TestObject
    {
        /// <summary>
        /// Gets or sets the unique identifier for the test object.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name associated with the test object.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
