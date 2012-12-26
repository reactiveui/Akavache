using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using ReactiveUI;

namespace Akavache
{
    /// <summary>
    /// This class represents an asynchronous key-value store backed by a 
    /// directory. It stores the last 'n' key requests in memory.
    /// </summary>
    public abstract class PersistentBlobCache : IBlobCache, IEnableLogger
    {
        protected MemoizingMRUCache<string, AsyncSubject<byte[]>> MemoizedRequests;
        protected readonly string CacheDirectory;
        protected ConcurrentDictionary<string, CacheIndexEntry> CacheIndex = new ConcurrentDictionary<string, CacheIndexEntry>();
        readonly Subject<Unit> actionTaken = new Subject<Unit>();
        bool disposed;
        protected IFilesystemProvider filesystem;

        readonly IDisposable flushThreadSubscription;
        DateTimeOffset lastFlushTime = DateTimeOffset.MinValue;

        public IScheduler Scheduler { get; protected set; }

        const string BlobCacheIndexKey = "__THISISTHEINDEX__FFS_DONT_NAME_A_FILE_THIS™";

        protected PersistentBlobCache(string cacheDirectory = null, IFilesystemProvider filesystemProvider = null, IScheduler scheduler = null)
        {
            this.CacheDirectory = cacheDirectory ?? GetDefaultRoamingCacheDirectory();
            this.Scheduler = scheduler ?? RxApp.TaskpoolScheduler;
            this.filesystem = filesystemProvider ?? new SimpleFilesystemProvider();

            // Here, we're not actually caching the requests directly (i.e. as
            // byte[]s), but as the "replayed result of the request", in the
            // AsyncSubject - this makes the code infinitely simpler because
            // we don't have to keep a separate list of "in-flight reads" vs
            // "already completed and cached reads"
            MemoizedRequests = new MemoizingMRUCache<string, AsyncSubject<byte[]>>(
                (x, c) => FetchOrWriteBlobFromDisk(x, c, false), 20);

            filesystem.CreateRecursive(CacheDirectory);

            FetchOrWriteBlobFromDisk(BlobCacheIndexKey, null, true)
                .Catch(Observable.Return(new byte[0]))
                .Select(x => Encoding.UTF8.GetString(x, 0, x.Length).Split('\n')
                    .SelectMany(ParseCacheIndexEntry)
                    .ToDictionary(y => y.Key, y => y.Value))
                .Select(x => new ConcurrentDictionary<string, CacheIndexEntry>(x))
                .Subscribe(x => CacheIndex = x);

            flushThreadSubscription = Disposable.Empty;

            if (!RxApp.InUnitTestRunner())
            {
                flushThreadSubscription = actionTaken
                    .Where(_ => CacheIndex != null)
                    .Throttle(TimeSpan.FromSeconds(30), Scheduler)
                    .SelectMany(_ => FlushCacheIndex(true))
                    .Subscribe(_ =>
                    {
                        this.Log().Debug("Flushing cache");
                        lastFlushTime = Scheduler.Now;
                    });
            }

            this.Log().Info("{0} entries in blob cache index", CacheIndex.Count);
        }

        public IServiceProvider ServiceProvider
        {
            get { return BlobCache.ServiceProvider; }
        }

        static readonly Lazy<IBlobCache> _LocalMachine = new Lazy<IBlobCache>(() => new CPersistentBlobCache(GetDefaultLocalMachineCacheDirectory()));

        public static IBlobCache LocalMachine
        {
            get { return _LocalMachine.Value; }
        }

        static readonly Lazy<IBlobCache> _UserAccount = new Lazy<IBlobCache>(() => new CPersistentBlobCache(GetDefaultRoamingCacheDirectory()));

        public static IBlobCache UserAccount
        {
            get { return _UserAccount.Value; }
        }

