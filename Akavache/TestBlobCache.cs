using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using ReactiveUI;

namespace Akavache
{
    public class TestBlobCache : ISecureBlobCache
    {
        public TestBlobCache(IScheduler scheduler = null, params KeyValuePair<string, byte[]>[] initialContents) :
            this(scheduler, (IEnumerable<KeyValuePair<string, byte[]>>)initialContents)
        {
        }

        public TestBlobCache(IScheduler scheduler = null, IEnumerable<KeyValuePair<string, byte[]>> initialContents = null)
        {
            Scheduler = scheduler ?? System.Reactive.Concurrency.Scheduler.CurrentThread;
            foreach (var item in initialContents ?? Enumerable.Empty<KeyValuePair<string, byte[]>>())
            {
                cache[item.Key] = new Tuple<CacheIndexEntry, byte[]>(new CacheIndexEntry(Scheduler.Now, null), item.Value);
            }
        }

        internal TestBlobCache(Action disposer, IScheduler scheduler = null, IEnumerable<KeyValuePair<string, byte[]>> initialContents = null)
            : this(scheduler, initialContents)
        {
            inner = Disposable.Create(disposer);
        }

        public IScheduler Scheduler { get; protected set; }

        public IServiceProvider ServiceProvider
        {
            get { return BlobCache.ServiceProvider; }
        }

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

        public void Invalidate(string key)
        {
            if (disposed) throw new ObjectDisposedException("TestBlobCache");
            lock (cache)
            {
                if (cache.ContainsKey(key))
                {
                    cache.Remove(key);
                }
            }
        }

        public void InvalidateAll()
        {
            if (disposed) throw new ObjectDisposedException("TestBlobCache");
            lock (cache)
            {
                cache.Clear();
            }
        }

        public void Dispose()
        {
            Scheduler = null;
            cache = null;
            if (inner != null)
            {
                inner.Dispose();
            }
            disposed = true;
        }

        static readonly object gate = 42;

        public static TestBlobCache OverrideGlobals(IScheduler scheduler = null, params KeyValuePair<string, byte[]>[] initialContents)
        {
            Monitor.Enter(gate);

            var local = BlobCache.LocalMachine;
            var user = BlobCache.UserAccount;
            var sec = BlobCache.Secure;

            var resetBlobCache = new Action(() =>
            {
                BlobCache.LocalMachine = local;
                BlobCache.Secure = sec;
                BlobCache.UserAccount = user;
                Monitor.Exit(gate);
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
