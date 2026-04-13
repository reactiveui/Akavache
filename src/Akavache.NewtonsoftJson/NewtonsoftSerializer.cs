// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
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
    /// <summary>The contract resolver used to enforce Akavache DateTime handling.</summary>
    private readonly NewtonsoftDateTimeContractResolver _contractResolver = new();
#if NET9_0_OR_GREATER
    /// <summary>Synchronisation primitive guarding access to <see cref="Options"/>.</summary>
    private readonly Lock _serializerLock = new();
#else
    /// <summary>Synchronisation primitive guarding access to <see cref="Options"/>.</summary>
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
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
    [SuppressMessage("Security", "CA2328:Ensure that JsonSerializerSettings are secure", Justification = "Akavache honours caller-supplied JsonSerializerSettings — including TypeNameHandling — because forcing TypeNameHandling.None would silently break consumers that round-trip polymorphic graphs. Callers deserializing untrusted blobs are responsible for supplying a SerializationBinder via Options.")]
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
            using MemoryStream stream = new(bytes);
            using StreamReader textReader = new(stream);
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
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for serialization.")]
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
    [SuppressMessage("Security", "CA2328:Ensure that JsonSerializerSettings are secure", Justification = "Akavache honours caller-supplied JsonSerializerSettings — including TypeNameHandling — because forcing TypeNameHandling.None would silently break consumers that round-trip polymorphic graphs. Callers deserializing untrusted blobs are responsible for supplying a SerializationBinder via Options.")]
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
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for serialization.")]
    internal byte[] SerializeToBson<T>(T item)
    {
        try
        {
            var serializer = GetSerializer();
            using MemoryStream ms = new();
            using BsonDataWriter writer = new(ms);

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
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
    internal T? DeserializeBsonFormat<T>(byte[] bytes)
    {
        try
        {
            var serializer = GetSerializer();
            using BsonDataReader reader = new(new MemoryStream(bytes));

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
                using BsonDataReader reader2 = new(new MemoryStream(bytes));
                if (forcedDateTimeKind.HasValue)
                {
                    reader2.DateTimeKindHandling = forcedDateTimeKind.Value;
                }

                return serializer.Deserialize<T>(reader2);
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
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
    [SuppressMessage("Security", "CA2328:Ensure that JsonSerializerSettings are secure", Justification = "Akavache honours caller-supplied JsonSerializerSettings — including TypeNameHandling — because forcing TypeNameHandling.None would silently break consumers that round-trip polymorphic graphs. Callers deserializing untrusted blobs are responsible for supplying a SerializationBinder via Options.")]
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

    /// <summary>
    /// Creates a <see cref="JsonSerializer"/> instance configured with the current options and contract resolver.
    /// </summary>
    /// <returns>A configured <see cref="JsonSerializer"/>.</returns>
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

    /// <summary>
    /// Returns a copy of the current <see cref="JsonSerializerSettings"/> with the Akavache contract resolver applied.
    /// </summary>
    /// <returns>The effective <see cref="JsonSerializerSettings"/> used for serialization.</returns>
    [SuppressMessage("Security", "CA2328:Ensure that JsonSerializerSettings are secure", Justification = "We honour caller-supplied JsonSerializerSettings — including TypeNameHandling — because Akavache is a generic serialization layer and forcing TypeNameHandling.None would silently break consumers that round-trip polymorphic graphs (a documented Newtonsoft.Json feature). Callers who deserialize untrusted blobs are responsible for restricting the binder via Options.SerializationBinder before they hand the settings to us.")]
    internal JsonSerializerSettings GetEffectiveSettings()
    {
        var settings = Options ?? new JsonSerializerSettings();

        // Create a copy to avoid modifying the original settings
        settings = new()
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
            SerializationBinder = settings.SerializationBinder,
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
        /// <summary>
        /// Gets or sets the wrapped value.
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
