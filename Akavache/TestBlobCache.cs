using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace Akavache
{
    public class TestBlobCache : ISecureBlobCache
    {
        public TestBlobCache(IScheduler scheduler = null, params KeyValuePair<string, byte[]>[] initialContents) : 
            this(scheduler, (IEnumerable<KeyValuePair<string, byte[]>>)initialContents) { }

        public TestBlobCache(IScheduler scheduler = null, IEnumerable<KeyValuePair<string, byte[]>> initialContents = null)
        {
            Scheduler = scheduler ?? System.Reactive.Concurrency.Scheduler.CurrentThread;
            foreach (var item in initialContents ?? Enumerable.Empty<KeyValuePair<string, byte[]>>())
            {
                cache[item.Key] = new Tuple<DateTimeOffset?, byte[]>(null, item.Value);
            }
        }

        public IScheduler Scheduler { get; protected set; }

        Dictionary<string, Tuple<DateTimeOffset?, byte[]>> cache = new Dictionary<string, Tuple<DateTimeOffset?, byte[]>>();

        public void Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = new DateTimeOffset?())
        {
            lock(cache)
            {
                cache[key] = new Tuple<DateTimeOffset?, byte[]>(absoluteExpiration, data);
            }
        }

        public IObservable<byte[]> GetAsync(string key)
        {
            lock(cache)
            {
                if (!cache.ContainsKey(key))
                {
                    return Observable.Throw<byte[]>(new KeyNotFoundException());
                }

                var item = cache[key];
                if (item.Item1 != null && Scheduler.Now > item.Item1.Value)
                {
                    cache.Remove(key);
                    return Observable.Throw<byte[]>(new KeyNotFoundException());
                }

                return Observable.Return(item.Item2, Scheduler);
            }
        }

        public IEnumerable<string> GetAllKeys()
        {
            lock(cache)
            {
                return cache.Keys.ToArray();
            }
        }

        public void Invalidate(string key)
        {
            lock(cache)
            {
                if (cache.ContainsKey(key))
                {
                    cache.Remove(key);
                }
            }
        }

        public void InvalidateAll()
        {
            lock (cache)
            {
                cache.Clear();
            }
        }

        public void Dispose()
        {
            Scheduler = null;
            cache = null;
        }

        public static IDisposable OverrideGlobals(out TestBlobCache testCache, IScheduler scheduler = null, params KeyValuePair<string, byte[]>[] initialContents)
        {
            var local = BlobCache.LocalMachine;
            var user = BlobCache.UserAccount;
            var sec = BlobCache.Secure;

            testCache = new TestBlobCache(scheduler, initialContents);
            BlobCache.LocalMachine = testCache; BlobCache.Secure = testCache; BlobCache.Secure = testCache;

            return Disposable.Create(() =>
            {
                BlobCache.LocalMachine = local; BlobCache.Secure = sec; BlobCache.UserAccount = user;
            });
        }
    }
}
