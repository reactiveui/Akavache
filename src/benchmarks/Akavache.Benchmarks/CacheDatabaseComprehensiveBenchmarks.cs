// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Splat.Builder;

namespace Akavache.Benchmarks;

/// <summary>
/// Measures the full Akavache V11 object API — get-or-fetch, get-and-fetch-latest, invalidation,
/// expiry, bulk access and each of the four built-in caches — against a SQLite-backed store.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[RequiresUnreferencedCode("Measuring the object API requires types to be preserved for serialization.")]
[RequiresDynamicCode("Measuring the object API requires types to be preserved for serialization.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Security",
    "CA5394:Do not use insecure randomness",
    Justification = "Benchmark fixture data drawn inside the measured region. A cryptographic "
        + "generator costs enough that the benchmark would partly be measuring it, and this data "
        + "is fixture material rather than a secret.")]
public class CacheDatabaseComprehensiveBenchmarks
{
    /// <summary>Message reported when a value read back out of the cache does not match the value that was written.</summary>
    private const string DataIntegrityFailureMessage = "Data integrity check failed";

    /// <summary>The application name the benchmark's Akavache instance is registered under.</summary>
    private const string ApplicationName = "AkavacheBenchmarksV11Comprehensive";

    /// <summary>Floor on the size of the pre-generated working set, so even the smallest benchmark size still cycles through varied objects.</summary>
    private const int MinimumTestObjectCount = 1000;

    /// <summary>Exclusive upper bound on the randomly generated <see cref="TestDataV11.Value"/> of a working-set object.</summary>
    private const int MaxTestObjectValue = 10_000;

    /// <summary>Exclusive upper bound, in days, on how far in the past a working-set object's creation timestamp is placed.</summary>
    private const int MaxTestObjectAgeDays = 365;

    /// <summary>Ceiling on the number of fetch-latest round trips per iteration, which are far more expensive than a plain insert.</summary>
    private const int MaxFetchLatestOperationCount = 100;

    /// <summary>How far in the future the expiry-write benchmark sets each entry's expiry.</summary>
    private const int EntryExpiryMinutes = 30;

    /// <summary>The Splat builder the Akavache instance under measurement is registered into.</summary>
    private readonly AppBuilder _appBuilder = AppBuilder.CreateSplatBuilder();

    /// <summary>The per-benchmark temp directory. Assigned by <see cref="GlobalSetup"/> before any benchmark runs.</summary>
    private string _tempDirectory = null!;

    /// <summary>The disposable handle that removes <see cref="_tempDirectory"/> when the benchmark finishes. Assigned by <see cref="GlobalSetup"/>.</summary>
    private IDisposable _directoryCleanup = null!;

    /// <summary>Pre-generated objects used as the benchmark's working set. Assigned by <see cref="GlobalSetup"/>.</summary>
    private List<TestDataV11> _testObjects = null!;

    /// <summary> Gets or sets the number of cache operations each measured benchmark performs. </summary>
    /// <value>
    /// The number of cache operations each measured benchmark performs.
    /// </value>
    [Params(10, 100, 1000)]
    public int BenchmarkSize { get; set; }

    /// <summary> Gets or sets the SQLite-backed cache the object benchmarks target directly. Assigned by <see cref="GlobalSetup"/>. </summary>
    /// <value>
    /// The SQLite-backed cache the object benchmarks target directly.
    /// </value>
    public IBlobCache BlobCache { get; set; } = null!;

