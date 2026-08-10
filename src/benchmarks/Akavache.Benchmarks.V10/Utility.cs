// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

namespace Akavache.Benchmarks.V10;

/// <summary> A set of utility helper methods for use throughout tests. </summary>
internal static class Utility
{
    /// <summary>How long to wait before retrying a file delete that lost a race with an antivirus or indexer holding the handle.</summary>
    private const int DeleteRetryDelayMilliseconds = 10;

    /// <summary> Deletes a directory. </summary>
    /// <param name="directoryPath">The path to delete.</param>
    internal static void DeleteDirectory(string directoryPath)
    {
        // From https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502
        try
        {
            DirectoryInfo di = new(directoryPath);
            var files = di.EnumerateFiles();
            var dirs = di.EnumerateDirectories();

            foreach (var file in files)
            {
                File.SetAttributes(file.FullName, FileAttributes.Normal);
                Retry(file.Delete);
            }

            foreach (var dir in dirs)
            {
                DeleteDirectory(dir.FullName);
            }

            File.SetAttributes(directoryPath, FileAttributes.Normal);
            Directory.Delete(directoryPath, false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("***** Failed to clean up!! *****");
            Console.Error.WriteLine(ex);
        }

        static void Retry(Action block, int retries = 2)
        {
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
                    Thread.Sleep(DeleteRetryDelayMilliseconds);
                }
            }
        }
    }

    /// <summary> Creates a fresh empty directory under the current working directory and returns a disposable that recursively deletes it on dispose. </summary>
    /// <param name="directoryPath">The full path of the created directory.</param>
    /// <returns>A disposable that cleans up the directory when disposed.</returns>
    internal static IDisposable WithEmptyDirectory(out string directoryPath)
    {
        DirectoryInfo di = new(Path.Combine(".", Guid.NewGuid().ToString()));
        if (di.Exists)
        {
            DeleteDirectory(di.FullName);
        }

        di.Create();

        directoryPath = di.FullName;
        return Disposable.Create(di.FullName, DeleteDirectory);
    }
}
