using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Foundation;

namespace Akavache
{
    public class MacFilesystemProvider : IFilesystemProvider
    {
        private readonly SimpleFilesystemProvider _inner = new SimpleFilesystemProvider();

        /// <summary>
        /// Opens the file for read asynchronous.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
        {
            return _inner.OpenFileForReadAsync(path, scheduler);
        }

        /// <summary>
        /// Opens the file for write asynchronous.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
        {
            return _inner.OpenFileForWriteAsync(path, scheduler);
        }

        /// <summary>
        /// Creates the recursive.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IObservable<Unit> CreateRecursive(string path)
        {
            return _inner.CreateRecursive(path);
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IObservable<Unit> Delete(string path)
        {
            return _inner.Delete(path);
        }

        /// <summary>
        /// Gets the default local machine cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultLocalMachineCacheDirectory()
        {
            return CreateAppDirectory(NSSearchPathDirectory.CachesDirectory);
        }

        /// <summary>
        /// Gets the default roaming cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultRoamingCacheDirectory()
        {
            return CreateAppDirectory(NSSearchPathDirectory.ApplicationSupportDirectory);
        }

        /// <summary>
        /// Gets the default secret cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultSecretCacheDirectory()
        {
            return CreateAppDirectory(NSSearchPathDirectory.ApplicationSupportDirectory, "SecretCache");
        }

        /// <summary>
        /// Creates the application directory.
        /// </summary>
        /// <param name="targetDir">The target dir.</param>
        /// <param name="subDir">The sub dir.</param>
        /// <returns></returns>
        private string CreateAppDirectory(NSSearchPathDirectory targetDir, string subDir = "BlobCache")
        {
            var fm = new NSFileManager();
            var url = fm.GetUrl(targetDir, NSSearchPathDomain.All, null, true, out var err);
            var ret = Path.Combine(url.RelativePath, BlobCache.ApplicationName, subDir);
            if (!Directory.Exists(ret)) {
                _inner.CreateRecursive(ret).Wait();
            }

            return ret;
        }
    }
}
