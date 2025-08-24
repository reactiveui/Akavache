using System;
using System.Collections.Generic;
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
using Splat.Builder;

namespace Akavache.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net90)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class CacheDatabaseComprehensiveBenchmarks
    {
        private readonly Random _randomNumberGenerator = new();
        private string _tempDirectory;
        private IDisposable _directoryCleanup;
        private List<TestDataV11> _testObjects;
        private AppBuilder _appBuilder = AppBuilder.CreateSplatBuilder();

        [Params(10, 100, 1000)]
        public int BenchmarkSize { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Initialize with builder pattern
            _appBuilder.WithAkavache<SystemJsonSerializer>(
                "AkavacheBenchmarksV11Comprehensive",
                builder =>
                builder.WithSqliteDefaults(),
                instance =>
                    {
                        // Register the BlobCache as the default IBlobCache

                        // Create temporary directory
                        _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

                        // Create database
                        BlobCache = new SqliteBlobCache(Path.Combine(_tempDirectory, "benchmarks-comprehensive-v11.db"), instance.Serializer);

                        // Pre-generate test objects
                        _testObjects = new List<TestDataV11>();
                        for (int i = 0; i < Math.Max(BenchmarkSize, 1000); i++)
                        {
                            _testObjects.Add(new TestDataV11
                            {
                                Id = Guid.NewGuid(),
                                Name = $"Test Object {i}",
                                Value = _randomNumberGenerator.Next(1, 10000),
                                Created = DateTimeOffset.Now.AddDays(-_randomNumberGenerator.Next(0, 365))
                            });
                        }
                    });
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            BlobCache?.Dispose();
            _directoryCleanup?.Dispose();
            CacheDatabase.Shutdown().FirstAsync().GetAwaiter().GetResult();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Clear the cache before each iteration
            BlobCache.InvalidateAll().FirstAsync().GetAwaiter().GetResult();
        }

        public IBlobCache BlobCache { get; set; }

        [Benchmark]
        [BenchmarkCategory("GetOrFetch")]
        public async Task GetOrFetchObject()
        {
            for (int i = 0; i < BenchmarkSize; i++)
            {
                var key = $"get_or_fetch_{i}";
                var testData = _testObjects[i % _testObjects.Count];

                await BlobCache.GetOrFetchObject(key, () =>
                    Observable.Return(testData));
            }
        }

        [Benchmark]
        [BenchmarkCategory("GetAndFetch")]
        public async Task GetAndFetchLatest()
        {
            // Pre-populate some data
            for (int i = 0; i < Math.Min(BenchmarkSize, 100); i++)
            {
                var key = $"get_and_fetch_{i}";
                var testData = _testObjects[i % _testObjects.Count];
                await BlobCache.InsertObject(key, testData);
            }

            var tasks = new List<Task>();
            for (int i = 0; i < Math.Min(BenchmarkSize, 100); i++)
            {
                var key = $"get_and_fetch_{i}";
                var testData = _testObjects[i % _testObjects.Count];

                var task = BlobCache.GetAndFetchLatest(key, () =>
                    Observable.Return(testData))
                    .Take(1) // Just take the first result to avoid infinite waiting
                    .FirstAsync()
                    .ToTask();

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        [Benchmark]
        [BenchmarkCategory("Invalidate")]
        public async Task InvalidateObjects()
        {
            // Pre-populate data
            var keys = new List<string>();
            for (int i = 0; i < BenchmarkSize; i++)
            {
                var key = $"invalidate_test_{i}";
                var testData = _testObjects[i % _testObjects.Count];
                await BlobCache.InsertObject(key, testData);
                keys.Add(key);
            }

            // Now invalidate them
            foreach (var key in keys)
            {
                await BlobCache.InvalidateObject<TestDataV11>(key);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Expiration")]
        public async Task InsertWithExpiration()
        {
            var expiration = DateTimeOffset.Now.AddMinutes(30);

            for (int i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects[i % _testObjects.Count];
                await BlobCache.InsertObject($"expiration_test_{i}", testData, expiration);
            }
        }

        [Benchmark]
        [BenchmarkCategory("UserAccount")]
        public async Task UserAccountOperations()
        {
            var userCache = CacheDatabase.UserAccount;

            for (int i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects[i % _testObjects.Count];
                var key = $"user_data_{i}";

                await userCache.InsertObject(key, testData);
                var retrieved = await userCache.GetObject<TestDataV11>(key);

                // Verify data integrity
                if (retrieved.Id != testData.Id)
                {
                    throw new InvalidOperationException("Data integrity check failed");
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory("LocalMachine")]
        public async Task LocalMachineOperations()
        {
            var localCache = CacheDatabase.LocalMachine;

            for (int i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects[i % _testObjects.Count];
                var key = $"local_data_{i}";

                await localCache.InsertObject(key, testData);
                var retrieved = await localCache.GetObject<TestDataV11>(key);

                // Verify data integrity
                if (retrieved.Id != testData.Id)
                {
                    throw new InvalidOperationException("Data integrity check failed");
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory("Secure")]
        public async Task SecureOperations()
        {
            var secureCache = CacheDatabase.Secure;

            for (int i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects[i % _testObjects.Count];
                var key = $"secure_data_{i}";

                await secureCache.InsertObject(key, testData);
                var retrieved = await secureCache.GetObject<TestDataV11>(key);

                // Verify data integrity
                if (retrieved.Id != testData.Id)
                {
                    throw new InvalidOperationException("Data integrity check failed");
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory("InMemory")]
        public async Task InMemoryOperations()
        {
            var memoryCache = CacheDatabase.InMemory;

            for (int i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects[i % _testObjects.Count];
                var key = $"memory_data_{i}";

                await memoryCache.InsertObject(key, testData);
                var retrieved = await memoryCache.GetObject<TestDataV11>(key);

                // Verify data integrity
                if (retrieved.Id != testData.Id)
                {
                    throw new InvalidOperationException("Data integrity check failed");
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory("Mixed")]
        public async Task MixedOperations()
        {
            var caches = new IBlobCache[]
            {
                CacheDatabase.UserAccount,
                CacheDatabase.LocalMachine,
                CacheDatabase.InMemory
            };

            for (int i = 0; i < BenchmarkSize; i++)
            {
                var cache = caches[i % caches.Length];
                var testData = _testObjects[i % _testObjects.Count];
                var key = $"mixed_data_{i}";

                // Insert
                await cache.InsertObject(key, testData);

                // Read
                var retrieved = await cache.GetObject<TestDataV11>(key);

                // Update
                retrieved.Value += 1;
                await cache.InsertObject(key, retrieved);

                // Read again
                var updated = await cache.GetObject<TestDataV11>(key);

                // Verify update
                if (updated.Value != testData.Value + 1)
                {
                    throw new InvalidOperationException("Update verification failed");
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory("Serializer")]
        public async Task SerializerPerformance()
        {
            for (int i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects[i % _testObjects.Count];
                var key = $"serializer_test_{i}";

                // Test the serializer performance by inserting and retrieving complex objects
                await BlobCache.InsertObject(key, testData);
                var retrieved = await BlobCache.GetObject<TestDataV11>(key);

                // Verify serialization worked correctly
                if (retrieved.Id != testData.Id || retrieved.Name != testData.Name)
                {
                    throw new InvalidOperationException("Serialization integrity check failed");
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory("BulkOperations")]
        public async Task BulkOperations()
        {
            var keyValuePairs = new Dictionary<string, TestDataV11>();
            for (int i = 0; i < BenchmarkSize; i++)
            {
                keyValuePairs[$"bulk_test_{i}"] = _testObjects[i % _testObjects.Count];
            }

            // Bulk insert
            await BlobCache.InsertObjects(keyValuePairs);

            // Bulk get
            var keys = keyValuePairs.Keys.ToArray();
            var retrieved = await BlobCache.GetObjects<TestDataV11>(keys).ToList();

            // Verify bulk operations
            if (retrieved.Count != BenchmarkSize)
            {
                throw new InvalidOperationException("Bulk operation integrity check failed");
            }
        }
    }
}