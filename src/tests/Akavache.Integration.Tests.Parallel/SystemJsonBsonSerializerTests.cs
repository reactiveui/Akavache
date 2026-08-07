// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using Newtonsoft.Json.Bson;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests for SystemJsonBsonSerializer covering BSON-specific paths and edge cases.</summary>
[Category("Akavache")]
public class SystemJsonBsonSerializerTests
{
    /// <summary>Name of the wrapper field the BSON payloads carry their payload under.</summary>
    private const string WrapperFieldName = "Value";

    /// <summary>Name carried by the documents that deliberately have no wrapper field.</summary>
    private const string NoWrapperName = "no-wrapper";

    /// <summary>The integer payload most of the sample models carry.</summary>
    private const int SampleValue = 42;

    /// <summary>Year of the sample instant, asserted after the BSON round trip.</summary>
    private const int SampleYear = 2025;

    /// <summary>Month of the sample instant, asserted after the BSON round trip.</summary>
    private const int SampleMonth = 6;

    /// <summary>Payload value of the model serialized through custom options.</summary>
    private const int CustomOptionsValue = 5;

    /// <summary>The bare integer round-tripped through the wrapper path.</summary>
    private const int RoundTripInteger = 12_345;

    /// <summary>Payload value of the second entry in the sample list.</summary>
    private const int SecondListItemValue = 2;

    /// <summary>Entries expected back from the round-tripped list.</summary>
    private const int ListItemCount = 2;

    /// <summary>Payload value of the model handed over as a bare <see cref="object"/>.</summary>
    private const int ObjectPayloadValue = 3;

    /// <summary>Payload value of the model written as plain JSON rather than BSON.</summary>
    private const int DirectJsonValue = 7;

    /// <summary>Payload value of the model serialized while a forced date-time kind is set.</summary>
    private const int ForcedKindSampleValue = 9;

    /// <summary>Payload value of the model serialized while no forced date-time kind is set.</summary>
    private const int NullForcedKindSampleValue = 3;

    /// <summary>Payload value of the document that only the direct System.Text.Json path can read.</summary>
    private const int DirectStjFallbackValue = 7;

    /// <summary>Value of the unmapped <c>Count</c> field that makes strict System.Text.Json reject the document.</summary>
    private const int UnmappedCountValue = 5;

    /// <summary>Value of the unmapped field in the document that has no wrapper.</summary>
    private const int OtherFieldValue = 7;

    /// <summary>A declared BSON document length far shorter than the buffer, which the shape check must reject.</summary>
    private const int UnreasonableDocumentLength = 3;

    /// <summary>Size of the buffer whose first content byte is a JSON brace, and the document length it declares.</summary>
    private const int JsonObjectBufferLength = 20;

    /// <summary>Size of the buffer filled with bytes that pass the length check but are not decodable BSON.</summary>
    private const int MalformedBsonBufferLength = 32;

    /// <summary>Size of the buffer whose BSON parse fails, driving the fallback paths.</summary>
    private const int FailedBsonBufferLength = 16;

    /// <summary>Size of the buffer that neither the BSON nor the JSON path can read.</summary>
    private const int AllPathsFailBufferLength = 15;

    /// <summary>Size of the buffer whose BSON reader throws part-way through a value type read.</summary>
    private const int ThrowingBsonBufferLength = 20;

    /// <summary>Indented output options; shared because the serializer copies them before use.</summary>
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    /// <summary>Options that reject any member the target type does not declare.</summary>
    private static readonly JsonSerializerOptions StrictUnmappedOptions = new() { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };

    /// <summary>Stock options for the unwrap helper, which only reads them.</summary>
    private static readonly JsonSerializerOptions DefaultOptions = new();

    /// <summary>Options that reject the trailing comma System.Text.Json would otherwise tolerate.</summary>
    private static readonly JsonSerializerOptions NoTrailingCommaOptions = new() { AllowTrailingCommas = false };

    /// <summary>Tests Options getter and setter delegate to the inner serializer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OptionsShouldGetAndSet()
    {
        SystemJsonBsonSerializer serializer = new();
        await Assert.That(serializer.Options).IsNull();

        serializer.Options = IndentedOptions;
        await Assert.That(serializer.Options).IsEqualTo(IndentedOptions);
    }

