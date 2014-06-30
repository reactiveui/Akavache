using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using Splat;

namespace Akavache.SqlServerCompact
{
    public class SqlServerCompactPersistentBlobCache : IObjectBlobCache, IObjectBulkBlobCache, IEnableLogger
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            throw new NotImplementedException();
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
    }
}
