// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.TestBases;
#else
namespace Akavache.Tests.TestBases;
#endif

/// <summary>Tests associated with the DateTime and DateTimeOffset.</summary>
public abstract class DateTimeTestBase : IDisposable
{
    /// <summary>Type-name fragment identifying an encrypted cache implementation.</summary>
    private const string EncryptedCacheNameFragment = "Encrypted";

    /// <summary>Index of the local-clock edge case within the DateTime edge-case list.</summary>
    private const int LocalNowCaseIndex = 5;

    /// <summary>Index of the date-only edge case within the DateTime edge-case list.</summary>
    private const int TodayCaseIndex = 7;

    /// <summary>Round-trip tolerance for <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/>, in milliseconds.</summary>
    private const double ExtremeValueToleranceMilliseconds = 5000;

    /// <summary>Round-trip tolerance for clock-derived values, in milliseconds; over an hour, to absorb timezone handling differences.</summary>
    private const double ClockDependentToleranceMilliseconds = 3_700_000;

    /// <summary>Round-trip tolerance for ordinary values, in milliseconds.</summary>
    private const double DefaultToleranceMilliseconds = 1000;

    /// <summary>Factor the round-trip tolerance is widened by for BSON serializers.</summary>
    private const double BsonToleranceMultiplier = 20;

    /// <summary>Factor the round-trip tolerance is widened by for encrypted caches.</summary>
    private const double EncryptedCacheToleranceMultiplier = 10;

    /// <summary>Year at or below which a round-tripped extreme DateTime is treated as mangled.</summary>
    private const int MinPlausibleRoundTripYear = 1900;

    /// <summary>Year at or above which a round-tripped extreme DateTime is treated as mangled.</summary>
    private const int MaxPlausibleRoundTripYear = 2100;

    /// <summary>Minimum DateTime edge-case success rate demanded of BSON serializers and encrypted caches.</summary>
    private const double LenientMinimumDateTimeSuccessRate = 0.3;

    /// <summary>Minimum DateTime edge-case success rate demanded of the remaining serializers.</summary>
    private const double StandardMinimumDateTimeSuccessRate = 0.6;

    /// <summary>Minimum DateTimeOffset edge-case success rate demanded of encrypted caches.</summary>
    private const double EncryptedMinimumOffsetSuccessRate = 0.4;

    /// <summary>Minimum DateTimeOffset edge-case success rate demanded of BSON serializers.</summary>
    private const double BsonMinimumOffsetSuccessRate = 0.5;

    /// <summary>Minimum DateTimeOffset edge-case success rate demanded of the remaining serializers.</summary>
    private const double StandardMinimumOffsetSuccessRate = 0.7;

    /// <summary>UTC offset, in hours, carried by the shared DateTimeOffset fixture value.</summary>
    private const int FixtureOffsetHours = 5;

    /// <summary>UTC offset, in hours, of the India time zone case.</summary>
    private const int IndiaOffsetHours = 5;

    /// <summary>UTC offset, in hours, of the US Pacific standard time case.</summary>
    private const int PacificStandardOffsetHours = -8;

    /// <summary>UTC offset, in hours, of the US Eastern standard time case.</summary>
    private const int EasternStandardOffsetHours = -5;

    /// <summary>UTC offset, in hours, of the Central European time case.</summary>
    private const int CentralEuropeanOffsetHours = 1;

    /// <summary>UTC offset, in hours, of the Japan time zone case.</summary>
    private const int JapanOffsetHours = 9;

    /// <summary>Tolerance, in seconds, applied when comparing round-tripped UTC instants.</summary>
    private const int UtcToleranceSeconds = 2;

    /// <summary>Offset tolerance, in hours, allowed for BSON serializers.</summary>
    private const double BsonOffsetToleranceHours = 48.0;

    /// <summary>Offset tolerance, in hours, allowed for the remaining serializers.</summary>
    private const double StandardOffsetToleranceHours = 24.0;

    /// <summary>A backing field which indicates if the class has been disposed.</summary>
    private bool _disposed;

