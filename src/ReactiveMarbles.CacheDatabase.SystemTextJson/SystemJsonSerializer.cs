// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using System.Text.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using System.Text.Json requires types to be preserved for deserialization.")]
#endif
    public T? Deserialize<T>(byte[] bytes) => JsonSerializer.Deserialize<T>(bytes, GetEffectiveOptions());

    /// <inheritdoc/>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using System.Text.Json requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using System.Text.Json requires types to be preserved for serialization.")]
#endif
    public byte[] Serialize<T>(T item) => JsonSerializer.SerializeToUtf8Bytes(item, GetEffectiveOptions());

    private JsonSerializerOptions GetEffectiveOptions()
    {
        var options = Options ?? new JsonSerializerOptions();

        // Add custom DateTime converters - default to UTC if no ForcedDateTimeKind is specified
        // This ensures consistency with BSON cache behavior
        var targetKind = ForcedDateTimeKind ?? DateTimeKind.Utc;

        // Create a copy to avoid modifying the original options
        options = new JsonSerializerOptions(options);

        // Add custom DateTime converters that respect the target DateTimeKind and use ticks-based serialization
        options.Converters.Add(new SystemJsonDateTimeConverter(targetKind));
        options.Converters.Add(new SystemJsonNullableDateTimeConverter(targetKind));
        options.Converters.Add(new SystemJsonDateTimeOffsetConverter());
        options.Converters.Add(new SystemJsonNullableDateTimeOffsetConverter());

        return options;
    }
}
