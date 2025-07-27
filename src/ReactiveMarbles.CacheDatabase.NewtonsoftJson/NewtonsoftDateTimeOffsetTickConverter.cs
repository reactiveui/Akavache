// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson;

/// <summary>
/// JSON converter for DateTimeOffset that preserves ticks and offset appropriately.
/// This converter matches the behavior of the NewtonsoftBson serializer for consistent DateTimeOffset handling.
/// </summary>
internal class NewtonsoftDateTimeOffsetTickConverter : JsonConverter
{
    /// <summary>
    /// Gets the default instance of the DateTimeOffsetConverter.
    /// </summary>
    public static NewtonsoftDateTimeOffsetTickConverter Default { get; } = new();

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);

    /// <inheritdoc/>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (reader.TokenType is not JsonToken.StartObject and not JsonToken.Date and not JsonToken.Integer)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.Date && reader.Value is not null)
        {
            return (DateTimeOffset)(DateTime)reader.Value;
        }

        // Handle the case where we stored it as an object with ticks and offset
        if (reader.TokenType == JsonToken.StartObject)
        {
            long ticks = 0;
            long offsetTicks = 0;

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value?.ToString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "Ticks":
                            ticks = (long)reader.Value!;
                            break;
                        case "OffsetTicks":
                            offsetTicks = (long)reader.Value!;
                            break;
                    }
                }
            }

            var offset = new TimeSpan(offsetTicks);
            return new DateTimeOffset(ticks, offset);
        }

        // Fallback for legacy integer-only format (assume UTC)
        if ((objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?)) && reader.Value is not null)
        {
            var ticks = (long)reader.Value;
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        return null;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            // Store both ticks and offset to preserve full DateTimeOffset information
            writer.WriteStartObject();
            writer.WritePropertyName("Ticks");
            writer.WriteValue(dateTimeOffset.Ticks);
            writer.WritePropertyName("OffsetTicks");
            writer.WriteValue(dateTimeOffset.Offset.Ticks);
            writer.WriteEndObject();
        }
    }
}
