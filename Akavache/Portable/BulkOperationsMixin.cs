using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    public static class BulkOperationsMixin
    {
        public static IObservable<IDictionary<string, byte[]>> Get(this IBlobCache This, IEnumerable<string> keys)
        {
            var bulkCache = This as IBulkBlobCache;
            if (bulkCache != null) return bulkCache.Get(keys);

            return keys.ToObservable()
                .SelectMany(x => 
                {
                    return This.Get(x)
                        .Select(y => new KeyValuePair<string, byte[]>(x,y))
                        .Catch<KeyValuePair<string, byte[]>, KeyNotFoundException>(_ => Observable.Empty<KeyValuePair<string, byte[]>>());
                })
                .ToDictionary(k => k.Key, v => v.Value);
        }

        public static IObservable<Unit> Insert(this IBlobCache This, IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            var bulkCache = This as IBulkBlobCache;
            if (bulkCache != null) return bulkCache.Insert(keyValuePairs, absoluteExpiration);

            return keyValuePairs.ToObservable()
                .SelectMany(x => This.Insert(x.Key, x.Value, absoluteExpiration))
                .TakeLast(1);
        }

        public static IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(this IBlobCache This, IEnumerable<string> keys)
        {
            var bulkCache = This as IBulkBlobCache;
            if (bulkCache != null) return bulkCache.GetCreatedAt(keys);

            return keys.ToObservable()
                .SelectMany(x => This.GetCreatedAt(x).Select(y => new { Key = x, Value = y }))
                .ToDictionary(k => k.Key, v => v.Value);
        }

        public static IObservable<Unit> Invalidate(this IBlobCache This, IEnumerable<string> keys)
        {
            var bulkCache = This as IBulkBlobCache;
            if (bulkCache != null) return bulkCache.Invalidate(keys);

            return keys.ToObservable()
               .SelectMany(x => This.Invalidate(x))
               .TakeLast(1);
        }

        public static IObservable<IDictionary<string, T>> GetObjects<T>(this IBlobCache This, IEnumerable<string> keys, bool noTypePrefix = false)
        {
            var bulkCache = This as IObjectBulkBlobCache;
            if (bulkCache != null) return bulkCache.GetObjects<T>(keys);

            return keys.ToObservable()
                .SelectMany(x => 
                {
                    return This.GetObject<T>(x)
                        .Select(y => new KeyValuePair<string, T>(x,y))
                        .Catch<KeyValuePair<string, T>, KeyNotFoundException>(_ => Observable.Empty<KeyValuePair<string, T>>());
                })
                .ToDictionary(k => k.Key, v => v.Value);           
        }

        public static IObservable<Unit> InsertObjects<T>(this IBlobCache This, IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            var bulkCache = This as IObjectBulkBlobCache;
            if (bulkCache != null) return bulkCache.InsertObjects(keyValuePairs, absoluteExpiration);

            return keyValuePairs.ToObservable()
                .SelectMany(x => This.InsertObject(x.Key, x.Value, absoluteExpiration))
                .TakeLast(1);
        }

        public static IObservable<Unit> InvalidateObjects<T>(this IBlobCache This, IEnumerable<string> keys)
        {
            var bulkCache = This as IObjectBulkBlobCache;
            if (bulkCache != null) return bulkCache.InvalidateObjects<T>(keys);

            return keys.ToObservable()
               .SelectMany(x => This.InvalidateObject<T>(x))
               .TakeLast(1);
        }
    }
}
