// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Threading.Tasks;
using Akavache.Core;
using Splat;

namespace Akavache;

/// <summary>
/// Extension methods associated with the serializer.
/// </summary>
public static class SerializerExtensions
{
    /// <summary>
    /// Inserts the specified key/value pairs into the blob.
    /// </summary>
    /// <typeparam name="T">The type of item to insert.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="keyValuePairs">The key/value to insert.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A observable which signals when complete.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using InsertObjects requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using InsertObjects requires types to be preserved for serialization.")]
#endif
    public static IObservable<Unit> InsertObjects<T>(this IBlobCache blobCache, IEnumerable<KeyValuePair<string, T>> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        var items = keyValuePairs.Select(x => new KeyValuePair<string, byte[]>(x.Key, blobCache.Serializer.Serialize(x.Value)));

        return blobCache.Insert(items, typeof(T), absoluteExpiration);
    }

    /// <summary>
    /// Gets a value pair filled with the specified keys with their corresponding values.
    /// </summary>
    /// <typeparam name="T">The type of item to get.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="keys">The keys to get the values for.</param>
    /// <returns>A observable with the specified values.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetObjects requires types to be preserved for Deserialization.")]
    [RequiresDynamicCode("Using GetObjects requires types to be preserved for Deserialization.")]
#endif
    public static IObservable<KeyValuePair<string, T>> GetObjects<T>(this IBlobCache blobCache, IEnumerable<string> keys)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache
            .Get(keys, typeof(T))
            .Select(x => (x.Key, Data: blobCache.Serializer.Deserialize<T>(x.Value)))
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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using InsertObject requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using InsertObject requires types to be preserved for serialization.")]
#endif
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

        // Handle null values by storing an empty byte array as a marker
        byte[] serializedData;
        if (value is null)
        {
            // Store empty byte array for null values
            serializedData = [];
        }
        else
        {
            try
            {
                serializedData = SerializeWithContext(value, blobCache);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize object of type {typeof(T).Name} for key '{key}'.", ex);
            }
        }

        return blobCache.Insert(key, serializedData, typeof(T), absoluteExpiration);
    }

    /// <summary>
    /// Get an object from the cache and deserialize it via the JSON
    /// serializer.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="key">The key to look up in the cache.</param>
    /// <returns>A Future result representing the object in the cache.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetObject requires types to be preserved for Deserialization.")]
    [RequiresDynamicCode("Using GetObject requires types to be preserved for Deserialization.")]
#endif
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

        return blobCache.Get(key, typeof(T)).Select(x =>
        {
            if (x is null)
            {
                // The underlying cache should have thrown KeyNotFoundException,
                // but if we get null here, we should throw it ourselves
                throw new KeyNotFoundException($"The key '{key}' was not found in the cache.");
            }

            if (x.Length == 0)
            {
                // Empty byte array could indicate a null value was stored
                // In this case, return default(T) as the stored null value
                return default;
            }

            try
            {
                return DeserializeWithContext<T>(x, blobCache);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize object of type {typeof(T).Name} for key '{key}'.", ex);
            }
        });
    }

    /// <summary>
    /// Retrieve a value from the key-value cache and deserialize it. If the key is not in
    /// the cache, this method should return an IObservable which
    /// OnError's with KeyNotFoundException.
    /// </summary>
    /// <param name="blobCache">The blob cache.</param>
    /// <param name="key">The key to return asynchronously.</param>
    /// <param name="type">The type of object to deserialize to.</param>
    /// <returns>A Future result representing the deserialized object.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using Get with Type requires types to be preserved for Deserialization.")]
    [RequiresDynamicCode("Using Get with Type requires types to be preserved for Deserialization.")]
#endif
    public static IObservable<object?> Get(this IBlobCache blobCache, string key, Type type)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
        }

        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return blobCache.Get(key, type).Select(bytes =>
        {
            if (bytes == null)
            {
                return null;
            }

            // Use reflection to call Deserialize<T> with the correct type
            var method = typeof(ISerializer).GetMethod(nameof(ISerializer.Deserialize))!;
            var genericMethod = method.MakeGenericMethod(type);
            return genericMethod.Invoke(blobCache.Serializer, [bytes]);
        });
    }

    /// <summary>
    /// Return all objects of a specific Type in the cache.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="blobCache">The blob cache.</param>
    /// <returns>A Future result representing all objects in the cache
    /// with the specified Type.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetAllObjects requires types to be preserved for Deserialization.")]
    [RequiresDynamicCode("Using GetAllObjects requires types to be preserved for Deserialization.")]
