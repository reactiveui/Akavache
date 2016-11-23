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
    public class SQLitePersistentBlobCache : IObjectBlobCache, IEnableLogger
    {
        public IScheduler Scheduler { get; private set; }
        public SQLiteConnection Connection { get; private set; }

        static readonly object disposeGate = 42;

        readonly IObservable<Unit> _initializer;
        SqliteOperationQueue opQueue;
        IDisposable queueThread;
        bool disposed = false;

        public SQLitePersistentBlobCache(string databaseFile, IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? BlobCache.TaskpoolScheduler;

            BlobCache.EnsureInitialized();

            Connection = new SQLiteConnection(databaseFile, storeDateTimeAsTicks: true);
            _initializer = Initialize();
        }

        internal void ReplaceOperationQueue(SqliteOperationQueue queue)
        {
            _initializer.Wait();

            opQueue.Dispose();

            opQueue = queue;
            opQueue.Start();
        }

        readonly AsyncSubject<Unit> shutdown = new AsyncSubject<Unit>();
        public IObservable<Unit> Shutdown { get { return shutdown; } }

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            if (key == null || data == null) return Observable.Throw<Unit>(new ArgumentNullException());

            var exp = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;
            var createdAt = Scheduler.Now.UtcDateTime;

            return _initializer
                .SelectMany(_ => BeforeWriteToDiskFilter(data, Scheduler))
                .SelectMany(encData => opQueue.Insert(new[] { new CacheElement() { 
                    Key = key, 
                    Value = encData, 
                    CreatedAt = createdAt, 
                    Expiration = exp, 
                }}))
                .PublishLast().PermaRef();
        }

        public IObservable<byte[]> Get(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>("SqlitePersistentBlobCache");
            if (key == null) return Observable.Throw<byte[]>(new ArgumentNullException());

            return _initializer.SelectMany(_ => opQueue.Select(new[] { key }))
                .SelectMany(x => x.Count() == 1 ? 
                    Observable.Return(x.First().Value) :
                    ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key))
                .SelectMany(x => AfterReadFromDiskFilter(x, Scheduler))
                .PublishLast().PermaRef();
        }

        public IObservable<IEnumerable<string>> GetAllKeys()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<List<string>>("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.GetAllKeys())
                .PublishLast().PermaRef();
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<DateTimeOffset?>("SqlitePersistentBlobCache");
            if (key == null) return Observable.Throw<DateTimeOffset?>(new ArgumentNullException());

            return _initializer.SelectMany(_ => opQueue.Select(new[] { key }))
                .Select(x => x.Count() == 1 ?
                    (DateTimeOffset?)new DateTimeOffset(x.First().CreatedAt, TimeSpan.Zero) :
                    default(DateTimeOffset?))
                .PublishLast().PermaRef();
        }

        public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
        {
            return GetCreatedAt(key);
        }

        public IObservable<Unit> Flush()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.Flush())
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> Invalidate(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.Invalidate(new[] { key }))
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> InvalidateAll()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.InvalidateAll())
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            if (key == null) return Observable.Throw<Unit>(new ArgumentNullException());

            var data = SerializeObject(value);
            var exp = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;
            var createdAt = Scheduler.Now.UtcDateTime;

            return _initializer
                .SelectMany(_ => BeforeWriteToDiskFilter(data, Scheduler))
                .SelectMany(encData => opQueue.Insert(new[] { new CacheElement() {
                    Key = key,
                    TypeName = typeof(T).FullName,
                    Value = encData,
                    CreatedAt = createdAt,
                    Expiration = exp,
                }}))
                .PublishLast().PermaRef();
        }

        public IObservable<T> GetObject<T>(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<T>("SqlitePersistentBlobCache");
            if (key == null) return Observable.Throw<T>(new ArgumentNullException());

            return _initializer.SelectMany(_ => opQueue.Select(new[] { key }))
                .SelectMany(x => x.Count() == 1 ? 
                    Observable.Return(x.First().Value) :
                    ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key))
                .SelectMany(x => AfterReadFromDiskFilter(x, Scheduler))
                .SelectMany(x => DeserializeObject<T>(x))
                .PublishLast().PermaRef();
        }

        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<IEnumerable<T>>("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.SelectTypes(new[] { typeof(T).FullName })
                .SelectMany(x => x.ToObservable()
                    .SelectMany(y => AfterReadFromDiskFilter(y.Value, Scheduler))
                    .SelectMany(y => DeserializeObject<T>(y))
                    .ToList()))
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            return Invalidate(key);
        }

        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.InvalidateTypes(new[] { typeof(T).FullName }))
                .PublishLast().PermaRef();
        }

        public IObservable<Unit> Vacuum()
        {
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");

            return _initializer.SelectMany(_ => opQueue.Vacuum())
                .PublishLast().PermaRef();
        }

        public void Dispose()
        {
            var disp = Interlocked.Exchange(ref queueThread, null);
            if (disp == null) return;

            var cleanup = Observable.Start(() => 
            {
                // NB: While we intentionally dispose the operation queue 
                // from a background thread so that we don't park the UI
                // while we're waiting for background operations to 
                // complete, we must serialize calls to sqlite3_close or
                // else SQLite3 will start throwing back "busy" at us.
                //
                // We intentionally serialize even the shutdown of the
                // background queue to be extra paranoid about not getting
                // 'busy' while cleaning up.
                lock (disposeGate) 
                {
                    disp.Dispose();
                    opQueue.Dispose();
                    Connection.Dispose();
                } 
            }, Scheduler);

            cleanup.Multicast(shutdown).PermaRef();
            disposed = true;
        }

        protected IObservable<Unit> Initialize()
        {
            var ret = Observable.Create<Unit>(async subj =>
            {
                // NB: This is in its own try block because depending on the 
                // platform, we may not have a modern SQLite3, where these
                // PRAGMAs are supported. These aren't critical, so let them
                // fail silently
                try 
                {
                    // NB: Setting journal_mode returns a row, nfi
                    Connection.ExecuteScalar<int>("PRAGMA journal_mode=WAL");
                    Connection.Execute("PRAGMA temp_store=MEMORY");
                    Connection.Execute("PRAGMA synchronous=OFF");
                }
                catch (SQLiteException) 
                {
                }

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

                    // NB: We have to do this here because you can't prepare
                    // statements until you've got the backing table
                    opQueue = new SqliteOperationQueue(Connection, Scheduler);
                    queueThread = opQueue.Start();

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
            catch (Exception)
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
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>("SqlitePersistentBlobCache");

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
            if (disposed) return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>("SqlitePersistentBlobCache");

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

    class CacheElement
    {
        [PrimaryKey]
        public string Key { get; set; }

        [Indexed]
        public string TypeName { get; set; }

        public byte[] Value { get; set; }

        [Indexed]
        public DateTime Expiration { get; set; }

        public DateTime CreatedAt { get; set; }
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
