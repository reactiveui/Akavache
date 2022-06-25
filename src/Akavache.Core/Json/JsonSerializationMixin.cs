﻿// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Reactive.Threading.Tasks;

using Newtonsoft.Json;

using Splat;

namespace Akavache;

/// <summary>
/// Set of extension methods associated with JSON serialization.
/// </summary>
public static class JsonSerializationMixin
{
    private static readonly ConcurrentDictionary<string, object> _inflightFetchRequests = new();

    /// <summary>
    /// Insert an object into the cache, via the JSON serializer.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="blobCache">The cache to insert the item.</param>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="value">The object to serialize.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>An observable which signals when the insertion has completed.</returns>
    public static IObservable<Unit> InsertObject<T>(this IBlobCache blobCache, string key, T value, DateTimeOffset? absoluteExpiration = null)
    {
        switch (blobCache)
        {
            case null:
                throw new ArgumentNullException(nameof(blobCache));
            case IObjectBlobCache objCache:
                return objCache.InsertObject(key, value, absoluteExpiration);
            default:
                {
                    var bytes = SerializeObject(value);
                    return blobCache.Insert(GetTypePrefixedKey(key, typeof(T)), bytes, absoluteExpiration);
                }
        }
    }

    /// <summary>
    /// Insert several objects into the cache, via the JSON serializer.
    /// Similarly to InsertAll, partial inserts should not happen.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="blobCache">The cache to insert the items.</param>
    /// <param name="keyValuePairs">The data to insert into the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
    public static IObservable<Unit> InsertAllObjects<T>(this IBlobCache blobCache, IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null) => blobCache is IObjectBlobCache objCache
            ? objCache.InsertObjects(keyValuePairs, absoluteExpiration)
            : throw new NotImplementedException();

    /// <summary>
    /// Get an object from the cache and deserialize it via the JSON
    /// serializer.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="blobCache">The cache to get the item.</param>
    /// <param name="key">The key to look up in the cache
    /// modified key name. If this is true, GetAllObjects will not find this object.</param>
    /// <returns>A Future result representing the object in the cache.</returns>
    public static IObservable<T?> GetObject<T>(this IBlobCache blobCache, string key) =>
        blobCache switch
        {
            null => throw new ArgumentNullException(nameof(blobCache)),
            IObjectBlobCache objCache => objCache.GetObject<T>(key),
            _ => blobCache.Get(GetTypePrefixedKey(key, typeof(T))).SelectMany(DeserializeObject<T>)
        };

    /// <summary>
    /// Return all objects of a specific Type in the cache.
    /// </summary>
    /// <param name="blobCache">The cache to get the items.</param>
    /// <typeparam name="T">The type of item to get.</typeparam>
    /// <returns>A Future result representing all objects in the cache
    /// with the specified Type.</returns>
    public static IObservable<IEnumerable<T>> GetAllObjects<T>(this IBlobCache blobCache) =>
        blobCache switch
        {
            null => throw new ArgumentNullException(nameof(blobCache)),
            IObjectBlobCache objCache => objCache.GetAllObjects<T>(),
            _ => blobCache.GetAllKeys()
                .SelectMany(x => x
                    .Where(y => y.StartsWith(
                        GetTypePrefixedKey(string.Empty, typeof(T)), StringComparison.InvariantCulture))
                    .ToObservable())
                .SelectMany(x =>
                    blobCache.GetObject<T>(x).Where(x => x is not null).Select(x => x!).Catch(Observable.Empty<T>()))
                .ToList()
        };

