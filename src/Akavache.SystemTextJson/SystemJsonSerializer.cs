// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Akavache.SystemTextJson;

/// <summary>
/// A serializer using System.Text.Json for JSON serialization.
/// </summary>
public class SystemJsonSerializer : ISerializer
{
    /// <summary>
    /// Gets or sets the JSON serializer options for customizing serialization behavior.
    /// </summary>
    public JsonSerializerOptions? Options { get; set; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <summary>
    /// Deserializes from bytes using the provided <see cref="JsonTypeInfo{T}"/> for
    /// AOT-safe deserialization. Static because the <see cref="JsonTypeInfo{T}"/>
    /// already encodes every option the serializer would otherwise read from
    /// <see cref="Options"/>; the dispatch extension method in
    /// <c>Akavache.SystemTextJson.Bson</c> calls into this directly.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The bytes.</param>
    /// <param name="jsonTypeInfo">The JSON type information for AOT-safe deserialization.</param>
    /// <returns>The deserialized instance, or <c>default</c> when <paramref name="bytes"/> is null or empty.</returns>
    public static T? DeserializeAot<T>(byte[] bytes, JsonTypeInfo<T> jsonTypeInfo)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        return JsonSerializer.Deserialize(bytes, jsonTypeInfo);
    }

    /// <summary>
    /// Serializes to bytes using the provided <see cref="JsonTypeInfo{T}"/> for
    /// AOT-safe serialization. Static for the same reason as
    /// <see cref="DeserializeAot{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <param name="jsonTypeInfo">The JSON type information for AOT-safe serialization.</param>
    /// <returns>The serialized bytes.</returns>
    public static byte[] SerializeAot<T>(T item, JsonTypeInfo<T> jsonTypeInfo) =>
        JsonSerializer.SerializeToUtf8Bytes(item, jsonTypeInfo);

    /// <inheritdoc/>
    [RequiresUnreferencedCode("Reflection-based JSON deserialization. For AOT, use the JsonTypeInfo overload.")]
    [RequiresDynamicCode("Reflection-based JSON deserialization. For AOT, use the JsonTypeInfo overload.")]
    public T? Deserialize<T>(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        var options = GetEffectiveOptions();
        return JsonSerializer.Deserialize<T>(bytes, options);
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode("Reflection-based JSON serialization. For AOT, use the JsonTypeInfo overload.")]
    [RequiresDynamicCode("Reflection-based JSON serialization. For AOT, use the JsonTypeInfo overload.")]
    public byte[] Serialize<T>(T item)
    {
        var options = GetEffectiveOptions();
        return JsonSerializer.SerializeToUtf8Bytes(item, options);
    }

    /// <summary>
    /// Gets the effective JsonSerializerOptions for this serializer.
    /// </summary>
    /// <returns>The configured JsonSerializerOptions.</returns>
    internal JsonSerializerOptions GetEffectiveOptions()
    {
        var options = Options ?? new JsonSerializerOptions();

        // Clone options to avoid modifying the original
        var effectiveOptions = new JsonSerializerOptions(options);

        return effectiveOptions;
    }
}
