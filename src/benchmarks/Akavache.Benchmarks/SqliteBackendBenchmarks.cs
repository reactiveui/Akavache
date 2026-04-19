// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class SqliteBackendBenchmarks
{
    private const string EncryptionKey = "benchmark-password";

    private string _tempDirectory = null!;
    private IDisposable _directoryCleanup = null!;
    private SqliteBlobCache _plain = null!;
    private EncryptedSqliteBlobCache _encrypted = null!;
    private string[] _keys = null!;
    private byte[][] _values = null!;
    private Dictionary<string, byte[]> _bulkPayload = null!;

    [Params(1, 100, 1000)]
    public int BenchmarkSize { get; set; }

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

        _keys = Enumerable.Range(0, BenchmarkSize).Select(i => $"bench_key_{i:D6}").ToArray();
        _values = Enumerable.Range(0, BenchmarkSize).Select(_ => PerfHelper.GenerateRandomBytes()).ToArray();
        _bulkPayload = new(BenchmarkSize);
        for (var i = 0; i < BenchmarkSize; i++)
        {
            _bulkPayload[_keys[i]] = _values[i];
        }

        // Pre-populate so the Read/Invalidate benchmarks have something to chew on.
        _plain.Insert(_bulkPayload).FirstAsync().GetAwaiter().GetResult();
        _encrypted.Insert(_bulkPayload).FirstAsync().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _plain?.Dispose();
        _encrypted?.Dispose();
        _directoryCleanup?.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory("Get_Plain")]
    public async Task Get_Plain()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            await _plain.Get(_keys[i]);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Get_Encrypted")]
    public async Task Get_Encrypted()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            await _encrypted.Get(_keys[i]);
        }
    }

    [Benchmark]
    [BenchmarkCategory("BulkGet_Plain")]
    public async Task BulkGet_Plain() =>
        await _plain.Get(_keys).ToList().FirstAsync();

    [Benchmark]
    [BenchmarkCategory("BulkGet_Encrypted")]
    public async Task BulkGet_Encrypted() =>
        await _encrypted.Get(_keys).ToList().FirstAsync();

    [Benchmark]
    [BenchmarkCategory("GetAllKeys_Plain")]
    public async Task GetAllKeys_Plain() =>
        await _plain.GetAllKeys().ToList().FirstAsync();

    [Benchmark]
    [BenchmarkCategory("GetAllKeys_Encrypted")]
    public async Task GetAllKeys_Encrypted() =>
        await _encrypted.GetAllKeys().ToList().FirstAsync();

    [Benchmark]
    [BenchmarkCategory("Insert_Plain")]
    public async Task Insert_Plain()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            await _plain.Insert("ins_" + _keys[i], _values[i]);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Insert_Encrypted")]
    public async Task Insert_Encrypted()
    {
        for (var i = 0; i < BenchmarkSize; i++)
        {
            await _encrypted.Insert("ins_" + _keys[i], _values[i]);
        }
    }

    [Benchmark]
    [BenchmarkCategory("BulkInsert_Plain")]
    public async Task BulkInsert_Plain() => await _plain.Insert(_bulkPayload);

    [Benchmark]
    [BenchmarkCategory("BulkInsert_Encrypted")]
    public async Task BulkInsert_Encrypted() => await _encrypted.Insert(_bulkPayload);

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
}
