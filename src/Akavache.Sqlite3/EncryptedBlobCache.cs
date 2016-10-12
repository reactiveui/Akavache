using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Akavache.Sqlite3
{
    public class EncryptedBlobCache : SqlitePersistentBlobCache, IObjectBlobCache, IBulkBlobCache, IObjectBulkBlobCache, ISecureBlobCache
    {
        public EncryptedBlobCache(string databaseFile, IScheduler scheduler = null) : base(databaseFile, scheduler)
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
            catch (Exception ex)
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
            catch (Exception ex)
            {
                return Observable.Throw<byte[]>(ex);
            }
        }
    }
}