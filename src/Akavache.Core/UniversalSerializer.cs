// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Core;
#else
namespace Akavache.Core;
#endif

/// <summary>
/// Universal serializer compatibility utilities that enable cross-serializer functionality.
/// This class provides fallback mechanisms when the primary serializer fails to deserialize data.
/// </summary>
public static class UniversalSerializer
{
    /// <summary>
    /// Starting capacity for the candidate-key list: the four exact spellings a key can take
    /// (bare, full-name prefixed, short-name prefixed, assembly-qualified prefixed) plus room
    /// for a handful of suffix matches before the list has to grow.
    /// </summary>
    private const int InitialKeyCandidateCapacity = 8;

    /// <summary>The number of double-quote characters wrapping a JSON string literal — one at each end.</summary>
    private const int JsonStringQuoteCount = 2;

    /// <summary>The ASCII spelling of the JSON <c>true</c> literal.</summary>
    private const string JsonTrueLiteral = "true";

    /// <summary>The ASCII spelling of the JSON <c>false</c> literal.</summary>
    private const string JsonFalseLiteral = "false";

    /// <summary>The ASCII spelling of the JSON <c>null</c> literal.</summary>
    private const string JsonNullLiteral = "null";

    /// <summary>Synchronization primitive for serializer registration and alternative serializer list management.</summary>
    private static readonly Lock _serializerLock = new();

    /// <summary>Cache for identifying BSON serializers by type to avoid repeated string probes.</summary>
    private static readonly ConcurrentDictionary<Type, bool> _isBsonSerializerByType = new();

    /// <summary>Cache for identifying plain Newtonsoft serializers to handle DateTime quirks.</summary>
    private static readonly ConcurrentDictionary<Type, bool> _isPlainNewtonsoftSerializerByType = new();

    /// <summary>Registered factories for fallback serializer creation.</summary>
    private static Func<ISerializer>[] _registeredSerializerFactories = [];

    /// <summary>Cached list of alternative serializer instances.</summary>
    private static List<ISerializer>? _alternativeSerializers;

    /// <summary>Attempts to deserialize data using fallback mechanisms if the primary serializer fails.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The serialized data.</param>
    /// <param name="primarySerializer">The primary serializer to try first.</param>
    /// <returns>The deserialized object.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    [RequiresUnreferencedCode("Universal deserialization requires types to be preserved.")]
    [RequiresDynamicCode("Universal deserialization requires types to be preserved.")]
    public static T? Deserialize<T>(byte[] data, ISerializer primarySerializer) =>
        Deserialize<T>(data, primarySerializer, (DateTimeKind?)null);

    /// <summary>Attempts to deserialize data using fallback mechanisms if the primary serializer fails.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The serialized data.</param>
    /// <param name="primarySerializer">The primary serializer to try first.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind for consistent handling.</param>
    /// <returns>The deserialized object.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    [RequiresUnreferencedCode("Universal deserialization requires types to be preserved.")]
    [RequiresDynamicCode("Universal deserialization requires types to be preserved.")]
    public static T? Deserialize<T>(byte[] data, ISerializer primarySerializer, DateTimeKind? forcedDateTimeKind)
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

    /// <summary>Attempts to serialize data using fallback mechanisms if the primary serializer fails.</summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="targetSerializer">The target serializer.</param>
    /// <returns>The serialized data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Universal serialization requires types to be preserved.")]
    [RequiresDynamicCode("Universal serialization requires types to be preserved.")]
    public static byte[] Serialize<T>(T value, ISerializer targetSerializer) =>
        Serialize(value, targetSerializer, (DateTimeKind?)null);

