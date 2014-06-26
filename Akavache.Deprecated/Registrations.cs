using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

#if UIKIT
using MonoTouch.Foundation;
#endif

#if APPKIT
using MonoMac.Foundation;
#endif

#if ANDROID
using Android.App;
#endif

namespace Akavache.Deprecated
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(IMutableDependencyResolver resolver)
        {
            if (ModeDetector.InUnitTestRunner()) return;

            var localCache = default(Lazy<IBlobCache>);
            var userAccount = default(Lazy<IBlobCache>);
            var secure = default(Lazy<ISecureBlobCache>);

            localCache = new Lazy<IBlobCache>(() =>
            {
                var fs = resolver.GetService<IFilesystemProvider>();
                return new CPersistentBlobCache(fs.GetDefaultLocalMachineCacheDirectory(), fs);
            });

            userAccount = new Lazy<IBlobCache>(() =>
            {
                var fs = resolver.GetService<IFilesystemProvider>();
                return new CPersistentBlobCache(fs.GetDefaultRoamingCacheDirectory(), fs);
            });

            secure = new Lazy<ISecureBlobCache>(() =>
            {
                var fs = resolver.GetService<IFilesystemProvider>();
                return new CEncryptedBlobCache(fs.GetDefaultRoamingCacheDirectory(), resolver.GetService<IEncryptionProvider>(), fs);
            });

            resolver.Register(() => localCache.Value, typeof(IBlobCache), "LocalMachine");
            resolver.Register(() => userAccount.Value, typeof(IBlobCache), "UserAccount");
            resolver.Register(() => secure.Value, typeof(ISecureBlobCache), null);
        }
    }
}
