using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using Akavache;

namespace Akavache.Tests
{
    static class Utility
    {
        public static void DeleteDirectory(string directoryPath)
        {
            // From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502

            var di = new DirectoryInfo(directoryPath);
            var files = di.EnumerateFiles();
            var dirs = di.EnumerateDirectories();

            foreach (var file in files)
            {
                File.SetAttributes(file.FullName, FileAttributes.Normal);
                (new Action(() => file.Delete())).Retry();
            }

            foreach (var dir in dirs)
            {
                DeleteDirectory(dir.FullName);
            }

            File.SetAttributes(directoryPath, FileAttributes.Normal);
            Directory.Delete(directoryPath, false);
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
            return Disposable.Create(() => DeleteDirectory(di.FullName));
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
