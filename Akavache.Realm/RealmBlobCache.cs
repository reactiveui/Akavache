using Realms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive;
using System.Reactive.Linq;
using Splat;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.IO;

namespace Akavache.Realm
{
    public class RealmBlobCache : IObjectBlobCache, IEnableLogger
    {
        public IScheduler Scheduler { get; private set; }

        private Realms.Realm instance;

        public IObservable<Unit> Shutdown
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        static readonly object disposeGate = 42;

        bool disposed = false;

        public RealmBlobCache(string databaseFile, IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? BlobCache.TaskpoolScheduler;

            BlobCache.EnsureInitialized();

            this.instance = Realms.Realm.GetInstance(databaseFile);
        }

        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = default(DateTimeOffset?))
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>(nameof(RealmBlobCache));
            if (key == null) return Observable.Throw<Unit>(new ArgumentNullException());

            var data = SerializeObject(value);
            var exp = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;
            var createdAt = Scheduler.Now.UtcDateTime;

            return this.BeforeWriteToDiskFilter(data, this.Scheduler)
                .SelectMany(encData => Observable.Start(() =>
                {
                    this.instance.Write(() =>
                    {
                        var element = this.instance.CreateObject<CacheElement>();
                        element.TypeName = typeof(T).FullName;
                        element.Key = key;
                        element.Value = encData;
                        element.CreatedAt = createdAt;
                        element.Expiration = exp;
                    });
                }))
                .PublishLast()
                .PermaRef();
        }

        public IObservable<T> GetObject<T>(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<T>(nameof(RealmBlobCache));
            if (key == null) return Observable.Throw<T>(new ArgumentNullException());

            return Observable.Start(() => this.instance.All<CacheElement>().FirstOrDefault(x => x.Key == key), this.Scheduler)
                .SelectMany(x => x == null ? ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key) : Observable.Return(x.Value))
                .SelectMany(x => this.AfterReadFromDiskFilter(x, this.Scheduler))
                .SelectMany(x => DeserializeObject<T>(x))
                .PublishLast()
                .PermaRef();
        }

        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<IEnumerable<T>>(nameof(RealmBlobCache));

            return Observable.Start(() => this.instance.All<CacheElement>().Where(x => x.TypeName == typeof(T).FullName), this.Scheduler)
                .SelectMany(x => x.ToObservable()
                    .SelectMany(y => AfterReadFromDiskFilter(y.Value, Scheduler))
                    .SelectMany(y => DeserializeObject<T>(y))
                    .ToList())
                .PublishLast()
                .PermaRef();
        }

        public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
        {
            return this.GetCreatedAt(key);
        }

        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>(nameof(RealmBlobCache));

            return this.Invalidate(key);
        }

        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>(nameof(RealmBlobCache));

            return Observable.Start(() => this.instance.All<CacheElement>().Where(x => x.TypeName == typeof(T).FullName), this.Scheduler)
                .SelectMany(x => Observable.Start(() => this.instance.Write(() =>
                {
                    foreach(var element in x)
                    {
                        this.instance.Remove(element);
                    }
                 }), this.Scheduler))
                .DefaultIfEmpty()
                .PublishLast()
                .PermaRef();
        }

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = default(DateTimeOffset?))
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>(nameof(RealmBlobCache));
            if (key == null) return Observable.Throw<Unit>(new ArgumentNullException());

            var exp = absoluteExpiration ?? DateTimeOffset.MaxValue;
            var createdAt = Scheduler.Now;

            return this.BeforeWriteToDiskFilter(data, this.Scheduler)
                .SelectMany(encData => Observable.Start(() =>
                {
                    this.instance.Write(() =>
                    {
                        var element = this.instance.CreateObject<CacheElement>();
                        element.Key = key;
                        element.Value = encData;
                        element.CreatedAt = createdAt;
                        element.Expiration = exp;
                    });
                }))
                .PublishLast()
                .PermaRef();
        }

        public IObservable<byte[]> Get(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>(nameof(RealmBlobCache));
            if (key == null) return Observable.Throw<byte[]>(new ArgumentNullException());

            return Observable.Start(() => this.instance.All<CacheElement>().FirstOrDefault(x => x.Key == key), this.Scheduler)
                .SelectMany(x => x == null ? ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key) : Observable.Return(x.Value))
                .SelectMany(x => this.AfterReadFromDiskFilter(x, this.Scheduler))
                .PublishLast()
                .PermaRef();
        }

        public IObservable<IEnumerable<string>> GetAllKeys()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<IEnumerable<string>>(nameof(RealmBlobCache));

            return Observable.Start(() => this.instance.All<CacheElement>().Select(x => x.Key).ToList())
                .PublishLast()
                .PermaRef();
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<DateTimeOffset?>(nameof(RealmBlobCache));
            if (key == null) return Observable.Throw<DateTimeOffset?>(new ArgumentNullException());

            return Observable.Start(() => this.instance.All<CacheElement>().FirstOrDefault(x => x.Key == key), this.Scheduler)
                .Select(x => x == null ? default(DateTimeOffset?) : x.CreatedAt)
                .PublishLast()
                .PermaRef();
        }

        public IObservable<Unit> Flush()
        {
            return Observable.Return(Unit.Default); 
        }

        public IObservable<Unit> Invalidate(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>(nameof(RealmBlobCache));

            return Observable.Start(() => this.instance.All<CacheElement>().FirstOrDefault(x => x.Key == key), this.Scheduler)
                .Where(x => x != null)
                .SelectMany(x => Observable.Start(() => this.instance.Write(() => this.instance.Remove(x)), this.Scheduler))
                .DefaultIfEmpty()
                .PublishLast()
                .PermaRef();
        }

        public IObservable<Unit> InvalidateAll()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>(nameof(RealmBlobCache));

            return Observable.Start(() => this.instance.RemoveAll())
                .PublishLast()
                .PermaRef();
        }

        public IObservable<Unit> Vacuum()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
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
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>(nameof(RealmBlobCache));

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
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>(nameof(RealmBlobCache));

            return Observable.Return(data);
        }

        // TODO: Move this and the SQLite equvivalent into a commin place
        byte[] SerializeObject<T>(T value)
        {
            var settings = Locator.Current.GetService<JsonSerializerSettings>() ?? new JsonSerializerSettings();
            var ms = new MemoryStream();
            var serializer = JsonSerializer.Create(settings);
            var writer = new BsonWriter(ms);

            serializer.Serialize(writer, new ObjectWrapper<T>() { Value = value });
            return ms.ToArray();
        }

        IObservable<T> DeserializeObject<T>(byte[] data)
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
                try
                {
                    var boxedVal = serializer.Deserialize<ObjectWrapper<T>>(reader).Value;
                    return Observable.Return(boxedVal);
                }
                catch (Exception ex)
                {
                    this.Log().WarnException("Failed to deserialize data as boxed, we may be migrating from an old Akavache", ex);
                }

                var rawVal = serializer.Deserialize<T>(reader);
                return Observable.Return(rawVal);
            }
            catch (Exception ex)
            {
                return Observable.Throw<T>(ex);
            }
        }
    }

    class CacheElement : RealmObject
    {
        [ObjectId]
        public string Key { get; set; }

        public string TypeName { get; set; }
        public byte[] Value { get; set; }
        public DateTimeOffset Expiration { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    interface IObjectWrapper { }
    class ObjectWrapper<T> : IObjectWrapper
    {
        public T Value { get; set; }
    }
}
