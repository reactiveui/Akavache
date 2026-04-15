// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Akavache.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Akavache.SystemTextJson;

/// <summary>
/// A BSON serializer that uses Newtonsoft.Json.Bson for BSON encoding/decoding
/// and System.Text.Json for object serialization.
/// </summary>
public partial class SystemJsonBsonSerializer : ISerializer
{
    /// <summary>The inner JSON serializer used for the JSON fallback path.</summary>
    private readonly SystemJsonSerializer _jsonSerializer = new();

    /// <summary>
    /// Gets or sets the JSON serializer options for customizing serialization behavior.
    /// </summary>
    public JsonSerializerOptions? Options
    {
        get => _jsonSerializer.Options;
        set => _jsonSerializer.Options = value;
    }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; } = DateTimeKind.Utc;

    /// <summary>
    /// Checks if data might be BSON format.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if data might be BSON.</returns>
    public static bool IsPotentialBsonData(byte[] data)
    {
        if (data is null || data.Length < 5)
        {
            return false;
        }

        var documentLength = BitConverter.ToInt32(data, 0);

        if (documentLength <= 4 || documentLength > data.Length + 100)
        {
            return false;
        }

        var firstChar = data[4];
        if (firstChar is (byte)'{' or (byte)'[' or (byte)'"')
        {
            return false;
        }

        // If the payload starts with a JSON opener (after whitespace), it's probably JSON,
        // not BSON. Byte-level probe — zero allocations.
        return !BinaryHelpers.StartsWithJsonOpener(data);
    }

    /// <summary>
    /// Deserializes from bytes using the provided <see cref="JsonTypeInfo{T}"/> for
    /// AOT-safe deserialization. BSON decoding is not AOT-friendly, so this overload
    /// routes through the sibling <see cref="SystemJsonSerializer.DeserializeAot{T}"/>
    /// method and returns a plain JSON result.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The bytes.</param>
    /// <param name="jsonTypeInfo">The JSON type information for AOT-safe deserialization.</param>
    /// <returns>The deserialized instance.</returns>
    public static T? DeserializeAot<T>(byte[] bytes, JsonTypeInfo<T> jsonTypeInfo) =>
        SystemJsonSerializer.DeserializeAot(bytes, jsonTypeInfo);

    /// <summary>
    /// Serializes to bytes using the provided <see cref="JsonTypeInfo{T}"/> for
    /// AOT-safe serialization. BSON encoding is not AOT-friendly, so this overload
    /// routes through <see cref="SystemJsonSerializer.SerializeAot{T}"/> and emits
    /// plain JSON bytes.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <param name="jsonTypeInfo">The JSON type information for AOT-safe serialization.</param>
    /// <returns>The serialized bytes.</returns>
    public static byte[] SerializeAot<T>(T item, JsonTypeInfo<T> jsonTypeInfo) =>
        SystemJsonSerializer.SerializeAot(item, jsonTypeInfo);

    /// <inheritdoc/>
    [RequiresUnreferencedCode("Reflection-based BSON deserialization. For AOT-safe paths use the JsonTypeInfo overload (JSON only).")]
    [RequiresDynamicCode("Reflection-based BSON deserialization. For AOT-safe paths use the JsonTypeInfo overload (JSON only).")]
    public T? Deserialize<T>(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        // Try BSON first
        // DeserializeBsonFormat is exception-safe and returns default on failure, so no
        // wrapping try/catch is needed here.
        if (IsPotentialBsonData(bytes))
        {
            var bsonResult = DeserializeBsonFormat<T>(bytes);
            if (bsonResult != null || typeof(T).IsValueType)
            {
                return bsonResult;
            }
        }

        // Fall back to JSON deserialization
        try
        {
            return _jsonSerializer.Deserialize<T>(bytes);
        }
        catch
        {
            return default;
        }
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode("Reflection-based BSON serialization. For AOT-safe paths use the JsonTypeInfo overload (JSON only).")]
    [RequiresDynamicCode("Reflection-based BSON serialization. For AOT-safe paths use the JsonTypeInfo overload (JSON only).")]
    public byte[] Serialize<T>(T item) => SerializeToBson(item);

    /// <summary>
    /// Normalizes DateTime formats from Newtonsoft.Json to System.Text.Json compatible format.
    /// </summary>
    /// <param name="jsonString">The JSON string to normalize.</param>
    /// <returns>Normalized JSON string.</returns>
    internal static string NormalizeDateTimeFormats(string jsonString)
    {
        var dateTimeTickPattern = GetDateRegex();

        return dateTimeTickPattern.Replace(jsonString, static match =>
        {
            if (!long.TryParse(match.Groups[1].Value, out var ticks))
            {
                return match.Value;
            }

            try
            {
                DateTime dateTime = new(ticks, DateTimeKind.Utc);
                var isoString = dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                return $"\"Date\":\"{isoString}\"";
            }
            catch
            {
                return match.Value;
            }
        });
    }

    /// <summary>
    /// Attempts to decode <paramref name="jsonString"/> as an
    /// <see cref="ObjectWrapper{T}"/>, preferring System.Text.Json and falling
    /// back to Newtonsoft.Json.
    /// </summary>
    /// <typeparam name="T">The wrapped value type.</typeparam>
    /// <param name="jsonString">The JSON string to decode.</param>
    /// <param name="options">The System.Text.Json options to use.</param>
    /// <param name="value">When this method returns <see langword="true"/>, contains the unwrapped value; otherwise <c>default</c>.</param>
    /// <returns><see langword="true"/> if either serializer produced a non-null wrapper.</returns>
    [RequiresUnreferencedCode("Reflection-based BSON deserialization.")]
    [RequiresDynamicCode("Reflection-based BSON deserialization.")]
    internal static bool TryUnwrapObjectWrapper<T>(string jsonString, JsonSerializerOptions options, out T? value)
    {
        try
        {
            var normalizedJson = NormalizeDateTimeFormats(jsonString);
            var stjWrapper = System.Text.Json.JsonSerializer.Deserialize<ObjectWrapper<T>>(normalizedJson, options);
            if (stjWrapper is not null)
            {
                value = stjWrapper.Value;
                return true;
            }
        }
        catch
        {
            // Fall through to Newtonsoft.
        }

        try
        {
            var newtonsoftWrapper = JsonConvert.DeserializeObject<ObjectWrapper<T>>(jsonString);
            if (newtonsoftWrapper is not null)
            {
                value = newtonsoftWrapper.Value;
                return true;
            }
        }
        catch
        {
            // Give up — caller falls through to direct deserialization.
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Serializes <paramref name="item"/> to BSON bytes, falling back to plain JSON on failure.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>The serialized BSON (or JSON fallback) bytes.</returns>
    [RequiresUnreferencedCode("Reflection-based BSON serialization.")]
    [RequiresDynamicCode("Reflection-based BSON serialization.")]
    internal byte[] SerializeToBson<T>(T item)
    {
        try
        {
            ObjectWrapper<T> wrapper = new(item);
            var options = _jsonSerializer.GetEffectiveOptions();
            var jsonString = System.Text.Json.JsonSerializer.Serialize(wrapper, options);
            var token = Newtonsoft.Json.Linq.JToken.Parse(jsonString);

            using MemoryStream ms = new(capacity: 256);
            using BsonDataWriter writer = new(ms);
            token.WriteTo(writer);
            return ms.ToArray();
        }
        catch
        {
            // Fall back to JSON
            return _jsonSerializer.Serialize(item);
        }
    }

    /// <summary>
    /// Attempts to decode <paramref name="bytes"/> as BSON, unwrapping any
    /// <see cref="ObjectWrapper{T}"/> payload and falling back to direct
    /// JSON/Newtonsoft deserialization on failure.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The BSON bytes to decode.</param>
    /// <returns>The deserialized value, or <c>default</c> if all paths fail.</returns>
    [RequiresUnreferencedCode("Reflection-based BSON deserialization.")]
    [RequiresDynamicCode("Reflection-based BSON deserialization.")]
    internal T? DeserializeBsonFormat<T>(byte[] bytes)
    {
        try
        {
            using BsonDataReader reader = new(new MemoryStream(bytes, writable: false));

            if (ForcedDateTimeKind.HasValue)
            {
                reader.DateTimeKindHandling = ForcedDateTimeKind.Value;
            }

            var token = Newtonsoft.Json.Linq.JToken.ReadFrom(reader);
            var jsonString = token.ToString(Formatting.None);

            if (jsonString.Contains("\"Value\":") &&
                TryUnwrapObjectWrapper<T>(jsonString, _jsonSerializer.GetEffectiveOptions(), out var wrappedValue))
            {
                return wrappedValue;
            }

            try
            {
                var normalizedJson = NormalizeDateTimeFormats(jsonString);
                var directOptions = _jsonSerializer.GetEffectiveOptions();
                return System.Text.Json.JsonSerializer.Deserialize<T>(normalizedJson, directOptions);
            }
            catch
            {
                // Try Newtonsoft as last resort
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch
            {
                // Final fallback
            }
        }
        catch
        {
            // Fall back if BSON handling fails
        }

        return default;
    }

    /// <summary>
    /// Wraps a value so that primitive and root-level types can be encoded as a
    /// BSON document (BSON requires an object root).
    /// </summary>
    /// <typeparam name="T">The wrapped value type.</typeparam>
    internal class ObjectWrapper<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectWrapper{T}"/> class.
        /// </summary>
        public ObjectWrapper()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectWrapper{T}"/> class with the supplied value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public ObjectWrapper(T? value) => Value = value;

        /// <summary>
        /// Gets or sets the wrapped value.
        /// </summary>
        public T? Value { get; set; }
    }
}
