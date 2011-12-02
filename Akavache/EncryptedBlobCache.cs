using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using ReactiveUI;

namespace Akavache
{
    public interface ISecureBlobCache : IBlobCache
    {
    }

    public class EncryptedBlobCache : PersistentBlobCache, ISecureBlobCache
    {
        static Lazy<ISecureBlobCache> _Current = new Lazy<ISecureBlobCache>(() => new EncryptedBlobCache());
        public static ISecureBlobCache Current
        {
            get { return _Current.Value; }
        }

        protected EncryptedBlobCache(IScheduler scheduler = null) : base(GetDefaultCacheDirectory(), scheduler)
        {
        }

        protected override IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data)
        {
            try
            {
                var ret = Observable.Return(DataProtectionApi.Encrypt(DataProtectionApi.KeyType.UserKey, data, null, null));

                // NB: MemoizedRequests will be null as we're disposing
                if (MemoizedRequests != null)
                {
                    lock(MemoizedRequests) MemoizedRequests.InvalidateAll();
                }
                return ret;
            } 
            catch(Exception ex)
            {
                return Observable.Throw<byte[]>(ex);
            }
            
        }

        protected override IObservable<byte[]> AfterReadFromDiskFilter(byte[] data)
        {
            try
            {
                string dontcare;
                var ret = Observable.Return(DataProtectionApi.Decrypt(data, null, out dontcare));

                // NB: MemoizedRequests will be null as we're disposing
                if (MemoizedRequests != null)
                {
                    lock(MemoizedRequests) MemoizedRequests.InvalidateAll();
                }
                return ret;
            } 
            catch(Exception ex)
            {
                return Observable.Throw<byte[]>(ex);
            }
        }

        static string GetDefaultCacheDirectory()
        {
            return RxApp.InUnitTestRunner() ?
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SecretCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GitHub", "SecretCache");
        }
    }
}