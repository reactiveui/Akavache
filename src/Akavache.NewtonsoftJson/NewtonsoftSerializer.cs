// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Akavache.NewtonsoftJson;

/// <summary>
/// A unified serializer using Newtonsoft.Json with automatic format detection.
/// Supports both JSON and BSON formats for maximum compatibility with Akavache.
/// </summary>
public class NewtonsoftSerializer : ISerializer
{
    private readonly NewtonsoftDateTimeContractResolver _contractResolver = new();

    /// <summary>
    /// Gets or sets the optional options.
    /// </summary>
    public JsonSerializerSettings? Options { get; set; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind
    {
        get => _contractResolver.ForceDateTimeKind;
        set => _contractResolver.ForceDateTimeKind = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use BSON format for serialization.
    /// When true, serializes to BSON for maximum Akavache compatibility.
    /// When false (default), serializes to JSON for better readability.
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
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
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
            using var stream = new MemoryStream(bytes);
            using var textReader = new StreamReader(stream);
            var serializer = JsonSerializer.Create(GetEffectiveSettings());
            return (T?)serializer.Deserialize(textReader, typeof(T));
        }
        catch
        {
            // Cross-serializer compatibility - try to handle data from other serializers
            return TryDeserializeFromOtherFormats<T>(bytes);
        }
    }

    /// <inheritdoc/>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for serialization.")]
#endif
    public byte[] Serialize<T>(T item)
    {
        if (UseBsonFormat)
        {
            return SerializeToBson(item);
        }

        var settings = GetEffectiveSettings();
        var jsonString = JsonConvert.SerializeObject(item, settings);
        return Encoding.UTF8.GetBytes(jsonString);
    }

    /// <summary>
    /// Serializes an object to BSON format.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>BSON bytes.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for serialization.")]
#endif
    private byte[] SerializeToBson<T>(T item)
    {
        try
        {
            var serializer = GetSerializer();
            using var ms = new MemoryStream();
            using var writer = new BsonDataWriter(ms);

            serializer.Serialize(writer, new ObjectWrapper<T>(item));
            return ms.ToArray();
        }
        catch
        {
            // Fall back to JSON if BSON serialization fails
            var settings = GetEffectiveSettings();
            var jsonString = JsonConvert.SerializeObject(item, settings);
            return Encoding.UTF8.GetBytes(jsonString);
        }
    }

    /// <summary>
    /// Deserializes BSON data using Newtonsoft.Json.Bson.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The BSON bytes.</param>
    /// <returns>The deserialized object.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
#endif
    private T? DeserializeBsonFormat<T>(byte[] bytes)
    {
        try
        {
            var serializer = GetSerializer();
            using var reader = new BsonDataReader(new MemoryStream(bytes));

            var forcedDateTimeKind = ForcedDateTimeKind;
            if (forcedDateTimeKind.HasValue)
            {
                reader.DateTimeKindHandling = forcedDateTimeKind.Value;
            }

            try
            {
                var wrapper = serializer.Deserialize<ObjectWrapper<T>>(reader);
                return wrapper is null ? default : wrapper.Value;
            }
            catch
            {
                // Reset stream and try direct deserialization
                reader.Close();
                using var reader2 = new BsonDataReader(new MemoryStream(bytes));
                if (forcedDateTimeKind.HasValue)
                {
                    reader2.DateTimeKindHandling = forcedDateTimeKind.Value;
                }

                var result = serializer.Deserialize<T>(reader2);
                return result;
            }
        }
        catch
        {
            // Fall back if BSON handling fails
            return default(T);
        }
    }

    /// <summary>
    /// Attempts to deserialize data that might be from other serializer formats.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The data bytes.</param>
    /// <returns>The deserialized object or default.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
#endif
    private T? TryDeserializeFromOtherFormats<T>(byte[] bytes)
    {
        // First try BSON format if not already attempted
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

            var settings = GetEffectiveSettings();

            // Try ObjectWrapper format first (from BSON serializers)
            if (jsonString.Contains("\"Value\":"))
            {
                try
                {
                    var wrapper = JsonConvert.DeserializeObject<SimpleObjectWrapper<T>>(jsonString, settings);
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

            // Try direct JSON deserialization with Newtonsoft.Json
            var result = JsonConvert.DeserializeObject<T>(jsonString, settings);
            if (result != null || (typeof(T).IsValueType && !Equals(result, default(T))))
            {
                return result;
            }

            return result; // Return the Newtonsoft result even if it's default
        }
        catch
        {
            return default(T);
        }
    }

    private JsonSerializer GetSerializer()
    {
        var settings = Options ?? new JsonSerializerSettings();

        lock (settings)
        {
            _contractResolver.ExistingContractResolver = settings.ContractResolver;
            _contractResolver.ForceDateTimeKind = ForcedDateTimeKind;
            settings.ContractResolver = _contractResolver;
            var serializer = JsonSerializer.Create(settings);
            settings.ContractResolver = _contractResolver.ExistingContractResolver;
            Options = settings; // Update the options to the new settings with the resolver.

            return serializer;
        }
    }

    private JsonSerializerSettings GetEffectiveSettings()
    {
        var settings = Options ?? new JsonSerializerSettings();

        // Create a copy to avoid modifying the original settings
        settings = new JsonSerializerSettings
        {
            ContractResolver = _contractResolver,
            DateTimeZoneHandling = settings.DateTimeZoneHandling,
            DateParseHandling = settings.DateParseHandling,
            FloatParseHandling = settings.FloatParseHandling,
            NullValueHandling = settings.NullValueHandling,
            DefaultValueHandling = settings.DefaultValueHandling,
            ObjectCreationHandling = settings.ObjectCreationHandling,
            MissingMemberHandling = settings.MissingMemberHandling,
            ReferenceLoopHandling = settings.ReferenceLoopHandling,
            CheckAdditionalContent = settings.CheckAdditionalContent,
            StringEscapeHandling = settings.StringEscapeHandling,
            Culture = settings.Culture,
            MaxDepth = settings.MaxDepth,
            Formatting = settings.Formatting,
            DateFormatHandling = settings.DateFormatHandling,
            DateFormatString = settings.DateFormatString,
            FloatFormatHandling = settings.FloatFormatHandling,
            Converters = settings.Converters,
            TypeNameHandling = settings.TypeNameHandling,
            MetadataPropertyHandling = settings.MetadataPropertyHandling,
            TypeNameAssemblyFormatHandling = settings.TypeNameAssemblyFormatHandling,
            ConstructorHandling = settings.ConstructorHandling,
            Error = settings.Error
        };

        // Set our contract resolver, preserving any existing one
        _contractResolver.ExistingContractResolver = settings.ContractResolver;
        settings.ContractResolver = _contractResolver;

        return settings;
    }

    /// <summary>
    /// Simple ObjectWrapper for compatibility with other serializers.
    /// </summary>
    /// <typeparam name="T">The wrapped type.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
    private class SimpleObjectWrapper<T>
    {
        public T? Value { get; set; }
    }

    /// <summary>
    /// Object wrapper for BSON compatibility with Akavache format.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    private class ObjectWrapper<T>
    {
        public ObjectWrapper()
        {
        }

        public ObjectWrapper(T? value) => Value = value;

        public T? Value { get; set; }
    }
}
