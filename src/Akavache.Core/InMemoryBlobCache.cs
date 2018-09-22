using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Splat;

namespace Akavache
{
    /// <summary>
    /// This class is an IBlobCache backed by a simple in-memory Dictionary. Use it for testing /
    /// mocking purposes
    /// </summary>
    public class InMemoryBlobCache : ISecureBlobCache, IObjectBlobCache, IEnableLogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
        /// </summary>
        public InMemoryBlobCache() : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
        /// </summary>
        /// <param name="scheduler">The scheduler.</param>
        public InMemoryBlobCache(IScheduler scheduler) : this(scheduler, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
        /// </summary>
        /// <param name="initialContents">The initial contents.</param>
        public InMemoryBlobCache(IEnumerable<KeyValuePair<string, byte[]>> initialContents) : this(null, initialContents)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
        /// </summary>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="initialContents">The initial contents.</param>
        public InMemoryBlobCache(IScheduler scheduler, IEnumerable<KeyValuePair<string, byte[]>> initialContents)
        {
            Scheduler = scheduler ?? CurrentThreadScheduler.Instance;
            foreach (var item in initialContents ?? Enumerable.Empty<KeyValuePair<string, byte[]>>()) {
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

        /// <summary>
        /// The IScheduler used to defer operations. By default, this is BlobCache.TaskpoolScheduler.
        /// </summary>
        public IScheduler Scheduler { get; protected set; }

        private readonly AsyncSubject<Unit> shutdown = new AsyncSubject<Unit>();

        /// <summary>
        /// This Observable fires after the Dispose completes successfully, since there is no such
        /// thing as an AsyncDispose().
        /// </summary>
        public IObservable<Unit> Shutdown { get { return shutdown; } }

        private readonly IDisposable inner;
        private bool disposed;
        private Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();

        /// <summary>
        /// Insert a blob into the cache with the specified key and expiration date.
        /// </summary>
        /// <param name="key">The key to use for the data.</param>
        /// <param name="data">The data to save in the cache.</param>
        /// <param name="absoluteExpiration">
        /// An optional expiration date. After the specified date, the key-value pair should be removed.
        /// </param>
        /// <returns>A signal to indicate when the key has been inserted.</returns>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            lock (cache) {
                cache[key] = new CacheEntry(null, data, Scheduler.Now, absoluteExpiration);
            }

            return Observable.Return(Unit.Default);
        }

        /// <summary>
        /// This method guarantees that all in-flight inserts have completed and any indexes have
        /// been written to disk.
        /// </summary>
        /// <returns>A signal indicating when the flush is complete.</returns>
        public IObservable<Unit> Flush()
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            return Observable.Return(Unit.Default);
        }

