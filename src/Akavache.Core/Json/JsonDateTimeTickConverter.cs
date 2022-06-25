// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Akavache;

/// <summary>
/// Since we use BSON at places, we want to just store ticks to avoid loosing precision.
/// By default BSON will use JSON ticks.
/// </summary>
internal class JsonDateTimeTickConverter : JsonConverter
{
    private readonly DateTimeKind? _forceDateTimeKindOverride;

    public JsonDateTimeTickConverter(DateTimeKind? forceDateTimeKindOverride = null) => _forceDateTimeKindOverride = forceDateTimeKindOverride;

    /// <summary>
    /// Gets a instance of the DateTimeConverter that handles the DateTime in UTC mode.
    /// </summary>
    public static JsonDateTimeTickConverter Default { get; } = new();

    /// <summary>
    /// Gets a instance of the DateTimeConverter that handles the DateTime in Local mode.
    /// </summary>
    public static JsonDateTimeTickConverter LocalDateTimeKindDefault { get; } = new(DateTimeKind.Local);

    public override bool CanConvert(Type objectType) => objectType == typeof(DateTime) || objectType == typeof(DateTime?);

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

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
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