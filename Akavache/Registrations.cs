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

namespace Akavache
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(IMutableDependencyResolver resolver)
        {
#if SILVERLIGHT || XAMARIN_MOBILE
            var fs = new IsolatedStorageProvider();
#else
            var fs = new SimpleFilesystemProvider();
#endif
            resolver.Register(() => fs, typeof(IFilesystemProvider), null);

            var localCache = default(Lazy<IBlobCache>);
            var userAccount = default(Lazy<IBlobCache>);
            var secure = default(Lazy<ISecureBlobCache>);

            if (!ModeDetector.InUnitTestRunner()) {
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

            resolver.Register(() => localCache.Value, typeof(IBlobCache), "LocalMachine");
            resolver.Register(() => userAccount.Value, typeof(IBlobCache), "UserAccount");
            resolver.Register(() => secure.Value, typeof(ISecureBlobCache), null);

            resolver.Register(() => new AkavacheHttpMixin(), typeof(IAkavacheHttpMixin), null);
          
#if APPKIT || UIKIT
            BlobCache.ApplicationName = NSBundle.MainBundle.BundleIdentifier;
            resolver.Register(() => new MacFilesystemProvider(), typeof(IFilesystemProvider), null);
#endif

#if ANDROID
            var ai = Application.Context.PackageManager.GetApplicationInfo(Application.Context.PackageName, 0);
            BlobCache.ApplicationName = ai.LoadLabel(Application.Context.PackageManager);

            resolver.Register(() => new AndroidFilesystemProvider(), typeof(IFilesystemProvider), null);
#endif
        }
    }
}
