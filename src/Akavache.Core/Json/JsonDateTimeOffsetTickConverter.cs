// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Akavache;

internal class JsonDateTimeOffsetTickConverter : JsonConverter
{
    public static JsonDateTimeOffsetTickConverter Default { get; } = new();

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            serializer.Serialize(writer, new DateTimeOffsetData(dateTimeOffset));
        }
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Date && reader.Value is not null)
        {
            return (DateTimeOffset)reader.Value;
        }

        var data = serializer.Deserialize<DateTimeOffsetData>(reader);

        return data is null ? null : (DateTimeOffset)data;
    }

    public override bool CanConvert(Type objectType) => objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);

    internal class DateTimeOffsetData
    {
        public DateTimeOffsetData(DateTimeOffset offset)
        {
            Ticks = offset.Ticks;
            OffsetTicks = offset.Offset.Ticks;
        }

        public long Ticks { get; set; }

        public long OffsetTicks { get; set; }

        public static explicit operator DateTimeOffset(DateTimeOffsetData value) // explicit byte to digit conversion operator
            =>
                new(value.Ticks, new(value.OffsetTicks));
    }
}
