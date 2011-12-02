using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using ReactiveUI;

namespace Akavache
{
    /// <summary>
    /// This class represents an asynchronous key-value store backed by a 
    /// directory. It stores the last 'n' key requests in memory.
    /// </summary>
    public abstract class PersistentBlobCache : IEnableLogger, IBlobCache
    {
        protected MemoizingMRUCache<string, AsyncSubject<byte[]>> MemoizedRequests;
        protected readonly IScheduler Scheduler;
        protected readonly string CacheDirectory;
        protected readonly ConcurrentDictionary<string, bool> CacheIndex = new ConcurrentDictionary<string, bool>();
        readonly Subject<Unit> actionTaken = new Subject<Unit>();

        const string BlobCacheIndexKey = "__THISISTHEINDEX__FFS_DONT_NAME_A_FILE_THIS™";

        protected PersistentBlobCache(string cacheDirectory = null, IScheduler scheduler = null)
        {
            this.CacheDirectory = cacheDirectory ?? GetDefaultCacheDirectory();
            this.Scheduler = scheduler ?? RxApp.TaskpoolScheduler;

            // Here, we're not actually caching the requests directly (i.e. as
            // byte[]s), but as the "replayed result of the request", in the
            // AsyncSubject - this makes the code infinitely simpler because
            // we don't have to keep a separate list of "in-flight reads" vs
            // "already completed and cached reads"
            MemoizedRequests = new MemoizingMRUCache<string, AsyncSubject<byte[]>>(
                FetchOrWriteBlobFromDisk, 20);

            if (!Directory.Exists(CacheDirectory))
            {
                this.Log().WarnFormat("Creating cache directory '{0}'", CacheDirectory);
                (new DirectoryInfo(CacheDirectory)).CreateRecursive();
            }

            CacheIndex = GetAsync(BlobCacheIndexKey)
                .Catch(Observable.Return(new byte[0]))
                .Select(x => Encoding.UTF8.GetString(x).Split('\n').ToDictionary(y => y, _ => true))
                .Select(x => new ConcurrentDictionary<string, bool>(x))
                .First();

            actionTaken
                .Throttle(TimeSpan.FromSeconds(2), RxApp.TaskpoolScheduler)
                .Subscribe(_ => FlushCacheIndex());

            this.Log().InfoFormat("{0} entries in blob cache index", CacheIndex.Count);
        }

        static readonly Lazy<IBlobCache> _LocalMachine = new Lazy<IBlobCache>(() => new CPersistentBlobCache(GetLocalMachineCacheDirectory()));
        public static IBlobCache LocalMachine 
        {
            get { return _LocalMachine.Value;  }
        }

        static readonly Lazy<IBlobCache> _UserAccount = new Lazy<IBlobCache>(() => new CPersistentBlobCache(GetDefaultCacheDirectory()));
        public static IBlobCache UserAccount 
        {
            get { return _UserAccount.Value;  }
        }

        class CPersistentBlobCache : PersistentBlobCache {
            public CPersistentBlobCache(string cacheDirectory) : base(cacheDirectory, RxApp.TaskpoolScheduler) { }
        }

        public void Insert(string key, byte[] data)
        {
            if (key == null || data == null)
            {
                throw new ArgumentNullException();
            }

            // NB: Since FetchOrWriteBlobFromDisk is guaranteed to not block,
            // we never sit on this lock for any real length of time
            lock(MemoizedRequests)
            {
                var err = MemoizedRequests.Get(key, data);

                // If we fail trying to fetch/write the key on disk, we want to 
                // try again instead of replaying the same failure
                err.LogErrors("Insert").Subscribe(
                    x => CacheIndex[key] = true, 
                    ex => Invalidate(key));
                
                actionTaken.OnNext(Unit.Default);
            }
        }

        public IObservable<byte[]> GetAsync(string key)
        {
            lock(MemoizedRequests)
            {
                // There are three scenarios here, and we handle all of them 
                // with aplomb and elegance:
                //
                // 1. The key is already in memory as a completed request - we 
                //     return the AsyncSubject which will replay the result
                //
                // 2. The key is currently being fetched from disk - in this
                //     case, MemoizingMRUCache has an AsyncSubject for it (since
                //     FetchOrWriteBlobFromDisk completes immediately), and the
                //     client will get the result when the disk I/O completes
                //
                // 3. The key isn't in memory and isn't being fetched - in
                //    this case, FetchOrWriteBlobFromDisk will be called which
                //    will immediately return an AsyncSubject representing the
                //    queued disk read.
                var ret = MemoizedRequests.Get(key);

                // If we fail trying to fetch/write the key on disk, we want to 
                // try again instead of replaying the same failure
                ret.LogErrors("GetAsync").Subscribe(
                    x => CacheIndex[key] = true,
                    ex => Invalidate(key));

                actionTaken.OnNext(Unit.Default);
                return ret;
            }
        }

