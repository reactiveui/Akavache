// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _serializerLock = new();
#else
    private readonly object _serializerLock = new();
#endif

    /// <summary>
    /// Gets or sets the JSON serializer settings for customizing serialization behavior.
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

        // BSON documents start with a 4-byte length field.
        var documentLength = BitConverter.ToInt32(data, 0);

        // Basic sanity check: document length should be reasonable and match actual data length.
        if (documentLength <= 4 || documentLength > data.Length + 100)
        {
            return false;
        }

        // Check if this looks like JSON instead.
        var firstChar = data[4];
        if (firstChar is (byte)'{' or (byte)'[' or (byte)'"')
        {
            return false;
        }

        // Additional check: try to identify JSON by looking for common JSON patterns in the data.
        var dataString = Encoding.UTF8.GetString(data);
        return !(dataString.TrimStart().StartsWith("{") || dataString.TrimStart().StartsWith("["));
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
            return default;
        }

        // Automatic format detection - try the expected format first.
        // DeserializeBsonFormat is exception-safe and returns default on failure, so no
        // wrapping try/catch is needed here.
        if (UseBsonFormat || IsPotentialBsonData(bytes))
        {
            var bsonResult = DeserializeBsonFormat<T>(bytes);
            if (bsonResult != null || typeof(T).IsValueType)
            {
                return bsonResult;
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
    /// Attempts to decode <paramref name="jsonString"/> as a
    /// <see cref="SimpleObjectWrapper{T}"/>.
    /// </summary>
    /// <typeparam name="T">The wrapped value type.</typeparam>
    /// <param name="jsonString">The JSON string to decode.</param>
    /// <param name="settings">The Newtonsoft.Json settings to use.</param>
    /// <param name="value">When this method returns <see langword="true"/>, contains the unwrapped value; otherwise <c>default</c>.</param>
    /// <returns><see langword="true"/> if a non-null wrapper was produced.</returns>
    internal static bool TryUnwrapSimpleObjectWrapper<T>(string jsonString, JsonSerializerSettings settings, out T? value)
    {
        try
        {
            var wrapper = JsonConvert.DeserializeObject<SimpleObjectWrapper<T>>(jsonString, settings);
            if (wrapper is not null)
            {
                value = wrapper.Value;
                return true;
            }
        }
        catch
        {
            // Fall through — caller will try direct deserialization.
        }

        value = default;
        return false;
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
    internal byte[] SerializeToBson<T>(T item)
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
    internal T? DeserializeBsonFormat<T>(byte[] bytes)
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
            return default;
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
    internal T? TryDeserializeFromOtherFormats<T>(byte[] bytes)
    {
        // First try BSON format if not already attempted
        // DeserializeBsonFormat is exception-safe and returns default on failure, so no
        // wrapping try/catch is needed here.
        if (!UseBsonFormat)
        {
            var bsonResult = DeserializeBsonFormat<T>(bytes);
            if (typeof(T).IsValueType
                ? !EqualityComparer<T>.Default.Equals(bsonResult!, default!)
                : bsonResult != null)
            {
                return bsonResult;
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
                return default;
            }

            var settings = GetEffectiveSettings();

            // Try ObjectWrapper format first (from BSON serializers).
            if (jsonString.Contains("\"Value\":") &&
                TryUnwrapSimpleObjectWrapper<T>(jsonString, settings, out var wrappedValue))
            {
                return wrappedValue;
            }

            // Try direct JSON deserialization with Newtonsoft.Json. Both arms of the
            // earlier non-null/value-type check returned the same result, so the unified
            // return below handles success and default-fall-through identically.
            return JsonConvert.DeserializeObject<T>(jsonString, settings);
        }
        catch
        {
            return default;
        }
    }

    internal JsonSerializer GetSerializer()
    {
        var settings = Options ?? new JsonSerializerSettings();

        lock (_serializerLock)
        {
            _contractResolver.ExistingContractResolver = settings.ContractResolver;
            _contractResolver.ForceDateTimeKind = ForcedDateTimeKind;
            settings.ContractResolver = _contractResolver;
            var serializer = JsonSerializer.Create(settings);
            settings.ContractResolver = _contractResolver.ExistingContractResolver;
            Options = settings;

            return serializer;
        }
    }

    internal JsonSerializerSettings GetEffectiveSettings()
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
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
    internal class SimpleObjectWrapper<T>
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