    /// <summary> Builds the Akavache V11 instance, creates its SQLite database under a fresh temp directory and pre-generates the working set the benchmarks cycle through. </summary>
    [GlobalSetup]
    public void GlobalSetup() => _ = _appBuilder.WithAkavache<SystemJsonSerializer>(
        ApplicationName,
        static builder =>
            builder
                .WithSqliteProvider()
                .WithSqliteDefaults(),
        instance =>
        {
            // Create temporary directory
            _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

            // Create database
            BlobCache = new SqliteBlobCache(
                Path.Combine(_tempDirectory, "benchmarks-comprehensive-v11.db"),
                instance.Serializer ?? throw new InvalidOperationException("The Akavache instance was built without a serializer"));

            // Pre-generate test objects
            _testObjects = [];
            for (var i = 0; i < Math.Max(BenchmarkSize, MinimumTestObjectCount); i++)
            {
                _testObjects.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Object {i}",
                    Value = PerfHelper.Rng.Next(1, MaxTestObjectValue),
                    Created = TimeProvider.System.GetLocalNow().AddDays(-PerfHelper.Rng.Next(0, MaxTestObjectAgeDays))
                });
            }
        });

    /// <summary> Closes the cache, removes the temp directory and shuts the shared cache database down. </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        BlobCache?.Dispose();
        _directoryCleanup?.Dispose();
        CacheDatabase.Shutdown().WaitForCompletion();
    }

    /// <summary> Clears the cache before each iteration so every measured run starts from an empty database. </summary>
    [IterationSetup]
    public void IterationSetup() => BlobCache.InvalidateAll().WaitForCompletion();

    /// <summary> Measures <see cref="BenchmarkSize"/> get-or-fetch calls against keys that are never present, so every call pays the fetch plus the write that caches its result. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("GetOrFetch")]
    public async Task GetOrFetchObject()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            var key = $"get_or_fetch_{i}";
            var testData = _testObjects[i % _testObjects.Count];

            await BlobCache.GetOrFetchObject(key, () =>
                Signal.Return(testData));
        }
    }

    /// <summary> Measures the get-and-fetch-latest path on keys that are already cached, so each call serves the cached value and races a refresh behind it. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("GetAndFetch")]
    public async Task GetAndFetchLatest()
    {
        // Pre-populate some data
        for (var i = 0; i < Math.Min(BenchmarkSize, MaxFetchLatestOperationCount); i++)
        {
            var key = $"get_and_fetch_{i}";
            var testData = _testObjects[i % _testObjects.Count];
            await BlobCache.InsertObject(key, testData);
        }

        List<Task> tasks = [];
        for (var i = 0; i < Math.Min(BenchmarkSize, MaxFetchLatestOperationCount); i++)
        {
            var key = $"get_and_fetch_{i}";
            var testData = _testObjects[i % _testObjects.Count];

            var task = BlobCache.GetAndFetchLatest(key, () =>
                    Signal.Return(testData))
                .Take(1) // Just take the first result to avoid infinite waiting
                .FirstAsync()
                .ToTask();

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary> Measures the typed invalidation path: <see cref="BenchmarkSize"/> objects are written, then removed one key at a time. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Invalidate")]
    public async Task InvalidateObjects()
    {
        // Pre-populate data
        List<string> keys = [];
        for (var i = 0; i < BenchmarkSize; i++)
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

    /// <summary> Measures object writes that also carry an absolute expiry, so the cache persists an expiry column alongside every serialized payload. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Expiration")]
    public async Task InsertWithExpiration()
    {
        var expiration = TimeProvider.System.GetLocalNow().AddMinutes(EntryExpiryMinutes);

        for (var i = 0; i < BenchmarkSize; i++)
        {
            var testData = _testObjects[i % _testObjects.Count];
            await BlobCache.InsertObject($"expiration_test_{i}", testData, expiration);
        }
    }

    /// <summary> Measures a write-then-read round trip through the built-in UserAccount cache, verifying the value survives the trip. </summary>
    /// <exception cref="InvalidOperationException">A value read back did not match the value written.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("UserAccount")]
    public async Task UserAccountOperations()
    {
        var userCache = CacheDatabase.UserAccount;

        for (var i = 0; i < BenchmarkSize; i++)
        {
            var testData = _testObjects[i % _testObjects.Count];
            var key = $"user_data_{i}";

            await userCache.InsertObject(key, testData);
            var retrieved = await userCache.GetObject<TestDataV11>(key);

            // Verify data integrity
            if (retrieved.Id != testData.Id)
            {
                throw new InvalidOperationException(DataIntegrityFailureMessage);
            }
        }
    }

    /// <summary> Measures a write-then-read round trip through the built-in LocalMachine cache, verifying the value survives the trip. </summary>
    /// <exception cref="InvalidOperationException">A value read back did not match the value written.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("LocalMachine")]
    public async Task LocalMachineOperations()
    {
        var localCache = CacheDatabase.LocalMachine;

        for (var i = 0; i < BenchmarkSize; i++)
        {
            var testData = _testObjects[i % _testObjects.Count];
            var key = $"local_data_{i}";

            await localCache.InsertObject(key, testData);
            var retrieved = await localCache.GetObject<TestDataV11>(key);

            // Verify data integrity
            if (retrieved.Id != testData.Id)
            {
                throw new InvalidOperationException(DataIntegrityFailureMessage);
            }
        }
    }

    /// <summary> Measures a write-then-read round trip through the built-in Secure cache, so the encryption layer's cost shows up next to the plain caches. </summary>
    /// <exception cref="InvalidOperationException">A value read back did not match the value written.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Secure")]
    public async Task SecureOperations()
    {
        var secureCache = CacheDatabase.Secure;

        for (var i = 0; i < BenchmarkSize; i++)
        {
            var testData = _testObjects[i % _testObjects.Count];
            var key = $"secure_data_{i}";

            await secureCache.InsertObject(key, testData);
            var retrieved = await secureCache.GetObject<TestDataV11>(key);

            // Verify data integrity
            if (retrieved.Id != testData.Id)
            {
                throw new InvalidOperationException(DataIntegrityFailureMessage);
            }
        }
    }

    /// <summary> Measures a write-then-read round trip through the built-in InMemory cache, giving a storage-free baseline for the serializer cost. </summary>
    /// <exception cref="InvalidOperationException">A value read back did not match the value written.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("InMemory")]
    public async Task InMemoryOperations()
    {
        var memoryCache = CacheDatabase.InMemory;

        for (var i = 0; i < BenchmarkSize; i++)
        {
            var testData = _testObjects[i % _testObjects.Count];
            var key = $"memory_data_{i}";

            await memoryCache.InsertObject(key, testData);
            var retrieved = await memoryCache.GetObject<TestDataV11>(key);

            // Verify data integrity
            if (retrieved.Id != testData.Id)
            {
                throw new InvalidOperationException(DataIntegrityFailureMessage);
            }
        }
    }

    /// <summary> Measures an insert/read/update/read cycle spread across three different caches, so cross-cache contention shows up alongside the single-cache numbers. </summary>
    /// <exception cref="InvalidOperationException">An updated value did not read back with the update applied.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Mixed")]
    public async Task MixedOperations()
    {
        IBlobCache[] caches =
        [
            CacheDatabase.UserAccount,
            CacheDatabase.LocalMachine,
            CacheDatabase.InMemory
        ];

        for (var i = 0; i < BenchmarkSize; i++)
        {
            var cache = caches[i % caches.Length];
            var testData = _testObjects[i % _testObjects.Count];
            var key = $"mixed_data_{i}";

            // Insert
            await cache.InsertObject(key, testData);

            // Read
            var retrieved = await cache.GetObject<TestDataV11>(key);

            // Update
            retrieved.Value++;
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

    /// <summary> Measures the serializer alone on the SQLite-backed cache: every iteration writes and reads back a full object and checks both a value and a string field survived. </summary>
    /// <exception cref="InvalidOperationException">A value read back did not match the value written.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Serializer")]
    public async Task SerializerPerformance()
    {
        for (var i = 0; i < BenchmarkSize; i++)
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

    /// <summary> Measures the bulk object API: <see cref="BenchmarkSize"/> objects written in one call and read back in one call, so per-key overhead is amortized. </summary>
    /// <exception cref="InvalidOperationException">The bulk read returned a different number of entries than were written.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("BulkOperations")]
    public async Task BulkOperations()
    {
        Dictionary<string, TestDataV11> keyValuePairs = [];
        for (var i = 0; i < BenchmarkSize; i++)
        {
            keyValuePairs[$"bulk_test_{i}"] = _testObjects[i % _testObjects.Count];
        }

        // Bulk insert
        await BlobCache.InsertObjects(keyValuePairs);

        // Bulk get
        string[] keys = [.. keyValuePairs.Keys];
        var retrieved = await BlobCache.GetObjects<TestDataV11>(keys).ToList();

        // Verify bulk operations
        if (retrieved.Count == BenchmarkSize)
        {
            return;
        }

        throw new InvalidOperationException("Bulk operation integrity check failed");
    }
}
