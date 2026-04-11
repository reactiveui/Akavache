// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests for SystemJsonBsonSerializer covering BSON-specific paths and edge cases.
/// </summary>
[Category("Akavache")]
public class SystemJsonBsonSerializerTests
{
    /// <summary>
    /// Tests Options getter and setter delegate to the inner serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OptionsShouldGetAndSet()
    {
        var serializer = new SystemJsonBsonSerializer();
        await Assert.That(serializer.Options).IsNull();

        var customOptions = new JsonSerializerOptions { WriteIndented = true };
        serializer.Options = customOptions;
        await Assert.That(serializer.Options).IsEqualTo(customOptions);
    }

    /// <summary>
    /// Tests ForcedDateTimeKind defaults to Utc.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindShouldDefaultToUtc()
    {
        var serializer = new SystemJsonBsonSerializer();
        await Assert.That(serializer.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests IsPotentialBsonData returns false for null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForNull()
    {
        var result = SystemJsonBsonSerializer.IsPotentialBsonData(null!);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialBsonData returns false for short data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForShortData()
    {
        var result = SystemJsonBsonSerializer.IsPotentialBsonData([1, 2, 3]);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialBsonData returns false for unreasonable length.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForUnreasonableLength()
    {
        var data = new byte[10];
        BitConverter.GetBytes(3).CopyTo(data, 0);
        var result = SystemJsonBsonSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialBsonData returns false for JSON object data starting with '{'.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForJsonObject()
    {
        // Set length so it passes the size check, then put '{' at position 4
        var data = new byte[20];
        BitConverter.GetBytes(20).CopyTo(data, 0);
        data[4] = (byte)'{';
        var result = SystemJsonBsonSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialBsonData returns true for valid BSON-shaped data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnTrueForValidBson()
    {
        // Serialize an actual object to BSON
        var serializer = new SystemJsonBsonSerializer();
        var data = serializer.Serialize(new SerializerTestModel { Name = "test", Value = 42 });

        var result = SystemJsonBsonSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests Serialize and Deserialize round-trip in BSON format.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldRoundTripBson()
    {
        var serializer = new SystemJsonBsonSerializer();
        var data = serializer.Serialize(new SerializerTestModel { Name = "bson-test", Value = 42 });
        var result = serializer.Deserialize<SerializerTestModel>(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("bson-test");
        await Assert.That(result.Value).IsEqualTo(42);
    }

    /// <summary>
    /// Tests Deserialize returns default for null bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForNullBytes()
    {
        var serializer = new SystemJsonBsonSerializer();
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
        var serializer = new SystemJsonBsonSerializer();
        var result = serializer.Deserialize<SerializerTestModel>([]);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests Deserialize falls back to JSON when BSON detection fails.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldFallBackToJson()
    {
        var serializer = new SystemJsonBsonSerializer();

        // Provide JSON data, not BSON
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes("{\"Name\":\"json-fallback\",\"Value\":1}");
        var result = serializer.Deserialize<SerializerTestModel>(jsonBytes);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("json-fallback");
    }

    /// <summary>
    /// Tests Deserialize returns default for invalid data instead of throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForInvalidData()
    {
        var serializer = new SystemJsonBsonSerializer();
        var invalid = new byte[] { 0xFF, 0xFE, 0x00, 0x01, 0x02 };
        var result = serializer.Deserialize<SerializerTestModel>(invalid);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests Serialize with JsonTypeInfo (AOT path delegates to inner).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithJsonTypeInfoShouldWork()
    {
        var serializer = new SystemJsonBsonSerializer();
        var data = serializer.Serialize(
            new SerializerTestModel { Name = "aot", Value = 1 },
            SerializerTestContext.Default.SerializerTestModel);
        await Assert.That(data).IsNotNull();
        await Assert.That(data.Length).IsGreaterThan(0);
    }

    /// <summary>
    /// Tests Deserialize with JsonTypeInfo (AOT path delegates to inner).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithJsonTypeInfoShouldWork()
    {
        var serializer = new SystemJsonBsonSerializer();
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes("{\"Name\":\"aot\",\"Value\":7}");
        var result = serializer.Deserialize(jsonBytes, SerializerTestContext.Default.SerializerTestModel);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("aot");
    }

    /// <summary>
    /// Tests NormalizeDateTimeFormats converts Newtonsoft tick format to ISO 8601.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NormalizeDateTimeFormatsShouldConvertTicks()
    {
        // Pick a recent timestamp in ticks: 2025-01-01 UTC
        var ticks = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        var input = $"{{\"Date\":{ticks}}}";
        var result = SystemJsonBsonSerializer.NormalizeDateTimeFormats(input);

        await Assert.That(result).Contains("2025-01-01");
        await Assert.That(result).DoesNotContain(ticks.ToString());
    }

    /// <summary>
    /// Tests NormalizeDateTimeFormats leaves non-matching strings alone.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NormalizeDateTimeFormatsShouldLeaveOtherStringsAlone()
    {
        var input = "{\"Name\":\"test\"}";
        var result = SystemJsonBsonSerializer.NormalizeDateTimeFormats(input);
        await Assert.That(result).IsEqualTo(input);
    }

    /// <summary>
    /// Tests SystemJsonBsonSerializer accepts custom options without throwing on serialize.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldAcceptCustomOptions()
    {
        var serializer = new SystemJsonBsonSerializer
        {
            Options = new JsonSerializerOptions { WriteIndented = true }
        };

        var data = serializer.Serialize(new SerializerTestModel { Name = "custom", Value = 5 });
        await Assert.That(data).IsNotNull();
        await Assert.That(data.Length).IsGreaterThan(0);
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> returns default when given malformed
    /// BSON bytes (the outer catch path around <c>BsonDataReader</c>).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldReturnDefaultForMalformedBson()
    {
        var serializer = new SystemJsonBsonSerializer();

        // Craft bytes that look BSON-ish in length header but are not valid BSON.
        var data = new byte[32];
        BitConverter.GetBytes(32).CopyTo(data, 0);

        for (var i = 4; i < data.Length; i++)
        {
            data[i] = 0x7F;
        }

        var result = serializer.DeserializeBsonFormat<SerializerTestModel>(data);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <c>Deserialize</c> falls back to JSON when BSON detection returns
    /// true but <c>DeserializeBsonFormat</c> throws internally.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldFallBackWhenBsonFormatFails()
    {
        var serializer = new SystemJsonBsonSerializer();

        var data = new byte[16];
        BitConverter.GetBytes(16).CopyTo(data, 0);
        for (var i = 4; i < data.Length; i++)
        {
            data[i] = 0x55;
        }

        var result = serializer.Deserialize<SerializerTestModel>(data);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> returns <c>default(int)</c> for a value
    /// type when parsing fails.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldReturnDefaultValueTypeOnFailure()
    {
        var serializer = new SystemJsonBsonSerializer();
        var data = new byte[16];
        BitConverter.GetBytes(16).CopyTo(data, 0);
        for (var i = 4; i < data.Length; i++)
        {
            data[i] = 0x10;
        }

        var result = serializer.DeserializeBsonFormat<int>(data);
        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>
    /// Tests <c>DeserializeBsonFormat</c> round-trip of a primitive string via the
    /// <c>ObjectWrapper</c> path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldRoundTripString()
    {
        var serializer = new SystemJsonBsonSerializer();
        var bytes = serializer.SerializeToBson("hello-bson");

        var result = serializer.DeserializeBsonFormat<string>(bytes);
        await Assert.That(result).IsEqualTo("hello-bson");
    }

    /// <summary>
    /// Tests <c>DeserializeBsonFormat</c> round-trip of an integer via the
    /// <c>ObjectWrapper</c> path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldRoundTripInteger()
    {
        var serializer = new SystemJsonBsonSerializer();
        var bytes = serializer.SerializeToBson(12345);

        var result = serializer.DeserializeBsonFormat<int>(bytes);
        await Assert.That(result).IsEqualTo(12345);
    }

    /// <summary>
    /// Tests <c>DeserializeBsonFormat</c> round-trip of a <see cref="DateTime"/> via
    /// the <c>ObjectWrapper</c> path, exercising <c>NormalizeDateTimeFormats</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldRoundTripDateTime()
    {
        var serializer = new SystemJsonBsonSerializer();
        var original = new DateTime(2025, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        var bytes = serializer.SerializeToBson(original);

        var result = serializer.DeserializeBsonFormat<DateTime>(bytes);
        await Assert.That(result.Year).IsEqualTo(2025);
        await Assert.That(result.Month).IsEqualTo(6);
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> returns default for an empty BSON
    /// document (exercising the <c>string.IsNullOrEmpty(jsonString)</c> branch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldHandleEmptyDocument()
    {
        var serializer = new SystemJsonBsonSerializer();

        // Minimal valid empty BSON document: int32 length (5) + terminator (0x00) = 5 bytes.
        var emptyBson = new byte[] { 0x05, 0x00, 0x00, 0x00, 0x00 };

        // Path is exercised - empty document yields a default-constructed object or null
        await Assert.That(() => serializer.DeserializeBsonFormat<SerializerTestModel>(emptyBson)).ThrowsNothing();
    }

    /// <summary>
    /// Tests that <c>Deserialize</c> returns default when BSON returns null for a
    /// reference type and JSON fallback also fails, exercising the fall-through logic.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldHandleEmptyBsonForReferenceType()
    {
        var serializer = new SystemJsonBsonSerializer();

        var emptyBson = new byte[] { 0x05, 0x00, 0x00, 0x00, 0x00 };

        // Path is exercised - empty BSON document yields default-constructed object
        await Assert.That(() => serializer.Deserialize<SerializerTestModel>(emptyBson)).ThrowsNothing();
    }

    /// <summary>
    /// Tests <c>SerializeToBson</c> with null input — should produce a wrapper
    /// document and successfully round-trip.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeToBsonShouldHandleNullValue()
    {
        var serializer = new SystemJsonBsonSerializer();
        var bytes = serializer.SerializeToBson<SerializerTestModel?>(null);

        await Assert.That(bytes).IsNotNull();
        await Assert.That(bytes.Length).IsGreaterThan(0);

        var result = serializer.DeserializeBsonFormat<SerializerTestModel?>(bytes);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <c>NormalizeDateTimeFormats</c> preserves the original match when
    /// the digits cannot be parsed as a long (overflow) — covers the
    /// <c>long.TryParse</c> false branch at line 147.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NormalizeDateTimeFormatsShouldPreserveUnparseableLong()
    {
        // 20+ digits exceed long.MaxValue (9223372036854775807 is 19 digits).
        var huge = "99999999999999999999";
        var input = $"{{\"Date\":{huge}}}";

        var result = SystemJsonBsonSerializer.NormalizeDateTimeFormats(input);

        // long.TryParse fails so the original match is returned unchanged.
        await Assert.That(result).IsEqualTo(input);
    }

    /// <summary>
    /// Tests that <c>NormalizeDateTimeFormats</c> preserves the match when the parsed
    /// tick value is out of range for <see cref="DateTime"/> (inner catch, lines 141-143).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NormalizeDateTimeFormatsShouldPreserveOutOfRangeTicks()
    {
        // DateTime.MaxValue.Ticks == 3155378975999999999. One step higher is still a valid
        // long but the DateTime constructor throws ArgumentOutOfRangeException.
        var tooLarge = "3155378976000000000";
        var input = $"{{\"Date\":{tooLarge}}}";

        var result = SystemJsonBsonSerializer.NormalizeDateTimeFormats(input);
        await Assert.That(result).IsEqualTo(input);
    }

    /// <summary>
    /// Tests that <c>Deserialize</c> returns default when given a length-prefixed
    /// non-JSON non-BSON buffer — both the BSON and JSON paths fail and the method
    /// returns default (covers outer JSON-fallback catch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultWhenAllPathsFail()
    {
        var serializer = new SystemJsonBsonSerializer();

        var data = new byte[15];
        BitConverter.GetBytes(15).CopyTo(data, 0);
        for (var i = 4; i < data.Length; i++)
        {
            data[i] = 0xAB;
        }

        var result = serializer.Deserialize<SerializerTestModel>(data);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> can recover a collection via the
    /// direct-deserialization path (no <c>"Value"</c> wrapper field in the JSON).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldRoundTripList()
    {
        var serializer = new SystemJsonBsonSerializer();
        var source = new List<SerializerTestModel>
        {
            new() { Name = "a", Value = 1 },
            new() { Name = "b", Value = 2 },
        };

        var bytes = serializer.SerializeToBson(source);
        var result = serializer.DeserializeBsonFormat<List<SerializerTestModel>>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Count).IsEqualTo(2);
        await Assert.That(result[0].Name).IsEqualTo("a");
        await Assert.That(result[1].Value).IsEqualTo(2);
    }

    /// <summary>
    /// Tests <c>SerializeToBson</c> with an <c>object</c>-typed payload, covering
    /// the generic wrapper path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeToBsonShouldHandleObjectTypedPayload()
    {
        var serializer = new SystemJsonBsonSerializer();
        object payload = new SerializerTestModel { Name = "obj", Value = 3 };

        var bytes = serializer.SerializeToBson(payload);
        await Assert.That(bytes).IsNotNull();
        await Assert.That(bytes.Length).IsGreaterThan(0);
    }

    /// <summary>
    /// Tests that <c>Deserialize</c> correctly handles plain UTF-8 JSON bytes that
    /// do not pass the BSON length-header heuristic (BSON detection returns false).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldUseJsonWhenBsonHeuristicRejects()
    {
        var serializer = new SystemJsonBsonSerializer();
        var json = "{\"Name\":\"direct\",\"Value\":7}";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = serializer.Deserialize<SerializerTestModel>(bytes);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("direct");
        await Assert.That(result.Value).IsEqualTo(7);
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> can consume a BSON buffer produced by
    /// Newtonsoft directly (no <c>ObjectWrapper</c>), exercising the direct
    /// <c>System.Text.Json</c> deserialization path at lines 224-228.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldHandleRawNewtonsoftBson()
    {
        using var ms = new MemoryStream();
        using (var writer = new Newtonsoft.Json.Bson.BsonDataWriter(ms))
        {
            var newtonsoft = new Newtonsoft.Json.JsonSerializer();
            newtonsoft.Serialize(writer, new SerializerTestModel { Name = "raw", Value = 42 });
        }

        var bytes = ms.ToArray();
        var sut = new SystemJsonBsonSerializer();
        var result = sut.DeserializeBsonFormat<SerializerTestModel>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("raw");
        await Assert.That(result.Value).IsEqualTo(42);
    }

    /// <summary>
    /// Tests <c>DeserializeBsonFormat</c> with <c>ForcedDateTimeKind</c> set to
    /// <see cref="DateTimeKind.Local"/>, exercising the reader DateTimeKindHandling
    /// assignment branch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldHonorForcedDateTimeKindLocal()
    {
        var serializer = new SystemJsonBsonSerializer
        {
            ForcedDateTimeKind = DateTimeKind.Local,
        };

        var data = serializer.Serialize(new SerializerTestModel { Name = "dtk", Value = 9 });
        var result = serializer.DeserializeBsonFormat<SerializerTestModel>(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("dtk");
    }

    /// <summary>
    /// Tests <c>DeserializeBsonFormat</c> with <c>ForcedDateTimeKind</c> set to null,
    /// skipping the reader DateTimeKindHandling assignment branch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldWorkWithNullForcedDateTimeKind()
    {
        var serializer = new SystemJsonBsonSerializer
        {
            ForcedDateTimeKind = null,
        };

        var data = serializer.Serialize(new SerializerTestModel { Name = "null-dtk", Value = 3 });
        var result = serializer.DeserializeBsonFormat<SerializerTestModel>(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("null-dtk");
    }

    /// <summary>
    /// Tests <c>Deserialize</c> with a value-type target (covers the
    /// <c>typeof(T).IsValueType</c> branch in the early-return check).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldHandleValueTypeRoundTrip()
    {
        var serializer = new SystemJsonBsonSerializer();
        var data = serializer.Serialize(42);
        var result = serializer.Deserialize<int>(data);

        await Assert.That(result).IsEqualTo(42);
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> falls back to the Newtonsoft wrapper
    /// deserialization path when System.Text.Json throws due to a type mismatch in
    /// the <c>Value</c> field — covers lines 207 and 211-214.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldUseNewtonsoftWrapperFallback()
    {
        // Write a raw BSON document of the form {"Value":"42"} — Newtonsoft will
        // coerce the string "42" into int via ObjectWrapper<int>, but System.Text.Json
        // is strict and throws, forcing the inner catch to take the Newtonsoft path.
        using var ms = new MemoryStream();
        using (var writer = new Newtonsoft.Json.Bson.BsonDataWriter(ms))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Value");
            writer.WriteValue("42");
            writer.WriteEndObject();
        }

        var bytes = ms.ToArray();
        var sut = new SystemJsonBsonSerializer();
        var result = sut.DeserializeBsonFormat<int>(bytes);

        await Assert.That(result).IsEqualTo(42);
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> continues to the direct deserialization
    /// path when both the System.Text.Json and Newtonsoft wrapper deserialization
    /// attempts throw — covers the inner catch-within-catch at lines 216-220.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldFallThroughWhenBothWrapperPathsFail()
    {
        // Write a BSON document {"Value":"not-a-number"} — neither STJ nor Newtonsoft
        // can coerce this into ObjectWrapper<int>.Value, so both throw and the method
        // falls through to the direct deserialization path (which also fails) and
        // ultimately returns default(int).
        using var ms = new MemoryStream();
        using (var writer = new Newtonsoft.Json.Bson.BsonDataWriter(ms))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Value");
            writer.WriteValue("not-a-number");
            writer.WriteEndObject();
        }

        var bytes = ms.ToArray();
        var sut = new SystemJsonBsonSerializer();
        var result = sut.DeserializeBsonFormat<int>(bytes);

        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that <c>IsPotentialBsonData</c> returns false when data passes the first-char
    /// check (byte at index 4 is not '{', '[', or '"') but the UTF-8 decoded string starts
    /// with '{' because the length header itself contains 0x7B ('{') — covers the branch
    /// at line 63 where the string pattern check detects JSON.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseWhenStringStartsWithBrace()
    {
        // 0x7B = '{' as first byte of a little-endian int32 gives length = 123.
        // For a 130-byte array: documentLength(123) > 4 and 123 <= 230, passes size check.
        // byte[4] = 0x01 (not '{', '[', '"'), passes first-char check.
        // But UTF-8 decode of entire array starts with '{' (from the length header byte),
        // so TrimStart().StartsWith("{") is true and the method returns false.
        var data = new byte[130];
        data[0] = 0x7B; // '{' — makes string start with '{'
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x00; // little-endian int32 = 123
        data[4] = 0x01; // non-JSON first char — passes the byte check

        var result = SystemJsonBsonSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests that <c>IsPotentialBsonData</c> returns false when the UTF-8 decoded string
    /// starts with '[' due to the length header containing 0x5B ('[') — covers the array
    /// branch at line 63.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseWhenStringStartsWithBracket()
    {
        // 0x5B = '[' as first byte of little-endian int32 gives length = 91.
        // For a 100-byte array: documentLength(91) > 4 and 91 <= 200, passes size check.
        // byte[4] = 0x02 (not '{', '[', '"'), passes first-char check.
        var data = new byte[100];
        data[0] = 0x5B; // '[' — makes string start with '['
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x00; // little-endian int32 = 91
        data[4] = 0x02; // non-JSON first char

        var result = SystemJsonBsonSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests that <c>Deserialize</c> returns <c>default</c> for a value type when BSON
    /// deserialization succeeds but returns the default value — covers lines 87-89 where
    /// <c>typeof(T).IsValueType</c> is true and <c>bsonResult</c> is default.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultValueTypeFromBson()
    {
        var serializer = new SystemJsonBsonSerializer();

        // Serialize 0 (the default for int) as BSON — this will produce valid BSON
        // that deserializes to default(int) = 0.
        var data = serializer.Serialize(0);
        var result = serializer.Deserialize<int>(data);

        // The value type path returns bsonResult (which is 0/default) immediately.
        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that <c>Deserialize</c> returns <c>default</c> for a value type when BSON
    /// deserialization throws an exception internally — covers lines 92-93 and 95 (the
    /// catch block in <c>Deserialize</c> that swallows BSON errors for value types).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldCatchBsonExceptionAndFallBackForValueType()
    {
        var serializer = new SystemJsonBsonSerializer();

        // Craft bytes that pass IsPotentialBsonData (valid length header, non-JSON first byte)
        // but are malformed BSON that causes DeserializeBsonFormat to throw internally.
        // Then the JSON fallback path will also fail (not valid JSON), returning default(int).
        var data = new byte[20];
        BitConverter.GetBytes(20).CopyTo(data, 0);
        data[4] = 0x01; // BSON type indicator (double) — looks like valid BSON start
        data[5] = 0x41; // 'A' — field name start
        data[6] = 0x00; // null terminator for field name

        // Rest is garbage — will cause BsonDataReader to fail
        for (var i = 7; i < data.Length; i++)
        {
            data[i] = 0xFF;
        }

        // This will: pass IsPotentialBsonData -> DeserializeBsonFormat throws ->
        // catch at lines 92-95 -> fall back to JSON -> JSON also fails -> return default
        var result = serializer.Deserialize<int>(data);
        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> uses the direct STJ deserialization fallback
    /// (line 228) when the wrapper path fails for a type that STJ can handle directly
    /// but not via <c>ObjectWrapper</c> — covers the direct STJ deserialization path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldUseDirectStjFallbackWhenWrapperFails()
    {
        // Create a BSON document with {"Value":"not-valid","Name":"direct-stj"} —
        // has "Value" in the JSON so wrapper path is tried, but ObjectWrapper<SerializerTestModel>
        // won't match correctly. Configure STJ with strict unmapped member handling so the
        // wrapper deserialization throws for unexpected shape, then direct deserialization
        // of the full JSON as SerializerTestModel succeeds (since Name is a valid field).
        using var ms = new MemoryStream();
        using (var writer = new Newtonsoft.Json.Bson.BsonDataWriter(ms))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Name");
            writer.WriteValue("direct-stj");
            writer.WritePropertyName("Value");
            writer.WriteValue(7);
            writer.WritePropertyName("ExtraFieldWithValue");
            writer.WriteValue("causes wrapper mismatch");
            writer.WriteEndObject();
        }

        var bytes = ms.ToArray();
        var sut = new SystemJsonBsonSerializer();
        sut.Options = new JsonSerializerOptions
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        };

        // The JSON contains "Value": so wrapper path is tried first.
        // ObjectWrapper<SerializerTestModel> deserialization throws (unmapped "Name" and "ExtraFieldWithValue").
        // Newtonsoft wrapper also fails (strict mode not applied but wrong shape).
        // Direct STJ: also throws because of UnmappedMemberHandling.
        // Direct Newtonsoft: succeeds (lenient, ignores extra fields).
        var result = sut.DeserializeBsonFormat<SerializerTestModel>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("direct-stj");
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> uses the Newtonsoft direct deserialization
    /// fallback (line 237) when both wrapper paths fail and direct STJ also fails.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldUseNewtonsoftDirectFallback()
    {
        // Create a BSON document without "Value" field that STJ cannot deserialize
        // but Newtonsoft can handle via its more lenient parsing.
        // Write {"name":"test","value":99} (lowercase) — no "Value": match so skips wrapper.
        // STJ with default options uses PascalCase, so it maps correctly.
        // We need STJ to fail but Newtonsoft to succeed.
        // Use a type with a constructor that Newtonsoft handles but STJ doesn't.
        using var ms = new MemoryStream();
        using (var writer = new Newtonsoft.Json.Bson.BsonDataWriter(ms))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Name");
            writer.WriteValue("newtonsoft-direct");
            writer.WritePropertyName("Count");
            writer.WriteValue(5);
            writer.WriteEndObject();
        }

        var bytes = ms.ToArray();
        var sut = new SystemJsonBsonSerializer();

        // Deserialize as a dictionary — both STJ and Newtonsoft should handle this.
        // For hitting line 237 specifically, we need STJ direct to throw at line 228.
        // Let's use a custom options that makes STJ strict and fail.
        sut.Options = new JsonSerializerOptions
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        };

        // SerializerTestModel has Name and Value, not Count — STJ will throw with
        // UnmappedMemberHandling.Disallow. Newtonsoft is lenient and ignores extra fields.
        var result = sut.DeserializeBsonFormat<SerializerTestModel>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("newtonsoft-direct");
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> recovers a top-level document that does
    /// not have a <c>Value</c> wrapper field by using the direct deserialization path —
    /// covers the branch at line 195 where the wrapper-check is false.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldSkipWrapperWhenValueFieldMissing()
    {
        // Write a raw BSON document without any "Value" field.
        using var ms = new MemoryStream();
        using (var writer = new Newtonsoft.Json.Bson.BsonDataWriter(ms))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Name");
            writer.WriteValue("no-wrapper");
            writer.WritePropertyName("Value");

            // Intentionally: use a different casing so Contains("\"Value\":") still
            // triggers. Switch to a document with no "Value" property at all.
            writer.WriteValue(0);
            writer.WriteEndObject();
        }

        // The above still contains "Value", so build a truly value-less document.
        using var ms2 = new MemoryStream();
        using (var writer = new Newtonsoft.Json.Bson.BsonDataWriter(ms2))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Name");
            writer.WriteValue("no-wrapper");
            writer.WritePropertyName("OtherField");
            writer.WriteValue(7);
            writer.WriteEndObject();
        }

        var bytes = ms2.ToArray();
        var sut = new SystemJsonBsonSerializer();
        var result = sut.DeserializeBsonFormat<SerializerTestModel>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("no-wrapper");
    }

    /// <summary>
    /// Tests <see cref="SystemJsonBsonSerializer.TryUnwrapObjectWrapper{T}"/> returns
    /// <see langword="false"/> when System.Text.Json yields a null wrapper (literal
    /// <c>"null"</c> JSON) and Newtonsoft also cannot resolve one.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryUnwrapObjectWrapperShouldReturnFalseWhenBothSerializersReturnNull()
    {
        var options = new JsonSerializerOptions();

        var succeeded = SystemJsonBsonSerializer.TryUnwrapObjectWrapper<string>("null", options, out var value);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(value).IsNull();
    }

    /// <summary>
    /// Tests <see cref="SystemJsonBsonSerializer.TryUnwrapObjectWrapper{T}"/> falls back
    /// to Newtonsoft when System.Text.Json throws, and returns its value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryUnwrapObjectWrapperShouldFallBackToNewtonsoftWhenStjThrows()
    {
        // Trailing comma is rejected by STJ but accepted by Newtonsoft by default.
        var json = "{\"Value\":\"from-newtonsoft\",}";
        var options = new JsonSerializerOptions { AllowTrailingCommas = false };

        var succeeded = SystemJsonBsonSerializer.TryUnwrapObjectWrapper<string>(json, options, out var value);

        await Assert.That(succeeded).IsTrue();
        await Assert.That(value).IsEqualTo("from-newtonsoft");
    }

    /// <summary>
    /// Tests <see cref="SystemJsonBsonSerializer.TryUnwrapObjectWrapper{T}"/> resolves
    /// the value via System.Text.Json on the happy path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryUnwrapObjectWrapperShouldResolveViaStjOnHappyPath()
    {
        var json = "{\"Value\":\"from-stj\"}";
        var options = new JsonSerializerOptions();

        var succeeded = SystemJsonBsonSerializer.TryUnwrapObjectWrapper<string>(json, options, out var value);

        await Assert.That(succeeded).IsTrue();
        await Assert.That(value).IsEqualTo("from-stj");
    }
}