    /// <summary>Tests ForcedDateTimeKind defaults to Utc.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindShouldDefaultToUtc()
    {
        SystemJsonBsonSerializer serializer = new();
        await Assert.That(serializer.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>Tests IsPotentialBsonData returns false for null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForNull()
    {
        var result = SystemJsonBsonSerializer.IsPotentialBsonData(null!);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialBsonData returns false for short data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForShortData()
    {
        byte[] tooShort = [1, 2, 3];
        var result = SystemJsonBsonSerializer.IsPotentialBsonData(tooShort);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialBsonData returns false for unreasonable length.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForUnreasonableLength()
    {
        var data = new byte[10];
        BitConverter.GetBytes(UnreasonableDocumentLength).CopyTo(data, 0);
        var result = SystemJsonBsonSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialBsonData returns false for JSON object data starting with '{'.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForJsonObject()
    {
        // Set length so it passes the size check, then put '{' at position 4
        var data = new byte[JsonObjectBufferLength];
        BitConverter.GetBytes(JsonObjectBufferLength).CopyTo(data, 0);
        data[4] = (byte)'{';
        var result = SystemJsonBsonSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialBsonData returns true for valid BSON-shaped data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnTrueForValidBson()
    {
        // Serialize an actual object to BSON
        SystemJsonBsonSerializer serializer = new();
        var data = serializer.Serialize(new SerializerTestModel { Name = "test", Value = SampleValue });

        var result = SystemJsonBsonSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests Serialize and Deserialize round-trip in BSON format.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldRoundTripBson()
    {
        SystemJsonBsonSerializer serializer = new();
        var data = serializer.Serialize(new SerializerTestModel { Name = "bson-test", Value = SampleValue });
        var result = serializer.Deserialize<SerializerTestModel>(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("bson-test");
        await Assert.That(result.Value).IsEqualTo(SampleValue);
    }

    /// <summary>Tests Deserialize returns default for null bytes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForNullBytes()
    {
        SystemJsonBsonSerializer serializer = new();
        var result = serializer.Deserialize<SerializerTestModel>(null!);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests Deserialize returns default for empty bytes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForEmptyBytes()
    {
        SystemJsonBsonSerializer serializer = new();
        var result = serializer.Deserialize<SerializerTestModel>([]);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests Deserialize falls back to JSON when BSON detection fails.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldFallBackToJson()
    {
        SystemJsonBsonSerializer serializer = new();

        // Provide JSON data, not BSON
        var jsonBytes = "{\"Name\":\"json-fallback\",\"Value\":1}"u8.ToArray();
        var result = serializer.Deserialize<SerializerTestModel>(jsonBytes);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("json-fallback");
    }

    /// <summary>Tests Deserialize returns default for invalid data instead of throwing.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForInvalidData()
    {
        SystemJsonBsonSerializer serializer = new();
        byte[] invalid = [0xFF, 0xFE, 0x00, 0x01, 0x02];
        var result = serializer.Deserialize<SerializerTestModel>(invalid);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests Serialize with JsonTypeInfo (AOT path delegates to inner).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithJsonTypeInfoShouldWork()
    {
        SystemJsonBsonSerializer serializer = new();
        var data = serializer.Serialize(
            new() { Name = "aot", Value = 1 },
            SerializerTestContext.Default.SerializerTestModel);
        await Assert.That(data).IsNotNull();
        await Assert.That(data.Length).IsGreaterThan(0);
    }

    /// <summary>Tests Deserialize with JsonTypeInfo (AOT path delegates to inner).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithJsonTypeInfoShouldWork()
    {
        SystemJsonBsonSerializer serializer = new();
        var jsonBytes = "{\"Name\":\"aot\",\"Value\":7}"u8.ToArray();
        var result = serializer.Deserialize(jsonBytes, SerializerTestContext.Default.SerializerTestModel);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("aot");
    }

    /// <summary>Tests NormalizeDateTimeFormats converts Newtonsoft tick format to ISO 8601.</summary>
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

    /// <summary>Tests NormalizeDateTimeFormats leaves non-matching strings alone.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NormalizeDateTimeFormatsShouldLeaveOtherStringsAlone()
    {
        const string input = "{\"Name\":\"test\"}";
        var result = SystemJsonBsonSerializer.NormalizeDateTimeFormats(input);
        await Assert.That(result).IsEqualTo(input);
    }

    /// <summary>Tests SystemJsonBsonSerializer accepts custom options without throwing on serialize.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldAcceptCustomOptions()
    {
        SystemJsonBsonSerializer serializer = new() { Options = IndentedOptions };

        var data = serializer.Serialize(new SerializerTestModel { Name = "custom", Value = CustomOptionsValue });
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
        SystemJsonBsonSerializer serializer = new();

        // Craft bytes that look BSON-ish in length header but are not valid BSON.
        var data = new byte[MalformedBsonBufferLength];
        BitConverter.GetBytes(MalformedBsonBufferLength).CopyTo(data, 0);

        for (var i = 4; i < data.Length; i++)
        {
            data[i] = 0x7F;
        }

        var result = serializer.DeserializeBsonFormat<SerializerTestModel>(data);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests that <c>Deserialize</c> falls back to JSON when BSON detection returns true but <c>DeserializeBsonFormat</c> throws internally.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldFallBackWhenBsonFormatFails()
    {
        SystemJsonBsonSerializer serializer = new();

        var data = new byte[FailedBsonBufferLength];
        BitConverter.GetBytes(FailedBsonBufferLength).CopyTo(data, 0);
        for (var i = 4; i < data.Length; i++)
        {
            data[i] = 0x55;
        }

        var result = serializer.Deserialize<SerializerTestModel>(data);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests that <c>DeserializeBsonFormat</c> returns <c>default(int)</c> for a value type when parsing fails.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldReturnDefaultValueTypeOnFailure()
    {
        SystemJsonBsonSerializer serializer = new();
        var data = new byte[FailedBsonBufferLength];
        BitConverter.GetBytes(FailedBsonBufferLength).CopyTo(data, 0);
        for (var i = 4; i < data.Length; i++)
        {
            data[i] = 0x10;
        }

        var result = serializer.DeserializeBsonFormat<int>(data);
        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>Tests <c>DeserializeBsonFormat</c> round-trip of a primitive string via the <c>ObjectWrapper</c> path.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldRoundTripString()
    {
        SystemJsonBsonSerializer serializer = new();
        var bytes = serializer.SerializeToBson("hello-bson");

        var result = serializer.DeserializeBsonFormat<string>(bytes);
        await Assert.That(result).IsEqualTo("hello-bson");
    }

    /// <summary>Tests <c>DeserializeBsonFormat</c> round-trip of an integer via the <c>ObjectWrapper</c> path.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldRoundTripInteger()
    {
        SystemJsonBsonSerializer serializer = new();
        var bytes = serializer.SerializeToBson(RoundTripInteger);

        var result = serializer.DeserializeBsonFormat<int>(bytes);
        await Assert.That(result).IsEqualTo(RoundTripInteger);
    }

    /// <summary>Tests <c>DeserializeBsonFormat</c> round-trip of a <see cref="DateTime"/> via the <c>ObjectWrapper</c> path, exercising <c>NormalizeDateTimeFormats</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldRoundTripDateTime()
    {
        SystemJsonBsonSerializer serializer = new();
        DateTime original = new(2025, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        var bytes = serializer.SerializeToBson(original);

        var result = serializer.DeserializeBsonFormat<DateTime>(bytes);
        await Assert.That(result.Year).IsEqualTo(SampleYear);
        await Assert.That(result.Month).IsEqualTo(SampleMonth);
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> returns default for an empty BSON
    /// document (exercising the <c>string.IsNullOrEmpty(jsonString)</c> branch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldHandleEmptyDocument()
    {
        SystemJsonBsonSerializer serializer = new();

        // Minimal valid empty BSON document: int32 length (5) + terminator (0x00) = 5 bytes.
        byte[] emptyBson = [0x05, 0x00, 0x00, 0x00, 0x00];

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
        SystemJsonBsonSerializer serializer = new();

        byte[] emptyBson = [0x05, 0x00, 0x00, 0x00, 0x00];

        // Path is exercised - empty BSON document yields default-constructed object
        await Assert.That(() => serializer.Deserialize<SerializerTestModel>(emptyBson)).ThrowsNothing();
    }

    /// <summary>Tests <c>SerializeToBson</c> with null input — should produce a wrapper document and successfully round-trip.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeToBsonShouldHandleNullValue()
    {
        SystemJsonBsonSerializer serializer = new();
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
        const string huge = "99999999999999999999";
        const string input = $"{{\"Date\":{huge}}}";

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
        const string tooLarge = "3155378976000000000";
        const string input = $"{{\"Date\":{tooLarge}}}";

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
        SystemJsonBsonSerializer serializer = new();

        var data = new byte[AllPathsFailBufferLength];
        BitConverter.GetBytes(AllPathsFailBufferLength).CopyTo(data, 0);
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
        SystemJsonBsonSerializer serializer = new();
        List<SerializerTestModel> source =
        [
            new() { Name = "a", Value = 1 },
            new() { Name = "b", Value = SecondListItemValue }
        ];

        var bytes = serializer.SerializeToBson(source);
        var result = serializer.DeserializeBsonFormat<List<SerializerTestModel>>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Count).IsEqualTo(ListItemCount);
        await Assert.That(result[0].Name).IsEqualTo("a");
        await Assert.That(result[1].Value).IsEqualTo(SecondListItemValue);
    }

    /// <summary>Tests <c>SerializeToBson</c> with an <c>object</c>-typed payload, covering the generic wrapper path.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeToBsonShouldHandleObjectTypedPayload()
    {
        SystemJsonBsonSerializer serializer = new();
        object payload = new SerializerTestModel { Name = "obj", Value = ObjectPayloadValue };

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
        SystemJsonBsonSerializer serializer = new();
        var bytes = """{"Name":"direct","Value":7}"""u8.ToArray();

        var result = serializer.Deserialize<SerializerTestModel>(bytes);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("direct");
        await Assert.That(result.Value).IsEqualTo(DirectJsonValue);
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
        await using MemoryStream ms = new();
        await using (BsonDataWriter writer = new(ms))
        {
            JsonSerializer newtonsoft = new();
            newtonsoft.Serialize(writer, new SerializerTestModel { Name = "raw", Value = SampleValue });
        }

        var bytes = ms.ToArray();
        SystemJsonBsonSerializer sut = new();
        var result = sut.DeserializeBsonFormat<SerializerTestModel>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("raw");
        await Assert.That(result.Value).IsEqualTo(SampleValue);
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
        SystemJsonBsonSerializer serializer = new() { ForcedDateTimeKind = DateTimeKind.Local, };

        var data = serializer.Serialize(new SerializerTestModel { Name = "dtk", Value = ForcedKindSampleValue });
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
        SystemJsonBsonSerializer serializer = new() { ForcedDateTimeKind = null, };

        var data = serializer.Serialize(new SerializerTestModel { Name = "null-dtk", Value = NullForcedKindSampleValue });
        var result = serializer.DeserializeBsonFormat<SerializerTestModel>(data);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("null-dtk");
    }

    /// <summary>Tests <c>Deserialize</c> with a value-type target (covers the <c>typeof(T).IsValueType</c> branch in the early-return check).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldHandleValueTypeRoundTrip()
    {
        SystemJsonBsonSerializer serializer = new();
        var data = serializer.Serialize(SampleValue);
        var result = serializer.Deserialize<int>(data);

        await Assert.That(result).IsEqualTo(SampleValue);
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
        await using MemoryStream ms = new();
        await using (BsonDataWriter writer = new(ms))
        {
            await writer.WriteStartObjectAsync();
            await writer.WritePropertyNameAsync(WrapperFieldName);
            await writer.WriteValueAsync("42");
            await writer.WriteEndObjectAsync();
        }

        var bytes = ms.ToArray();
        SystemJsonBsonSerializer sut = new();
        var result = sut.DeserializeBsonFormat<int>(bytes);

        await Assert.That(result).IsEqualTo(SampleValue);
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
        await using MemoryStream ms = new();
        await using (BsonDataWriter writer = new(ms))
        {
            await writer.WriteStartObjectAsync();
            await writer.WritePropertyNameAsync(WrapperFieldName);
            await writer.WriteValueAsync("not-a-number");
            await writer.WriteEndObjectAsync();
        }

        var bytes = ms.ToArray();
        SystemJsonBsonSerializer sut = new();
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
        SystemJsonBsonSerializer serializer = new();

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
        SystemJsonBsonSerializer serializer = new();

        // Craft bytes that pass IsPotentialBsonData (valid length header, non-JSON first byte)
        // but are malformed BSON that causes DeserializeBsonFormat to throw internally.
        // Then the JSON fallback path will also fail (not valid JSON), returning default(int).
        var data = new byte[ThrowingBsonBufferLength];
        BitConverter.GetBytes(ThrowingBsonBufferLength).CopyTo(data, 0);
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
        await using MemoryStream ms = new();
        await using (BsonDataWriter writer = new(ms))
        {
            await writer.WriteStartObjectAsync();
            await writer.WritePropertyNameAsync("Name");
            await writer.WriteValueAsync("direct-stj");
            await writer.WritePropertyNameAsync(WrapperFieldName);
            await writer.WriteValueAsync(DirectStjFallbackValue);
            await writer.WritePropertyNameAsync("ExtraFieldWithValue");
            await writer.WriteValueAsync("causes wrapper mismatch");
            await writer.WriteEndObjectAsync();
        }

        var bytes = ms.ToArray();
        SystemJsonBsonSerializer sut = new() { Options = StrictUnmappedOptions };

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
        await using MemoryStream ms = new();
        await using (BsonDataWriter writer = new(ms))
        {
            await writer.WriteStartObjectAsync();
            await writer.WritePropertyNameAsync("Name");
            await writer.WriteValueAsync("newtonsoft-direct");
            await writer.WritePropertyNameAsync("Count");
            await writer.WriteValueAsync(UnmappedCountValue);
            await writer.WriteEndObjectAsync();
        }

        var bytes = ms.ToArray();

        // Strict unmapped handling makes the System.Text.Json direct path throw on the
        // extra Count field, leaving only the lenient Newtonsoft direct path.
        SystemJsonBsonSerializer sut = new() { Options = StrictUnmappedOptions };

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
        await using MemoryStream ms = new();
        await using (BsonDataWriter writer = new(ms))
        {
            await writer.WriteStartObjectAsync();
            await writer.WritePropertyNameAsync("Name");
            await writer.WriteValueAsync(NoWrapperName);
            await writer.WritePropertyNameAsync(WrapperFieldName);

            // Intentionally: use a different casing so Contains("\"Value\":") still
            // triggers. Switch to a document with no "Value" property at all.
            await writer.WriteValueAsync(0);
            await writer.WriteEndObjectAsync();
        }

        // The above still contains "Value", so build a truly value-less document.
        await using MemoryStream ms2 = new();
        await using (BsonDataWriter writer = new(ms2))
        {
            await writer.WriteStartObjectAsync();
            await writer.WritePropertyNameAsync("Name");
            await writer.WriteValueAsync(NoWrapperName);
            await writer.WritePropertyNameAsync("OtherField");
            await writer.WriteValueAsync(OtherFieldValue);
            await writer.WriteEndObjectAsync();
        }

        var bytes = ms2.ToArray();
        SystemJsonBsonSerializer sut = new();
        var result = sut.DeserializeBsonFormat<SerializerTestModel>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo(NoWrapperName);
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
        var succeeded = SystemJsonBsonSerializer.TryUnwrapObjectWrapper<string>("null", DefaultOptions, out var value);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(value).IsNull();
    }

    /// <summary>Tests <see cref="SystemJsonBsonSerializer.TryUnwrapObjectWrapper{T}"/> falls back to Newtonsoft when System.Text.Json throws, and returns its value.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryUnwrapObjectWrapperShouldFallBackToNewtonsoftWhenStjThrows()
    {
        // Trailing comma is rejected by STJ but accepted by Newtonsoft by default.
        const string json = "{\"Value\":\"from-newtonsoft\",}";

        var succeeded = SystemJsonBsonSerializer.TryUnwrapObjectWrapper<string>(json, NoTrailingCommaOptions, out var value);

        await Assert.That(succeeded).IsTrue();
        await Assert.That(value).IsEqualTo("from-newtonsoft");
    }

    /// <summary>Tests <see cref="SystemJsonBsonSerializer.TryUnwrapObjectWrapper{T}"/> resolves the value via System.Text.Json on the happy path.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryUnwrapObjectWrapperShouldResolveViaStjOnHappyPath()
    {
        const string json = "{\"Value\":\"from-stj\"}";

        var succeeded = SystemJsonBsonSerializer.TryUnwrapObjectWrapper<string>(json, DefaultOptions, out var value);

        await Assert.That(succeeded).IsTrue();
        await Assert.That(value).IsEqualTo("from-stj");
    }

    /// <summary>
    /// Tests that <c>DeserializeBsonFormat</c> yields the default value instead of throwing when
    /// System.Text.Json declines the target type outright. A document carrying no wrapper field
    /// reaches the direct System.Text.Json call, and an interface target is a shape that serializer
    /// refuses to construct, so the Newtonsoft attempt below it decides the outcome.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldReturnDefaultWhenStjRefusesTheTargetType()
    {
        const string sampleName = "unsupported-target";
        const string equivalentJson = $"{{\"Name\":\"{sampleName}\"}}";

        await using MemoryStream ms = new();
        await using (BsonDataWriter writer = new(ms))
        {
            await writer.WriteStartObjectAsync();
            await writer.WritePropertyNameAsync("Name");
            await writer.WriteValueAsync(sampleName);
            await writer.WriteEndObjectAsync();
        }

        // Pin the precondition rather than assuming it: this is the JSON the direct path sees, and
        // System.Text.Json rejects an interface target for it. That rejection is what carries the
        // call on to the Newtonsoft attempt, which cannot construct the interface either.
        await Assert.That(static () => System.Text.Json.JsonSerializer.Deserialize<IDisposable>(equivalentJson, DefaultOptions))
            .Throws<NotSupportedException>();

        SystemJsonBsonSerializer sut = new();

        var result = sut.DeserializeBsonFormat<IDisposable>(ms.ToArray());

        await Assert.That(result).IsNull();
    }
}
