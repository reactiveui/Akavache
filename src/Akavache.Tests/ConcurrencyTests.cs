// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Akavache.SystemTextJson;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for concurrent operations on InMemoryBlobCache.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class ConcurrencyTests
{
    /// <summary>
    /// Tests that concurrent InsertObject operations do not cause IndexOutOfRangeException.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InMemoryBlobCache_ConcurrentInsertObject_ShouldNotThrowIndexOutOfRangeException()
    {
        // Arrange
        using var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        const int threadCount = 10;
        const int operationsPerThread = 100;
        var exceptions = new ConcurrentBag<Exception>();
        var tasks = new List<Task>();

        // Act
        for (var t = 0; t < threadCount; t++)
        {
            var threadId = t;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < operationsPerThread; i++)
                    {
                        var key = $"key_{threadId}_{i}";
                        var value = new TestObject { Id = i, Name = $"Thread {threadId} Item {i}" };

                        // Perform concurrent InsertObject operations
                        await cache.InsertObject(key, value);

                        // Occasionally invalidate to trigger cleanup operations
                        if (i % 10 == 0)
                        {
                            await cache.InvalidateObject<TestObject>($"key_{threadId}_{i - 5}");
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
        if (!exceptions.IsEmpty)
        {
            throw new AggregateException("Unexpected exceptions occurred during concurrent operations", exceptions);
        }
    }

    /// <summary>
    /// Tests that high volume stress operations do not cause IndexOutOfRangeException.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InMemoryBlobCache_HighVolumeStressTest_ShouldNotThrowIndexOutOfRangeException()
    {
        // Arrange
        using var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        const int threadCount = 50;
        const int operationsPerThread = 500;
        var exceptions = new ConcurrentBag<Exception>();
        var tasks = new List<Task>();
        var random = new Random(42); // Fixed seed for reproducibility

        // Act - Create a high-stress scenario with mixed operations
        for (var t = 0; t < threadCount; t++)
        {
            var threadId = t;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < operationsPerThread; i++)
                    {
                        var key = $"stress_key_{threadId}_{i}";
                        var value = new TestObject { Id = i, Name = $"Stress Thread {threadId} Item {i}" };

                        // Mix of operations to stress the Dictionary and HashSet operations
                        switch (random.Next(0, 4))
                        {
                            case 0: // Insert
                                await cache.InsertObject(key, value);
                                break;
                            case 1: // Get (may trigger expiration cleanup)
                                try
                                {
                                    await cache.GetObject<TestObject>(key);
                                }
                                catch (KeyNotFoundException)
                                {
                                    // Expected if key doesn't exist
                                }

                                break;
                            case 2: // Invalidate
                                await cache.InvalidateObject<TestObject>(key);
                                break;
                            case 3: // Vacuum to trigger cleanup
                                await cache.Vacuum();
                                break;
                        }

                        // Add some randomized delays to create more realistic timing
                        if (random.Next(0, 20) == 0)
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

        if (indexOutOfRangeExceptions.Count > 0)
        {
            throw new AggregateException("IndexOutOfRangeExceptions occurred during stress test", indexOutOfRangeExceptions);
        }
    }

    private class TestObject
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
