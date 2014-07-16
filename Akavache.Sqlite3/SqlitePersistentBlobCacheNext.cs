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
using Splat;
using System.Threading.Tasks;
using System.Threading;
using System.Reactive.Disposables;

namespace Akavache.Sqlite3
{
    /// <summary>
    /// This class represents an IBlobCache backed by a SQLite3 database, and
    /// it is the default (and best!) implementation.
    /// </summary>
    public class SqlitePersistentBlobCacheNext : IObjectBlobCache, IEnableLogger
    {
        public IScheduler Scheduler { get; private set; }
        public SQLiteConnection Connection { get; private set; }

        readonly IObservable<Unit> _initializer;
        readonly SqliteOperationQueue opQueue;
        IDisposable queueThread;
        bool disposed = false;

        public SqlitePersistentBlobCacheNext(string databaseFile, IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? BlobCache.TaskpoolScheduler;

            BlobCache.EnsureInitialized();

            Connection = new SQLiteConnection(databaseFile, storeDateTimeAsTicks: true);
            opQueue = new SqliteOperationQueue(Connection, Scheduler);
            queueThread = opQueue.Start();

            _initializer = Initialize();
        }

        readonly AsyncSubject<Unit> shutdown = new AsyncSubject<Unit>();
        public IObservable<Unit> Shutdown { get { return shutdown; } }

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            var item = new CacheElement() {
                Key = key,
                Value = data,
                Expiration = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime,
            };

            return _initializer.SelectMany(_ => opQueue.Insert(new[] { item }))
                .PublishLast().PermaRef();
        }

        public IObservable<byte[]> Get(string key)
        {
            if (disposed) return Observable.Throw<byte[]>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            return _initializer.SelectMany(_ => opQueue.Select(new[] { key }))
                .Select(x => x[0].Value)
                .PublishLast().PermaRef();
        }

        public IObservable<List<string>> GetAllKeys()
        {
            if (disposed) return Observable.Throw<List<string>>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            return _initializer.SelectMany(_ => opQueue.GetAllKeys())
                .PublishLast().PermaRef();
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (disposed) return Observable.Throw<DateTimeOffset?>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            return _initializer.SelectMany(_ => opQueue.Select(new[] { key }))
                .Select(x => (DateTimeOffset?)new DateTimeOffset(x[0].CreatedAt, TimeSpan.Zero))
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> Flush()
        {
            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            return _initializer.SelectMany(_ => opQueue.Flush())
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> Invalidate(string key)
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.Invalidate(new[] { key }))
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> InvalidateAll()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.InvalidateAll())
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException("SqlitePersistentBlobCache"));
            var data = SerializeObject(value);
            var item = new CacheElement() {
                Key = key,
                Value = data,
                Expiration = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime,
            };

            return _initializer.SelectMany(_ => opQueue.Insert(new[] { item }))
                .PublishLast().PermaRef();
        }

        public IObservable<T> GetObject<T>(string key, bool noTypePrefix = false)
        {
            if (disposed) return Observable.Throw<T>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            return _initializer.SelectMany(_ => opQueue.Select(new[] { key }))
                .SelectMany(x => DeserializeObject<T>(x[0].Value))
                .PublishLast().PermaRef();
        }

        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            if (disposed) return Observable.Throw<IEnumerable<T>>(new ObjectDisposedException("SqlitePersistentBlobCache"));

            return _initializer.SelectMany(_ => opQueue.SelectTypes(new[] { typeof(T).FullName })
                .SelectMany(x => x.ToObservable()
                    .SelectMany(y => DeserializeObject<T>(y.Value))
                    .ToList()))
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");
            return Invalidate(key);
        }

        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.InvalidateTypes(new[] { typeof(T).FullName }))
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> Vacuum()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.Vacuum())
                .PublishLast().PermaRef();
        }

        public void Dispose()
        {
            var disp = Interlocked.Exchange(ref queueThread, null);
            if (disp == null) return;

            Observable.Start(() => queueThread.Dispose(), Scheduler)
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
                    Connection.CreateTable<CacheElement>();

                    var schemaVersion = GetSchemaVersion();

                    if (schemaVersion < 2)
                    {
                        Connection.Execute("ALTER TABLE CacheElement RENAME TO VersionOneCacheElement;");
                        Connection.CreateTable<CacheElement>();

                        var sql = "INSERT INTO CacheElement SELECT Key,TypeName,Value,Expiration,\"{0}\" AS CreatedAt FROM VersionOneCacheElement;";
                        Connection.Execute(String.Format(sql, BlobCache.TaskpoolScheduler.Now.UtcDateTime.Ticks));
                        Connection.Execute("DROP TABLE VersionOneCacheElement;");
                    
                        Connection.Insert(new SchemaInfo() { Version = 2, });
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

        protected int GetSchemaVersion()
        {
            bool shouldCreateSchemaTable = false;
            int versionNumber = 0;

            try 
            {
                versionNumber = Connection.ExecuteScalar<int>("SELECT Version from SchemaInfo ORDER BY Version DESC LIMIT 1");
            }
            catch (Exception ex)
            {
                shouldCreateSchemaTable = true;
            }

            if (shouldCreateSchemaTable)
            {
                Connection.CreateTable<SchemaInfo>();
                versionNumber = 1;
            }

            return versionNumber;
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

    /*
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
    */
}
