// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// JSON converter for DateTime that preserves ticks and handles DateTimeKind appropriately.
/// </summary>
internal class JsonDateTimeTickConverter : JsonConverter
{
    private readonly DateTimeKind? _forceDateTimeKindOverride;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDateTimeTickConverter"/> class.
    /// </summary>
    /// <param name="forceDateTimeKindOverride">Optional DateTime kind override.</param>
    public JsonDateTimeTickConverter(DateTimeKind? forceDateTimeKindOverride = null)
    {
        _forceDateTimeKindOverride = forceDateTimeKindOverride;
    }

    /// <summary>
    /// Gets a instance of the DateTimeConverter that handles the DateTime in UTC mode.
    /// </summary>
    public static JsonDateTimeTickConverter Default { get; } = new();

    /// <summary>
    /// Gets a instance of the DateTimeConverter that handles the DateTime in Local mode.
    /// </summary>
    public static JsonDateTimeTickConverter LocalDateTimeKindDefault { get; } = new(DateTimeKind.Local);

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(DateTime) || objectType == typeof(DateTime?);

    /// <inheritdoc/>
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
            var dateTime = (DateTime)reader.Value;

            // Apply the DateTimeKind override even for direct DateTime values
            var targetKind = _forceDateTimeKindOverride ?? DateTimeKind.Utc;

            // Convert to the target kind if necessary
            var result = targetKind switch
            {
                DateTimeKind.Utc => DateTime.SpecifyKind(dateTime.ToUniversalTime(), DateTimeKind.Utc),
                DateTimeKind.Local => DateTime.SpecifyKind(dateTime.ToLocalTime(), DateTimeKind.Local),
                _ => DateTime.SpecifyKind(dateTime, targetKind)
            };

            return result;
        }

        if ((objectType == typeof(DateTime) || objectType == typeof(DateTime?)) && reader.Value is not null)
        {
            var ticks = (long)reader.Value;
            var result = new DateTime(ticks, _forceDateTimeKindOverride ?? DateTimeKind.Utc);

            return result;
        }

        return null;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is DateTime dateTime)
        {
            // Always serialize as ticks to avoid BSON's native DateTime handling
            var ticks = _forceDateTimeKindOverride switch
            {
                DateTimeKind.Local => dateTime.Ticks,
                _ => dateTime.ToUniversalTime().Ticks
            };

            // Write as raw number to force tick-based serialization
            writer.WriteValue(ticks);
        }
    }
}
