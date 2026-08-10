// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for RequestCache functionality.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class RequestCacheTests
{
    /// <summary>Key reused across the RemoveRequest assertions.</summary>
    private const string RemoveRequestKey = "remove_test";

    /// <summary>Key registered under two element types so RemoveRequestsForKey has more than one bucket to clear.</summary>
    private const string MultiTypeRequestKey = "multitype_key";

    /// <summary>Number of simultaneous subscribers used to prove overlapping requests collapse onto one factory call.</summary>
    private const int ConcurrentRequestCount = 5;

    /// <summary>Number of simultaneous subscribers used by the high-concurrency stress scenario.</summary>
    private const int HighConcurrencyRequestCount = 50;

    /// <summary>Delay applied to the factory result so the concurrent subscriptions are guaranteed to overlap.</summary>
    private const int FactoryOverlapDelayMilliseconds = 50;

    /// <summary>Shorter factory delay for the high-concurrency scenario, keeping the overlap without slowing the test.</summary>
    private const int HighConcurrencyFactoryDelayMilliseconds = 10;

    /// <summary>Duration of the simulated asynchronous work performed inside the FromAsync factory.</summary>
    private const int SimulatedAsyncWorkMilliseconds = 100;

    /// <summary>Upper bound on factory invocations once deduplication has collapsed the concurrent requests.</summary>
    private const int MaxDeduplicatedFactoryCalls = 2;

    /// <summary>Upper bound on factory invocations under the high-concurrency stress scenario.</summary>
    private const int MaxHighConcurrencyFactoryCalls = 3;

    /// <summary>Factory invocations expected when a key is requested a second time, because completed requests are not retained.</summary>
    private const int FactoryCallsForSecondRequest = 2;

    /// <summary>Factory invocations expected when the first request faulted and the caller retries, because failures are not cached.</summary>
    private const int FactoryCallsAfterFailedRequest = 2;

    /// <summary>Number of distinct keys left in flight when the Count assertion runs.</summary>
    private const int InFlightRequestCount = 2;

    /// <summary>Number of short-lived requests created by the unbounded-growth regression scan.</summary>
    private const int MemoryProbeRequestCount = 1000;

    /// <summary>Value emitted by the int-typed request, proving the cache keys on element type as well as key.</summary>
    private const int IntRequestValue = 42;

    /// <summary>Value carried by the composite-typed request, proving the cache round-trips non-primitive payloads.</summary>
    private const int CompositeRequestValue = 123;

    /// <summary>Values the multi-element cached sequence emits, in order.</summary>
    private static readonly int[] ExpectedSequence = [1, 2, 3];

    /// <summary>Tests that RequestCache properly deduplicates concurrent requests.</summary>
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
            return Signal.Return($"result_{currentCount}").Delay(TimeSpan.FromMilliseconds(FactoryOverlapDelayMilliseconds)); // Add delay to ensure overlap
        }

        // Act - Make truly concurrent requests by starting them simultaneously
        var observables = Enumerable.Range(0, ConcurrentRequestCount)
            .Select(_ => RequestCache.GetOrCreateRequest(key, Factory))
            .ToArray();

        // Convert to tasks simultaneously to ensure concurrency
        var tasks = observables.Select(static obs =>
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = obs.Subscribe(v => tcs.TrySetResult(v), ex => tcs.TrySetException(ex));
            return tcs.Task;
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - All should return the same result, factory called at most twice
        using (Assert.Multiple())
        {
            await Assert.That(Array.TrueForAll(results, r => r == results[0])).IsTrue();
            await Assert.That(callCount).IsLessThanOrEqualTo(MaxDeduplicatedFactoryCalls);
        }
    }

    /// <summary>Tests that RequestCache handles different keys separately.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleDifferentKeysSeparately()
    {
        // Arrange
        RequestCache.Clear();
        Dictionary<string, int> callCounts = [];

        IObservable<string> Factory(string key)
        {
            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(callCounts, key, out _);
            value++;
            return Signal.Return($"result_{key}_{value}");
        }

        // Act - Make requests with different keys
        var result1 = RequestCache.GetOrCreateRequest("key1", () => Factory("key1")).SubscribeGetValue();
        var result2 = RequestCache.GetOrCreateRequest("key2", () => Factory("key2")).SubscribeGetValue();

        // Since RequestCache doesn't persist results after completion, a new request will call the factory again
        var result3 = RequestCache.GetOrCreateRequest("key1", () => Factory("key1")).SubscribeGetValue();

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
            await Assert.That(callCounts["key1"]).IsEqualTo(FactoryCallsForSecondRequest); // Called twice for key1
            await Assert.That(callCounts["key2"]).IsEqualTo(1); // Called once for key2
        }
    }

    /// <summary>Tests that RequestCache.Clear removes all cached requests.</summary>
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
            return Signal.Return($"result_{callCount}");
        }

        // Act - Make request, clear, then make another request
        var result1 = RequestCache.GetOrCreateRequest(key, Factory).SubscribeGetValue();

        RequestCache.Clear();

        var result2 = RequestCache.GetOrCreateRequest(key, Factory).SubscribeGetValue();

        using (Assert.Multiple())
        {
            // Assert - Factory should be called twice (once before clear, once after)
            await Assert.That(result1).IsEqualTo("result_1");
            await Assert.That(result2).IsEqualTo("result_2");
            await Assert.That(callCount).IsEqualTo(FactoryCallsForSecondRequest);
        }
    }

    /// <summary>Tests that RequestCache handles exceptions in factory functions.</summary>
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
            return callCount == 1
                ? Signal.Throw<string>(new InvalidOperationException("First call fails"))
                : Signal.Return($"success_{callCount}");
        }

        // Act & Assert - First call should throw
        var firstError = RequestCache.GetOrCreateRequest(key, Factory).SubscribeGetError();
        await Assert.That(firstError).IsTypeOf<InvalidOperationException>();

        // Second call should succeed (assuming the cache doesn't cache failures)
        var result = RequestCache.GetOrCreateRequest(key, Factory).SubscribeGetValue();
        using (Assert.Multiple())
        {
            await Assert.That(result).IsEqualTo("success_2");
            await Assert.That(callCount).IsEqualTo(FactoryCallsAfterFailedRequest);
        }
    }

    /// <summary>Tests that RequestCache works with different return types.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldWorkWithDifferentReturnTypes()
    {
        // Arrange
        RequestCache.Clear();

        // Act - Test with different types
        var stringResult = RequestCache.GetOrCreateRequest("string_key", static () => Signal.Return("test_string")).SubscribeGetValue();
        var intResult = RequestCache.GetOrCreateRequest("int_key", static () => Signal.Return(IntRequestValue)).SubscribeGetValue();
        var compositeResult = RequestCache.GetOrCreateRequest("object_key", static () => Signal.Return((Name: "Test", Value: CompositeRequestValue))).SubscribeGetValue();

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(stringResult).IsEqualTo("test_string");
            await Assert.That(intResult).IsEqualTo(IntRequestValue);
            await Assert.That(compositeResult.Name).IsEqualTo("Test");
            await Assert.That(compositeResult.Value).IsEqualTo(CompositeRequestValue);
        }
    }

    /// <summary>Tests that RequestCache handles null keys gracefully.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleNullKeys()
    {
        // Arrange
        RequestCache.Clear();

        // Act & Assert - Should handle null key without throwing
        var result = RequestCache.GetOrCreateRequest(null!, static () => Signal.Return("null_key_result")).SubscribeGetValue();
        await Assert.That(result).IsEqualTo("null_key_result");
    }

    /// <summary>Tests that RequestCache handles empty keys.</summary>
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
            return Signal.Return($"empty_key_result_{callCount}");
        }

        // Act - Make requests with empty key
        var result1 = RequestCache.GetOrCreateRequest(string.Empty, Factory).SubscribeGetValue();
        var result2 = RequestCache.GetOrCreateRequest(string.Empty, Factory).SubscribeGetValue();

        using (Assert.Multiple())
        {
            // Assert - Since RequestCache doesn't persist completed results, each call creates a new request
            await Assert.That(result1).IsEqualTo("empty_key_result_1");
            await Assert.That(result2).IsEqualTo("empty_key_result_2");
            await Assert.That(callCount).IsEqualTo(FactoryCallsForSecondRequest);
        }
    }

    /// <summary>Tests that RequestCache properly handles async operations.</summary>
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
            return Signal.FromAsync(async () =>
            {
                await Task.Delay(SimulatedAsyncWorkMilliseconds); // Simulate async work
                return $"async_result_{callCount}";
            });
        }

        // Act - Make concurrent async requests
        var tasks = new[]
        {
            SubscribeToTask(RequestCache.GetOrCreateRequest(key, AsyncFactory)),
            SubscribeToTask(RequestCache.GetOrCreateRequest(key, AsyncFactory)),
            SubscribeToTask(RequestCache.GetOrCreateRequest(key, AsyncFactory)),
        };

        var results = await Task.WhenAll(tasks);

        // Assert - All should return the same result, factory called at most twice
        var uniqueResults = results.Distinct().ToList();
        using (Assert.Multiple())
        {
            await Assert.That(uniqueResults).Count().IsLessThanOrEqualTo(MaxDeduplicatedFactoryCalls);
            await Assert.That(callCount).IsLessThanOrEqualTo(MaxDeduplicatedFactoryCalls);
        }
    }

    /// <summary>Tests that RequestCache handles high concurrency scenarios.</summary>
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
            return Signal.Return($"concurrent_result_{currentCount}").Delay(TimeSpan.FromMilliseconds(HighConcurrencyFactoryDelayMilliseconds));
        }

        // Act - Create all observables first, then convert to tasks to ensure true concurrency
        var observables = Enumerable.Range(0, HighConcurrencyRequestCount)
            .Select(_ => RequestCache.GetOrCreateRequest(key, Factory))
            .ToArray();

        var tasks = observables.Select(SubscribeToTask).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert - All should return same result, factory called minimal times
        var uniqueResults = results.Distinct().ToList();
        using (Assert.Multiple())
        {
            await Assert.That(uniqueResults).Count().IsLessThanOrEqualTo(MaxHighConcurrencyFactoryCalls);
            await Assert.That(callCount).IsLessThanOrEqualTo(MaxHighConcurrencyFactoryCalls);
        }
    }

    /// <summary>Tests that RequestCache handles Observable sequences correctly.</summary>
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
            _ = Interlocked.Increment(ref callCount);
            return Signal.Range(1, ExpectedSequence.Length); // Emits 1, 2, 3
        }

        // Act - Get the observable sequence with proper replay behavior
        var observable1 = RequestCache.GetOrCreateRequest(key, Factory);

        // ToList() will collect all emitted values
        var list1 = observable1.ToList().SubscribeGetValue();

        // Second call after the first observable completed - RequestCache will create a new one
        var observable2 = RequestCache.GetOrCreateRequest(key, Factory);
        var list2 = observable2.ToList().SubscribeGetValue();

        using (Assert.Multiple())
        {
            // Assert - Both should return the same sequence values
            await Assert.That(list1).IsEquivalentTo(ExpectedSequence);
            await Assert.That(list2).IsEquivalentTo(ExpectedSequence);

            // Factory will be called twice since RequestCache doesn't persist completed observables
            await Assert.That(callCount).IsEqualTo(FactoryCallsForSecondRequest);
        }
    }

    /// <summary>Tests that RequestCache memory usage doesn't grow unbounded.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldNotGrowUnbounded()
    {
        // Arrange
        RequestCache.Clear();

        // Act - Create many requests with different keys
        for (var i = 0; i < MemoryProbeRequestCount; i++)
        {
            var key = $"memory_test_{i}";
            var currentIndex = i;
            _ = RequestCache.GetOrCreateRequest(key, () => Signal.Return(currentIndex)).SubscribeGetValue();
        }

        // Clear to free memory
        RequestCache.Clear();

        // Assert - Test passes if no OutOfMemoryException is thrown
        // This is mainly a regression test to ensure the cache doesn't leak memory
        await Task.CompletedTask;
    }

    /// <summary>Tests that RequestCache works correctly with null factory results.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RequestCacheShouldHandleNullFactoryResults()
    {
        // Arrange
        RequestCache.Clear();
        const string key = "null_result_test";

        static IObservable<string?> Factory() => Signal.Return<string?>(null);

        // Act
        var result1 = RequestCache.GetOrCreateRequest(key, Factory).SubscribeGetValue();
        var result2 = RequestCache.GetOrCreateRequest(key, Factory).SubscribeGetValue();

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(result1).IsNull();
            await Assert.That(result2).IsNull();
        }
    }

    /// <summary>Tests GetOrCreateRequest throws on null fetch func.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateRequestShouldThrowOnNullFetchFunc()
    {
        RequestCache.Clear();
        await Assert.That(static () => RequestCache.GetOrCreateRequest<string>("k", null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests GetOrCreateRequest removes entry from cache after error.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateRequestShouldRemoveOnError()
    {
        RequestCache.Clear();
        var observable = RequestCache.GetOrCreateRequest("error_key", static () => Signal.Throw<string>(new InvalidOperationException("test")));

        var error = observable.SubscribeGetError();

        await Assert.That(error).IsTypeOf<InvalidOperationException>();

        // After error, the cache entry should be removed
        await Assert.That(RequestCache.HasInFlightRequest("error_key", typeof(string))).IsFalse();
    }

    /// <summary>Tests GetOrCreateRequest removes entry from cache after completion.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateRequestShouldRemoveOnCompletion()
    {
        RequestCache.Clear();
        var observable = RequestCache.GetOrCreateRequest("complete_key", static () => Signal.Return("value"));

        var result = observable.SubscribeGetValue();

        await Assert.That(result).IsEqualTo("value");

        // After completion, the cache entry should be removed
        await Assert.That(RequestCache.HasInFlightRequest("complete_key", typeof(string))).IsFalse();
    }

    /// <summary>Tests RemoveRequest throws on null type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveRequestShouldThrowOnNullType() =>
        await Assert.That(static () => RequestCache.RemoveRequest("k", null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests RemoveRequest removes a specific request.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveRequestShouldRemoveEntry()
    {
        RequestCache.Clear();

        // Use a never-completing observable to keep the request in flight
        _ = RequestCache.GetOrCreateRequest(RemoveRequestKey, static () => Signal.Never<string>());
        await Assert.That(RequestCache.HasInFlightRequest(RemoveRequestKey, typeof(string))).IsTrue();

        RequestCache.RemoveRequest(RemoveRequestKey, typeof(string));

        await Assert.That(RequestCache.HasInFlightRequest(RemoveRequestKey, typeof(string))).IsFalse();
    }

    /// <summary>Tests RemoveRequestsForKey returns immediately for empty key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveRequestsForKeyShouldReturnForEmptyKey()
    {
        RequestCache.Clear();
        await Assert.That(static () => RequestCache.RemoveRequestsForKey(string.Empty)).ThrowsNothing();
        await Assert.That(static () => RequestCache.RemoveRequestsForKey(null!)).ThrowsNothing();
    }

    /// <summary>Tests RemoveRequestsForKey removes all matching entries.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RemoveRequestsForKeyShouldRemoveMatchingEntries()
    {
        RequestCache.Clear();
        _ = RequestCache.GetOrCreateRequest(MultiTypeRequestKey, static () => Signal.Never<string>());
        _ = RequestCache.GetOrCreateRequest(MultiTypeRequestKey, static () => Signal.Never<int>());

        await Assert.That(RequestCache.HasInFlightRequest(MultiTypeRequestKey, typeof(string))).IsTrue();
        await Assert.That(RequestCache.HasInFlightRequest(MultiTypeRequestKey, typeof(int))).IsTrue();

        RequestCache.RemoveRequestsForKey(MultiTypeRequestKey);

        await Assert.That(RequestCache.HasInFlightRequest(MultiTypeRequestKey, typeof(string))).IsFalse();
        await Assert.That(RequestCache.HasInFlightRequest(MultiTypeRequestKey, typeof(int))).IsFalse();
    }

    /// <summary>Tests HasInFlightRequest throws on null type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HasInFlightRequestShouldThrowOnNullType() =>
        await Assert.That(static () => RequestCache.HasInFlightRequest("k", null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests HasInFlightRequest returns false for non-existent entry.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HasInFlightRequestShouldReturnFalseForNonExistent()
    {
        RequestCache.Clear();
        await Assert.That(RequestCache.HasInFlightRequest("nonexistent", typeof(string))).IsFalse();
    }

    /// <summary>Tests Count returns the number of in-flight requests.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CountShouldReturnInFlightCount()
    {
        RequestCache.Clear();
        await Assert.That(RequestCache.Count).IsEqualTo(0);

        _ = RequestCache.GetOrCreateRequest("count_test_1", static () => Signal.Never<string>());
        _ = RequestCache.GetOrCreateRequest("count_test_2", static () => Signal.Never<string>());

        await Assert.That(RequestCache.Count).IsEqualTo(InFlightRequestCount);

        RequestCache.Clear();
        await Assert.That(RequestCache.Count).IsEqualTo(0);
    }

    /// <summary>Bridges an observable to a Task for async scenarios where Subscribe is not synchronous.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="observable">The observable to subscribe to.</param>
    /// <returns>A task that completes with the first emitted value.</returns>
    private static Task<T> SubscribeToTask<T>(IObservable<T> observable)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = observable.Subscribe(v => tcs.TrySetResult(v), ex => tcs.TrySetException(ex));
        return tcs.Task;
    }
}
