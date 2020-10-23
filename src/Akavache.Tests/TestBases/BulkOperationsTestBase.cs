// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Akavache.Tests
{
    /// <summary>
    /// A base class for tests about bulk operations.
    /// </summary>
    public abstract class BulkOperationsTestBase
    {
        /// <summary>
        /// Tests if Get with multiple keys work correctly.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task GetShouldWorkWithMultipleKeys()
        {
            using (Utility.WithEmptyDirectory(out var path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = new byte[] { 0x10, 0x20, 0x30, };
                var keys = new[] { "Foo", "Bar", "Baz", };

                await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data).FirstAsync())).ConfigureAwait(false);

                Assert.Equal(keys.Length, (await fixture.GetAllKeys().FirstAsync()).Count());

                var allData = await fixture.Get(keys).FirstAsync();

                Assert.Equal(keys.Length, allData.Count);
                Assert.True(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]));
            }
        }

        /// <summary>
        /// Tests to make sure that Get invalidates all the old keys.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task GetShouldInvalidateOldKeys()
        {
            using (Utility.WithEmptyDirectory(out var path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = new byte[] { 0x10, 0x20, 0x30, };
                var keys = new[] { "Foo", "Bar", "Baz", };

                await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data, DateTimeOffset.MinValue).FirstAsync())).ConfigureAwait(false);

                var allData = await fixture.Get(keys).FirstAsync();
                Assert.Equal(0, allData.Count);
                Assert.Equal(0, (await fixture.GetAllKeys().FirstAsync()).Count());
            }
        }

        /// <summary>
        /// Tests to make sure that insert works with multiple keys.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task InsertShouldWorkWithMultipleKeys()
        {
            using (Utility.WithEmptyDirectory(out var path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = new byte[] { 0x10, 0x20, 0x30, };
                var keys = new[] { "Foo", "Bar", "Baz", };

                await fixture.Insert(keys.ToDictionary(k => k, v => data)).FirstAsync();

                Assert.Equal(keys.Length, (await fixture.GetAllKeys().FirstAsync()).Count());

                var allData = await fixture.Get(keys).FirstAsync();

                Assert.Equal(keys.Length, allData.Count);
                Assert.True(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]));
            }
        }

        /// <summary>
        /// Invalidate should be able to trash multiple keys.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task InvalidateShouldTrashMultipleKeys()
        {
            using (Utility.WithEmptyDirectory(out var path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = new byte[] { 0x10, 0x20, 0x30, };
                var keys = new[] { "Foo", "Bar", "Baz", };

                await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data).FirstAsync())).ConfigureAwait(false);

                Assert.Equal(keys.Length, (await fixture.GetAllKeys().FirstAsync()).Count());

                await fixture.Invalidate(keys).FirstAsync();

                Assert.Equal(0, (await fixture.GetAllKeys().FirstAsync()).Count());
            }
        }

        /// <summary>
        /// Gets the <see cref="IBlobCache"/> we want to do the tests against.
        /// </summary>
        /// <param name="path">The path to the blob cache.</param>
        /// <returns>The blob cache for testing.</returns>
        protected abstract IBlobCache CreateBlobCache(string path);
    }
}
