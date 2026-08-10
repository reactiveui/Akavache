// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Threading.Tasks;
using Akavache.Sqlite3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Akavache.Benchmarks.V10;

/// <summary> Measures how fast Akavache V10 reads blobs and objects back out of a pre-seeded SQLite-backed cache, sequentially and concurrently. </summary>
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
public class AkavacheV10ReadBenchmarks
{
    /// <summary>Floor on the number of entries seeded into the read database, so even the smallest benchmark size still reads from a realistically populated cache.</summary>
    private const int MinimumSeededItemCount = 1000;

    /// <summary>How many entries are seeded per unit of <see cref="BenchmarkSize"/>, so reads keep missing the same few rows.</summary>
    private const int SeededItemsPerBenchmarkItem = 10;

    /// <summary>Number of entries seeded per pass, keeping the in-flight generated set small enough not to distort memory during setup.</summary>
    private const int SeedChunkItemCount = 500;

    /// <summary>Ceiling on how many serialized objects each seeding chunk contributes alongside its raw blobs.</summary>
    private const int SeedObjectsPerChunk = 100;

    /// <summary>Exclusive upper bound on the randomly generated <see cref="TestData.Value"/> of a seeded object.</summary>
    private const int MaxTestDataValue = 1000;

    /// <summary>Exclusive upper bound, in days, on how far in the past a seeded object's creation timestamp is placed.</summary>
    private const int MaxTestDataAgeDays = 30;

    /// <summary>How many seeded entries pass between seeding progress reports.</summary>
    private const int SeedProgressReportInterval = 2000;

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

    /// <summary> Gets or sets the BLOB cache. </summary>
    /// <value>
    /// The BLOB cache.
    /// </value>
    public IBlobCache? BenchBlobCache { get; set; }

    /// <summary> Gets or sets the size. </summary>
    /// <value>
    /// The size.
    /// </value>
    public int Size { get; set; }

    /// <summary> Gets or sets the keys. </summary>
    /// <value>
    /// The keys.
    /// </value>
    public List<string>? Keys { get; set; }

    /// <summary> Gets the root folder for the integration tests. </summary>
    /// <returns>The root folder.</returns>
    [SuppressMessage(
        "Modernization",
        "SST2209:A null-forgiving operator has no local effect",
        Justification = "StackFrame.GetFileName and Path.GetDirectoryName are both nullable, so dropping the operator produces CS8604 on Path.Combine.")]
    public static string GetIntegrationTestRootDirectory()
    {
        // XXX: This is an evil hack, but it's okay for a unit test
        // We can't use Assembly.Location because unit test runners love
        // to move stuff to temp directories
        StackFrame st = new(true);
        DirectoryInfo di = new(Path.Combine(Path.GetDirectoryName(st.GetFileName())!));

        return di.FullName;
    }

    /// <summary> Globals the setup. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Initialize Akavache V10 style
        BlobCache.ApplicationName = "AkavacheBenchmarksV10";
        Registrations.Start("AkavacheExperiment", static () => { });

        // Create temporary directory
        _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

        // Generate database synchronously to avoid deadlocks
        BenchBlobCache = GenerateAGiantDatabaseSync(_tempDirectory);

        Keys = new(await BenchBlobCache.GetAllKeys());
        Size = BenchmarkSize;
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

    /// <summary> Sequentials the read. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Read")]
    public async Task SequentialRead()
    {
        var toFetch = new string[Size];
        for (var i = 0; i < toFetch.Length; i++)
        {
            toFetch[i] = Keys![PerfHelper.Rng.Next(0, Keys.Count - 1)];
        }

        foreach (var v in toFetch)
        {
            await BenchBlobCache!.Get(v);
        }
    }

    /// <summary> Randoms the read. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Read")]
    public async Task RandomRead()
    {
        List<Task> tasks = [];

        for (var i = 0; i < Size; i++)
        {
            var randomKey = Keys![PerfHelper.Rng.Next(0, Keys.Count - 1)];
            tasks.Add(BenchBlobCache!.Get(randomKey).FirstAsync().ToTask());
        }

        await Task.WhenAll(tasks);
    }

    /// <summary> Sequentials the object read. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("ObjectRead")]
    public async Task SequentialObjectRead()
    {
        var toFetch = new string[Size];
        for (var i = 0; i < toFetch.Length; i++)
        {
            toFetch[i] = Keys![PerfHelper.Rng.Next(0, Keys.Count - 1)];
        }

        foreach (var v in toFetch)
        {
            try
            {
                await BenchBlobCache!.GetObject<TestData>(v);
            }
            catch (KeyNotFoundException)
            {
                // Some keys might not have object data
            }
        }
    }

    /// <summary> Generates a giant database synchronously for GlobalSetup. </summary>
    /// <param name="path">A path to use for generating it.</param>
    /// <returns>The blob cache.</returns>
    private SqlRawPersistentBlobCache GenerateAGiantDatabaseSync(string path)
    {
        try
        {
            path ??= GetIntegrationTestRootDirectory();

            var giantDbSize = Math.Max(MinimumSeededItemCount, BenchmarkSize * SeededItemsPerBenchmarkItem);
            SqlRawPersistentBlobCache cache = new(Path.Combine(path, "benchmarks-read-v10.db"));

            var keys = cache.GetAllKeys().ToList().FirstAsync().GetAwaiter().GetResult();
            if (keys.Count >= giantDbSize)
            {
                return cache;
            }

            _ = cache.InvalidateAll().FirstAsync().GetAwaiter().GetResult();

            // Generate smaller chunks to avoid memory issues
            List<string> ret = [];
            var remaining = giantDbSize;

            while (remaining > 0)
            {
                var chunkSize = Math.Min(SeedChunkItemCount, remaining);
                foreach (var kvp in PerfHelper.GenerateRandomDatabaseContents(chunkSize))
                {
                    _ = cache.Insert(kvp.Key, kvp.Value).FirstAsync().GetAwaiter().GetResult();
                    ret.Add(kvp.Key);
                }

                // Also add some object data
                for (var i = 0; i < Math.Min(SeedObjectsPerChunk, chunkSize); i++)
                {
                    TestData testData = new()
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Test Item {i}",
                        Value = PerfHelper.Rng.Next(1, MaxTestDataValue),
                        Created = TimeProvider.System.GetLocalNow().AddDays(-PerfHelper.Rng.Next(0, MaxTestDataAgeDays)),
                    };

                    var objectKey = $"object_{i}_{TimeProvider.System.GetLocalNow().DateTime.Ticks}";
                    _ = cache.InsertObject(objectKey, testData).FirstAsync().GetAwaiter().GetResult();
                    ret.Add(objectKey);
                }

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
