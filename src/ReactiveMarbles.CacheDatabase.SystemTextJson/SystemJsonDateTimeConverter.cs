// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReactiveMarbles.CacheDatabase.SystemTextJson;

/// <summary>
/// Custom DateTime converter for System.Text.Json that preserves ticks and handles DateTimeKind appropriately.
/// This converter matches the behavior of the NewtonsoftBson serializer for consistent DateTime handling.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SystemJsonDateTimeConverter"/> class.
/// </remarks>
/// <param name="forcedKind">The forced DateTime kind.</param>
internal class SystemJsonDateTimeConverter(DateTimeKind? forcedKind = null) : JsonConverter<DateTime>
{
    private readonly DateTimeKind _targetKind = forcedKind ?? DateTimeKind.Utc;

    /// <inheritdoc/>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Handle ticks-based serialization (matching NewtonsoftBson behavior)
            var ticks = reader.GetInt64();
            return new DateTime(ticks, _targetKind);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            // Handle standard DateTime string format
            var dateTime = reader.GetDateTime();

            // Apply the forced kind - always ensure we convert properly
            return _targetKind switch
            {
                DateTimeKind.Utc => DateTime.SpecifyKind(dateTime.ToUniversalTime(), DateTimeKind.Utc),
                DateTimeKind.Local => DateTime.SpecifyKind(dateTime.ToLocalTime(), DateTimeKind.Local),
                _ => DateTime.SpecifyKind(dateTime, _targetKind)
            };
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} when reading DateTime");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Store ticks in a way that preserves the intent while allowing proper deserialization
        // Convert to UTC for consistent storage, but handle each kind appropriately
        var ticksToStore = value.Kind switch
        {
            DateTimeKind.Utc => value.Ticks,
            DateTimeKind.Local => value.ToUniversalTime().Ticks,
            DateTimeKind.Unspecified => value.Ticks, // Preserve original ticks for unspecified
            _ => value.Ticks
        };

        writer.WriteNumberValue(ticksToStore);
    }
}
