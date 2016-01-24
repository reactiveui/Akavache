using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache.Sqlite3;
using Xunit;

namespace Akavache.Tests.Performance
{
    public abstract class ReadTests
    {
        protected abstract IBlobCache CreateBlobCache(string path);
        readonly Random prng = new Random();

        [Fact]
        public async Task SequentialSimpleReads()
        {
            await GeneratePerfRangesForBlock(async (cache, size, keys) => 
            {
                var st = new Stopwatch();
                var toFetch = Enumerable.Range(0, size)
                    .Select(_ => keys[prng.Next(0, keys.Count - 1)])
                    .ToArray();

                st.Start();

                foreach (var v in toFetch) {
                    await cache.Get(v);
                }

                st.Stop();
                return st.ElapsedMilliseconds;
            });
        }

        [Fact]
        public async Task SequentialBulkReads()
        {
            await GeneratePerfRangesForBlock(async (cache, size, keys) => 
            {
                var st = new Stopwatch();

                int count = 0;
                var toFetch = Enumerable.Range(0, size)
                    .Select(_ => keys[prng.Next(0, keys.Count - 1)])
                    .GroupBy(_ => ++count / 32)
                    .ToArray();

                st.Start();

                foreach (var group in toFetch) {
                    await cache.Get(group);
                }
                                
                st.Stop();
                return st.ElapsedMilliseconds;
            });
        }

        [Fact]
        public async Task ParallelSimpleReads()
        {
            await GeneratePerfRangesForBlock(async (cache, size, keys) => 
            {
                var st = new Stopwatch();
                var toFetch = Enumerable.Range(0, size)
                    .Select(_ => keys[prng.Next(0, keys.Count - 1)])
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

        public async Task GeneratePerfRangesForBlock(Func<IBlobCache, int, List<string>, Task<long>> block)
        {
            var results = new Dictionary<int, long>();
            var dbName = default(string);

            var dirPath = default(string);
            using (Utility.WithEmptyDirectory(out dirPath))
            using (var cache = await GenerateAGiantDatabase(dirPath))
            {
                var keys = await cache.GetAllKeys();
                dbName = dbName ?? cache.GetType().Name;

                foreach (var size in PerfHelper.GetPerfRanges())
                {
                    results[size] = await block(cache, size, keys.ToList());
                }
            }

            Console.WriteLine(dbName);
            foreach (var kvp in results) {
                Console.WriteLine("{0}: {1}", kvp.Key, kvp.Value);
            }
        }

        async Task<IBlobCache> GenerateAGiantDatabase(string path)
        {
            path = path ?? IntegrationTestHelper.GetIntegrationTestRootDirectory();

            var giantDbSize = PerfHelper.GetPerfRanges().Last();
            var cache = CreateBlobCache(path);

            var keys = await cache.GetAllKeys();
            if (keys.Count() == giantDbSize) return cache;;

            await cache.InvalidateAll();
            await PerfHelper.GenerateDatabase(cache, giantDbSize);

            return cache;
        }


    }

    public abstract class Sqlite3ReadTests : ReadTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new SQLitePersistentBlobCache(Path.Combine(path, "blob.db"));
        }
    }
}
