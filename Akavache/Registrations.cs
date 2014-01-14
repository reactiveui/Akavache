using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace Akavache
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(Action<Func<object>, Type, string> registerFunction)
        {
#if SILVERLIGHT || XAMARIN_MOBILE
            var fs = new IsolatedStorageProvider();
#else
            var fs = new SimpleFilesystemProvider();
#endif
            registerFunction(() => fs, typeof(IFilesystemProvider), null);

            var localCache = default(Lazy<IBlobCache>);
            var userAccount = default(Lazy<IBlobCache>);
            var secure = default(Lazy<ISecureBlobCache>);

            if (!RxApp.InUnitTestRunner()) {
                localCache = new Lazy<IBlobCache>(() =>
                    new CPersistentBlobCache(fs.GetDefaultLocalMachineCacheDirectory(), fs));

                userAccount = new Lazy<IBlobCache>(() =>
                    new CPersistentBlobCache(fs.GetDefaultRoamingCacheDirectory(), fs));

                secure = new Lazy<ISecureBlobCache>(() =>
                    new CEncryptedBlobCache(fs.GetDefaultRoamingCacheDirectory(), fs));
            } else {
                localCache = new Lazy<IBlobCache>(() => new TestBlobCache());
                userAccount = new Lazy<IBlobCache>(() => new TestBlobCache());
                secure = new Lazy<ISecureBlobCache>(() => new TestBlobCache());
            }

            registerFunction(() => localCache.Value, typeof(IBlobCache), "LocalMachine");
            registerFunction(() => userAccount.Value, typeof(IBlobCache), "UserAccount");
            registerFunction(() => secure.Value, typeof(ISecureBlobCache), null);

            registerFunction(() => new AkavacheHttpMixin(), typeof(IAkavacheHttpMixin), null);
        }
    }
}
