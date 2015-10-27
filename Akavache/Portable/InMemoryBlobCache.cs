using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Splat;
using System.Reactive.Subjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.IO;

namespace Akavache
{
    /// <summary>
    /// This class is an IBlobCache backed by a simple in-memory Dictionary.
    /// Use it for testing / mocking purposes
    /// </summary>
    public class InMemoryBlobCache : ISecureBlobCache, IObjectBlobCache, IEnableLogger
    {
        public InMemoryBlobCache() : this(null, null)
        {
        }

        public InMemoryBlobCache(IScheduler scheduler) : this(scheduler, null)
        {
        }

        public InMemoryBlobCache(IEnumerable<KeyValuePair<string, byte[]>> initialContents) : this(null, initialContents)
        {
        }

        public InMemoryBlobCache(IScheduler scheduler, IEnumerable<KeyValuePair<string, byte[]>> initialContents)
        {
            Scheduler = scheduler ?? CurrentThreadScheduler.Instance;
            foreach (var item in initialContents ?? Enumerable.Empty<KeyValuePair<string, byte[]>>())
            {
                cache[item.Key] = new CacheEntry(null, item.Value, Scheduler.Now, null);
            }
        }

        internal InMemoryBlobCache(Action disposer, 
            IScheduler scheduler, 
            IEnumerable<KeyValuePair<string, byte[]>> initialContents)
            : this(scheduler, initialContents)
        {
            inner = Disposable.Create(disposer);
        }

        public IScheduler Scheduler { get; protected set; }

        readonly AsyncSubject<Unit> shutdown = new AsyncSubject<Unit>();
        public IObservable<Unit> Shutdown { get { return shutdown; } }

        readonly IDisposable inner;
        bool disposed;
        Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");

            lock (cache)
            {
                cache[key] = new CacheEntry(null, data, Scheduler.Now, absoluteExpiration);
            }

            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> Flush()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");

            return Observable.Return(Unit.Default);
        }

        public IObservable<byte[]> Get(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>("InMemoryBlobCache");
            
            CacheEntry entry;
            lock (cache)
            {
                if (!cache.TryGetValue(key, out entry))
                {
                    return ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key);
                }
            }

