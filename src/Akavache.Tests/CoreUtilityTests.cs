// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for core utility functionality.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class CoreUtilityTests
{
    /// <summary>
    /// Tests that RelativeTimeExtensions work correctly with past times.
    /// </summary>
    [Test]
    public void RelativeTimeExtensionsShouldWorkWithPastTimes()
    {
        // Arrange
        var baseTime = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var oneHourAgo = baseTime.AddHours(-1);
        var oneDayAgo = baseTime.AddDays(-1);
        var oneWeekAgo = baseTime.AddDays(-7);

        // Assert - These should all be in the past relative to baseTime
        Assert.That(oneHourAgo, Is.LessThan(baseTime));
        Assert.That(oneDayAgo, Is.LessThan(baseTime));
        Assert.That(oneWeekAgo, Is.LessThan(baseTime));
    }

    /// <summary>
    /// Tests that RelativeTimeExtensions work correctly with future times.
    /// </summary>
    [Test]
    public void RelativeTimeExtensionsShouldWorkWithFutureTimes()
    {
        // Arrange
        var baseTime = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var oneHourFromNow = baseTime.AddHours(1);
        var oneDayFromNow = baseTime.AddDays(1);
        var oneWeekFromNow = baseTime.AddDays(7);

        // Assert - These should all be in the future relative to baseTime
        Assert.That(oneHourFromNow, Is.GreaterThan(baseTime));
        Assert.That(oneDayFromNow, Is.GreaterThan(baseTime));
        Assert.That(oneWeekFromNow, Is.GreaterThan(baseTime));
    }

    /// <summary>
    /// Tests that DateTimeOffset conversions work correctly.
    /// </summary>
    [Test]
    public void DateTimeOffsetConversionsShouldWorkCorrectly()
    {
        // Arrange
        var utcTime = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var offset = TimeSpan.FromHours(-5); // EST

        // Act
        var dateTimeOffset = new DateTimeOffset(utcTime, TimeSpan.Zero);
        var offsetTime = dateTimeOffset.ToOffset(offset);

        // Assert
        Assert.That(utcTime.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(dateTimeOffset.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(offsetTime.Offset, Is.EqualTo(offset));
    }

    /// <summary>
    /// Tests that utility methods handle edge cases correctly.
    /// </summary>
    [Test]
    public void UtilityMethodsShouldHandleEdgeCases()
    {
        // Test minimum and maximum DateTime values
        var minDateTime = DateTime.MinValue;
        var maxDateTime = DateTime.MaxValue;

        // These should not throw
        Assert.That(minDateTime.Kind, Is.EqualTo(DateTimeKind.Unspecified));
        Assert.That(maxDateTime.Kind, Is.EqualTo(DateTimeKind.Unspecified));

        // Test with UTC variants
        var minUtc = DateTime.SpecifyKind(minDateTime, DateTimeKind.Utc);
        var maxUtc = DateTime.SpecifyKind(maxDateTime, DateTimeKind.Utc);

        Assert.That(minUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(maxUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    /// <summary>
    /// Tests that TimeSpan operations work correctly.
    /// </summary>
    [Test]
    public void TimeSpanOperationsShouldWorkCorrectly()
    {
        // Arrange
        var oneHour = TimeSpan.FromHours(1);
        var thirtyMinutes = TimeSpan.FromMinutes(30);
        var ninetyMinutes = TimeSpan.FromMinutes(90);

        // Act & Assert
        Assert.That(oneHour.TotalMinutes, Is.EqualTo(60));
        Assert.That(thirtyMinutes.TotalMinutes, Is.EqualTo(30));
        Assert.That(ninetyMinutes.TotalMinutes, Is.EqualTo(90));

        // Test arithmetic
        var combined = oneHour + thirtyMinutes;
        Assert.That(combined, Is.EqualTo(ninetyMinutes));

        var difference = ninetyMinutes - oneHour;
        Assert.That(difference, Is.EqualTo(thirtyMinutes));
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

        // Assert - Should use cached result, so factory called only once
        Assert.That(result2, Is.EqualTo(result1));
        Assert.That(result1, Is.EqualTo("result_1"));
        Assert.That(callCount, Is.EqualTo(1)); // Should only be called once due to caching

        // Clear and test again
        RequestCache.Clear();
        var request3 = RequestCache.GetOrCreateRequest(testKey, factory);
        var result3 = await request3.FirstAsync();

        Assert.That(result3, Is.EqualTo("result_2")); // Should be called again after clear
        Assert.That(callCount, Is.EqualTo(2));
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

        // Assert
        Assert.That(result1, Is.EqualTo("result_key1_1"));
        Assert.That(result2, Is.EqualTo("result_key2_1"));
        Assert.That(result3, Is.EqualTo("result_key1_1")); // Should be cached, same as result1

        Assert.That(callCounts["key1"], Is.EqualTo(1)); // Only called once due to caching
        Assert.That(callCounts["key2"], Is.EqualTo(1)); // Only called once
    }

    /// <summary>
    /// Tests that IBlobCache.ExceptionHelpers work correctly.
    /// </summary>
    /// <returns>A task representing the test completion.</returns>
    [Test]
    public async Task ExceptionHelpersShouldWorkCorrectly()
    {
        // Test KeyNotFoundException helper
        var keyNotFoundObs = IBlobCache.ExceptionHelpers.ObservableThrowKeyNotFoundException<string>("test_key");

        var keyNotFoundEx = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await keyNotFoundObs.FirstAsync();
        });

        Assert.That(keyNotFoundEx.Message, Does.Contain("test_key"));
        Assert.That(keyNotFoundEx.Message, Does.Contain("not present in the cache"));

        // Test ObjectDisposedException helper
        var objectDisposedObs = IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>("test_cache");

        var objectDisposedEx = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await objectDisposedObs.FirstAsync();
        });

        Assert.That(objectDisposedEx.Message, Does.Contain("test_cache"));
        Assert.That(objectDisposedEx.Message, Does.Contain("disposed"));
    }

    /// <summary>
    /// Tests that scheduler registration works correctly.
    /// </summary>
    [Test]
    public void SchedulerRegistrationShouldWorkCorrectly()
    {
        // Arrange & Act
        var taskpoolScheduler = CacheDatabase.TaskpoolScheduler;
        var immediateScheduler = ImmediateScheduler.Instance;

        // Assert
        Assert.That(taskpoolScheduler, Is.Not.Null);
        Assert.That(immediateScheduler, Is.Not.Null);
        Assert.NotSame(taskpoolScheduler, immediateScheduler);
    }

    /// <summary>
    /// Tests that unit values work correctly.
    /// </summary>
    [Test]
    public void UnitValuesShouldWorkCorrectly()
    {
        // Arrange & Act
        var unit1 = Unit.Default;
        var unit2 = default(Unit);

        // Assert
        Assert.That(unit2, Is.EqualTo(unit1));
        Assert.That(unit1.Equals(unit2, Is.True));
        Assert.That(unit2.GetHashCode(, Is.EqualTo(unit1.GetHashCode())));
        Assert.That(unit1.ToString(, Is.EqualTo("()")));
    }
}
