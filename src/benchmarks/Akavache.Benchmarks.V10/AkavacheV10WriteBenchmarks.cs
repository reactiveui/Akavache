// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using System.Security.Cryptography;
using Akavache.Sqlite3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Akavache.Benchmarks.V10;

/// <summary>
/// AkavacheV10WriteBenchmarks.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class AkavacheV10WriteBenchmarks
{
    /// <summary>The per-benchmark temp directory created in setup and cleaned up in teardown.</summary>
    private string? _tempDirectory;

    /// <summary>The disposable handle that removes <see cref="_tempDirectory"/> when the benchmark finishes.</summary>
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
        BlobCache.ApplicationName = "AkavacheBenchmarksV10Write";

        // Create temporary directory
        _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

        // Create fresh database for each run
        BenchBlobCache = new SqlRawPersistentBlobCache(Path.Combine(_tempDirectory, "benchmarks-write-v10.db"));
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
    public void IterationSetup() =>

        // Clear the cache before each iteration
        BenchBlobCache!.InvalidateAll().FirstAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Sequentials the write.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Write")]
    public async Task SequentialWrite()
    {
        foreach (var kvp in PerfHelper.GenerateRandomDatabaseContents(BenchmarkSize))
        {
            await BenchBlobCache!.Insert(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Sequentials the object write.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Write")]
    public async Task SequentialObjectWrite()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            TestData testData = new()
            {
                Id = Guid.NewGuid(),
                Name = $"Test Item {i}",
                Value = RandomNumberGenerator.GetInt32(1, 1000),
                Created = DateTimeOffset.Now.AddDays(-RandomNumberGenerator.GetInt32(0, 30))
            };

            await BenchBlobCache!.InsertObject($"object_{i}", testData);
        }
    }

    /// <summary>
    /// Parallels the write.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Write")]
    public async Task ParallelWrite()
    {
        var dataToWrite = PerfHelper.GenerateRandomDatabaseContents(BenchmarkSize);
        List<Task> tasks = [];

        foreach (var kvp in dataToWrite)
        {
            tasks.Add(BenchBlobCache!.Insert(kvp.Key, kvp.Value).FirstAsync().ToTask());
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Writes the with expiration.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Write")]
    public async Task WriteWithExpiration()
    {
        var dataToWrite = PerfHelper.GenerateRandomDatabaseContents(BenchmarkSize);
        var expiration = DateTimeOffset.Now.AddHours(1);

        foreach (var kvp in dataToWrite)
        {
            await BenchBlobCache!.Insert(kvp.Key, kvp.Value, expiration);
        }
    }
}