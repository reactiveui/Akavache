using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using ReactiveUI;
using System.Reactive.Subjects;

namespace Akavache
{
    public class TestBlobCache : ISecureBlobCache
    {
        public TestBlobCache() : this(null, null)
        {
        }

        public TestBlobCache(IScheduler scheduler) : this(scheduler, null)
        {
        }

        public TestBlobCache(IEnumerable<KeyValuePair<string, byte[]>> initialContents) : this(null, initialContents)
        {
        }

        public TestBlobCache(IScheduler scheduler, IEnumerable<KeyValuePair<string, byte[]>> initialContents)
        {
            Scheduler = scheduler ?? System.Reactive.Concurrency.Scheduler.CurrentThread;
            foreach (var item in initialContents ?? Enumerable.Empty<KeyValuePair<string, byte[]>>())
            {
                cache[item.Key] = new Tuple<CacheIndexEntry, byte[]>(new CacheIndexEntry(Scheduler.Now, null), item.Value);
            }
        }

        internal TestBlobCache(Action disposer, 
            IScheduler scheduler, 
            IEnumerable<KeyValuePair<string, byte[]>> initialContents)
            : this(scheduler, initialContents)
        {
            inner = Disposable.Create(disposer);
        }

        public IScheduler Scheduler { get; protected set; }

        IServiceProvider serviceProvider;
        public IServiceProvider ServiceProvider
        {
            get { return serviceProvider; }
            set { serviceProvider = value; }
        }

        readonly AsyncSubject<Unit> shutdown = new AsyncSubject<Unit>();
        public IObservable<Unit> Shutdown { get { return shutdown; } }

        readonly IDisposable inner;
        bool disposed;
        Dictionary<string, Tuple<CacheIndexEntry, byte[]>> cache = new Dictionary<string, Tuple<CacheIndexEntry, byte[]>>();

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = new DateTimeOffset?())
        {
            if (disposed) throw new ObjectDisposedException("TestBlobCache");
            lock (cache)
            {
                cache[key] = new Tuple<CacheIndexEntry, byte[]>(new CacheIndexEntry(Scheduler.Now, absoluteExpiration), data);
            }

            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> Flush()
        {
            return Observable.Return(Unit.Default);
        }

        public IObservable<byte[]> GetAsync(string key)
        {
            if (disposed) throw new ObjectDisposedException("TestBlobCache");
            lock (cache)
            {
                if (!cache.ContainsKey(key))
                {
                    return Observable.Throw<byte[]>(new KeyNotFoundException());
                }

                var item = cache[key];
                if (item.Item1.ExpiresAt != null && Scheduler.Now > item.Item1.ExpiresAt.Value)
                {
                    cache.Remove(key);
                    return Observable.Throw<byte[]>(new KeyNotFoundException());
                }

                return Observable.Return(item.Item2, Scheduler);
            }
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            lock (cache)
            {
                if (!cache.ContainsKey(key))
                {
                    return Observable.Return<DateTimeOffset?>(null);
                }

                return Observable.Return<DateTimeOffset?>(cache[key].Item1.CreatedAt);
            }
        }

        public IEnumerable<string> GetAllKeys()
        {
            if (disposed) throw new ObjectDisposedException("TestBlobCache");
            lock (cache)
            {
                return cache.Keys.ToArray();
            }
        }

        public IObservable<Unit> Invalidate(string key)
        {
            if (disposed) throw new ObjectDisposedException("TestBlobCache");
            lock (cache)
            {
                if (cache.ContainsKey(key))
                {
                    cache.Remove(key);
                }
            }

            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> InvalidateAll()
        {
            if (disposed) throw new ObjectDisposedException("TestBlobCache");
            lock (cache)
            {
                cache.Clear();
            }

            return Observable.Return(Unit.Default);
        }

        public void Dispose()
        {
            Scheduler = null;
            cache = null;
            if (inner != null)
            {
                inner.Dispose();
            }

            shutdown.OnNext(Unit.Default); shutdown.OnCompleted();
            disposed = true;
        }

        public static TestBlobCache OverrideGlobals(IScheduler scheduler = null, params KeyValuePair<string, byte[]>[] initialContents)
        {
            var local = BlobCache.LocalMachine;
            var user = BlobCache.UserAccount;
            var sec = BlobCache.Secure;

            var resetBlobCache = new Action(() =>
            {
                BlobCache.LocalMachine = local;
                BlobCache.Secure = sec;
                BlobCache.UserAccount = user;
            });

            var testCache = new TestBlobCache(resetBlobCache, scheduler, initialContents);
            BlobCache.LocalMachine = testCache;
            BlobCache.Secure = testCache;
            BlobCache.UserAccount = testCache;

            return testCache;
        }

        public static TestBlobCache OverrideGlobals(IDictionary<string, byte[]> initialContents, IScheduler scheduler = null)
        {
            return OverrideGlobals(scheduler, initialContents.ToArray());
        }

        public static TestBlobCache OverrideGlobals(IDictionary<string, object> initialContents, IScheduler scheduler = null)
        {
            var initialSerializedContents = initialContents
                .Select(item => new KeyValuePair<string, byte[]>(item.Key, JsonSerializationMixin.SerializeObject(item.Value)))
                .ToArray();

            return OverrideGlobals(scheduler, initialSerializedContents);
        }
    }
}
