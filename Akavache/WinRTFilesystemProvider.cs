using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Windows.Foundation;
using ReactiveUI;
using Windows.Foundation;
using Windows.Storage;

namespace Akavache
{
    public class WinRTFileSystemProvider : IFileSystemProvider, IEnableLogger
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

        public IObservable<byte[]> ReadFileToBytesAsync(string path, IScheduler scheduler)
        {
            return Observable.Zip(
                SafeOpenFileAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read),
                Observable.Return(new MemoryStream()),
                (file, memoryStream) => new {file, memoryStream})
            .SelectMany(x => x.file.CopyToAsync(x.memoryStream, scheduler)
            .Select(_ => x.memoryStream.ToArray()));
        }

        public IObservable<byte[]> WriteBytesToFileAsync(string path, byte[] data, IScheduler scheduler)
        {
            return Observable.Zip(
                Observable.Return(new MemoryStream(data)),
                SafeOpenFileAsync(path, FileMode.Create, FileAccess.Write, FileShare.Read),
                (bytes, file) => new { bytes, file })
            .SelectMany(x => x.bytes.CopyToAsync(x.file, scheduler)
            .Select(_ => data));
        }

        public IObservable<Unit> CreateRecursiveAsync(string path)
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

        public IObservable<Unit> DeleteFileAsync(string path)
        {
            return StorageFile.GetFileFromPathAsync(path).ToObservable()
                .SelectMany(x => x.DeleteAsync().ToObservable());
        }

        IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share)
        {
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);

            return StorageFolder.GetFolderFromPathAsync(folder).ToObservable()
                .SelectMany(x => openFileStrategies[mode](x, name).ToObservable())
                .SelectMany(x => access == FileAccess.Read ?
                    x.OpenStreamForReadAsync().ToObservable() :
                    x.OpenStreamForWriteAsync().ToObservable());
        }

    }
}