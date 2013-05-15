using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akavache.Sqlite3
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(Action<Func<object>, Type> registerFunction)
        {
            var fs = RxApp.DependencyResolver.GetService<IFilesystemProvider>();
            if (fs == null)
            {
                throw new Exception("Failed to initialize Akavache properly. Do you have a reference to Akavache.dll?");
            }

            var localCache = new Lazy<IBlobCache>(() =>
                new SqlitePersistentBlobCache(Path.Combine(fs.GetDefaultLocalMachineCacheDirectory(), "blobs.db"), RxApp.TaskpoolScheduler));
            registerFunction(() => localCache.Value, typeof(IBlobCache), "LocalMachine");

            var userAccount = new Lazy<IBlobCache>(() => 
                new SqlitePersistentBlobCache(Path.Combine(fs.GetDefaultRoamingCacheDirectory(), "blobs.db"), RxApp.TaskpoolScheduler));
            registerFunction(() => userAccount.Value, typeof(IBlobCache), "UserAccount");
                
            var secure = new Lazy<ISecureBlobCache>(() => 
                new EncryptedBlobCache(Path.Combine(fs.GetDefaultSecretCacheDirectory(), "secret.db"), RxApp.TaskpoolScheduler));
            registerFunction(() => secure.Value, typeof(ISecureBlobCache));
        }
    }
}
