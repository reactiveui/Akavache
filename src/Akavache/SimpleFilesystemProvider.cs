using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Splat;
using System.Reflection;
using System.Diagnostics;

namespace Akavache
{
    public class SimpleFilesystemProvider : IFilesystemProvider
    {
        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
        {
            return Utility.SafeOpenFileAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read, scheduler);
        }

        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
        {
            return Utility.SafeOpenFileAsync(path, FileMode.Create, FileAccess.Write, FileShare.None, scheduler);
        }

        public IObservable<Unit> CreateRecursive(string path)
        {
            Utility.CreateRecursive(new DirectoryInfo(path));
            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> Delete(string path)
        {
            return Observable.Start(() => File.Delete(path), BlobCache.TaskpoolScheduler);
        }
                
        public string GetDefaultRoamingCacheDirectory()
        {
            return ModeDetector.InUnitTestRunner() ?
                Path.Combine(GetAssemblyDirectoryName(), "BlobCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BlobCache.ApplicationName, "BlobCache");
        }

        public string GetDefaultSecretCacheDirectory()
        {
            return ModeDetector.InUnitTestRunner() ?
                Path.Combine(GetAssemblyDirectoryName(), "SecretCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BlobCache.ApplicationName, "SecretCache");
        }

        public string GetDefaultLocalMachineCacheDirectory()
        {
            return ModeDetector.InUnitTestRunner() ?
                Path.Combine(GetAssemblyDirectoryName(), "LocalBlobCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), BlobCache.ApplicationName, "BlobCache");
        }

        protected static string GetAssemblyDirectoryName()
        {
            var assemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.Assert(assemblyDirectoryName != null, "The directory name of the assembly location is null");
            return assemblyDirectoryName;
        }
    }
}