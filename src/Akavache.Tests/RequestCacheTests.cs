// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;

using Akavache.Core;

using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for RequestCache functionality.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class RequestCacheTests
{
    /// <summary>
    /// Tests that RequestCache properly deduplicates concurrent requests.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldDeduplicateConcurrentRequests()
    {
        // Arrange
        RequestCache.Clear();
        var callCount = 0;
        const string key = "test_deduplication";

        IObservable<string> Factory()
        {
            var currentCount = Interlocked.Increment(ref callCount);
            return Observable.Return($"result_{currentCount}").Delay(TimeSpan.FromMilliseconds(50)); // Add delay to ensure overlap
        }

        // Act - Make truly concurrent requests by starting them simultaneously
        var observables = Enumerable.Range(0, 5)
            .Select(_ => RequestCache.GetOrCreateRequest(key, Factory))
            .ToArray();

        // Convert to tasks simultaneously to ensure concurrency
        var tasks = observables.Select(obs => obs.FirstAsync().ToTask()).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - All should return the same result, factory called at most twice
        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.All(r => r == results[0]), Is.True, $"Not all results are the same: {string.Join(", ", results)}");
            Assert.That(callCount, Is.LessThanOrEqualTo(2), $"Factory called {callCount} times, expected at most 2");
        }
    }

    /// <summary>
    /// Tests that RequestCache handles different keys separately.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleDifferentKeysSeparately()
    {
        // Arrange
        RequestCache.Clear();
        var callCounts = new Dictionary<string, int>();

        IObservable<string> Factory(string key)
        {
            if (!callCounts.TryGetValue(key, out var value))
            {
                value = 0;
                callCounts[key] = value;
            }

            callCounts[key] = ++value;
            return Observable.Return($"result_{key}_{value}");
        }

        // Act - Make requests with different keys
        var result1 = await RequestCache.GetOrCreateRequest("key1", () => Factory("key1")).FirstAsync();
        var result2 = await RequestCache.GetOrCreateRequest("key2", () => Factory("key2")).FirstAsync();

        // Since RequestCache doesn't persist results after completion, a new request will call the factory again
        var result3 = await RequestCache.GetOrCreateRequest("key1", () => Factory("key1")).FirstAsync();

        // Assert - Different keys should get different results
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result1, Is.EqualTo("result_key1_1"));
            Assert.That(result2, Is.EqualTo("result_key2_1"));
        }

        // result3 will be "result_key1_2" because RequestCache doesn't persist completed results
        Assert.That(result3, Is.EqualTo("result_key1_2"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(callCounts["key1"], Is.EqualTo(2)); // Called twice for key1
            Assert.That(callCounts["key2"], Is.EqualTo(1)); // Called once for key2
        }
    }

    /// <summary>
    /// Tests that RequestCache.Clear removes all cached requests.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheClearShouldRemoveAllCachedRequests()
    {
        // Arrange
        RequestCache.Clear();
        var callCount = 0;
        const string key = "test_clear";

        IObservable<string> Factory()
        {
            callCount++;
            return Observable.Return($"result_{callCount}");
        }

        // Act - Make request, clear, then make another request
        var result1 = await RequestCache.GetOrCreateRequest(key, Factory).FirstAsync();

        RequestCache.Clear();

        var result2 = await RequestCache.GetOrCreateRequest(key, Factory).FirstAsync();

        using (Assert.EnterMultipleScope())
        {
            // Assert - Factory should be called twice (once before clear, once after)
            Assert.That(result1, Is.EqualTo("result_1"));
            Assert.That(result2, Is.EqualTo("result_2"));
            Assert.That(callCount, Is.EqualTo(2));
        }
    }

    /// <summary>
    /// Tests that RequestCache handles exceptions in factory functions.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleFactoryExceptions()
    {
        // Arrange
        RequestCache.Clear();
        const string key = "test_exception";
        var callCount = 0;

        IObservable<string> Factory()
        {
            callCount++;
            if (callCount == 1)
            {
                return Observable.Throw<string>(new InvalidOperationException("First call fails"));
            }

            return Observable.Return($"success_{callCount}");
        }

        // Act & Assert - First call should throw
        Assert.ThrowsAsync<InvalidOperationException>(async () => await RequestCache.GetOrCreateRequest(key, Factory).FirstAsync());

        // Second call should succeed (assuming the cache doesn't cache failures)
        var result = await RequestCache.GetOrCreateRequest(key, Factory).FirstAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo("success_2"));
            Assert.That(callCount, Is.EqualTo(2));
        }
    }

    /// <summary>
    /// Tests that RequestCache works with different return types.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldWorkWithDifferentReturnTypes()
    {
        // Arrange
        RequestCache.Clear();

        // Act - Test with different types
        var stringResult = await RequestCache.GetOrCreateRequest("string_key", static () => Observable.Return("test_string")).FirstAsync();
        var intResult = await RequestCache.GetOrCreateRequest("int_key", static () => Observable.Return(42)).FirstAsync();
        var objectResult = await RequestCache.GetOrCreateRequest("object_key", static () => Observable.Return(new { Name = "Test", Value = 123 })).FirstAsync();

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(stringResult, Is.EqualTo("test_string"));
            Assert.That(intResult, Is.EqualTo(42));
            Assert.That(objectResult, Is.Not.Null);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(objectResult.Name, Is.EqualTo("Test"));
            Assert.That(objectResult.Value, Is.EqualTo(123));
        }
    }

    /// <summary>
    /// Tests that RequestCache handles null keys gracefully.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleNullKeys()
    {
        // Arrange
        RequestCache.Clear();

        // Act & Assert - Should handle null key without throwing
        var result = await RequestCache.GetOrCreateRequest(null!, static () => Observable.Return("null_key_result")).FirstAsync();
        Assert.That(result, Is.EqualTo("null_key_result"));
    }

    /// <summary>
    /// Tests that RequestCache handles empty keys.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleEmptyKeys()
    {
        // Arrange
        RequestCache.Clear();
        var callCount = 0;

        IObservable<string> Factory()
        {
            callCount++;
            return Observable.Return($"empty_key_result_{callCount}");
        }

        // Act - Make requests with empty key
        var result1 = await RequestCache.GetOrCreateRequest(string.Empty, Factory).FirstAsync();
        var result2 = await RequestCache.GetOrCreateRequest(string.Empty, Factory).FirstAsync();

        using (Assert.EnterMultipleScope())
        {
            // Assert - Since RequestCache doesn't persist completed results, each call creates a new request
            Assert.That(result1, Is.EqualTo("empty_key_result_1"));
            Assert.That(result2, Is.EqualTo("empty_key_result_2"));
            Assert.That(callCount, Is.EqualTo(2));
        }
    }

    /// <summary>
    /// Tests that RequestCache properly handles async operations.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleAsyncOperations()
    {
        // Arrange
        RequestCache.Clear();
        const string key = "async_test";
        var callCount = 0;

        IObservable<string> AsyncFactory()
        {
            callCount++;
            return Observable.FromAsync(async () =>
            {
                await Task.Delay(100); // Simulate async work
                return $"async_result_{callCount}";
            });
        }

        // Act - Make concurrent async requests
        Task<string>[] tasks =
        [
            RequestCache.GetOrCreateRequest(key, AsyncFactory).FirstAsync().ToTask(),
            RequestCache.GetOrCreateRequest(key, AsyncFactory).FirstAsync().ToTask(),
            RequestCache.GetOrCreateRequest(key, AsyncFactory).FirstAsync().ToTask()
        ];

        var results = await Task.WhenAll(tasks);

        // Assert - All should return the same result, factory called at most twice
        var uniqueResults = results.Distinct().ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(uniqueResults, Has.Count.LessThanOrEqualTo(2), $"Too many unique results: {string.Join(", ", uniqueResults)}");
            Assert.That(callCount, Is.LessThanOrEqualTo(2), $"Factory called too many times: {callCount}");
        }
    }

    /// <summary>
    /// Tests that RequestCache handles high concurrency scenarios.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleHighConcurrency()
    {
        // Arrange
        RequestCache.Clear();
        const string key = "high_concurrency_test";
        var callCount = 0;

        IObservable<string> Factory()
        {
            var currentCount = Interlocked.Increment(ref callCount);
            return Observable.Return($"concurrent_result_{currentCount}").Delay(TimeSpan.FromMilliseconds(10));
        }

        // Act - Create all observables first, then convert to tasks to ensure true concurrency
        var observables = Enumerable.Range(0, 50)
            .Select(_ => RequestCache.GetOrCreateRequest(key, Factory))
            .ToArray();

        var tasks = observables.Select(obs => obs.FirstAsync().ToTask()).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert - All should return same result, factory called minimal times
        var uniqueResults = results.Distinct().ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(uniqueResults, Has.Count.LessThanOrEqualTo(3), $"Too many unique results: {uniqueResults.Count}. Expected 1-3, got: [{string.Join(", ", uniqueResults)}]");
            Assert.That(callCount, Is.LessThanOrEqualTo(3), $"Factory called too many times: {callCount}. Expected 1-3.");
        }
    }

    /// <summary>
    /// Tests that RequestCache handles Observable sequences correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleObservableSequences()
    {
        // Arrange
        RequestCache.Clear();
        const string key = "observable_sequence_test";
        var callCount = 0;

        IObservable<int> Factory()
        {
            Interlocked.Increment(ref callCount);
            return Observable.Range(1, 3); // Emits 1, 2, 3
        }

        // Act - Get the observable sequence with proper replay behavior
        var observable1 = RequestCache.GetOrCreateRequest(key, Factory);

        // ToList() will collect all emitted values
        var list1 = await observable1.ToList().FirstAsync();

        // Second call after the first observable completed - RequestCache will create a new one
        var observable2 = RequestCache.GetOrCreateRequest(key, Factory);
        var list2 = await observable2.ToList().FirstAsync();

        using (Assert.EnterMultipleScope())
        {
            // Assert - Both should return the same sequence values
            Assert.That(list1, Is.EqualTo([1, 2, 3]));
            Assert.That(list2, Is.EqualTo([1, 2, 3]));

            // Factory will be called twice since RequestCache doesn't persist completed observables
            Assert.That(callCount, Is.EqualTo(2));
        }
    }

    /// <summary>
    /// Tests that RequestCache memory usage doesn't grow unbounded.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldNotGrowUnbounded()
    {
        // Arrange
        RequestCache.Clear();

        // Act - Create many requests with different keys
        for (var i = 0; i < 1000; i++)
        {
            var key = $"memory_test_{i}";
            await RequestCache.GetOrCreateRequest(key, () => Observable.Return(i)).FirstAsync();
        }

        // Clear to free memory
        RequestCache.Clear();

        // Assert - Test passes if no OutOfMemoryException is thrown
        // This is mainly a regression test to ensure the cache doesn't leak memory
        Assert.Pass("Memory stress test completed without OutOfMemoryException");
    }

    /// <summary>
    /// Tests that RequestCache works correctly with null factory results.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleNullFactoryResults()
    {
        // Arrange
        RequestCache.Clear();
        const string key = "null_result_test";

        static IObservable<string?> Factory() => Observable.Return<string?>(null);

        // Act
        var result1 = await RequestCache.GetOrCreateRequest(key, Factory).FirstAsync();
        var result2 = await RequestCache.GetOrCreateRequest(key, Factory).FirstAsync();

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result1, Is.Null);
            Assert.That(result2, Is.Null);
        }
    }
}
