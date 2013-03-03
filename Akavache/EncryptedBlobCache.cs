using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Security.Cryptography;
using ReactiveUI;

namespace Akavache
{
    public abstract class EncryptedBlobCache : PersistentBlobCache, ISecureBlobCache
    {
        static readonly Lazy<ISecureBlobCache> _Current = new Lazy<ISecureBlobCache>(() => new CEncryptedBlobCache(GetDefaultCacheDirectory()));
        public static ISecureBlobCache Current
        {
            get { return _Current.Value; }
        }

        protected EncryptedBlobCache(
            string cacheDirectory = null, 
            IFilesystemProvider filesystemProvider = null, 
            IScheduler scheduler = null,
            Action<AsyncSubject<byte[]>> invalidatedCallback = null) 
            : base(cacheDirectory, filesystemProvider, scheduler, invalidatedCallback)
        {
        }

        class CEncryptedBlobCache : EncryptedBlobCache {
#if SILVERLIGHT
            public CEncryptedBlobCache(string cacheDirectory) : base(cacheDirectory, new IsolatedStorageProvider(), RxApp.TaskpoolScheduler) { }
#else
            public CEncryptedBlobCache(string cacheDirectory) : base(cacheDirectory, null, RxApp.TaskpoolScheduler) { }
#endif
        }

        protected override IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data, IScheduler scheduler)
        {
            try
            {
#if SILVERLIGHT
                var ret = Observable.Return(ProtectedData.Protect(data, null));
#else
                var ret = Observable.Return(ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser));
#endif
                return ret;
            } 
            catch(Exception ex)
            {
                return Observable.Throw<byte[]>(ex);
            }
            
        }

        protected override IObservable<byte[]> AfterReadFromDiskFilter(byte[] data, IScheduler scheduler)
        {
            try
            {
#if SILVERLIGHT
                var ret = Observable.Return(ProtectedData.Unprotect(data, null));
#else
                var ret = Observable.Return(ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser));
#endif
                return ret;
            } 
            catch(Exception ex)
            {
                return Observable.Throw<byte[]>(ex);
            }
        }

        protected static string GetDefaultCacheDirectory()
        {
#if SILVERLIGHT
            return "SecretCache";
#else
            return RxApp.InUnitTestRunner() ?
                Path.Combine(GetAssemblyDirectoryName(), "SecretCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BlobCache.ApplicationName, "SecretCache");
#endif
        }

        
    }
}