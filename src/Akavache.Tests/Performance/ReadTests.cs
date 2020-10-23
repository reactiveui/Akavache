// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Akavache.Tests.Performance
{
    /// <summary>
    /// Performance read tests.
    /// </summary>
    public abstract class ReadTests
    {
        private readonly Random _randomNumberGenerator = new Random();

        /// <summary>
        /// Tests the performance of sequential simple reads.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public Task SequentialSimpleReads()
        {
            return GeneratePerfRangesForBlock(async (cache, size, keys) =>
            {
                var st = new Stopwatch();
                var toFetch = Enumerable.Range(0, size)
                    .Select(_ => keys[_randomNumberGenerator.Next(0, keys.Count - 1)])
                    .ToArray();

                st.Start();

                foreach (var v in toFetch)
                {
                    await cache.Get(v);
                }

                st.Stop();
                return st.ElapsedMilliseconds;
            });
        }

        /// <summary>
        /// Tests the performance of sequential bulk reads.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public Task SequentialBulkReads()
        {
            return GeneratePerfRangesForBlock(async (cache, size, keys) =>
            {
                var st = new Stopwatch();

                int count = 0;
                var toFetch = Enumerable.Range(0, size)
                    .Select(_ => keys[_randomNumberGenerator.Next(0, keys.Count - 1)])
                    .GroupBy(_ => ++count / 32)
                    .ToArray();

                st.Start();

                foreach (var group in toFetch)
                {
                    await cache.Get(group);
                }

                st.Stop();
                return st.ElapsedMilliseconds;
            });
        }

        /// <summary>
        /// Tests the performance of parallel simple reads.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public Task ParallelSimpleReads()
        {
            return GeneratePerfRangesForBlock(async (cache, size, keys) =>
            {
                var st = new Stopwatch();
                var toFetch = Enumerable.Range(0, size)
                    .Select(_ => keys[_randomNumberGenerator.Next(0, keys.Count - 1)])
                    .ToArray();

                st.Start();

                await toFetch.ToObservable(BlobCache.TaskpoolScheduler)
                    .Select(x => Observable.Defer(() => cache.Get(x)))
                    .Merge(32)
                    .ToArray();

                st.Stop();
                return st.ElapsedMilliseconds;
            });
        }

        /// <summary>
        /// Abstract method for generating the blob cache we want to test for.
        /// </summary>
        /// <param name="path">The path to the DB.</param>
        /// <returns>The created blob cache.</returns>
        protected abstract IBlobCache CreateBlobCache(string path);

        /// <summary>
        /// Generate performance block ranges for a block.
        /// </summary>
        /// <param name="block">The block to generate for.</param>
        /// <returns>A task to monitor the progress.</returns>
        private async Task GeneratePerfRangesForBlock(Func<IBlobCache, int, List<string>, Task<long>> block)
        {
            var results = new Dictionary<int, long>();
            var dbName = default(string);

            var dirPath = default(string);
            using (Utility.WithEmptyDirectory(out dirPath))
            using (var cache = await GenerateAGiantDatabase(dirPath).ConfigureAwait(false))
            {
                var keys = await cache.GetAllKeys();
                dbName = cache.GetType().Name;

                foreach (var size in PerfHelper.GetPerfRanges())
                {
                    results[size] = await block(cache, size, keys.ToList()).ConfigureAwait(false);
                }
            }

            Console.WriteLine(dbName);
            foreach (var kvp in results)
            {
                Console.WriteLine("{0}: {1}", kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Generates a giant database.
        /// </summary>
        /// <param name="path">A path to use for generating it.</param>
        /// <returns>The blob cache.</returns>
        private async Task<IBlobCache> GenerateAGiantDatabase(string path)
        {
            path ??= IntegrationTestHelper.GetIntegrationTestRootDirectory();

            var giantDbSize = PerfHelper.GetPerfRanges().Last();
            var cache = CreateBlobCache(path);

            var keys = await cache.GetAllKeys();
            if (keys.Count() == giantDbSize)
            {
                return cache;
            }

            await cache.InvalidateAll();
            await PerfHelper.GenerateDatabase(cache, giantDbSize).ConfigureAwait(false);

            return cache;
        }
    }
}