        /// <summary>
        /// Retrieve a value from the key-value cache. If the key is not in the cache, this method
        /// should return an IObservable which OnError's with KeyNotFoundException.
        /// </summary>
        /// <param name="key">The key to return asynchronously.</param>
        /// <returns>A Future result representing the byte data.</returns>
        public IObservable<byte[]> Get(string key)
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>("InMemoryBlobCache");
            }

            CacheEntry entry;
            lock (cache) {
                if (!cache.TryGetValue(key, out entry)) {
                    return ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key);
                }
            }

            if (entry.ExpiresAt != null && Scheduler.Now > entry.ExpiresAt.Value) {
                cache.Remove(key);
                return ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key);
            }

            return Observable.Return(entry.Value, Scheduler);
        }

        /// <summary>
        /// Returns the time that the key was added to the cache, or returns null if the key isn't in
        /// the cache.
        /// </summary>
        /// <param name="key">The key to return the date for.</param>
        /// <returns>The date the key was created on.</returns>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<DateTimeOffset?>("InMemoryBlobCache");
            }

            CacheEntry entry;
            lock (cache) {
                if (!cache.TryGetValue(key, out entry)) {
                    return Observable.Return<DateTimeOffset?>(null);
                }
            }
            return Observable.Return<DateTimeOffset?>(entry.CreatedAt, Scheduler);
        }

        /// <summary>
        /// Return all keys in the cache. Note that this method is normally for diagnostic / testing
        /// purposes, and that it is not guaranteed to be accurate with respect to in-flight requests.
        /// </summary>
        /// <returns>A list of valid keys for the cache.</returns>
        public IObservable<IEnumerable<string>> GetAllKeys()
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<List<string>>("InMemoryBlobCache");
            }

            lock (cache) {
                return Observable.Return(cache
                    .Where(x => x.Value.ExpiresAt == null || x.Value.ExpiresAt >= Scheduler.Now)
                    .Select(x => x.Key)
                    .ToList());
            }
        }

        /// <summary>
        /// Remove a key from the cache. If the key doesn't exist, this method should do nothing and
        /// return (*not* throw KeyNotFoundException).
        /// </summary>
        /// <param name="key">The key to remove from the cache.</param>
        /// <returns></returns>
        public IObservable<Unit> Invalidate(string key)
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            lock (cache) {
                cache.Remove(key);
            }

            return Observable.Return(Unit.Default);
        }

        /// <summary>
        /// Invalidate all entries in the cache (i.e. clear it). Note that this method is blocking
        /// and incurs a significant performance penalty if used while the cache is being used on
        /// other threads.
        /// </summary>
        /// <returns>A signal indicating when the invalidate is complete.</returns>
        public IObservable<Unit> InvalidateAll()
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            lock (cache) {
                cache.Clear();
            }

            return Observable.Return(Unit.Default);
        }

        /// <summary>
        /// Insert an object into the cache, via the JSON serializer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the completion of the insert.</returns>
        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            var data = SerializeObject(value);

            lock (cache) {
                cache[key] = new CacheEntry(typeof(T).FullName, data, Scheduler.Now, absoluteExpiration);
            }

            return Observable.Return(Unit.Default);
        }

        /// <summary>
        /// Get an object from the cache and deserialize it via the JSON serializer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key to look up in the cache.</param>
        /// <returns>A Future result representing the object in the cache.</returns>
        public IObservable<T> GetObject<T>(string key)
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<T>("InMemoryBlobCache");
            }

            CacheEntry entry;
            lock (cache) {
                if (!cache.TryGetValue(key, out entry)) {
                    return ExceptionHelper.ObservableThrowKeyNotFoundException<T>(key);
                }
            }
            if (entry.ExpiresAt != null && Scheduler.Now > entry.ExpiresAt.Value) {
                cache.Remove(key);
                return ExceptionHelper.ObservableThrowKeyNotFoundException<T>(key);
            }

            var obj = DeserializeObject<T>(entry.Value);

            return Observable.Return(obj, Scheduler);
        }

        /// <summary>
        /// Returns the time that the object with the key was added to the cache, or returns null if
        /// the key isn't in the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key to return the date for.</param>
        /// <returns>The date the key was created on.</returns>
        public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
        {
            return GetCreatedAt(key);
        }

        /// <summary>
        /// Return all objects of a specific Type in the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// A Future result representing all objects in the cache with the specified Type.
        /// </returns>
        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<IEnumerable<T>>("InMemoryBlobCache");
            }

            lock (cache) {
                return Observable.Return(cache
                    .Where(x => x.Value.TypeName == typeof(T).FullName && (x.Value.ExpiresAt == null || x.Value.ExpiresAt >= Scheduler.Now))
                    .Select(x => DeserializeObject<T>(x.Value.Value))
                    .ToList(), Scheduler);
            }
        }

        /// <summary>
        /// Invalidates a single object from the cache. It is important that the Type Parameter for
        /// this method be correct, and you cannot use IBlobCache.Invalidate to perform the same task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key to invalidate.</param>
        /// <returns>A Future result representing the completion of the invalidation.</returns>
        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            return Invalidate(key);
        }

        /// <summary>
        /// Invalidates all objects of the specified type. To invalidate all objects regardless of
        /// type, use InvalidateAll.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>A Future result representing the completion of the invalidation.</returns>
        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            lock (cache) {
                var toDelete = cache.Where(x => x.Value.TypeName == typeof(T).FullName).ToArray();
                foreach (var obj in toDelete) {
                    cache.Remove(obj.Key);
                }
            }

            return Observable.Return(Unit.Default);
        }

        /// <summary>
        /// This method eagerly removes all expired keys from the blob cache, as well as does any
        /// cleanup operations that makes sense (Hint: on SQLite3 it does a Vacuum)
        /// </summary>
        /// <returns>A signal indicating when the operation is complete.</returns>
        public IObservable<Unit> Vacuum()
        {
            if (disposed) {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            lock (cache) {
                var toDelete = cache.Where(x => x.Value.ExpiresAt >= Scheduler.Now);
                foreach (var kvp in toDelete) {
                    cache.Remove(kvp.Key);
                }
            }

            return Observable.Return(Unit.Default);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Scheduler = null;
            cache = null;
            if (inner != null) {
                inner.Dispose();
            }

            shutdown.OnNext(Unit.Default);
            shutdown.OnCompleted();
            disposed = true;
        }

        private byte[] SerializeObject<T>(T value)
        {
            var settings = Locator.Current.GetService<JsonSerializerSettings>() ?? new JsonSerializerSettings();
            var ms = new MemoryStream();
            var serializer = JsonSerializer.Create(settings);
            var writer = new BsonWriter(ms);

            serializer.Serialize(writer, new ObjectWrapper<T>() { Value = value });
            return ms.ToArray();
        }

        private T DeserializeObject<T>(byte[] data)
        {
            var settings = Locator.Current.GetService<JsonSerializerSettings>() ?? new JsonSerializerSettings();
            var serializer = JsonSerializer.Create(settings);
            var reader = new BsonReader(new MemoryStream(data));
            var forcedDateTimeKind = BlobCache.ForcedDateTimeKind;

            if (forcedDateTimeKind.HasValue) {
                reader.DateTimeKindHandling = forcedDateTimeKind.Value;
            }

            try {
                return serializer.Deserialize<ObjectWrapper<T>>(reader).Value;
            } catch (Exception ex) {
                this.Log().WarnException("Failed to deserialize data as boxed, we may be migrating from an old Akavache", ex);
            }

            return serializer.Deserialize<T>(reader);
        }

        /// <summary>
        /// Overrides the globals.
        /// </summary>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="initialContents">The initial contents.</param>
        /// <returns></returns>
        public static InMemoryBlobCache OverrideGlobals(IScheduler scheduler = null, params KeyValuePair<string, byte[]>[] initialContents)
        {
            var local = BlobCache.LocalMachine;
            var user = BlobCache.UserAccount;
            var sec = BlobCache.Secure;

            var resetBlobCache = new Action(() => {
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

        /// <summary>
        /// Overrides the globals.
        /// </summary>
        /// <param name="initialContents">The initial contents.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
        public static InMemoryBlobCache OverrideGlobals(IDictionary<string, byte[]> initialContents, IScheduler scheduler = null)
        {
            return OverrideGlobals(scheduler, initialContents.ToArray());
        }

        /// <summary>
        /// Overrides the globals.
        /// </summary>
        /// <param name="initialContents">The initial contents.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
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
        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        /// <value>The created at.</value>
        public DateTimeOffset CreatedAt { get; protected set; }

        /// <summary>
        /// Gets or sets the expires at.
        /// </summary>
        /// <value>The expires at.</value>
        public DateTimeOffset? ExpiresAt { get; protected set; }

        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        /// <value>The name of the type.</value>
        public string TypeName { get; protected set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public byte[] Value { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheEntry"/> class.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="value">The value.</param>
        /// <param name="createdAt">The created at.</param>
        /// <param name="expiresAt">The expires at.</param>
        public CacheEntry(string typeName, byte[] value, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
        {
            TypeName = typeName;
            Value = value;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
        }
    }

    internal interface IObjectWrapper { }

    internal class ObjectWrapper<T> : IObjectWrapper
    {
        public T Value { get; set; }
    }
}
