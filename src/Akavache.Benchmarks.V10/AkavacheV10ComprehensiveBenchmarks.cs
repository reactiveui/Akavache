// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using Akavache;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Splat;
using SQLite;

namespace Akavache.Benchmarks.V10
{
    /// <summary>
    /// AkavacheV10ComprehensiveBenchmarks.
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net90)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class AkavacheV10ComprehensiveBenchmarks
    {
        private readonly Random _randomNumberGenerator = new();
        private string? _tempDirectory;
        private IDisposable? _directoryCleanup;
        private List<TestData>? _testObjects;

        /// <summary>
        /// Gets or sets the size of the benchmark.
        /// </summary>
        /// <value>
        /// The size of the benchmark.
        /// </value>
        [Params(10, 100, 1000)]
        public int BenchmarkSize { get; set; }

        /// <summary>
        /// Gets or sets the bench BLOB cache.
        /// </summary>
        /// <value>
        /// The bench BLOB cache.
        /// </value>
        public IBlobCache? BenchBlobCache { get; set; }

        /// <summary>
        /// Globals the setup.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            // Initialize Akavache V10 style
            BlobCache.ApplicationName = "AkavacheBenchmarksV10Comprehensive";

            // Create temporary directory
            _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

            // Create database
            BenchBlobCache = new Sqlite3.SqlRawPersistentBlobCache(Path.Combine(_tempDirectory, "benchmarks-comprehensive-v10.db"));

            // Pre-generate test objects
            _testObjects = new List<TestData>();
            for (var i = 0; i < Math.Max(BenchmarkSize, 1000); i++)
            {
                _testObjects.Add(new TestData
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Object {i}",
                    Value = _randomNumberGenerator.Next(1, 10000),
                    Created = DateTimeOffset.Now.AddDays(-_randomNumberGenerator.Next(0, 365))
                });
            }
        }

        /// <summary>
        /// Globals the cleanup.
        /// </summary>
        [GlobalCleanup]
        public async void GlobalCleanup()
        {
            BenchBlobCache?.Dispose();
            _directoryCleanup?.Dispose();
            await BlobCache.Shutdown();
        }

        /// <summary>
        /// Iterations the setup.
        /// </summary>
        [IterationSetup]
        public void IterationSetup()
        {
            // Clear the cache before each iteration
            BenchBlobCache!.InvalidateAll().FirstAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the or fetch object.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("GetOrFetch")]
        public async Task GetOrFetchObject()
        {
            for (var i = 0; i < BenchmarkSize; i++)
            {
                var key = $"get_or_fetch_{i}";
                var testData = _testObjects![i % _testObjects.Count];

                await BenchBlobCache!.GetOrFetchObject(key, () =>
                    Observable.Return(testData));
            }
        }

        /// <summary>
        /// Gets the and fetch latest.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("GetAndFetch")]
        public async Task GetAndFetchLatest()
        {
            // Pre-populate some data
            for (var i = 0; i < Math.Min(BenchmarkSize, 100); i++)
            {
                var key = $"get_and_fetch_{i}";
                var testData = _testObjects![i % _testObjects.Count];
                await BenchBlobCache!.InsertObject(key, testData);
            }

            var tasks = new List<Task>();
            for (var i = 0; i < Math.Min(BenchmarkSize, 100); i++)
            {
                var key = $"get_and_fetch_{i}";
                var testData = _testObjects![i % _testObjects.Count];

                var task = BenchBlobCache!.GetAndFetchLatest(key, () =>
                    Observable.Return(testData))
                    .Take(1) // Just take the first result to avoid infinite waiting
                    .FirstAsync()
                    .ToTask();

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Invalidates the objects.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("Invalidate")]
        public async Task InvalidateObjects()
        {
            // Pre-populate data
            var keys = new List<string>();
            for (var i = 0; i < BenchmarkSize; i++)
            {
                var key = $"invalidate_test_{i}";
                var testData = _testObjects![i % _testObjects.Count];
                await BenchBlobCache!.InsertObject(key, testData);
                keys.Add(key);
            }

            // Now invalidate them
            foreach (var key in keys)
            {
                await BenchBlobCache!.InvalidateObject<TestData>(key);
            }
        }

        /// <summary>
        /// Inserts the with expiration.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("Expiration")]
        public async Task InsertWithExpiration()
        {
            var expiration = DateTimeOffset.Now.AddMinutes(30);

            for (var i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects![i % _testObjects.Count];
                await BenchBlobCache!.InsertObject($"expiration_test_{i}", testData, expiration);
            }
        }

        /// <summary>
        /// Users the account operations.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Data integrity check failed.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("UserAccount")]
        public async Task UserAccountOperations()
        {
            var userCache = BlobCache.UserAccount;

            for (var i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects![i % _testObjects.Count];
                var key = $"user_data_{i}";

                await userCache.InsertObject(key, testData);
                var retrieved = await userCache.GetObject<TestData>(key);

                // Verify data integrity
                if (retrieved.Id != testData.Id)
                {
                    throw new InvalidOperationException("Data integrity check failed");
                }
            }
        }

        /// <summary>
        /// Locals the machine operations.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Data integrity check failed.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("LocalMachine")]
        public async Task LocalMachineOperations()
        {
            var localCache = BlobCache.LocalMachine;

            for (var i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects![i % _testObjects.Count];
                var key = $"local_data_{i}";

                await localCache.InsertObject(key, testData);
                var retrieved = await localCache.GetObject<TestData>(key);

                // Verify data integrity
                if (retrieved.Id != testData.Id)
                {
                    throw new InvalidOperationException("Data integrity check failed");
                }
            }
        }

        /// <summary>
        /// Secures the operations.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Data integrity check failed.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("Secure")]
        public async Task SecureOperations()
        {
            var secureCache = BlobCache.Secure;

            for (var i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects![i % _testObjects.Count];
                var key = $"secure_data_{i}";

                await secureCache.InsertObject(key, testData);
                var retrieved = await secureCache.GetObject<TestData>(key);

                // Verify data integrity
                if (retrieved.Id != testData.Id)
                {
                    throw new InvalidOperationException("Data integrity check failed");
                }
            }
        }

        /// <summary>
        /// Ins the memory operations.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Data integrity check failed.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("InMemory")]
        public async Task InMemoryOperations()
        {
            var memoryCache = BlobCache.InMemory;

            for (var i = 0; i < BenchmarkSize; i++)
            {
                var testData = _testObjects![i % _testObjects.Count];
                var key = $"memory_data_{i}";

                await memoryCache.InsertObject(key, testData);
                var retrieved = await memoryCache.GetObject<TestData>(key);

                // Verify data integrity
                if (retrieved.Id != testData.Id)
                {
                    throw new InvalidOperationException("Data integrity check failed");
                }
            }
        }

        /// <summary>
        /// Mixeds the operations.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Update verification failed.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("Mixed")]
        public async Task MixedOperations()
        {
            var caches = new IBlobCache[]
            {
                BlobCache.UserAccount,
                BlobCache.LocalMachine,
                BlobCache.InMemory
            };

            for (var i = 0; i < BenchmarkSize; i++)
            {
                var cache = caches[i % caches.Length];
                var testData = _testObjects![i % _testObjects.Count];
                var key = $"mixed_data_{i}";

                // Insert
                await cache.InsertObject(key, testData);

                // Read
                var retrieved = await cache.GetObject<TestData>(key);

                // Update
                retrieved.Value += 1;
                await cache.InsertObject(key, retrieved);

                // Read again
                var updated = await cache.GetObject<TestData>(key);

                // Verify update
                if (updated.Value != testData.Value + 1)
                {
                    throw new InvalidOperationException("Update verification failed");
                }
            }
        }
    }
}
