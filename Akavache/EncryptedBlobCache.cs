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
        static readonly Lazy<ISecureBlobCache> _Current = new Lazy<ISecureBlobCache>(() => {
            var fs = default(IFilesystemProvider);
            try {
                fs = RxApp.DependencyResolver.GetService<IFilesystemProvider>();
            } catch (Exception ex) {
                LogHost.Default.DebugException("Couldn't find custom fs provider for secret cache", ex);
            }
            
#if SILVERLIGHT
            fs = fs ?? new IsolatedStorageProvider();
#else
            fs = fs ?? new SimpleFilesystemProvider();
#endif
            return new CEncryptedBlobCache(fs.GetDefaultSecretCacheDirectory(), fs);
        });

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
            public CEncryptedBlobCache(string cacheDirectory, IFilesystemProvider fsProvider) : base(cacheDirectory, fsProvider, RxApp.TaskpoolScheduler) { }
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

    }
}
