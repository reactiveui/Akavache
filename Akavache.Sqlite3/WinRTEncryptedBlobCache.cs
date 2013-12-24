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

            var dpapi = new DataProtectionProvider("LOCAL=user");
            return dpapi.ProtectAsync(data.AsBuffer()).ToObservable()
                .Select(x => x.ToArray());
        }

        protected override IObservable<byte[]> AfterReadFromDiskFilter(byte[] data, IScheduler scheduler)
        {
            if (data.Length == 0)
            {
                return Observable.Return(data);
            }

            var dpapi = new DataProtectionProvider();
            return dpapi.UnprotectAsync(data.AsBuffer()).ToObservable()
                .Select(x => x.ToArray());
        }
    }
}