    // NB: This isn't exactly thread-safe, but it's Close Enough(tm)
    // We make up for the fact that the keys could get kicked out
    // from under us via the Catch below
    /// <summary>
    /// <para>
    /// Attempt to return an object from the cache. If the item doesn't
    /// exist or returns an error, call a Func to return the latest
    /// version of an object and insert the result in the cache.
    /// </para>
    /// <para>
    /// For most Internet applications, this method is the best method to
    /// call to fetch static data (i.e. images) from the network.
    /// </para>
    /// </summary>
    /// <param name="blobCache">The cache to get the item.</param>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="fetchFunc">A Func which will asynchronously return
    /// the latest value for the object should the cache not contain the
    /// key.
    ///
    /// Observable.Start is the most straightforward way (though not the
    /// most efficient!) to implement this Func.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <typeparam name="T">The type of item to get.</typeparam>
    /// <returns>A Future result representing the deserialized object from
    /// the cache.</returns>
    public static IObservable<T?> GetOrFetchObject<T>(this IBlobCache blobCache, string key, Func<IObservable<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null) => blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.GetObject<T>(key).Catch<T?, Exception>(ex =>
        {
            var prefixedKey = blobCache.GetHashCode().ToString(CultureInfo.InvariantCulture) + key;

            var result = Observable.Defer(fetchFunc)
                .Do(x => blobCache.InsertObject(key, x, absoluteExpiration))
                .Finally(() => _inflightFetchRequests.TryRemove(prefixedKey, out _))
                .Multicast(new AsyncSubject<T>()).RefCount();

            return (IObservable<T>)_inflightFetchRequests.GetOrAdd(prefixedKey, result);
        });

    /// <summary>
    /// <para>
    /// Attempt to return an object from the cache. If the item doesn't
    /// exist or returns an error, call a Func to return the latest
    /// version of an object and insert the result in the cache.
    /// </para>
    /// <para>
    /// For most Internet applications, this method is the best method to
    /// call to fetch static data (i.e. images) from the network.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of item to get.</typeparam>
    /// <param name="blobCache">The cache to get the item.</param>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="fetchFunc">A Func which will asynchronously return
    /// the latest value for the object should the cache not contain the
    /// key. </param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the deserialized object from
    /// the cache.</returns>
    public static IObservable<T?> GetOrFetchObject<T>(this IBlobCache blobCache, string key, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null) => blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.GetOrFetchObject(key, () => fetchFunc().ToObservable(), absoluteExpiration);

    /// <summary>
    /// <para>
    /// Attempt to return an object from the cache. If the item doesn't
    /// exist or returns an error, call a Func to create a new one.
    /// </para>
    /// <para>
    /// For most Internet applications, this method is the best method to
    /// call to fetch static data (i.e. images) from the network.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of item to get.</typeparam>
    /// <param name="blobCache">The cache to get the item.</param>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="fetchFunc">A Func which will return
    /// the latest value for the object should the cache not contain the
    /// key. </param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the deserialized object from
    /// the cache.</returns>
    public static IObservable<T?> GetOrCreateObject<T>(this IBlobCache blobCache, string key, Func<T> fetchFunc, DateTimeOffset? absoluteExpiration = null) => blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.GetOrFetchObject(key, () => Observable.Return(fetchFunc()), absoluteExpiration);

    /// <summary>
    /// Returns the time that the key was added to the cache, or returns
    /// null if the key isn't in the cache.
    /// </summary>
    /// <typeparam name="T">The type of item to get.</typeparam>
    /// <param name="blobCache">The cache to get the item.</param>
    /// <param name="key">The key to return the date for.</param>
    /// <returns>The date the key was created on.</returns>
    public static IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(this IBlobCache blobCache, string key) =>
        blobCache switch
        {
            null => throw new ArgumentNullException(nameof(blobCache)),
            IObjectBlobCache objCache => objCache.GetObjectCreatedAt<T>(key),
            _ => blobCache.GetCreatedAt(GetTypePrefixedKey(key, typeof(T)))
        };