#endif
    public static IObservable<T> GetAllObjects<T>(this IBlobCache blobCache) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache
                .GetAll(typeof(T))
                .Select(x => blobCache.Serializer.Deserialize<T>(x.Value))
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
            throw new ArgumentException($"'-{nameof(key)}' cannot be null or whitespace.", nameof(key));
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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using InsertAllObjects requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using InsertAllObjects requires types to be preserved for serialization.")]
#endif
    public static IObservable<Unit> InsertAllObjects<T>(this IBlobCache blobCache, IEnumerable<KeyValuePair<string, T>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.Insert(keyValuePairs.Select(x => new KeyValuePair<string, byte[]>(x.Key, blobCache.Serializer.Serialize(x.Value))), absoluteExpiration);

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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
#endif
    public static IObservable<T?> GetOrFetchObject<T>(this IBlobCache blobCache, string key, Func<IObservable<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (fetchFunc is null)
        {
            throw new ArgumentNullException(nameof(fetchFunc));
        }

        // Try to get from cache first
        return blobCache.GetObject<T>(key).Catch<T?, Exception>(ex =>
        {
            // When a cache miss occurs (either key not found or expired),
            // we need to fetch the data. We use RequestCache to deduplicate
            // concurrent requests for the same key, but we should only clear
            // the RequestCache when we're sure the cache entry has expired,
            // not just on any cache miss.

            // Use request cache for concurrent request deduplication
            return RequestCache.GetOrCreateRequest(key, () =>
                fetchFunc().SelectMany(value =>
                    blobCache.InsertObject(key, value, absoluteExpiration)
                        .Select(__ => value)
                        .Take(1))); // Ensure we only take one result
        });
    }

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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
#endif
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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetOrCreateObject requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using GetOrCreateObject requires types to be preserved for serialization.")]
#endif
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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
#endif
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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
#endif
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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using InsertObjects requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using InsertObjects requires types to be preserved for serialization.")]
#endif
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

        if (keyValuePairs.Count == 0)
        {
            return Observable.Return(Unit.Default);
        }

        // For mixed object types, we need to serialize each one individually and use its specific type
        var insertOperations = keyValuePairs
            .Select(kvp => blobCache.Insert(kvp.Key, blobCache.Serializer.Serialize(kvp.Value), kvp.Value?.GetType() ?? typeof(object), absoluteExpiration))
            .ToList();

        // Wait for all insert operations to complete by merging and taking the count
        return insertOperations.Merge()
            .TakeLast(insertOperations.Count)
            .LastOrDefaultAsync()
            .Select(_ => Unit.Default);
    }

    /// <summary>
    /// Attempts to serialize an object with context and enhanced compatibility.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="cache">The cache.</param>
    /// <returns>
    /// The serialized data as byte array.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails.</exception>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Serialization requires types to be preserved.")]
    [RequiresDynamicCode("Serialization requires types to be preserved.")]
#endif
    public static byte[] SerializeWithContext<T>(T value, IBlobCache cache)
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        var serializer = cache.Serializer;

        try
        {
            // For DateTime objects, use the Universal Serializer Shim for better compatibility
            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                return UniversalSerializer.Serialize(value, serializer, cache.ForcedDateTimeKind);
            }

            // For regular serialization, apply forced DateTime kind if specified
            if (cache.ForcedDateTimeKind.HasValue)
            {
                serializer.ForcedDateTimeKind = cache.ForcedDateTimeKind;
            }

            return serializer.Serialize(value);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to serialize object of type {typeof(T).Name}. " +
                "Please ensure a CacheDatabase serializer package is referenced and properly initialized. " +
                $"Error: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize data with context and enhanced compatibility.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    /// <param name="cache">The cache.</param>
    /// <returns>
    /// The deserialized object.
    /// </returns>
    /// <exception cref="InvalidOperationException">$"Failed to deserialize data to type {typeof(T).Name}. " +
    ///                 $"Data length: {data.Length} bytes. " +
    ///                 "Please ensure the data was serialized with a compatible serializer. " +
    ///                 $"Error: {ex.Message}, ex.</exception>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Deserialization requires types to be preserved.")]
    [RequiresDynamicCode("Deserialization requires types to be preserved.")]
#endif
    public static T? DeserializeWithContext<T>(byte[] data, IBlobCache cache)
    {
        if (cache == null || data == null || data.Length == 0)
        {
            return default;
        }

        var serializer = cache.Serializer;

        try
        {
            // For DateTime objects, use the Universal Serializer Shim for better compatibility
            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                return UniversalSerializer.Deserialize<T>(data, serializer, cache.ForcedDateTimeKind);
            }

            // For regular deserialization, apply forced DateTime kind if specified
            if (cache.ForcedDateTimeKind.HasValue)
            {
                serializer.ForcedDateTimeKind = cache.ForcedDateTimeKind;
            }

            return serializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            // For critical DateTime failures, try the Universal Serializer Shim as a fallback
            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?) ||
                typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTimeOffset?))
            {
                try
                {
                    return UniversalSerializer.Deserialize<T>(data, serializer, cache.ForcedDateTimeKind);
                }
                catch
                {
                    // If even the universal shim fails, throw the original exception
                }
            }

            throw new InvalidOperationException(
                $"Failed to deserialize data to type {typeof(T).Name}. " +
                $"Data length: {data.Length} bytes. " +
                "Please ensure the data was serialized with a compatible serializer. " +
                $"Error: {ex.Message}",
                ex);
        }
    }

    internal static string GetTypePrefixedKey(this string key, Type type) => type.FullName + "___" + key;
}
