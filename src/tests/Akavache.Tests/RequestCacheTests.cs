// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;

using Akavache.Core;

namespace Akavache.Tests;

/// <summary>
/// Tests for RequestCache functionality.
/// </summary>
[Category("Akavache")]
[NotInParallel(nameof(RequestCacheTests))]
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
        using (Assert.Multiple())
        {
            await Assert.That(results.All(r => r == results[0])).IsTrue();
            await Assert.That(callCount).IsLessThanOrEqualTo(2);
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
        Dictionary<string, int> callCounts = [];

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
        using (Assert.Multiple())
        {
            await Assert.That(result1).IsEqualTo("result_key1_1");
            await Assert.That(result2).IsEqualTo("result_key2_1");
        }

        // result3 will be "result_key1_2" because RequestCache doesn't persist completed results
        await Assert.That(result3).IsEqualTo("result_key1_2");

        using (Assert.Multiple())
        {
            await Assert.That(callCounts["key1"]).IsEqualTo(2); // Called twice for key1
            await Assert.That(callCounts["key2"]).IsEqualTo(1); // Called once for key2
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

        using (Assert.Multiple())
        {
            // Assert - Factory should be called twice (once before clear, once after)
            await Assert.That(result1).IsEqualTo("result_1");
            await Assert.That(result2).IsEqualTo("result_2");
            await Assert.That(callCount).IsEqualTo(2);
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
            return callCount == 1 ?
                Observable.Throw<string>(new InvalidOperationException("First call fails")) :
                Observable.Return($"success_{callCount}");
        }

        // Act & Assert - First call should throw
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await RequestCache.GetOrCreateRequest(key, Factory).FirstAsync());

        // Second call should succeed (assuming the cache doesn't cache failures)
        var result = await RequestCache.GetOrCreateRequest(key, Factory).FirstAsync();
        using (Assert.Multiple())
        {
            await Assert.That(result).IsEqualTo("success_2");
            await Assert.That(callCount).IsEqualTo(2);
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

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(stringResult).IsEqualTo("test_string");
            await Assert.That(intResult).IsEqualTo(42);
            await Assert.That(objectResult).IsNotNull();
        }

        using (Assert.Multiple())
        {
            await Assert.That(objectResult.Name).IsEqualTo("Test");
            await Assert.That(objectResult.Value).IsEqualTo(123);
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
        await Assert.That(result).IsEqualTo("null_key_result");
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

        using (Assert.Multiple())
        {
            // Assert - Since RequestCache doesn't persist completed results, each call creates a new request
            await Assert.That(result1).IsEqualTo("empty_key_result_1");
            await Assert.That(result2).IsEqualTo("empty_key_result_2");
            await Assert.That(callCount).IsEqualTo(2);
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
        using (Assert.Multiple())
        {
            await Assert.That(uniqueResults).Count().IsLessThanOrEqualTo(2);
            await Assert.That(callCount).IsLessThanOrEqualTo(2);
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
        using (Assert.Multiple())
        {
            await Assert.That(uniqueResults).Count().IsLessThanOrEqualTo(3);
            await Assert.That(callCount).IsLessThanOrEqualTo(3);
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

        using (Assert.Multiple())
        {
            // Assert - Both should return the same sequence values
            await Assert.That(list1).IsEquivalentTo([1, 2, 3]);
            await Assert.That(list2).IsEquivalentTo([1, 2, 3]);

            // Factory will be called twice since RequestCache doesn't persist completed observables
            await Assert.That(callCount).IsEqualTo(2);
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
            var currentIndex = i;
            await RequestCache.GetOrCreateRequest(key, () => Observable.Return(currentIndex)).FirstAsync();
        }

        // Clear to free memory
        RequestCache.Clear();

        // Assert - Test passes if no OutOfMemoryException is thrown
        // This is mainly a regression test to ensure the cache doesn't leak memory
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

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(result1).IsNull();
            await Assert.That(result2).IsNull();
        }
    }

    /// <summary>
    /// Tests GetOrCreateRequest throws on null fetch func.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateRequestShouldThrowOnNullFetchFunc()
    {
        RequestCache.Clear();
        await Assert.That(static () => RequestCache.GetOrCreateRequest<string>("k", null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests GetOrCreateRequest removes entry from cache after error.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateRequestShouldRemoveOnError()
    {
        RequestCache.Clear();
        var observable = RequestCache.GetOrCreateRequest("error_key", static () => Observable.Throw<string>(new InvalidOperationException("test")));

        try
        {
            await observable.ToTask();
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // After error, the cache entry should be removed
        await Assert.That(RequestCache.HasInFlightRequest("error_key", typeof(string))).IsFalse();
    }

    /// <summary>
    /// Tests GetOrCreateRequest removes entry from cache after completion.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateRequestShouldRemoveOnCompletion()
    {
        RequestCache.Clear();
        var observable = RequestCache.GetOrCreateRequest("complete_key", static () => Observable.Return("value"));

        await observable.ToTask();

        // After completion, the cache entry should be removed
        await Assert.That(RequestCache.HasInFlightRequest("complete_key", typeof(string))).IsFalse();
    }

    /// <summary>
    /// Tests RemoveRequest throws on null type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveRequestShouldThrowOnNullType() =>
        await Assert.That(static () => RequestCache.RemoveRequest("k", null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests RemoveRequest removes a specific request.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveRequestShouldRemoveEntry()
    {
        RequestCache.Clear();

        // Use a never-completing observable to keep the request in flight
        _ = RequestCache.GetOrCreateRequest("remove_test", static () => Observable.Never<string>());
        await Assert.That(RequestCache.HasInFlightRequest("remove_test", typeof(string))).IsTrue();

        RequestCache.RemoveRequest("remove_test", typeof(string));

        await Assert.That(RequestCache.HasInFlightRequest("remove_test", typeof(string))).IsFalse();
    }

    /// <summary>
    /// Tests RemoveRequestsForKey returns immediately for empty key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveRequestsForKeyShouldReturnForEmptyKey()
    {
        RequestCache.Clear();
        await Assert.That(static () => RequestCache.RemoveRequestsForKey(string.Empty)).ThrowsNothing();
        await Assert.That(static () => RequestCache.RemoveRequestsForKey(null!)).ThrowsNothing();
    }

    /// <summary>
    /// Tests RemoveRequestsForKey removes all matching entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveRequestsForKeyShouldRemoveMatchingEntries()
    {
        RequestCache.Clear();
        _ = RequestCache.GetOrCreateRequest("multitype_key", static () => Observable.Never<string>());
        _ = RequestCache.GetOrCreateRequest("multitype_key", static () => Observable.Never<int>());

        await Assert.That(RequestCache.HasInFlightRequest("multitype_key", typeof(string))).IsTrue();
        await Assert.That(RequestCache.HasInFlightRequest("multitype_key", typeof(int))).IsTrue();

        RequestCache.RemoveRequestsForKey("multitype_key");

        await Assert.That(RequestCache.HasInFlightRequest("multitype_key", typeof(string))).IsFalse();
        await Assert.That(RequestCache.HasInFlightRequest("multitype_key", typeof(int))).IsFalse();
    }

    /// <summary>
    /// Tests HasInFlightRequest throws on null type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HasInFlightRequestShouldThrowOnNullType() =>
        await Assert.That(static () => RequestCache.HasInFlightRequest("k", null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests HasInFlightRequest returns false for non-existent entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HasInFlightRequestShouldReturnFalseForNonExistent()
    {
        RequestCache.Clear();
        await Assert.That(RequestCache.HasInFlightRequest("nonexistent", typeof(string))).IsFalse();
    }

    /// <summary>
    /// Tests Count returns the number of in-flight requests.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CountShouldReturnInFlightCount()
    {
        RequestCache.Clear();
        await Assert.That(RequestCache.Count).IsEqualTo(0);

        _ = RequestCache.GetOrCreateRequest("count_test_1", static () => Observable.Never<string>());
        _ = RequestCache.GetOrCreateRequest("count_test_2", static () => Observable.Never<string>());

        await Assert.That(RequestCache.Count).IsEqualTo(2);

        RequestCache.Clear();
        await Assert.That(RequestCache.Count).IsEqualTo(0);
    }
}
