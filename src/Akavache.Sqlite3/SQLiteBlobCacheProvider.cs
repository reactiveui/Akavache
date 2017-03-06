using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace Akavache.Sqlite3
{
    public class SQLiteBlobCacheProvider : IBlobCacheProvider
    {
        IFilesystemProvider getFilesystemProvider()
        {
            // NB: We want the most recently registered fs, since there really 
            // only should be one 
            var fs = Locator.Current.GetService<IFilesystemProvider>();
            if (fs == null)
            {
                throw new Exception("Failed to initialize Akavache properly. Do you have a reference to Akavache.dll?");
            }

            return fs;
        }   

        public virtual IBlobCache CreateLocalMachine(string fileName)
        {
            var fs = getFilesystemProvider();
            fs.CreateRecursive(fs.GetDefaultLocalMachineCacheDirectory()).SubscribeOn(BlobCache.TaskpoolScheduler).Wait();
            return new SQLitePersistentBlobCache(Path.Combine(fs.GetDefaultLocalMachineCacheDirectory(), fileName), BlobCache.TaskpoolScheduler);
        }

        public virtual IBlobCache CreateUserAccount(string fileName)
        {
            var fs = getFilesystemProvider();
            fs.CreateRecursive(fs.GetDefaultRoamingCacheDirectory()).SubscribeOn(BlobCache.TaskpoolScheduler).Wait();
            return new SQLitePersistentBlobCache(Path.Combine(fs.GetDefaultRoamingCacheDirectory(), fileName), BlobCache.TaskpoolScheduler);
        }

        public virtual ISecureBlobCache CreateSecure(string fileName)
        {
            var fs = getFilesystemProvider();
            fs.CreateRecursive(fs.GetDefaultSecretCacheDirectory()).SubscribeOn(BlobCache.TaskpoolScheduler).Wait();
            return new SQLiteEncryptedBlobCache(Path.Combine(fs.GetDefaultSecretCacheDirectory(), fileName), Locator.Current.GetService<IEncryptionProvider>(), BlobCache.TaskpoolScheduler);
        }
    }
}
