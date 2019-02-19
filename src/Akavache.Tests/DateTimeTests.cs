using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Akavache.Tests
{
    public class DateTimeTests
    {
        public DateTimeTests()
        {
            SQLitePCL.Batteries_V2.Init();
        }

        [Fact]
        public void JsonDateTimeContractResolver_ValidateConverter()
        {
            //Verify our converter used
            var cr = (IContractResolver)new JsonDateTimeContractResolver(null);
            var c = cr.ResolveContract(typeof(DateTime));
            Assert.True(c.Converter == JsonDateTimeTickConverter.Default);
            c = cr.ResolveContract(typeof(DateTime));
            Assert.True(c.Converter == JsonDateTimeTickConverter.Default);
            c = cr.ResolveContract(typeof(DateTime?));
            Assert.True(c.Converter == JsonDateTimeTickConverter.Default);
            c = cr.ResolveContract(typeof(DateTime?));
            Assert.True(c.Converter == JsonDateTimeTickConverter.Default);

            //Verify the other converter is used
            cr = new JsonDateTimeContractResolver(new FakeDateTimeHighPrecisionContractResolver());
            c = cr.ResolveContract(typeof(DateTime));
            Assert.True(c.Converter is FakeDateTimeHighPrecisionJsonConverter);
            c = cr.ResolveContract(typeof(DateTimeOffset));
            Assert.True(c.Converter == JsonDateTimeOffsetTickConverter.Default);
        }

        class FakeDateTimeHighPrecisionContractResolver : DefaultContractResolver
        {
            protected override JsonContract CreateContract(Type objectType)
            {
                var contract = base.CreateContract(objectType);
                if (objectType == typeof(DateTime) || objectType == typeof(DateTime?))
                    contract.Converter = new FakeDateTimeHighPrecisionJsonConverter();
                return contract;
            }
        }

        class FakeDateTimeHighPrecisionJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.Integer && reader.TokenType != JsonToken.Date)
                    return null;

                // If you need to deserialize already-serialized DateTimeOffsets, it would come in as JsonToken.Date, uncomment to handle
                // Newly serialized values will come in as JsonToken.Integer
                if (reader.TokenType == JsonToken.Date)
                    return (DateTime)reader.Value;

                var ticks = (long)reader.Value;
                return new DateTime(ticks, DateTimeKind.Utc);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value != null) {
                    var dateTime = value is DateTime dt ? dt : ((DateTime?)value).Value;
                    serializer.Serialize(writer, dateTime.ToUniversalTime().Ticks);
                }
            }
        }


        [Theory]
        [MemberData(nameof(DateTimeOffsetData))]
        public async Task GetOrFetchAsync_DateTimeOffsetShouldBeEqualEveryTime(TestObjectDateTimeOffset data)
        {
            var (firstResult, secondResult) = await PerformTimeStampGrab(data);
            Assert.Equal(firstResult.Timestamp, secondResult.Timestamp);
            Assert.Equal(firstResult.Timestamp.UtcTicks, secondResult.Timestamp.UtcTicks);
            Assert.Equal(firstResult.Timestamp.Offset, secondResult.Timestamp.Offset);
            Assert.Equal(firstResult.Timestamp.Ticks, secondResult.Timestamp.Ticks);
            Assert.Equal(firstResult.TimestampNullable, secondResult.TimestampNullable);
        }

        [Theory]
        [MemberData(nameof(DateTimeData))]
        public async Task GetOrFetchAsync_DateTimeShouldBeEqualEveryTime(TestObjectDateTime data)
        {
            var (firstResult, secondResult) = await PerformTimeStampGrab(data);
            Assert.Equal(firstResult.Timestamp, secondResult.Timestamp);
            Assert.Equal(firstResult.TimestampNullable, secondResult.TimestampNullable);
        }

        private async Task<(TData first, TData second)> PerformTimeStampGrab<TData>(TData data)
        {
            const string key = "key";

            Task<TData> fetchFunction() => Task.FromResult(data);

            //Act
            var firstResult = await BlobCache.InMemory.GetOrFetchObject(key, fetchFunction);
            var secondResult = await BlobCache.InMemory.GetOrFetchObject(key, fetchFunction);

            return (firstResult, secondResult);
        }

        public static IEnumerable<object[]> DateTimeOffsetData => new[]
        {
            new object[] { new TestObjectDateTimeOffset { Timestamp = TestNowOffset, TimestampNullable = null} },
            new object[] { new TestObjectDateTimeOffset { Timestamp = TestNowOffset, TimestampNullable = TestNowOffset } },
        };

        public static IEnumerable<object[]> DateTimeData => new[]
        {
            new object[] { new TestObjectDateTime { Timestamp = TestNow, TimestampNullable = null} },
            new object[] { new TestObjectDateTime { Timestamp = TestNow, TimestampNullable = TestNow } },
        };

        private static DateTime TestNow { get; } = DateTime.Now;
        private static DateTimeOffset TestNowOffset { get; } = DateTimeOffset.Now;

        public class TestObjectDateTimeOffset
        {
            public DateTimeOffset Timestamp { get; set; }
            public DateTimeOffset? TimestampNullable { get; set; }
        }

        public class TestObjectDateTime
        {
            public DateTime Timestamp { get; set; }

            public DateTime? TimestampNullable { get; set; }
        }
    }
}
