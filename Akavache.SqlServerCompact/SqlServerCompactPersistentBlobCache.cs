using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Splat;

namespace Akavache.SqlServerCompact
{
    public class SqlServerCompactPersistentBlobCache : IObjectBulkBlobCache, IEnableLogger
    {
        readonly IObservable<Unit> initializer;
        MemoizingMRUCache<string, IObservable<CacheElement>> inflightCache;

        public SqlServerCompactPersistentBlobCache(string databaseFile)
        {
            Connection = new SqlConnection(databaseFile);

            initializer = Initialize();

            inflightCache = new MemoizingMRUCache<string, IObservable<CacheElement>>((key, ce) =>
            {
                return initializer
                    .SelectMany(_ => Connection.QueryElement(key))
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

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            return Observable.Start(() =>
            {
                var connection = new SqlConnection();
                var command = connection.CreateCommand();
                command.CommandType = CommandType.TableDirect;
                command.CommandText = "HAHAA";
                var asyncResult = command.BeginExecuteNonQuery();
                command.EndExecuteNonQuery(asyncResult);
            });
        }

        public IObservable<byte[]> Get(string key)
        {
            throw new NotImplementedException();
        }

        public IObservable<List<string>> GetAllKeys()
        {
            throw new NotImplementedException();
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Flush()
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Invalidate(string key)
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> InvalidateAll()
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Vacuum()
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Shutdown { get; private set; }
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

        private SqlConnection Connection { get; set; }

        protected IObservable<Unit> Initialize()
        {
            var ret = Observable.Create<Unit>(async subj =>
            {
                try
                {
                    await Connection.CreateCacheElementTable();
                    await GetSchemaVersion();

                    subj.OnNext(Unit.Default);
                    subj.OnCompleted();
                }
                catch (Exception ex)
                {
                    subj.OnError(ex);
                }
            });

            var connectableObs = ret.PublishLast();
            connectableObs.Connect();
            return connectableObs;
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
