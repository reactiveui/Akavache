using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Akavache;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using ReactiveUI;
using SQLite;

namespace Akavache.Sqlite3
{
    public class SqlitePersistentBlobCache : IObjectBlobCache
    {
        public IScheduler Scheduler { get; private set; }
        public IServiceProvider ServiceProvider { get; private set; }

        readonly SQLiteAsyncConnection _connection;
        readonly MemoizingMRUCache<string, IObservable<CacheElement>> _inflightCache;
        readonly DateTimeOffset _maxDateTime;
        bool disposed = false;

        public SqlitePersistentBlobCache(string databaseFile, IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? RxApp.TaskpoolScheduler;

            _connection = new SQLiteAsyncConnection(databaseFile, true);
            _connection.CreateTableAsync<CacheElement>();

            _maxDateTime = new DateTimeOffset(DateTime.MaxValue, TimeSpan.Zero);

            _inflightCache = new MemoizingMRUCache<string, IObservable<CacheElement>>((key, _) =>
            {
                return _connection.QueryAsync<CacheElement>("SELECT * FROM CacheElement WHERE Key = ? LIMIT 1;", key)
                    .SelectMany(x =>
                    {
                        return (x.Count == 1) ?
                            Observable.Return(x[0]) : Observable.Throw<CacheElement>(new KeyNotFoundException());
                    })
                    .SelectMany(x =>
                    {
                        if (x.Expiration < Scheduler.Now.UtcDateTime) 
                        {
                            Invalidate(key);
                            return Observable.Throw<CacheElement>(new KeyNotFoundException());
                        }
                        else 
                        {
                            return Observable.Return(x);
                        }
                    });
            }, 10);
        }

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            lock (_inflightCache) _inflightCache.Invalidate(key);

            var element = new CacheElement()
            {
                Expiration = absoluteExpiration != null ? absoluteExpiration.Value.UtcDateTime : DateTime.MaxValue,
                Key = key,
                Value = data,
            };

            return _connection.InsertAsync(element, "OR REPLACE", typeof(CacheElement)).Select(_ => Unit.Default);
        }

        public IObservable<byte[]> GetAsync(string key)
        {
            if (disposed) return Observable.Throw<byte[]>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            lock (_inflightCache) {
                return _inflightCache.Get(key)
                    .Select(x => x.Value)
                    .Finally(() => _inflightCache.Invalidate(key));
            }
        }

        public IEnumerable<string> GetAllKeys()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");

            return _connection.QueryAsync<CacheElement>("SELECT Key FROM CacheElement;")
                .First()
                .Select(x => x.Key)
                .ToArray();
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (disposed) return Observable.Throw<DateTimeOffset?>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            lock (_inflightCache) 
            {
                return _inflightCache.Get(key)
                    .Select(x => x.Expiration == DateTime.MaxValue ?
                        default(DateTimeOffset?) : new DateTimeOffset(x.Expiration, TimeSpan.Zero))
                    .Finally(() => _inflightCache.Invalidate(key));
            }           
        }

        public IObservable<Unit> Flush()
        {
            // NB: We don't need to sync metadata when using SQLite3
            return Observable.Return(Unit.Default);
        }

        public void Invalidate(string key)
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            lock(_inflightCache) _inflightCache.Invalidate(key);
            _connection.ExecuteAsync("DELETE FROM CacheElement WHERE Key=?;", key);
        }

        public void InvalidateAll()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            lock(_inflightCache) _inflightCache.InvalidateAll();
            _connection.ExecuteAsync("DELETE FROM CacheElement;");
        }

        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            var ms = new MemoryStream();
            var serializer = new JsonSerializer();
            var writer = new BsonWriter(ms);

            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            serializer.Serialize(writer, value);

            lock (_inflightCache) _inflightCache.Invalidate(key);

            var element = new CacheElement()
            {
                Expiration = absoluteExpiration != null ? absoluteExpiration.Value.UtcDateTime : DateTime.MaxValue,
                Key = key,
                Value = ms.ToArray(),
                TypeName = typeof(T).FullName
            };

            return _connection.InsertAsync(element, "OR REPLACE", typeof(CacheElement)).Select(_ => Unit.Default);
        }

        public IObservable<T> GetObjectAsync<T>(string key, bool noTypePrefix = false)
        {
            if (disposed) return Observable.Throw<T>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            lock (_inflightCache) 
            {
                var ret = _inflightCache.Get(key);
                return ret.SelectMany(x => DeserializeObject<T>(x.Value));
            }
        }

        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            if (disposed) return Observable.Throw<IEnumerable<T>>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            return _connection.QueryAsync<CacheElement>("SELECT * FROM CacheElement WHERE TypeName = ?;", typeof(T).FullName)
                .SelectMany(x => x.ToObservable())
                .SelectMany(x => DeserializeObject<T>(x.Value))
                .ToList();
        }

        public void InvalidateObject<T>(string key)
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            Invalidate(key);
        }

        public void InvalidateAllObjects<T>()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            _connection.ExecuteAsync("DELETE * FROM CacheElement WHERE TypeName = ?;", typeof(T).FullName);
        }

        public void Dispose()
        {
            SQLiteConnectionPool.Shared.Reset();
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
            if (disposed) return Observable.Throw<byte[]>(new ObjectDisposedException("SqlitePersistentBlobCache"));

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
            if (disposed) return Observable.Throw<byte[]>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            return Observable.Return(data);
        }


        IObservable<T> DeserializeObject<T>(byte[] data)
        {
            var serializer = new JsonSerializer();
            var reader = new BsonReader(new MemoryStream(data));
            if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                reader.ReadRootValueAsArray = true;
            }

            try 
            {
                var val = serializer.Deserialize<T>(reader);
                return Observable.Return(val);
            }
            catch (Exception ex) 
            {
                return Observable.Throw<T>(ex);
            }           
        }

    }

    class CacheElement
    {
        [PrimaryKey]
        public string Key { get; set; }

        public string TypeName { get; set; }
        public byte[] Value { get; set; }
        public DateTime Expiration { get; set; }
    }
}
