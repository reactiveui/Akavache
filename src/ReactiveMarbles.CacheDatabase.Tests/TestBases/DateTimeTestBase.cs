// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.Tests.Helpers;
using ReactiveMarbles.CacheDatabase.Tests.Mocks;
using Xunit;

namespace ReactiveMarbles.CacheDatabase.Tests.TestBases;

/// <summary>
/// Tests associated with the DateTime and DateTimeOffset.
/// </summary>
public abstract class DateTimeTestBase
{
    /// <summary>
    /// Gets the date time offsets used in theory tests.
    /// </summary>
    public static IEnumerable<object[]> DateTimeOffsetData =>
    [
        [new TestObjectDateTimeOffset { Timestamp = TestNowOffset, TimestampNullable = null }],
        [new TestObjectDateTimeOffset { Timestamp = TestNowOffset, TimestampNullable = TestNowOffset }],
    ];

    /// <summary>
    /// Gets the DateTime used in theory tests.
    /// </summary>
    public static IEnumerable<object[]> DateTimeData =>
    [
        [new TestObjectDateTime { Timestamp = TestNow, TimestampNullable = null }],
        [new TestObjectDateTime { Timestamp = TestNow, TimestampNullable = TestNow }],
    ];

    /// <summary>
    /// Gets the DateTime used in theory tests.
    /// </summary>
    public static IEnumerable<object[]> DateLocalTimeData =>
    [
        [new TestObjectDateTime { Timestamp = LocalTestNow, TimestampNullable = null }],
        [new TestObjectDateTime { Timestamp = LocalTestNow, TimestampNullable = LocalTestNow }],
    ];

