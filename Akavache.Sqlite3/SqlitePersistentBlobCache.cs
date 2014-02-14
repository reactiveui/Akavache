using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Akavache.Sqlite3.Internal;
using ReactiveUI;
using System.Threading.Tasks;

namespace Akavache.Sqlite3
{
    /// <summary>
    /// This class represents an IBlobCache backed by a SQLite3 database, and
    /// it is the default (and best!) implementation.
    /// </summary>
    public class SqlitePersistentBlobCache : IObjectBlobCache, IEnableLogger
    {
        public IScheduler Scheduler { get; private set; }
        public SQLiteAsyncConnection Connection { get; private set; }
        public IServiceProvider ServiceProvider { get; private set; }

        readonly MemoizingMRUCache<string, IObservable<CacheElement>> _inflightCache;
        readonly IObservable<Unit> _initializer;
        bool disposed = false;

        public SqlitePersistentBlobCache(string databaseFile, IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? RxApp.TaskpoolScheduler;

            Connection = new SQLiteAsyncConnection(databaseFile, storeDateTimeAsTicks: true);

            _initializer = Initialize();

            _inflightCache = new MemoizingMRUCache<string, IObservable<CacheElement>>((key, ce) =>
            {
                return _initializer
                    .SelectMany(_ => Connection.QueryAsync<CacheElement>("SELECT * FROM CacheElement WHERE Key=? LIMIT 1;", key))
                    .SelectMany(x =>
                    {
                        return (x.Count == 1) ?  Observable.Return(x[0]) : ObservableThrowKeyNotFoundException(key);
                    })
                    .SelectMany(x =>
                    {
                        if (x.Expiration < Scheduler.Now.UtcDateTime) 
                        {
                            return Invalidate(key).SelectMany(_ => ObservableThrowKeyNotFoundException(key));
                        }
                        else 
                        {
                            return Observable.Return(x);
                        }
                    });
            }, 10);
        }

        readonly AsyncSubject<Unit> shutdown = new AsyncSubject<Unit>();
        public IObservable<Unit> Shutdown { get { return shutdown; } }

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            lock (_inflightCache) _inflightCache.Invalidate(key);

            var element = new CacheElement()
            {
                Expiration = absoluteExpiration != null ? absoluteExpiration.Value.UtcDateTime : DateTime.MaxValue,
                Key = key,
                CreatedAt = RxApp.TaskpoolScheduler.Now.UtcDateTime,
            };

            var ret = _initializer
                .SelectMany(_ => BeforeWriteToDiskFilter(data, Scheduler))
                .Do(x => element.Value = x)
                .SelectMany(x => Connection.InsertAsync(element, "OR REPLACE", typeof(CacheElement)).Select(_ => Unit.Default))
                .Multicast(new AsyncSubject<Unit>());

            ret.Connect();
            return ret;
        }

        public IObservable<Unit> Insert(IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            lock (_inflightCache) 
            {
                foreach(var kvp in keyValuePairs) _inflightCache.Invalidate(kvp.Key);
            }

            var elements = keyValuePairs.Select(kvp => new CacheElement()
            {
                Expiration = absoluteExpiration != null ? absoluteExpiration.Value.UtcDateTime : DateTime.MaxValue,
                Key = kvp.Key,
                Value = kvp.Value,
                CreatedAt = RxApp.TaskpoolScheduler.Now.UtcDateTime,
            }).ToList();

            var encryptAllTheData = elements.ToObservable()
                .Select(x => Observable.Defer(() => BeforeWriteToDiskFilter(x.Value, Scheduler))
                    .Do(y => x.Value = y))
                .Merge(4)
                .TakeLast(1);

            var ret = encryptAllTheData
                .SelectMany(_ => _initializer)
                .SelectMany(_ => Connection.InsertAllAsync(elements, "OR REPLACE").Select(__ => Unit.Default))
                .Multicast(new AsyncSubject<Unit>());

            ret.Connect();
            return ret;
        }

        public IObservable<byte[]> GetAsync(string key)
        {
            if (disposed) return Observable.Throw<byte[]>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            lock (_inflightCache) 
            {
                return _inflightCache.Get(key)
                    .Select(x => x.Value)
                    .SelectMany(x => AfterReadFromDiskFilter(x, Scheduler))
                    .Finally(() => { lock(_inflightCache) { _inflightCache.Invalidate(key); } } );
            }
        }

