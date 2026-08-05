// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;

namespace Akavache.Integration.Tests;

/// <summary>Tests for SystemJsonSerializer covering all paths including JsonTypeInfo AOT-safe overloads.</summary>
[Category("Akavache")]
public class SystemJsonSerializerTests
{
    /// <summary>Payload value carried through the reflection-based round trip.</summary>
    private const int RoundTripValue = 42;

    /// <summary>Payload value carried through the metadata-only serialize case.</summary>
    private const int AotSerializedValue = 99;

    /// <summary>Payload value carried through the metadata-only round trip.</summary>
    private const int AotRoundTripValue = 7;

    /// <summary>Options that rename properties to camelCase; shared because <see cref="SystemJsonSerializer"/> copies them before use.</summary>
    private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Tests Serialize and Deserialize round-trip.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldRoundTrip()
    {
        SystemJsonSerializer serializer = new();
        var data = serializer.Serialize(new SerializerTestModel { Name = "test", Value = RoundTripValue });
        var result = serializer.Deserialize<SerializerTestModel>(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("test");
        await Assert.That(result.Value).IsEqualTo(RoundTripValue);
    }

    /// <summary>Tests Deserialize returns default for null bytes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForNullBytes()
    {
        SystemJsonSerializer serializer = new();
        var result = serializer.Deserialize<SerializerTestModel>(null!);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests Deserialize returns default for empty bytes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForEmptyBytes()
    {
        SystemJsonSerializer serializer = new();
        var result = serializer.Deserialize<SerializerTestModel>([]);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests Serialize and Deserialize with custom options.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldUseCustomOptions()
    {
        SystemJsonSerializer serializer = new() { Options = CamelCaseOptions };

        var data = serializer.Serialize(new SerializerTestModel { Name = "test", Value = 1 });
        var json = Encoding.UTF8.GetString(data);

        // CamelCase policy applied
        await Assert.That(json.Contains("name")).IsTrue();
    }

    /// <summary>Tests AOT-safe Serialize with JsonTypeInfo.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithJsonTypeInfoShouldWork()
    {
        SystemJsonSerializer serializer = new();
        SerializerTestModel model = new() { Name = "aot", Value = AotSerializedValue };

        var data = serializer.Serialize(model, SerializerTestContext.Default.SerializerTestModel);
        await Assert.That(data).IsNotNull();
        await Assert.That(data.Length).IsGreaterThan(0);

        var json = Encoding.UTF8.GetString(data);
        await Assert.That(json).Contains("aot");
    }

    /// <summary>Tests AOT-safe Deserialize with JsonTypeInfo.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithJsonTypeInfoShouldWork()
    {
        SystemJsonSerializer serializer = new();
        SerializerTestModel model = new() { Name = "aot-roundtrip", Value = AotRoundTripValue };

        var data = serializer.Serialize(model, SerializerTestContext.Default.SerializerTestModel);
        var result = serializer.Deserialize(data, SerializerTestContext.Default.SerializerTestModel);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("aot-roundtrip");
        await Assert.That(result.Value).IsEqualTo(AotRoundTripValue);
    }

    /// <summary>Tests AOT-safe Deserialize returns default for null bytes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithJsonTypeInfoShouldReturnDefaultForNullBytes()
    {
        SystemJsonSerializer serializer = new();
        var result = serializer.Deserialize(null!, SerializerTestContext.Default.SerializerTestModel);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests AOT-safe Deserialize returns default for empty bytes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithJsonTypeInfoShouldReturnDefaultForEmptyBytes()
    {
        SystemJsonSerializer serializer = new();
        var result = serializer.Deserialize([], SerializerTestContext.Default.SerializerTestModel);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests ForcedDateTimeKind setter and getter.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindShouldGetAndSet()
    {
        SystemJsonSerializer serializer = new();
        await Assert.That(serializer.ForcedDateTimeKind).IsNull();

        serializer.ForcedDateTimeKind = DateTimeKind.Utc;
        await Assert.That(serializer.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
    }
}
