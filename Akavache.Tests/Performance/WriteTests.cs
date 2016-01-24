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
    public abstract class WriteTests
    {
        protected abstract IBlobCache CreateBlobCache(string path);
        readonly Random prng = new Random();

        [Fact]
        public async Task SequentialSimpleWrites()
        {
            await GeneratePerfRangesForBlock(async (cache, size) => {
                var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

                var st = new Stopwatch();
                st.Start();

                foreach (var kvp in toWrite) {
                    await cache.Insert(kvp.Key, kvp.Value);
                }

                st.Stop();
                return st.ElapsedMilliseconds;
            });
        }

        [Fact]
        public async Task SequentialBulkWrites()
        {
            await GeneratePerfRangesForBlock(async (cache, size) => {
                var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

                var st = new Stopwatch();
                st.Start();

                await cache.Insert(toWrite);

                st.Stop();
                return st.ElapsedMilliseconds;
            });

        }

        [Fact]
        public async Task ParallelSimpleWrites()
        { 
            await GeneratePerfRangesForBlock(async (cache, size) => {
                var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

                var st = new Stopwatch();
                st.Start();

                await toWrite.ToObservable(BlobCache.TaskpoolScheduler)
                    .Select(x => Observable.Defer(() => cache.Insert(x.Key, x.Value)))
                    .Merge(32)
                    .ToArray();

                st.Stop();
                return st.ElapsedMilliseconds;
            });
        }

        public async Task GeneratePerfRangesForBlock(Func<IBlobCache, int, Task<long>> block)
        {
            var results = new Dictionary<int, long>();
            var dbName = default(string);

            var dirPath = default(string);
            using (Utility.WithEmptyDirectory(out dirPath))
            using (var cache = CreateBlobCache(dirPath))
            {
                dbName = dbName ?? cache.GetType().Name;

                foreach (var size in PerfHelper.GetPerfRanges())
                {
                    results[size] = await block(cache, size);
                }
            }

            Console.WriteLine(dbName);
            foreach (var kvp in results) {
                Console.WriteLine("{0}: {1}", kvp.Key, kvp.Value);
            }
        }
    }

    public abstract class Sqlite3WriteTests : WriteTests
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new SQLitePersistentBlobCache(Path.Combine(path, "blob.db"));
        }
    }
}
