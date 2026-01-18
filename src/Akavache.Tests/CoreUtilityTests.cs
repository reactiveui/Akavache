// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;

namespace Akavache.Tests;

/// <summary>
/// Tests for core utility functionality.
/// </summary>
[Category("Akavache")]
[NotInParallel]
public class CoreUtilityTests
{
    /// <summary>
    /// Tests that RelativeTimeExtensions work correctly with past times.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RelativeTimeExtensionsShouldWorkWithPastTimes()
    {
        // Arrange
        var baseTime = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var oneHourAgo = baseTime.AddHours(-1);
        var oneDayAgo = baseTime.AddDays(-1);
        var oneWeekAgo = baseTime.AddDays(-7);

        // Assert - These should all be in the past relative to baseTime
        using (Assert.Multiple())
        {
            await Assert.That(oneHourAgo).IsLessThan(baseTime);
            await Assert.That(oneDayAgo).IsLessThan(baseTime);
            await Assert.That(oneWeekAgo).IsLessThan(baseTime);
        }
    }

    /// <summary>
    /// Tests that RelativeTimeExtensions work correctly with future times.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RelativeTimeExtensionsShouldWorkWithFutureTimes()
    {
        // Arrange
        var baseTime = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var oneHourFromNow = baseTime.AddHours(1);
        var oneDayFromNow = baseTime.AddDays(1);
        var oneWeekFromNow = baseTime.AddDays(7);

        // Assert - These should all be in the future relative to baseTime
        using (Assert.Multiple())
        {
            await Assert.That(oneHourFromNow).IsGreaterThan(baseTime);
            await Assert.That(oneDayFromNow).IsGreaterThan(baseTime);
            await Assert.That(oneWeekFromNow).IsGreaterThan(baseTime);
        }
    }

    /// <summary>
    /// Tests that DateTimeOffset conversions work correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task DateTimeOffsetConversionsShouldWorkCorrectly()
    {
        // Arrange
        var utcTime = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var offset = TimeSpan.FromHours(-5); // EST

        // Act
        var dateTimeOffset = new DateTimeOffset(utcTime, TimeSpan.Zero);
        var offsetTime = dateTimeOffset.ToOffset(offset);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(utcTime.Kind).IsEqualTo(DateTimeKind.Utc);
            await Assert.That(dateTimeOffset.Offset).IsEqualTo(TimeSpan.Zero);
            await Assert.That(offsetTime.Offset).IsEqualTo(offset);
        }
    }

    /// <summary>
    /// Tests that utility methods handle edge cases correctly.
    /// </summary>
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

    /// <summary>
    /// Tests that TimeSpan operations work correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task TimeSpanOperationsShouldWorkCorrectly()
    {
        // Arrange
        var oneHour = TimeSpan.FromHours(1);
        var thirtyMinutes = TimeSpan.FromMinutes(30);
        var ninetyMinutes = TimeSpan.FromMinutes(90);

        using (Assert.Multiple())
        {
            // Act & Assert
            await Assert.That(oneHour.TotalMinutes).IsEqualTo(60);
            await Assert.That(thirtyMinutes.TotalMinutes).IsEqualTo(30);
            await Assert.That(ninetyMinutes.TotalMinutes).IsEqualTo(90);
        }

        // Test arithmetic
        var combined = oneHour + thirtyMinutes;
        await Assert.That(combined).IsEqualTo(ninetyMinutes);

        var difference = ninetyMinutes - oneHour;
        await Assert.That(difference).IsEqualTo(thirtyMinutes);
    }

    /// <summary>
    /// Tests that RequestCache functionality works correctly.
    /// </summary>
    /// <returns>A task representing the test completion.</returns>
    [Test]
    public async Task RequestCacheShouldWorkCorrectly()
    {
        // Arrange
        const string testKey = "test_request_key";
        var callCount = 0;

        // Function that increments call count
        Func<IObservable<string>> factory = () =>
        {
            callCount++;
            return Observable.Return($"result_{callCount}");
        };

        // Act - Call multiple times with same key
        var request1 = RequestCache.GetOrCreateRequest(testKey, factory);
        var request2 = RequestCache.GetOrCreateRequest(testKey, factory);

        var result1 = await request1.FirstAsync();
        var result2 = await request2.FirstAsync();

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
        var result3 = await request3.FirstAsync();

        using (Assert.Multiple())
        {
            await Assert.That(result3).IsEqualTo("result_2"); // Should be called again after clear
            await Assert.That(callCount).IsEqualTo(2);
        }
    }

    /// <summary>
    /// Tests that RequestCache handles different key types correctly.
    /// </summary>
    /// <returns>A task representing the test completion.</returns>
    [Test]
    public async Task RequestCacheShouldHandleDifferentKeys()
    {
        // Arrange
        var callCounts = new Dictionary<string, int>();

        Func<string, IObservable<string>> factory = key =>
        {
            if (!callCounts.ContainsKey(key))
            {
                callCounts[key] = 0;
            }

            callCounts[key]++;
            return Observable.Return($"result_{key}_{callCounts[key]}");
        };

        // Act - Use different keys
        var request1 = RequestCache.GetOrCreateRequest("key1", () => factory("key1"));
        var request2 = RequestCache.GetOrCreateRequest("key2", () => factory("key2"));
        var request3 = RequestCache.GetOrCreateRequest("key1", () => factory("key1")); // Same as first

        var result1 = await request1.FirstAsync();
        var result2 = await request2.FirstAsync();
        var result3 = await request3.FirstAsync();

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(result1).IsEqualTo("result_key1_1");
            await Assert.That(result2).IsEqualTo("result_key2_1");
            await Assert.That(result3).IsEqualTo("result_key1_1"); // Should be cached, same as result1

            await Assert.That(callCounts["key1"]).IsEqualTo(1); // Only called once due to caching
            await Assert.That(callCounts["key2"]).IsEqualTo(1); // Only called once
        }
    }

    /// <summary>
    /// Tests that IBlobCache.ExceptionHelpers work correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ExceptionHelpersShouldWorkCorrectly()
    {
        // Test KeyNotFoundException helper
        var keyNotFoundObs = IBlobCache.ExceptionHelpers.ObservableThrowKeyNotFoundException<string>("test_key");

        var keyNotFoundEx = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await keyNotFoundObs.FirstAsync();
        });

        await Assert.That(keyNotFoundEx.Message).Contains("test_key");
        await Assert.That(keyNotFoundEx.Message).Contains("not present in the cache");

        // Test ObjectDisposedException helper
        var objectDisposedObs = IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>("test_cache");

        var objectDisposedEx = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await objectDisposedObs.FirstAsync();
        });

        await Assert.That(objectDisposedEx.Message).Contains("test_cache");
        await Assert.That(objectDisposedEx.Message).Contains("disposed");
    }

    /// <summary>
    /// Tests that scheduler registration works correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SchedulerRegistrationShouldWorkCorrectly()
    {
        // Arrange & Act
        var taskpoolScheduler = CacheDatabase.TaskpoolScheduler;
        var immediateScheduler = ImmediateScheduler.Instance;

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(taskpoolScheduler).IsNotNull();
            await Assert.That(immediateScheduler).IsNotNull();
        }

        await Assert.That(immediateScheduler).IsNotEquivalentTo(taskpoolScheduler);
    }

    /// <summary>
    /// Tests that unit values work correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UnitValuesShouldWorkCorrectly()
    {
        // Arrange & Act
        var unit1 = Unit.Default;
        var unit2 = default(Unit);

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
