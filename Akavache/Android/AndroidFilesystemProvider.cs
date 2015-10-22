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
        readonly SimpleFilesystemProvider _inner = new SimpleFilesystemProvider();

        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
        {
            return _inner.OpenFileForReadAsync(path, scheduler);
        }

        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
        {
            return _inner.OpenFileForWriteAsync(path, scheduler);
        }

        public IObservable<Unit> CreateRecursive(string path)
        {
            return _inner.CreateRecursive(path);
        }

        public IObservable<Unit> Delete(string path)
        {
            return _inner.Delete(path);
        }

        public string GetDefaultLocalMachineCacheDirectory()
        {
            return Application.Context.CacheDir.AbsolutePath;
        }

        public string GetDefaultRoamingCacheDirectory()
        {
            return Application.Context.FilesDir.AbsolutePath;
        }

        public string GetDefaultSecretCacheDirectory()
        {
            var path = Application.Context.FilesDir.AbsolutePath;
            var di = new DirectoryInfo(Path.Combine(path, "Secret"));
            if (!di.Exists) di.CreateRecursive();

            return di.FullName;
        }
    }
}