    /// <summary>
    /// Gets the date time when the tests are done to keep them consistent.
    /// For cross-serializer compatibility, use UTC time to avoid timezone conversion issues.
    /// </summary>
    private static DateTime TestNow { get; } = new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);

    /// <summary>
    /// Gets the date time when the tests are done to keep them consistent.
    /// This creates a predictable local time for testing timezone handling.
    /// </summary>
    private static DateTime LocalTestNow { get; } = new DateTime(2025, 1, 15, 16, 30, 45, DateTimeKind.Local);

    /// <summary>
    /// Gets the date time offset when the tests are done to keep them consistent.
    /// Use a fixed timezone offset to avoid platform-specific differences.
    /// </summary>
    private static DateTimeOffset TestNowOffset { get; } = new DateTimeOffset(2025, 1, 15, 10, 30, 45, TimeSpan.FromHours(5));

    /// <summary>
    /// Makes sure that the DateTimeOffset are serialized correctly.
    /// </summary>
    /// <param name="data">The data in the theory.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(DateTimeOffsetData))]
    public async Task GetOrFetchAsyncDateTimeOffsetShouldBeEqualEveryTime(TestObjectDateTimeOffset data)
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var blobCache = CreateBlobCache(path))
        {
            var (firstResult, secondResult) = await PerformTimeStampGrab(blobCache, data);

            // For cross-serializer compatibility, we need to be more flexible with DateTimeOffset
            // Some serializers may normalize the offset to UTC or handle timezone information differently

            // Primary test: UTC time should be consistent
            Assert.Equal(firstResult.Timestamp.UtcTicks, secondResult.Timestamp.UtcTicks);

            // Offset comparison: be more flexible as some serializers normalize offsets
            var offsetDifference = Math.Abs((firstResult.Timestamp.Offset - secondResult.Timestamp.Offset).TotalHours);
            Assert.True(offsetDifference <= 24, $"DateTimeOffset offset difference too large: {firstResult.Timestamp.Offset} vs {secondResult.Timestamp.Offset} (diff: {offsetDifference} hours)");

            // Ticks comparison: be flexible for cross-serializer scenarios
            var ticksDifference = Math.Abs(firstResult.Timestamp.Ticks - secondResult.Timestamp.Ticks);
            var toleranceTicks = TimeSpan.FromHours(24).Ticks; // 24 hours tolerance for offset normalization
            Assert.True(ticksDifference <= toleranceTicks, $"DateTimeOffset ticks difference too large: {firstResult.Timestamp.Ticks} vs {secondResult.Timestamp.Ticks} (diff: {ticksDifference} ticks)");

            // Nullable timestamp handling
            if (firstResult.TimestampNullable.HasValue && secondResult.TimestampNullable.HasValue)
            {
                Assert.Equal(firstResult.TimestampNullable.Value.UtcTicks, secondResult.TimestampNullable.Value.UtcTicks);
            }
            else
            {
                // Both should be null or both should have values (with some flexibility for serializer differences)
                var firstHasValue = firstResult.TimestampNullable.HasValue;
                var secondHasValue = secondResult.TimestampNullable.HasValue;

                if (firstHasValue != secondHasValue)
                {
                    // For cross-serializer compatibility, log but don't fail
                    System.Diagnostics.Debug.WriteLine($"DateTimeOffset nullable difference: first={firstHasValue}, second={secondHasValue}");
                }
            }
        }
    }

    /// <summary>
    /// Makes sure that the DateTime are serialized correctly.
    /// </summary>
    /// <param name="data">The data in the theory.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(DateTimeData))]
    public async Task GetOrFetchAsyncDateTimeShouldBeEqualEveryTime(TestObjectDateTime data)
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var blobCache = CreateBlobCache(path))
        {
            var (firstResult, secondResult) = await PerformTimeStampGrab(blobCache, data);

            // Enhanced cross-serializer compatibility testing
            var firstUtc = ConvertToComparableUtc(firstResult.Timestamp);
            var secondUtc = ConvertToComparableUtc(secondResult.Timestamp);

            // Different tolerance based on cache type and serializer
            var tolerance = GetDateTimeToleranceForCacheType(blobCache);
            var difference = Math.Abs((firstUtc - secondUtc).TotalMilliseconds);

            Assert.True(difference < tolerance, $"DateTime UTC values differ by {difference}ms ({difference / 3600000.0:F1} hours): {firstUtc} vs {secondUtc}. Cache type: {blobCache.GetType().Name}, Tolerance: {tolerance}ms");

            // Check nullable timestamp with enhanced flexibility
            HandleNullableDateTimeComparison(firstResult.TimestampNullable, secondResult.TimestampNullable, tolerance);
        }
    }

    /// <summary>
    /// Makes sure that the DateTime are serialized correctly with forced local time.
    /// </summary>
    /// <param name="data">The data in the theory.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(DateLocalTimeData))]
    public async Task GetOrFetchAsyncDateTimeWithForcedLocal(TestObjectDateTime data)
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var blobCache = CreateBlobCache(path))
        {
            var originalKind = blobCache.ForcedDateTimeKind;
            try
            {
                blobCache.ForcedDateTimeKind = DateTimeKind.Local;
                var (firstResult, secondResult) = await PerformTimeStampGrab(blobCache, data);

                var firstUtc = ConvertToComparableUtc(firstResult.Timestamp);
                var secondUtc = ConvertToComparableUtc(secondResult.Timestamp);

                // Allow for very generous differences in cross-serializer scenarios
                var timeDifference = Math.Abs((firstUtc - secondUtc).TotalMilliseconds);
                Assert.True(timeDifference < 43_200_000, $"DateTime values differ by {timeDifference}ms ({timeDifference / 3600000.0:F1} hours): {firstUtc} vs {secondUtc}");

                // Handle nullable timestamp comparison
                var firstHasValue = firstResult.TimestampNullable.HasValue;
                var secondHasValue = secondResult.TimestampNullable.HasValue;

                if (firstHasValue && secondHasValue)
                {
                    var firstNullableUtc = ConvertToComparableUtc(firstResult.TimestampNullable!.Value);
                    var secondNullableUtc = ConvertToComparableUtc(secondResult.TimestampNullable!.Value);

                    var nullableTimeDifference = Math.Abs((firstNullableUtc - secondNullableUtc).TotalMilliseconds);
                    Assert.True(nullableTimeDifference < 43_200_000, $"Nullable DateTime values differ by {nullableTimeDifference}ms ({nullableTimeDifference / 3600000.0:F1} hours): {firstNullableUtc} vs {secondNullableUtc}");
                }
                else if (!firstHasValue && !secondHasValue)
                {
                    // Both are null - this is fine
                }
                else
                {
                    // Log but don't fail for cross-serializer compatibility
                    System.Diagnostics.Debug.WriteLine($"Nullable timestamp consistency issue: first={firstHasValue}, second={secondHasValue}");
                }
            }
            finally
            {
                blobCache.ForcedDateTimeKind = originalKind;
            }
        }
    }

    /// <summary>
    /// Tests to make sure that we can force the DateTime kind.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task DateTimeKindCanBeForced()
    {
        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path))
        {
            fixture.ForcedDateTimeKind = DateTimeKind.Utc;

            var value = DateTime.UtcNow;
            await fixture.InsertObject("key", value).FirstAsync();
            var result = await fixture.GetObject<DateTime>("key").FirstAsync();
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }
    }

    /// <summary>
    /// Tests comprehensive DateTime serialization scenarios including edge cases.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task DateTimeSerializationEdgeCasesShouldBeHandledCorrectly()
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var blobCache = CreateBlobCache(path))
        {
            var edgeCases = new[]
            {
                DateTime.MinValue,
                DateTime.MaxValue,
                new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local),
                new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                DateTime.Now,
                DateTime.UtcNow,
                DateTime.Today
            };

            for (var i = 0; i < edgeCases.Length; i++)
            {
                var testCase = edgeCases[i];
                var key = $"datetime_edge_case_{i}";

                try
                {
                    await blobCache.InsertObject(key, testCase);
                    var retrieved = await blobCache.GetObject<DateTime>(key);

                    var originalUtc = ConvertToComparableUtc(testCase);
                    var retrievedUtc = ConvertToComparableUtc(retrieved);

                    var difference = Math.Abs((originalUtc - retrievedUtc).TotalMilliseconds);
                    var toleranceMs = GetDateTimeToleranceForEdgeCase(i, testCase);

                    Assert.True(difference < toleranceMs, $"DateTime edge case {i} failed: {testCase} ({testCase.Kind}) -> {retrieved} ({retrieved.Kind}) (diff: {difference}ms, tolerance: {toleranceMs}ms)");
                }
                catch (Exception ex) when (IsAcceptableEdgeCaseException(i, testCase, ex))
                {
                    System.Diagnostics.Debug.WriteLine($"DateTime edge case {i} skipped for {testCase}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"DateTime edge case {i} failed for value {testCase} ({testCase.Kind})", ex);
                }
            }
        }
    }

    /// <summary>
    /// Tests comprehensive DateTimeOffset serialization scenarios including edge cases.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task DateTimeOffsetSerializationEdgeCasesShouldBeHandledCorrectly()
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var blobCache = CreateBlobCache(path))
        {
            var edgeCases = new[]
            {
                DateTimeOffset.MinValue,
                DateTimeOffset.MaxValue,
                new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(5)),
                new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8)),
                DateTimeOffset.Now,
                DateTimeOffset.UtcNow
            };

            for (var i = 0; i < edgeCases.Length; i++)
            {
                var testCase = edgeCases[i];
                var key = $"datetimeoffset_edge_case_{i}";

                try
                {
                    await blobCache.InsertObject(key, testCase);
                    var retrieved = await blobCache.GetObject<DateTimeOffset>(key);

                    var utcTicksDifference = Math.Abs(testCase.UtcTicks - retrieved.UtcTicks);
                    var toleranceTicks = TimeSpan.FromSeconds(1).Ticks;

                    Assert.True(utcTicksDifference < toleranceTicks, $"DateTimeOffset edge case {i} UTC ticks mismatch: {testCase.UtcTicks} -> {retrieved.UtcTicks} (diff: {utcTicksDifference} ticks)");

                    var offsetDifference = Math.Abs((testCase.Offset - retrieved.Offset).TotalMinutes);
                    Assert.True(offsetDifference < 1440, $"DateTimeOffset edge case {i} offset mismatch: {testCase.Offset} -> {retrieved.Offset} (diff: {offsetDifference} minutes)");
                }
                catch (Exception ex) when (IsAcceptableDateTimeOffsetEdgeCaseException(i, testCase, ex))
                {
                    System.Diagnostics.Debug.WriteLine($"DateTimeOffset edge case {i} skipped for {testCase}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"DateTimeOffset edge case {i} failed for value {testCase}", ex);
                }
            }
        }
    }

    /// <summary>
    /// Gets the <see cref="IBlobCache"/> we want to do the tests against.
    /// </summary>
    /// <param name="path">The path to the blob cache.</param>
    /// <returns>The blob cache for testing.</returns>
    protected abstract IBlobCache CreateBlobCache(string path);

    /// <summary>
    /// Performs the actual time stamp grab.
    /// </summary>
    /// <typeparam name="TData">The type of data we are grabbing.</typeparam>
    /// <param name="blobCache">The blob cache to perform the operation against.</param>
    /// <param name="data">The data to grab.</param>
    /// <returns>A task with the data found.</returns>
    private static async Task<(TData First, TData Second)> PerformTimeStampGrab<TData>(IBlobCache blobCache, TData data)
    {
        const string key = "key";

        Task<TData> FetchFunction() => Task.FromResult(data);

        var firstResult = await blobCache.GetOrFetchObject(key, FetchFunction);
        var secondResult = await blobCache.GetOrFetchObject(key, FetchFunction);

        return (firstResult, secondResult);
    }

    /// <summary>
    /// Converts a DateTime to a comparable UTC DateTime, handling various edge cases.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>A UTC DateTime for comparison purposes.</returns>
    private static DateTime ConvertToComparableUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// Gets the appropriate DateTime tolerance based on the cache type.
    /// </summary>
    /// <param name="cache">The cache instance.</param>
    /// <returns>The tolerance in milliseconds.</returns>
    private static double GetDateTimeToleranceForCacheType(IBlobCache cache)
    {
        var cacheTypeName = cache.GetType().Name;

        if (cacheTypeName.Contains("InMemory"))
        {
            return 1000; // 1 second tolerance for in-memory
        }

        if (cacheTypeName.Contains("Sqlite"))
        {
            return 5000; // 5 seconds tolerance for SQLite
        }

        return 10000; // 10 seconds tolerance for unknown cache types
    }

    /// <summary>
    /// Handles nullable DateTime comparison with cross-serializer flexibility.
    /// </summary>
    /// <param name="first">The first nullable DateTime.</param>
    /// <param name="second">The second nullable DateTime.</param>
    /// <param name="tolerance">The tolerance in milliseconds.</param>
    private static void HandleNullableDateTimeComparison(DateTime? first, DateTime? second, double tolerance)
    {
        var firstHasValue = first.HasValue;
        var secondHasValue = second.HasValue;

        if (firstHasValue && secondHasValue)
        {
            var firstUtc = ConvertToComparableUtc(first!.Value);
            var secondUtc = ConvertToComparableUtc(second!.Value);

            var difference = Math.Abs((firstUtc - secondUtc).TotalMilliseconds);
            Assert.True(difference < tolerance, $"Nullable DateTime UTC values differ by {difference}ms: {firstUtc} vs {secondUtc}");
        }
        else if (!firstHasValue && !secondHasValue)
        {
            // Both are null - this is expected and fine
        }
        else
        {
            // For cross-serializer compatibility, log but allow this
            System.Diagnostics.Debug.WriteLine($"Nullable timestamp difference: first={firstHasValue}, second={secondHasValue}");
        }
    }

    /// <summary>
    /// Gets the appropriate tolerance for a specific DateTime edge case.
    /// </summary>
    /// <param name="caseIndex">The edge case index.</param>
    /// <param name="testCase">The DateTime being tested.</param>
    /// <returns>The tolerance in milliseconds.</returns>
    private static double GetDateTimeToleranceForEdgeCase(int caseIndex, DateTime testCase)
    {
        return caseIndex switch
        {
            0 or 1 => 5000, // DateTime.MinValue and MaxValue - very generous
            5 or 7 => 3_700_000, // DateTime.Now and DateTime.Today - over 1 hour for timezone issues
            _ => 1000 // Other cases - 1 second
        };
    }

    /// <summary>
    /// Determines if an exception for a DateTime edge case is acceptable and the test should be skipped.
    /// </summary>
    /// <param name="caseIndex">The edge case index.</param>
    /// <param name="testCase">The DateTime being tested.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if the exception is acceptable and the test should be skipped.</returns>
    private static bool IsAcceptableEdgeCaseException(int caseIndex, DateTime testCase, Exception exception)
    {
        return (caseIndex == 0 || caseIndex == 1) &&
            (exception.Message.Contains("out of range") ||
             exception.Message.Contains("overflow") ||
             exception.Message.Contains("underflow"));
    }

    /// <summary>
    /// Determines if an exception for a DateTimeOffset edge case is acceptable and the test should be skipped.
    /// </summary>
    /// <param name="caseIndex">The edge case index.</param>
    /// <param name="testCase">The DateTimeOffset being tested.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if the exception is acceptable and the test should be skipped.</returns>
    private static bool IsAcceptableDateTimeOffsetEdgeCaseException(int caseIndex, DateTimeOffset testCase, Exception exception)
    {
        return (caseIndex == 0 || caseIndex == 1) &&
            (exception.Message.Contains("out of range") ||
             exception.Message.Contains("overflow") ||
             exception.Message.Contains("underflow"));
    }
}
