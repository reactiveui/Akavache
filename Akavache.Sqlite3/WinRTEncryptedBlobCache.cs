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
    public class EncryptedBlobCache : SqlitePersistentBlobCache, ISecureBlobCache
    {
        public EncryptedBlobCache(string databaseFile, IScheduler scheduler = null) : base(databaseFile, scheduler)
        {
        }

        protected override IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data, IScheduler scheduler)
        {
            if (data.Length == 0)
            {
                return Observable.Return(data);
            }

            return Encryption.EncryptBlock(data).ToObservable();
        }

        protected override IObservable<byte[]> AfterReadFromDiskFilter(byte[] data, IScheduler scheduler)
        {
            if (data.Length == 0)
            {
                return Observable.Return(data);
            }

            return Encryption.DecryptBlock(data).ToObservable();
        }
    }
}