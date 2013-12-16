using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UIKIT
using MonoTouch.Foundation;
#else
using MonoMac.Foundation;
#endif

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

            var localCache = new Lazy<IBlobCache>(() => 
                new CPersistentBlobCache(fs.GetDefaultLocalMachineCacheDirectory(), fs));
            registerFunction(() => localCache.Value, typeof(IBlobCache), "LocalMachine");

            var userAccount = new Lazy<IBlobCache>(() => 
                new CPersistentBlobCache(fs.GetDefaultRoamingCacheDirectory(), fs));
            registerFunction(() => userAccount.Value, typeof(IBlobCache), "UserAccount");
                
            var secure = new Lazy<ISecureBlobCache>(() => 
                new CEncryptedBlobCache(fs.GetDefaultRoamingCacheDirectory(), fs));
            registerFunction(() => secure.Value, typeof(ISecureBlobCache), null);

            registerFunction(() => new AkavacheHttpMixin(), typeof(IAkavacheHttpMixin), null);
          
#if APPKIT || UIKIT
            BlobCache.ApplicationName = NSBundle.MainBundle.BundleIdentifier;
            registerFunction(() => new MacFilesystemProvider(), typeof(IFilesystemProvider), null);
#endif

#if ANDROID
            var ai = Application.Context.PackageManager.GetApplicationInfo(Application.Context.PackageName, 0);
            BlobCache.ApplicationName = ai.LoadLabel(Application.Context.PackageManager);

            registerFunction(() => new AndroidFilesystemProvider(), typeof(IFilesystemProvider), null);
#endif
        }
    }
}
