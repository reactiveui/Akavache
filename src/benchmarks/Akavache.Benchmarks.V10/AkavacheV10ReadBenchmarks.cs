// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reactive.Threading.Tasks;
using System.Security.Cryptography;
using Akavache.Sqlite3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Akavache.Benchmarks.V10;

/// <summary>
/// AkavacheV10ReadBenchmarks.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class AkavacheV10ReadBenchmarks
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
    public List<string>? Keys { get; set; }

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
        var di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(st.GetFileName())!));

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
        Registrations.Start("AkavacheExperiment", static () => { });

        // Create temporary directory
        _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

        // Generate database synchronously to avoid deadlocks
        BenchBlobCache = GenerateAGiantDatabaseSync(_tempDirectory);
        Keys = await BenchBlobCache.GetAllKeys().Select(static x => x.ToList());
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
            .Select(_ => Keys![RandomNumberGenerator.GetInt32(0, Keys.Count - 1)])
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
            var randomKey = Keys![RandomNumberGenerator.GetInt32(0, Keys.Count - 1)];
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
            .Select(_ => Keys![RandomNumberGenerator.GetInt32(0, Keys.Count - 1)])
            .ToArray();

        foreach (var v in toFetch)
        {
            try
            {
                await BenchBlobCache!.GetObject<TestData>(v);
            }
            catch
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
                foreach (var kvp in PerfHelper.GenerateRandomDatabaseContents(chunkSize))
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
                        Value = RandomNumberGenerator.GetInt32(1, 1000),
                        Created = DateTimeOffset.Now.AddDays(-RandomNumberGenerator.GetInt32(0, 30))
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
