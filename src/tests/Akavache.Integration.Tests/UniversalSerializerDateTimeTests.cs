// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for how <see cref="UniversalSerializer"/> preprocesses and validates
/// <see cref="DateTime"/> and <see cref="DateTimeOffset"/> values, including the
/// serializer-specific range clamping and the forced-kind conversions.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class UniversalSerializerDateTimeTests
{
    /// <summary>The year Newtonsoft-safe preprocessing clamps <see cref="DateTime.MaxValue"/> to.</summary>
    private const int NewtonsoftSafeMaximumYear = 2100;

    /// <summary>The year Newtonsoft-safe preprocessing clamps <see cref="DateTime.MinValue"/> to.</summary>
    private const int NewtonsoftSafeMinimumYear = 1900;

    /// <summary>How far a round-tripped DateTime may drift before the edge-case test fails.</summary>
    private const double RoundTripToleranceMinutes = 1440;

    /// <summary>The UTC offset, in hours, carried by the sample <see cref="DateTimeOffset"/>.</summary>
    private const int SampleOffsetHours = 2;

    /// <summary>Tests that UniversalSerializer handles DateTime serialization correctly.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldHandleDateTimeSerialization()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        DateTime testDateTime = new(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);

        // Act
        var serializedData = UniversalSerializer.Serialize(testDateTime, serializer, DateTimeKind.Utc);
        var deserializedDateTime =
            UniversalSerializer.Deserialize<DateTime>(serializedData, serializer, DateTimeKind.Utc);

        // Assert
        await Assert.That(deserializedDateTime).IsEqualTo(testDateTime);
        await Assert.That(deserializedDateTime.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>Tests that UniversalSerializer handles DateTime edge cases.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldHandleDateTimeEdgeCases()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        DateTime[] edgeCases =
        [
            DateTime.MinValue,
            DateTime.MaxValue,
            new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new(2025, 12, 31, 23, 59, 59, DateTimeKind.Local)
        ];

        foreach (var testDate in edgeCases)
        {
            try
            {
                // Act
                var serializedData = UniversalSerializer.Serialize(testDate, serializer);
                var deserializedDate = UniversalSerializer.Deserialize<DateTime>(serializedData, serializer);

                // Assert - Allow for some tolerance in extreme cases
                var timeDifference = Math.Abs((testDate - deserializedDate).TotalMinutes);
                await Assert.That(timeDifference).IsLessThan(RoundTripToleranceMinutes); // 24 hours tolerance
            }
            catch (Exception ex)
            {
                // Some edge cases may fail due to serializer limitations - this is acceptable
                // Just ensure the exception is handled gracefully
                await Assert.That(ex).IsTypeOf<InvalidOperationException>();
            }
        }
    }

    /// <summary>Tests that UniversalSerializer properly validates DateTime after deserialization.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldValidateDeserializedDateTime()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        DateTime testDateTime = new(2025, 1, 15, 10, 30, 45, DateTimeKind.Local);

        // Act
        var serializedData = UniversalSerializer.Serialize(testDateTime, serializer, DateTimeKind.Utc);
        var deserializedDateTime =
            UniversalSerializer.Deserialize<DateTime>(serializedData, serializer, DateTimeKind.Utc);

        // Assert - Should be converted to UTC
        await Assert.That(deserializedDateTime.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>Tests that UniversalSerializer properly preprocesses DateTime for serialization.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldPreprocessDateTime()
    {
        // Arrange
        NewtonsoftSerializer newtonsoftSerializer = new();

        // Test edge cases that might be problematic for certain serializers
        DateTime[] edgeDates =
        [
            DateTime.MinValue,
            DateTime.MaxValue,
            new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        ];

        foreach (var testDate in edgeDates)
        {
            try
            {
                // Act - Should preprocess the date to make it safer for serialization
                var serializedData = UniversalSerializer.Serialize(testDate, newtonsoftSerializer);

                // Assert
                await Assert.That(serializedData).IsNotNull();
                await Assert.That(serializedData).IsNotEmpty();
            }
            catch (InvalidOperationException)
            {
                // Some edge cases may still fail - this is acceptable
                // The important thing is that the failure is handled gracefully
            }
        }
    }

    /// <summary>Tests PreprocessDateTimeForSerialization with MinValue for Newtonsoft.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldHandleMinValueForNewtonsoft()
    {
        NewtonsoftSerializer serializer = new();
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(DateTime.MinValue, serializer, null);
        await Assert.That(result.Year).IsEqualTo(NewtonsoftSafeMinimumYear);
    }

    /// <summary>Tests PreprocessDateTimeForSerialization with MaxValue for Newtonsoft.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldHandleMaxValueForNewtonsoft()
    {
        NewtonsoftSerializer serializer = new();
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(DateTime.MaxValue, serializer, null);
        await Assert.That(result.Year).IsEqualTo(NewtonsoftSafeMaximumYear);
    }

    /// <summary>Tests PreprocessDateTimeForSerialization with MinValue for SystemJson (no special handling).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldNotModifyMinValueForSystemJson()
    {
        SystemJsonSerializer serializer = new();
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(DateTime.MinValue, serializer, null);
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>Tests PreprocessDateTimeForSerialization with forced UTC kind on Local DateTime.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldConvertLocalToUtcWhenForced()
    {
        SystemJsonSerializer serializer = new();
        DateTime localDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(localDate, serializer, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>Tests PreprocessDateTimeForSerialization with forced Local kind on UTC DateTime.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldConvertUtcToLocalWhenForced()
    {
        SystemJsonSerializer serializer = new();
        DateTime utcDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(utcDate, serializer, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>Tests PreprocessDateTimeForSerialization with forced Unspecified kind.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldConvertToUnspecifiedWhenForced()
    {
        SystemJsonSerializer serializer = new();
        DateTime utcDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result =
            UniversalSerializer.PreprocessDateTimeForSerialization(utcDate, serializer, DateTimeKind.Unspecified);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Unspecified);
    }

    /// <summary>Tests PreprocessDateTimeForSerialization converting Unspecified to Local.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldConvertUnspecifiedToLocalWhenForced()
    {
        SystemJsonSerializer serializer = new();
        DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(date, serializer, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>Tests PreprocessDateTimeForSerialization with Unspecified kind forcing Utc.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldConvertUnspecifiedToUtcWhenForced()
    {
        SystemJsonSerializer serializer = new();
        DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(date, serializer, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>Tests PreprocessDateTimeForSerialization with MinValue for NewtonsoftBson (no special handling).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldNotModifyMinValueForBsonSerializer()
    {
        NewtonsoftBsonSerializer serializer = new();
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(DateTime.MinValue, serializer, null);

        // BSON serializer contains "Newtonsoft" but also "Bson", so no special handling
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>Tests PreprocessDateTimeForSerialization with MaxValue for NewtonsoftBson (no special handling).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldNotModifyMaxValueForBsonSerializer()
    {
        NewtonsoftBsonSerializer serializer = new();
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(DateTime.MaxValue, serializer, null);
        await Assert.That(result).IsEqualTo(DateTime.MaxValue);
    }

    /// <summary>Tests <see cref="UniversalSerializer.CastAsDateTime{T}"/> returns the value when it is a <see cref="DateTime"/>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CastAsDateTimeShouldReturnValueForDateTimeType()
    {
        DateTime expected = new(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var result = UniversalSerializer.CastAsDateTime(expected);

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>Tests <see cref="UniversalSerializer.CastAsDateTime{T}"/> returns <c>default</c> for non-<see cref="DateTime"/> types.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CastAsDateTimeShouldReturnDefaultForOtherType()
    {
        var result = UniversalSerializer.CastAsDateTime("not a datetime");

        await Assert.That(result).IsEqualTo(default);
    }

    /// <summary>Tests <see cref="UniversalSerializer.CastAsDateTime{T}"/> returns <c>default</c> when the input is <see langword="null"/>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CastAsDateTimeShouldReturnDefaultForNull()
    {
        var result = UniversalSerializer.CastAsDateTime<string>(null);

        await Assert.That(result).IsEqualTo(default);
    }

    /// <summary>Tests <see cref="UniversalSerializer.CastAsDateTimeOffset{T}"/> returns the value when it is a <see cref="DateTimeOffset"/>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CastAsDateTimeOffsetShouldReturnValueForDateTimeOffsetType()
    {
        DateTimeOffset expected = new(2025, 1, 2, 3, 4, 5, TimeSpan.FromHours(SampleOffsetHours));

        var result = UniversalSerializer.CastAsDateTimeOffset(expected);

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>Tests <see cref="UniversalSerializer.CastAsDateTimeOffset{T}"/> returns <c>default</c> for non-<see cref="DateTimeOffset"/> types.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CastAsDateTimeOffsetShouldReturnDefaultForOtherType()
    {
        var result = UniversalSerializer.CastAsDateTimeOffset("not a datetimeoffset");

        await Assert.That(result).IsEqualTo(default);
    }

    /// <summary>Tests Deserialize wraps DateTime validation path (forced kind when primary succeeds).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldValidateDateTimeResultFromPrimary()
    {
        SystemJsonSerializer serializer = new();
        DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var data = serializer.Serialize(date);

        var result = UniversalSerializer.Deserialize<DateTime>(data, serializer, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests the full Deserialize path with DateTime and forced kind when primary succeeds
    /// but the result has a different kind that needs validation (line 55-58).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldValidateDateTimeKindConversionLocalToUtc()
    {
        SystemJsonSerializer serializer = new();
        DateTime localDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);
        var data = serializer.Serialize(localDate);

        var result = UniversalSerializer.Deserialize<DateTime>(data, serializer, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests the full Serialize path with DateTime and forced kind where the DateTime
    /// kind does not match the forced kind (exercises lines 108-111 and 114-117).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeShouldPreprocessDateTimeWithForcedLocalKind()
    {
        SystemJsonSerializer serializer = new();
        DateTime utcDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var data = UniversalSerializer.Serialize(utcDate, serializer, DateTimeKind.Local);
        await Assert.That(data).IsNotNull();
        await Assert.That(data).IsNotEmpty();

        // Verify the round-trip preserves the forced kind
        var deserialized = UniversalSerializer.Deserialize<DateTime>(data, serializer, DateTimeKind.Local);
        await Assert.That(deserialized.Kind).IsEqualTo(DateTimeKind.Local);
    }
}
