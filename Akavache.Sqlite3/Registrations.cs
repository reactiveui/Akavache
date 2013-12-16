using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;

namespace Akavache.Sqlite3
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(Action<Func<object>, Type, string> registerFunction)
        {
            // NB: We want the most recently registered fs, since there really 
            // only should be one 
            var fs = Locator.Current.GetServices<IFilesystemProvider>().LastOrDefault();
            if (fs == null)
            {
                throw new Exception("Failed to initialize Akavache properly. Do you have a reference to Akavache.dll?");
            }

            var localCache = new Lazy<IBlobCache>(() =>{
                fs.CreateRecursive(fs.GetDefaultLocalMachineCacheDirectory()).Wait();
                return new SqlitePersistentBlobCache(Path.Combine(fs.GetDefaultLocalMachineCacheDirectory(), "blobs.db"), RxApp.TaskpoolScheduler);
            });
            registerFunction(() => localCache.Value, typeof(IBlobCache), "LocalMachine");

            var userAccount = new Lazy<IBlobCache>(() =>{
                fs.CreateRecursive(fs.GetDefaultRoamingCacheDirectory()).Wait();
                return new SqlitePersistentBlobCache(Path.Combine(fs.GetDefaultRoamingCacheDirectory(), "userblobs.db"), RxApp.TaskpoolScheduler);
            });
            registerFunction(() => userAccount.Value, typeof(IBlobCache), "UserAccount");
                
            var secure = new Lazy<ISecureBlobCache>(() => {
                fs.CreateRecursive(fs.GetDefaultSecretCacheDirectory()).Wait();
                return new EncryptedBlobCache(Path.Combine(fs.GetDefaultSecretCacheDirectory(), "secret.db"), RxApp.TaskpoolScheduler);
            });
            registerFunction(() => secure.Value, typeof(ISecureBlobCache), null);
        }
    }
}
