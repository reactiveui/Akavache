// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Akavache.Tests.Performance;

/// <summary>
/// Performance write tests.
/// </summary>
public abstract class WriteTests
{
    /// <summary>
    /// Do write tests for sequential simple reads.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public Task SequentialSimpleWrites() =>
        GeneratePerfRangesForBlock(async (cache, size) =>
        {
            var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

            var st = new Stopwatch();
            st.Start();

            foreach (var kvp in toWrite)
            {
                await cache.Insert(kvp.Key, kvp.Value);
            }

            st.Stop();
            return st.ElapsedMilliseconds;
        });

    /// <summary>
    /// Do write tests for sequential bulk writes.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public Task SequentialBulkWrites() =>
        GeneratePerfRangesForBlock(async (cache, size) =>
        {
            var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

            var st = new Stopwatch();
            st.Start();

            await cache.Insert(toWrite);

            st.Stop();
            return st.ElapsedMilliseconds;
        });

    /// <summary>
    /// Do write tests for parallel simple writes.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public Task ParallelSimpleWrites() =>
        GeneratePerfRangesForBlock(async (cache, size) =>
        {
            var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

            var st = new Stopwatch();
            st.Start();

            await toWrite.ToObservable(BlobCache.TaskpoolScheduler)
                .Select(x => Observable.Defer(() => cache.Insert(x.Key, x.Value)))
                .Merge(32)
                .ToArray();

            st.Stop();
            return st.ElapsedMilliseconds;
        });

    /// <summary>
    /// Generates the blob cache we want to do the performance tests against.
    /// </summary>
    /// <param name="path">The path to the cache.</param>
    /// <returns>The blob cache.</returns>
    protected abstract IBlobCache CreateBlobCache(string path);

    private async Task GeneratePerfRangesForBlock(Func<IBlobCache, int, Task<long>> block)
    {
        var results = new Dictionary<int, long>();
        var dbName = default(string);

        using (Utility.WithEmptyDirectory(out var dirPath))
        using (var cache = CreateBlobCache(dirPath))
        {
            dbName = cache.GetType().Name;

            foreach (var size in PerfHelper.GetPerfRanges())
            {
                results[size] = await block(cache, size).ConfigureAwait(false);
            }
        }

        Console.WriteLine(dbName);
        foreach (var kvp in results)
        {
            Console.WriteLine("{0}: {1}", kvp.Key, kvp.Value);
        }
    }
}
