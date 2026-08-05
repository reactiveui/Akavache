// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Loggers;

namespace Akavache.Benchmarks;

/// <summary> Helper utilities for the V11 benchmark fixtures — generates random keys and payloads to populate a cache before measuring read/write throughput. </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Security",
    "CA5394:Do not use insecure randomness",
    Justification = "Benchmark fixture data drawn inside the measured region. A cryptographic "
        + "generator costs enough that the benchmark would partly be measuring it, and this data "
        + "is fixture material rather than a secret.")]
internal static class PerfHelper
{
    /// <summary>
    /// Supplies the keys and payloads the fixtures insert before measuring. Deliberately
    /// <see cref="Random"/> rather than a cryptographic generator: these draws happen inside the
    /// measured region, and a cryptographic generator is costly enough that the benchmarks would
    /// partly be measuring it. The data is fixture material, not a secret.
    /// </summary>
    internal static readonly Random Rng = new(DataSeed);

    /// <summary>Fixed seed so a benchmark run generates the same keys and payloads every time.</summary>
    private const int DataSeed = 20_260_805;

    /// <summary>Number of entries generated and inserted per batch while seeding a cache, so a large target size never builds one enormous dictionary.</summary>
    private const int SeedBatchItemCount = 4096;

    /// <summary>Exclusive upper bound on the length, in bytes, of a generated random payload (and therefore of a generated key).</summary>
    private const int MaxPayloadByteCount = 256;

    /// <summary>The smallest cache population swept by the performance ranges.</summary>
    private const int TinyCacheItemCount = 1;

    /// <summary>A cache population small enough that per-operation overhead dominates.</summary>
    private const int SmallCacheItemCount = 10;

    /// <summary>A mid-sized cache population.</summary>
    private const int MediumCacheItemCount = 100;

    /// <summary>A cache population large enough for batching effects to show up.</summary>
    private const int LargeCacheItemCount = 1_000;

    /// <summary>A cache population where storage throughput dominates.</summary>
    private const int HugeCacheItemCount = 10_000;

    /// <summary>The largest cache population swept by the performance ranges.</summary>
    private const int MassiveCacheItemCount = 100_000;

    /// <summary> Tests generating a database. </summary>
    /// <param name="targetCache">The target blob cache.</param>
    /// <param name="size">The number of items to generate.</param>
    /// <returns>A list of generated items.</returns>
    internal static async Task<List<string>> GenerateDatabase(IBlobCache targetCache, int size)
    {
        List<string> ret = [];

        // Write out in batches of SeedBatchItemCount.
        while (size > 0)
        {
            var toWriteSize = Math.Min(SeedBatchItemCount, size);
            var toWrite = GenerateRandomDatabaseContents(toWriteSize);

            await targetCache.Insert(toWrite);

            ret.AddRange(toWrite.Keys);

            size -= toWrite.Count;
            ConsoleLogger.Default.WriteLine($"{size} items remaining");
        }

        return ret;
    }

    /// <summary> Generate the contents of the database. </summary>
    /// <param name="toWriteSize">The size of the database to write.</param>
    /// <returns>A dictionary of the contents.</returns>
    internal static Dictionary<string, byte[]> GenerateRandomDatabaseContents(int toWriteSize)
    {
        Dictionary<string, byte[]> contents = new(toWriteSize, StringComparer.Ordinal);

        for (var i = 0; i < toWriteSize; i++)
        {
            contents[GenerateRandomKey()] = GenerateRandomBytes();
        }

        return contents;
    }

    /// <summary> Generate random bytes for a value. </summary>
    /// <returns>The generated random bytes.</returns>
    internal static byte[] GenerateRandomBytes()
    {
        var ret = new byte[Rng.Next(1, MaxPayloadByteCount)];
        Rng.NextBytes(ret);
        return ret;
    }

    /// <summary> Generates a random key for the database. </summary>
    /// <returns>The random key.</returns>
    internal static string GenerateRandomKey()
    {
        var bytes = GenerateRandomBytes();

        // NB: Mask off the MSB and set bit 5 so we always end up with
        // valid UTF-8 characters that aren't control characters
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)((bytes[i] & 0x7F) | 0x20);
        }

        return Encoding.UTF8.GetString(bytes, 0, Math.Min(MaxPayloadByteCount, bytes.Length));
    }

    /// <summary> Gets a series of size values to use in generating performance tests. </summary>
    /// <returns>The range of sizes.</returns>
    internal static int[] GetPerfRanges() =>
    [
        TinyCacheItemCount,
        SmallCacheItemCount,
        MediumCacheItemCount,
        LargeCacheItemCount,
        HugeCacheItemCount,
        MassiveCacheItemCount,
    ];
}
