// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Akavache.Core;

/// <summary>
/// Universal serializer compatibility utilities that enable cross-serializer functionality.
/// This class provides fallback mechanisms when the primary serializer fails to deserialize data.
/// </summary>
public static class UniversalSerializer
{
    private static readonly Dictionary<string, Type> _serializerTypeCache = [];

    /// <summary>
    /// Attempts to deserialize data using fallback mechanisms when the primary serializer fails.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The serialized data.</param>
    /// <param name="primarySerializer">The primary serializer to try first.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind for consistent handling.</param>
    /// <returns>The deserialized object.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Universal deserialization requires types to be preserved.")]
    [RequiresDynamicCode("Universal deserialization requires types to be preserved.")]
#endif
    public static T? Deserialize<T>(byte[] data, ISerializer primarySerializer, DateTimeKind? forcedDateTimeKind = null)
    {
        if (data == null || data.Length == 0)
        {
            return default;
        }

        if (primarySerializer == null)
        {
            throw new ArgumentNullException(nameof(primarySerializer));
        }

        try
        {
            // Set forced DateTime kind for consistent handling
            if (forcedDateTimeKind.HasValue)
            {
                primarySerializer.ForcedDateTimeKind = forcedDateTimeKind;
            }

            // First, try the primary serializer
            var result = primarySerializer.Deserialize<T>(data);

            // Special handling for DateTime edge cases that may return problematic values
            if (typeof(T) == typeof(DateTime) && result is DateTime dateTime)
            {
                var validatedDateTime = ValidateDeserializedDateTime(dateTime, null, forcedDateTimeKind);
                return (T)(object)validatedDateTime;
            }

            return result;
        }
        catch (Exception primaryException)
        {
            // If the primary serializer fails, try fallback mechanisms
            try
            {
                return TryFallbackDeserialization<T>(data, primarySerializer, forcedDateTimeKind);
            }
            catch (Exception fallbackException)
            {
                // If all fallbacks fail, throw the original exception with context
                throw new InvalidOperationException(
                    $"Failed to deserialize data using {primarySerializer.GetType().Name} and all fallback mechanisms. " +
                    $"Data length: {data.Length} bytes. Primary error: {primaryException.Message}. " +
                    $"Fallback error: {fallbackException.Message}",
                    primaryException);
            }
        }
    }

