// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Akavache;

/// <summary>
/// Json Date Time Offset Tick Converter.
/// </summary>
/// <seealso cref="Newtonsoft.Json.JsonConverter" />
public class JsonDateTimeOffsetTickConverter : JsonConverter
{
    /// <summary>
    /// Gets the default.
    /// </summary>
    /// <value>
    /// The default.
    /// </value>
    public static JsonDateTimeOffsetTickConverter Default { get; } = new();

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (serializer == null)
        {
            throw new ArgumentNullException(nameof(serializer));
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            serializer.Serialize(writer, new DateTimeOffsetData(dateTimeOffset));
        }
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>
    /// The object value.
    /// </returns>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (serializer == null)
        {
            throw new ArgumentNullException(nameof(serializer));
        }

        if (reader.TokenType == JsonToken.Date && reader.Value is not null)
        {
            return (DateTimeOffset)reader.Value;
        }

        var data = serializer.Deserialize<DateTimeOffsetData>(reader);

        return data is null ? null : (DateTimeOffset)data;
    }

    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <param name="objectType">Type of the object.</param>
    /// <returns>
    /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type objectType) => objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);

    internal class DateTimeOffsetData(DateTimeOffset offset)
    {
        public long Ticks { get; set; } = offset.Ticks;

        public long OffsetTicks { get; set; } = offset.Offset.Ticks;

        public static explicit operator DateTimeOffset(DateTimeOffsetData value) // explicit byte to digit conversion operator
            =>
                new(value.Ticks, new(value.OffsetTicks));
    }
}