        public IObservable<IDictionary<string, byte[]>> GetAsync(IEnumerable<string> keys)
        {
            if (disposed) return Observable.Throw<IDictionary<string, byte[]>>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            string questionMarks = String.Join(",", keys.Select(_ => "?"));
            return _initializer
                .SelectMany(_ => Connection.QueryAsync<CacheElement>(String.Format("SELECT * FROM CacheElement WHERE Key IN ({0});", questionMarks), keys.ToArray()))
                .SelectMany(xs =>
                {
                    var invalidXs = xs.Where(x => x.Expiration < Scheduler.Now.UtcDateTime).ToList();

                    var invalidate = (invalidXs.Count > 0) ?
                        Invalidate(invalidXs.Select(x => x.Key)) :
                        Observable.Return(Unit.Default);
                                        
                    var validXs = xs.Where(x => x.Expiration >= Scheduler.Now.UtcDateTime).ToList();

                    return invalidate.SelectMany(_ => validXs.ToObservable())
                        .Select(x => Observable.Defer(() => AfterReadFromDiskFilter(x.Value, Scheduler)
                            .Do(y => x.Value = y)))
                        .Merge(4) 
                        .Aggregate(Unit.Default, (acc,x) => acc)
                        .Select(_ => validXs.ToDictionary(k => k.Key, v => v.Value));
                });
        }

        public IEnumerable<string> GetAllKeys()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");

