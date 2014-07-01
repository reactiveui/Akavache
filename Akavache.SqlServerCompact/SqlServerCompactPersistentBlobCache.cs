using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Splat;

namespace Akavache.SqlServerCompact
{
    public class SqlServerCompactPersistentBlobCache : IObjectBulkBlobCache, IEnableLogger
    {
        const string typeName = "SqlServerCompactPersistentBlobCache";
        readonly DateTime DateTimeMaxValueSqlServerCeCompatible = DateTime.MaxValue.AddMilliseconds(-1);

        readonly IObservable<Unit> initializer;
        readonly MemoizingMRUCache<string, IObservable<CacheElement>> inflightCache;
        readonly AsyncSubject<Unit> shutdown = new AsyncSubject<Unit>();
        bool disposed;
        readonly string databaseFile;

        public SqlServerCompactPersistentBlobCache(string databaseFile, IScheduler scheduler = null)
        {
            this.databaseFile = databaseFile;
            Scheduler = scheduler ?? BlobCache.TaskpoolScheduler;
            var connectionString = BuildConnectionString(databaseFile);
            Connection = new SqlCeConnection(connectionString);

            initializer = Initialize(connectionString);

            inflightCache = new MemoizingMRUCache<string, IObservable<CacheElement>>((key, ce) =>
            {
                return initializer
                    .SelectMany(_ => Connection.QueryCacheById(key))
                    .SelectMany(x =>
                    {
                        return (x.Count == 1) ? Observable.Return(x[0]) : ObservableThrowKeyNotFoundException(key);
                    })
                    .SelectMany(x =>
                    {
                        if (x.Expiration < Scheduler.Now.UtcDateTime)
                        {
                            return Invalidate(key).SelectMany(_ => ObservableThrowKeyNotFoundException(key));
                        }
                        return Observable.Return(x);
                    });
            }, 10);
        }

        string BuildConnectionString(string databaseFile)
        {
            return String.Format("Data Source={0};Persist Security Info=False;", databaseFile);
        }

        public void Dispose()
        {
            if (disposed) return;

            Connection.Dispose();

            disposed = true;
        }

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            if (disposed) return Observable.Throw<Unit>(new ObjectDisposedException(typeName));
            lock (inflightCache) inflightCache.Invalidate(key);

            var element = new CacheElement
            {
                Key = key,
                Value = data,
                CreatedAt = Scheduler.Now.UtcDateTime,
                Expiration = absoluteExpiration != null ? absoluteExpiration.Value.UtcDateTime : DateTimeMaxValueSqlServerCeCompatible
            };

            var ret = initializer
                .SelectMany(_ => BeforeWriteToDiskFilter(data, Scheduler))
                .Do(x => element.Value = x)
                .SelectMany(x => Connection.Insert(element))
                .Multicast(new AsyncSubject<Unit>());

            ret.Connect();

            return ret;
        }

        public IObservable<byte[]> Get(string key)
        {
            if (disposed) return Observable.Throw<byte[]>(new ObjectDisposedException(typeName));
            lock (inflightCache)
            {
                return inflightCache.Get(key)
                    .Select(x => x.Value)
                    .SelectMany(x => AfterReadFromDiskFilter(x, Scheduler))
                    .Finally(() => { lock (inflightCache) { inflightCache.Invalidate(key); } });
            }
        }

        public IObservable<List<string>> GetAllKeys()
        {
            if (disposed) throw new ObjectDisposedException(typeName);

            return initializer
                .SelectMany(_ => Connection.QueryCacheByExpiration(BlobCache.TaskpoolScheduler.Now.UtcDateTime))
                .Select(x => x.Select(y => y.Key).ToList());
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (disposed) return Observable.Throw<DateTimeOffset?>(new ObjectDisposedException(typeName));
            lock (inflightCache)
            {
                return inflightCache.Get(key)
                    .Select(x => x.CreatedAt == DateTime.MaxValue ?
                        default(DateTimeOffset?) : new DateTimeOffset(x.CreatedAt, TimeSpan.Zero))
                    .Catch<DateTimeOffset?, KeyNotFoundException>(_ => Observable.Return(default(DateTimeOffset?)))
                    .Finally(() => { lock (inflightCache) { inflightCache.Invalidate(key); } });
            }
        }

        public IObservable<Unit> Flush()
        {
            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> Invalidate(string key)
        {
            if (disposed) throw new ObjectDisposedException(typeName);
            lock (inflightCache) inflightCache.Invalidate(key);
            return initializer.SelectMany(_ => Connection.DeleteFromCache(key));
        }

        public IObservable<Unit> InvalidateAll()
        {
            if (disposed) throw new ObjectDisposedException(typeName);

            return initializer.SelectMany(_ => Connection.DeleteAllFromCache());
        }

        public IObservable<Unit> Vacuum()
        {
            if (disposed) throw new ObjectDisposedException("SqlitePersistentBlobCache");

            var nowTime = Scheduler.Now.UtcDateTime;
            return initializer
                .SelectMany(_ => Connection.DeleteExpiredElements(nowTime))
                .SelectMany(_ => Observable.Defer(() => Connection.Vacuum(nowTime).Retry(3)))
                .Select(_ => Unit.Default);
        }

        public IObservable<Unit> Shutdown { get { return shutdown; } }
        public IScheduler Scheduler { get; private set; }
        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<T> GetObject<T>(string key, bool noTypePrefix = false)
        {
            throw new NotImplementedException();
        }

        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Insert(IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<IDictionary<string, byte[]>> Get(IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        public IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Invalidate(IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> InsertObjects<T>(IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<IDictionary<string, T>> GetObjects<T>(IEnumerable<string> keys, bool noTypePrefix = false)
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> InvalidateObjects<T>(IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        private SqlCeConnection Connection { get; set; }

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

        protected IObservable<Unit> Initialize(string connectionString)
        {
            return Observable.Create<Unit>(async (obs) =>
            {
                try
                {
                    if (!File.Exists(databaseFile))
                    {
                        await CreateDatabaseFile(connectionString);
                    }
                 
                    // TODO: consider migrating tables and stuff
                    var currentVersion = await GetSchemaVersion();
                    
                    var tableExists = await Connection.CacheElementsTableExists();
                    if (!tableExists)
                    {
                        await Connection.CreateCacheElementTable();
                    }

                    obs.OnNext(Unit.Default);
                    obs.OnCompleted();
                }
                catch (Exception ex)
                {
                    obs.OnError(ex);
                }
            });
        }

        static Task CreateDatabaseFile(string connectionString)
        {
            return Task.Run(() =>
            {
                var en = new SqlCeEngine(connectionString);
                en.CreateDatabase();
            });
        }

        protected async Task<int> GetSchemaVersion()
        {
            bool shouldCreateSchemaTable = false;
            int versionNumber = 0;

            try
            {
                versionNumber = await Connection.ExecuteScalarAsync<int>("SELECT Version from SchemaInfo ORDER BY Version DESC LIMIT 1");
            }
            catch (Exception)
            {
                shouldCreateSchemaTable = true;
            }

            if (shouldCreateSchemaTable)
            {
                await Connection.CreateSchemaInfoTable();
                versionNumber = 1;
            }

            return versionNumber;
        }

        static IObservable<CacheElement> ObservableThrowKeyNotFoundException(string key, Exception innerException = null)
        {
            return Observable.Throw<CacheElement>(
                new KeyNotFoundException(String.Format(CultureInfo.InvariantCulture,
                "The given key '{0}' was not present in the cache.", key), innerException));
        }
    }
}
