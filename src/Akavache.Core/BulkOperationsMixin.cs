﻿using System;
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
        /// <summary>
        /// Gets the specified keys.
        /// </summary>
        /// <param name="This">The this.</param>
        /// <param name="keys">The keys.</param>
        /// <returns></returns>
        public static IObservable<IDictionary<string, byte[]>> Get(this IBlobCache This, IEnumerable<string> keys)
        {
            if (This is IBulkBlobCache bulkCache) {
                return bulkCache.Get(keys);
            }

            return keys.ToObservable()
                .SelectMany(x => {
                    return This.Get(x)
                        .Select(y => new KeyValuePair<string, byte[]>(x, y))
                        .Catch<KeyValuePair<string, byte[]>, KeyNotFoundException>(_ => Observable.Empty<KeyValuePair<string, byte[]>>());
                })
                .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Inserts the specified key value pairs.
        /// </summary>
        /// <param name="This">The this.</param>
        /// <param name="keyValuePairs">The key value pairs.</param>
        /// <param name="absoluteExpiration">The absolute expiration.</param>
        /// <returns></returns>
        public static IObservable<Unit> Insert(this IBlobCache This, IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            if (This is IBulkBlobCache bulkCache) {
                return bulkCache.Insert(keyValuePairs, absoluteExpiration);
            }

            return keyValuePairs.ToObservable()
                .SelectMany(x => This.Insert(x.Key, x.Value, absoluteExpiration))
                .TakeLast(1);
        }

        /// <summary>
        /// Gets the created at.
        /// </summary>
        /// <param name="This">The this.</param>
        /// <param name="keys">The keys.</param>
        /// <returns></returns>
        public static IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(this IBlobCache This, IEnumerable<string> keys)
        {
            if (This is IBulkBlobCache bulkCache) {
                return bulkCache.GetCreatedAt(keys);
            }

            return keys.ToObservable()
                .SelectMany(x => This.GetCreatedAt(x).Select(y => new { Key = x, Value = y }))
                .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Invalidates the specified keys.
        /// </summary>
        /// <param name="This">The this.</param>
        /// <param name="keys">The keys.</param>
        /// <returns></returns>
        public static IObservable<Unit> Invalidate(this IBlobCache This, IEnumerable<string> keys)
        {
            if (This is IBulkBlobCache bulkCache) {
                return bulkCache.Invalidate(keys);
            }

            return keys.ToObservable()
               .SelectMany(x => This.Invalidate(x))
               .TakeLast(1);
        }

        /// <summary>
        /// Gets the objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This">The this.</param>
        /// <param name="keys">The keys.</param>
        /// <param name="noTypePrefix">if set to <c>true</c> [no type prefix].</param>
        /// <returns></returns>
        public static IObservable<IDictionary<string, T>> GetObjects<T>(this IBlobCache This, IEnumerable<string> keys, bool noTypePrefix = false)
        {
            if (This is IObjectBulkBlobCache bulkCache) {
                return bulkCache.GetObjects<T>(keys);
            }

            return keys.ToObservable()
                .SelectMany(x => {
                    return This.GetObject<T>(x)
                        .Select(y => new KeyValuePair<string, T>(x, y))
                        .Catch<KeyValuePair<string, T>, KeyNotFoundException>(_ => Observable.Empty<KeyValuePair<string, T>>());
                })
                .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Inserts the objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This">The this.</param>
        /// <param name="keyValuePairs">The key value pairs.</param>
        /// <param name="absoluteExpiration">The absolute expiration.</param>
        /// <returns></returns>
        public static IObservable<Unit> InsertObjects<T>(this IBlobCache This, IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            if (This is IObjectBulkBlobCache bulkCache) {
                return bulkCache.InsertObjects(keyValuePairs, absoluteExpiration);
            }

            return keyValuePairs.ToObservable()
                .SelectMany(x => This.InsertObject(x.Key, x.Value, absoluteExpiration))
                .TakeLast(1);
        }

        /// <summary>
        /// Invalidates the objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This">The this.</param>
        /// <param name="keys">The keys.</param>
        /// <returns></returns>
        public static IObservable<Unit> InvalidateObjects<T>(this IBlobCache This, IEnumerable<string> keys)
        {
            if (This is IObjectBulkBlobCache bulkCache) {
                return bulkCache.InvalidateObjects<T>(keys);
            }

            return keys.ToObservable()
               .SelectMany(x => This.InvalidateObject<T>(x))
               .TakeLast(1);
        }
    }
}
