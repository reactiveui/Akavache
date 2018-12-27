using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Akavache.Tests
{
    public class DateTimeTests
    {
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