    /// <summary>
    /// <para>
    /// This method attempts to returned a cached value, while
    /// simultaneously calling a Func to return the latest value. When the
    /// latest data comes back, it replaces what was previously in the
    /// cache.
    /// </para>
    /// <para>
    /// This method is best suited for loading dynamic data from the
    /// Internet, while still showing the user earlier data.
    /// </para>
    /// <para>
    /// This method returns an IObservable that may return *two* results
    /// (first the cached data, then the latest data). Therefore, it's
    /// important for UI applications that in your Subscribe method, you
    /// write the code to merge the second result when it comes in.
    /// </para>
    /// <para>
    /// This also means that await'ing this method is a Bad Idea(tm), always
    /// use Subscribe.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of item to get.</typeparam>
    /// <param name="blobCache">The cache to get the item.</param>
    /// <param name="key">The key to store the returned result under.</param>
    /// <param name="fetchFunc">A method to fetch a observable.</param>
    /// <param name="fetchPredicate">An optional Func to determine whether
    /// the updated item should be fetched. If the cached version isn't found,
    /// this parameter is ignored and the item is always fetched.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <param name="shouldInvalidateOnError">If this is true, the cache will
    /// be cleared when an exception occurs in fetchFunc.</param>
    /// <param name="cacheValidationPredicate">An optional Func to determine
    /// if the fetched value should be cached.</param>
    /// <returns>An Observable stream containing either one or two
    /// results (possibly a cached version, then the latest version).</returns>
    public static IObservable<T?> GetAndFetchLatest<T>(
        this IBlobCache blobCache,
        string key,
        Func<IObservable<T>> fetchFunc,
        Func<DateTimeOffset, bool>? fetchPredicate = null,
        DateTimeOffset? absoluteExpiration = null,
        bool shouldInvalidateOnError = false,
        Func<T, bool>? cacheValidationPredicate = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

#pragma warning disable CS8604 // Possible null reference argument.
        var fetch = Observable.Defer(() => blobCache.GetObjectCreatedAt<T>(key))
            .Select(x => fetchPredicate is null || x is null || fetchPredicate(x.Value))
            .Where(x => x)
            .SelectMany(_ =>
            {
                var fetchObs = fetchFunc().Catch<T, Exception>(ex =>
                {
                    var shouldInvalidate = shouldInvalidateOnError ?
                        blobCache.InvalidateObject<T>(key) :
                        Observable.Return(Unit.Default);
                    return shouldInvalidate.SelectMany(__ => Observable.Throw<T>(ex));
                });

                return fetchObs
                    .SelectMany(x =>
                        cacheValidationPredicate is not null && !cacheValidationPredicate(x)
                            ? Observable.Return(default(T))
                            : blobCache.InvalidateObject<T>(key).Select(__ => x))
                    .SelectMany(x =>
                        cacheValidationPredicate is not null && !cacheValidationPredicate(x)
                            ? Observable.Return(default(T))
                            : blobCache.InsertObject(key, x, absoluteExpiration).Select(__ => x));
            });

        if (fetch is null)
        {
            return Observable.Throw<T>(new InvalidOperationException("Could not find a valid way to fetch the value"));
        }

        var result = blobCache.GetObject<T>(key).Select(x => (x, true))
            .Catch(Observable.Return((default(T), false)));

#pragma warning restore CS8604 // Possible null reference argument.

        return result.SelectMany(x => x.Item2 ? Observable.Return(x.Item1) : Observable.Empty<T>())
            .Concat(fetch)
            .Multicast(new ReplaySubject<T?>())
            .RefCount();
    }

