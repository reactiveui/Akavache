// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.Tests.Mocks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Akavache.Tests;

/// <summary>
/// Tests for NewtonsoftSerializer covering BSON detection, format detection, and edge cases.
/// </summary>
[Category("Akavache")]
public class NewtonsoftSerializerTests
{
    /// <summary>
    /// Tests IsPotentialBsonData returns false for null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForNull()
    {
        var result = NewtonsoftSerializer.IsPotentialBsonData(null!);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialBsonData returns false for short data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForShortData()
    {
        var result = NewtonsoftSerializer.IsPotentialBsonData([1, 2, 3]);
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
        var result = NewtonsoftSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialBsonData returns false for JSON object.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForJsonObject()
    {
        var data = new byte[20];
        BitConverter.GetBytes(20).CopyTo(data, 0);
        data[4] = (byte)'{';
        var result = NewtonsoftSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialBsonData returns true for valid BSON-shaped data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnTrueForValidBson()
    {
        var serializer = new NewtonsoftBsonSerializer();
        var data = serializer.Serialize(new UserObject { Name = "test", Bio = "bio", Blog = "blog" });

        var result = NewtonsoftSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests Deserialize returns default for null bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForNullBytes()
    {
        var serializer = new NewtonsoftSerializer();
        var result = serializer.Deserialize<UserObject>(null!);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests Deserialize returns default for empty bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForEmptyBytes()
    {
        var serializer = new NewtonsoftSerializer();
        var result = serializer.Deserialize<UserObject>([]);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests Deserialize falls back to BSON when data is BSON-shaped.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldFallBackToBsonForBsonData()
    {
        var bsonSerializer = new NewtonsoftBsonSerializer();
        var testObj = new UserObject { Name = "bson", Bio = "bio", Blog = "blog" };
        var bsonData = bsonSerializer.Serialize(testObj);

        var jsonSerializer = new NewtonsoftSerializer();
        var result = jsonSerializer.Deserialize<UserObject>(bsonData);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("bson");
    }

    /// <summary>
    /// Tests Deserialize returns default for invalid data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultForInvalidData()
    {
        var serializer = new NewtonsoftSerializer();
        var invalid = new byte[] { 0xFF, 0xFE, 0x00, 0x01, 0x02 };
        var result = serializer.Deserialize<UserObject>(invalid);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests SerializeToBson with custom settings.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeToBsonShouldUseCustomSettings()
    {
        var serializer = new NewtonsoftSerializer
        {
            UseBsonFormat = true,
            Options = new JsonSerializerSettings { Formatting = Formatting.Indented }
        };

        var testObj = new UserObject { Name = "test", Bio = "bio", Blog = "blog" };
        var data = serializer.Serialize(testObj);

        await Assert.That(data).IsNotNull();
        await Assert.That(data.Length).IsGreaterThan(0);
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats handles ObjectWrapper format.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldHandleObjectWrapper()
    {
        var serializer = new NewtonsoftSerializer();
        var json = "{\"Value\":{\"Name\":\"wrapped\",\"Bio\":\"bio\",\"Blog\":\"blog\"}}";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = serializer.TryDeserializeFromOtherFormats<UserObject>(bytes);
        await Assert.That(result).IsNotNull();
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats returns default for non-JSON-looking data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldReturnDefaultForNonJson()
    {
        var serializer = new NewtonsoftSerializer();
        var bytes = Encoding.UTF8.GetBytes("not json data");

        var result = serializer.TryDeserializeFromOtherFormats<UserObject>(bytes);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats returns default for whitespace.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldReturnDefaultForWhitespace()
    {
        var serializer = new NewtonsoftSerializer();
        var bytes = Encoding.UTF8.GetBytes("   ");

        var result = serializer.TryDeserializeFromOtherFormats<UserObject>(bytes);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests round-trip with custom settings.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldRoundTripWithCustomSettings()
    {
        var serializer = new NewtonsoftSerializer
        {
            Options = new JsonSerializerSettings { Formatting = Formatting.Indented }
        };

        var testObj = new UserObject { Name = "test", Bio = "bio", Blog = "blog" };
        var bytes = serializer.Serialize(testObj);
        var result = serializer.Deserialize<UserObject>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("test");
    }

    /// <summary>
    /// Tests SerializeToBson falls back to JSON when BSON serialization throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeToBsonShouldFallBackToJsonOnFailure()
    {
        var converter = new BsonOnlyThrowingConverter();
        var serializer = new NewtonsoftSerializer
        {
            UseBsonFormat = true,
            Options = new JsonSerializerSettings
            {
                Converters = { converter }
            }
        };

        var testObj = new UserObject { Name = "fallback", Bio = "bio", Blog = "blog" };
        var data = serializer.Serialize(testObj);

        await Assert.That(data).IsNotNull();
        await Assert.That(data.Length).IsGreaterThan(0);

        // The BSON path threw, so the fallback produced JSON bytes starting with '{'.
        var asString = Encoding.UTF8.GetString(data);
        await Assert.That(asString.TrimStart().StartsWith("{")).IsTrue();
    }

    /// <summary>
    /// Tests DeserializeBsonFormat falls back to direct deserialization when wrapper fails.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldFallBackToDirectDeserialization()
    {
        // Serialize a UserObject directly as a BSON document (not wrapped in ObjectWrapper).
        // With MissingMemberHandling.Error, parsing as ObjectWrapper<UserObject> will throw
        // because the BSON contains unknown fields (Name, Bio, Blog). The inner catch then
        // reopens the stream and deserializes directly as UserObject, which succeeds.
        using var ms = new MemoryStream();
        await using (var writer = new BsonDataWriter(ms) { CloseOutput = false })
        {
            var inner = JsonSerializer.Create();
            inner.Serialize(writer, new UserObject { Name = "direct", Bio = "bio", Blog = "blog" });
        }

        var bytes = ms.ToArray();
        var serializer = new NewtonsoftSerializer
        {
            Options = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            }
        };

        var result = serializer.DeserializeBsonFormat<UserObject>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("direct");
    }

    /// <summary>
    /// Tests DeserializeBsonFormat returns default when BSON parsing fails entirely.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldReturnDefaultForInvalidBson()
    {
        var serializer = new NewtonsoftSerializer();
        var bytes = new byte[] { 0x10, 0x00, 0x00, 0x00, 0xFF, 0xFE, 0xFD, 0xFC };

        var result = serializer.DeserializeBsonFormat<UserObject>(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests DeserializeBsonFormat honors ForcedDateTimeKind on the inner direct deserialization path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeBsonFormatShouldHonorForcedDateTimeKindOnFallback()
    {
        using var ms = new MemoryStream();
        await using (var writer = new BsonDataWriter(ms) { CloseOutput = false })
        {
            var inner = JsonSerializer.Create();
            inner.Serialize(writer, new UserObject { Name = "kind", Bio = "b", Blog = "g" });
        }

        var bytes = ms.ToArray();
        var serializer = new NewtonsoftSerializer
        {
            ForcedDateTimeKind = DateTimeKind.Utc,
            Options = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            }
        };

        var result = serializer.DeserializeBsonFormat<UserObject>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("kind");
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats returns the BSON result when BSON decoding succeeds.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldReturnBsonResult()
    {
        var bsonSerializer = new NewtonsoftBsonSerializer();
        var testObj = new UserObject { Name = "bsonFallback", Bio = "bio", Blog = "blog" };
        var bsonData = bsonSerializer.Serialize(testObj);

        var serializer = new NewtonsoftSerializer();
        var result = serializer.TryDeserializeFromOtherFormats<UserObject>(bsonData);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("bsonFallback");
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats catches exceptions when SimpleObjectWrapper parsing fails.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldCatchWrapperParseFailure()
    {
        var serializer = new NewtonsoftSerializer();

        // Contains "Value": but the inner value is not a valid UserObject shape,
        // so SimpleObjectWrapper<UserObject> parsing throws and falls through to direct deserialization.
        var json = "{\"Value\":12345}";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = serializer.TryDeserializeFromOtherFormats<UserObject>(bytes);

        // Wrapper parse throws, falls through to direct UserObject deserialization which succeeds
        // (UserObject has no "Value" property, so it's ignored) and returns a default-valued instance.
        await Assert.That(result).IsNotNull();
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats returns a list when given a JSON array.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldHandleJsonArray()
    {
        var serializer = new NewtonsoftSerializer();

        var json = "[1,2,3]";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = serializer.TryDeserializeFromOtherFormats<int[]>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Length).IsEqualTo(3);
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats returns default when a malformed JSON object throws during direct deserialization.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldReturnDefaultForMalformedJson()
    {
        var serializer = new NewtonsoftSerializer();

        // Starts with '{' so it passes the JSON shape check, but is malformed so the
        // direct deserialization throws and the outer catch returns default.
        var json = "{\"Name\":";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = serializer.TryDeserializeFromOtherFormats<UserObject>(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats returns a wrapped value for a value type inside a SimpleObjectWrapper.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldUnwrapValueTypeWrapper()
    {
        var serializer = new NewtonsoftSerializer();
        var json = "{\"Value\":42}";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = serializer.TryDeserializeFromOtherFormats<int>(bytes);

        await Assert.That(result).IsEqualTo(42);
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats deserializes directly when the JSON does not contain a Value property.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldDeserializeDirectlyWithoutValueKey()
    {
        var serializer = new NewtonsoftSerializer();
        var json = "{\"Name\":\"direct\",\"Bio\":\"b\",\"Blog\":\"g\"}";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = serializer.TryDeserializeFromOtherFormats<UserObject>(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("direct");
    }

    /// <summary>
    /// Tests IsPotentialBsonData returns false when the first byte check passes but the full
    /// UTF-8 string starts with a JSON object pattern.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseWhenStringStartsWithJsonPattern()
    {
        // Build data where byte[0] = 0x7B ('{'), making the UTF-8 string start with '{'.
        // byte[4] is a non-JSON byte so it passes the first-char check,
        // but the string-level TrimStart().StartsWith("{") catches it.
        var data = new byte[130];
        data[0] = 0x7B; // '{' = 123 decimal, so documentLength = 123 (little-endian)
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x00;
        data[4] = 0x01; // Not '{', '[', or '"' — passes the byte-level check

        var result = NewtonsoftSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialBsonData returns false when encoding the data to UTF-8 string
    /// reveals it starts with a JSON array pattern.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseWhenStringStartsWithJsonArrayPattern()
    {
        // byte[0] = 0x5B ('['), making the UTF-8 string start with '['.
        // documentLength = 91 (little-endian), data.Length = 100 which is within range.
        // byte[4] = 0x01, so the first-char check passes.
        var data = new byte[100];
        data[0] = 0x5B; // '[' = 91 decimal
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x00;
        data[4] = 0x01;

        var result = NewtonsoftSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests Deserialize returns default(int) = 0 for a value type when BSON detection
    /// succeeds and BSON deserialization returns the default value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnDefaultValueTypeFromBsonPath()
    {
        // Serialize int 0 via BSON, then deserialize with auto-detection.
        var bsonSerializer = new NewtonsoftSerializer { UseBsonFormat = true };
        var bsonData = bsonSerializer.Serialize(0);

        var jsonSerializer = new NewtonsoftSerializer();
        var result = jsonSerializer.Deserialize<int>(bsonData);

        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>
    /// Tests Deserialize for a value type falls through BSON catch to JSON deserialization
    /// when the data looks like BSON but BSON decoding throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldFallThroughBsonCatchForValueType()
    {
        // Craft data that IsPotentialBsonData returns true for but BSON deserialization fails.
        // Then JSON deserialization succeeds for the value type.
        var jsonBytes = Encoding.UTF8.GetBytes("42");

        // Prepend a fake BSON header that makes IsPotentialBsonData return true:
        // documentLength matches data size, first content byte is non-JSON.
        var data = new byte[jsonBytes.Length + 5];
        var docLen = data.Length;
        BitConverter.GetBytes(docLen).CopyTo(data, 0);
        data[4] = 0x10; // Non-JSON byte at index 4 — BSON element type marker
        Array.Copy(jsonBytes, 0, data, 5, jsonBytes.Length);

        // IsPotentialBsonData will check string content — the full string won't start with '{' or '['
        // because the first bytes are the BSON header. BSON deserialization will fail, catch fires,
        // then JSON deserialization of the mangled bytes also fails, hitting TryDeserializeFromOtherFormats.
        var serializer = new NewtonsoftSerializer();
        var result = serializer.Deserialize<int>(data);

        // The value type default is returned when all paths fail.
        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats BSON catch block fires for value type
    /// when UseBsonFormat is false and BSON deserialization throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldCatchBsonFailureForValueType()
    {
        var serializer = new NewtonsoftSerializer();

        // Data that is not valid BSON and not valid JSON, starting with '{'
        // so it passes the JSON-looking check but fails direct JSON deserialization too.
        var json = "{\"broken";
        var bytes = Encoding.UTF8.GetBytes(json);

        // TryDeserializeFromOtherFormats: BSON attempt throws (catch at 243-244),
        // then JSON parsing of malformed data throws (outer catch returns default).
        var result = serializer.TryDeserializeFromOtherFormats<int>(bytes);

        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats with a SimpleObjectWrapper containing
    /// a valid object with a Value key, verifying the wrapper deserialization path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldReturnWrappedStringValue()
    {
        var serializer = new NewtonsoftSerializer();
        var json = "{\"Value\":\"hello\"}";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = serializer.TryDeserializeFromOtherFormats<string>(bytes);

        await Assert.That(result).IsEqualTo("hello");
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats returns the final result for a reference type
    /// that deserializes to null from JSON that does not contain a Value key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldReturnFinalNullResult()
    {
        var serializer = new NewtonsoftSerializer();

        // Valid JSON object but deserializing as string[] yields null from DeserializeObject
        // because a JSON object cannot be deserialized as a string array — this throws and
        // the outer catch returns default (null).
        var json = "{\"key\":\"value\"}";
        var bytes = Encoding.UTF8.GetBytes(json);

        var result = serializer.TryDeserializeFromOtherFormats<string[]>(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests TryDeserializeFromOtherFormats returns the BSON result for a value type
    /// when BSON deserialization succeeds with a non-default value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeFromOtherFormatsShouldReturnBsonResultForValueType()
    {
        // Serialize an int via BSON then call TryDeserializeFromOtherFormats directly.
        var bsonSerializer = new NewtonsoftSerializer { UseBsonFormat = true };
        var bsonData = bsonSerializer.Serialize(99);

        var jsonSerializer = new NewtonsoftSerializer();
        var result = jsonSerializer.TryDeserializeFromOtherFormats<int>(bsonData);

        await Assert.That(result).IsEqualTo(99);
    }

    /// <summary>
    /// Tests Deserialize returns default value type result when BSON succeeds with default int
    /// and UseBsonFormat is true, ensuring the IsValueType branch at line 98 is covered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnBsonValueTypeWhenUseBsonFormatIsTrue()
    {
        var serializer = new NewtonsoftSerializer { UseBsonFormat = true };
        var data = serializer.Serialize(7);
        var result = serializer.Deserialize<int>(data);

        await Assert.That(result).IsEqualTo(7);
    }

    /// <summary>
    /// Tests <see cref="NewtonsoftSerializer.TryUnwrapSimpleObjectWrapper{T}"/> returns
    /// <see langword="false"/> when JsonConvert yields a null wrapper (literal
    /// <c>"null"</c> JSON), which the production call site cannot reach because its
    /// <c>"Value":</c> guard excludes that shape.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryUnwrapSimpleObjectWrapperShouldReturnFalseForNullLiteral()
    {
        var settings = new JsonSerializerSettings();

        var succeeded = NewtonsoftSerializer.TryUnwrapSimpleObjectWrapper<string>("null", settings, out var value);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(value).IsNull();
    }

    /// <summary>
    /// Tests <see cref="NewtonsoftSerializer.TryUnwrapSimpleObjectWrapper{T}"/> returns
    /// <see langword="false"/> when JsonConvert throws (malformed JSON).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryUnwrapSimpleObjectWrapperShouldReturnFalseWhenDeserializerThrows()
    {
        var settings = new JsonSerializerSettings();

        var succeeded = NewtonsoftSerializer.TryUnwrapSimpleObjectWrapper<string>("{\"Value\":", settings, out var value);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(value).IsNull();
    }

    /// <summary>
    /// Tests <see cref="NewtonsoftSerializer.TryUnwrapSimpleObjectWrapper{T}"/> resolves
    /// the happy path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryUnwrapSimpleObjectWrapperShouldResolveValueOnHappyPath()
    {
        var settings = new JsonSerializerSettings();

        var succeeded = NewtonsoftSerializer.TryUnwrapSimpleObjectWrapper<string>("{\"Value\":\"hello\"}", settings, out var value);

        await Assert.That(succeeded).IsTrue();
        await Assert.That(value).IsEqualTo("hello");
    }

    /// <summary>
    /// A converter that throws only when writing via a <see cref="BsonDataWriter"/>,
    /// forcing the BSON path to fall back to JSON while allowing the JSON fallback to succeed.
    /// </summary>
    private sealed class BsonOnlyThrowingConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => objectType == typeof(UserObject);

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => null;

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (writer is BsonDataWriter)
            {
                throw new InvalidOperationException("BSON writing is not supported by this converter.");
            }

            var user = (UserObject?)value;
            writer.WriteStartObject();
            writer.WritePropertyName("Name");
            writer.WriteValue(user?.Name);
            writer.WritePropertyName("Bio");
            writer.WriteValue(user?.Bio);
            writer.WritePropertyName("Blog");
            writer.WriteValue(user?.Blog);
            writer.WriteEndObject();
        }
    }
}
