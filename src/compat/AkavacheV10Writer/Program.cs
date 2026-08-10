// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Akavache;
using Akavache.Sqlite3;

namespace AkavacheV10Writer;

/// <summary>
/// Entry point for the compatibility writer. Produces an Akavache 10 database at a known path so
/// the V11 reader can prove it still reads what version 10 wrote.
/// </summary>
internal static class Program
{
    /// <summary>The application name both halves of the compatibility check initialise with.</summary>
    private const string ApplicationName = "AkavacheCompatTest";

    /// <summary>The database file name both halves of the compatibility check agree on.</summary>
    private const string DatabaseFileName = "akavache-test.db";

    /// <summary>Writes the dataset, reporting progress to standard output.</summary>
    /// <returns>Zero when the dataset was written; one when it was not.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the database path has no parent directory to create.</exception>
    internal static async Task<int> Main()
    {
        var dbPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), DatabaseFileName));
        Report($"V10 Writer starting. DB path: {dbPath}");

        var directory = Path.GetDirectoryName(dbPath)
            ?? throw new InvalidOperationException($"Database path '{dbPath}' has no parent directory.");

        _ = Directory.CreateDirectory(directory);

        BlobCache.ApplicationName = ApplicationName;
        Akavache.Sqlite3.Registrations.Start(ApplicationName, static () => { });

        using SqlRawPersistentBlobCache cache = new(dbPath);
        var exitCode = 0;

        try
        {
            new V10CacheWriter(cache, Report).WriteDataset();
        }
        catch (Exception ex)
        {
            Report($"ERROR during inserts: {ex}");
            exitCode = 1;
        }
        finally
        {
            await ShutdownAsync();
        }

        Report("V10 Writer completed.");
        return exitCode;
    }

    /// <summary>Shuts Akavache 10 down, reporting rather than rethrowing a teardown failure.</summary>
    /// <returns>A task that completes once shutdown has been attempted.</returns>
    private static async Task ShutdownAsync()
    {
        try
        {
            await BlobCache.Shutdown();
        }
        catch (Exception ex)
        {
            Report($"Shutdown error: {ex}");
        }
    }

    /// <summary>Writes one progress line. Console output is this tool's product, not incidental logging.</summary>
    /// <param name="message">The line to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Report(string message) => Console.WriteLine(message);
}