            if(entry.ExpiresAt != null && Scheduler.Now > entry.ExpiresAt.Value)
            {
                cache.Remove(key);
                return ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key);
            }

            return Observable.Return(entry.Value, Scheduler);
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<DateTimeOffset?>("InMemoryBlobCache");

            CacheEntry entry;
            lock (cache)
            {                
                if (!cache.TryGetValue(key, out entry))
                {
                    return Observable.Return<DateTimeOffset?>(null);
                }                
            }
            return Observable.Return<DateTimeOffset?>(entry.CreatedAt, Scheduler);
        }

        public IObservable<IEnumerable<string>> GetAllKeys()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<List<string>>("InMemoryBlobCache");

            lock (cache)
            {
                return Observable.Return(cache
                    .Where(x => x.Value.ExpiresAt == null || x.Value.ExpiresAt >= Scheduler.Now)
                    .Select(x => x.Key)
                    .ToList());
            }
        }

        public IObservable<Unit> Invalidate(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");

            lock (cache)
            {
                cache.Remove(key);
            }

            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> InvalidateAll()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");

            lock (cache)
            {
                cache.Clear();
            }

            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");

            var data = SerializeObject(value);

            lock (cache)
            {
                cache[key] = new CacheEntry(typeof(T).FullName, data, Scheduler.Now, absoluteExpiration);
            }

            return Observable.Return(Unit.Default);
        }

        public IObservable<T> GetObject<T>(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<T>("InMemoryBlobCache");

            CacheEntry entry;
            lock (cache)
            {
                if (!cache.TryGetValue(key, out entry))
                {
                    return ExceptionHelper.ObservableThrowKeyNotFoundException<T>(key);
                }
            }
            if (entry.ExpiresAt != null && Scheduler.Now > entry.ExpiresAt.Value)
            {
                cache.Remove(key);
                return ExceptionHelper.ObservableThrowKeyNotFoundException<T>(key);
            }

            T obj = DeserializeObject<T>(entry.Value);

            return Observable.Return(obj, Scheduler);
        }

        public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
        {
            return this.GetCreatedAt(key);
        }

        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<IEnumerable<T>>("InMemoryBlobCache");

            lock (cache)
            {
                return Observable.Return(cache
                    .Where(x => x.Value.TypeName == typeof(T).FullName && (x.Value.ExpiresAt == null || x.Value.ExpiresAt >= Scheduler.Now))
                    .Select(x => DeserializeObject<T>(x.Value.Value))
                    .ToList(), Scheduler);
            }
        }

        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");

            return this.Invalidate(key);
        }

        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");

            lock (cache)
            {
                var toDelete = cache.Where(x => x.Value.TypeName == typeof(T).FullName).ToArray();
                foreach(var obj in toDelete) cache.Remove(obj.Key);
            }

            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> Vacuum()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");

            lock (cache)
            {
                var toDelete = cache.Where(x => x.Value.ExpiresAt >= Scheduler.Now);
                foreach (var kvp in toDelete) cache.Remove(kvp.Key);
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

            shutdown.OnNext(Unit.Default);
            shutdown.OnCompleted();
            disposed = true;
        }

        byte[] SerializeObject<T>(T value)
        {
            var settings = Locator.Current.GetService<JsonSerializerSettings>() ?? new JsonSerializerSettings();
            var ms = new MemoryStream();
            var serializer = JsonSerializer.Create(settings);
            var writer = new BsonWriter(ms);

            serializer.Serialize(writer, new ObjectWrapper<T>() { Value = value });
            return ms.ToArray();
        }

        T DeserializeObject<T>(byte[] data)
        {
            var settings = Locator.Current.GetService<JsonSerializerSettings>() ?? new JsonSerializerSettings();
            var serializer = JsonSerializer.Create(settings);
            var reader = new BsonReader(new MemoryStream(data));
            var forcedDateTimeKind = BlobCache.ForcedDateTimeKind;

            if (forcedDateTimeKind.HasValue)
            {
                reader.DateTimeKindHandling = forcedDateTimeKind.Value;
            }

            try
            {
                return serializer.Deserialize<ObjectWrapper<T>>(reader).Value;
            }
            catch (Exception ex)
            {
                this.Log().WarnException("Failed to deserialize data as boxed, we may be migrating from an old Akavache", ex);
            }

            return serializer.Deserialize<T>(reader);
        }

        public static InMemoryBlobCache OverrideGlobals(IScheduler scheduler = null, params KeyValuePair<string, byte[]>[] initialContents)
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

            var testCache = new InMemoryBlobCache(resetBlobCache, scheduler, initialContents);
            BlobCache.LocalMachine = testCache;
            BlobCache.Secure = testCache;
            BlobCache.UserAccount = testCache;

            return testCache;
        }

        public static InMemoryBlobCache OverrideGlobals(IDictionary<string, byte[]> initialContents, IScheduler scheduler = null)
        {
            return OverrideGlobals(scheduler, initialContents.ToArray());
        }

        public static InMemoryBlobCache OverrideGlobals(IDictionary<string, object> initialContents, IScheduler scheduler = null)
        {
            var initialSerializedContents = initialContents
                .Select(item => new KeyValuePair<string, byte[]>(item.Key, JsonSerializationMixin.SerializeObject(item.Value)))
                .ToArray();

            return OverrideGlobals(scheduler, initialSerializedContents);
        }
    }

    public class CacheEntry
    {
        public DateTimeOffset CreatedAt { get; protected set; }
        public DateTimeOffset? ExpiresAt { get; protected set; }
        public string TypeName { get; protected set; }
        public byte[] Value { get; protected set; }

        public CacheEntry(string typeName, byte[] value, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
        {
            TypeName = typeName;
            Value = value;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
        }
    }

    interface IObjectWrapper { }
    class ObjectWrapper<T> : IObjectWrapper
    {
        public T Value { get; set; }
    }
}