        public IEnumerable<string> GetAllKeys()
        {
            lock (MemoizedRequests)
            {
                actionTaken.OnNext(Unit.Default);
                return CacheIndex.Keys.ToArray();
            }
        }

        public void Invalidate(string key)
        {
            lock(MemoizedRequests)
            {
                this.Log().DebugFormat("Invalidating {0}", key);
                MemoizedRequests.Invalidate(key);

                bool dontcare;
                CacheIndex.TryRemove(key, out dontcare);

                var deleteMe = new Action(() =>
                {
                    try
                    {
                        File.Delete(GetPathForKey(key));
                    }
                    catch (FileNotFoundException ex) { this.Log().Warn(ex); }
                });

                actionTaken.OnNext(Unit.Default);
                deleteMe.Retry();
            }
        }

        public void InvalidateAll()
        {
            lock(MemoizedRequests)
            {
                foreach(var key in CacheIndex.Keys.ToArray())
                {
                    Invalidate(key);
                }
            }
        }

        public void Dispose()
        {
            // We need to make sure that all outstanding writes are flushed
            // before we bail
            AsyncSubject<byte[]>[] requests;
            lock(MemoizedRequests)
            {
                requests = MemoizedRequests.CachedValues().ToArray();
                MemoizedRequests = null;
            }

            if (requests.Length > 0)
            {
                // Since these are all AsyncSubjects, most of them will replay
                // immediately, except for the ones still outstanding; we'll 
                // Merge them all then wait for them all to complete.
                requests.Merge()
                    .Timeout(TimeSpan.FromSeconds(30), Scheduler)
                    .Wait();
            }

            FlushCacheIndex().First();
        }

        IObservable<Unit> FlushCacheIndex()
        {
            var index = CacheIndex.Keys.ToArray();
            return WriteBlobToDisk(BlobCacheIndexKey, Encoding.UTF8.GetBytes(String.Join("\n", index)))
                .Select(_ => Unit.Default);
        }

        protected virtual IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data)
        {
            return Observable.Return(data);
        }

        protected virtual IObservable<byte[]> AfterReadFromDiskFilter(byte[] data)
        {
            return Observable.Return(data);
        }

        AsyncSubject<byte[]> FetchOrWriteBlobFromDisk(string key, object byteData)
        {
            // If this is secretly a write, dispatch to WriteBlobToDisk (we're 
            // kind of abusing the 'context' variable from MemoizingMRUCache 
            // here a bit)
            if (byteData != null)
            {
                return WriteBlobToDisk(key, (byte[]) byteData);
            }

            var ms = new MemoryStream();
            var ret = new AsyncSubject<byte[]>();

            Utility.SafeOpenFileAsync(GetPathForKey(key), FileMode.Open, FileAccess.Read, FileShare.Read)
                .SelectMany(x => x.CopyToAsync(ms))
                .SelectMany(x => AfterReadFromDiskFilter(ms.ToArray()))
                .Catch<byte[], FileNotFoundException>(ex => Observable.Throw<byte[]>(new KeyNotFoundException()))
                .Multicast(ret).Connect();

            return ret;
        }

        AsyncSubject<byte[]> WriteBlobToDisk(string key, byte[] byteData)
        {
            var ret = new AsyncSubject<byte[]>();
            var files = Observable.Zip(
                BeforeWriteToDiskFilter(byteData).Select(x => new MemoryStream(x)),
                Utility.SafeOpenFileAsync(GetPathForKey(key), FileMode.Create, FileAccess.Write, FileShare.None),
                (from, to) => new { from, to }
            );

            // NB: The fact that our writing AsyncSubject waits until the 
            // write actually completes means that an Insert immediately 
            // followed by a Get will take longer to process - however,
            // this also means that failed writes will disappear from the
            // cache, which is A Good Thing.
            files
                .SelectMany(x => x.from.CopyToAsync(x.to))
                .Select(_ => byteData)
                .Multicast(ret).Connect();
    
            return ret;
        }

        string GetPathForKey(string key)
        {
            return Path.Combine(CacheDirectory, Utility.GetMd5Hash(key));
        }

        static string GetDefaultCacheDirectory()
        {
            return RxApp.InUnitTestRunner() ?
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "BlobCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GitHub", "BlobCache");
        }

        static string GetLocalMachineCacheDirectory()
        {
            return RxApp.InUnitTestRunner() ?
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "LocalBlobCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GitHub", "BlobCache");
        }
    }
}