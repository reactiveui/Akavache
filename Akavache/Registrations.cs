using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(Action<Func<object>, Type, string> registerFunction)
        {
#if SILVERLIGHT
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
        }
    }
}
