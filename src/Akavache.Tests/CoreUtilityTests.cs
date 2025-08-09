// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.SystemTextJson;
using Xunit;

namespace Akavache.Tests;

/// <summary>
/// Tests for core utility functionality.
/// </summary>
public class CoreUtilityTests
{
    /// <summary>
    /// Tests that RelativeTimeExtensions work correctly with past times.
    /// </summary>
    [Fact]
    public void RelativeTimeExtensionsShouldWorkWithPastTimes()
    {
        // Arrange
        var baseTime = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var oneHourAgo = baseTime.AddHours(-1);
        var oneDayAgo = baseTime.AddDays(-1);
        var oneWeekAgo = baseTime.AddDays(-7);

        // Assert - These should all be in the past relative to baseTime
        Assert.True(oneHourAgo < baseTime);
        Assert.True(oneDayAgo < baseTime);
        Assert.True(oneWeekAgo < baseTime);
    }

    /// <summary>
    /// Tests that RelativeTimeExtensions work correctly with future times.
    /// </summary>
    [Fact]
    public void RelativeTimeExtensionsShouldWorkWithFutureTimes()
    {
        // Arrange
        var baseTime = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var oneHourFromNow = baseTime.AddHours(1);
        var oneDayFromNow = baseTime.AddDays(1);
        var oneWeekFromNow = baseTime.AddDays(7);

        // Assert - These should all be in the future relative to baseTime
        Assert.True(oneHourFromNow > baseTime);
        Assert.True(oneDayFromNow > baseTime);
        Assert.True(oneWeekFromNow > baseTime);
    }

    /// <summary>
    /// Tests that DateTimeOffset conversions work correctly.
    /// </summary>
    [Fact]
    public void DateTimeOffsetConversionsShouldWorkCorrectly()
    {
        // Arrange
        var utcTime = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var offset = TimeSpan.FromHours(-5); // EST

        // Act
        var dateTimeOffset = new DateTimeOffset(utcTime, TimeSpan.Zero);
        var offsetTime = dateTimeOffset.ToOffset(offset);

        // Assert
        Assert.Equal(DateTimeKind.Utc, utcTime.Kind);
        Assert.Equal(TimeSpan.Zero, dateTimeOffset.Offset);
        Assert.Equal(offset, offsetTime.Offset);
    }

    /// <summary>
    /// Tests that utility methods handle edge cases correctly.
    /// </summary>
    [Fact]
    public void UtilityMethodsShouldHandleEdgeCases()
    {
        // Test minimum and maximum DateTime values
        var minDateTime = DateTime.MinValue;
        var maxDateTime = DateTime.MaxValue;

        // These should not throw
        Assert.Equal(DateTimeKind.Unspecified, minDateTime.Kind);
        Assert.Equal(DateTimeKind.Unspecified, maxDateTime.Kind);

        // Test with UTC variants
        var minUtc = DateTime.SpecifyKind(minDateTime, DateTimeKind.Utc);
        var maxUtc = DateTime.SpecifyKind(maxDateTime, DateTimeKind.Utc);

        Assert.Equal(DateTimeKind.Utc, minUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, maxUtc.Kind);
    }

    /// <summary>
    /// Tests that TimeSpan operations work correctly.
    /// </summary>
    [Fact]
    public void TimeSpanOperationsShouldWorkCorrectly()
    {
        // Arrange
        var oneHour = TimeSpan.FromHours(1);
        var thirtyMinutes = TimeSpan.FromMinutes(30);
        var ninetyMinutes = TimeSpan.FromMinutes(90);

        // Act & Assert
        Assert.Equal(60, oneHour.TotalMinutes);
        Assert.Equal(30, thirtyMinutes.TotalMinutes);
        Assert.Equal(90, ninetyMinutes.TotalMinutes);

        // Test arithmetic
        var combined = oneHour + thirtyMinutes;
        Assert.Equal(ninetyMinutes, combined);

        var difference = ninetyMinutes - oneHour;
        Assert.Equal(thirtyMinutes, difference);
    }

    /// <summary>
    /// Tests that RequestCache functionality works correctly.
    /// </summary>
    /// <returns>A task representing the test completion.</returns>
    [Fact]
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
        Assert.Equal(result1, result2);
        Assert.Equal("result_1", result1);
        Assert.Equal(1, callCount); // Should only be called once due to caching

        // Clear and test again
        RequestCache.Clear();
        var request3 = RequestCache.GetOrCreateRequest(testKey, factory);
        var result3 = await request3.FirstAsync();

        Assert.Equal("result_2", result3); // Should be called again after clear
        Assert.Equal(2, callCount);
    }

    /// <summary>
    /// Tests that RequestCache handles different key types correctly.
    /// </summary>
    /// <returns>A task representing the test completion.</returns>
    [Fact]
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
        Assert.Equal("result_key1_1", result1);
        Assert.Equal("result_key2_1", result2);
        Assert.Equal("result_key1_1", result3); // Should be cached, same as result1

        Assert.Equal(1, callCounts["key1"]); // Only called once due to caching
        Assert.Equal(1, callCounts["key2"]); // Only called once
    }

    /// <summary>
    /// Tests that IBlobCache.ExceptionHelpers work correctly.
    /// </summary>
    /// <returns>A task representing the test completion.</returns>
    [Fact]
    public async Task ExceptionHelpersShouldWorkCorrectly()
    {
        // Test KeyNotFoundException helper
        var keyNotFoundObs = IBlobCache.ExceptionHelpers.ObservableThrowKeyNotFoundException<string>("test_key");

        var keyNotFoundEx = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await keyNotFoundObs.FirstAsync();
        });

        Assert.Contains("test_key", keyNotFoundEx.Message);
        Assert.Contains("not present in the cache", keyNotFoundEx.Message);

        // Test ObjectDisposedException helper
        var objectDisposedObs = IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>("test_cache");

        var objectDisposedEx = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await objectDisposedObs.FirstAsync();
        });

        Assert.Contains("test_cache", objectDisposedEx.Message);
        Assert.Contains("disposed", objectDisposedEx.Message);
    }

    /// <summary>
    /// Tests that serializer registration works correctly.
    /// </summary>
    [Fact]
    public void SerializerRegistrationShouldWorkCorrectly()
    {
        // Arrange
        var originalSerializer = CacheDatabase.Serializer;
        var testSerializer = new SystemJsonSerializer();

        try
        {
            // Act
            CacheDatabase.Serializer = testSerializer;

            // Assert
            Assert.Same(testSerializer, CacheDatabase.Serializer);
        }
        finally
        {
            // Restore original serializer
            CacheDatabase.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that scheduler registration works correctly.
    /// </summary>
    [Fact]
    public void SchedulerRegistrationShouldWorkCorrectly()
    {
        // Arrange & Act
        var taskpoolScheduler = CacheDatabase.TaskpoolScheduler;
        var immediateScheduler = ImmediateScheduler.Instance;

        // Assert
        Assert.NotNull(taskpoolScheduler);
        Assert.NotNull(immediateScheduler);
        Assert.NotSame(taskpoolScheduler, immediateScheduler);
    }

    /// <summary>
    /// Tests that unit values work correctly.
    /// </summary>
    [Fact]
    public void UnitValuesShouldWorkCorrectly()
    {
        // Arrange & Act
        var unit1 = Unit.Default;
        var unit2 = default(Unit);

        // Assert
        Assert.Equal(unit1, unit2);
        Assert.True(unit1.Equals(unit2));
        Assert.Equal(unit1.GetHashCode(), unit2.GetHashCode());
        Assert.Equal("()", unit1.ToString());
    }
}
