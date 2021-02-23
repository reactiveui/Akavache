// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Akavache.Tests
{
    /// <summary>
    /// Tests associated with the DateTime and DateTimeOffset.
    /// </summary>
    public abstract class DateTimeTestBase
    {
        /// <summary>
        /// Gets the date time offsets used in theory tests.
        /// </summary>
        public static IEnumerable<object[]> DateTimeOffsetData => new[]
        {
            new object[] { new TestObjectDateTimeOffset { Timestamp = TestNowOffset, TimestampNullable = null } },
            new object[] { new TestObjectDateTimeOffset { Timestamp = TestNowOffset, TimestampNullable = TestNowOffset } },
        };

        /// <summary>
        /// Gets the DateTime used in theory tests.
        /// </summary>
        public static IEnumerable<object[]> DateTimeData => new[]
        {
            new object[] { new TestObjectDateTime { Timestamp = TestNow, TimestampNullable = null } },
            new object[] { new TestObjectDateTime { Timestamp = TestNow, TimestampNullable = TestNow } },
        };

        /// <summary>
        /// Gets the DateTime used in theory tests.
        /// </summary>
        public static IEnumerable<object[]> DateLocalTimeData => new[]
        {
            new object[] { new TestObjectDateTime { Timestamp = LocalTestNow, TimestampNullable = null } },
            new object[] { new TestObjectDateTime { Timestamp = LocalTestNow, TimestampNullable = LocalTestNow } },
        };

        /// <summary>
        /// Gets the date time when the tests are done to keep them consistent.
        /// </summary>
        private static DateTime TestNow { get; } = DateTime.Now;

        /// <summary>
        /// Gets the date time when the tests are done to keep them consistent.
        /// </summary>
        private static DateTime LocalTestNow { get; } = TimeZoneInfo.ConvertTimeFromUtc(TestNow.ToUniversalTime(), TimeZoneInfo.CreateCustomTimeZone("testTimeZone", TimeSpan.FromHours(6), "Test Time Zone", "Test Time Zone"));

        /// <summary>
        /// Gets the date time off set when the tests are done to keep them consistent.
        /// </summary>
        private static DateTimeOffset TestNowOffset { get; } = DateTimeOffset.Now;

        /// <summary>
        /// Makes sure that the DateTimeOffset are serialized correctly.
        /// </summary>
        /// <param name="data">The data in the theory.</param>
        /// <returns>A task to monitor the progress.</returns>
        [Theory]
        [MemberData(nameof(DateTimeOffsetData))]
        public async Task GetOrFetchAsyncDateTimeOffsetShouldBeEqualEveryTime(TestObjectDateTimeOffset data)
        {
            using (Utility.WithEmptyDirectory(out var path))
            using (var blobCache = CreateBlobCache(path))
            {
                var (firstResult, secondResult) = await PerformTimeStampGrab(blobCache, data).ConfigureAwait(false);
                Assert.Equal(firstResult.Timestamp, secondResult.Timestamp);
                Assert.Equal(firstResult.Timestamp.UtcTicks, secondResult.Timestamp.UtcTicks);
                Assert.Equal(firstResult.Timestamp.Offset, secondResult.Timestamp.Offset);
                Assert.Equal(firstResult.Timestamp.Ticks, secondResult.Timestamp.Ticks);
                Assert.Equal(firstResult.TimestampNullable, secondResult.TimestampNullable);
            }
        }

        /// <summary>
        /// Makes sure that the DateTime are serialized correctly.
        /// </summary>
        /// <param name="data">The data in the theory.</param>
        /// <returns>A task to monitor the progress.</returns>
        [Theory]
        [MemberData(nameof(DateTimeData))]
        public async Task GetOrFetchAsyncDateTimeShouldBeEqualEveryTime(TestObjectDateTime data)
        {
            using (Utility.WithEmptyDirectory(out var path))
            using (var blobCache = CreateBlobCache(path))
            {
                var (firstResult, secondResult) = await PerformTimeStampGrab(blobCache, data).ConfigureAwait(false);
                Assert.Equal(secondResult.Timestamp.Kind, DateTimeKind.Utc);
                Assert.Equal(firstResult.Timestamp.ToUniversalTime(), secondResult.Timestamp.ToUniversalTime());
                Assert.Equal(firstResult.TimestampNullable?.ToUniversalTime(), secondResult.TimestampNullable?.ToUniversalTime());
            }
        }

        /// <summary>
        /// Makes sure that the DateTime are serialized correctly.
        /// </summary>
        /// <param name="data">The data in the theory.</param>
        /// <returns>A task to monitor the progress.</returns>
        [Theory]
        [MemberData(nameof(DateLocalTimeData))]
        public async Task GetOrFetchAsyncDateTimeWithForcedLocal(TestObjectDateTime data)
        {
            using (Utility.WithEmptyDirectory(out var path))
            using (var blobCache = CreateBlobCache(path))
            {
                blobCache.ForcedDateTimeKind = DateTimeKind.Local;
                var (firstResult, secondResult) = await PerformTimeStampGrab(blobCache, data).ConfigureAwait(false);
                Assert.Equal(secondResult.Timestamp.Kind, DateTimeKind.Local);
                Assert.Equal(firstResult.Timestamp, secondResult.Timestamp);
                Assert.Equal(firstResult.Timestamp.ToUniversalTime(), secondResult.Timestamp.ToUniversalTime());
                Assert.Equal(firstResult.TimestampNullable?.ToUniversalTime(), secondResult.TimestampNullable?.ToUniversalTime());
                BlobCache.ForcedDateTimeKind = null;
            }
        }

        /// <summary>
        /// Tests to make sure that we can force the DateTime kind.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task DateTimeKindCanBeForced()
        {
            using (Utility.WithEmptyDirectory(out var path))
            using (var fixture = CreateBlobCache(path))
            {
                fixture.ForcedDateTimeKind = DateTimeKind.Utc;

                var value = DateTime.UtcNow;
                await fixture.InsertObject("key", value).FirstAsync();
                var result = await fixture.GetObject<DateTime>("key").FirstAsync();
                Assert.Equal(DateTimeKind.Utc, result.Kind);
            }
        }

        /// <summary>
        /// Gets the <see cref="IBlobCache"/> we want to do the tests against.
        /// </summary>
        /// <param name="path">The path to the blob cache.</param>
        /// <returns>The blob cache for testing.</returns>
        protected abstract IBlobCache CreateBlobCache(string path);

        /// <summary>
        /// Performs the actual time stamp grab.
        /// </summary>
        /// <typeparam name="TData">The type of data we are grabbing.</typeparam>
        /// <param name="blobCache">The blob cache to perform the operation against.</param>
        /// <param name="data">The data to grab.</param>
        /// <returns>A task with the data found.</returns>
        private async Task<(TData First, TData Second)> PerformTimeStampGrab<TData>(IBlobCache blobCache, TData data)
        {
            const string key = "key";

            Task<TData> FetchFunction() => Task.FromResult(data);

            var firstResult = await blobCache.GetOrFetchObject(key, FetchFunction);
            var secondResult = await blobCache.GetOrFetchObject(key, FetchFunction);

            return (firstResult, secondResult);
        }
    }
}
