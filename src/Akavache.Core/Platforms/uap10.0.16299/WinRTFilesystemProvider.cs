using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Splat;
using Windows.Foundation;
using Windows.Storage;

namespace Akavache
{
    public class WinRTFilesystemProvider : IFilesystemProvider, IEnableLogger
    {
        /// <summary>
        /// Opens the file for read asynchronous.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
        {
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);

            return StorageFolder.GetFolderFromPathAsync(folder).ToObservable()
                .SelectMany(x => x.GetFileAsync(name).ToObservable())
                .SelectMany(x => x.OpenStreamForReadAsync().ToObservable());
        }

        /// <summary>
        /// Opens the file for write asynchronous.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
        {
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);

            return StorageFolder.GetFolderFromPathAsync(folder).ToObservable()
                .SelectMany(x => x.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting).ToObservable())
                .SelectMany(x => x.OpenStreamForWriteAsync().ToObservable());
        }

        /// <summary>
        /// Creates the recursive.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IObservable<Unit> CreateRecursive(string path)
        {
            var paths = path.Split('\\');

            var firstFolderThatExists = Observable.Range(0, paths.Length - 1)
                .Select(x =>
                    StorageFolder.GetFolderFromPathAsync(string.Join("\\", paths.Take(paths.Length - x)))
                    .ToObservable()
                    .Catch(Observable.Empty<StorageFolder>()))
                .Concat()
                .Take(1);

            return firstFolderThatExists
                .Select(x => {
                    if (x.Path == path) {
                        return null;
                    }

                    return new { Root = x, Paths = path.Replace(x.Path + "\\", "").Split('\\') };
                })
                .SelectMany(x => {
                    if (x == null) {
                        return Observable.Return(default(StorageFolder));
                    }

                    return x.Paths.ToObservable().Aggregate(x.Root, (acc, y) => acc.CreateFolderAsync(y).ToObservable().First());
                })
                .Select(_ => Unit.Default);
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IObservable<Unit> Delete(string path)
        {
            return StorageFile.GetFileFromPathAsync(path).ToObservable()
                .SelectMany(x => x.DeleteAsync().ToObservable());
        }

        /// <summary>
        /// Gets the default roaming cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultRoamingCacheDirectory()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "BlobCache");
        }

        /// <summary>
        /// Gets the default local machine cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultLocalMachineCacheDirectory()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "BlobCache");
        }

        /// <summary>
        /// Gets the default secret cache directory.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultSecretCacheDirectory()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "SecretCache");
        }
    }
}