        class CPersistentBlobCache : PersistentBlobCache
        {
#if SILVERLIGHT
            public CPersistentBlobCache(string cacheDirectory) : base(cacheDirectory, new IsolatedStorageProvider(), RxApp.TaskpoolScheduler) { }
#else
            public CPersistentBlobCache(string cacheDirectory) : base(cacheDirectory, null, RxApp.TaskpoolScheduler) { }
#endif
        }

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException("PersistentBlobCache"));
            if (key == null || data == null)
            {
                return Observable.Throw<Unit>(new ArgumentNullException());
            }

            // NB: Since FetchOrWriteBlobFromDisk is guaranteed to not block,
            // we never sit on this lock for any real length of time
            lock (MemoizedRequests)
            {
                MemoizedRequests.Invalidate(key);
                var err = MemoizedRequests.Get(key, data);

                // If we fail trying to fetch/write the key on disk, we want to 
                // try again instead of replaying the same failure
                err.LogErrors("Insert").Subscribe(
                    x => CacheIndex[key] = new CacheIndexEntry(Scheduler.Now, absoluteExpiration),
                    ex => Invalidate(key));

                return err.Select(_ => Unit.Default);
            }
        }

        public IObservable<byte[]> GetAsync(string key)
        {
            if (disposed) return Observable.Throw<byte[]>(new ObjectDisposedException("PersistentBlobCache"));

            lock (MemoizedRequests)
            {
                IObservable<byte[]> ret;
                if (IsKeyStale(key))
                {
                    Invalidate(key);
                    ret = Observable.Throw<byte[]>(new KeyNotFoundException());
                    return ret;
                }

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
                ret = MemoizedRequests.Get(key);

                // If we fail trying to fetch/write the key on disk, we want to 
                // try again instead of replaying the same failure
                ret.LogErrors("GetAsync")
                    .Subscribe(x => { }, ex => Invalidate(key));

                return ret;
            }
        }

        bool IsKeyStale(string key)
        {
            if (disposed) throw new ObjectDisposedException("PersistentBlobCache");

            CacheIndexEntry value;
            return (CacheIndex.TryGetValue(key, out value) && value.ExpiresAt != null && value.ExpiresAt < Scheduler.Now);
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            CacheIndexEntry value;
            if (!CacheIndex.TryGetValue(key, out value))
            {
                return Observable.Return<DateTimeOffset?>(null);
            }

            return Observable.Return<DateTimeOffset?>(value.CreatedAt);
        }

        public IEnumerable<string> GetAllKeys()
        {
            if (disposed) throw new ObjectDisposedException("PersistentBlobCache");

            lock (MemoizedRequests)
            {
                return CacheIndex.Keys.ToArray();
            }
        }

        public void Invalidate(string key)
        {
            if (disposed) throw new ObjectDisposedException("PersistentBlobCache");

            Action deleteMe;
            lock (MemoizedRequests)
            {
                this.Log().Debug("Invalidating {0}", key);
                MemoizedRequests.Invalidate(key);

                CacheIndexEntry dontcare;
                CacheIndex.TryRemove(key, out dontcare);

                var path = GetPathForKey(key);
                deleteMe = () =>
                {
                    try
                    {
                        filesystem.Delete(path);
                    }

                    catch (FileNotFoundException ex) { this.Log().Warn(ex); }
                    catch (IsolatedStorageException ex) { this.Log().Warn(ex); }

                    actionTaken.OnNext(Unit.Default);
                };
            }

            try
            {
                deleteMe.Retry(1);
            }
            catch (Exception ex)
            {
                this.Log().Warn("Really can't delete key: " + key, ex);
            }
        }

        public void InvalidateAll()
        {
            if (disposed) throw new ObjectDisposedException("PersistentBlobCache");

            lock (MemoizedRequests)
            {
                foreach (var key in CacheIndex.Keys.ToArray())
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

            if (MemoizedRequests == null)
            {
                return;
            }

            lock (MemoizedRequests)
            {
                var mq = Interlocked.Exchange(ref MemoizedRequests, null);
                if (mq == null)
                {
                    return;
                }

                requests = mq.CachedValues().ToArray();

                MemoizedRequests = null;

                actionTaken.OnCompleted();
                flushThreadSubscription.Dispose();
            }

            IObservable<byte[]> requestChain = null;

            if (requests.Length > 0)
            {
                // Since these are all AsyncSubjects, most of them will replay
                // immediately, except for the ones still outstanding; we'll 
                // Merge them all then wait for them all to complete.
                requestChain = requests.Merge(System.Reactive.Concurrency.Scheduler.Immediate)
                    .Timeout(TimeSpan.FromSeconds(30), Scheduler)
                    .Aggregate((acc, x) => x);
            }

            requestChain = requestChain ?? Observable.Return(new byte[0]);

            requestChain.SelectMany(FlushCacheIndex(true)).Subscribe(_ => { });
            disposed = true;
        }

        /// <summary>
        /// This method is called immediately before writing any data to disk.
        /// Override this in encrypting data stores in order to encrypt the
        /// data.
        /// </summary>
        /// <param name="data">The byte data about to be written to disk.</param>
        /// <param name="scheduler">The scheduler to use if an operation has
        /// to be deferred. If the operation can be done immediately, use
        /// Observable.Return and ignore this parameter.</param>
        /// <returns>A Future result representing the encrypted data</returns>
        protected virtual IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data, IScheduler scheduler)
        {
            if (disposed) return Observable.Throw<byte[]>(new ObjectDisposedException("PersistentBlobCache"));

            return Observable.Return(data);
        }

        /// <summary>
        /// This method is called immediately after reading any data to disk.
        /// Override this in encrypting data stores in order to decrypt the
        /// data.
        /// </summary>
        /// <param name="data">The byte data that has just been read from
        /// disk.</param>
        /// <param name="scheduler">The scheduler to use if an operation has
        /// to be deferred. If the operation can be done immediately, use
        /// Observable.Return and ignore this parameter.</param>
        /// <returns>A Future result representing the decrypted data</returns>
        protected virtual IObservable<byte[]> AfterReadFromDiskFilter(byte[] data, IScheduler scheduler)
        {
            if (disposed) return Observable.Throw<byte[]>(new ObjectDisposedException("PersistentBlobCache"));
            if (disposed) throw new ObjectDisposedException("PersistentBlobCache");

            return Observable.Return(data);
        }

        AsyncSubject<byte[]> FetchOrWriteBlobFromDisk(string key, object byteData, bool synchronous)
        {
            // If this is secretly a write, dispatch to WriteBlobToDisk (we're 
            // kind of abusing the 'context' variable from MemoizingMRUCache 
            // here a bit)
            if (byteData != null)
            {
                return WriteBlobToDisk(key, (byte[])byteData, synchronous);
            }

            var ret = new AsyncSubject<byte[]>();
            var ms = new MemoryStream();

            var scheduler = synchronous ? System.Reactive.Concurrency.Scheduler.Immediate : Scheduler;
            if (disposed)
            {
                Observable.Throw<byte[]>(new ObjectDisposedException("PersistentBlobCache")).Multicast(ret).Connect();
                goto leave;
            }

            filesystem.SafeOpenFileAsync(GetPathForKey(key), FileMode.Open, FileAccess.Read, FileShare.Read, scheduler)
                .SelectMany(x => x.CopyToAsync(ms, scheduler))
                .SelectMany(x => AfterReadFromDiskFilter(ms.ToArray(), scheduler))
                .Catch<byte[], FileNotFoundException>(ex => Observable.Throw<byte[]>(new KeyNotFoundException()))
                .Catch<byte[], IsolatedStorageException>(ex => Observable.Throw<byte[]>(new KeyNotFoundException()))
                .Do(_ => { if (!synchronous && key != BlobCacheIndexKey) { actionTaken.OnNext(Unit.Default); } })
                .Multicast(ret).Connect();

        leave:
            return ret;
        }

        AsyncSubject<byte[]> WriteBlobToDisk(string key, byte[] byteData, bool synchronous)
        {
            var ret = new AsyncSubject<byte[]>();
            var scheduler = synchronous ? System.Reactive.Concurrency.Scheduler.Immediate : Scheduler;

            if (disposed)
            {
                Observable.Throw<byte[]>(new ObjectDisposedException("PersistentBlobCache")).Multicast(ret).Connect();
                goto leave;
            }

            var files = Observable.Zip(
                BeforeWriteToDiskFilter(byteData, scheduler).Select(x => new MemoryStream(x)),
                filesystem.SafeOpenFileAsync(GetPathForKey(key), FileMode.Create, FileAccess.Write, FileShare.None, scheduler),
                (from, to) => new { from, to }
            );

            // NB: The fact that our writing AsyncSubject waits until the 
            // write actually completes means that an Insert immediately 
            // followed by a Get will take longer to process - however,
            // this also means that failed writes will disappear from the
            // cache, which is A Good Thing.
            files
                .SelectMany(x => x.from.CopyToAsync(x.to, scheduler))
                .Select(_ => byteData)
                .Do(_ => { if (!synchronous && key != BlobCacheIndexKey) { actionTaken.OnNext(Unit.Default); } })
                .Multicast(ret).Connect();

        leave:
            return ret;
        }

        public IObservable<Unit> Flush()
        {
            return FlushCacheIndex(false);
        }

        IObservable<Unit> FlushCacheIndex(bool synchronous)
        {
            if (disposed) return Observable.Return(Unit.Default);

            var index = CacheIndex.Select(x => JsonConvert.SerializeObject(x));
            return WriteBlobToDisk(BlobCacheIndexKey, Encoding.UTF8.GetBytes(String.Join("\n", index)), synchronous)
                .Select(_ => Unit.Default)
                .Catch<Unit, Exception>(ex =>
                {
                    this.Log().WarnException("Couldn't flush cache index", ex);
                    return Observable.Return(Unit.Default);
                });
        }

        IEnumerable<KeyValuePair<string, CacheIndexEntry>> ParseCacheIndexEntry(string s)
        {
            if (disposed) throw new ObjectDisposedException("PersistentBlobCache");

            if (String.IsNullOrWhiteSpace(s))
            {
                return Enumerable.Empty<KeyValuePair<string, CacheIndexEntry>>();
            }

            try
            {
                return new[] { JsonConvert.DeserializeObject<KeyValuePair<string, CacheIndexEntry>>(s) };
            }
            catch (Exception ex)
            {
                this.Log().Warn("Invalid cache index entry", ex);
                return Enumerable.Empty<KeyValuePair<string, CacheIndexEntry>>();
            }
        }

        string GetPathForKey(string key)
        {
            return Path.Combine(CacheDirectory, Utility.GetMd5Hash(key));
        }

#if SILVERLIGHT
        protected static string GetDefaultRoamingCacheDirectory()
        {
            return "BlobCache";
        }

        protected static string GetDefaultLocalMachineCacheDirectory()
        {
            return "LocalBlobCache";
        }
#elif WINRT
        protected static string GetDefaultRoamingCacheDirectory()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "BlobCache");
        }

        protected static string GetDefaultLocalMachineCacheDirectory()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "BlobCache");
        }
#else
        protected static string GetDefaultRoamingCacheDirectory()
        {
            return RxApp.InUnitTestRunner() ?
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "BlobCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BlobCache.ApplicationName, "BlobCache");
        }

        protected static string GetDefaultLocalMachineCacheDirectory()
        {
            return RxApp.InUnitTestRunner() ? 
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "LocalBlobCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), BlobCache.ApplicationName, "BlobCache");
        }
#endif
    }
}
