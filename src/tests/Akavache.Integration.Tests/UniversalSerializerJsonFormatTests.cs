// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for <see cref="UniversalSerializer.TryDeserializeJsonFormat{T}"/>, covering the
/// registered JSON serializer walk, the skipping of BSON-named serializers and the fall
/// back to the minimal JSON reader.
/// </summary>
[Category("Akavache")]
public class UniversalSerializerJsonFormatTests
{
    /// <summary>The integer value encoded by the numeric JSON payload these tests feed in.</summary>
    private const int EncodedIntegerValue = 42;

    /// <summary>The year of the sample date these tests serialize.</summary>
    private const int SampleDateYear = 2025;

    /// <summary>Tests TryDeserializeJsonFormat with registered JSON serializers.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldDeserializeWithRegistry()
    {
        SerializerRegistryFixture.RegisterAll();
        try
        {
            SystemJsonSerializer jsonSerializer = new();
            UserObject testObject = new() { Name = "JSON Direct", Bio = "Bio", Blog = "Blog" };
            var jsonData = jsonSerializer.Serialize(testObject);

            var result = UniversalSerializer.TryDeserializeJsonFormat<UserObject>(jsonData, null);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("JSON Direct");
        }
        finally
        {
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests TryDeserializeJsonFormat with forced DateTime kind through registry.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldHandleDateTimeKind()
    {
        SerializerRegistryFixture.RegisterAll();
        try
        {
            SystemJsonSerializer jsonSerializer = new();
            DateTime testDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var jsonData = jsonSerializer.Serialize(testDate);

            var result = UniversalSerializer.TryDeserializeJsonFormat<DateTime>(jsonData, DateTimeKind.Utc);
            await Assert.That(result.Year).IsEqualTo(SampleDateYear);
        }
        finally
        {
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests TryDeserializeJsonFormat falls back to basic deserialization for string.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldFallBackToBasicDeserialization()
    {
        var data = "\"simple string value\""u8.ToArray();
        var result = UniversalSerializer.TryDeserializeJsonFormat<string>(data, null);

        // Even if JSON serializer lookup fails, TryBasicJsonDeserialization handles strings
        await Assert.That(result).IsNotNull();
    }

    /// <summary>Tests TryDeserializeJsonFormat returns default for completely invalid data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldReturnDefaultForBinaryData()
    {
        byte[] data = [0xFF, 0xFE, 0x00, 0x01, 0x02];
        var result = UniversalSerializer.TryDeserializeJsonFormat<UserObject>(data, null);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests TryDeserializeJsonFormat continues when JSON serializer throws.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldContinueWhenSerializerThrows()
    {
        UniversalSerializer.RegisterSerializer(static () => new ThrowingSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        var data = "\"hello\""u8.ToArray();
        var result = UniversalSerializer.TryDeserializeJsonFormat<string>(data, null);
        await Assert.That(result).IsEqualTo("hello");
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat lines 641-643: when a registered BSON serializer is present,
    /// it is skipped (continue) during JSON format deserialization.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldSkipBsonSerializers()
    {
        // Register both a BSON serializer (should be skipped) and a JSON serializer (should be used)
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonStringSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        var data = "\"from-json\""u8.ToArray();
        var result = UniversalSerializer.TryDeserializeJsonFormat<string>(data, null);
        await Assert.That(result).IsEqualTo("from-json");
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat when all registered serializers are BSON (skipped),
    /// falls back to TryBasicJsonDeserialization for int type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldFallBackToBasicJsonWhenOnlyBsonRegistered()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonStringSerializer());

        var data = "42"u8.ToArray();
        var result = UniversalSerializer.TryDeserializeJsonFormat<int>(data, null);
        await Assert.That(result).IsEqualTo(EncodedIntegerValue);
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat skips BSON-named serializers and then
    /// falls back when no non-BSON serializer is registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldSkipAllBsonAndFallBackToBasic()
    {
        // Register only BSON serializers - all should be skipped
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonMinValueSerializer());
        UniversalSerializer.RegisterSerializer(static () => new ThrowingBsonSerializer());

        var data = "\"test-skip-bson\""u8.ToArray();
        var result = UniversalSerializer.TryDeserializeJsonFormat<string>(data, null);
        await Assert.That(result).IsEqualTo("test-skip-bson");
    }
}
