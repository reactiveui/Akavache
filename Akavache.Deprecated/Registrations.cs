using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

            // NB: We want the most recently registered fs, since there really 
            // only should be one 
            var fs = Locator.Current.GetService<IFilesystemProvider>();
            if (fs == null)
            {
                throw new Exception("Failed to initialize Akavache properly. Do you have a reference to Akavache.dll?");
            }

            var localCache = new Lazy<IBlobCache>(() => {
                fs.CreateRecursive(fs.GetDefaultLocalMachineCacheDirectory()).Wait();
                return new PersistentBlobCache(fs.GetDefaultLocalMachineCacheDirectory(), fs, BlobCache.TaskpoolScheduler);
            });
            resolver.Register(() => localCache.Value, typeof(IBlobCache), "LocalMachine");

            var userAccount = new Lazy<IBlobCache>(() =>
            {
                fs.CreateRecursive(fs.GetDefaultRoamingCacheDirectory()).Wait();
                return new PersistentBlobCache(fs.GetDefaultRoamingCacheDirectory(), fs, BlobCache.TaskpoolScheduler);
            });
            resolver.Register(() => userAccount.Value, typeof(IBlobCache), "UserAccount");

            var secure = new Lazy<ISecureBlobCache>(() =>
            {
                fs.CreateRecursive(fs.GetDefaultSecretCacheDirectory()).Wait();
                return new EncryptedBlobCache(fs.GetDefaultRoamingCacheDirectory(), resolver.GetService<IEncryptionProvider>(), fs, BlobCache.TaskpoolScheduler);
            });
            resolver.Register(() => secure.Value, typeof(ISecureBlobCache), null);
        }
    }
}
