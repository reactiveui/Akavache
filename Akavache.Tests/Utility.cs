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

            string[] files = Directory.GetFiles(directoryPath);
            string[] dirs = Directory.GetDirectories(directoryPath);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                (new Action(() => File.Delete(Path.Combine(directoryPath, file)))).Retry();
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(Path.Combine(directoryPath, dir));
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

        public static void Retry(this Action block, int retries = 3)
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
                    retries--;
                    if (retries == 0)
                    {
                        Thread.Sleep(10);
                        throw;
                    }
                }
            }
        }
    }
}