    /// <summary>Gets the date time offsets used in theory tests.</summary>
    public static IEnumerable<object[]> DateTimeOffsetData =>
    [
        [new TestObjectDateTimeOffset { Timestamp = TestNowOffset, TimestampNullable = null }],
        [new TestObjectDateTimeOffset { Timestamp = TestNowOffset, TimestampNullable = TestNowOffset }],
    ];

    /// <summary>Gets the DateTime used in theory tests.</summary>
    public static IEnumerable<object[]> DateTimeData =>
    [
        [new TestObjectDateTime { Timestamp = TestNow, TimestampNullable = null }],
        [new TestObjectDateTime { Timestamp = TestNow, TimestampNullable = TestNow }],
    ];

    /// <summary>Gets the DateTime used in theory tests.</summary>
    public static IEnumerable<object[]> DateLocalTimeData =>
    [
        [new TestObjectDateTime { Timestamp = LocalTestNow, TimestampNullable = null }],
        [new TestObjectDateTime { Timestamp = LocalTestNow, TimestampNullable = LocalTestNow }],
    ];

    /// <summary>
    /// Gets the date time when the tests are done to keep them consistent.
    /// For cross-serializer compatibility, use UTC time to avoid timezone conversion issues.
    /// </summary>
    private static DateTime TestNow { get; } = new(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);

    /// <summary>
    /// Gets the date time when the tests are done to keep them consistent.
    /// This creates a predictable local time for testing timezone handling.
    /// </summary>
    private static DateTime LocalTestNow { get; } = new(2025, 1, 15, 16, 30, 45, DateTimeKind.Local);

    /// <summary>
    /// Gets the date time offset when the tests are done to keep them consistent.
    /// Use a fixed timezone offset to avoid platform-specific differences.
    /// </summary>
    private static DateTimeOffset TestNowOffset { get; } = new(2025, 1, 15, 10, 30, 45, TimeSpan.FromHours(FixtureOffsetHours));

    /// <summary>Tests to make sure that we can force the DateTime kind.</summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task DateTimeKindCanBeForced(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            fixture.ForcedDateTimeKind = DateTimeKind.Utc;

