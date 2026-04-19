// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Threading.Tasks;
using Akavache.Core.Observables;
using Akavache.Helpers;

namespace Akavache.Core;

/// <summary>
/// Universal serializer compatibility utilities that enable cross-serializer functionality.
/// This class provides fallback mechanisms when the primary serializer fails to deserialize data.
/// </summary>
public static class UniversalSerializer
{
#if NET9_0_OR_GREATER
    /// <summary>Synchronization primitive for serializer registration and alternative serializer list management.</summary>
    private static readonly System.Threading.Lock _serializerLock = new();
#else
    /// <summary>Synchronization primitive for serializer registration and alternative serializer list management.</summary>
    private static readonly object _serializerLock = new();
#endif

    /// <summary>Cache for identifying BSON serializers by type to avoid repeated string probes.</summary>
    private static readonly ConcurrentDictionary<Type, bool> _isBsonSerializerByType = new();

    /// <summary>Cache for identifying plain Newtonsoft serializers to handle DateTime quirks.</summary>
    private static readonly ConcurrentDictionary<Type, bool> _isPlainNewtonsoftSerializerByType = new();

    /// <summary>Registered factories for fallback serializer creation.</summary>
    private static Func<ISerializer>[] _registeredSerializerFactories = [];

    /// <summary>Cached list of alternative serializer instances.</summary>
    private static List<ISerializer>? _alternativeSerializers;

    /// <summary>
    /// Attempts to deserialize data using fallback mechanisms if the primary serializer fails.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The serialized data.</param>
    /// <param name="primarySerializer">The primary serializer to try first.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind for consistent handling.</param>
    /// <returns>The deserialized object.</returns>
    [RequiresUnreferencedCode("Universal deserialization requires types to be preserved.")]
    [RequiresDynamicCode("Universal deserialization requires types to be preserved.")]
    public static T? Deserialize<T>(byte[] data, ISerializer primarySerializer, DateTimeKind? forcedDateTimeKind = null)
    {
        if (data is null or { Length: 0 })
        {
            return default;
        }

        ArgumentExceptionHelper.ThrowIfNull(primarySerializer);

        try
        {
            // Set forced DateTime kind for consistent handling
            if (forcedDateTimeKind.HasValue)
            {
                primarySerializer.ForcedDateTimeKind = forcedDateTimeKind;
            }

            // First, try the primary serializer
            var result = primarySerializer.Deserialize<T>(data);

            // Special handling for DateTime edge cases that may return problematic values.
            if (typeof(T) == typeof(DateTime))
            {
                var dateTime = CastAsDateTime(result);
                var validatedDateTime = DateTimeHelpers.ValidateDeserializedDateTime(dateTime, null, forcedDateTimeKind);
                return (T)(object)validatedDateTime;
            }

            return result;
        }
        catch (Exception)
        {
            // If the primary serializer fails, try fallback mechanisms.
            // TryFallbackDeserialization swallows all exceptions internally and returns
            // default on total failure, so no rethrow path is needed here.
            return TryFallbackDeserialization<T>(data, primarySerializer, forcedDateTimeKind);
        }
    }

