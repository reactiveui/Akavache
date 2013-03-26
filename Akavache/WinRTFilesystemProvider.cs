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
using ReactiveUI;
using Windows.Foundation;
using Windows.Storage;

namespace Akavache
{
    public class SimpleFilesystemProvider : IFilesystemProvider, IEnableLogger
    {
        readonly IDictionary<FileMode, Func<StorageFolder, string, IAsyncOperation<StorageFile>>> openFileStrategies
            = new Dictionary<FileMode, Func<StorageFolder, string, IAsyncOperation<StorageFile>>>
        {
            { FileMode.Create, (f,name) => f.CreateFileAsync(name) },
            { FileMode.CreateNew, (f,name) => f.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting) },
            { FileMode.Open, (f,name) => f.GetFileAsync(name) },
            { FileMode.OpenOrCreate, (f,name) => f.CreateFileAsync(name, CreationCollisionOption.OpenIfExists) },
            { FileMode.Truncate, (f,name) => null }, // ???
            { FileMode.Append, (f,name) => null } // ???
        };

        public IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share, IScheduler scheduler)
        {
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);

            return StorageFolder.GetFolderFromPathAsync(folder).ToObservable()
                .SelectMany(x => openFileStrategies[mode](x, name).ToObservable())
                .SelectMany(x => access == FileAccess.Read ?
                    x.OpenStreamForReadAsync().ToObservable() :
                    x.OpenStreamForWriteAsync().ToObservable());
        }

        public IObservable<Unit> CreateRecursive(string path)
        {
            var paths = path.Split('\\');

            var firstFolderThatExists = Observable.Range(0, paths.Length - 1)
                .Select(x =>
                    StorageFolder.GetFolderFromPathAsync(String.Join("\\", paths.Take(paths.Length - x)))
                    .ToObservable()
                    .LoggedCatch(this, Observable.Empty<StorageFolder>()))
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
