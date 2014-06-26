using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
using System.Reflection;
using Splat;

namespace Akavache.Sqlite3
{
    public class SQLiteEncryptedBlobCache : SQLitePersistentBlobCache, ISecureBlobCache
    {
        private readonly IEncryptionProvider encryption;

        public SQLiteEncryptedBlobCache(string databaseFile, IEncryptionProvider encryptionProvider = null, IScheduler scheduler = null) : base(databaseFile, scheduler)
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