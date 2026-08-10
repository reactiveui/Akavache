// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Akavache.Benchmarks;

/// <summary> Measures how fast Akavache V11 reads blobs back out of a pre-seeded SQLite-backed cache, sequentially, concurrently and as a single bulk request. </summary>
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
public class CacheDatabaseReadBenchmarks
{
    /// <summary>Floor on the number of entries seeded into the read database, so even the smallest benchmark size still reads from a realistically populated cache.</summary>
    private const int MinimumSeededItemCount = 1000;

    /// <summary>How many entries are seeded per unit of <see cref="BenchmarkSize"/>, so reads keep missing the same few rows.</summary>
    private const int SeededItemsPerBenchmarkItem = 10;

    /// <summary>Number of entries seeded per pass, keeping the in-flight generated set small enough not to distort memory during setup.</summary>
    private const int SeedChunkItemCount = 500;

    /// <summary>How many seeded entries pass between seeding progress reports.</summary>
    private const int SeedProgressReportInterval = 2000;

    /// <summary>The per-benchmark temp directory. Assigned by <see cref="GlobalSetup"/> before any benchmark runs.</summary>
    private string _tempDirectory = null!;

    /// <summary>The disposable handle that removes <see cref="_tempDirectory"/> when the benchmark finishes. Assigned by <see cref="GlobalSetup"/>.</summary>
    private IDisposable _directoryCleanup = null!;

    /// <summary> Gets or sets the size of the benchmark, which drives both how many entries are seeded and how many reads each iteration issues. </summary>
    /// <value>
    /// The size of the benchmark.
    /// </value>
    [Params(10, 100, 1000)]
    public int BenchmarkSize { get; set; }

    /// <summary> Gets or sets the seeded SQLite-backed cache the reads are issued against. Assigned by <see cref="GlobalSetup"/>. </summary>
    /// <value>
    /// The seeded SQLite-backed cache.
    /// </value>
    public IBlobCache BlobCache { get; set; } = null!;

    /// <summary> Gets or sets the number of reads a single measured iteration issues. Assigned by <see cref="GlobalSetup"/> from <see cref="BenchmarkSize"/>. </summary>
    /// <value>
    /// The number of reads a single measured iteration issues.
    /// </value>
    public int Size { get; set; }

    /// <summary> Gets or sets every key present in the seeded database, which the benchmarks sample at random. Assigned by <see cref="GlobalSetup"/>. </summary>
    /// <value>
    /// Every key present in the seeded database.
    /// </value>
    public IList<string> Keys { get; set; } = null!;

    /// <summary> Gets the root folder for the integration tests. </summary>
    /// <returns>The root folder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the stack frame carries no source path, which happens when the assembly is built without a portable PDB.</exception>
    public static string GetIntegrationTestRootDirectory()
    {
        // XXX: This is an evil hack, but it's okay for a unit test
        // We can't use Assembly.Location because unit test runners love
        // to move stuff to temp directories
        StackFrame st = new(true);
        var sourceFile = st.GetFileName()
            ?? throw new InvalidOperationException("The benchmark assembly carries no source file information; build it with a portable PDB.");

        var sourceDirectory = Path.GetDirectoryName(sourceFile)
            ?? throw new InvalidOperationException($"Source path '{sourceFile}' has no parent directory.");

        DirectoryInfo di = new(sourceDirectory);

        return di.FullName;
    }

    /// <summary> Creates a temp directory, seeds a database large enough for the requested benchmark size and caches its key set. </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        // Create temporary directory
        _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

        // Generate database synchronously to avoid deadlocks
        BlobCache = GenerateAGiantDatabaseSync(_tempDirectory);
        Keys = BlobCache.GetAllKeys().ToList().WaitForValue() ?? [];
        Size = BenchmarkSize;
    }

    /// <summary> Closes the cache and removes the temp directory the seeded database lives in. </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        BlobCache?.Dispose();
        _directoryCleanup?.Dispose();
    }

    /// <summary> Measures <see cref="Size"/> single-key reads issued one after another, so each round trip pays the full latency of the one before it. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Read")]
    public async Task SequentialRead()
    {
        var toFetch = new string[Size];
        for (var i = 0; i < toFetch.Length; i++)
        {
            toFetch[i] = Keys[PerfHelper.Rng.Next(0, Keys.Count - 1)];
        }

        foreach (var v in toFetch)
        {
            await BlobCache.Get(v);
        }
    }

    /// <summary> Measures <see cref="Size"/> single-key reads started together and awaited as a group, so the cache's operation queue sees them concurrently. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Read")]
    public async Task RandomRead()
    {
        List<Task> tasks = [];

        for (var i = 0; i < Size; i++)
        {
            var randomKey = Keys[PerfHelper.Rng.Next(0, Keys.Count - 1)];
            tasks.Add(BlobCache.Get(randomKey).FirstAsync());
        }

        await Task.WhenAll(tasks);
    }

    /// <summary> Measures fetching the same <see cref="Size"/> keys through the bulk read API, so the per-key overhead of <see cref="SequentialRead"/> is amortized into one request. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Read")]
    public async Task BulkRead()
    {
        var toFetch = new string[Size];
        for (var i = 0; i < toFetch.Length; i++)
        {
            toFetch[i] = Keys[PerfHelper.Rng.Next(0, Keys.Count - 1)];
        }

        await BlobCache.Get(toFetch).ToList().FirstAsync();
    }

    /// <summary> Generates a giant database synchronously for GlobalSetup. </summary>
    /// <param name="path">A path to use for generating it.</param>
    /// <returns>The blob cache.</returns>
    private SqliteBlobCache GenerateAGiantDatabaseSync(string path)
    {
        try
        {
            path ??= GetIntegrationTestRootDirectory();

            // Ensure enough data for benchmarks
            var giantDbSize = Math.Max(MinimumSeededItemCount, BenchmarkSize * SeededItemsPerBenchmarkItem);
            SqliteBlobCache cache = new(Path.Combine(path, "benchmarks-read.db"), new SystemJsonSerializer());

            var keys = cache.GetAllKeys().ToList().WaitForValue();
            if (keys?.Count >= giantDbSize)
            {
                return cache;
            }

            cache.InvalidateAll().WaitForCompletion();

            // Generate smaller chunks to avoid memory issues
            List<string> ret = [];
            var remaining = giantDbSize;

            while (remaining > 0)
            {
                // Process in reasonable chunks
                var chunkSize = Math.Min(SeedChunkItemCount, remaining);
                var toWrite = PerfHelper.GenerateRandomDatabaseContents(chunkSize);

                cache.Insert(toWrite).WaitForCompletion();

                ret.AddRange(toWrite.Keys);

                remaining -= chunkSize;

                if (remaining % SeedProgressReportInterval == 0 || remaining == 0)
                {
                    ConsoleLogger.Default.WriteLine($"Generated {giantDbSize - remaining}/{giantDbSize} items");
                }
            }

            return cache;
        }
        catch (Exception ex)
        {
            ConsoleLogger.Default.WriteLine($"Error in GenerateAGiantDatabaseSync: {ex.Message}");
            throw;
        }
    }
}