            var now = RxApp.TaskpoolScheduler.Now.UtcTicks;
            return _initializer
                .SelectMany(_ => Connection.QueryAsync<CacheElement>("SELECT Key FROM CacheElement WHERE Expiration >= ?;", now))
                .Select(x => x.Select(y => y.Key).ToList())
                .First();
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (disposed) return Observable.Throw<DateTimeOffset?>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            lock (_inflightCache) 
            {
                return _inflightCache.Get(key)
                    .Select(x => x.CreatedAt == DateTime.MaxValue ?
                        default(DateTimeOffset?) : new DateTimeOffset(x.CreatedAt, TimeSpan.Zero))
                    .Catch<DateTimeOffset?, KeyNotFoundException>(_ => Observable.Return(default(DateTimeOffset?)))
                    .Finally(() => { lock(_inflightCache) { _inflightCache.Invalidate(key); } } );
            }           
        }

        public IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(IEnumerable<string> keys)
        {
            if (disposed) return Observable.Throw<IDictionary<string, DateTimeOffset?>>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            var questionMarks = String.Join(",", keys.Select(_ => "?"));
            return _initializer
                .SelectMany(_ => Connection.QueryAsync<CacheElement>(String.Format("SELECT * FROM CacheElement WHERE Key IN ({0});", questionMarks), keys.ToArray()))
                .SelectMany(xs =>
                {
                    var invalidXs = xs.Where(x => x.Expiration < Scheduler.Now.UtcDateTime).ToList();

                    var invalidate = (invalidXs.Count > 0) ?
                        Invalidate(invalidXs.Select(x => x.Key)) :
                        Observable.Return(Unit.Default);

                    return invalidate.Select(_ =>
                        xs.Where(x => x.Expiration >= Scheduler.Now.UtcDateTime)
                            .ToDictionary(k => k.Key, v => new DateTimeOffset?(new DateTimeOffset(v.Expiration))));
                });
        }

        public IObservable<Unit> Flush()
        {
            // NB: We don't need to sync metadata when using SQLite3
            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> Invalidate(string key)
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            lock(_inflightCache) _inflightCache.Invalidate(key);
            return _initializer.SelectMany(__ => 
                Connection.ExecuteAsync("DELETE FROM CacheElement WHERE Key=?;", key).Select(_ => Unit.Default));
        }

        public IObservable<Unit> Invalidate(IEnumerable<string> keys)
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            lock (_inflightCache) foreach (var v in keys) { _inflightCache.Invalidate(v); }

            var questionMarks = String.Join(",", keys.Select(_ => "?"));
            return _initializer.SelectMany(__ => 
                Connection.ExecuteAsync(String.Format("DELETE FROM CacheElement WHERE Key IN ({0});", questionMarks), keys.ToArray()).Select(_ => Unit.Default));
        }

        public IObservable<Unit> InvalidateAll()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            lock(_inflightCache) _inflightCache.InvalidateAll();
            return _initializer.SelectMany(__ => 
                Connection.ExecuteAsync("DELETE FROM CacheElement;").Select(_ => Unit.Default));
        }

        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            var data = SerializeObject(value);

            lock (_inflightCache) _inflightCache.Invalidate(key);

            var element = new CacheElement()
            {
                Expiration = absoluteExpiration != null ? absoluteExpiration.Value.UtcDateTime : DateTime.MaxValue,
                Key = key,
                TypeName = typeof(T).FullName,
                CreatedAt = RxApp.TaskpoolScheduler.Now.UtcDateTime,
            };

            var ret = _initializer
                .SelectMany(_ => BeforeWriteToDiskFilter(data, Scheduler))
                .Do(x => element.Value = x)
                .SelectMany(x => Connection.InsertAsync(element, "OR REPLACE", typeof(CacheElement)).Select(_ => Unit.Default))
                .Multicast(new AsyncSubject<Unit>());

            ret.Connect();
            return ret;
        }

        public IObservable<Unit> InsertObjects<T>(IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            lock (_inflightCache) 
            {
                foreach(var kvp in keyValuePairs) _inflightCache.Invalidate(kvp.Key);
            }

            var serializedElements = Observable.Start(() =>
            {
                return keyValuePairs.Select(x => new CacheElement()
                {
                    Expiration = absoluteExpiration != null ? absoluteExpiration.Value.UtcDateTime : DateTime.MaxValue,
                    Key = x.Key,
                    TypeName = typeof(T).FullName,
                    Value = SerializeObject<T>(x.Value),
                    CreatedAt = RxApp.TaskpoolScheduler.Now.UtcDateTime,
                }).ToList();
            }, Scheduler);

            var ret = serializedElements
                .SelectMany(x => x.ToObservable())
                .Select(x => Observable.Defer(() => BeforeWriteToDiskFilter(x.Value, Scheduler))
                    .Select(y => { x.Value = y; return x; }))
                .Merge(4)
                .ToList()
                .SelectMany(x => Connection.InsertAllAsync(x, "OR REPLACE").Select(_ => Unit.Default))
                .Multicast(new AsyncSubject<Unit>());

            return _initializer.SelectMany(_ => ret.PermaRef());
        }

        public IObservable<T> GetObjectAsync<T>(string key, bool noTypePrefix = false)
        {
            if (disposed) return Observable.Throw<T>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            lock (_inflightCache) 
            {
                var ret = _inflightCache.Get(key);
                return ret
                    .SelectMany(x => AfterReadFromDiskFilter(x.Value, Scheduler))
                    .SelectMany(x => DeserializeObject<T>(x, this.ServiceProvider ?? BlobCache.ServiceProvider))
                    .Multicast(new AsyncSubject<T>())
                    .PermaRef();
            }
        }

        public IObservable<IDictionary<string, T>> GetObjectsAsync<T>(IEnumerable<string> keys, bool noTypePrefix = false)
        {
            if (disposed) return Observable.Throw<IDictionary<string, T>>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            string questionMarks = String.Join(",", keys.Select(_ => "?"));
            return _initializer
                .SelectMany(_ => Connection.QueryAsync<CacheElement>(String.Format("SELECT * FROM CacheElement WHERE Key IN ({0});", questionMarks), keys.ToArray()))
                .SelectMany(xs =>
                {
                    var invalidXs = xs.Where(x => x.Expiration < Scheduler.Now.UtcDateTime).ToList();
                    var invalidate = (invalidXs.Count > 0) ?
                        Invalidate(invalidXs.Select(x => x.Key)) :
                        Observable.Return(Unit.Default);

                    var validXs = xs.Where(x => x.Expiration >= Scheduler.Now.UtcDateTime).ToList();

                    if (validXs.Count == 0)
                    {
                        return invalidate.Select(_ => new Dictionary<string, T>());
                    }

                    return validXs.ToObservable()
                        .SelectMany(x => AfterReadFromDiskFilter(x.Value, Scheduler)
                            .Select(val => { x.Value = val; return x; }))
                        .SelectMany(x => DeserializeObject<T>(x.Value, this.ServiceProvider ?? BlobCache.ServiceProvider)
                            .Select(val => new { Key = x.Key, Value = val }))
                        .ToDictionary(k => k.Key, v => v.Value);
                })
                .Multicast(new AsyncSubject<IDictionary<string, T>>())
                .PermaRef();
        }

        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            if (disposed) return Observable.Throw<IEnumerable<T>>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            return _initializer
                .SelectMany(_ => Connection.QueryAsync<CacheElement>("SELECT * FROM CacheElement WHERE TypeName=?;", typeof(T).FullName))
                .SelectMany(x => x.ToObservable())
                .SelectMany(x => AfterReadFromDiskFilter(x.Value, Scheduler))
                .SelectMany(x => DeserializeObject<T>(x, this.ServiceProvider ?? BlobCache.ServiceProvider))
                .ToList();
        }

        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            return Invalidate(key);
        }

        public IObservable<Unit> InvalidateObjects<T>(IEnumerable<string> keys)
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            return Invalidate(keys);
        }

        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            return _initializer
                .SelectMany(_ => Connection.ExecuteAsync("DELETE FROM CacheElement WHERE TypeName=?;", typeof(T).FullName))
                .Select(_ => Unit.Default);
        }

        public IObservable<Unit> Vacuum()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");

            var nowTime = RxApp.TaskpoolScheduler.Now.UtcTicks;
            return _initializer
                .SelectMany(_ => Connection.ExecuteAsync("DELETE FROM CacheElement WHERE Expiration < ?;", nowTime))
                .SelectMany(_ => Observable.Defer(() => Connection.ExecuteAsync("VACUUM;", nowTime).Retry(3)))
                .Select(_ => Unit.Default);
        }

        public void Dispose()
        {
            Connection.Shutdown()
                .Multicast(shutdown)
                .PermaRef();

            disposed = true;
        }

        protected IObservable<Unit> Initialize()
        {
            var ret = Observable.Create<Unit>(async subj =>
            {
                try
                {
                    await Connection.CreateTableAsync<CacheElement>();

                    var schemaVersion = await GetSchemaVersion();

                    if (schemaVersion < 2)
                    {
                        await Connection.ExecuteAsync("ALTER TABLE CacheElement RENAME TO VersionOneCacheElement;");
                        await Connection.CreateTableAsync<CacheElement>();

                        var sql = "INSERT INTO CacheElement SELECT Key,TypeName,Value,Expiration,\"{0}\" AS CreatedAt FROM VersionOneCacheElement;";
                        await Connection.ExecuteAsync(String.Format(sql, RxApp.TaskpoolScheduler.Now.UtcDateTime.Ticks));
                        await Connection.ExecuteAsync("DROP TABLE VersionOneCacheElement;");
                    
                        await Connection.InsertAsync(new SchemaInfo() { Version = 2, });
                    }

                    subj.OnNext(Unit.Default);
                    subj.OnCompleted();
                }
                catch (Exception ex)
                {
                    subj.OnError(ex);
                }
            });

            return ret.PublishLast().PermaRef();
        }

        protected IObservable<int> GetSchemaVersion()
        {
            return Connection.ExecuteScalarAsync<int>("SELECT Version from SchemaInfo ORDER BY Version DESC LIMIT 1")
                .Catch(Observable.Return(0))
                .SelectMany(version => 
                {
                    if (version > 0) return Observable.Return(version);

                    return Connection.CreateTableAsync<SchemaInfo>()
                        .Select(_ => 1);
                });
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

        byte[] SerializeObject<T>(T value)
        {
            var ms = new MemoryStream();
            var serializer = JsonSerializer.Create(BlobCache.SerializerSettings ?? new JsonSerializerSettings());
            var writer = new BsonWriter(ms);

            var serviceProvider = this.ServiceProvider ?? BlobCache.ServiceProvider;
            if (serviceProvider != null)
            {
                serializer.Converters.Add(new JsonObjectConverter(serviceProvider));
            }

            serializer.Serialize(writer, new ObjectWrapper<T>() { Value = value });
            return ms.ToArray();
        }

        IObservable<T> DeserializeObject<T>(byte[] data, IServiceProvider serviceProvider)
        {
            var serializer = JsonSerializer.Create(BlobCache.SerializerSettings ?? new JsonSerializerSettings());
            var reader = new BsonReader(new MemoryStream(data));

            if (serviceProvider != null) 
            {
                serializer.Converters.Add(new JsonObjectConverter(serviceProvider));
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

        static IObservable<CacheElement> ObservableThrowKeyNotFoundException(string key, Exception innerException = null)
        {
            return Observable.Throw<CacheElement>(
                new KeyNotFoundException(String.Format(CultureInfo.InvariantCulture,
                "The given key '{0}' was not present in the cache.", key), innerException));
        }
    }

    class CacheElement
    {
        [PrimaryKey]
        public string Key { get; set; }

        public string TypeName { get; set; }
        public byte[] Value { get; set; }
        public DateTime Expiration { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    class VersionOneCacheElement
    {
        [PrimaryKey]
        public string Key { get; set; }

        public string TypeName { get; set; }
        public byte[] Value { get; set; }
        public DateTime Expiration { get; set; }
    }

    class SchemaInfo
    {
        public int Version { get; set; }
    }

    interface IObjectWrapper {}
    class ObjectWrapper<T> : IObjectWrapper
    {
        public T Value { get; set; }
    }
}