// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Akavache.Benchmarks;

/// <summary> Measures how fast Akavache V11 writes blobs and objects into a SQLite-backed cache, one key at a time, in bulk and with an expiry. </summary>
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
public class CacheDatabaseWriteBenchmarks
{
    /// <summary>Exclusive upper bound on the randomly generated <see cref="TestDataV11.Value"/> of a written object.</summary>
    private const int MaxTestDataValue = 1000;

    /// <summary>Exclusive upper bound, in days, on how far in the past a written object's creation timestamp is placed.</summary>
    private const int MaxTestDataAgeDays = 30;

    /// <summary>The per-benchmark temp directory. Assigned by <see cref="GlobalSetup"/> before any benchmark runs.</summary>
    private string _tempDirectory = null!;

    /// <summary>The disposable handle that removes <see cref="_tempDirectory"/> when the benchmark finishes. Assigned by <see cref="GlobalSetup"/>.</summary>
    private IDisposable _directoryCleanup = null!;

    /// <summary> Gets or sets the number of entries each measured write stores. </summary>
    /// <value>
    /// The number of entries each measured write stores.
    /// </value>
    [Params(10, 100, 1000)]
    public int BenchmarkSize { get; set; }

    /// <summary> Gets or sets the SQLite-backed cache under measurement. Assigned by <see cref="GlobalSetup"/>. </summary>
    /// <value>
    /// The SQLite-backed cache under measurement.
    /// </value>
    public IBlobCache BlobCache { get; set; } = null!;

    /// <summary> Creates a temp directory and a fresh SQLite database so every run writes into an empty file. </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        // Create temporary directory
        _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

        // Create fresh database for each run
        BlobCache = new SqliteBlobCache(Path.Combine(_tempDirectory, "benchmarks-write-v11.db"), new SystemJsonSerializer());
    }

    /// <summary> Closes the cache and removes the temp directory it was created in. </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        BlobCache?.Dispose();
        _directoryCleanup?.Dispose();
    }

    /// <summary> Clears the cache before each iteration so every measured write starts from an empty database. </summary>
    [IterationSetup]
    public void IterationSetup() => BlobCache.InvalidateAll().WaitForCompletion();

    /// <summary> Measures inserting <see cref="BenchmarkSize"/> freshly generated blobs one key at a time, awaiting each write before the next starts. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Write")]
    public async Task SequentialWrite()
    {
        foreach (var kvp in PerfHelper.GenerateRandomDatabaseContents(BenchmarkSize))
        {
            await BlobCache.Insert(kvp.Key, kvp.Value);
        }
    }

    /// <summary> Measures inserting <see cref="BenchmarkSize"/> objects one at a time, so the serializer cost sits on the measured write path. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Write")]
    [RequiresUnreferencedCode("Measuring InsertObject requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Measuring InsertObject requires types to be preserved for serialization.")]
    public async Task SequentialObjectWrite()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            TestDataV11 testData = new()
            {
                Id = Guid.NewGuid(),
                Name = $"Test Item {i}",
                Value = PerfHelper.Rng.Next(1, MaxTestDataValue),
                Created = TimeProvider.System.GetLocalNow().AddDays(-PerfHelper.Rng.Next(0, MaxTestDataAgeDays))
            };

            await BlobCache.InsertObject($"object_{i}", testData);
        }
    }

    /// <summary> Measures handing the same <see cref="BenchmarkSize"/> blobs to the cache as a single bulk insert, so the per-call overhead of <see cref="SequentialWrite"/> is amortized. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Write")]
    public async Task BulkWrite()
    {
        var dataToWrite = PerfHelper.GenerateRandomDatabaseContents(BenchmarkSize);
        await BlobCache.Insert(dataToWrite);
    }

    /// <summary> Measures the sequential write path when every entry also carries an absolute expiry, which the cache has to persist alongside the payload. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Write")]
    public async Task WriteWithExpiration()
    {
        var dataToWrite = PerfHelper.GenerateRandomDatabaseContents(BenchmarkSize);
        var expiration = TimeProvider.System.GetLocalNow().AddHours(1);

        foreach (var kvp in dataToWrite)
        {
            await BlobCache.Insert(kvp.Key, kvp.Value, expiration);
        }
    }
}
