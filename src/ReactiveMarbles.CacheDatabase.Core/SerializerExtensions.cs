// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace ReactiveMarbles.CacheDatabase.Core;

/// <summary>
/// Extension methods associated with the serializer.
/// </summary>
public static class SerializerExtensions
{
    private static ISerializer Serializer => CoreRegistrations.Serializer ?? throw new InvalidOperationException("Unable to resolve ISerializer, make sure you are including a relevant CacheDatabase Serializer NuGet package, then initialise CoreRegistrations.Serializer with an instance.");

    /// <summary>
    /// Inserts the specified key/value pairs into the blob.
    /// </summary>
    /// <typeparam name="T">The type of item to insert.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="keyValuePairs">The key/value to insert.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A observable which signals when complete.</returns>
    public static IObservable<Unit> InsertObjects<T>(this IBlobCache blobCache, IEnumerable<KeyValuePair<string, T>> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        var items = keyValuePairs.Select(x => new KeyValuePair<string, byte[]>(x.Key, Serializer.Serialize(x.Value)));

        return blobCache.Insert(items, typeof(T), absoluteExpiration);
    }

    /// <summary>
    /// Gets a value pair filled with the specified keys with their corresponding values.
    /// </summary>
    /// <typeparam name="T">The type of item to get.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="keys">The keys to get the values for.</param>
    /// <returns>A observable with the specified values.</returns>
    public static IObservable<KeyValuePair<string, T>> GetObjects<T>(this IBlobCache blobCache, IEnumerable<string> keys)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache
            .Get(keys, typeof(T))
            .Select(x => (x.Key, Data: Serializer.Deserialize<T>(x.Value)))
            .Where(x => x.Data is not null).Select(x => new KeyValuePair<string, T>(x.Key, x.Data!));
    }

    /// <summary>
    /// Insert an object into the cache, via the JSON serializer.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="value">The object to serialize.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
    public static IObservable<Unit> InsertObject<T>(this IBlobCache blobCache, string key, T value, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
        }

        return blobCache.Insert(key, Serializer.Serialize(value), typeof(T), absoluteExpiration);
    }

    /// <summary>
    /// Get an object from the cache and deserialize it via the JSON
    /// serializer.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="key">The key to look up in the cache.</param>
    /// <returns>A Future result representing the object in the cache.</returns>
    public static IObservable<T?> GetObject<T>(this IBlobCache blobCache, string key)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
        }

        return blobCache.Get(key, typeof(T)).Select(x => x is null ? default : Serializer.Deserialize<T>(x));
    }

    /// <summary>
    /// Return all objects of a specific Type in the cache.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <returns>A Future result representing all objects in the cache
    /// with the specified Type.</returns>
    public static IObservable<T> GetAllObjects<T>(this IBlobCache blobCache) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache
                .GetAll(typeof(T))
                .Select(x => Serializer.Deserialize<T>(x.Value))
                .Where(x => x is not null)
                .Select(x => x!);

    /// <summary>
    /// Returns the time that the object with the key was added to the cache, or returns
    /// null if the key isn't in the cache.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="key">The key to return the date for.</param>
    /// <returns>The date the key was created on.</returns>
    public static IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(this IBlobCache blobCache, string key)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
        }

        return blobCache.GetCreatedAt(key, typeof(T));
    }

    /// <summary>
    /// Invalidates a single object from the cache. It is important that the Type
    /// Parameter for this method be correct, and you cannot use
    /// IBlobCache.Invalidate to perform the same task.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    public static IObservable<Unit> InvalidateObject<T>(this IBlobCache blobCache, string key)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
        }

        return blobCache.Invalidate(key, typeof(T));
    }

    /// <summary>
    /// Invalidates several objects from the cache. It is important that the Type
    /// Parameter for this method be correct, and you cannot use
    /// IBlobCache.Invalidate to perform the same task.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="keys">The keys to invalidate.</param>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    public static IObservable<Unit> InvalidateObjects<T>(this IBlobCache blobCache, IEnumerable<string> keys)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        return blobCache.Invalidate(keys, typeof(T));
    }

    /// <summary>
    /// Invalidates all objects of the specified type. To invalidate all
    /// objects regardless of type, use InvalidateAll.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    public static IObservable<Unit> InvalidateAllObjects<T>(this IBlobCache blobCache) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.InvalidateAll(typeof(T));

    /// <summary>
    /// Insert several objects into the cache, via the JSON serializer.
    /// Similarly to InsertAll, partial inserts should not happen.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="blobCache">The cache to insert the items.</param>
    /// <param name="keyValuePairs">The data to insert into the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
    public static IObservable<Unit> InsertAllObjects<T>(this IBlobCache blobCache, IEnumerable<KeyValuePair<string, T>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.Insert(keyValuePairs.Select(x => new KeyValuePair<string, byte[]>(x.Key, Serializer.Serialize<T>(x.Value))), absoluteExpiration);

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
    public static IObservable<T?> GetOrFetchObject<T>(this IBlobCache blobCache, string key, Func<IObservable<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null) =>
        blobCache.GetObject<T>(key).Catch<T?, Exception>(_ => fetchFunc());

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
    public static IObservable<T?> GetOrFetchObject<T>(this IBlobCache blobCache, string key, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null) =>
        blobCache.GetOrFetchObject(key, () => fetchFunc().ToObservable(), absoluteExpiration);

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
    /// <returns>A Future result representing the deserialized object from
    /// the cache.</returns>
    public static IObservable<T?> GetOrCreateObject<T>(this IBlobCache blobCache, string key, Func<T> fetchFunc) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.GetObject<T>(key).Catch<T?, Exception>(_ =>
                {
                    var value = fetchFunc();
                    return blobCache.InsertObject(key, value).Select(_ => value);
                });

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
                        cacheValidationPredicate is not null && !cacheValidationPredicate(x!)
                            ? Observable.Return(default(T))
                            : blobCache.InsertObject(key, x, absoluteExpiration).Select(__ => x));
            });

        if (fetch is null)
        {
            return Observable.Throw<T>(new Exception("Could not find a valid way to fetch the value"));
        }

        var result = blobCache.GetObject<T>(key).Select(x => (x, true))
            .Catch(Observable.Return((default(T), false)));

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
        Func<T, bool>? cacheValidationPredicate = null) =>
            blobCache.GetAndFetchLatest(key, () => fetchFunc().ToObservable(), fetchPredicate, absoluteExpiration, shouldInvalidateOnError, cacheValidationPredicate);

    /// <summary>
    /// Insert several objects of mixed types into the cache.
    /// </summary>
    /// <param name="blobCache">The cache to insert the items.</param>
    /// <param name="keyValuePairs">The data to insert into the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
    public static IObservable<Unit> InsertObjects(this IBlobCache blobCache, IDictionary<string, object> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (keyValuePairs is null)
        {
            throw new ArgumentNullException(nameof(keyValuePairs));
        }

        // For mixed object types, we need to serialize each one individually and use its specific type
        return keyValuePairs
            .Select(kvp => blobCache.Insert(kvp.Key, Serializer.Serialize(kvp.Value), kvp.Value?.GetType() ?? typeof(object), absoluteExpiration))
            .Merge()
            .TakeLast(1)
            .Select(_ => Unit.Default);
    }

    internal static string GetTypePrefixedKey(this string key, Type type) => type.FullName + "___" + key;
}
