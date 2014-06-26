using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Splat;
using Akavache;

namespace Akavache.Deprecated
{
    public class EncryptedBlobCache : PersistentBlobCache, ISecureBlobCache
    {
        private readonly IEncryptionProvider encryption;

        public EncryptedBlobCache(
            string cacheDirectory = null,
            IEncryptionProvider encryptionProvider = null,
            IFilesystemProvider filesystemProvider = null, 
            IScheduler scheduler = null,
            Action<AsyncSubject<byte[]>> invalidatedCallback = null) 
            : base(cacheDirectory, filesystemProvider, scheduler, invalidatedCallback)
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
    }
}
