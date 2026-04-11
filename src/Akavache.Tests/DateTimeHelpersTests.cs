// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;
using Akavache.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for the pure <see cref="DateTimeHelpers"/> functions extracted from
/// <c>UniversalSerializer</c>: kind conversion, edge-case handling, validation, and the
/// three DateTime recovery strategies. These tests exercise the helpers in isolation
/// without going through any serializer.
/// </summary>
[Category("Akavache")]
public class DateTimeHelpersTests
{

    /// <summary>
    /// Tests HandleDateTimeEdgeCase with MinValue and UTC forced kind.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeEdgeCaseShouldHandleMinValueWithUtcKind()
    {
        var result = DateTimeHelpers.HandleDateTimeEdgeCase<DateTime>(DateTime.MinValue, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(result).IsEqualTo(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
    }

    /// <summary>
    /// Tests HandleDateTimeEdgeCase converting Local to UTC.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeEdgeCaseShouldConvertLocalToUtc()
    {
        var localDate = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);
        var result = DateTimeHelpers.HandleDateTimeEdgeCase<DateTime>(localDate, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests HandleDateTimeEdgeCase converting UTC to Local.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeEdgeCaseShouldConvertUtcToLocal()
    {
        var utcDate = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = DateTimeHelpers.HandleDateTimeEdgeCase<DateTime>(utcDate, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests HandleDateTimeEdgeCase with no forced kind.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeEdgeCaseShouldReturnUnchangedWhenNoForcedKind()
    {
        var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = DateTimeHelpers.HandleDateTimeEdgeCase<DateTime>(date, null);
        await Assert.That(result).IsEqualTo(date);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests HandleDateTimeEdgeCase converting Unspecified to UTC.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeEdgeCaseShouldConvertUnspecifiedToUtc()
    {
        var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var result = DateTimeHelpers.HandleDateTimeEdgeCase<DateTime>(date, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests HandleDateTimeEdgeCase converting Unspecified to Local.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeEdgeCaseShouldConvertUnspecifiedToLocal()
    {
        var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var result = DateTimeHelpers.HandleDateTimeEdgeCase<DateTime>(date, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests HandleDateTimeWithCrossSerializerSupport for normal DateTime.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeWithCrossSerializerSupportShouldHandleNormalDateTime()
    {
        var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = DateTimeHelpers.HandleDateTimeWithCrossSerializerSupport<DateTime>(date, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests HandleDateTimeWithCrossSerializerSupport with MinValue when edge case processing doesn't change it.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeWithCrossSerializerSupportShouldHandleMinValue()
    {
        var result = DateTimeHelpers.HandleDateTimeWithCrossSerializerSupport<DateTime>(DateTime.MinValue, null);

        // Without forced kind, MinValue stays MinValue
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests HandleDateTimeOffsetWithCrossSerializerSupport for MinValue.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeOffsetWithCrossSerializerSupportShouldHandleMinValue()
    {
        var result = DateTimeHelpers.HandleDateTimeOffsetWithCrossSerializerSupport<DateTimeOffset>(DateTimeOffset.MinValue);
        await Assert.That(result).IsEqualTo(DateTimeOffset.MinValue);
    }

    /// <summary>
    /// Tests HandleDateTimeOffsetWithCrossSerializerSupport for MaxValue.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeOffsetWithCrossSerializerSupportShouldHandleMaxValue()
    {
        var result = DateTimeHelpers.HandleDateTimeOffsetWithCrossSerializerSupport<DateTimeOffset>(DateTimeOffset.MaxValue);
        await Assert.That(result).IsEqualTo(DateTimeOffset.MaxValue);
    }

    /// <summary>
    /// Tests HandleDateTimeOffsetWithCrossSerializerSupport for normal value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeOffsetWithCrossSerializerSupportShouldHandleNormalValue()
    {
        var dto = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.FromHours(5));
        var result = DateTimeHelpers.HandleDateTimeOffsetWithCrossSerializerSupport<DateTimeOffset>(dto);
        await Assert.That(result).IsEqualTo(dto);
    }

    /// <summary>
    /// Tests HandleDateTimeOffsetWithCrossSerializerSupport handles normal offsets.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeOffsetWithCrossSerializerSupportShouldHandleAll()
    {
        var min = DateTimeHelpers.HandleDateTimeOffsetWithCrossSerializerSupport<DateTimeOffset>(DateTimeOffset.MinValue);
        var max = DateTimeHelpers.HandleDateTimeOffsetWithCrossSerializerSupport<DateTimeOffset>(DateTimeOffset.MaxValue);
        var normal = DateTimeHelpers.HandleDateTimeOffsetWithCrossSerializerSupport<DateTimeOffset>(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await Assert.That(min).IsEqualTo(DateTimeOffset.MinValue);
        await Assert.That(max).IsEqualTo(DateTimeOffset.MaxValue);
        await Assert.That(normal.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Tests ValidateDeserializedDateTime with MinValue and original value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDeserializedDateTimeShouldRecoverOriginalValue()
    {
        var original = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = DateTimeHelpers.ValidateDeserializedDateTime(DateTime.MinValue, original, null);
        await Assert.That(result).IsEqualTo(original);
    }

    /// <summary>
    /// Tests ValidateDeserializedDateTime with MinValue but no original.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDeserializedDateTimeShouldReturnMinValueWhenNoOriginal()
    {
        var result = DateTimeHelpers.ValidateDeserializedDateTime(DateTime.MinValue, null, null);
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests ValidateDeserializedDateTime with forced UTC kind.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDeserializedDateTimeShouldApplyForcedUtcKind()
    {
        var localDate = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);
        var result = DateTimeHelpers.ValidateDeserializedDateTime(localDate, null, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests ValidateDeserializedDateTime with forced Local kind.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDeserializedDateTimeShouldApplyForcedLocalKind()
    {
        var utcDate = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = DateTimeHelpers.ValidateDeserializedDateTime(utcDate, null, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests ValidateDeserializedDateTime with forced Unspecified kind.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDeserializedDateTimeShouldApplyForcedUnspecifiedKind()
    {
        var utcDate = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = DateTimeHelpers.ValidateDeserializedDateTime(utcDate, null, DateTimeKind.Unspecified);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Tests ValidateDeserializedDateTime with MinValue and unreasonable original.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDeserializedDateTimeShouldNotRecoverUnreasonableOriginal()
    {
        var original = new DateTime(1800, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = DateTimeHelpers.ValidateDeserializedDateTime(DateTime.MinValue, original, null);

        // The original year is outside 1900-2100 range so it won't be recovered
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests ValidateDeserializedDateTime with MinValue but original also MinValue.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDeserializedDateTimeShouldKeepMinValueIfOriginalIsAlsoMinValue()
    {
        var result = DateTimeHelpers.ValidateDeserializedDateTime(DateTime.MinValue, DateTime.MinValue, null);
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery with data containing year patterns.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldRecoverFromYearPatterns()
    {
        var data = Encoding.UTF8.GetBytes("{\"date\":\"2025-03-15T10:30:00Z\"}");
        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, DateTime.MinValue);
        await Assert.That(result).IsNotEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery with data containing 2024.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldRecoverFrom2024Pattern()
    {
        var data = Encoding.UTF8.GetBytes("date: 2024-12-25");
        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, DateTime.MinValue);
        await Assert.That(result).IsNotEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery with data containing 2026.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldRecoverFrom2026Pattern()
    {
        var data = Encoding.UTF8.GetBytes("timestamp: 2026-01-01");
        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, DateTime.MinValue);
        await Assert.That(result).IsNotEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery with short data (no recovery possible).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldReturnOriginalForShortData()
    {
        var data = new byte[] { 1, 2, 3 };
        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, DateTime.MinValue);
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery with non-MinValue input.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldReturnOriginalWhenNotMinValue()
    {
        var original = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var data = Encoding.UTF8.GetBytes("some data");
        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, original);
        await Assert.That(result).IsEqualTo(original);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery with large data containing no year patterns (strategy 3).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldUseFallbackForLargeData()
    {
        // Large data without year patterns or valid BSON timestamps
        var data = new byte[100];
        Array.Fill(data, (byte)0xAA);
        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, DateTime.MinValue);

        // Strategy 3: large data should return fallback
        await Assert.That(result).IsNotEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery with binary data containing valid epoch timestamp.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldRecoverFromBinaryTimestamp()
    {
        // Create data with a valid Unix epoch millisecond timestamp for ~2025
        var data = new byte[16];
        var epochMs = (long)(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        BitConverter.GetBytes(epochMs).CopyTo(data, 0);

        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, DateTime.MinValue);
        await Assert.That(result).IsNotEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery with ISO 8601 date pattern.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldRecoverFromIso8601Pattern()
    {
        // Data that has ISO date pattern but not a year keyword
        var data = Encoding.UTF8.GetBytes("{\"ts\":\"1999-12-31T23:59:59\"}");
        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, DateTime.MinValue);

        // Should detect ISO pattern and return fallback
        await Assert.That(result).IsNotEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery returns fallback for large data without year patterns.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldUseFallbackForLargeDataNoPatterns()
    {
        // 60 bytes of zeros - no year patterns, no valid epoch ms
        var data = new byte[60];
        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, DateTime.MinValue);
        await Assert.That(result).IsNotEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests HandleDateTimeEdgeCase MinValue without forced UTC kind returns unchanged.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeEdgeCaseShouldNotSpecialCaseMinValueWithoutUtcKind()
    {
        var result = DateTimeHelpers.HandleDateTimeEdgeCase<DateTime>(DateTime.MinValue, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests HandleDateTimeWithCrossSerializerSupport MinValue fallback path
    /// (processed == MinValue but input != MinValue is impossible via HandleDateTimeEdgeCase,
    /// so this just confirms normal MinValue passthrough).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeWithCrossSerializerSupportShouldPassThroughMinValueFromMinValue()
    {
        var result = DateTimeHelpers.HandleDateTimeWithCrossSerializerSupport<DateTime>(DateTime.MinValue, DateTimeKind.Utc);

        // With forced UTC kind, MinValue gets SpecifyKind -> still MinValue but Utc.
        await Assert.That(result).IsEqualTo(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
    }

    /// <summary>
    /// Tests ValidateDeserializedDateTime with Unspecified input forced to Utc.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDeserializedDateTimeShouldConvertUnspecifiedToUtc()
    {
        var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var result = DateTimeHelpers.ValidateDeserializedDateTime(date, null, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests ValidateDeserializedDateTime with Local input forced to Local returns specified Local.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDeserializedDateTimeShouldSpecifyLocalWhenUnspecifiedInputForcedLocal()
    {
        var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var result = DateTimeHelpers.ValidateDeserializedDateTime(date, null, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery returns problematic result when data is small and no patterns match.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldReturnOriginalForSmallDataNoPattern()
    {
        // 15 bytes (>10 for recovery check), no year pattern, no valid epoch candidate, <= 50 so strategy 3 skipped.
        var data = new byte[15];
        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, DateTime.MinValue);
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests HandleDateTimeEdgeCase with Unspecified kind input and Unspecified forced kind
    /// where they already match so no conversion is needed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandleDateTimeEdgeCaseShouldNotConvertWhenKindsMatch()
    {
        var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);
        var result = DateTimeHelpers.HandleDateTimeEdgeCase<DateTime>(date, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
        await Assert.That(result).IsEqualTo(date);
    }

    /// <summary>
    /// Tests ValidateDeserializedDateTime with Unspecified input forced to Unspecified
    /// where kinds already match so no conversion happens.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDeserializedDateTimeShouldNotConvertWhenKindsMatch()
    {
        var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = DateTimeHelpers.ValidateDeserializedDateTime(date, null, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(result).IsEqualTo(date);
    }

    /// <summary>
    /// Tests AttemptDateTimeRecovery with data containing only non-ASCII bytes that are exactly 8 bytes
    /// so the binary timestamp loop runs but finds no valid candidate.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttemptDateTimeRecoveryShouldHandleBinaryDataWithNoValidTimestamp()
    {
        // 12 bytes (> 10), no year pattern in UTF8, 8+ bytes for binary scan
        // Values chosen so BitConverter.ToInt64 won't produce a date in 2000-2100 range
        var data = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };
        var result = DateTimeHelpers.AttemptDateTimeRecovery(data, DateTime.MinValue);

        // Small data (<= 50 bytes), no year pattern, binary search finds ticks=1 which is year ~1970
        // so recovery returns MinValue
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromText"/> returns a fixed
    /// fallback date when the data contains the year "2024" as a substring.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromTextShouldRecoverFrom2024Pattern()
    {
        var data = Encoding.UTF8.GetBytes("payload contains 2024 in it somewhere");

        var result = DateTimeHelpers.TryRecoverDateTimeFromText(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromText"/> returns a fixed
    /// fallback date when the data contains the year "2025" as a substring.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromTextShouldRecoverFrom2025Pattern()
    {
        var data = Encoding.UTF8.GetBytes("event happened on 2025 day");

        var result = DateTimeHelpers.TryRecoverDateTimeFromText(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromText"/> returns a fixed
    /// fallback date when the data contains the year "2026" as a substring.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromTextShouldRecoverFrom2026Pattern()
    {
        var data = Encoding.UTF8.GetBytes("scheduled for 2026 next year");

        var result = DateTimeHelpers.TryRecoverDateTimeFromText(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromText"/> returns a fixed
    /// fallback date when the data contains an ISO 8601 datetime substring (and no year hint).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromTextShouldRecoverFromIso8601Pattern()
    {
        // Year deliberately outside the 2024-2026 hint set so the ISO 8601 path fires.
        var data = Encoding.UTF8.GetBytes("captured at 2030-06-15T12:34:56 utc");

        var result = DateTimeHelpers.TryRecoverDateTimeFromText(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromText"/> returns
    /// <c>null</c> when the data contains no year hint and no ISO 8601 signature.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromTextShouldReturnNullWhenNoPattern()
    {
        var data = Encoding.UTF8.GetBytes("nothing date-like here at all");

        var result = DateTimeHelpers.TryRecoverDateTimeFromText(data);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromBinary"/> returns
    /// <c>null</c> for buffers shorter than 8 bytes (the minimum needed for an Int64 read).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromBinaryShouldReturnNullForShortBuffer()
    {
        var result = DateTimeHelpers.TryRecoverDateTimeFromBinary(new byte[4]);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromBinary"/> recovers a
    /// DateTime when the buffer contains a plausible Unix-epoch millisecond timestamp.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromBinaryShouldRecoverPlausibleTimestamp()
    {
        // Build a buffer where the first 8 bytes encode an Int64 little-endian millisecond
        // value that decodes to a year between 2000 and 2100.
        var target = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var ms = (long)(target - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        var data = BitConverter.GetBytes(ms);

        var result = DateTimeHelpers.TryRecoverDateTimeFromBinary(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromBinary"/> returns
    /// <c>null</c> when the buffer contains no plausible timestamp at any 4-byte aligned offset.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromBinaryShouldReturnNullWhenNoPlausibleTimestamp()
    {
        // All zeros decode to 1970-01-01, which is outside the 2000-2100 plausibility window.
        var data = new byte[16];

        var result = DateTimeHelpers.TryRecoverDateTimeFromBinary(data);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromBinary"/> tolerates
    /// <see cref="ArgumentOutOfRangeException"/> from <see cref="DateTime.AddMilliseconds"/>
    /// when the decoded ticks exceed <see cref="DateTime"/>'s representable range, and continues
    /// scanning the buffer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromBinaryShouldTolerateOverflowAtCandidateOffset()
    {
        // First 8 bytes: Int64.MaxValue → AddMilliseconds throws ArgumentOutOfRangeException.
        // Remaining 8 bytes: zero → AddMilliseconds(0) succeeds but year (1970) is out of range.
        // Net result: scan completes without throwing and returns null.
        var data = new byte[16];
        BitConverter.GetBytes(long.MaxValue).CopyTo(data, 0);

        var result = DateTimeHelpers.TryRecoverDateTimeFromBinary(data);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromLargeDataFallback"/>
    /// returns a fixed safe date when the buffer is larger than 50 bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromLargeDataFallbackShouldReturnSafeDateForLargeBuffers()
    {
        var data = new byte[64];

        var result = DateTimeHelpers.TryRecoverDateTimeFromLargeDataFallback(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Verifies <see cref="DateTimeHelpers.TryRecoverDateTimeFromLargeDataFallback"/>
    /// returns <c>null</c> when the buffer is at or below the 50-byte threshold.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRecoverDateTimeFromLargeDataFallbackShouldReturnNullForSmallBuffers()
    {
        var result = DateTimeHelpers.TryRecoverDateTimeFromLargeDataFallback(new byte[50]);

        await Assert.That(result).IsNull();
    }
}
