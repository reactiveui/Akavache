// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Akavache;
using Akavache.Sqlite3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Splat;

namespace Akavache.Benchmarks.V10
{
    /// <summary>
    /// AkavacheV10ReadBenchmarks.
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net90)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class AkavacheV10ReadBenchmarks
    {
        private readonly Random _randomNumberGenerator = new();
        private string? _tempDirectory;
        private IDisposable? _directoryCleanup;

        /// <summary>
        /// Gets or sets the size of the benchmark.
        /// </summary>
        /// <value>
        /// The size of the benchmark.
        /// </value>
        [Params(10, 100, 1000)]
        public int BenchmarkSize { get; set; }

        /// <summary>
        /// Gets or sets the BLOB cache.
        /// </summary>
        /// <value>
        /// The BLOB cache.
        /// </value>
        public IBlobCache? BenchBlobCache { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the keys.
        /// </summary>
        /// <value>
        /// The keys.
        /// </value>
        public IList<string>? Keys { get; set; }

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
            var di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(st.GetFileName()!)!));

            return di.FullName;
        }

        /// <summary>
        /// Globals the setup.
        /// </summary>
        [GlobalSetup]
        public async void GlobalSetup()
        {
            // Initialize Akavache V10 style
            BlobCache.ApplicationName = "AkavacheBenchmarksV10";
            Akavache.Sqlite3.Registrations.Start("AkavacheExperiment", () => { });

            // Create temporary directory
            _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

            // Generate database synchronously to avoid deadlocks
            BenchBlobCache = GenerateAGiantDatabaseSync(_tempDirectory);
            Keys = await BenchBlobCache.GetAllKeys().Select(x => x.ToList());
            Size = BenchmarkSize;
        }

        /// <summary>
        /// Globals the cleanup.
        /// </summary>
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            BenchBlobCache?.Dispose();
            _directoryCleanup?.Dispose();
            BlobCache.Shutdown().Wait();
        }

        /// <summary>
        /// Sequentials the read.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("Read")]
        public async Task SequentialRead()
        {
            var toFetch = Enumerable.Range(0, Size)
                .Select(_ => Keys![_randomNumberGenerator.Next(0, Keys.Count - 1)])
                .ToArray();

            foreach (var v in toFetch)
            {
                await BenchBlobCache!.Get(v);
            }
        }

        /// <summary>
        /// Randoms the read.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("Read")]
        public async Task RandomRead()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < Size; i++)
            {
                var randomKey = Keys![_randomNumberGenerator.Next(0, Keys.Count - 1)];
                tasks.Add(BenchBlobCache!.Get(randomKey).FirstAsync().ToTask());
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Sequentials the object read.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Benchmark]
        [BenchmarkCategory("ObjectRead")]
        public async Task SequentialObjectRead()
        {
            var toFetch = Enumerable.Range(0, Size)
                .Select(_ => Keys![_randomNumberGenerator.Next(0, Keys.Count - 1)])
                .ToArray();

            foreach (var v in toFetch)
            {
                try
                {
                    await BenchBlobCache!.GetObject<TestData>(v);
                }
                catch (Exception)
                {
                    // Some keys might not have object data
                }
            }
        }

        /// <summary>
        /// Generates a giant database synchronously for GlobalSetup.
        /// </summary>
        /// <param name="path">A path to use for generating it.</param>
        /// <returns>The blob cache.</returns>
        private SqlRawPersistentBlobCache GenerateAGiantDatabaseSync(string path)
        {
            try
            {
                path ??= GetIntegrationTestRootDirectory();

                var giantDbSize = Math.Max(1000, BenchmarkSize * 10); // Ensure enough data for benchmarks
                var cache = new SqlRawPersistentBlobCache(Path.Combine(path, "benchmarks-read-v10.db"));

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

                    foreach (var kvp in toWrite)
                    {
                        cache.Insert(kvp.Key, kvp.Value).FirstAsync().GetAwaiter().GetResult();
                        ret.Add(kvp.Key);
                    }

                    // Also add some object data
                    for (var i = 0; i < Math.Min(100, chunkSize); i++)
                    {
                        var testData = new TestData
                        {
                            Id = Guid.NewGuid(),
                            Name = $"Test Item {i}",
                            Value = _randomNumberGenerator.Next(1, 1000),
                            Created = DateTimeOffset.Now.AddDays(-_randomNumberGenerator.Next(0, 30))
                        };

                        var objectKey = $"object_{i}_{DateTime.Now.Ticks}";
                        cache.InsertObject(objectKey, testData).FirstAsync().GetAwaiter().GetResult();
                        ret.Add(objectKey);
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
    }
}
