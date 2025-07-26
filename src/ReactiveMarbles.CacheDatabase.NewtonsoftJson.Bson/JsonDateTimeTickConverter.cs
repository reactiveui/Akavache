// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// JSON converter for DateTime that preserves ticks and handles DateTimeKind appropriately.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonDateTimeTickConverter"/> class.
/// </remarks>
/// <param name="forceDateTimeKindOverride">Optional DateTime kind override.</param>
internal class JsonDateTimeTickConverter(DateTimeKind? forceDateTimeKindOverride = null) : JsonConverter
{
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
            var targetKind = forceDateTimeKindOverride ?? DateTimeKind.Utc;

            // Convert to the target kind if necessary, ensuring we always return DateTime
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
            var targetKind = forceDateTimeKindOverride ?? DateTimeKind.Utc;

            // Create DateTime from ticks with the specified kind
            var result = new DateTime(ticks, targetKind);

            return result;
        }

        return null;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is DateTime dateTime)
        {
            // Store ticks in a way that preserves the intent while allowing proper deserialization
            // Convert to UTC for consistent storage, but handle each kind appropriately
            var ticksToStore = dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime.Ticks,
                DateTimeKind.Local => dateTime.ToUniversalTime().Ticks,
                DateTimeKind.Unspecified => dateTime.Ticks, // Preserve original ticks for unspecified
                _ => dateTime.Ticks
            };

            writer.WriteValue(ticksToStore);
        }
    }
}
