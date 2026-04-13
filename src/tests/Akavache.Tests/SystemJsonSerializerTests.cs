// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests for SystemJsonSerializer covering all paths including JsonTypeInfo AOT-safe overloads.
/// </summary>
[Category("Akavache")]
public class SystemJsonSerializerTests
{
    /// <summary>
    /// Tests Serialize and Deserialize round-trip.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldRoundTrip()
    {
        var serializer = new SystemJsonSerializer();
        var data = serializer.Serialize(new SerializerTestModel { Name = "test", Value = 42 });
        var result = serializer.Deserialize<SerializerTestModel>(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("test");
        await Assert.That(result.Value).IsEqualTo(42);
    }

    /// <summary>
    /// Tests Deserialize returns default for null bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForNullBytes()
    {
        var serializer = new SystemJsonSerializer();
        var result = serializer.Deserialize<SerializerTestModel>(null!);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests Deserialize returns default for empty bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForEmptyBytes()
    {
        var serializer = new SystemJsonSerializer();
        var result = serializer.Deserialize<SerializerTestModel>([]);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests Serialize and Deserialize with custom options.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldUseCustomOptions()
    {
        var serializer = new SystemJsonSerializer
        {
            Options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        };

        var data = serializer.Serialize(new SerializerTestModel { Name = "test", Value = 1 });
        var json = Encoding.UTF8.GetString(data);

        // CamelCase policy applied
        await Assert.That(json.Contains("name")).IsTrue();
    }

    /// <summary>
    /// Tests AOT-safe Serialize with JsonTypeInfo.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithJsonTypeInfoShouldWork()
    {
        var serializer = new SystemJsonSerializer();
        var model = new SerializerTestModel { Name = "aot", Value = 99 };

        var data = serializer.Serialize(model, SerializerTestContext.Default.SerializerTestModel);
        await Assert.That(data).IsNotNull();
        await Assert.That(data.Length).IsGreaterThan(0);

        var json = Encoding.UTF8.GetString(data);
        await Assert.That(json).Contains("aot");
    }

    /// <summary>
    /// Tests AOT-safe Deserialize with JsonTypeInfo.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithJsonTypeInfoShouldWork()
    {
        var serializer = new SystemJsonSerializer();
        var model = new SerializerTestModel { Name = "aot-roundtrip", Value = 7 };

        var data = serializer.Serialize(model, SerializerTestContext.Default.SerializerTestModel);
        var result = serializer.Deserialize(data, SerializerTestContext.Default.SerializerTestModel);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("aot-roundtrip");
        await Assert.That(result.Value).IsEqualTo(7);
    }

    /// <summary>
    /// Tests AOT-safe Deserialize returns default for null bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithJsonTypeInfoShouldReturnDefaultForNullBytes()
    {
        var serializer = new SystemJsonSerializer();
        var result = serializer.Deserialize(null!, SerializerTestContext.Default.SerializerTestModel);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests AOT-safe Deserialize returns default for empty bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithJsonTypeInfoShouldReturnDefaultForEmptyBytes()
    {
        var serializer = new SystemJsonSerializer();
        var result = serializer.Deserialize([], SerializerTestContext.Default.SerializerTestModel);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests ForcedDateTimeKind setter and getter.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindShouldGetAndSet()
    {
        var serializer = new SystemJsonSerializer();
        await Assert.That(serializer.ForcedDateTimeKind).IsNull();

        serializer.ForcedDateTimeKind = DateTimeKind.Utc;
        await Assert.That(serializer.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
    }
}
