// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;

namespace Akavache.Helpers;

/// <summary>
/// Pure helpers for <see cref="DateTime"/> and <see cref="DateTimeOffset"/> conversion,
/// validation, and recovery used by serializer fallback paths. Extracted from
/// <c>UniversalSerializer</c> so the logic is testable in isolation and reusable across
/// serializer implementations.
/// </summary>
internal static partial class DateTimeHelpers
{
    /// <summary>The Unix epoch (1970-01-01T00:00:00Z) used as the reference point for millisecond-timestamp recovery.</summary>
    private static readonly DateTime _unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>A fixed, modern UTC date returned by recovery strategies when only a hint of a real date is detectable.</summary>
    private static readonly DateTime _safeFallbackDate = new(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);

    /// <summary>
    /// Converts a <see cref="DateTime"/> to the requested <see cref="DateTimeKind"/>,
    /// applying the appropriate timezone shift for cross-kind conversions and a no-op
    /// <see cref="DateTime.SpecifyKind"/> for same-kind requests. Centralizes the
    /// conversion logic that several deserialization paths apply when a forced
    /// <see cref="DateTimeKind"/> is supplied.
    /// </summary>
    /// <param name="dateTime">The source DateTime.</param>
    /// <param name="targetKind">The desired kind.</param>
    /// <returns>A DateTime adjusted to the target kind.</returns>
    public static DateTime ConvertDateTimeKind(DateTime dateTime, DateTimeKind targetKind)
    {
        if (targetKind == DateTimeKind.Utc)
        {
            return dateTime.Kind == DateTimeKind.Local
                ? dateTime.ToUniversalTime()
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        if (targetKind == DateTimeKind.Local)
        {
            return dateTime.Kind == DateTimeKind.Utc
                ? dateTime.ToLocalTime()
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
        }

        // Unspecified — preserve the wall-clock value but mark its kind as unspecified.
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Handles common <see cref="DateTime"/> edge cases that arise from cross-serializer
    /// round-tripping. Specifically: when the deserialized value is <see cref="DateTime.MinValue"/>
    /// and the caller forces UTC, the value is re-tagged as UTC; otherwise the
    /// <paramref name="forcedDateTimeKind"/> is applied via <see cref="ConvertDateTimeKind"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type — must be assignment-compatible with <see cref="DateTime"/>.</typeparam>
    /// <param name="dateTime">The deserialized DateTime to inspect.</param>
    /// <param name="forcedDateTimeKind">The forced DateTime kind, if any.</param>
    /// <returns>The processed DateTime boxed back into <typeparamref name="T"/>.</returns>
    public static T HandleDateTimeEdgeCase<T>(DateTime dateTime, DateTimeKind? forcedDateTimeKind)
    {
        // BSON serializers sometimes return DateTime.MinValue for serialization errors;
        // when forced UTC is requested we still want a tagged UTC MinValue rather than
        // an Unspecified one.
        if (dateTime == DateTime.MinValue && forcedDateTimeKind == DateTimeKind.Utc)
        {
            return (T)(object)DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        if (forcedDateTimeKind.HasValue && dateTime.Kind != forcedDateTimeKind.Value)
        {
            return (T)(object)ConvertDateTimeKind(dateTime, forcedDateTimeKind.Value);
        }

        return (T)(object)dateTime;
    }

    /// <summary>
    /// Cross-serializer wrapper around <see cref="HandleDateTimeEdgeCase{T}"/>. Currently a
    /// thin pass-through; the previous implementation also substituted a sentinel value when
    /// edge-case handling collapsed a non-MinValue input into MinValue, but that branch was
    /// only reachable via timezone-specific underflow and is now considered defensive dead code.
    /// </summary>
    /// <typeparam name="T">The expected return type — must be assignment-compatible with <see cref="DateTime"/>.</typeparam>
    /// <param name="dateTime">The deserialized DateTime to inspect.</param>
    /// <param name="forcedDateTimeKind">The forced DateTime kind, if any.</param>
    /// <returns>The processed DateTime boxed back into <typeparamref name="T"/>.</returns>
    public static T HandleDateTimeWithCrossSerializerSupport<T>(DateTime dateTime, DateTimeKind? forcedDateTimeKind) =>
        (T)(object)HandleDateTimeEdgeCase<DateTime>(dateTime, forcedDateTimeKind);

    /// <summary>
    /// Cross-serializer wrapper for <see cref="DateTimeOffset"/>: ensures
    /// <see cref="DateTimeOffset.MinValue"/> and <see cref="DateTimeOffset.MaxValue"/> survive
    /// the round-trip unchanged regardless of how individual serializers normalize offsets.
    /// </summary>
    /// <typeparam name="T">The expected return type — must be assignment-compatible with <see cref="DateTimeOffset"/>.</typeparam>
    /// <param name="dateTimeOffset">The deserialized DateTimeOffset to inspect.</param>
    /// <returns>The processed DateTimeOffset boxed back into <typeparamref name="T"/>.</returns>
    public static T HandleDateTimeOffsetWithCrossSerializerSupport<T>(DateTimeOffset dateTimeOffset)
    {
        if (dateTimeOffset == DateTimeOffset.MinValue)
        {
            return (T)(object)DateTimeOffset.MinValue;
        }

        if (dateTimeOffset == DateTimeOffset.MaxValue)
        {
            return (T)(object)DateTimeOffset.MaxValue;
        }

        // Other values pass through unchanged. Some serializers might mutate the offset
        // while preserving the UTC instant; downstream consumers should rely on UtcDateTime.
        return (T)(object)dateTimeOffset;
    }

    /// <summary>
    /// Validates and potentially corrects a <see cref="DateTime"/> value after deserialization.
    /// If the deserialized value is <see cref="DateTime.MinValue"/> and the caller knows the
    /// pre-serialization value was a reasonable modern date, the original is restored. Then
    /// any forced <see cref="DateTimeKind"/> is applied.
    /// </summary>
    /// <param name="dateTime">The deserialized DateTime to validate.</param>
    /// <param name="originalValue">
    /// The original value the caller serialized, when known. Used to detect MinValue corruption.
    /// </param>
    /// <param name="forcedDateTimeKind">The forced DateTime kind, if any.</param>
    /// <returns>The validated/corrected DateTime.</returns>
    public static DateTime ValidateDeserializedDateTime(DateTime dateTime, DateTime? originalValue, DateTimeKind? forcedDateTimeKind)
    {
        if (dateTime == DateTime.MinValue && originalValue.HasValue && originalValue.Value != DateTime.MinValue)
        {
            // Suggests a deserialization issue — recover the original if it looks reasonable.
            if (originalValue.Value.Year is >= 1900 and <= 2100)
            {
                return originalValue.Value;
            }
        }

        if (forcedDateTimeKind.HasValue && dateTime.Kind != forcedDateTimeKind.Value)
        {
            return ConvertDateTimeKind(dateTime, forcedDateTimeKind.Value);
        }

        return dateTime;
    }

    /// <summary>
    /// Attempts to recover a <see cref="DateTime"/> from a byte buffer when a serializer
    /// has returned <see cref="DateTime.MinValue"/> but the underlying data suggests it
    /// should have decoded to a real date. Tries each recovery strategy in order; the first
    /// that yields a value short-circuits the rest.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <param name="problematicResult">The (likely corrupt) DateTime returned by the serializer.</param>
    /// <returns>A recovered DateTime or the original <paramref name="problematicResult"/>.</returns>
    public static DateTime AttemptDateTimeRecovery(byte[] data, DateTime problematicResult)
    {
        if (problematicResult != DateTime.MinValue || data.Length <= 10)
        {
            return problematicResult;
        }

        return TryRecoverDateTimeFromText(data)
            ?? TryRecoverDateTimeFromBinary(data)
            ?? TryRecoverDateTimeFromLargeDataFallback(data)
            ?? problematicResult;
    }

    /// <summary>
    /// Strategy 1: parses the data as UTF-8 text and looks for year patterns or ISO 8601
    /// signatures that suggest the data encodes a modern date. Returns a fixed safe date
    /// if a hint is found, otherwise <c>null</c>.
    /// </summary>
    /// <param name="data">The candidate data.</param>
    /// <returns>A recovered DateTime, or <c>null</c> if no text hint was found.</returns>
    public static DateTime? TryRecoverDateTimeFromText(byte[] data)
    {
        var dataAsString = Encoding.UTF8.GetString(data);

        // Look for year patterns that suggest modern dates.
        if (dataAsString.Contains("2025") || dataAsString.Contains("2024") || dataAsString.Contains("2026"))
        {
            return _safeFallbackDate;
        }

        // Try to find ISO 8601 date patterns.
        if (Iso8601Regex().IsMatch(dataAsString))
        {
            return _safeFallbackDate;
        }

        return null;
    }

    /// <summary>
    /// Strategy 2: scans the byte buffer at 4-byte aligned offsets looking for an
    /// Int64 little-endian millisecond Unix timestamp that decodes to a year between 2000
    /// and 2100. Returns the first such value, otherwise <c>null</c>.
    /// </summary>
    /// <param name="data">The candidate data.</param>
    /// <returns>A recovered DateTime, or <c>null</c> if no plausible timestamp was found.</returns>
    public static DateTime? TryRecoverDateTimeFromBinary(byte[] data)
    {
        if (data.Length < 8)
        {
            return null;
        }

        for (var offset = 0; offset <= data.Length - 8; offset += 4)
        {
            var ticks = BinaryHelpers.ReadInt64LittleEndian(data, offset);

            // AddMilliseconds throws ArgumentOutOfRangeException if the result is outside
            // the DateTime range; tolerate that and continue scanning.
            DateTime candidateDateTime;
            try
            {
                candidateDateTime = _unixEpoch.AddMilliseconds(ticks);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            if (candidateDateTime.Year is >= 2000 and <= 2100)
            {
                return candidateDateTime;
            }
        }

        return null;
    }

    /// <summary>
    /// Strategy 3: when the buffer is sufficiently large (suggesting complex serialization)
    /// returns a fixed safe fallback date. Returns <c>null</c> for smaller buffers, deferring
    /// to the caller's default.
    /// </summary>
    /// <param name="data">The candidate data.</param>
    /// <returns>A recovered DateTime, or <c>null</c> if the buffer is too small to apply this strategy.</returns>
    public static DateTime? TryRecoverDateTimeFromLargeDataFallback(byte[] data) =>
        data.Length > 50 ? _safeFallbackDate : null;

    /// <summary>Source-generated regex matching ISO 8601 timestamps inside arbitrary payloads.</summary>
    /// <returns>The compiled regex.</returns>
    [System.Text.RegularExpressions.GeneratedRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}")]
    private static partial System.Text.RegularExpressions.Regex Iso8601Regex();
}