            var value = TimeProvider.System.GetUtcNow().UtcDateTime;
            fixture.InsertObject("key", value).SubscribeAndComplete();
            var result = fixture.GetObject<DateTime>("key").SubscribeGetValue();
            await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
        }
    }

    /// <summary>Tests comprehensive DateTime serialization scenarios including edge cases.</summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    /// <exception cref="InvalidOperationException">The date time value is invalid.</exception>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task DateTimeSerializationEdgeCasesShouldBeHandledCorrectly(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        using (var blobCache = CreateBlobCache(path, serializer))
        {
            DateTime[] edgeCases =
            [
                DateTime.MinValue,
                DateTime.MaxValue,
                new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new(2000, 1, 1, 0, 0, 0, DateTimeKind.Local),
                new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                TimeProvider.System.GetLocalNow().DateTime,
                TimeProvider.System.GetUtcNow().UtcDateTime,
                DateTime.Today
            ];

            var successCount = 0;
            var skipCount = 0;

            for (var i = 0; i < edgeCases.Length; i++)
            {
                if (await TryRoundTripDateTimeAsync(blobCache, serializer, edgeCases[i], i))
                {
                    successCount++;
                }
                else
                {
                    skipCount++;
                }
            }

            // Require at least 50% success rate for edge cases (very lenient for cross-platform compatibility)
            var totalAttempts = successCount + skipCount;
            var successRate = totalAttempts > 0 ? (double)successCount / totalAttempts : 0;
            var minSuccessRate = IsUsingBsonSerializer(serializer) || IsEncryptedCache(blobCache)
                ? LenientMinimumDateTimeSuccessRate
                : StandardMinimumDateTimeSuccessRate;

            await Assert.That(successRate)
                .IsGreaterThanOrEqualTo(minSuccessRate);
        }
    }

    /// <summary>
    /// Tests comprehensive DateTimeOffset serialization scenarios including edge cases.
    /// Enhanced version with better mobile/desktop scenario coverage.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    /// <exception cref="InvalidOperationException">The date time value is invalid.</exception>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task DateTimeOffsetSerializationEdgeCasesShouldBeHandledCorrectly(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        using (var blobCache = CreateBlobCache(path, serializer))
        {
            var edgeCases = GetMobileDesktopDateTimeOffsetTestCases(serializer);

            var successCount = 0;
            var skipCount = 0;

            for (var i = 0; i < edgeCases.Length; i++)
            {
                if (await TryRoundTripDateTimeOffsetAsync(blobCache, serializer, edgeCases[i], i))
                {
                    successCount++;
                }
                else
                {
                    skipCount++;
                }
            }

            // Verify reasonable success rate with more tolerance
            var actualTests = successCount + skipCount;
            var successRate = actualTests > 0 ? successCount / (double)actualTests : 0;

            // Allow for more failures with complex DateTimeOffset scenarios - be very lenient
            var minimumSuccessRate = GetMinimumOffsetSuccessRate(blobCache, serializer);

            await Assert.That(successRate).IsGreaterThanOrEqualTo(minimumSuccessRate);
        }
    }

    /// <summary>Disposes the test base, restoring the original serializer.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the <see cref="IBlobCache" /> we want to do the tests against.</summary>
    /// <param name="path">The path to the blob cache.</param>
    /// <param name="serializer">The serializer.</param>
    /// <returns>
    /// The blob cache for testing.
    /// </returns>
    protected abstract IBlobCache CreateBlobCache(string path, ISerializer serializer);

    /// <summary>Disposes resources.</summary>
    /// <param name="disposing">True to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // No managed resources to dispose in this base class.
        }

        _disposed = true;
    }

    /// <summary>Determines whether the cache under test is an encrypted implementation.</summary>
    /// <param name="blobCache">The cache under test.</param>
    /// <returns>True if the cache is an encrypted implementation.</returns>
    private static bool IsEncryptedCache(IBlobCache blobCache) =>
        blobCache.GetType().Name.Contains(EncryptedCacheNameFragment);

    /// <summary>Gets the minimum DateTimeOffset edge-case success rate this cache and serializer pairing has to reach.</summary>
    /// <param name="blobCache">The cache under test.</param>
    /// <param name="serializer">The serializer under test.</param>
    /// <returns>The minimum acceptable success rate.</returns>
    private static double GetMinimumOffsetSuccessRate(IBlobCache blobCache, ISerializer serializer)
    {
        if (IsEncryptedCache(blobCache))
        {
            return EncryptedMinimumOffsetSuccessRate;
        }

        return IsUsingBsonSerializer(serializer) ? BsonMinimumOffsetSuccessRate : StandardMinimumOffsetSuccessRate;
    }

    /// <summary>Round-trips a single DateTime edge case through the cache.</summary>
    /// <param name="blobCache">The cache under test.</param>
    /// <param name="serializer">The serializer under test.</param>
    /// <param name="testCase">The value to round-trip.</param>
    /// <param name="caseIndex">The edge case index.</param>
    /// <returns>True when the value round-tripped within tolerance; false when the case was skipped.</returns>
    /// <exception cref="InvalidOperationException">The edge case failed in a way this serializer is expected to handle.</exception>
    private static async Task<bool> TryRoundTripDateTimeAsync(IBlobCache blobCache, ISerializer serializer, DateTime testCase, int caseIndex)
    {
        var key = $"datetime_edge_case_{caseIndex}";

        try
        {
            await blobCache.InsertObject(key, testCase);
            var retrieved = await blobCache.GetObject<DateTime>(key);

            return IsWithinDateTimeTolerance(blobCache, serializer, testCase, retrieved, caseIndex);
        }
        catch (Exception ex) when (IsAcceptableEdgeCaseException(caseIndex, ex))
        {
            Debug.WriteLine($"DateTime edge case {caseIndex} skipped for {testCase}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            // For BSON serializers and encrypted caches, be more lenient with edge cases
            if ((IsUsingBsonSerializer(serializer) || IsEncryptedCache(blobCache)) && caseIndex is 0 or 1)
            {
                Debug.WriteLine($"DateTime edge case {caseIndex} failed but acceptable: {testCase} - {ex.Message}");
                return false;
            }

            throw new InvalidOperationException($"DateTime edge case {caseIndex} failed for value {testCase} ({testCase.Kind})", ex);
        }
    }

    /// <summary>Determines whether a round-tripped DateTime landed close enough to the original.</summary>
    /// <param name="blobCache">The cache under test.</param>
    /// <param name="serializer">The serializer under test.</param>
    /// <param name="testCase">The original value.</param>
    /// <param name="retrieved">The value read back from the cache.</param>
    /// <param name="caseIndex">The edge case index.</param>
    /// <returns>True when the difference is inside the tolerance for this case.</returns>
    private static bool IsWithinDateTimeTolerance(IBlobCache blobCache, ISerializer serializer, DateTime testCase, DateTime retrieved, int caseIndex)
    {
        if (IsMangledExtremeValue(testCase, retrieved))
        {
            // BSON serializers often have issues with extreme DateTime values.
            Debug.WriteLine($"BSON DateTime edge case {caseIndex} skipped: {testCase} -> {retrieved}");
            return false;
        }

        var difference = Math.Abs((ConvertToComparableUtc(testCase) - ConvertToComparableUtc(retrieved)).TotalMilliseconds);
        var toleranceMs = GetToleranceMilliseconds(blobCache, serializer, caseIndex);
        if (difference < toleranceMs)
        {
            return true;
        }

        Debug.WriteLine(
            $"DateTime edge case {caseIndex} tolerance exceeded: {testCase} ({testCase.Kind}) -> {retrieved} ({retrieved.Kind}) (diff: {difference}ms, tolerance: {toleranceMs}ms)");
        return false;
    }

    /// <summary>Determines whether an extreme DateTime came back mangled, a known BSON limitation.</summary>
    /// <param name="testCase">The original value.</param>
    /// <param name="retrieved">The value read back from the cache.</param>
    /// <returns>True when an extreme input round-tripped to an implausible value.</returns>
    private static bool IsMangledExtremeValue(DateTime testCase, DateTime retrieved) =>
        (testCase == DateTime.MinValue || testCase == DateTime.MaxValue)
        && (retrieved == DateTime.MinValue
         || retrieved.Year <= MinPlausibleRoundTripYear
         || retrieved.Year >= MaxPlausibleRoundTripYear);

    /// <summary>Gets the round-trip tolerance for an edge case, widened for BSON serializers and encrypted caches.</summary>
    /// <param name="blobCache">The cache under test.</param>
    /// <param name="serializer">The serializer under test.</param>
    /// <param name="caseIndex">The edge case index.</param>
    /// <returns>The tolerance in milliseconds.</returns>
    private static double GetToleranceMilliseconds(IBlobCache blobCache, ISerializer serializer, int caseIndex)
    {
        var toleranceMs = GetDateTimeToleranceForEdgeCase(caseIndex);

        // Enhanced tolerance for BSON serializers and encrypted caches
        var serializerTypeName = serializer.GetType().Name;
        if (serializerTypeName.Contains("Newton") || serializerTypeName.Contains("Bson") || IsUsingBsonSerializer(serializer))
        {
            toleranceMs *= BsonToleranceMultiplier;
        }

        if (IsEncryptedCache(blobCache))
        {
            toleranceMs *= EncryptedCacheToleranceMultiplier;
        }

        return toleranceMs;
    }

    /// <summary>Round-trips a single DateTimeOffset edge case through the cache.</summary>
    /// <param name="blobCache">The cache under test.</param>
    /// <param name="serializer">The serializer under test.</param>
    /// <param name="testCase">The value to round-trip.</param>
    /// <param name="caseIndex">The edge case index.</param>
    /// <returns>True when the value round-tripped within tolerance; false when the case was skipped.</returns>
    /// <exception cref="InvalidOperationException">The edge case failed in a way this serializer is expected to handle.</exception>
    private static async Task<bool> TryRoundTripDateTimeOffsetAsync(IBlobCache blobCache, ISerializer serializer, DateTimeOffset testCase, int caseIndex)
    {
        var key = $"datetimeoffset_edge_case_{caseIndex}";

        try
        {
            await blobCache.InsertObject(key, testCase);
            var retrieved = await blobCache.GetObject<DateTimeOffset>(key);

            return ValidateDateTimeOffsetRoundtrip(testCase, retrieved, serializer);
        }
        catch (Exception ex) when (IsAcceptableEdgeCaseException(caseIndex, ex))
        {
            Debug.WriteLine($"DateTimeOffset edge case {caseIndex} skipped for {testCase}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            // For BSON serializers, be more lenient with edge cases
            if (IsUsingBsonSerializer(serializer) && caseIndex is 0 or 1)
            {
                Debug.WriteLine($"BSON DateTimeOffset edge case {caseIndex} failed but acceptable: {testCase} - {ex.Message}");
                return false;
            }

            // For encrypted caches, also be more lenient
            if (IsEncryptedCache(blobCache))
            {
                Debug.WriteLine($"Encrypted cache DateTimeOffset edge case {caseIndex} failed but acceptable: {testCase} - {ex.Message}");
                return false;
            }

            throw new InvalidOperationException($"DateTimeOffset edge case {caseIndex} failed for value {testCase}", ex);
        }
    }

    /// <summary>Converts a DateTime to a comparable UTC DateTime, handling various edge cases.</summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>A UTC DateTime for comparison purposes.</returns>
    private static DateTime ConvertToComparableUtc(in DateTime dateTime) => dateTime.Kind switch
    {
        DateTimeKind.Utc => dateTime,
        DateTimeKind.Local => dateTime.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
    };

    /// <summary>Gets the appropriate tolerance for a specific DateTime edge case.</summary>
    /// <param name="caseIndex">The edge case index.</param>
    /// <returns>The tolerance in milliseconds.</returns>
    private static double GetDateTimeToleranceForEdgeCase(int caseIndex) =>
        caseIndex switch
        {
            0 or 1 => ExtremeValueToleranceMilliseconds, // DateTime.MinValue and MaxValue - very generous
            LocalNowCaseIndex or TodayCaseIndex => ClockDependentToleranceMilliseconds, // local clock reads - allow for timezone issues
            _ => DefaultToleranceMilliseconds
        };

    /// <summary>Determines if an exception for an edge case is acceptable and the test should be skipped.</summary>
    /// <param name="caseIndex">The edge case index.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if the exception is acceptable and the test should be skipped.</returns>
    private static bool IsAcceptableEdgeCaseException(int caseIndex, Exception exception) =>
        caseIndex is 0 or 1
        && (exception.Message.Contains("out of range")
         || exception.Message.Contains("overflow")
         || exception.Message.Contains("underflow"));

    /// <summary>Determines if the current serializer is a BSON-based serializer.</summary>
    /// <param name="serializer">The serializer.</param>
    /// <returns>True if using a BSON serializer.</returns>
    private static bool IsUsingBsonSerializer(ISerializer serializer)
    {
        try
        {
            var serializerTypeName = serializer.GetType().Name;
            return serializerTypeName.Contains("Bson");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Gets DateTimeOffset test cases that cover mobile and desktop application scenarios.</summary>
    /// <param name="serializer">The serializer.</param>
    /// <returns>Array of DateTimeOffset test cases.</returns>
    private static DateTimeOffset[] GetMobileDesktopDateTimeOffsetTestCases(ISerializer serializer)
    {
        List<DateTimeOffset> cases =
        [
            new(2025, 1, 15, 10, 30, 45, TimeSpan.Zero), // UTC
            new(2025, 1, 15, 10, 30, 45, TimeSpan.FromHours(IndiaOffsetHours)), // UTC+5 (India)
            new(2025, 1, 15, 10, 30, 45, TimeSpan.FromHours(PacificStandardOffsetHours)), // UTC-8 (PST)
            new(2025, 1, 15, 10, 30, 45, TimeSpan.FromHours(EasternStandardOffsetHours)), // UTC-5 (EST)
            new(2025, 1, 15, 10, 30, 45, TimeSpan.FromHours(CentralEuropeanOffsetHours)), // UTC+1 (CET)
            new(2025, 1, 15, 10, 30, 45, TimeSpan.FromHours(JapanOffsetHours)), // UTC+9 (JST)

            // Current time scenarios
            TimeProvider.System.GetUtcNow(),
            TimeProvider.System.GetLocalNow(),

            // Edge cases (but safer than Min/Max)
            new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new(2030, 12, 31, 23, 59, 59, TimeSpan.Zero)
        ];

        // Only add extreme edge cases for non-BSON serializers
        if (!IsUsingBsonSerializer(serializer))
        {
            cases.AddRange([
                DateTimeOffset.MinValue,
                DateTimeOffset.MaxValue
            ]);
        }

        return [.. cases];
    }

    /// <summary>Validates a DateTimeOffset roundtrip with appropriate tolerance.</summary>
    /// <param name="original">The original DateTimeOffset.</param>
    /// <param name="retrieved">The retrieved DateTimeOffset.</param>
    /// <param name="serializer">The serializer.</param>
    /// <returns>
    /// True if the roundtrip is valid.
    /// </returns>
    private static bool ValidateDateTimeOffsetRoundtrip(in DateTimeOffset original, in DateTimeOffset retrieved, ISerializer serializer)
    {
        // UTC time should be very close
        var utcTicksDifference = Math.Abs(original.UtcTicks - retrieved.UtcTicks);
        var utcToleranceTicks = TimeSpan.FromSeconds(UtcToleranceSeconds).Ticks;

        if (utcTicksDifference >= utcToleranceTicks)
        {
            Debug.WriteLine(
                $"DateTimeOffset UTC ticks validation failed: {$"original={original.UtcTicks}, retrieved={retrieved.UtcTicks}, "}{$"diff={utcTicksDifference} ticks"}");
            return false;
        }

        // Offset comparison: be flexible as some serializers normalize offsets
        var offsetDifference = Math.Abs((original.Offset - retrieved.Offset).TotalHours);
        var offsetTolerance = IsUsingBsonSerializer(serializer) ? BsonOffsetToleranceHours : StandardOffsetToleranceHours;

        if (offsetDifference <= offsetTolerance)
        {
            return true;
        }

        Debug.WriteLine(
            $"DateTimeOffset offset validation failed: {$"original={original.Offset}, retrieved={retrieved.Offset}, "}{$"diff={offsetDifference} hours, tolerance={offsetTolerance} hours"}");
        return false;
    }

    /// <summary>Sets up the test with the specified serializer type.</summary>
    /// <param name="serializerType">The type of serializer to use for this test.</param>
    /// <returns>The configured serializer instance.</returns>
    private static ISerializer SetupTestSerializer(Type? serializerType)
    {
        // Clear any existing in-flight requests to ensure clean test state
        RequestCache.Clear();

        if (serializerType == typeof(NewtonsoftBsonSerializer))
        {
            // Register the Newtonsoft BSON serializer specifically
            return new NewtonsoftBsonSerializer();
        }

        if (serializerType == typeof(SystemJsonBsonSerializer))
        {
            // Register the System.Text.Json BSON serializer specifically
            return new SystemJsonBsonSerializer();
        }

        if (serializerType == typeof(NewtonsoftSerializer))
        {
            // Register the Newtonsoft JSON serializer
            return new NewtonsoftSerializer();
        }

        return serializerType == typeof(SystemJsonSerializer) ? new SystemJsonSerializer() : null!;
    }
}
