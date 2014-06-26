using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Reactive.Linq;
using System.Reflection;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;
using Splat;

namespace Akavache.Deprecated
{
    public abstract class EncryptedBlobCache : PersistentBlobCache, ISecureBlobCache
    {
        private readonly IEncryptionProvider encryption;

        protected EncryptedBlobCache(string cacheDirectory = null, IEncryptionProvider encryptionProvider = null, IFilesystemProvider filesystemProvider = null, IScheduler scheduler = null)
            : base(cacheDirectory, filesystemProvider, scheduler)
        {
            this.encryption = encryptionProvider ?? Locator.Current.GetService<IEncryptionProvider>();

            if (this.encryption == null)
            {
                throw new Exception("No IEncryptionProvider available. This should never happen, your DependencyResolver is broken");
            }
        }

        protected override IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data, IScheduler scheduler)
        {
            if (data.Length == 0) 
            {
                return Observable.Return(data);
            }

            return this.encryption.EncryptBlock(data);
        }

        protected override IObservable<byte[]> AfterReadFromDiskFilter(byte[] data, IScheduler scheduler)
        {
            if (data.Length == 0) 
            {
                return Observable.Return(data);
            }

            return this.encryption.DecryptBlock(data);
        }

        protected static string GetDefaultCacheDirectory()
        {
            return Path.Combine(ApplicationData.Current.RoamingFolder.Path, "SecretCache");
        }
    }

    class CEncryptedBlobCache : EncryptedBlobCache
    {
        public CEncryptedBlobCache(string cacheDirectory, IEncryptionProvider encryption, IFilesystemProvider fs) : base(cacheDirectory, encryption, fs, BlobCache.TaskpoolScheduler) { }
    }
}