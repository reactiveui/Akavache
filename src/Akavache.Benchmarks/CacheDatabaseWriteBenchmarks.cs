using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;

namespace Akavache.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net90)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class CacheDatabaseWriteBenchmarks
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

            // Create fresh database for each run
            BlobCache = new SqliteBlobCache(Path.Combine(_tempDirectory, "benchmarks-write-v11.db"));
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            BlobCache?.Dispose();
            _directoryCleanup?.Dispose();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Clear the cache before each iteration
            BlobCache.InvalidateAll().FirstAsync().GetAwaiter().GetResult();
        }

        public IBlobCache BlobCache { get; set; }

        [Benchmark]
        [BenchmarkCategory("Write")]
        public async Task SequentialWrite()
        {
            var dataToWrite = PerfHelper.GenerateRandomDatabaseContents(BenchmarkSize);

            foreach (var kvp in dataToWrite)
            {
                await BlobCache.Insert(kvp.Key, kvp.Value);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Write")]
        public async Task SequentialObjectWrite()
        {
            for (int i = 0; i < BenchmarkSize; i++)
            {
                var testData = new TestDataV11
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Item {i}",
                    Value = _randomNumberGenerator.Next(1, 1000),
                    Created = DateTimeOffset.Now.AddDays(-_randomNumberGenerator.Next(0, 30))
                };
                
                await BlobCache.InsertObject($"object_{i}", testData);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Write")]
        public async Task BulkWrite()
        {
            var dataToWrite = PerfHelper.GenerateRandomDatabaseContents(BenchmarkSize);
            await BlobCache.Insert(dataToWrite);
        }

        [Benchmark]
        [BenchmarkCategory("Write")]
        public async Task WriteWithExpiration()
        {
            var dataToWrite = PerfHelper.GenerateRandomDatabaseContents(BenchmarkSize);
            var expiration = DateTimeOffset.Now.AddHours(1);

            foreach (var kvp in dataToWrite)
            {
                await BlobCache.Insert(kvp.Key, kvp.Value, expiration);
            }
        }
    }

    public class TestDataV11
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public DateTimeOffset Created { get; set; }
    }
}