    /// <summary>
    /// <para>
    /// This method attempts to returned a cached value, while
    /// simultaneously calling a Func to return the latest value. When the
    /// latest data comes back, it replaces what was previously in the
    /// cache.
    /// </para>
    /// <para>
    /// This method is best suited for loading dynamic data from the
    /// Internet, while still showing the user earlier data.
    /// </para>
    /// <para>
    /// This method returns an IObservable that may return *two* results
    /// (first the cached data, then the latest data). Therefore, it's
    /// important for UI applications that in your Subscribe method, you
    /// write the code to merge the second result when it comes in.
    /// </para>
    /// <para>
    /// This also means that awaiting this method is a Bad Idea(tm), always
    /// use Subscribe.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of item to get.</typeparam>
    /// <param name="blobCache">The cache to get the item.</param>
    /// <param name="key">The key to store the returned result under.</param>
    /// <param name="fetchFunc">A method that will fetch the task.</param>
    /// <param name="fetchPredicate">An optional Func to determine whether
    /// the updated item should be fetched. If the cached version isn't found,
    /// this parameter is ignored and the item is always fetched.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <param name="shouldInvalidateOnError">If this is true, the cache will
    /// be cleared when an exception occurs in fetchFunc.</param>
    /// <param name="cacheValidationPredicate">An optional Func to determine
    /// if the fetched value should be cached.</param>
    /// <returns>An Observable stream containing either one or two
    /// results (possibly a cached version, then the latest version).</returns>
    public static IObservable<T?> GetAndFetchLatest<T>(
        this IBlobCache blobCache,
        string key,
        Func<Task<T>> fetchFunc,
        Func<DateTimeOffset, bool>? fetchPredicate = null,
        DateTimeOffset? absoluteExpiration = null,
        bool shouldInvalidateOnError = false,
        Func<T, bool>? cacheValidationPredicate = null) => blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.GetAndFetchLatest(key, () => fetchFunc().ToObservable(), fetchPredicate, absoluteExpiration, shouldInvalidateOnError, cacheValidationPredicate);

    /// <summary>
    /// Invalidates a single object from the cache. It is important that the Type
    /// Parameter for this method be correct, and you cannot use
    /// IBlobCache.Invalidate to perform the same task.
    /// </summary>
    /// <typeparam name="T">The type of item to invalidate.</typeparam>
    /// <param name="blobCache">The cache to invalidate.</param>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>An observable that signals when the operation has completed.</returns>
    public static IObservable<Unit> InvalidateObject<T>(this IBlobCache blobCache, string key) =>
        blobCache switch
        {
            null => throw new ArgumentNullException(nameof(blobCache)),
            IObjectBlobCache objCache => objCache.InvalidateObject<T>(key),
            _ => blobCache.Invalidate(GetTypePrefixedKey(key, typeof(T)))
        };

    /// <summary>
    /// Invalidates all objects of the specified type. To invalidate all
    /// objects regardless of type, use InvalidateAll.
    /// </summary>
    /// <typeparam name="T">The type of item to invalidate.</typeparam>
    /// <param name="blobCache">The cache to invalidate.</param>
    /// <returns>An observable that signals when the operation has finished.</returns>
    /// <remarks>Returns a Unit for each invalidation completion. Use Wait instead of First to wait for
    /// this.</remarks>
    public static IObservable<Unit> InvalidateAllObjects<T>(this IBlobCache blobCache)
    {
        switch (blobCache)
        {
            case null:
                throw new ArgumentNullException(nameof(blobCache));
            case IObjectBlobCache objCache:
                return objCache.InvalidateAllObjects<T>();
        }

        var ret = new AsyncSubject<Unit>();
        blobCache.GetAllKeys()
            .SelectMany(x =>
                x.Where(y => y.StartsWith(GetTypePrefixedKey(string.Empty, typeof(T)), StringComparison.InvariantCulture))
                    .ToObservable())
            .SelectMany(blobCache.Invalidate)
            .Subscribe(
                _ => { },
                ex => ret.OnError(ex),
                () =>
                {
                    ret.OnNext(Unit.Default);
                    ret.OnCompleted();
                });

        return ret;
    }

    internal static byte[] SerializeObject(object value) => SerializeObject<object>(value);

    internal static byte[] SerializeObject<T>(T value)
    {
        var settings = Locator.Current.GetService<JsonSerializerSettings>();
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, settings));
    }

    internal static IObservable<T?> DeserializeObject<T>(byte[] x)
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

    internal static string GetTypePrefixedKey(string key, Type type) => type.FullName + "___" + key;
}