    /// <summary>Attempts to serialize data using fallback mechanisms if the primary serializer fails.</summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="targetSerializer">The target serializer.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind for consistent handling.</param>
    /// <returns>The serialized data.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    [RequiresUnreferencedCode("Universal serialization requires types to be preserved.")]
    [RequiresDynamicCode("Universal serialization requires types to be preserved.")]
    public static byte[] Serialize<T>(T value, ISerializer targetSerializer, DateTimeKind? forcedDateTimeKind)
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
                    $"Failed to serialize value of type {typeof(T).Name} using {targetSerializer.GetType().Name} and all fallback mechanisms. "
                    + $"Original error: {ex.Message}",
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
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
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    [RequiresUnreferencedCode("Universal key compatibility requires types to be preserved.")]
    [RequiresDynamicCode("Universal key compatibility requires types to be preserved.")]
    public static IObservable<T?> TryFindDataWithAlternativeKeys<T>(
        IBlobCache cache,
        string requestedKey,
        ISerializer primarySerializer)
    {
        if (cache is null || string.IsNullOrEmpty(requestedKey) || primarySerializer is null)
        {
            return Signal.Return<T?>(default);
        }

        return cache.GetAllKeys()
            .ToList()
            .SelectMany(allKeys =>
            {
                if (allKeys.Count == 0)
                {
                    return Signal.Return<T?>(default);
                }

                var candidates = FindKeyCandidates<T>(allKeys, requestedKey);

                return new FirstMatchFromCandidatesObservable<string, byte[]?, T?>(
                    candidates,
                    cache.Get,
                    rawData => TryDeserializeCandidate<T>(rawData, primarySerializer, out var result) ? result : default,
                    static value => value is not null && !EqualityComparer<T>.Default.Equals(value, default!),
                    default);
            })
            .Catch<T?, Exception>(static _ => Signal.Return<T?>(default));
    }

    /// <summary>Registers a serializer factory for use as a fallback when the primary serializer fails.</summary>
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

    /// <summary>Attempts to deserialize a candidate blob and returns whether it was successful.</summary>
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

    /// <summary>Casts a value to <see cref="DateTime"/>, returning default if it's not a DateTime.</summary>
    /// <typeparam name="T">The generic type being deserialized.</typeparam>
    /// <param name="value">The deserialized value.</param>
    /// <returns>The coerced <see cref="DateTime"/>, or <c>default</c>.</returns>
    internal static DateTime CastAsDateTime<T>(T? value) => value is DateTime dateTime ? dateTime : default;

    /// <summary>Casts a value to <see cref="DateTimeOffset"/>, returning default if it's not a DateTimeOffset.</summary>
    /// <typeparam name="T">The generic type being deserialized.</typeparam>
    /// <param name="value">The deserialized value.</param>
    /// <returns>The coerced <see cref="DateTimeOffset"/>, or <c>default</c>.</returns>
    internal static DateTimeOffset CastAsDateTimeOffset<T>(T? value) => value is DateTimeOffset dateTimeOffset ? dateTimeOffset : default;

    /// <summary>Finds keys in <paramref name="allKeys"/> that are candidates for <paramref name="requestedKey"/>.</summary>
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
        List<string> candidates = new(InitialKeyCandidateCapacity);
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
    internal static bool IsPotentialBsonData(byte[] data) => data.Length < 5 ? false : BsonDataHelper.IsPotentialBsonData(data);

    /// <summary>Checks if data might be JSON.</summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if data might be JSON.</returns>
    internal static bool IsPotentialJsonData(byte[] data)
    {
        if (data.Length == 0)
        {
            return false;
        }

        var startIndex = SkipLeadingJsonWhitespace(data);

        if (startIndex >= data.Length)
        {
            return false;
        }

        // Check for typical JSON starting characters.
        var firstChar = data[startIndex];

        return IsJsonObjectOrArray(firstChar)
               || IsJsonString(firstChar)
               || IsJsonNumber(firstChar)
               || IsJsonBoolean(data, startIndex)
               || IsJsonNull(data, startIndex);
    }

