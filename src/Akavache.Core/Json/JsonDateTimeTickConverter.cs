// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Akavache
{
    /// <summary>
    /// Since we use BSON at places, we want to just store ticks to avoid loosing precision.
    /// By default BSON will use JSON ticks.
    /// </summary>
    internal class JsonDateTimeTickConverter : JsonConverter
    {
        public static JsonDateTimeTickConverter Default { get; } = new JsonDateTimeTickConverter();

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Integer && reader.TokenType != JsonToken.Date)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.Date)
            {
                return (DateTime)reader.Value;
            }

            if (objectType == typeof(DateTime) || objectType == typeof(DateTime?))
            {
                var ticks = (long)reader.Value;
                var dateTime = new DateTime(ticks, DateTimeKind.Utc);
                return dateTime;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DateTime dateTime)
            {
                serializer.Serialize(writer, dateTime.ToUniversalTime().Ticks);
            }
        }
    }
}
