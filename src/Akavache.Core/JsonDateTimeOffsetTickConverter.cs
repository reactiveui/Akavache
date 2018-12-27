using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Akavache
{
    internal class JsonDateTimeOffsetTickConverter : JsonConverter
    {
        public static JsonDateTimeOffsetTickConverter Default { get; } = new JsonDateTimeOffsetTickConverter();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DateTimeOffset dateTimeOffset)
            {
                serializer.Serialize(writer, new DateTimeOffsetData(dateTimeOffset));
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Date)
            {
                return (DateTimeOffset)reader.Value;
            }
            
            var data = serializer.Deserialize<DateTimeOffsetData>(reader);

            if (data == null)
            {
                return null;
            }

            return (DateTimeOffset)data;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);
        }

        internal class DateTimeOffsetData
        {
            public DateTimeOffsetData(DateTimeOffset offset)
            {
                Ticks = offset.Ticks;
                OffsetTicks = offset.Offset.Ticks;
            }

            public long Ticks { get; set; }

            public long OffsetTicks { get; set; }

            public static explicit operator DateTimeOffset(DateTimeOffsetData value)  // explicit byte to digit conversion operator
            {
                return new DateTimeOffset(value.Ticks, new TimeSpan(value.OffsetTicks));
            }
        }
    }
}
