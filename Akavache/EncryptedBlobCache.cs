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
        protected EncryptedBlobCache(
            string cacheDirectory = null, 
            IFilesystemProvider filesystemProvider = null, 
            IScheduler scheduler = null,
            Action<AsyncSubject<byte[]>> invalidatedCallback = null) 
            : base(cacheDirectory, filesystemProvider, scheduler, invalidatedCallback)
        {
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

    class CEncryptedBlobCache : EncryptedBlobCache {
        public CEncryptedBlobCache(string cacheDirectory, IFilesystemProvider fsProvider) : base(cacheDirectory, fsProvider, RxApp.TaskpoolScheduler) { }
    }
}
