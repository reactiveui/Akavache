// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.Helpers;
#else
namespace Akavache.Tests.Helpers;
#endif

/// <summary>A set of utility helper methods for use throughout tests.</summary>
internal static class Utility
{
    /// <summary>How many times a single file delete is retried while a handle is still held.</summary>
    private const int FileDeleteRetries = 20;

    /// <summary>How many times the directory delete itself is retried once its contents are gone.</summary>
    private const int DirectoryDeleteRetries = 2;

    /// <summary>Base pause between delete retries, in milliseconds; doubled on each successive attempt.</summary>
    private const int DeleteRetryBaseDelayMilliseconds = 250;

    /// <summary>Pause before the final directory delete, giving the OS time to release file handles.</summary>
    private const int HandleReleaseDelayMilliseconds = 150;

    /// <summary>The highest number of times the retry backoff doubles the base delay.</summary>
    private const int MaxBackoffDoublings = 4;

    /// <summary>Upper bound on any single retry pause, in milliseconds.</summary>
    private const int MaxRetryDelayMilliseconds = 2000;

    /// <summary>Root temp directory used to host per-test scratch folders.</summary>
    private static readonly string TempRoot = Path.Combine(Path.GetTempPath(), "AkavacheTests");

    /// <summary>Deletes a directory.</summary>
    /// <param name="directoryPath">The path to delete.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "PSH1021:Do not force a garbage collection",
        Justification = "A cache a test left undisposed releases its SQLite file handle from a finalizer, so the directory stays locked until one runs. Retrying alone just burns the attempts.")]
    internal static void DeleteDirectory(string directoryPath)
    {
        // From https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            DirectoryInfo di = new(directoryPath);
            var files = di.EnumerateFiles();
            var dirs = di.EnumerateDirectories();

            foreach (var file in files)
            {
                ClearReadOnly(file.FullName);

                // Retry deleting single file multiple times, allowing time for file handles to release
                Retry(file.Delete, FileDeleteRetries, DeleteRetryBaseDelayMilliseconds);
            }

            foreach (var dir in dirs)
            {
                DeleteDirectory(dir.FullName);
            }

            ClearReadOnly(directoryPath);

            // A cache the test did not dispose still owns its SQLite handle, and that handle is
            // only released when the finalizer runs. Windows keeps the .db locked until then, so
            // the delete below fails however many times it is retried. Force the collection, then
            // give the released handles a moment to settle.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(HandleReleaseDelayMilliseconds);
            Retry(() => Directory.Delete(directoryPath, false), DirectoryDeleteRetries, DeleteRetryBaseDelayMilliseconds);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("***** Failed to clean up!! *****");
            Console.Error.WriteLine(ex);
        }
    }

    /// <summary>Creates a fresh empty directory under the test temp root and returns a disposable that deletes it.</summary>
    /// <param name="directoryPath">The path of the created directory.</param>
    /// <returns>A disposable that cleans up the directory on dispose.</returns>
    internal static IDisposable WithEmptyDirectory(out string directoryPath)
    {
        try
        {
            _ = Directory.CreateDirectory(TempRoot);
        }
        catch (IOException)
        {
            // A parallel test assembly is creating the same root, or the volume is momentarily busy.
            // The per-test directory create below is the operation that has to succeed.
        }
        catch (UnauthorizedAccessException)
        {
            // The root already exists and is owned by another user; the per-test create below reports any real problem.
        }

        DirectoryInfo di = new(Path.Combine(TempRoot, Guid.NewGuid().ToString()));
        if (di.Exists)
        {
            DeleteDirectory(di.FullName);
        }

        di.Create();

        directoryPath = di.FullName;
        return Scope.Create(directoryPath, DeleteDirectory);
    }

    /// <summary>Clears the read-only attribute so an entry can be deleted.</summary>
    /// <param name="path">The file or directory path.</param>
    private static void ClearReadOnly(string path)
    {
        try
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
        catch (IOException)
        {
            // The entry vanished or is locked by another process; the delete that follows surfaces the real problem.
        }
        catch (UnauthorizedAccessException)
        {
            // Attribute changes are denied for this entry; attempt the delete regardless.
        }
    }

    /// <summary>Runs an action, retrying with exponential backoff while attempts remain.</summary>
    /// <param name="block">The action to run.</param>
    /// <param name="retries">The number of retries allowed after the first attempt.</param>
    /// <param name="sleepMs">The base pause between attempts, in milliseconds.</param>
    private static void Retry(Action block, int retries, int sleepMs)
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
                var delay = Math.Min(sleepMs * (1 << Math.Min(attempt, MaxBackoffDoublings)), MaxRetryDelayMilliseconds);
                Thread.Sleep(delay);
            }
        }
    }
}