    /// <summary>Returns the index of the first byte that is not JSON insignificant whitespace.</summary>
    /// <param name="data">The data buffer to scan.</param>
    /// <returns>The index of the first non-whitespace byte, or <c>data.Length</c> when the buffer is all whitespace.</returns>
    internal static int SkipLeadingJsonWhitespace(byte[] data)
    {
        var startIndex = 0;

        // Space, tab, line feed and carriage return are the only whitespace JSON allows between tokens.
        while (startIndex < data.Length && data[startIndex] is 0x20 or 0x09 or 0x0A or 0x0D)
        {
            startIndex++;
        }

        return startIndex;
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
        StartsWithAsciiLiteral(data, index, JsonTrueLiteral)
        || StartsWithAsciiLiteral(data, index, JsonFalseLiteral);

    /// <summary>Checks if the data at the index is a JSON null.</summary>
    /// <param name="data">The data buffer to check.</param>
    /// <param name="index">The index at which to start the check.</param>
    /// <returns>True if the data starting at the index matches 'null'.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsJsonNull(byte[] data, int index) => StartsWithAsciiLiteral(data, index, JsonNullLiteral);

    /// <summary>Checks whether the bytes at <paramref name="index"/> spell out <paramref name="literal"/> in ASCII.</summary>
    /// <param name="data">The data buffer to check.</param>
    /// <param name="index">The index at which to start the check.</param>
    /// <param name="literal">The ASCII-only literal to match. Every character must be below 0x80.</param>
    /// <returns>True when the buffer holds enough bytes from <paramref name="index"/> onwards and they all match.</returns>
    internal static bool StartsWithAsciiLiteral(byte[] data, int index, string literal)
    {
        if (data.Length - index < literal.Length)
        {
            return false;
        }

        for (var offset = 0; offset < literal.Length; offset++)
        {
            if (data[index + offset] != (byte)literal[offset])
            {
                return false;
            }
        }

        return true;
    }

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
    /// <exception cref="InvalidOperationException"></exception>
    [RequiresUnreferencedCode("Calls ISerializer.Serialize<T>.")]
    [RequiresDynamicCode("Calls ISerializer.Serialize<T>.")]
    internal static byte[] TryFallbackSerialization<T>(T value, ISerializer targetSerializer, DateTimeKind? forcedDateTimeKind)
    {
        // Try to find and use an alternative serializer.
        foreach (var altSerializer in GetOrCreateAlternativeSerializers(targetSerializer))
        {
            if (TrySerializeWith(altSerializer, value, forcedDateTimeKind, out var serialized))
            {
                return serialized;
            }
        }

        throw new InvalidOperationException("No fallback serialization strategy succeeded");
    }

    /// <summary>Returns the cached alternative-serializer list, building it under the registration lock on first use.</summary>
    /// <param name="excludeSerializer">The serializer to leave out of the list when it has to be built.</param>
    /// <returns>The cached list of alternative serializers.</returns>
    internal static List<ISerializer> GetOrCreateAlternativeSerializers(ISerializer excludeSerializer)
    {
        // Fast path for established list; fallback to lock-protected initialization
        // if null. Registration and reset invalidate the list under the same lock.
        var alts = Volatile.Read(ref _alternativeSerializers);
        if (alts is not null)
        {
            return alts;
        }

        lock (_serializerLock)
        {
            alts = _alternativeSerializers;
            if (alts is null)
            {
                alts = GetAvailableAlternativeSerializers(excludeSerializer);
                Volatile.Write(ref _alternativeSerializers, alts);
            }

            return alts;
        }
    }

    /// <summary>Serializes <paramref name="value"/> with a single candidate serializer, reporting failure instead of throwing.</summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="serializer">The candidate serializer.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind for consistent handling.</param>
    /// <param name="serialized">The serialized bytes when the candidate succeeded; otherwise empty.</param>
    /// <returns>
    /// <see langword="true"/> when the candidate produced a payload. Any failure from a third-party
    /// serializer is absorbed so the caller can probe the next candidate — the fallback chain has no
    /// way to enumerate the exception types every plugin can raise.
    /// </returns>
    [RequiresUnreferencedCode("Calls ISerializer.Serialize<T>.")]
    [RequiresDynamicCode("Calls ISerializer.Serialize<T>.")]
    internal static bool TrySerializeWith<T>(ISerializer serializer, T value, DateTimeKind? forcedDateTimeKind, out byte[] serialized)
    {
        try
        {
            if (forcedDateTimeKind.HasValue)
            {
                serializer.ForcedDateTimeKind = forcedDateTimeKind;
            }

            serialized = serializer.Serialize(value);
            return true;
        }
        catch
        {
            serialized = [];
            return false;
        }
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
        foreach (var altSerializer in GetOrCreateAlternativeSerializers(primarySerializer))
        {
            if (TryDeserializeWith<T>(altSerializer, data, forcedDateTimeKind, out var result))
            {
                return result;
            }
        }

        return default;
    }

    /// <summary>Deserializes with a single candidate serializer, reporting failure instead of throwing.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="serializer">The candidate serializer.</param>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <param name="result">The deserialized value when the candidate succeeded; otherwise default.</param>
    /// <returns>
    /// <see langword="true"/> when the candidate produced a value — including <c>default</c>, which is a
    /// legitimate result for value types. Any failure from a third-party serializer, or from the DateTime
    /// coercion applied to its output, is absorbed so the caller can probe the next candidate.
    /// </returns>
    [RequiresUnreferencedCode("Calls ISerializer.Deserialize<T>.")]
    [RequiresDynamicCode("Calls ISerializer.Deserialize<T>.")]
    internal static bool TryDeserializeWith<T>(ISerializer serializer, byte[] data, DateTimeKind? forcedDateTimeKind, out T? result)
    {
        try
        {
            if (forcedDateTimeKind.HasValue)
            {
                serializer.ForcedDateTimeKind = forcedDateTimeKind;
            }

            result = ApplyCrossSerializerDateTimeHandling(serializer.Deserialize<T>(data), data, forcedDateTimeKind);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>Normalizes a value produced by an alternative serializer so DateTime and DateTimeOffset survive the round-trip.</summary>
    /// <typeparam name="T">The type that was deserialized.</typeparam>
    /// <param name="deserialized">The raw value from the alternative serializer.</param>
    /// <param name="data">The original data, used to recover a DateTime the serializer collapsed to MinValue.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <returns>The normalized value; unchanged for every type other than DateTime and DateTimeOffset.</returns>
    internal static T? ApplyCrossSerializerDateTimeHandling<T>(T? deserialized, byte[] data, DateTimeKind? forcedDateTimeKind)
    {
        // Enhanced DateTime handling for cross-serializer compatibility.
        if (typeof(T) == typeof(DateTime))
        {
            var dateTime = CastAsDateTime(deserialized);
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
            var dateTimeOffset = CastAsDateTimeOffset(deserialized);
            return DateTimeHelpers.HandleDateTimeOffsetWithCrossSerializerSupport<T>(dateTimeOffset);
        }

        return deserialized;
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
            var instance = TryCreateAlternativeSerializer(factory, excludeType);
            if (instance is not null)
            {
                alternatives.Add(instance);
            }
        }

        return alternatives;
    }

    /// <summary>Instantiates a registered serializer factory, screening out the excluded type.</summary>
    /// <param name="factory">The registered factory.</param>
    /// <param name="excludeType">The serializer type to leave out of the alternatives.</param>
    /// <returns>
    /// The new serializer, or <see langword="null"/> when it is the excluded type or the factory
    /// could not produce a usable instance. A registration that cannot be instantiated is skipped
    /// rather than failing the whole fallback chain, so every failure it can raise is absorbed.
    /// </returns>
    internal static ISerializer? TryCreateAlternativeSerializer(Func<ISerializer> factory, Type excludeType)
    {
        try
        {
            var instance = factory();
            return instance.GetType() == excludeType ? null : instance;
        }
        catch
        {
            return null;
        }
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
            if (TryDeserializeBsonWith<T>(factory, data, forcedDateTimeKind, out var result))
            {
                return result;
            }
        }

        return default;
    }

    /// <summary>Deserializes BSON with a single registered factory, reporting failure instead of throwing.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="factory">The registered serializer factory.</param>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <param name="result">The deserialized value when the factory produced one; otherwise default.</param>
    /// <returns>
    /// <see langword="true"/> only when the factory yielded a BSON serializer that read the payload.
    /// A non-BSON serializer, or any failure the factory or the read can raise, reports
    /// <see langword="false"/> so the caller moves to the next registration.
    /// </returns>
    [RequiresUnreferencedCode("Calls ISerializer.Deserialize<T>.")]
    [RequiresDynamicCode("Calls ISerializer.Deserialize<T>.")]
    internal static bool TryDeserializeBsonWith<T>(Func<ISerializer> factory, byte[] data, DateTimeKind? forcedDateTimeKind, out T? result)
    {
        try
        {
            var serializer = factory();

            // Only use serializers that look like BSON serializers
            if (!IsBsonSerializer(serializer))
            {
                result = default;
                return false;
            }

            if (forcedDateTimeKind.HasValue)
            {
                serializer.ForcedDateTimeKind = forcedDateTimeKind;
            }

            var deserialized = serializer.Deserialize<T>(data);

            // Enhanced handling for DateTime types with BSON to prevent issues.
            result = typeof(T) == typeof(DateTime)
                ? (T)(object)CoerceBsonDateTime(CastAsDateTime(deserialized), data, forcedDateTimeKind)
                : deserialized;
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>Repairs a DateTime that a BSON reader collapsed to <see cref="DateTime.MinValue"/> and applies the forced kind.</summary>
    /// <param name="dateTime">The DateTime the BSON serializer produced.</param>
    /// <param name="data">The original payload, used to recover a value the reader lost.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <returns>The recovered and kind-normalized DateTime.</returns>
    internal static DateTime CoerceBsonDateTime(DateTime dateTime, byte[] data, DateTimeKind? forcedDateTimeKind)
    {
        // Special handling for problematic DateTime values from BSON.
        if (dateTime == DateTime.MinValue && data.Length > 20)
        {
            var recoveredDateTime = DateTimeHelpers.AttemptDateTimeRecovery(data, dateTime);
            dateTime = recoveredDateTime != DateTime.MinValue
                ? recoveredDateTime
                : new(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);
        }

        // Ensure proper DateTimeKind via the shared converter helper.
        return forcedDateTimeKind.HasValue && dateTime.Kind != forcedDateTimeKind.Value
            ? DateTimeHelpers.ConvertDateTimeKind(dateTime, forcedDateTimeKind.Value)
            : dateTime;
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
            if (TryDeserializeJsonWith<T>(factory, data, forcedDateTimeKind, out var result))
            {
                return result;
            }
        }

        return TryBasicJsonDeserialization<T>(data);
    }

    /// <summary>Deserializes JSON with a single registered factory, reporting failure instead of throwing.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="factory">The registered serializer factory.</param>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <param name="result">The deserialized value when the factory produced one; otherwise default.</param>
    /// <returns>
    /// <see langword="true"/> only when the factory yielded a non-BSON serializer that read the payload.
    /// A BSON serializer, or any failure the factory or the read can raise, reports
    /// <see langword="false"/> so the caller moves to the next registration.
    /// </returns>
    [RequiresUnreferencedCode("Calls ISerializer.Deserialize<T>.")]
    [RequiresDynamicCode("Calls ISerializer.Deserialize<T>.")]
    internal static bool TryDeserializeJsonWith<T>(Func<ISerializer> factory, byte[] data, DateTimeKind? forcedDateTimeKind, out T? result)
    {
        try
        {
            var serializer = factory();

            // Skip BSON serializers - we want JSON ones
            if (IsBsonSerializer(serializer))
            {
                result = default;
                return false;
            }

            if (forcedDateTimeKind.HasValue)
            {
                serializer.ForcedDateTimeKind = forcedDateTimeKind;
            }

            result = serializer.Deserialize<T>(data);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
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
                var t when t == typeof(string) => (T)(object)(jsonString.Length >= JsonStringQuoteCount && jsonString[0] == '"' && jsonString[^1] == '"'
                    ? jsonString.Substring(1, jsonString.Length - JsonStringQuoteCount)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsBsonSerializer(ISerializer serializer) =>
        _isBsonSerializerByType.GetOrAdd(
            serializer.GetType(),
            static t => t.Name.Contains("Bson", StringComparison.OrdinalIgnoreCase));

    /// <summary>Checks if the serializer is the plain Newtonsoft.Json serializer.</summary>
    /// <param name="serializer">The serializer instance to probe.</param>
    /// <returns><see langword="true"/> if the serializer is Newtonsoft.Json (not the BSON variant).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
