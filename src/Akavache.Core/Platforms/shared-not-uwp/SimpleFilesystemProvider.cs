using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using Splat;

namespace Akavache
{
    public class SimpleFilesystemProvider : IFilesystemProvider
    {
        /// <summary>
        /// Opens the file for read asynchronous.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
        {
            return Utility.SafeOpenFileAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read, scheduler);
        }

        /// <summary>
        /// Opens the file for write asynchronous.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
        {
            return Utility.SafeOpenFileAsync(path, FileMode.Create, FileAccess.Write, FileShare.None, scheduler);
        }

        /// <summary>
        /// Creates the recursive.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IObservable<Unit> CreateRecursive(string path)
        {
            Utility.CreateRecursive(new DirectoryInfo(path));
            return Observable.Return(Unit.Default);
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IObservable<Unit> Delete(string path)
        {
            return Observable.Start(() => File.Delete(path), BlobCache.TaskpoolScheduler);
        }

        /// <summary>
        /// Gets the default roaming cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultRoamingCacheDirectory()
        {
            return ModeDetector.InUnitTestRunner() ?
                Path.Combine(GetAssemblyDirectoryName(), "BlobCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BlobCache.ApplicationName, "BlobCache");
        }

        /// <summary>
        /// Gets the default secret cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultSecretCacheDirectory()
        {
            return ModeDetector.InUnitTestRunner() ?
                Path.Combine(GetAssemblyDirectoryName(), "SecretCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BlobCache.ApplicationName, "SecretCache");
        }

        /// <summary>
        /// Gets the default local machine cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultLocalMachineCacheDirectory()
        {
            return ModeDetector.InUnitTestRunner() ?
                Path.Combine(GetAssemblyDirectoryName(), "LocalBlobCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), BlobCache.ApplicationName, "BlobCache");
        }

        /// <summary>
        /// Gets the name of the assembly directory.
        /// </summary>
        /// <returns></returns>
        protected static string GetAssemblyDirectoryName()
        {
            var assemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.Assert(assemblyDirectoryName != null, "The directory name of the assembly location is null");
            return assemblyDirectoryName;
        }
    }
}
