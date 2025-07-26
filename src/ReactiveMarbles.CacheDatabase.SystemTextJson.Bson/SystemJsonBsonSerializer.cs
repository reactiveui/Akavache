// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using ReactiveMarbles.CacheDatabase.Core;
using Splat;

namespace ReactiveMarbles.CacheDatabase.SystemTextJson.Bson;

/// <summary>
/// A hybrid serializer that uses System.Text.Json for object serialization but writes to/reads from BSON format.
/// This provides compatibility with BSON-based caches while using System.Text.Json's performance.
/// </summary>
public class SystemJsonBsonSerializer : ISerializer, IEnableLogger
{
    /// <summary>
    /// Gets or sets the optional System.Text.Json options.
    /// </summary>
    public JsonSerializerOptions? Options { get; set; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <inheritdoc/>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using hybrid System.Text.Json with BSON requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using hybrid System.Text.Json with BSON requires types to be preserved for deserialization.")]
#endif
    public T? Deserialize<T>(byte[] bytes)
    {
        try
        {
            // Try to read as BSON first (for compatibility with existing BSON data)
            using var ms = new MemoryStream(bytes);
            using var reader = new BsonDataReader(ms);

            if (ForcedDateTimeKind.HasValue)
            {
                reader.DateTimeKindHandling = ForcedDateTimeKind.Value;
            }

            // Read BSON into a Newtonsoft JToken first
            var token = Newtonsoft.Json.Linq.JToken.ReadFrom(reader);

            // Convert JToken to JSON string
            var jsonString = token.ToString(Formatting.None);

            // Try to deserialize as ObjectWrapper first (for Akavache compatibility)
            try
            {
                var options = GetEffectiveOptions();
                var wrapper = System.Text.Json.JsonSerializer.Deserialize<ObjectWrapper<T>>(jsonString, options);
                if (wrapper is not null && wrapper.Value is not null)
                {
                    return wrapper.Value;
                }
            }
            catch (Exception wrapperEx)
            {
                this.Log().Debug(wrapperEx, "Failed to deserialize as ObjectWrapper, trying direct deserialization");
            }

            // Fallback to direct deserialization
            var directOptions = GetEffectiveOptions();
            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, directOptions);
        }
        catch (Exception ex)
        {
            this.Log().Debug(ex, "Failed to deserialize as BSON, attempting direct System.Text.Json deserialization");

            // Fallback: try to deserialize as regular JSON with System.Text.Json
            try
            {
                var options = GetEffectiveOptions();
                return System.Text.Json.JsonSerializer.Deserialize<T>(bytes, options);
            }
            catch (Exception fallbackEx)
            {
                this.Log().Error(fallbackEx, "Failed to deserialize data with both BSON and JSON methods");
                throw;
            }
        }
    }

    /// <inheritdoc/>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using hybrid System.Text.Json with BSON requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using hybrid System.Text.Json with BSON requires types to be preserved for serialization.")]
#endif
    public byte[] Serialize<T>(T item)
    {
        try
        {
            // Wrap the item for compatibility with Akavache BSON format
            var wrapper = new ObjectWrapper<T>(item);

            // Serialize to JSON string using System.Text.Json
            var options = GetEffectiveOptions();
            var jsonString = System.Text.Json.JsonSerializer.Serialize(wrapper, options);

            // Parse JSON string into Newtonsoft JToken
            var token = Newtonsoft.Json.Linq.JToken.Parse(jsonString);

            // Write JToken to BSON
            using var ms = new MemoryStream();
            using var writer = new BsonDataWriter(ms);

            token.WriteTo(writer);

            return ms.ToArray();
        }
        catch (Exception ex)
        {
            this.Log().Error(ex, "Failed to serialize to BSON format");
            throw;
        }
    }

    private JsonSerializerOptions GetEffectiveOptions()
    {
        var options = Options ?? new JsonSerializerOptions();

        // Add custom DateTime converters - default to UTC if no ForcedDateTimeKind is specified
        // This ensures consistency with BSON cache behavior
        var targetKind = ForcedDateTimeKind ?? DateTimeKind.Utc;

        // Create a copy to avoid modifying the original options
        options = new JsonSerializerOptions(options);

        // Add custom DateTime converters that respect the target DateTimeKind
        options.Converters.Add(new SystemJsonDateTimeConverter(targetKind));
        options.Converters.Add(new SystemJsonNullableDateTimeConverter(targetKind));

        return options;
    }

    /// <summary>
    /// Object wrapper for compatibility with Akavache BSON format.
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
        public ObjectWrapper(T value) => Value = value;

        /// <summary>
        /// Gets or sets the wrapped value.
        /// </summary>
        public T Value { get; set; } = default!;
    }
}
