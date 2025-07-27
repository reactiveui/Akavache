// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.SystemTextJson;

/// <summary>
/// A unified serializer using System.Text.Json with automatic format detection.
/// Supports both JSON and BSON formats for maximum compatibility.
/// </summary>
public class SystemJsonSerializer : ISerializer
{
    /// <summary>
    /// Gets or sets the optional options.
    /// </summary>
    public JsonSerializerOptions? Options { get; set; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use BSON format for serialization.
    /// When true, serializes to BSON for maximum Akavache compatibility.
    /// When false (default), serializes to JSON for better performance.
    /// </summary>
    public bool UseBsonFormat { get; set; }

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

        try
        {
            // BSON documents start with a 4-byte length field
            var documentLength = BitConverter.ToInt32(data, 0);

            // Basic sanity check: document length should be reasonable and match actual data length
            if (documentLength <= 4 || documentLength > data.Length + 100)
            {
                return false;
            }

            // Check if this looks like JSON instead
            var firstChar = data[4];
            if (firstChar == '{' || firstChar == '[' || firstChar == '"')
            {
                // This looks more like JSON
                return false;
            }

            // Additional check: try to identify JSON by looking for common JSON patterns in the data
            var dataString = Encoding.UTF8.GetString(data);
            return !(dataString.TrimStart().StartsWith("{") || dataString.TrimStart().StartsWith("["));
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using System.Text.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using System.Text.Json requires types to be preserved for deserialization.")]
#endif
    public T? Deserialize<T>(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return default(T);
        }

        // Automatic format detection - try the expected format first
        if (UseBsonFormat || IsPotentialBsonData(bytes))
        {
            try
            {
                var bsonResult = DeserializeBsonFormat<T>(bytes);
                if (bsonResult != null || typeof(T).IsValueType)
                {
                    return bsonResult;
                }
            }
            catch
            {
                // Fall back to JSON if BSON fails
            }
        }

        // Try JSON format
        try
        {
            var options = GetEffectiveOptions();
            return System.Text.Json.JsonSerializer.Deserialize<T>(bytes, options);
        }
        catch
        {
            // Cross-serializer compatibility - try other formats
            return TryDeserializeFromOtherFormats<T>(bytes);
        }
    }

    /// <inheritdoc/>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using System.Text.Json requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using System.Text.Json requires types to be preserved for serialization.")]
#endif
    public byte[] Serialize<T>(T item)
    {
        if (UseBsonFormat)
        {
            return SerializeToBson(item);
        }

        var options = GetEffectiveOptions();
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(item, options);
    }

    /// <summary>
    /// Normalizes DateTime formats from Newtonsoft.Json to System.Text.Json compatible format.
    /// </summary>
    /// <param name="jsonString">The JSON string to normalize.</param>
    /// <returns>Normalized JSON string.</returns>
    private static string NormalizeDateTimeFormats(string jsonString)
    {
        // Pattern to match DateTime tick values like "Date":638725392000000000
        var dateTimeTickPattern = new Regex(@"""Date"":(\d{15,})");

        return dateTimeTickPattern.Replace(jsonString, match =>
        {
            if (long.TryParse(match.Groups[1].Value, out var ticks))
            {
                try
                {
                    var dateTime = new DateTime(ticks, DateTimeKind.Utc);
                    var isoString = dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    return $"\"Date\":\"{isoString}\"";
                }
                catch
                {
                    // If conversion fails, return original
                    return match.Value;
                }
            }

            return match.Value;
        });
    }

    /// <summary>
    /// Gets the effective JsonSerializerOptions for this serializer.
    /// </summary>
    /// <returns>The configured JsonSerializerOptions.</returns>
    private JsonSerializerOptions GetEffectiveOptions()
    {
        var options = Options ?? new JsonSerializerOptions();

        // Clone options to avoid modifying the original
        var effectiveOptions = new JsonSerializerOptions(options);

        // Apply simple DateTime handling for cross-serializer compatibility
        if (ForcedDateTimeKind.HasValue)
        {
            // For now, use default handling but ensure consistent behavior
            // This can be enhanced later without breaking cross-serializer compatibility
        }

        return effectiveOptions;
    }

    /// <summary>
    /// Serializes an object to BSON format using System.Text.Json for the object and Newtonsoft.Json.Bson for BSON writing.
    /// This provides a hybrid approach for maximum compatibility while leveraging System.Text.Json performance.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>BSON bytes if possible, otherwise JSON bytes.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using System.Text.Json requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using System.Text.Json requires types to be preserved for serialization.")]
#endif
    private byte[] SerializeToBson<T>(T item)
    {
        try
        {
            // Wrap the item for compatibility with Akavache BSON format
            var wrapper = new ObjectWrapper<T>(item);

            // Serialize to JSON string using System.Text.Json
            var options = GetEffectiveOptions();
            var jsonString = System.Text.Json.JsonSerializer.Serialize(wrapper, options);

            // Parse JSON string into Newtonsoft JToken for BSON conversion
            var token = Newtonsoft.Json.Linq.JToken.Parse(jsonString);

            // Write JToken to BSON using Newtonsoft.Json.Bson
            using var ms = new MemoryStream();
            using var writer = new BsonDataWriter(ms);

            token.WriteTo(writer);
            return ms.ToArray();
        }
        catch
        {
            // Fall back to JSON if BSON serialization fails
            var fallbackOptions = GetEffectiveOptions();
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(item, fallbackOptions);
        }
    }

    /// <summary>
    /// Deserializes BSON data using Newtonsoft.Json.Bson for BSON reading and System.Text.Json for object deserialization.
    /// This provides a hybrid approach for maximum compatibility while leveraging System.Text.Json performance.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The BSON bytes.</param>
    /// <returns>The deserialized object or default if BSON handling fails.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using System.Text.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using System.Text.Json requires types to be preserved for deserialization.")]
#endif
    private T? DeserializeBsonFormat<T>(byte[] bytes)
    {
        try
        {
            using var reader = new BsonDataReader(new MemoryStream(bytes));

            // Set DateTimeKind handling if specified
            if (ForcedDateTimeKind.HasValue)
            {
                reader.DateTimeKindHandling = ForcedDateTimeKind.Value;
            }

            // Read BSON into a Newtonsoft JToken
            var token = Newtonsoft.Json.Linq.JToken.ReadFrom(reader);

            // Convert JToken to JSON string
            var jsonString = token.ToString(Formatting.None);

            if (!string.IsNullOrEmpty(jsonString))
            {
                // Try ObjectWrapper format first (for Akavache compatibility)
                if (jsonString.Contains("\"Value\":"))
                {
                    try
                    {
                        // Apply DateTime normalization before System.Text.Json deserialization
                        var normalizedJson = NormalizeDateTimeFormats(jsonString);
                        var options = GetEffectiveOptions();
                        var wrapper = System.Text.Json.JsonSerializer.Deserialize<ObjectWrapper<T>>(normalizedJson, options);
                        if (wrapper != null)
                        {
                            return wrapper.Value;
                        }
                    }
                    catch
                    {
                        // Try with Newtonsoft.Json for ObjectWrapper
                        try
                        {
                            var newtonsoftWrapper = JsonConvert.DeserializeObject<ObjectWrapper<T>>(jsonString);
                            if (newtonsoftWrapper != null)
                            {
                                return newtonsoftWrapper.Value;
                            }
                        }
                        catch
                        {
                            // Continue to direct deserialization
                        }
                    }
                }

                // Try direct deserialization with special BSON handling
                try
                {
                    // Apply DateTime normalization for direct deserialization too
                    var normalizedJson = NormalizeDateTimeFormats(jsonString);
                    var directOptions = GetEffectiveOptions();
                    var directResult = System.Text.Json.JsonSerializer.Deserialize<T>(normalizedJson, directOptions);
                    if (directResult != null || (typeof(T).IsValueType && !Equals(directResult, default(T))))
                    {
                        return directResult;
                    }
                }
                catch
                {
                    // Continue to fallback
                }

                // If direct deserialization fails, try to use Newtonsoft for consistency
                try
                {
                    var newtonsoftResult = JsonConvert.DeserializeObject<T>(jsonString);
                    return newtonsoftResult;
                }
                catch
                {
                    // Final fallback
                }
            }
        }
        catch
        {
            // Fall back if BSON handling fails
        }

        return default(T);
    }

    /// <summary>
    /// Attempts to deserialize data that might be from other serializer formats.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The data bytes.</param>
    /// <returns>The deserialized object or default.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using System.Text.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using System.Text.Json requires types to be preserved for deserialization.")]
#endif
    private T? TryDeserializeFromOtherFormats<T>(byte[] bytes)
    {
        // First try BSON format (from BSON serializers) if not already attempted
        if (!UseBsonFormat)
        {
            try
            {
                var bsonResult = DeserializeBsonFormat<T>(bytes);
                if (bsonResult != null || (typeof(T).IsValueType && !Equals(bsonResult, default(T))))
                {
                    return bsonResult;
                }
            }
            catch
            {
                // Continue to next attempt
            }
        }

        // Try JSON format
        try
        {
            var jsonString = Encoding.UTF8.GetString(bytes);

            // Skip if it doesn't look like JSON
            if (string.IsNullOrWhiteSpace(jsonString) ||
                (!jsonString.TrimStart().StartsWith("{") && !jsonString.TrimStart().StartsWith("[")))
            {
                return default(T);
            }

            var options = GetEffectiveOptions();

            // Try ObjectWrapper format first (for cross-serializer BSON compatibility)
            if (jsonString.Contains("\"Value\":"))
            {
                try
                {
                    var wrapper = System.Text.Json.JsonSerializer.Deserialize<SimpleObjectWrapper<T>>(jsonString, options);
                    if (wrapper is not null)
                    {
                        return wrapper.Value;
                    }
                }
                catch
                {
                    // Continue to direct deserialization
                }
            }

            // Special handling for DateTime compatibility with Newtonsoft.Json
            if (jsonString.Contains("\"Date\":") && typeof(T).GetProperties().Any(p => p.PropertyType == typeof(DateTime)))
            {
                try
                {
                    // Try to normalize DateTime format from Newtonsoft.Json to System.Text.Json
                    var normalizedJson = NormalizeDateTimeFormats(jsonString);
                    var normalizedResult = System.Text.Json.JsonSerializer.Deserialize<T>(normalizedJson, options);
                    if (normalizedResult != null || (typeof(T).IsValueType && !Equals(normalizedResult, default(T))))
                    {
                        return normalizedResult;
                    }
                }
                catch
                {
                    // Continue to direct deserialization
                }
            }

            // Try direct JSON deserialization
            var directOptions = GetEffectiveOptions();
            var directResult = System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, directOptions);
            if (directResult != null || (typeof(T).IsValueType && !Equals(directResult, default(T))))
            {
                return directResult;
            }

            // Final fallback - try alternative JSON parsers if available
            try
            {
                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    // Try Newtonsoft.Json as fallback for cross-serializer compatibility
                    var newtonsoftResult = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonString);
                    if (newtonsoftResult != null || (typeof(T).IsValueType && !Equals(newtonsoftResult, default(T))))
                    {
                        return newtonsoftResult;
                    }
                }
            }
            catch
            {
                // Final fallback failed
            }

            return directResult; // Return the System.Text.Json result even if it's default
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// Simple ObjectWrapper for compatibility with other serializers.
    /// </summary>
    /// <typeparam name="T">The wrapped type.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
    private class SimpleObjectWrapper<T>
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public T? Value { get; set; }
    }

    /// <summary>
    /// Object wrapper for BSON compatibility with Akavache format.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    private class ObjectWrapper<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectWrapper{T}"/> class.
        /// </summary>
        public ObjectWrapper()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectWrapper{T}"/> class.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public ObjectWrapper(T? value) => Value = value;

        /// <summary>
        /// Gets or sets the wrapped value.
        /// </summary>
        public T? Value { get; set; }
    }
}
