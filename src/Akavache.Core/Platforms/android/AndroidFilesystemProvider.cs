using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using Akavache;
using Android.App;
using Android.Content;

namespace Akavache
{
    public class AndroidFilesystemProvider : IFilesystemProvider
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
            return Application.Context.CacheDir.AbsolutePath;
        }

        /// <summary>
        /// Gets the default roaming cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultRoamingCacheDirectory()
        {
            return Application.Context.FilesDir.AbsolutePath;
        }

        /// <summary>
        /// Gets the default secret cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultSecretCacheDirectory()
        {
            var path = Application.Context.FilesDir.AbsolutePath;
            var di = new DirectoryInfo(Path.Combine(path, "Secret"));
            if (!di.Exists) {
                di.CreateRecursive();
            }

            return di.FullName;
        }
    }
}
