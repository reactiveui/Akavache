using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

namespace Akavache.Sqlite3
{
    public class ServiceLocationRegistration : IWantsToRegisterStuff
    {
        public void Register()
        {
            var fs = RxApp.GetService<IFilesystemProvider>();

            BlobCache.defaultLocalMachineOverride = new Lazy<IBlobCache>(() => 
                new SqlitePersistentBlobCache(Path.Combine(fs.GetDefaultLocalMachineCacheDirectory(), "blobs.db"), RxApp.TaskpoolScheduler));
            BlobCache.defaultUserAccountOverride = new Lazy<IBlobCache>(() => 
                new SqlitePersistentBlobCache(Path.Combine(fs.GetDefaultRoamingCacheDirectory(), "blobs.db"), RxApp.TaskpoolScheduler));
            BlobCache.defaultSecureOverride = new Lazy<ISecureBlobCache>(() => 
                new EncryptedBlobCache(Path.Combine(fs.GetDefaultSecretCacheDirectory(), "secret.db"), RxApp.TaskpoolScheduler));
        }
    }
}

