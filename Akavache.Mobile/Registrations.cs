using Newtonsoft.Json;
using ReactiveUI.Mobile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UIKIT
using MonoTouch.Foundation;
using ReactiveUI.Mobile;
#endif

#if APPKIT
using MonoMac.Foundation;
#endif

#if ANDROID
using Android.App;
#endif

#if APPKIT
namespace Akavache.Mac
#else
namespace Akavache.Mobile
#endif
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(Action<Func<object>, Type, string> registerFunction)
        {
            registerFunction(() => new JsonSerializerSettings() 
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            }, typeof(JsonSerializerSettings), null);

            var akavacheDriver = new AkavacheDriver();
            registerFunction(() => akavacheDriver, typeof(ISuspensionDriver), null);

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