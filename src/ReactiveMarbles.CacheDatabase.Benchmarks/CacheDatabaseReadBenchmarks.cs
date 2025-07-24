using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

using Iced.Intel;

using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net50)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class CacheDatabaseReadBenchmarks
    {
        private readonly Random _randomNumberGenerator = new Random();

        [GlobalSetup]
        public void GlobalSetup()
        {
            Utility.WithEmptyDirectory(out var item);

            BlobCache = GenerateAGiantDatabase(item).Result;
            Keys = BlobCache.GetAllKeys().ToList().GetAwaiter().GetResult();
            Size = PerfHelper.GetPerfRanges().Last();
        }

        public IBlobCache BlobCache { get; set; }

        public int Size {  get; set; }

        public IList<string> Keys {  get; set; }

        [Benchmark]
        public async Task SequentialRead()
        {
            var toFetch = Enumerable.Range(0, Size)
                .Select(_ => Keys[_randomNumberGenerator.Next(0, Keys.Count - 1)])
                .ToArray();

            foreach (var v in toFetch)
            {
                await BlobCache.Get(v);
            }
        }

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
                var keys = await cache.GetAllKeys().ToList();
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
            path ??= GetIntegrationTestRootDirectory();

            var giantDbSize = PerfHelper.GetPerfRanges().Last();
            var cache = new Sqlite3.SqliteBlobCache(Path.Combine(path, "benchmarks-read.db"));

            var keys = await cache.GetAllKeys();
            if (keys.Count() == giantDbSize)
            {
                return cache;
            }

            await cache.InvalidateAll();
            await PerfHelper.GenerateDatabase(cache, giantDbSize).ConfigureAwait(false);

            return cache;
        }

        /// <summary>
        /// Gets the root folder for the integration tests.
        /// </summary>
        /// <returns>The root folder.</returns>
        public static string GetIntegrationTestRootDirectory()
        {
            // XXX: This is an evil hack, but it's okay for a unit test
            // We can't use Assembly.Location because unit test runners love
            // to move stuff to temp directories
            var st = new StackFrame(true);
            var di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(st.GetFileName())));

            return di.FullName;
        }
    }
}
