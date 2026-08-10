// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using Akavache.Sqlite3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Akavache.Benchmarks.V10;

/// <summary> Measures how fast Akavache V10 writes blobs and objects into a SQLite-backed cache, sequentially, in parallel and with an expiry. </summary>
[System.Diagnostics.DebuggerDisplay("{BenchmarkSize}")]
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Security",
    "CA5394:Do not use insecure randomness",
    Justification = "Benchmark fixture data drawn inside the measured region. A cryptographic "
        + "generator costs enough that the benchmark would partly be measuring it, and this data "
        + "is fixture material rather than a secret.")]
public class AkavacheV10WriteBenchmarks
{
    /// <summary>Exclusive upper bound on the randomly generated <see cref="TestData.Value"/> of a written object.</summary>
    private const int MaxTestDataValue = 1000;

    /// <summary>Exclusive upper bound, in days, on how far in the past a written object's creation timestamp is placed.</summary>
    private const int MaxTestDataAgeDays = 30;

    /// <summary>The per-benchmark temp directory created in setup and cleaned up in teardown.</summary>
    private string? _tempDirectory;

    /// <summary>The disposable handle that removes <see cref="_tempDirectory"/> when the benchmark finishes.</summary>
    private IDisposable? _directoryCleanup;

    /// <summary> Gets or sets the size of the benchmark. </summary>
    /// <value>
    /// The size of the benchmark.
    /// </value>
    [Params(10, 100, 1000)]
    public int BenchmarkSize { get; set; }

    /// <summary> Gets or sets the bench BLOB cache. </summary>
    /// <value>
    /// The bench BLOB cache.
    /// </value>
    public IBlobCache? BenchBlobCache { get; set; }

    /// <summary> Globals the setup. </summary>
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

    /// <summary> Globals the cleanup. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        BenchBlobCache?.Dispose();
        _directoryCleanup?.Dispose();
        await BlobCache.Shutdown();
    }

    /// <summary> Clears the cache before each iteration so every measured write starts from an empty database. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [IterationSetup]
    public void IterationSetup() => BenchBlobCache!.InvalidateAll().FirstAsync().GetAwaiter().GetResult();

    /// <summary> Sequentials the write. </summary>
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

    /// <summary> Sequentials the object write. </summary>
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
                Value = PerfHelper.Rng.Next(1, MaxTestDataValue),
                Created = TimeProvider.System.GetLocalNow().AddDays(-PerfHelper.Rng.Next(0, MaxTestDataAgeDays)),
            };

            await BenchBlobCache!.InsertObject($"object_{i}", testData);
        }
    }

    /// <summary> Parallels the write. </summary>
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

    /// <summary> Writes the with expiration. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Write")]
    public async Task WriteWithExpiration()
    {
        var dataToWrite = PerfHelper.GenerateRandomDatabaseContents(BenchmarkSize);
        var expiration = TimeProvider.System.GetLocalNow().AddHours(1);

        foreach (var kvp in dataToWrite)
        {
            await BenchBlobCache!.Insert(kvp.Key, kvp.Value, expiration);
        }
    }
}
