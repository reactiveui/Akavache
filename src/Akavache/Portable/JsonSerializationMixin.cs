using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Splat;
using Akavache.Internal;

namespace Akavache
{
    public static class JsonSerializationMixin
    {
        static readonly ConcurrentDictionary<string, object> inflightFetchRequests = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Insert an object into the cache, via the JSON serializer.
        /// </summary>
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        public static IObservable<Unit> InsertObject<T>(this IBlobCache This, string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            var objCache = This as IObjectBlobCache;
            if (objCache != null) return objCache.InsertObject<T>(key, value, absoluteExpiration);

            var bytes = SerializeObject(value);
            return This.Insert(GetTypePrefixedKey(key, typeof(T)), bytes, absoluteExpiration);
        }

        /// <summary>
        /// Insert several objects into the cache, via the JSON serializer. 
        /// Similarly to InsertAll, partial inserts should not happen.
        /// </summary>
        /// <param name="keyValuePairs">The data to insert into the cache</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the completion of the insert.</returns>
        public static IObservable<Unit> InsertAllObjects<T>(this IBlobCache This, IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            var objCache = This as IObjectBlobCache;
            if (objCache != null) return objCache.InsertObjects<T>(keyValuePairs, absoluteExpiration);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get an object from the cache and deserialize it via the JSON
        /// serializer.
        /// </summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <param name="noTypePrefix">Use the exact key name instead of a
        /// modified key name. If this is true, GetAllObjects will not find this object.</param>
        /// <returns>A Future result representing the object in the cache.</returns>
        public static IObservable<T> GetObject<T>(this IBlobCache This, string key)
        {
            var objCache = This as IObjectBlobCache;
            if (objCache != null) return objCache.GetObject<T>(key);

            return This.Get(GetTypePrefixedKey(key, typeof(T))).SelectMany(DeserializeObject<T>);
        }

        /// <summary>
        /// Return all objects of a specific Type in the cache.
        /// </summary>
        /// <returns>A Future result representing all objects in the cache
        /// with the specified Type.</returns>
        public static IObservable<IEnumerable<T>> GetAllObjects<T>(this IBlobCache This)
        {
            var objCache = This as IObjectBlobCache;
            if (objCache != null) return objCache.GetAllObjects<T>();

            // NB: This isn't exactly thread-safe, but it's Close Enough(tm)
            // We make up for the fact that the keys could get kicked out
            // from under us via the Catch below
            return This.GetAllKeys()
                .SelectMany(x => x
                    .Where(y => 
                        y.StartsWith(GetTypePrefixedKey("", typeof(T))))
                    .ToObservable())
                .SelectMany(x => This.GetObject<T>(x)
                    .Catch(Observable.Empty<T>()))
                .ToList();
        }

        /// <summary>
        /// Attempt to return an object from the cache. If the item doesn't
        /// exist or returns an error, call a Func to return the latest
        /// version of an object and insert the result in the cache.
        ///
        /// For most Internet applications, this method is the best method to
        /// call to fetch static data (i.e. images) from the network.
        /// </summary>
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="fetchFunc">A Func which will asynchronously return
        /// the latest value for the object should the cache not contain the
        /// key. 
        ///
        /// Observable.Start is the most straightforward way (though not the
        /// most efficient!) to implement this Func.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the deserialized object from
        /// the cache.</returns>
        public static IObservable<T> GetOrFetchObject<T>(this IBlobCache This, string key, Func<IObservable<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
        {
            return This.GetObject<T>(key).Catch<T, Exception>(_ =>
            {
                object dontcare;
                var prefixedKey = This.GetHashCode().ToString() + key;

                var result = Observable.Defer(() => fetchFunc())
                    .Do(x => This.InsertObject(key, x, absoluteExpiration))
                    .Finally(() => inflightFetchRequests.TryRemove(prefixedKey, out dontcare))
                    .Multicast(new AsyncSubject<T>()).RefCount();
            
                return (IObservable<T>)inflightFetchRequests.GetOrAdd(prefixedKey, result);
            });
        }

        /// <summary>
        /// Attempt to return an object from the cache. If the item doesn't
        /// exist or returns an error, call a Func to return the latest
        /// version of an object and insert the result in the cache.
        ///
        /// For most Internet applications, this method is the best method to
        /// call to fetch static data (i.e. images) from the network.
        /// </summary>
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="fetchFunc">A Func which will asynchronously return
        /// the latest value for the object should the cache not contain the
        /// key. </param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the deserialized object from
        /// the cache.</returns>
        public static IObservable<T> GetOrFetchObject<T>(this IBlobCache This, string key, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
        {
            return This.GetOrFetchObject(key, () => fetchFunc().ToObservable(), absoluteExpiration);
        }

        /// <summary>
        /// Attempt to return an object from the cache. If the item doesn't
        /// exist or returns an error, call a Func to create a new one.
        ///
        /// For most Internet applications, this method is the best method to
        /// call to fetch static data (i.e. images) from the network.
        /// </summary>
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="fetchFunc">A Func which will return
        /// the latest value for the object should the cache not contain the
        /// key. </param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the deserialized object from
        /// the cache.</returns>
        public static IObservable<T> GetOrCreateObject<T>(this IBlobCache This, string key, Func<T> fetchFunc, DateTimeOffset? absoluteExpiration = null)
        {
            return This.GetOrFetchObject(key, () => Observable.Return(fetchFunc()), absoluteExpiration);
        }

        /// <summary>
        /// Returns the time that the key was added to the cache, or returns 
        /// null if the key isn't in the cache.
        /// </summary>
        /// <param name="key">The key to return the date for.</param>
        /// <returns>The date the key was created on.</returns>
        public static IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(this IBlobCache This, string key)
        {
            var objCache = This as IObjectBlobCache;
            if (objCache != null) return objCache.GetObjectCreatedAt<T>(key);

            return This.GetCreatedAt(GetTypePrefixedKey(key, typeof(T)));
        }

        /// <summary>
        /// This method attempts to returned a cached value, while
        /// simultaneously calling a Func to return the latest value. When the
        /// latest data comes back, it replaces what was previously in the
        /// cache.
        ///
        /// This method is best suited for loading dynamic data from the
        /// Internet, while still showing the user earlier data.
        ///
        /// This method returns an IObservable that may return *two* results
        /// (first the cached data, then the latest data). Therefore, it's
        /// important for UI applications that in your Subscribe method, you
        /// write the code to merge the second result when it comes in.
        ///
        /// This also means that await'ing this method is a Bad Idea(tm), always
        /// use Subscribe.
        /// </summary>
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc"></param>
        /// <param name="fetchPredicate">An optional Func to determine whether
        /// the updated item should be fetched. If the cached version isn't found,
        /// this parameter is ignored and the item is always fetched.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <param name="shouldInvalidateOnError">If this is true, the cache will
        /// be cleared when an exception occurs in fetchFunc</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version)</returns>
        public static IObservable<T> GetAndFetchLatest<T>(this IBlobCache This,
            string key,
            Func<IObservable<T>> fetchFunc,
            Func<DateTimeOffset, bool> fetchPredicate = null,
            DateTimeOffset? absoluteExpiration = null,
            bool shouldInvalidateOnError = false)
        {
            var fetch = Observable.Defer(() => This.GetObjectCreatedAt<T>(key))
                .Select(x => fetchPredicate == null || x == null || fetchPredicate(x.Value))
                .Where(x => x != false)
                .SelectMany(_ => 
                {
                    var fetchObs = fetchFunc().Catch<T, Exception>(ex =>
                    {
                        var shouldInvalidate = shouldInvalidateOnError ?
                            This.InvalidateObject<T>(key) :
                            Observable.Return(Unit.Default);
                        return shouldInvalidate.SelectMany(__ => Observable.Throw<T>(ex));
                    });

                    return fetchObs
                        .SelectMany(x => This.InvalidateObject<T>(key).Select(__ => x))
                        .SelectMany(x => This.InsertObject<T>(key, x, absoluteExpiration).Select(__ => x));
                });

            var result = This.GetObject<T>(key).Select(x => new Tuple<T, bool>(x, true))
                .Catch(Observable.Return(new Tuple<T, bool>(default(T), false)));

            return result.SelectMany(x =>
            {
                return x.Item2 ?
                    Observable.Return(x.Item1) :
                    Observable.Empty<T>();
            }).Concat(fetch).Multicast(new ReplaySubject<T>()).RefCount();
        }

        /// <summary>
        /// This method attempts to returned a cached value, while
        /// simultaneously calling a Func to return the latest value. When the
        /// latest data comes back, it replaces what was previously in the
        /// cache.
        ///
        /// This method is best suited for loading dynamic data from the
        /// Internet, while still showing the user earlier data.
        ///
        /// This method returns an IObservable that may return *two* results
        /// (first the cached data, then the latest data). Therefore, it's
        /// important for UI applications that in your Subscribe method, you
        /// write the code to merge the second result when it comes in.
        /// 
        /// This also means that await'ing this method is a Bad Idea(tm), always
        /// use Subscribe.
        /// </summary>
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc"></param>
        /// <param name="fetchPredicate">An optional Func to determine whether
        /// the updated item should be fetched. If the cached version isn't found,
        /// this parameter is ignored and the item is always fetched.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version)</returns>
        public static IObservable<T> GetAndFetchLatest<T>(this IBlobCache This,
            string key,
            Func<Task<T>> fetchFunc,
            Func<DateTimeOffset, bool> fetchPredicate = null,
            DateTimeOffset? absoluteExpiration = null)
        {
            return This.GetAndFetchLatest(key, () => fetchFunc().ToObservable(), fetchPredicate, absoluteExpiration);
        }

        /// <summary>
        /// Invalidates a single object from the cache. It is important that the Type
        /// Parameter for this method be correct, and you cannot use 
        /// IBlobCache.Invalidate to perform the same task.
        /// </summary>
        /// <param name="key">The key to invalidate.</param>
        public static IObservable<Unit> InvalidateObject<T>(this IBlobCache This, string key)
        {
            var objCache = This as IObjectBlobCache;
            if (objCache != null) return objCache.InvalidateObject<T>(key);

            return This.Invalidate(GetTypePrefixedKey(key, typeof(T)));
        }

        /// <summary>
        /// Invalidates all objects of the specified type. To invalidate all
        /// objects regardless of type, use InvalidateAll.
        /// </summary>
        /// <remarks>Returns a Unit for each invalidation completion. Use Wait instead of First to wait for 
        /// this.</remarks>
        public static IObservable<Unit> InvalidateAllObjects<T>(this IBlobCache This)
        {
            var objCache = This as IObjectBlobCache;
            if (objCache != null) return objCache.InvalidateAllObjects<T>();

            var ret = new AsyncSubject<Unit>();
            This.GetAllKeys()
                .SelectMany(x => 
                    x.Where(y => y.StartsWith(GetTypePrefixedKey("", typeof(T))))
                    .ToObservable())
                .SelectMany(This.Invalidate)
                .Subscribe(
                    _ => { },
                    ex => ret.OnError(ex),
                    () => { ret.OnNext(Unit.Default); ret.OnCompleted(); });

            return ret;
        }

        internal static byte[] SerializeObject(object value)
        {
            return SerializeObject<object>(value);
        }

        internal static byte[] SerializeObject<T>(T value)
        {
            var settings = Locator.Current.GetService<JsonSerializerSettings>();
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, settings));
        }

        static IObservable<T> DeserializeObject<T>(byte[] x)
        {
            var settings = Locator.Current.GetService<JsonSerializerSettings>();

            try
            {
                var bytes = Encoding.UTF8.GetString(x, 0, x.Length);

                var ret = JsonConvert.DeserializeObject<T>(bytes, settings);
                return Observable.Return(ret);
            }
            catch (Exception ex)
            {
                return Observable.Throw<T>(ex);
            }
        }

        internal static string GetTypePrefixedKey(string key, Type type)
        {
            return type.FullName + "___" + key;
        }
    }
}
