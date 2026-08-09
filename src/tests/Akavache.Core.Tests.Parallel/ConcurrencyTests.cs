// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for concurrent operations on InMemoryBlobCache.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public sealed class ConcurrencyTests
{
    /// <summary>How often, in iterations, the insert loop interleaves an invalidation to trigger cleanup.</summary>
    private const int InvalidateEveryNthOperation = 10;

    /// <summary>How many iterations back the invalidation reaches, so it targets an already-inserted key.</summary>
    private const int InvalidationLookbackOffset = 5;

    /// <summary>Number of distinct cache operations the stress loop cycles through.</summary>
    private const int OperationVariantCount = 4;

    /// <summary>Stress operation that inserts the object.</summary>
    private const int InsertVariant = 0;

    /// <summary>Stress operation that reads the object back.</summary>
    private const int GetVariant = 1;

    /// <summary>Stress operation that invalidates the object.</summary>
    private const int InvalidateVariant = 2;

    /// <summary>Stress operation that vacuums the cache.</summary>
    private const int VacuumVariant = 3;

    /// <summary>How often, in iterations, the stress loop yields so the threads interleave realistically.</summary>
    private const int DelayEveryNthOperation = 20;

    /// <summary>How many entries the concurrent write/read round-trip test stores.</summary>
    private const int ConcurrentWriteCount = 50;

    /// <summary>Tests that concurrent InsertObject operations do not cause IndexOutOfRangeException.</summary>
    /// <returns>A task representing the test.</returns>
    /// <exception cref="AggregateException"></exception>
    [Test]
    public async Task InMemoryBlobCache_ConcurrentInsertObject_ShouldNotThrowIndexOutOfRangeException()
    {
        // Arrange
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
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
                        if (i % InvalidateEveryNthOperation == 0)
                        {
                            await localCache.InvalidateObject<TestObject>($"key_{threadId}_{i - InvalidationLookbackOffset}");
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
            .Where(static ex => ex is IndexOutOfRangeException)
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

    /// <summary>Tests that high volume stress operations do not cause IndexOutOfRangeException.</summary>
    /// <returns>A task representing the test.</returns>
    /// <exception cref="AggregateException"></exception>
    [Test]
    public async Task InMemoryBlobCache_HighVolumeStressTest_ShouldNotThrowIndexOutOfRangeException()
    {
        // Arrange
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
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

                        await RunStressOperation(localCache, (threadId + i) % OperationVariantCount, key, value);

                        // Add deterministic delays every 20 iterations to create more
                        // realistic timing without depending on System.Random.
                        if (i % DelayEveryNthOperation == 0)
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
            .Where(static ex => ex is IndexOutOfRangeException)
            .ToList();

        if (indexOutOfRangeExceptions.Count == 0)
        {
            return;
        }

        throw new AggregateException("IndexOutOfRangeExceptions occurred during stress test", indexOutOfRangeExceptions);
    }

    /// <summary>Concurrent writes followed by concurrent reads round-trip every entry intact.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InMemoryBlobCache_ConcurrentWritesShouldNotCorrupt()
    {
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        var localCache = cache;

        var writeTasks = Enumerable.Range(0, ConcurrentWriteCount)
            .Select(i =>
            {
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = localCache.InsertObject($"user_{i}", new TestObject { Id = i, Name = $"User{i}" })
                    .Subscribe(_ => tcs.SetResult(), tcs.SetException);
                return tcs.Task;
            });
        await Task.WhenAll(writeTasks);

        List<TestObject?> results = [];
        foreach (var i in Enumerable.Range(0, ConcurrentWriteCount))
        {
            var obj = await localCache.GetObject<TestObject>($"user_{i}");
            results.Add(obj);
        }

        await Assert.That(results.TrueForAll(static r => r is { Name: { } name } && name.StartsWith("User", StringComparison.Ordinal))).IsTrue();
    }

    /// <summary>
    /// Runs one of the four cache operations the stress test cycles through. The variant is derived
    /// deterministically from the thread index and iteration so all branches get reproducible coverage
    /// without a shared <see cref="Random"/> instance across threads.
    /// </summary>
    /// <param name="cache">The cache under stress.</param>
    /// <param name="variant">Which operation to run; one of the <c>*Variant</c> constants.</param>
    /// <param name="key">The key the operation acts on.</param>
    /// <param name="value">The value to insert when the variant is an insert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task RunStressOperation(InMemoryBlobCache cache, int variant, string key, TestObject value)
    {
        switch (variant)
        {
            case InsertVariant:
            {
                await cache.InsertObject(key, value);
                break;
            }

            case GetVariant:
            {
                await GetObjectIgnoringNotFound(cache, key);
                break;
            }

            case InvalidateVariant:
            {
                await cache.InvalidateObject<TestObject>(key);
                break;
            }

            case VacuumVariant:
            {
                await cache.Vacuum();
                break;
            }
        }
    }

    /// <summary>Attempts to retrieve an object from the cache using the specified key while ignoring any KeyNotFoundException.</summary>
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

    /// <summary>A simple object used for testing concurrent operations.</summary>
    private sealed class TestObject
    {
        /// <summary>Gets or sets the unique identifier for the test object.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the name associated with the test object.</summary>
        public string Name { get; set; } = string.Empty;
    }
}
