using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive;
using System.Reactive.Linq;

#if UNIFIED
using Foundation;
#elif UIKIT
using MonoTouch.Foundation;
#else
using MonoMac.Foundation;
#endif

namespace Akavache
{
    public class MacFilesystemProvider : IFilesystemProvider
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
            return CreateAppDirectory(NSSearchPathDirectory.CachesDirectory);
        }

        public string GetDefaultRoamingCacheDirectory()
        {
            return CreateAppDirectory(NSSearchPathDirectory.ApplicationSupportDirectory);
        }

        public string GetDefaultSecretCacheDirectory()
        {
            return CreateAppDirectory(NSSearchPathDirectory.ApplicationSupportDirectory, "SecretCache");
        }

        string CreateAppDirectory(NSSearchPathDirectory targetDir, string subDir = "BlobCache")
        {
            NSError err;

            var fm = new NSFileManager();
            var url = fm.GetUrl(targetDir, NSSearchPathDomain.All, null, true, out err);
            var ret = Path.Combine(url.RelativePath, BlobCache.ApplicationName, subDir);
            if (!Directory.Exists(ret)) _inner.CreateRecursive(ret).Wait();

            return ret;
        }
    }
}

