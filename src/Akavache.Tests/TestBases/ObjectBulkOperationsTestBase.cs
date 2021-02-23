// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
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
    /// Base class for tests associated with object based bulk operations.
    /// </summary>
    public abstract class ObjectBulkOperationsTestBase
    {
        /// <summary>
        /// Tests to make sure that Get works with multiple key types.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task GetShouldWorkWithMultipleKeys()
        {
            using (Utility.WithEmptyDirectory(out var path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = Tuple.Create("Foo", 4);
                var keys = new[] { "Foo", "Bar", "Baz", };

                await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data).FirstAsync())).ConfigureAwait(false);

                Assert.Equal(keys.Length, (await fixture.GetAllKeys().FirstAsync()).Count());

                var allData = await fixture.GetObjects<Tuple<string, int>>(keys).FirstAsync();

                Assert.Equal(keys.Length, allData.Count);
                Assert.True(allData.All(x => x.Value.Item1 == data.Item1 && x.Value.Item2 == data.Item2));
            }
        }

        /// <summary>
        /// Tests to make sure that Get works with multiple key types.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task GetShouldInvalidateOldKeys()
        {
            using (Utility.WithEmptyDirectory(out var path))
            using (var fixture = CreateBlobCache(path))
            {
                var data = Tuple.Create("Foo", 4);
                var keys = new[] { "Foo", "Bar", "Baz", };

                await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data, DateTimeOffset.MinValue).FirstAsync())).ConfigureAwait(false);

                var allData = await fixture.GetObjects<Tuple<string, int>>(keys).FirstAsync();
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
                var data = Tuple.Create("Foo", 4);
                var keys = new[] { "Foo", "Bar", "Baz", };

                await fixture.InsertObjects(keys.ToDictionary(k => k, v => data)).FirstAsync();

                Assert.Equal(keys.Length, (await fixture.GetAllKeys().FirstAsync()).Count());

                var allData = await fixture.GetObjects<Tuple<string, int>>(keys).FirstAsync();

                Assert.Equal(keys.Length, allData.Count);
                Assert.True(allData.All(x => x.Value.Item1 == data.Item1 && x.Value.Item2 == data.Item2));
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
                var data = Tuple.Create("Foo", 4);
                var keys = new[] { "Foo", "Bar", "Baz", };

                await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data).FirstAsync())).ConfigureAwait(false);

                Assert.Equal(keys.Length, (await fixture.GetAllKeys().FirstAsync()).Count());

                await fixture.InvalidateObjects<Tuple<string, int>>(keys).FirstAsync();

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