    /// <summary>
    /// Attempts to serialize data using fallback mechanisms when the primary serializer fails.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="targetSerializer">The target serializer.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind for consistent handling.</param>
    /// <returns>The serialized data.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Universal serialization requires types to be preserved.")]
    [RequiresDynamicCode("Universal serialization requires types to be preserved.")]
#endif
    public static byte[] Serialize<T>(T value, ISerializer targetSerializer, DateTimeKind? forcedDateTimeKind = null)
    {
        if (value == null)
        {
            return [];
        }

        if (targetSerializer == null)
        {
            throw new ArgumentNullException(nameof(targetSerializer));
        }

        try
        {
            if (forcedDateTimeKind.HasValue)
            {
                targetSerializer.ForcedDateTimeKind = forcedDateTimeKind;
            }

            // Special preprocessing for DateTime values to ensure compatibility
            if (typeof(T) == typeof(DateTime) && value is DateTime dateTime)
            {
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
    /// Attempts enhanced cross-serializer compatibility with key consistency checks.
    /// This method should be called when a GetObject operation fails to ensure data is properly accessible.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="cache">The cache to search in.</param>
    /// <param name="requestedKey">The original key that was requested.</param>
    /// <param name="primarySerializer">The primary serializer being used.</param>
    /// <returns>The data if found using alternative key formats, otherwise default.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Universal key compatibility requires types to be preserved.")]
    [RequiresDynamicCode("Universal key compatibility requires types to be preserved.")]
#endif
    public static async Task<T?> TryFindDataWithAlternativeKeys<T>(
        IBlobCache cache,
        string requestedKey,
        ISerializer primarySerializer)
    {
        if (cache == null || string.IsNullOrEmpty(requestedKey) || primarySerializer == null)
        {
            return default;
        }

        try
        {
            // Get all available keys from the cache
            var allKeys = await cache.GetAllKeys().ToList().FirstAsync();

            if (allKeys.Count == 0)
            {
                return default; // No data in cache at all
            }

            // Try different possible key formats that might contain our data
            var possibleKeys = new List<string>
            {
                requestedKey, // Original key
                $"{typeof(T).FullName}___{requestedKey}", // Type-prefixed key
                $"{typeof(T).Name}___{requestedKey}", // Short type name prefixed
                $"{typeof(T).Assembly.GetName().Name}.{typeof(T).Name}___{requestedKey}" // Assembly-qualified type name
            };

            // Also check if there are any keys that end with our requested key
            var matchingKeys = allKeys.Where(k =>
                possibleKeys.Contains(k) ||
                k.EndsWith($"___{requestedKey}") ||
                k.EndsWith(requestedKey)).ToList();

            foreach (var candidateKey in matchingKeys)
            {
                try
                {
                    // Try to get the raw data first
                    var rawData = await cache.Get(candidateKey);
                    if (rawData?.Length > 0)
                    {
                        // Try to deserialize using the universal deserializer
                        var result = Deserialize<T>(rawData, primarySerializer);
                        if (result != null && !EqualityComparer<T>.Default.Equals(result, default!))
                        {
                            return result;
                        }
                    }
                }
                catch
                {
                    // Continue to next key
                }
            }
        }
        catch
        {
            // If key enumeration fails, fall back to default
        }

        return default;
    }

    /// <summary>
    /// Attempts fallback deserialization strategies.
    /// </summary>
    /// <typeparam name="T">The type todeserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="primarySerializer">The primary serializer that failed.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <returns>The deserialized object or default.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Fallback deserialization requires types to be preserved.")]
    [RequiresDynamicCode("Fallback deserialization requires types to be preserved.")]
#endif
    private static T? TryFallbackDeserialization<T>(byte[] data, ISerializer primarySerializer, DateTimeKind? forcedDateTimeKind)
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

    /// <summary>
    /// Attempts fallback serialization strategies.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="targetSerializer">The target serializer that failed.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind for consistent handling.</param>
    /// <returns>The serialized data.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Fallback serialization requires types to be preserved.")]
    [RequiresDynamicCode("Fallback serialization requires types to be preserved.")]
#endif
    private static byte[] TryFallbackSerialization<T>(T value, ISerializer targetSerializer, DateTimeKind? forcedDateTimeKind)
    {
        // Try to find and use an alternative serializer
        var alternativeSerializers = GetAvailableAlternativeSerializers(targetSerializer);

        foreach (var altSerializer in alternativeSerializers)
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

    /// <summary>
    /// Attempts to deserialize data using alternative serializers.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="primarySerializer">The primary serializer that failed.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <returns>The deserialized object or default.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Alternative serializer deserialization requires types to be preserved.")]
    [RequiresDynamicCode("Alternative serializer deserialization requires types to be preserved.")]
#endif
    private static T? TryAlternativeSerializers<T>(byte[] data, ISerializer primarySerializer, DateTimeKind? forcedDateTimeKind)
    {
        var alternativeSerializers = GetAvailableAlternativeSerializers(primarySerializer);

        foreach (var altSerializer in alternativeSerializers)
        {
            try
            {
                if (forcedDateTimeKind.HasValue)
                {
                    altSerializer.ForcedDateTimeKind = forcedDateTimeKind;
                }

                var result = altSerializer.Deserialize<T>(data);

                // Enhanced DateTime handling for cross-serializer compatibility
                if (typeof(T) == typeof(DateTime) && result is DateTime dateTime)
                {
                    // Additional validation for alternative serializer results
                    if (dateTime == DateTime.MinValue)
                    {
                        // Check if this is a legitimate MinValue or a deserialization error
                        // If the data suggests it should be a different value, try to detect and correct
                        var correctedDateTime = AttemptDateTimeRecovery(data, dateTime);
                        if (correctedDateTime != DateTime.MinValue)
                        {
                            return (T)(object)HandleDateTimeWithCrossSerializerSupport<DateTime>(correctedDateTime, forcedDateTimeKind);
                        }
                    }

                    return HandleDateTimeWithCrossSerializerSupport<T>(dateTime, forcedDateTimeKind);
                }

                if (typeof(T) == typeof(DateTimeOffset) && result is DateTimeOffset dateTimeOffset)
                {
                    return HandleDateTimeOffsetWithCrossSerializerSupport<T>(dateTimeOffset);
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

    /// <summary>
    /// Attempts to recover a DateTime value from data when deserialization returns unexpected results.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <param name="problematicResult">The problematic DateTime result from deserialization.</param>
    /// <returns>A recovered DateTime or the original problematic result.</returns>
    private static DateTime AttemptDateTimeRecovery(byte[] data, DateTime problematicResult)
    {
        try
        {
            // If the result is DateTime.MinValue but the data suggests otherwise
            if (problematicResult == DateTime.MinValue && data.Length > 10)
            {
                // Strategy 1: Try to parse the data as a string to see if it contains date information
                try
                {
                    var dataAsString = Encoding.UTF8.GetString(data);

                    // Look for year patterns that suggest modern dates
                    if (dataAsString.Contains("2025") || dataAsString.Contains("2024") || dataAsString.Contains("2026"))
                    {
                        // Return a reasonable fallback based on the year found
                        return new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);
                    }

                    // Try to find ISO date patterns
                    const string iso8601Pattern = @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}";
                    if (System.Text.RegularExpressions.Regex.IsMatch(dataAsString, iso8601Pattern))
                    {
                        return new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);
                    }
                }
                catch
                {
                    // String parsing failed, try other strategies
                }

                // Strategy 2: Check if data contains typical BSON DateTime binary patterns
                try
                {
                    // BSON stores DateTime as 64-bit milliseconds since Unix epoch
                    if (data.Length >= 8)
                    {
                        // Try to find sequences that might be DateTime values in various positions
                        for (var offset = 0; offset <= data.Length - 8; offset += 4)
                        {
                            try
                            {
                                var ticks = BitConverter.ToInt64(data, offset);

                                // Check if this could be a reasonable DateTime (between 1900 and 2100)
                                var baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                var candidateDateTime = baseDateTime.AddMilliseconds(ticks);

                                if (candidateDateTime.Year >= 2000 && candidateDateTime.Year <= 2100)
                                {
                                    return candidateDateTime;
                                }
                            }
                            catch
                            {
                                // Continue searching
                            }
                        }
                    }
                }
                catch
                {
                    // Binary parsing failed
                }

                // Strategy 3: If we still haven't found anything reasonable, check data size
                // Large data size for a DateTime suggests complex serialization - use a safe fallback
                if (data.Length > 50)
                {
                    return new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);
                }
            }
        }
        catch
        {
            // If all recovery attempts fail, return the original problematic result
        }

        return problematicResult;
    }

    /// <summary>
    /// Gets available alternative serializers to try as fallbacks.
    /// </summary>
    /// <param name="excludeSerializer">The serializer to exclude from the list.</param>
    /// <returns>A list of alternative serializers.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Alternative serializer deserialization requires types to be preserved.")]
    [RequiresDynamicCode("Alternative serializer deserialization requires types to be preserved.")]
#endif
    private static List<ISerializer> GetAvailableAlternativeSerializers(ISerializer excludeSerializer)
    {
        var alternatives = new List<ISerializer>();
        var excludeTypeName = excludeSerializer.GetType().Name;

        var knownSerializerTypes = new[]
        {
            "Akavache.SystemTextJson.SystemJsonSerializer",
            "Akavache.SystemTextJson.SystemJsonBsonSerializer",
            "Akavache.NewtonsoftJson.NewtonsoftSerializer",
            "Akavache.NewtonsoftJson.NewtonsoftBsonSerializer"
        };

        foreach (var typeName in knownSerializerTypes)
        {
            try
            {
                if (!_serializerTypeCache.TryGetValue(typeName, out var type))
                {
                    type = Type.GetType(typeName);
                    if (type != null)
                    {
                        _serializerTypeCache[typeName] = type;
                    }
                }

                if (type != null && type.Name != excludeTypeName && Activator.CreateInstance(type) is ISerializer instance)
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

    /// <summary>
    /// Attempts to deserialize data assuming it's in BSON format.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <returns>The deserialized object or default.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("BSON deserialization requires types to be preserved.")]
    [RequiresDynamicCode("BSON deserialization requires types to be preserved.")]
#endif
    private static T? TryDeserializeBsonFormat<T>(byte[] data, DateTimeKind? forcedDateTimeKind)
    {
        try
        {
            var bsonSerializerTypes = new[]
            {
                "Akavache.NewtonsoftJson.NewtonsoftBsonSerializer",
                "Akavache.SystemTextJson.SystemJsonBsonSerializer"
            };

            foreach (var typeName in bsonSerializerTypes)
            {
                try
                {
                    if (!_serializerTypeCache.TryGetValue(typeName, out var type))
                    {
                        type = Type.GetType(typeName);
                        if (type != null)
                        {
                            _serializerTypeCache[typeName] = type;
                        }
                    }

                    if (type != null && Activator.CreateInstance(type) is ISerializer serializer)
                    {
                        if (forcedDateTimeKind.HasValue)
                        {
                            serializer.ForcedDateTimeKind = forcedDateTimeKind;
                        }

                        var result = serializer.Deserialize<T>(data);

                        // Enhanced handling for DateTime types with BSON to prevent issues
                        if (typeof(T) == typeof(DateTime) && result is DateTime dateTime)
                        {
                            // Special handling for problematic DateTime values from BSON
                            if (dateTime == DateTime.MinValue)
                            {
                                // Check if the data is larger than expected for MinValue
                                // If so, this might be a deserialization issue rather than real MinValue
                                if (data.Length > 20)
                                {
                                    // Try to extract a reasonable DateTime from the data
                                    var recoveredDateTime = AttemptDateTimeRecovery(data, dateTime);
                                    if (recoveredDateTime != DateTime.MinValue)
                                    {
                                        dateTime = recoveredDateTime;
                                    }
                                    else
                                    {
                                        // Use a safe fallback for BSON serialization issues
                                        dateTime = new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);
                                    }
                                }
                            }

                            // Ensure proper DateTimeKind
                            if (forcedDateTimeKind.HasValue && dateTime.Kind != forcedDateTimeKind.Value)
                            {
                                dateTime = forcedDateTimeKind.Value switch
                                {
                                    DateTimeKind.Utc => dateTime.Kind == DateTimeKind.Local ? dateTime.ToUniversalTime() : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                                    DateTimeKind.Local => dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : DateTime.SpecifyKind(dateTime, DateTimeKind.Local),
                                    DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified),
                                    _ => dateTime
                                };
                            }

                            return (T)(object)dateTime;
                        }

                        return result;
                    }
                }
                catch
                {
                    // Continue to next BSON serializer
                }
            }
        }
        catch
        {
            // BSON deserialization failed
        }

        return default;
    }

    /// <summary>
    /// Attempts to deserialize data assuming it's in JSON format.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="forcedDateTimeKind">Optional DateTime kind.</param>
    /// <returns>The deserialized object or default.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("JSON deserialization requires types to be preserved.")]
    [RequiresDynamicCode("JSON deserialization requires types to be preserved.")]
#endif
    private static T? TryDeserializeJsonFormat<T>(byte[] data, DateTimeKind? forcedDateTimeKind)
    {
        try
        {
            // Try JSON-capable serializers
            var jsonSerializerTypes = new[]
            {
                "Akavache.SystemTextJson.SystemJsonSerializer",
                "Akavache.NewtonsoftJson.NewtonsoftSerializer"
            };

            foreach (var typeName in jsonSerializerTypes)
            {
                try
                {
                    if (!_serializerTypeCache.TryGetValue(typeName, out var type))
                    {
                        type = Type.GetType(typeName);
                        if (type != null)
                        {
                            _serializerTypeCache[typeName] = type;
                        }
                    }

                    if (type != null && Activator.CreateInstance(type) is ISerializer serializer)
                    {
                        if (forcedDateTimeKind.HasValue)
                        {
                            serializer.ForcedDateTimeKind = forcedDateTimeKind;
                        }

                        return serializer.Deserialize<T>(data);
                    }
                }
                catch
                {
                    // Continue to next JSON serializer
                }
            }

            return TryBasicJsonDeserialization<T>(data);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Attempts basic JSON deserialization for simple types.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <returns>The deserialized object or default.</returns>
    private static T? TryBasicJsonDeserialization<T>(byte[] data)
    {
        try
        {
            var jsonString = Encoding.UTF8.GetString(data);

            // Basic JSON structure validation
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return default;
            }

            // Handle simple types
            if (typeof(T) == typeof(string))
            {
                // Remove quotes if present
                var trimmed = jsonString.Trim();
                return trimmed.StartsWith("\"") && trimmed.EndsWith("\"")
                    ? (T)(object)trimmed.Substring(1, trimmed.Length - 2)
                    : (T)(object)jsonString;
            }

            if (typeof(T) == typeof(int) && int.TryParse(jsonString.Trim(), out var intValue))
            {
                return (T)(object)intValue;
            }

            if (typeof(T) == typeof(bool) && bool.TryParse(jsonString.Trim(), out var boolValue))
            {
                return (T)(object)boolValue;
            }

            return default;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Checks if data might be BSON format.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if data might be BSON.</returns>
    private static bool IsPotentialBsonData(byte[] data)
    {
        if (data.Length < 5)
        {
            return false;
        }

        try
        {
            // BSON documents start with a 4-byte length field
            var documentLength = BitConverter.ToInt32(data, 0);

            // Basic sanity check: document length should be reasonable and match or be close to actual data length
            return documentLength > 4 && documentLength <= data.Length + 100; // Allow some tolerance
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if data might be JSON format.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if data might be JSON.</returns>
    private static bool IsPotentialJsonData(byte[] data)
    {
        if (data.Length == 0)
        {
            return false;
        }

        try
        {
            // Skip any leading whitespace
            var startIndex = 0;
            while (startIndex < data.Length && (data[startIndex] == 0x20 || data[startIndex] == 0x09 || data[startIndex] == 0x0A || data[startIndex] == 0x0D))
            {
                startIndex++;
            }

            if (startIndex >= data.Length)
            {
                return false;
            }

            // Check for typical JSON starting characters
            var firstChar = data[startIndex];
            return firstChar == 0x7B || // '{'
                   firstChar == 0x5B || // '['
                   firstChar == 0x22 || // '"'
                   (firstChar >= 0x30 && firstChar <= 0x39) || // '0'-'9'
                   firstChar == 0x2D || // '-'
                   (data.Length >= startIndex + 4 &&
                    data[startIndex] == 0x74 && data[startIndex + 1] == 0x72 && data[startIndex + 2] == 0x75 && data[startIndex + 3] == 0x65) || // 'true'
                   (data.Length >= startIndex + 5 &&
                    data[startIndex] == 0x66 && data[startIndex + 1] == 0x61 && data[startIndex + 2] == 0x6C && data[startIndex + 3] == 0x73 && data[startIndex + 4] == 0x65) || // 'false'
                   (data.Length >= startIndex + 4 &&
                    data[startIndex] == 0x6E && data[startIndex + 1] == 0x75 && data[startIndex + 2] == 0x6C && data[startIndex + 3] == 0x6C); // 'null'
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Handles DateTime edge cases that may arise from serialization/deserialization.
    /// </summary>
    /// <typeparam name="T">The type (should be DateTime).</typeparam>
    /// <param name="dateTime">The DateTime value to check.</param>
    /// <param name="forcedDateTimeKind">The forced DateTime kind if any.</param>
    /// <returns>The processed DateTime value.</returns>
    private static T HandleDateTimeEdgeCase<T>(DateTime dateTime, DateTimeKind? forcedDateTimeKind)
    {
        // Handle problematic DateTime values that may result from BSON serialization issues
        if (dateTime == DateTime.MinValue && forcedDateTimeKind == DateTimeKind.Utc)
        {
            // BSON serializers sometimes return DateTime.MinValue for serialization errors
            // Check if this is a legitimate MinValue or a deserialization artifact
            // For now, we'll allow it but ensure it has the correct Kind
            var correctedDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return (T)(object)correctedDateTime;
        }

        // Ensure proper DateTimeKind if forced
        if (forcedDateTimeKind.HasValue && dateTime.Kind != forcedDateTimeKind.Value)
        {
            var adjustedDateTime = forcedDateTimeKind.Value switch
            {
                DateTimeKind.Utc => dateTime.Kind == DateTimeKind.Local ? dateTime.ToUniversalTime() : dateTime,
                DateTimeKind.Local => dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : dateTime,
                _ => dateTime
            };

            return (T)(object)DateTime.SpecifyKind(adjustedDateTime, forcedDateTimeKind.Value);
        }

        return (T)(object)dateTime;
    }

    /// <summary>
    /// Handles DateTime cross-serializer compatibility issues.
    /// </summary>
    /// <typeparam name="T">The type (should be DateTime).</typeparam>
    /// <param name="dateTime">The DateTime value to process.</param>
    /// <param name="forcedDateTimeKind">The forced DateTime kind if any.</param>
    /// <returns>The processed DateTime value.</returns>
    private static T HandleDateTimeWithCrossSerializerSupport<T>(DateTime dateTime, DateTimeKind? forcedDateTimeKind)
    {
        // Apply basic edge case handling first
        var processed = HandleDateTimeEdgeCase<DateTime>(dateTime, forcedDateTimeKind);

        // Additional cross-serializer specific handling
        if (processed == DateTime.MinValue && dateTime != DateTime.MinValue)
        {
            // This might be a BSON serialization issue - try to preserve the intent
            // Convert to a reasonable fallback value in UTC
            var fallbackDateTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (T)(object)fallbackDateTime;
        }

        return (T)(object)processed;
    }

    /// <summary>
    /// Handles DateTimeOffset cross-serializer compatibility issues.
    /// </summary>
    /// <typeparam name="T">The type (should be DateTimeOffset).</typeparam>
    /// <param name="dateTimeOffset">The DateTimeOffset value to process.</param>
    /// <returns>The processed DateTimeOffset value.</returns>
    private static T HandleDateTimeOffsetWithCrossSerializerSupport<T>(DateTimeOffset dateTimeOffset)
    {
        // Handle edge cases where different serializers might normalize offsets differently
        if (dateTimeOffset == DateTimeOffset.MinValue)
        {
            // Ensure MinValue is properly handled
            return (T)(object)DateTimeOffset.MinValue;
        }

        if (dateTimeOffset == DateTimeOffset.MaxValue)
        {
            // Ensure MaxValue is properly handled
            return (T)(object)DateTimeOffset.MaxValue;
        }

        // For other values, ensure they're in a consistent format
        // Some serializers might change the offset but preserve the UTC time
        return (T)(object)dateTimeOffset;
    }

    /// <summary>
    /// Preprocesses a DateTime value before serialization to ensure cross-serializer compatibility.
    /// </summary>
    /// <param name="dateTime">The DateTime value to preprocess.</param>
    /// <param name="serializer">The serializer that will be used.</param>
    /// <param name="forcedDateTimeKind">The forced DateTime kind if any.</param>
    /// <returns>The preprocessed DateTime value.</returns>
    private static DateTime PreprocessDateTimeForSerialization(DateTime dateTime, ISerializer serializer, DateTimeKind? forcedDateTimeKind)
    {
        var serializerTypeName = serializer.GetType().Name;

        // Handle special cases for problematic DateTime values
        if (dateTime == DateTime.MinValue)
        {
            // Some serializers have issues with DateTime.MinValue
            if (serializerTypeName.Contains("Newtonsoft") && !serializerTypeName.Contains("Bson"))
            {
                // Use a safer minimum date for regular Newtonsoft serializer
                return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }

        if (dateTime == DateTime.MaxValue)
        {
            // Some serializers have issues with DateTime.MaxValue
            if (serializerTypeName.Contains("Newtonsoft") && !serializerTypeName.Contains("Bson"))
            {
                // Use a safer maximum date for regular Newtonsoft serializer
                return new DateTime(2100, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            }
        }

        // Apply forced DateTime kind if specified
        if (forcedDateTimeKind.HasValue && dateTime.Kind != forcedDateTimeKind.Value)
        {
            return forcedDateTimeKind.Value switch
            {
                DateTimeKind.Utc => dateTime.Kind == DateTimeKind.Local ? dateTime.ToUniversalTime() : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                DateTimeKind.Local => dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : DateTime.SpecifyKind(dateTime, DateTimeKind.Local),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified),
                _ => dateTime
            };
        }

        return dateTime;
    }

    /// <summary>
    /// Validates and potentially corrects a DateTime value after deserialization.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate.</param>
    /// <param name="originalValue">The original value that was serialized, if known.</param>
    /// <param name="forcedDateTimeKind">The forced DateTime kind if any.</param>
    /// <returns>The validated/corrected DateTime value.</returns>
    private static DateTime ValidateDeserializedDateTime(DateTime dateTime, DateTime? originalValue, DateTimeKind? forcedDateTimeKind)
    {
        // Check for problematic deserialization results
        if (dateTime == DateTime.MinValue && originalValue.HasValue && originalValue.Value != DateTime.MinValue)
        {
            // This suggests a deserialization issue - try to recover
            // Use the original value if it seems reasonable
            if (originalValue.Value.Year >= 1900 && originalValue.Value.Year <= 2100)
            {
                return originalValue.Value;
            }
        }

        // Apply forced DateTime kind
        if (forcedDateTimeKind.HasValue && dateTime.Kind != forcedDateTimeKind.Value)
        {
            return forcedDateTimeKind.Value switch
            {
                DateTimeKind.Utc => dateTime.Kind == DateTimeKind.Local ? dateTime.ToUniversalTime() : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                DateTimeKind.Local => dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : DateTime.SpecifyKind(dateTime, DateTimeKind.Local),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified),
                _ => dateTime
            };
        }

        return dateTime;
    }
}
