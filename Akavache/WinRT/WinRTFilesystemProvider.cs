using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Windows.Foundation;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Splat;

namespace Akavache
{
    public class WinRTFilesystemProvider : IFilesystemProvider, IEnableLogger
    {
        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
        {
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);

            return StorageFolder.GetFolderFromPathAsync(folder).ToObservable()
                .SelectMany(x => x.GetFileAsync(name).ToObservable())
                .SelectMany(x => x.OpenStreamForReadAsync().ToObservable());
        }

        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
        {
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);

            return StorageFolder.GetFolderFromPathAsync(folder).ToObservable()
                .SelectMany(x => x.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting).ToObservable())
                .SelectMany(x => x.OpenStreamForWriteAsync().ToObservable());
        }

        public IObservable<Unit> CreateRecursive(string path)
        {
            var paths = path.Split('\\');

            var firstFolderThatExists = Observable.Range(0, paths.Length - 1)
                .Select(x =>
                    StorageFolder.GetFolderFromPathAsync(String.Join("\\", paths.Take(paths.Length - x)))
                    .ToObservable()
                    .Catch(Observable.Empty<StorageFolder>()))
                .Concat()
                .Take(1);

            return firstFolderThatExists
                .Select(x =>
                {
                    if (x.Path == path) return null;
                    return new { Root = x, Paths = path.Replace(x.Path + "\\", "").Split('\\')};
                })
                .SelectMany(x =>
                {
                    if (x == null) return Observable.Return(default(StorageFolder));
                    return x.Paths.ToObservable().Aggregate(x.Root, (acc, y) => acc.CreateFolderAsync(y).ToObservable().First());
                })
                .Select(_ => Unit.Default);
        }

        public IObservable<Unit> Delete(string path)
        {
            return StorageFile.GetFileFromPathAsync(path).ToObservable()
                .SelectMany(x => x.DeleteAsync().ToObservable());
        }

        public string GetDefaultRoamingCacheDirectory()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "BlobCache");
        }

        public string GetDefaultLocalMachineCacheDirectory()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "BlobCache");
        }

	    public string GetDefaultSecretCacheDirectory()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "SecretCache");
        }
    }
}
