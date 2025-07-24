// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;

namespace ReactiveMarbles.CacheDatabase.Benchmarks
{
    /// <summary>
    /// A set of utility helper methods for use throughout tests.
    /// </summary>
    internal static class Utility
    {
        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="directoryPath">The path to delete.</param>
        public static void DeleteDirectory(string directoryPath)
        {
            // From https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502
            try
            {
                var di = new DirectoryInfo(directoryPath);
                var files = di.EnumerateFiles();
                var dirs = di.EnumerateDirectories();

                foreach (var file in files)
                {
                    File.SetAttributes(file.FullName, FileAttributes.Normal);
                    new Action(() => file.Delete()).Retry();
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
        }

        public static IDisposable WithEmptyDirectory(out string directoryPath)
        {
            var di = new DirectoryInfo(Path.Combine(".", Guid.NewGuid().ToString()));
            if (di.Exists)
            {
                DeleteDirectory(di.FullName);
            }

            di.Create();

            directoryPath = di.FullName;
            return Disposable.Create(() =>
            {
                DeleteDirectory(di.FullName);
            });
        }

        public static void Retry(this Action block, int retries = 2)
        {
            while (true)
            {
                try
                {
                    block();
                    return;
                }
                catch (Exception)
                {
                    if (retries == 0)
                    {
                        throw;
                    }

                    retries--;
                    Thread.Sleep(10);
                }
            }
        }
    }
}
