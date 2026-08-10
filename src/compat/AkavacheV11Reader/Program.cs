// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Akavache;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;

namespace AkavacheV11Reader;

/// <summary>
/// Entry point for the compatibility reader. Opens the database the V10 writer produced and reports
/// whether Akavache 11 still reads every value version 10 stored.
/// </summary>
internal static class Program
{
    /// <summary>The application name both halves of the compatibility check initialise with.</summary>
    private const string ApplicationName = "AkavacheCompatTest";

    /// <summary>The database file name both halves of the compatibility check agree on.</summary>
    private const string DatabaseFileName = "akavache-test.db";

    /// <summary>Verifies the dataset, reporting each entry to standard output.</summary>
    /// <returns>Zero when every entry round-tripped; one when any did not.</returns>
    internal static async Task<int> Main()
    {
        var dbPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), DatabaseFileName));
        Report($"V11 Reader starting. DB path: {dbPath}");

        var instance = CacheDatabase.CreateBuilder(ApplicationName)
            .WithSerializer<SystemJsonSerializer>()
            .WithSqliteProvider()
            .WithSqliteDefaults()
            .Build();

        IReadOnlyList<VerificationResult> results;

        try
        {
            using SqliteBlobCache readerCache = new(dbPath, instance.Serializer!);
            results = await new V11CacheVerifier(readerCache).VerifyDatasetAsync();
        }
        finally
        {
            await CacheDatabase.Shutdown();
        }

        foreach (var result in results)
        {
            Report(result.ToString());
        }

        var allPassed = true;
        foreach (var result in results)
        {
            allPassed &= result.Passed;
        }

        Report(allPassed ? "Compatibility verified." : "Mismatch found.");

        return allPassed ? 0 : 1;
    }

    /// <summary>Writes one report line. Console output is this tool's product, not incidental logging.</summary>
    /// <param name="message">The line to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Report(string message) => Console.WriteLine(message);
}
