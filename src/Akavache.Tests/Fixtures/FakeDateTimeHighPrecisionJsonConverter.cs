// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Akavache.Tests
{
    /// <summary>
    /// A fake converter for the DateTime and high precision.
    /// </summary>
    public class FakeDateTimeHighPrecisionJsonConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Integer && reader.TokenType != JsonToken.Date)
            {
                return null;
            }

            // If you need to deserialize already-serialized DateTimeOffsets, it would come in as JsonToken.Date, uncomment to handle
            // Newly serialized values will come in as JsonToken.Integer
            if (reader.TokenType == JsonToken.Date)
            {
                return (DateTime)reader.Value;
            }

            var ticks = (long)reader.Value;
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is not null)
            {
                var dateTime = value is DateTime dt ? dt : ((DateTime?)value).Value;
                serializer.Serialize(writer, dateTime.ToUniversalTime().Ticks);
            }
        }
    }
}
