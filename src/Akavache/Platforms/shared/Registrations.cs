
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

#if COCOA
using Foundation;
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


//TODO SHANE in theory all the types could just be FileSystemProvider
//and then have no if defs
#if XAMARIN_MOBILE
            var fs = new IsolatedStorageProvider();
#elif WINDOWS_UWP
            var fs = new WinRTFilesystemProvider();
#elif COCOA
            var fs = new MacFilesystemProvider();
#elif ANDROID
            var fs = new AndroidFilesystemProvider();
#else
            var fs = new SimpleFilesystemProvider();
#endif
            resolver.Register(() => fs, typeof(IFilesystemProvider), null);

#if WINDOWS_UWP
            var enc = new WinRTEncryptionProvider();
#else
            var enc = new EncryptionProvider();
#endif
            resolver.Register(() => enc, typeof(IEncryptionProvider), null);

            var localCache = new Lazy<IBlobCache>(() => new InMemoryBlobCache());
            var userAccount = new Lazy<IBlobCache>(() => new InMemoryBlobCache());
            var secure = new Lazy<ISecureBlobCache>(() => new InMemoryBlobCache());

            resolver.Register(() => localCache.Value, typeof(IBlobCache), "LocalMachine");
            resolver.Register(() => userAccount.Value, typeof(IBlobCache), "UserAccount");
            resolver.Register(() => secure.Value, typeof(ISecureBlobCache), null);

            resolver.Register(() => new AkavacheHttpMixin(), typeof(IAkavacheHttpMixin), null);

#if COCOA
            BlobCache.ApplicationName = NSBundle.MainBundle.BundleIdentifier;
#endif

#if ANDROID
            var ai = Application.Context.PackageManager.GetApplicationInfo(Application.Context.PackageName, 0);
            BlobCache.ApplicationName = ai.LoadLabel(Application.Context.PackageManager);
#endif
        }
    }
}
