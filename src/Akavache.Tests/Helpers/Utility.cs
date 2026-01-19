// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

namespace Akavache.Tests.Helpers;

/// <summary>
/// A set of utility helper methods for use throughout tests.
/// </summary>
internal static class Utility
{
    private static readonly string TempRoot = Path.Combine(Path.GetTempPath(), "AkavacheTests");

    /// <summary>
    /// Deletes a directory.
    /// </summary>
    /// <param name="directoryPath">The path to delete.</param>
    public static void DeleteDirectory(string directoryPath)
    {
        // From https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            var di = new DirectoryInfo(directoryPath);
            var files = di.EnumerateFiles();
            var dirs = di.EnumerateDirectories();

            foreach (var file in files)
            {
                try
                {
                    File.SetAttributes(file.FullName, FileAttributes.Normal);
                }
                catch
                {
                }

                // Retry deleting single file multiple times, allowing time for file handles to release
                new Action(() => file.Delete()).Retry(20, 250);
            }

            foreach (var dir in dirs)
            {
                DeleteDirectory(dir.FullName);
            }

            try
            {
                File.SetAttributes(directoryPath, FileAttributes.Normal);
            }
            catch
            {
            }

            // Encourage GC/finalizers to release file handles before final directory delete (Windows)
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Add delay before final directory delete to allow file handles to release
            Thread.Sleep(150);
            Directory.Delete(directoryPath, false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("***** Failed to clean up!! *****");
            Console.Error.WriteLine(ex);
        }
    }

    public static IDisposable WithEmptyDirectory(out string directoryPath)
    {
        try
        {
            Directory.CreateDirectory(TempRoot);
        }
        catch
        {
        }

        var di = new DirectoryInfo(Path.Combine(TempRoot, Guid.NewGuid().ToString()));
        if (di.Exists)
        {
            DeleteDirectory(di.FullName);
        }

        di.Create();

        directoryPath = di.FullName;
        return Disposable.Create(() => DeleteDirectory(di.FullName));
    }

    public static void Retry(this Action block, int retries = 2, int sleepMs = 500)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                block();
                return;
            }
            catch (Exception) when (retries != 0)
            {
                retries--;
                attempt++;

                // exponential backoff within reason
                var delay = Math.Min(sleepMs * (1 << Math.Min(attempt, 4)), 2000);
                Thread.Sleep(delay);
            }
        }
    }
}
