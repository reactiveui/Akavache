// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Akavache;

/// <summary>
/// Since we use BSON at places, we want to just store ticks to avoid loosing precision.
/// By default BSON will use JSON ticks.
/// </summary>
public class JsonDateTimeTickConverter(DateTimeKind? forceDateTimeKindOverride = null) : JsonConverter
{
    private readonly DateTimeKind? _forceDateTimeKindOverride = forceDateTimeKindOverride;

    /// <summary>
    /// Gets a instance of the DateTimeConverter that handles the DateTime in UTC mode.
    /// </summary>
    public static JsonDateTimeTickConverter Default { get; } = new();

    /// <summary>
    /// Gets a instance of the DateTimeConverter that handles the DateTime in Local mode.
    /// </summary>
    public static JsonDateTimeTickConverter LocalDateTimeKindDefault { get; } = new(DateTimeKind.Local);

    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <param name="objectType">Type of the object.</param>
    /// <returns>
    /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type objectType) => objectType == typeof(DateTime) || objectType == typeof(DateTime?);

    /// <summary>
    /// Reads the json.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing value.</param>
    /// <param name="serializer">The serializer.</param>
    /// <returns>An object.</returns>
    /// <exception cref="ArgumentNullException">nameof(reader).</exception>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (reader.TokenType is not JsonToken.Integer and not JsonToken.Date)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.Date && reader.Value is not null)
        {
            return (DateTime)reader.Value;
        }

        if ((objectType == typeof(DateTime) || objectType == typeof(DateTime?)) && reader.Value is not null)
        {
            var ticks = (long)reader.Value;

            return new DateTime(ticks, _forceDateTimeKindOverride ?? DateTimeKind.Utc);
        }

        return null;
    }

    /// <summary>
    /// Writes the json.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The serializer.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (serializer == null)
        {
            throw new ArgumentNullException(nameof(serializer));
        }

        if (value is DateTime dateTime)
        {
            switch (_forceDateTimeKindOverride)
            {
                case DateTimeKind.Local:
                    serializer.Serialize(writer, dateTime.Ticks);
                    break;
                default:
                    serializer.Serialize(writer, dateTime.ToUniversalTime().Ticks);
                    break;
            }
        }
    }
}
