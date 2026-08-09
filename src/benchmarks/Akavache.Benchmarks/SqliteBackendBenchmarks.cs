// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Akavache.EncryptedSqlite3;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Akavache.Benchmarks;

/// <summary>
/// Focused single-operation benchmarks against the SQLite backends.
/// Targets the code paths that the SQLitePCLRaw rewrite (issue #1180) will touch:
/// single-key Get/Insert/Invalidate, bulk Get/Insert, GetAllKeys. Runs the
/// unencrypted <see cref="SqliteBlobCache"/> and encrypted
/// <see cref="EncryptedSqliteBlobCache"/> in parallel so the before/after
/// comparison captures both backends.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{BenchmarkSize}")]
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class SqliteBackendBenchmarks : IDisposable
{
    /// <summary>The password the encrypted backend's database is opened with.</summary>
    private const string EncryptionKey = "benchmark-password";

    /// <summary>The per-benchmark temp directory. Assigned by <see cref="GlobalSetup"/> before any benchmark runs.</summary>
    private string _tempDirectory = null!;

    /// <summary>The disposable handle that removes <see cref="_tempDirectory"/> when the benchmark finishes. Assigned by <see cref="GlobalSetup"/>.</summary>
    private IDisposable _directoryCleanup = null!;

    /// <summary>The unencrypted backend under measurement. Assigned by <see cref="GlobalSetup"/>.</summary>
    private SqliteBlobCache _plain = null!;

    /// <summary>The encrypted backend under measurement. Assigned by <see cref="GlobalSetup"/>.</summary>
    private EncryptedSqliteBlobCache _encrypted = null!;

    /// <summary>The keys both backends are pre-populated with. Assigned by <see cref="GlobalSetup"/>.</summary>
    private string[] _keys = null!;

    /// <summary>The payloads stored against <see cref="_keys"/>. Assigned by <see cref="GlobalSetup"/>.</summary>
    private byte[][] _values = null!;

    /// <summary>The same key/value set as a single dictionary, so the bulk benchmarks do not rebuild it per iteration. Assigned by <see cref="GlobalSetup"/>.</summary>
    private Dictionary<string, byte[]> _bulkPayload = null!;

    /// <summary>Tracks whether <see cref="Dispose(bool)"/> has already run.</summary>
    private int _disposedValue;

    /// <summary> Gets or sets the number of entries each backend is populated with and the number of operations each measured benchmark issues. </summary>
    /// <value>
    /// The number of entries each backend is populated with.
    /// </value>
    [Params(1, 100, 1000)]
    public int BenchmarkSize { get; set; }

    /// <summary> Creates both backends under a fresh temp directory and pre-populates them so the read and invalidate benchmarks have data to work on. </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _directoryCleanup = Utility.WithEmptyDirectory(out _tempDirectory);

        var serializer = new SystemJsonSerializer();
        _plain = new(Path.Combine(_tempDirectory, "bench-plain.db"), serializer);
        _encrypted = new(
            Path.Combine(_tempDirectory, "bench-encrypted.db"),
            EncryptionKey,
            serializer);

        _keys = new string[BenchmarkSize];
        _values = new byte[BenchmarkSize][];
        _bulkPayload = new(BenchmarkSize);
        for (var i = 0; i < BenchmarkSize; i++)
        {
            _keys[i] = $"bench_key_{i:D6}";
            _values[i] = PerfHelper.GenerateRandomBytes();
            _bulkPayload[_keys[i]] = _values[i];
        }

        // Pre-populate so the Read/Invalidate benchmarks have something to chew on.
        _plain.Insert(_bulkPayload).WaitForCompletion();
        _encrypted.Insert(_bulkPayload).WaitForCompletion();
    }

    /// <summary> Closes both backends and removes the temp directory their databases live in. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [GlobalCleanup]
    public void GlobalCleanup() => Dispose();

    /// <summary> Releases both backends and the temp directory they were created in. </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Measures <see cref="BenchmarkSize"/> single-key reads against the unencrypted backend. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Get_Plain")]
    public async Task Get_Plain()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            await _plain.Get(_keys[i]);
        }
    }

    /// <summary> Measures <see cref="BenchmarkSize"/> single-key reads against the encrypted backend, so the decryption cost per row is visible. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Get_Encrypted")]
    public async Task Get_Encrypted()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            await _encrypted.Get(_keys[i]);
        }
    }

    /// <summary> Measures reading every key from the unencrypted backend in a single bulk request. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("BulkGet_Plain")]
    public async Task BulkGet_Plain() =>
        await _plain.Get(_keys).ToList().FirstAsync();

    /// <summary> Measures reading every key from the encrypted backend in a single bulk request. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("BulkGet_Encrypted")]
    public async Task BulkGet_Encrypted() =>
        await _encrypted.Get(_keys).ToList().FirstAsync();

    /// <summary> Measures enumerating the whole key set of the unencrypted backend. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("GetAllKeys_Plain")]
    public async Task GetAllKeys_Plain() =>
        await _plain.GetAllKeys().ToList().FirstAsync();

    /// <summary> Measures enumerating the whole key set of the encrypted backend. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("GetAllKeys_Encrypted")]
    public async Task GetAllKeys_Encrypted() =>
        await _encrypted.GetAllKeys().ToList().FirstAsync();

    /// <summary> Measures <see cref="BenchmarkSize"/> single-key writes into the unencrypted backend, under keys that do not collide with the pre-populated set. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Insert_Plain")]
    public async Task Insert_Plain()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            await _plain.Insert($"ins_{_keys[i]}", _values[i]);
        }
    }

    /// <summary> Measures <see cref="BenchmarkSize"/> single-key writes into the encrypted backend, so the encryption cost per row is visible. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Insert_Encrypted")]
    public async Task Insert_Encrypted()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            await _encrypted.Insert($"ins_{_keys[i]}", _values[i]);
        }
    }

    /// <summary> Measures writing the whole key/value set into the unencrypted backend in a single bulk call. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("BulkInsert_Plain")]
    public async Task BulkInsert_Plain() => await _plain.Insert(_bulkPayload);

    /// <summary> Measures writing the whole key/value set into the encrypted backend in a single bulk call. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("BulkInsert_Encrypted")]
    public async Task BulkInsert_Encrypted() => await _encrypted.Insert(_bulkPayload);

    /// <summary> Measures deleting <see cref="BenchmarkSize"/> keys from the unencrypted backend, re-inserting them first so every iteration has the same amount to delete. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Invalidate_Plain")]
    public async Task Invalidate_Plain()
    {
        // Re-insert so the Invalidate path has something to delete on every iteration.
        await _plain.Insert(_bulkPayload);
        for (var i = 0; i < BenchmarkSize; i++)
        {
            await _plain.Invalidate(_keys[i]);
        }
    }

    /// <summary> Measures deleting <see cref="BenchmarkSize"/> keys from the encrypted backend, re-inserting them first so every iteration has the same amount to delete. </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    [BenchmarkCategory("Invalidate_Encrypted")]
    public async Task Invalidate_Encrypted()
    {
        await _encrypted.Insert(_bulkPayload);
        for (var i = 0; i < BenchmarkSize; i++)
        {
            await _encrypted.Invalidate(_keys[i]);
        }
    }

    /// <summary> Releases the backends and the temp directory. </summary>
    /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        // Claimed up front so a second caller returns immediately rather than racing the first
        // through the disposal below.
        if (Interlocked.Exchange(ref _disposedValue, 1) != 0)
        {
            return;
        }

        if (!disposing)
        {
            return;
        }

        _plain?.Dispose();
        _encrypted?.Dispose();
        _directoryCleanup?.Dispose();
    }
}
