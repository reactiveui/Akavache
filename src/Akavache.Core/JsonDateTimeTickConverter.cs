using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Akavache
{
    /// <summary>
    /// Since we use BSON at places, we want to just store ticks to avoid loosing precision.
    /// By default BSON will use JSON ticks.
    /// </summary>
    internal class JsonDateTimeTickConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?) || objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Integer && reader.TokenType != JsonToken.Date)
            {
                return null;
            }

            DateTime dateTime;

            // If you need to deserialize already-serialized DateTimeOffsets, it would come in as JsonToken.Date
            // Newly serialized values will come in as JsonToken.Integer
            if (reader.TokenType == JsonToken.Date)
            {
                dateTime = (DateTime)reader.Value;
            }
            else
            {
                var ticks = (long)reader.Value;
                dateTime = new DateTime(ticks, DateTimeKind.Utc);

            }

            if (objectType == typeof(DateTime) || objectType == typeof(DateTime?))
            {
                return dateTime;
            }

            if (objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?))
            {
                return (DateTimeOffset)dateTime;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (value)
            {
                case DateTime dateTime:
                    serializer.Serialize(writer, dateTime.Ticks);
                    break;
                case DateTimeOffset dateTimeOffset:
                    serializer.Serialize(writer, dateTimeOffset.UtcDateTime.Ticks);
                    break;
                case null:
                    return;
            }
        }
    }
}
