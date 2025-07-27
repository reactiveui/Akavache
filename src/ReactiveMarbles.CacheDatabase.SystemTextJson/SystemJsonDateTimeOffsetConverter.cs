// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReactiveMarbles.CacheDatabase.SystemTextJson;

/// <summary>
/// Custom DateTimeOffset converter for System.Text.Json that preserves ticks and offset appropriately.
/// This converter matches the behavior of the NewtonsoftBson serializer for consistent DateTimeOffset handling.
/// </summary>
internal class SystemJsonDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    /// <inheritdoc/>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Handle the case where we stored it as an object with ticks and offset
            long ticks = 0;
            long offsetTicks = 0;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "Ticks":
                            ticks = reader.GetInt64();
                            break;
                        case "OffsetTicks":
                            offsetTicks = reader.GetInt64();
                            break;
                    }
                }
            }

            var offset = new TimeSpan(offsetTicks);
            return new DateTimeOffset(ticks, offset);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            // Handle standard DateTimeOffset string format
            return reader.GetDateTimeOffset();
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            // Fallback for legacy integer-only format (assume UTC)
            var ticks = reader.GetInt64();
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} when reading DateTimeOffset");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // Store both ticks and offset to preserve full DateTimeOffset information
        writer.WriteStartObject();
        writer.WritePropertyName("Ticks");
        writer.WriteNumberValue(value.Ticks);
        writer.WritePropertyName("OffsetTicks");
        writer.WriteNumberValue(value.Offset.Ticks);
        writer.WriteEndObject();
    }
}
