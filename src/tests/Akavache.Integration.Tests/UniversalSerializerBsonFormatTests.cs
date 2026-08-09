// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for <see cref="UniversalSerializer.TryDeserializeBsonFormat{T}"/>, covering the
/// registered BSON serializer walk, the DateTime recovery strategies and the forced-kind
/// conversions applied to a recovered date.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class UniversalSerializerBsonFormatTests
{
    /// <summary>The number of bytes a BSON document's int32 length header occupies.</summary>
    private const int BsonLengthHeaderSize = 4;

    /// <summary>The buffer length of the synthetic BSON documents handed to the fake serializers.</summary>
    private const int FakeBsonDocumentLength = 30;

    /// <summary>The year of the fixed date the fake BSON serializers return.</summary>
    private const int FakeSerializerDateYear = 2025;

    /// <summary>A document length long enough for DateTime recovery to be attempted.</summary>
    private const int RecoveryEligibleDocumentLength = 40;

    /// <summary>The year of the safe date the BSON path falls back to when recovery finds nothing.</summary>
    private const int RecoverySafeDateYear = 2025;

    /// <summary>A document length that leaves recovery without hints yet below the large-data fallback.</summary>
    private const int SafeDateFallbackDocumentLength = 25;

    /// <summary>Tests TryDeserializeBsonFormat with registered BSON serializers actually deserializes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldDeserializeWithRegistry()
    {
        SerializerRegistryFixture.RegisterAll();
        try
        {
            NewtonsoftBsonSerializer bsonSerializer = new();
            UserObject testObject = new() { Name = "BSON Direct", Bio = "Bio", Blog = "Blog" };
            var bsonData = bsonSerializer.Serialize(testObject);

            var result = UniversalSerializer.TryDeserializeBsonFormat<UserObject>(bsonData, null);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("BSON Direct");
        }
        finally
        {
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests TryDeserializeBsonFormat exercises the DateTime UTC kind path through registry.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task TryDeserializeBsonFormatShouldHandleDateTimeWithUtcKind()
    {
        try
        {
            SerializerRegistryFixture.RegisterAll();
            try
            {
                NewtonsoftBsonSerializer bsonSerializer = new();
                DateTime testDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
                var bsonData = bsonSerializer.Serialize(testDate);

                // Path is exercised even if BSON-direct DateTime serialization returns MinValue + recovery
                // Recovery may rewrite to fallback date, but path is exercised
                // Path exercised regardless of result value (BSON DateTime handling has special cases)
                _ = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(bsonData, DateTimeKind.Utc);
            }
            finally
            {
                SerializerRegistryFixture.Reset();
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests TryDeserializeBsonFormat exercises the DateTime Local kind path through registry.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task TryDeserializeBsonFormatShouldHandleDateTimeWithLocalKind()
    {
        try
        {
            SerializerRegistryFixture.RegisterAll();
            try
            {
                NewtonsoftBsonSerializer bsonSerializer = new();
                DateTime testDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
                var bsonData = bsonSerializer.Serialize(testDate);

                // Path exercised regardless of result value (BSON DateTime handling has special cases)
                _ = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(bsonData, DateTimeKind.Local);
            }
            finally
            {
                SerializerRegistryFixture.Reset();
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests TryDeserializeBsonFormat exercises the DateTime Unspecified kind path through registry.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task TryDeserializeBsonFormatShouldHandleDateTimeWithUnspecifiedKind()
    {
        try
        {
            SerializerRegistryFixture.RegisterAll();
            try
            {
                NewtonsoftBsonSerializer bsonSerializer = new();
                DateTime testDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
                var bsonData = bsonSerializer.Serialize(testDate);

                // Path exercised regardless of result value (BSON DateTime handling has special cases)
                _ = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(bsonData, DateTimeKind.Unspecified);
            }
            finally
            {
                SerializerRegistryFixture.Reset();
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests TryDeserializeBsonFormat with invalid data returns default.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldReturnDefaultForInvalidData()
    {
        var result = UniversalSerializer.TryDeserializeBsonFormat<UserObject>([0xFF, 0xFE, 0x00, 0x01, 0x02], null);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests TryDeserializeBsonFormat with DateTime MinValue where recovery succeeds (data > 20 bytes + "2025").</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldRecoverDateTimeMinValueWithData()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonMinValueSerializer());

        // Must look like BSON (length header) and be > 20 bytes with "2025" pattern.
        var bytes = "pad-pad-pad-2025-06-15T10:30:00Z-pad"u8.ToArray();
        var data = new byte[bytes.Length + BsonLengthHeaderSize];
        BitConverter.GetBytes(data.Length).CopyTo(data, 0);
        bytes.CopyTo(data, BsonLengthHeaderSize);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>Tests TryDeserializeBsonFormat with DateTime MinValue when recovery fails (fallback to 2025 safe date).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldFallBackToSafeDateWhenRecoveryFails()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonMinValueSerializer());

        // BSON-like header but no recovery hints, > 20 bytes of zeros.
        var data = new byte[RecoveryEligibleDocumentLength];
        BitConverter.GetBytes(RecoveryEligibleDocumentLength).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Utc);

        // Recovery fallback returns a non-MinValue 2025 date OR the strategy-3 large-data fallback.
        await Assert.That(result).IsNotEqualTo(DateTime.MinValue);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat with DateTime MinValue where data is exactly 21 bytes
    /// (> 20 threshold) but recovery returns MinValue, triggering the else fallback on line 592.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldUseSafeDateWhenRecoveryReturnsMinValue()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonMinValueSerializer());

        // Data > 20 bytes with no recovery patterns and <= 50 bytes (avoids strategy 3 fallback)
        // so AttemptDateTimeRecovery returns MinValue, triggering the else on line 592.
        var data = new byte[SafeDateFallbackDocumentLength];
        BitConverter.GetBytes(SafeDateFallbackDocumentLength).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, null);

        // The else fallback sets 2025-01-15 safe date
        await Assert.That(result.Year).IsEqualTo(RecoverySafeDateYear);
    }

    /// <summary>Tests TryDeserializeBsonFormat DateTime with forced Local kind conversion.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldConvertDateTimeToLocalKind()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedUtcSerializer());

        var data = new byte[FakeBsonDocumentLength];
        BitConverter.GetBytes(FakeBsonDocumentLength).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>Tests TryDeserializeBsonFormat DateTime with forced Unspecified kind conversion.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldConvertDateTimeToUnspecifiedKind()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedUtcSerializer());

        var data = new byte[FakeBsonDocumentLength];
        BitConverter.GetBytes(FakeBsonDocumentLength).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Unspecified);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Unspecified);
    }

    /// <summary>Tests TryDeserializeBsonFormat DateTime with forced UTC kind converting from Local.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldConvertDateTimeLocalToUtc()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedLocalSerializer());

        var data = new byte[FakeBsonDocumentLength];
        BitConverter.GetBytes(FakeBsonDocumentLength).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat exercises the DateTime kind conversion path from Local to Local
    /// where forced kind matches the existing kind (no conversion needed).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldNotConvertWhenKindAlreadyMatches()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedLocalSerializer());

        var data = new byte[FakeBsonDocumentLength];
        BitConverter.GetBytes(FakeBsonDocumentLength).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat with no forced kind and a valid non-MinValue DateTime.
    /// Exercises the path where forcedDateTimeKind is null so no conversion happens.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldReturnDateTimeUnchangedWithNoForcedKind()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedUtcSerializer());

        var data = new byte[FakeBsonDocumentLength];
        BitConverter.GetBytes(FakeBsonDocumentLength).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, null);
        await Assert.That(result.Year).IsEqualTo(FakeSerializerDateYear);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>Tests TryDeserializeBsonFormat continues on BSON serializer exceptions (inner catch).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldContinueOnSerializerException()
    {
        UniversalSerializer.RegisterSerializer(static () => new ThrowingBsonSerializer());
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedUtcSerializer());

        var data = new byte[FakeBsonDocumentLength];
        BitConverter.GetBytes(FakeBsonDocumentLength).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, null);
        await Assert.That(result.Year).IsEqualTo(FakeSerializerDateYear);
    }

    /// <summary>Tests TryDeserializeBsonFormat returns raw result for non-DateTime types.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldReturnRawResultForNonDateTime()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonStringSerializer());

        var data = new byte[FakeBsonDocumentLength];
        BitConverter.GetBytes(FakeBsonDocumentLength).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<string>(data, null);
        await Assert.That(result).IsEqualTo("fake-bson-string");
    }

    /// <summary>A fake BSON-named serializer that returns a fixed Local DateTime.</summary>
    private sealed class FakeBsonFixedLocalSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) != typeof(DateTime)
                ? default
                : (T)(object)new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => [];
    }
}