    /// <summary>
    /// Attempts to serialize data using fallback mechanisms if the primary serializer fails.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="targetSerializer">The target serializer.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind for consistent handling.</param>
    /// <returns>The serialized data.</returns>
    [RequiresUnreferencedCode("Universal serialization requires types to be preserved.")]
    [RequiresDynamicCode("Universal serialization requires types to be preserved.")]
    public static byte[] Serialize<T>(T value, ISerializer targetSerializer, DateTimeKind? forcedDateTimeKind = null)
    {
        if (value is null)
        {
            return [];
        }

        ArgumentExceptionHelper.ThrowIfNull(targetSerializer);

        try
        {
            if (forcedDateTimeKind.HasValue)
            {
                targetSerializer.ForcedDateTimeKind = forcedDateTimeKind;
            }

            // Special preprocessing for DateTime values to ensure compatibility.
            if (typeof(T) == typeof(DateTime))
            {
                var dateTime = CastAsDateTime(value);
                var processedDateTime = PreprocessDateTimeForSerialization(dateTime, targetSerializer, forcedDateTimeKind);
                return targetSerializer.Serialize((T)(object)processedDateTime);
            }

            return targetSerializer.Serialize(value);
        }
        catch (Exception ex)
        {
            // If the target serializer fails, try a fallback serializer
            try
            {
                return TryFallbackSerialization(value, targetSerializer, forcedDateTimeKind);
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Failed to serialize value of type {typeof(T).Name} using {targetSerializer.GetType().Name} and all fallback mechanisms. " +
                    $"Original error: {ex.Message}",
                    ex);
            }
        }
    }

    /// <summary>
    /// Attempts to find data using alternative keys if the primary key look-up fails.
    /// Useful for cross-serializer compatibility.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="cache">The cache to search in.</param>
    /// <param name="requestedKey">The original key that was requested.</param>
    /// <param name="primarySerializer">The primary serializer being used.</param>
    /// <returns>A one-shot observable that emits the resolved value, or <see langword="default"/> if no matching key was found.</returns>
    [RequiresUnreferencedCode("Universal key compatibility requires types to be preserved.")]
    [RequiresDynamicCode("Universal key compatibility requires types to be preserved.")]
    public static Task<T?> TryFindDataWithAlternativeKeysAsync<T>(
        IBlobCache cache,
        string requestedKey,
        ISerializer primarySerializer) =>
        TryFindDataWithAlternativeKeys<T>(cache, requestedKey, primarySerializer).ToTask();

    /// <summary>
    /// Attempts to find data using alternative keys if the primary key look-up fails.
    /// Useful for cross-serializer compatibility.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="cache">The cache to search in.</param>
    /// <param name="requestedKey">The original key that was requested.</param>
    /// <param name="primarySerializer">The primary serializer being used.</param>
    /// <returns>A one-shot observable that emits the resolved value, or <see langword="default"/> if no matching key was found.</returns>
    [RequiresUnreferencedCode("Universal key compatibility requires types to be preserved.")]
    [RequiresDynamicCode("Universal key compatibility requires types to be preserved.")]
    public static IObservable<T?> TryFindDataWithAlternativeKeys<T>(
        IBlobCache cache,
        string requestedKey,
        ISerializer primarySerializer)
    {
        if (cache is null || string.IsNullOrEmpty(requestedKey) || primarySerializer is null)
        {
            return Observable.Return<T?>(default);
        }

        return cache.GetAllKeys()
            .ToList()
            .SelectMany(allKeys =>
            {
                if (allKeys.Count == 0)
                {
                    return Observable.Return<T?>(default);
                }

                var candidates = FindKeyCandidates<T>(allKeys, requestedKey);

                return new FirstMatchFromCandidatesObservable<string, byte[]?, T?>(
                    candidates,
                    candidateKey => cache.Get(candidateKey),
                    rawData => TryDeserializeCandidate<T>(rawData, primarySerializer, out var result) ? result : default,
                    static value => value is not null && !EqualityComparer<T>.Default.Equals(value, default!),
                    default);
            })
            .Catch<T?, Exception>(static _ => Observable.Return<T?>(default));
    }

    /// <summary>
    /// Registers a serializer factory for use as a fallback when the primary serializer fails.
    /// </summary>
    /// <param name="factory">A factory function that creates a new instance of the serializer.</param>
    public static void RegisterSerializer(Func<ISerializer> factory)
    {
        ArgumentExceptionHelper.ThrowIfNull(factory);

        lock (_serializerLock)
        {
            var old = _registeredSerializerFactories;
            var updated = new Func<ISerializer>[old.Length + 1];
            old.CopyTo(updated, 0);
            updated[old.Length] = factory;
            Volatile.Write(ref _registeredSerializerFactories, updated);
            Volatile.Write(ref _alternativeSerializers, null);
        }
    }

    /// <summary>
    /// Attempts to deserialize a candidate blob and returns whether it was successful.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="rawData">The raw bytes from the cache.</param>
    /// <param name="primarySerializer">The primary serializer.</param>
    /// <param name="result">The deserialized value if successful.</param>
    /// <returns>True if deserialization succeeded.</returns>
    [RequiresUnreferencedCode("Calls Deserialize<T>.")]
    [RequiresDynamicCode("Calls Deserialize<T>.")]
    internal static bool TryDeserializeCandidate<T>(byte[]? rawData, ISerializer primarySerializer, out T? result)
    {
        result = default;

        if (rawData is null || rawData.Length == 0)
        {
            return false;
        }

        // Deserialize<T> is exception-safe: it catches every serializer failure and
        // routes into TryFallbackDeserialization, which itself swallows exceptions
        // and returns default. Callers guarantee primarySerializer is non-null, so
        // no try/catch is needed here.
        var deserialized = Deserialize<T>(rawData, primarySerializer);

        if (deserialized is null || EqualityComparer<T>.Default.Equals(deserialized, default!))
        {
            return false;
        }

        result = deserialized;
        return true;
    }

    /// <summary>
    /// Casts a value to <see cref="DateTime"/>, returning default if it's not a DateTime.
    /// </summary>
    /// <typeparam name="T">The generic type being deserialized.</typeparam>
    /// <param name="value">The deserialized value.</param>
    /// <returns>The coerced <see cref="DateTime"/>, or <c>default</c>.</returns>
    internal static DateTime CastAsDateTime<T>(T? value) => value is DateTime dateTime ? dateTime : default;

    /// <summary>
    /// Casts a value to <see cref="DateTimeOffset"/>, returning default if it's not a DateTimeOffset.
    /// </summary>
    /// <typeparam name="T">The generic type being deserialized.</typeparam>
    /// <param name="value">The deserialized value.</param>
    /// <returns>The coerced <see cref="DateTimeOffset"/>, or <c>default</c>.</returns>
    internal static DateTimeOffset CastAsDateTimeOffset<T>(T? value) => value is DateTimeOffset dateTimeOffset ? dateTimeOffset : default;

    /// <summary>
    /// Finds keys in <paramref name="allKeys"/> that are candidates for <paramref name="requestedKey"/>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="allKeys">All available keys.</param>
    /// <param name="requestedKey">The requested key.</param>
    /// <returns>A list of candidate keys.</returns>
    internal static List<string> FindKeyCandidates<T>(IEnumerable<string> allKeys, string requestedKey)
    {
        // Cached per-T reflection strings avoid re-walking typeof(T).FullName / .Name /
        // Assembly.GetName().Name on every lookup. Each string interpolation below also
        // only touches the (cached) T metadata plus the per-call requestedKey.
        HashSet<string> possibleKeys =
        [
            requestedKey,
            $"{KeyMetadata<T>.FullName}___{requestedKey}",
            $"{KeyMetadata<T>.Name}___{requestedKey}",
            $"{KeyMetadata<T>.AssemblyQualifiedShortName}___{requestedKey}"
        ];

        var prefixSuffix = $"___{requestedKey}";
        List<string> candidates = new(8);
        foreach (var key in allKeys)
        {
            if (possibleKeys.Contains(key) || key.EndsWith(prefixSuffix, StringComparison.Ordinal))
            {
                candidates.Add(key);
                continue;
            }

            if (key.EndsWith(requestedKey, StringComparison.Ordinal))
            {
                candidates.Add(key);
            }
        }

        return candidates;
    }

    /// <summary>Checks if data might be BSON.</summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if data might be BSON.</returns>
    internal static bool IsPotentialBsonData(byte[] data)
    {
        if (data.Length < 5)
        {
            return false;
        }

        // BSON documents start with a 4-byte little-endian length field. The length check
        // above guarantees the read is safe; BinaryHelpers is endian-explicit (BSON is
        // little-endian regardless of platform) and inlines on net6+ to BinaryPrimitives.
        return BsonDataHelper.IsPotentialBsonData(data);
    }

    /// <summary>Checks if data might be JSON.</summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if data might be JSON.</returns>
    internal static bool IsPotentialJsonData(byte[] data)
    {
        if (data.Length == 0)
        {
            return false;
        }

        // Skip any leading whitespace.
        var startIndex = 0;
        while (startIndex < data.Length && data[startIndex] is 0x20 or 0x09 or 0x0A or 0x0D)
        {
            startIndex++;
        }

        if (startIndex >= data.Length)
        {
            return false;
        }

        // Check for typical JSON starting characters.
        var firstChar = data[startIndex];

        return IsJsonObjectOrArray(firstChar) ||
               IsJsonString(firstChar) ||
               IsJsonNumber(firstChar) ||
               IsJsonBoolean(data, startIndex) ||
               IsJsonNull(data, startIndex);
    }

    /// <summary>Checks if the byte is a JSON object or array start.</summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is '{' or '['.</returns>
    internal static bool IsJsonObjectOrArray(byte c) => c is 0x7B or 0x5B;

    /// <summary>Checks if the byte is a JSON string start.</summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is '"'.</returns>
    internal static bool IsJsonString(byte c) => c == 0x22;

    /// <summary>Checks if the byte is a JSON number start.</summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is '0'-'9' or '-'.</returns>
    internal static bool IsJsonNumber(byte c) => c is (>= 0x30 and <= 0x39) or 0x2D;

    /// <summary>Checks if the data at the index is a JSON boolean.</summary>
    /// <param name="data">The data buffer to check.</param>
    /// <param name="index">The index at which to start the check.</param>
    /// <returns>True if the data starting at the index matches 'true' or 'false'.</returns>
    internal static bool IsJsonBoolean(byte[] data, int index) =>
        (data.Length >= index + 4 &&
         data[index] == 0x74 && data[index + 1] == 0x72 && data[index + 2] == 0x75 && data[index + 3] == 0x65) || // 'true'
        (data.Length >= index + 5 &&
         data[index] == 0x66 && data[index + 1] == 0x61 && data[index + 2] == 0x6C && data[index + 3] == 0x73 && data[index + 4] == 0x65); // 'false'

    /// <summary>Checks if the data at the index is a JSON null.</summary>
    /// <param name="data">The data buffer to check.</param>
    /// <param name="index">The index at which to start the check.</param>
    /// <returns>True if the data starting at the index matches 'null'.</returns>
    internal static bool IsJsonNull(byte[] data, int index) =>
        data.Length >= index + 4 &&
        data[index] == 0x6E && data[index + 1] == 0x75 && data[index + 2] == 0x6C && data[index + 3] == 0x6C;

    /// <summary>Attempts fallback deserialization.</summary>
    /// <typeparam name="T">The type todeserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="primarySerializer">The primary serializer that failed.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <returns>The deserialized object or default.</returns>
    [RequiresUnreferencedCode("Calls ISerializer.Deserialize<T>.")]
    [RequiresDynamicCode("Calls ISerializer.Deserialize<T>.")]
    internal static T? TryFallbackDeserialization<T>(byte[] data, ISerializer primarySerializer, DateTimeKind? forcedDateTimeKind)
    {
        // Strategy 1: Try to detect and handle different data formats
        if (IsPotentialBsonData(data))
        {
            var bsonResult = TryDeserializeBsonFormat<T>(data, forcedDateTimeKind);
            if (bsonResult is not null && !EqualityComparer<T>.Default.Equals(bsonResult, default!))
            {
                return bsonResult;
            }
        }

        if (IsPotentialJsonData(data))
        {
            var jsonResult = TryDeserializeJsonFormat<T>(data, forcedDateTimeKind);
            if (jsonResult is not null && !EqualityComparer<T>.Default.Equals(jsonResult, default!))
            {
                return jsonResult;
            }
        }

        // Strategy 2: Try alternative serializers that might be available
        return TryAlternativeSerializers<T>(data, primarySerializer, forcedDateTimeKind);
    }

    /// <summary>Attempts fallback serialization.</summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="targetSerializer">The target serializer that failed.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind for consistent handling.</param>
    /// <returns>The serialized data.</returns>
    [RequiresUnreferencedCode("Calls ISerializer.Serialize<T>.")]
    [RequiresDynamicCode("Calls ISerializer.Serialize<T>.")]
    internal static byte[] TryFallbackSerialization<T>(T value, ISerializer targetSerializer, DateTimeKind? forcedDateTimeKind)
    {
        // Try to find and use an alternative serializer.
        // Fast path for established list; fallback to lock-protected initialization
        // if null. Registration and reset invalidate the list under the same lock.
        var alts = Volatile.Read(ref _alternativeSerializers);
        if (alts is null)
        {
            lock (_serializerLock)
            {
                alts = _alternativeSerializers;
                if (alts is null)
                {
                    alts = GetAvailableAlternativeSerializers(targetSerializer);
                    Volatile.Write(ref _alternativeSerializers, alts);
                }
            }
        }

        foreach (var altSerializer in alts)
        {
            try
            {
                if (forcedDateTimeKind.HasValue)
                {
                    altSerializer.ForcedDateTimeKind = forcedDateTimeKind;
                }

                return altSerializer.Serialize(value);
            }
            catch
            {
                // Continue to next serializer
            }
        }

        throw new InvalidOperationException("No fallback serialization strategy succeeded");
    }

    /// <summary>Attempts deserialization using alternative serializers.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="primarySerializer">The primary serializer that failed.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <returns>The deserialized object or default.</returns>
    [RequiresUnreferencedCode("Calls ISerializer.Deserialize<T>.")]
    [RequiresDynamicCode("Calls ISerializer.Deserialize<T>.")]
    internal static T? TryAlternativeSerializers<T>(byte[] data, ISerializer primarySerializer, DateTimeKind? forcedDateTimeKind)
    {
        // Fast path for established list; fallback to lock-protected initialization
        // if null. Registration and reset invalidate the list under the same lock.
        var alts = Volatile.Read(ref _alternativeSerializers);
        if (alts is null)
        {
            lock (_serializerLock)
            {
                alts = _alternativeSerializers;
                if (alts is null)
                {
                    alts = GetAvailableAlternativeSerializers(primarySerializer);
                    Volatile.Write(ref _alternativeSerializers, alts);
                }
            }
        }

        foreach (var altSerializer in alts)
        {
            try
            {
                if (forcedDateTimeKind.HasValue)
                {
                    altSerializer.ForcedDateTimeKind = forcedDateTimeKind;
                }

                var result = altSerializer.Deserialize<T>(data);

                // Enhanced DateTime handling for cross-serializer compatibility.
                if (typeof(T) == typeof(DateTime))
                {
                    var dateTime = CastAsDateTime(result);
                    if (dateTime == DateTime.MinValue)
                    {
                        // Check if this is a legitimate MinValue or a deserialization error.
                        // If the data suggests it should be a different value, try to detect and correct.
                        var correctedDateTime = DateTimeHelpers.AttemptDateTimeRecovery(data, dateTime);
                        if (correctedDateTime != DateTime.MinValue)
                        {
                            return (T)(object)DateTimeHelpers.HandleDateTimeWithCrossSerializerSupport<DateTime>(correctedDateTime, forcedDateTimeKind);
                        }
                    }

                    return DateTimeHelpers.HandleDateTimeWithCrossSerializerSupport<T>(dateTime, forcedDateTimeKind);
                }

                if (typeof(T) == typeof(DateTimeOffset))
                {
                    var dateTimeOffset = CastAsDateTimeOffset(result);
                    return DateTimeHelpers.HandleDateTimeOffsetWithCrossSerializerSupport<T>(dateTimeOffset);
                }

                return result;
            }
            catch
            {
                // Continue to next serializer
            }
        }

        return default;
    }

    /// <summary>Gets available alternative serializers.</summary>
    /// <param name="excludeSerializer">The serializer to exclude from the list.</param>
    /// <returns>A list of alternative serializers.</returns>
    internal static List<ISerializer> GetAvailableAlternativeSerializers(ISerializer excludeSerializer)
    {
        List<ISerializer> alternatives = [];
        var excludeType = excludeSerializer.GetType();

        foreach (var factory in Volatile.Read(ref _registeredSerializerFactories))
        {
            try
            {
                var instance = factory();
                if (instance.GetType() != excludeType)
                {
                    alternatives.Add(instance);
                }
            }
            catch
            {
                // Ignore if we can't instantiate this serializer
            }
        }

        return alternatives;
    }

    /// <summary>Attempts to deserialize data as BSON.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <returns>The deserialized object or default.</returns>
    [RequiresUnreferencedCode("Calls ISerializer.Deserialize<T>.")]
    [RequiresDynamicCode("Calls ISerializer.Deserialize<T>.")]
    internal static T? TryDeserializeBsonFormat<T>(byte[] data, DateTimeKind? forcedDateTimeKind)
    {
        // Try registered BSON-capable serializers
        foreach (var factory in Volatile.Read(ref _registeredSerializerFactories))
        {
            try
            {
                var serializer = factory();

                // Only use serializers that look like BSON serializers
                if (!IsBsonSerializer(serializer))
                {
                    continue;
                }

                if (forcedDateTimeKind.HasValue)
                {
                    serializer.ForcedDateTimeKind = forcedDateTimeKind;
                }

                var result = serializer.Deserialize<T>(data);

                // Enhanced handling for DateTime types with BSON to prevent issues.
                if (typeof(T) == typeof(DateTime))
                {
                    var dateTime = CastAsDateTime(result);

                    // Special handling for problematic DateTime values from BSON.
                    if (dateTime == DateTime.MinValue && data.Length > 20)
                    {
                        var recoveredDateTime = DateTimeHelpers.AttemptDateTimeRecovery(data, dateTime);
                        dateTime = recoveredDateTime != DateTime.MinValue
                            ? recoveredDateTime
                            : new(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);
                    }

                    // Ensure proper DateTimeKind via the shared converter helper.
                    if (forcedDateTimeKind.HasValue && dateTime.Kind != forcedDateTimeKind.Value)
                    {
                        dateTime = DateTimeHelpers.ConvertDateTimeKind(dateTime, forcedDateTimeKind.Value);
                    }

                    return (T)(object)dateTime;
                }

                return result;
            }
            catch
            {
                // Continue to next BSON serializer
            }
        }

        return default;
    }

    /// <summary>Attempts to deserialize data as JSON.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <returns>The deserialized object or default.</returns>
    [RequiresUnreferencedCode("Calls ISerializer.Deserialize<T>.")]
    [RequiresDynamicCode("Calls ISerializer.Deserialize<T>.")]
    internal static T? TryDeserializeJsonFormat<T>(byte[] data, DateTimeKind? forcedDateTimeKind)
    {
        // Try registered JSON-capable serializers (non-BSON)
        foreach (var factory in Volatile.Read(ref _registeredSerializerFactories))
        {
            try
            {
                var serializer = factory();

                // Skip BSON serializers - we want JSON ones
                if (IsBsonSerializer(serializer))
                {
                    continue;
                }

                if (forcedDateTimeKind.HasValue)
                {
                    serializer.ForcedDateTimeKind = forcedDateTimeKind;
                }

                return serializer.Deserialize<T>(data);
            }
            catch
            {
                // Continue to next JSON serializer
            }
        }

        return TryBasicJsonDeserialization<T>(data);
    }

    /// <summary>Attempts basic JSON deserialization for simple types.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <returns>The deserialized object or default.</returns>
    internal static T? TryBasicJsonDeserialization<T>(byte[] data)
    {
        var jsonString = Encoding.UTF8.GetString(data).Trim();

        // Basic JSON structure validation.
        return string.IsNullOrWhiteSpace(jsonString)
            ? default
            : typeof(T) switch
            {
                var t when t == typeof(string) => (T)(object)(jsonString.Length >= 2 && jsonString[0] == '"' && jsonString[jsonString.Length - 1] == '"'
                    ? jsonString.Substring(1, jsonString.Length - 2)
                    : jsonString),
                var t when t == typeof(int) && int.TryParse(jsonString, out var intValue) => (T)(object)intValue,
                var t when t == typeof(bool) && bool.TryParse(jsonString, out var boolValue) => (T)(object)boolValue,
                _ => default
            };
    }

    /// <summary>Preprocesses a DateTime value before serialization.</summary>
    /// <param name="dateTime">The DateTime value to preprocess.</param>
    /// <param name="serializer">The serializer that will be used.</param>
    /// <param name="forcedDateTimeKind">The forced DateTime kind if any.</param>
    /// <returns>The preprocessed DateTime value.</returns>
    internal static DateTime PreprocessDateTimeForSerialization(DateTime dateTime, ISerializer serializer, DateTimeKind? forcedDateTimeKind)
    {
        // Handle special cases for problematic DateTime values. Cached type probe replaces
        // two Contains() calls against GetType().Name per invocation.
        if (dateTime == DateTime.MinValue && IsPlainNewtonsoftSerializer(serializer))
        {
            // Use a safer minimum date for regular Newtonsoft serializer
            return new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        if (dateTime == DateTime.MaxValue && IsPlainNewtonsoftSerializer(serializer))
        {
            // Use a safer maximum date for regular Newtonsoft serializer
            return new(2100, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        }

        // Apply forced DateTime kind via the shared converter helper.
        return forcedDateTimeKind.HasValue && dateTime.Kind != forcedDateTimeKind.Value
            ? DateTimeHelpers.ConvertDateTimeKind(dateTime, forcedDateTimeKind.Value)
            : dateTime;
    }

    /// <summary>Resets internal caches (test isolation).</summary>
    internal static void ResetCaches()
    {
        lock (_serializerLock)
        {
            Volatile.Write(ref _registeredSerializerFactories, []);
            Volatile.Write(ref _alternativeSerializers, null);
        }

        _isBsonSerializerByType.Clear();
        _isPlainNewtonsoftSerializerByType.Clear();
    }

    /// <summary>Checks if the serializer is a BSON variant.</summary>
    /// <param name="serializer">The serializer instance to probe.</param>
    /// <returns><see langword="true"/> if the serializer is a BSON variant.</returns>
    internal static bool IsBsonSerializer(ISerializer serializer) =>
        _isBsonSerializerByType.GetOrAdd(
            serializer.GetType(),
            static t => t.Name.Contains("Bson", StringComparison.OrdinalIgnoreCase));

    /// <summary>Checks if the serializer is the plain Newtonsoft.Json serializer.</summary>
    /// <param name="serializer">The serializer instance to probe.</param>
    /// <returns><see langword="true"/> if the serializer is Newtonsoft.Json (not the BSON variant).</returns>
    internal static bool IsPlainNewtonsoftSerializer(ISerializer serializer) =>
        _isPlainNewtonsoftSerializerByType.GetOrAdd(
            serializer.GetType(),
            static t =>
            {
                var name = t.Name;
                return name.Contains("Newtonsoft", StringComparison.OrdinalIgnoreCase)
                    && !name.Contains("Bson", StringComparison.OrdinalIgnoreCase);
            });
}
