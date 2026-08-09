// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for core utility functionality.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class CoreUtilityTests
{
    /// <summary>Number of days in the week-sized offset used to build past and future sample times.</summary>
    private const int WeekSpanDays = 7;

    /// <summary>UTC offset of the Eastern Standard Time zone, used as a representative non-zero offset.</summary>
    private const int EasternStandardOffsetHours = -5;

    /// <summary>Minutes in one hour, the conversion factor the TimeSpan arithmetic assertions rely on.</summary>
    private const double MinutesPerHour = 60;

    /// <summary>Minutes in half an hour, the smaller operand of the TimeSpan arithmetic assertions.</summary>
    private const double HalfHourMinutes = 30;

    /// <summary>Minutes in an hour and a half, the expected sum of the two TimeSpan operands.</summary>
    private const double HourAndAHalfMinutes = MinutesPerHour + HalfHourMinutes;

    /// <summary>Number of factory invocations expected once the request cache has been cleared and the key re-requested.</summary>
    private const int FactoryCallsAcrossCacheClear = 2;

    /// <summary>Tests that RelativeTimeExtensions work correctly with past times.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RelativeTimeExtensionsShouldWorkWithPastTimes()
    {
        // Arrange
        DateTime baseTime = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var oneHourAgo = baseTime.AddHours(-1);
        var oneDayAgo = baseTime.AddDays(-1);
        var oneWeekAgo = baseTime.AddDays(-WeekSpanDays);

        // Assert - These should all be in the past relative to baseTime
        using (Assert.Multiple())
        {
            await Assert.That(oneHourAgo).IsLessThan(baseTime);
            await Assert.That(oneDayAgo).IsLessThan(baseTime);
            await Assert.That(oneWeekAgo).IsLessThan(baseTime);
        }
    }

    /// <summary>Tests that RelativeTimeExtensions work correctly with future times.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RelativeTimeExtensionsShouldWorkWithFutureTimes()
    {
        // Arrange
        DateTime baseTime = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var oneHourFromNow = baseTime.AddHours(1);
        var oneDayFromNow = baseTime.AddDays(1);
        var oneWeekFromNow = baseTime.AddDays(WeekSpanDays);

        // Assert - These should all be in the future relative to baseTime
        using (Assert.Multiple())
        {
            await Assert.That(oneHourFromNow).IsGreaterThan(baseTime);
            await Assert.That(oneDayFromNow).IsGreaterThan(baseTime);
            await Assert.That(oneWeekFromNow).IsGreaterThan(baseTime);
        }
    }

    /// <summary>Tests that DateTimeOffset conversions work correctly.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task DateTimeOffsetConversionsShouldWorkCorrectly()
    {
        // Arrange
        DateTime utcTime = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var offset = TimeSpan.FromHours(EasternStandardOffsetHours);

        // Act
        DateTimeOffset dateTimeOffset = new(utcTime, TimeSpan.Zero);
        var offsetTime = dateTimeOffset.ToOffset(offset);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(utcTime.Kind).IsEqualTo(DateTimeKind.Utc);
            await Assert.That(dateTimeOffset.Offset).IsEqualTo(TimeSpan.Zero);
            await Assert.That(offsetTime.Offset).IsEqualTo(offset);
        }
    }

    /// <summary>Tests that utility methods handle edge cases correctly.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UtilityMethodsShouldHandleEdgeCases()
    {
        // Test minimum and maximum DateTime values
        var minDateTime = DateTime.MinValue;
        var maxDateTime = DateTime.MaxValue;

        using (Assert.Multiple())
        {
            // These should not throw
            await Assert.That(minDateTime.Kind).IsEqualTo(DateTimeKind.Unspecified);
            await Assert.That(maxDateTime.Kind).IsEqualTo(DateTimeKind.Unspecified);
        }

        // Test with UTC variants
        var minUtc = DateTime.SpecifyKind(minDateTime, DateTimeKind.Utc);
        var maxUtc = DateTime.SpecifyKind(maxDateTime, DateTimeKind.Utc);

        using (Assert.Multiple())
        {
            await Assert.That(minUtc.Kind).IsEqualTo(DateTimeKind.Utc);
            await Assert.That(maxUtc.Kind).IsEqualTo(DateTimeKind.Utc);
        }
    }

    /// <summary>Tests that TimeSpan operations work correctly.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task TimeSpanOperationsShouldWorkCorrectly()
    {
        // Arrange
        var oneHour = TimeSpan.FromHours(1);
        var thirtyMinutes = TimeSpan.FromMinutes(HalfHourMinutes);
        var ninetyMinutes = TimeSpan.FromMinutes(HourAndAHalfMinutes);

        using (Assert.Multiple())
        {
            // Act & Assert
            await Assert.That(oneHour.TotalMinutes).IsEqualTo(MinutesPerHour);
            await Assert.That(thirtyMinutes.TotalMinutes).IsEqualTo(HalfHourMinutes);
            await Assert.That(ninetyMinutes.TotalMinutes).IsEqualTo(HourAndAHalfMinutes);
        }

        await Assert.That(oneHour + thirtyMinutes).IsEqualTo(ninetyMinutes);
        await Assert.That(ninetyMinutes - oneHour).IsEqualTo(thirtyMinutes);
    }

    /// <summary>Tests that RequestCache functionality works correctly.</summary>
    /// <returns>A task representing the test completion.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task RequestCacheShouldWorkCorrectly()
    {
        // Arrange
        const string testKey = "test_request_key";
        var callCount = 0;

        // Function that increments call count
        Func<IObservable<string>> factory = () =>
        {
            callCount++;
            return Signal.Return($"result_{callCount}");
        };

        // Act - Call multiple times with same key
        var request1 = RequestCache.GetOrCreateRequest(testKey, factory);
        var request2 = RequestCache.GetOrCreateRequest(testKey, factory);

        var result1 = request1.SubscribeGetValue();
        var result2 = request2.SubscribeGetValue();

        using (Assert.Multiple())
        {
            // Assert - Should use cached result, so factory called only once
            await Assert.That(result2).IsEqualTo(result1);
            await Assert.That(result1).IsEqualTo("result_1");
            await Assert.That(callCount).IsEqualTo(1); // Should only be called once due to caching
        }

        // Clear and test again
        RequestCache.Clear();
        var request3 = RequestCache.GetOrCreateRequest(testKey, factory);

        var result3 = request3.SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(result3).IsEqualTo("result_2"); // Should be called again after clear
            await Assert.That(callCount).IsEqualTo(FactoryCallsAcrossCacheClear);
        }
    }

    /// <summary>Tests that RequestCache handles different key types correctly.</summary>
    /// <returns>A task representing the test completion.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task RequestCacheShouldHandleDifferentKeys()
    {
        // Arrange
        Dictionary<string, int> callCounts = [];

        // Act - Use different keys
        var request1 = RequestCache.GetOrCreateRequest("key1", () => Factory("key1"));
        var request2 = RequestCache.GetOrCreateRequest("key2", () => Factory("key2"));
        var request3 = RequestCache.GetOrCreateRequest("key1", () => Factory("key1")); // Same as first

        var result1 = request1.SubscribeGetValue();
        var result2 = request2.SubscribeGetValue();
        var result3 = request3.SubscribeGetValue();

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(result1).IsEqualTo("result_key1_1");
            await Assert.That(result2).IsEqualTo("result_key2_1");
            await Assert.That(result3).IsEqualTo("result_key1_1"); // Should be cached, same as result1

            await Assert.That(callCounts["key1"]).IsEqualTo(1); // Only called once due to caching
            await Assert.That(callCounts["key2"]).IsEqualTo(1); // Only called once
        }

        IObservable<string> Factory(string key)
        {
            ref var callCount = ref CollectionsMarshal.GetValueRefOrAddDefault(callCounts, key, out _);
            callCount++;
            return Signal.Return($"result_{key}_{callCount}");
        }
    }

    /// <summary>Tests that IBlobCache.ExceptionHelpers work correctly.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ExceptionHelpersShouldWorkCorrectly()
    {
        // Test KeyNotFoundException helper
        var keyNotFoundObs = IBlobCache.ExceptionHelpers.ObservableThrowKeyNotFoundException<string>("test_key");

        var keyNotFoundError = keyNotFoundObs.SubscribeGetError();
        await Assert.That(keyNotFoundError).IsTypeOf<KeyNotFoundException>();

        var keyNotFoundEx = (KeyNotFoundException)keyNotFoundError!;
        await Assert.That(keyNotFoundEx.Message).Contains("test_key");
        await Assert.That(keyNotFoundEx.Message).Contains("not present in the cache");

        // Test ObjectDisposedException helper
        var objectDisposedObs = IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>("test_cache");

        var objectDisposedError = objectDisposedObs.SubscribeGetError();
        await Assert.That(objectDisposedError).IsTypeOf<ObjectDisposedException>();

        var objectDisposedEx = (ObjectDisposedException)objectDisposedError!;
        await Assert.That(objectDisposedEx.Message).Contains("test_cache");
        await Assert.That(objectDisposedEx.Message).Contains("disposed");
    }

    /// <summary>Tests that scheduler registration works correctly.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SchedulerRegistrationShouldWorkCorrectly()
    {
        // Arrange & Act
        var taskpoolScheduler = CacheDatabase.TaskpoolScheduler;
        var immediateScheduler = ImmediateSequencer.Instance;

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(taskpoolScheduler).IsNotNull();
            await Assert.That(immediateScheduler).IsNotNull();
        }

        await Assert.That(immediateScheduler).IsNotEquivalentTo(taskpoolScheduler);
    }

    /// <summary>Tests that unit values work correctly.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UnitValuesShouldWorkCorrectly()
    {
        // Arrange & Act
        var unit1 = RxVoid.Default;
        var unit2 = default(RxVoid);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(unit2).IsEqualTo(unit1);
            await Assert.That(unit1).IsEqualTo(unit2);
        }

        using (Assert.Multiple())
        {
            await Assert.That(unit2.GetHashCode()).IsEqualTo(unit1.GetHashCode());
            await Assert.That(unit1.ToString()).IsEqualTo("()");
        }
    }
}
