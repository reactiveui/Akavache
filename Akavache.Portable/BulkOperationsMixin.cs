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
        public static IObservable<IDictionary<string, byte[]>> GetAsync(this IBlobCache This, IEnumerable<string> keys)
        {
            var bulkCache = This as IBulkBlobCache;
            if (bulkCache != null) return bulkCache.GetAsync(keys);

            return keys.ToObservable()
                .SelectMany(x => This.GetAsync(x).Select(y => new { Key = x, Value = y }))
                .ToDictionary(k => k.Key, v => v.Value);
        }

        public static IObservable<Unit> Insert(this IBlobCache This, IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            var bulkCache = This as IBulkBlobCache;
            if (bulkCache != null) return bulkCache.Insert(keyValuePairs, absoluteExpiration);

            return keyValuePairs.ToObservable()
                .SelectMany(x => This.Insert(x.Key, x.Value))
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
    }
}
