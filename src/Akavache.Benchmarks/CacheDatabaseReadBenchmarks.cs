using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;
using System.Reactive.Threading.Tasks;

namespace Akavache.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net90)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class CacheDatabaseReadBenchmarks
    {
        private readonly Random _randomNumberGenerator = new();
        private string _tempDirectory;
        private IDisposable _directoryCleanup;

        [Params(10, 100, 1000)]
        public int BenchmarkSize { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Initialize the serializer first
            CacheDatabase.Serializer = new SystemJsonSerializer();

            // Create temporary directory
            _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

            // Generate database synchronously to avoid deadlocks
            BlobCache = GenerateAGiantDatabaseSync(_tempDirectory);
            Keys = BlobCache.GetAllKeys().ToList().FirstAsync().GetAwaiter().GetResult();
            Size = BenchmarkSize;
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            BlobCache?.Dispose();
            _directoryCleanup?.Dispose();
        }

        public IBlobCache BlobCache { get; set; }

        public int Size { get; set; }

        public IList<string> Keys { get; set; }

        [Benchmark]
        [BenchmarkCategory("Read")]
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

        [Benchmark]
        [BenchmarkCategory("Read")]
        public async Task RandomRead()
        {
            var tasks = new List<Task>();

            for (int i = 0; i < Size; i++)
            {
                var randomKey = Keys[_randomNumberGenerator.Next(0, Keys.Count - 1)];
                tasks.Add(BlobCache.Get(randomKey).FirstAsync().ToTask());
            }

            await Task.WhenAll(tasks);
        }

        [Benchmark]
        [BenchmarkCategory("Read")]
        public async Task BulkRead()
        {
            var toFetch = Enumerable.Range(0, Size)
                .Select(_ => Keys[_randomNumberGenerator.Next(0, Keys.Count - 1)])
                .ToArray();

            await BlobCache.Get(toFetch).ToList().FirstAsync();
        }

        /// <summary>
        /// Generates a giant database synchronously for GlobalSetup.
        /// </summary>
        /// <param name="path">A path to use for generating it.</param>
        /// <returns>The blob cache.</returns>
        private SqliteBlobCache GenerateAGiantDatabaseSync(string path)
        {
            try
            {
                path ??= GetIntegrationTestRootDirectory();

                var giantDbSize = Math.Max(1000, BenchmarkSize * 10); // Ensure enough data for benchmarks
                var cache = new SqliteBlobCache(Path.Combine(path, "benchmarks-read.db"));

                var keys = cache.GetAllKeys().ToList().FirstAsync().GetAwaiter().GetResult();
                if (keys.Count >= giantDbSize)
                {
                    return cache;
                }

                cache.InvalidateAll().FirstAsync().GetAwaiter().GetResult();

                // Generate smaller chunks to avoid memory issues
                var ret = new List<string>();
                var remaining = giantDbSize;

                while (remaining > 0)
                {
                    var chunkSize = Math.Min(500, remaining); // Process in reasonable chunks
                    var toWrite = PerfHelper.GenerateRandomDatabaseContents(chunkSize);

                    cache.Insert(toWrite).FirstAsync().GetAwaiter().GetResult();

                    foreach (var k in toWrite.Keys)
                    {
                        ret.Add(k);
                    }

                    remaining -= chunkSize;

                    if (remaining % 2000 == 0 || remaining == 0)
                    {
                        Console.WriteLine($"Generated {giantDbSize - remaining}/{giantDbSize} items");
                    }
                }

                return cache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateAGiantDatabaseSync: {ex.Message}");
                throw;
            }
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
