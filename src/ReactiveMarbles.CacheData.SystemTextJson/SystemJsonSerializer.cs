// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.SystemTextJson;

/// <summary>
/// A converter using System.Text.Json.
/// </summary>
public class SystemJsonSerializer : ISerializer
{
    /// <summary>
    /// Gets or sets the optional options.
    /// </summary>
    public JsonSerializerOptions? Options { get; set; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <inheritdoc/>
    public T? Deserialize<T>(byte[] bytes) => (T?)JsonSerializer.Deserialize(bytes, typeof(T), GetEffectiveOptions());

    /// <inheritdoc/>
    public byte[] Serialize<T>(T item) => JsonSerializer.SerializeToUtf8Bytes(item, GetEffectiveOptions());

    private JsonSerializerOptions GetEffectiveOptions()
    {
        var options = Options ?? new JsonSerializerOptions();

        // If ForcedDateTimeKind is set, we need to add custom converters
        if (ForcedDateTimeKind.HasValue)
        {
            // Create a copy to avoid modifying the original options
            options = new JsonSerializerOptions(options);

            // Add custom DateTime converters that respect ForcedDateTimeKind
            options.Converters.Add(new SystemJsonDateTimeConverter(ForcedDateTimeKind.Value));
            options.Converters.Add(new SystemJsonNullableDateTimeConverter(ForcedDateTimeKind.Value));
        }

        return options;
    }
}
