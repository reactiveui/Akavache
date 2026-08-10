// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>Provides extension methods for serializer operations on blob caches.</summary>
public static class SerializerExtensions
{
    /// <summary>Extension members for <c>IBlobCache</c>.</summary>
    /// <param name="blobCache">The blob cache to insert into.</param>
    extension(IBlobCache blobCache)
    {
        /// <summary>Inserts multiple objects into the cache with their associated keys.</summary>
        /// <typeparam name="T">The type of items to insert.</typeparam>
        /// <param name="keyValuePairs">The key-value pairs to insert.</param>
        /// <returns>An observable that signals when the operation is complete.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using InsertObjects requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using InsertObjects requires types to be preserved for serialization.")]
        public IObservable<RxVoid> InsertObjects<T>(IEnumerable<KeyValuePair<string, T>> keyValuePairs) =>
            blobCache.InsertObjects(keyValuePairs, (DateTimeOffset?)null);

        /// <summary>Inserts multiple objects into the cache with their associated keys.</summary>
        /// <typeparam name="T">The type of items to insert.</typeparam>
        /// <param name="keyValuePairs">The key-value pairs to insert.</param>
        /// <param name="absoluteExpiration">An optional expiration date for the cached data.</param>
        /// <returns>An observable that signals when the operation is complete.</returns>
        [RequiresUnreferencedCode("Using InsertObjects requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using InsertObjects requires types to be preserved for serialization.")]
        public IObservable<RxVoid> InsertObjects<T>(IEnumerable<KeyValuePair<string, T>> keyValuePairs, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.Insert(SerializeValues(blobCache, keyValuePairs), typeof(T), absoluteExpiration);
        }

        /// <summary>Insert several objects of mixed types into the cache.</summary>
        /// <param name="keyValuePairs">The data to insert into the cache.</param>
        /// <returns>A Future result representing the completion of the insert.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using InsertObjects requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using InsertObjects requires types to be preserved for serialization.")]
        public IObservable<RxVoid> InsertObjects(IDictionary<string, object> keyValuePairs) =>
            blobCache.InsertObjects(keyValuePairs, (DateTimeOffset?)null);

        /// <summary>Insert several objects of mixed types into the cache.</summary>
        /// <param name="keyValuePairs">The data to insert into the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the completion of the insert.</returns>
        [RequiresUnreferencedCode("Using InsertObjects requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using InsertObjects requires types to be preserved for serialization.")]
        public IObservable<RxVoid> InsertObjects(IDictionary<string, object> keyValuePairs, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentExceptionHelper.ThrowIfNull(keyValuePairs);

            if (keyValuePairs.Count == 0)
            {
                return ImmutableReturnRxVoidSignal.Instance;
            }

            // For mixed object types, we need to serialize each one individually and use its specific type.
            var insertOperations = new IObservable<RxVoid>[keyValuePairs.Count];
            var index = 0;
            foreach (var kvp in keyValuePairs)
            {
                var value = kvp.Value;
                insertOperations[index] = blobCache.Insert(kvp.Key, blobCache.Serializer.Serialize(value), value?.GetType() ?? typeof(object), absoluteExpiration);
                index++;
            }

            return insertOperations.RunAll();
        }

        /// <summary>Retrieves objects from the cache for the specified keys.</summary>
        /// <typeparam name="T">The type of items to retrieve.</typeparam>
        /// <param name="keys">The keys for the objects to retrieve.</param>
        /// <returns>An observable that emits key-value pairs for the found objects.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        [RequiresUnreferencedCode("Using GetObjects requires types to be preserved for Deserialization.")]
        [RequiresDynamicCode("Using GetObjects requires types to be preserved for Deserialization.")]
        public IObservable<KeyValuePair<string, T>> GetObjects<T>(IEnumerable<string> keys)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache
                .Get(keys, typeof(T))
                .Select(x => (x.Key, Data: blobCache.Serializer.Deserialize<T>(x.Value)))
                .WhereSelect(static x => x.Data is not null, static x => new KeyValuePair<string, T>(x.Key, x.Data!));
        }

        /// <summary>Inserts an object into the cache using the configured serializer.</summary>
        /// <typeparam name="T">The type of object to insert.</typeparam>
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="value">The object to serialize and cache.</param>
        /// <returns>An observable that signals when the insertion is complete.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using InsertObject requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using InsertObject requires types to be preserved for serialization.")]
        public IObservable<RxVoid> InsertObject<T>(string key, T value) =>
            blobCache.InsertObject(key, value, (DateTimeOffset?)null);

        /// <summary>Inserts an object into the cache using the configured serializer.</summary>
        /// <typeparam name="T">The type of object to insert.</typeparam>
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="value">The object to serialize and cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date for the cached data.</param>
        /// <returns>An observable that signals when the insertion is complete.</returns>
        /// <exception cref="InvalidOperationException">No serializer has been registered for the cache.</exception>
        [RequiresUnreferencedCode("Using InsertObject requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using InsertObject requires types to be preserved for serialization.")]
        public IObservable<RxVoid> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentValidation.ThrowIfNullOrWhiteSpace(key);

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

        /// <summary>Get an object from the cache and deserialize it via the JSON serializer.</summary>
        /// <typeparam name="T">The type of object associated with the blob.</typeparam>
        /// <param name="key">The key to look up in the cache.</param>
        /// <returns>A Future result representing the object in the cache.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        [RequiresUnreferencedCode("Using GetObject requires types to be preserved for Deserialization.")]
        [RequiresDynamicCode("Using GetObject requires types to be preserved for Deserialization.")]
        public IObservable<T?> GetObject<T>(string key)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentValidation.ThrowIfNullOrWhiteSpace(key);

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

        /// <summary>Return all objects of a specific Type in the cache.</summary>
        /// <typeparam name="T">The type of object associated with the blob.</typeparam>
        /// <returns>A Future result representing all objects in the cache
        /// with the specified Type.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        [RequiresUnreferencedCode("Using GetAllObjects requires types to be preserved for Deserialization.")]
        [RequiresDynamicCode("Using GetAllObjects requires types to be preserved for Deserialization.")]
        public IObservable<T> GetAllObjects<T>()
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache
                .GetAll(typeof(T))
                .TrySelect(x => blobCache.Serializer.Deserialize<T>(x.Value));
        }

        /// <summary>Returns the time that the object with the key was added to the cache, or returns null if the key isn't in the cache.</summary>
        /// <typeparam name="T">The type of object associated with the blob.</typeparam>
        /// <param name="key">The key to return the date for.</param>
        /// <returns>The date the key was created on.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentValidation.ThrowIfNullOrWhiteSpace(key);

            return blobCache.GetCreatedAt(key, typeof(T));
        }

        /// <summary>
        /// Invalidates a single object from the cache. It is important that the Type
        /// Parameter for this method be correct, and you cannot use
        /// IBlobCache.Invalidate to perform the same task.
        /// </summary>
        /// <typeparam name="T">The type of object associated with the blob.</typeparam>
        /// <param name="key">The key to invalidate.</param>
        /// <returns>A Future result representing the completion of the invalidation.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public IObservable<RxVoid> InvalidateObject<T>(string key)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentValidation.ThrowIfNullOrWhiteSpace(key);

            return blobCache.Invalidate(key, typeof(T));
        }

        /// <summary>
        /// Invalidates several objects from the cache. It is important that the Type
        /// Parameter for this method be correct, and you cannot use
        /// IBlobCache.Invalidate to perform the same task.
        /// </summary>
        /// <typeparam name="T">The type of object associated with the blob.</typeparam>
        /// <param name="keys">The keys to invalidate.</param>
        /// <returns>A Future result representing the completion of the invalidation.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public IObservable<RxVoid> InvalidateObjects<T>(IEnumerable<string> keys)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentExceptionHelper.ThrowIfNull(keys);

            return blobCache.Invalidate(keys, typeof(T));
        }

        /// <summary>Invalidates all objects of the specified type. To invalidate all objects regardless of type, use InvalidateAll.</summary>
        /// <typeparam name="T">The type of object associated with the blob.</typeparam>
        /// <returns>A Future result representing the completion of the invalidation.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public IObservable<RxVoid> InvalidateAllObjects<T>()
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.InvalidateAll(typeof(T));
        }

        /// <summary>
        /// Insert several objects into the cache, via the JSON serializer.
        /// Similarly to InsertAll, partial inserts should not happen.
        /// </summary>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <param name="keyValuePairs">The data to insert into the cache.</param>
        /// <returns>A Future result representing the completion of the insert.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using InsertAllObjects requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using InsertAllObjects requires types to be preserved for serialization.")]
        public IObservable<RxVoid> InsertAllObjects<T>(IEnumerable<KeyValuePair<string, T>> keyValuePairs) =>
            blobCache.InsertAllObjects(keyValuePairs, (DateTimeOffset?)null);

        /// <summary>
        /// Insert several objects into the cache, via the JSON serializer.
        /// Similarly to InsertAll, partial inserts should not happen.
        /// </summary>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <param name="keyValuePairs">The data to insert into the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the completion of the insert.</returns>
        [RequiresUnreferencedCode("Using InsertAllObjects requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using InsertAllObjects requires types to be preserved for serialization.")]
        public IObservable<RxVoid> InsertAllObjects<T>(IEnumerable<KeyValuePair<string, T>> keyValuePairs, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.Insert(SerializeValues(blobCache, keyValuePairs), absoluteExpiration);
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
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="fetchFunc">
        /// <para>A Func which will asynchronously return the latest value for the object
        /// should the cache not contain the key.</para>
        /// <para>Observable.Start is the most straightforward way (though not the
        /// most efficient!) to implement this Func.</para>
        /// </param>
        /// <returns>A Future result representing the deserialized object from
        /// the cache.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
        public IObservable<T?> GetOrFetchObject<T>(string key, Func<IObservable<T>> fetchFunc) =>
            blobCache.GetOrFetchObject(key, fetchFunc, (DateTimeOffset?)null);

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
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="fetchFunc">
        /// <para>A Func which will asynchronously return the latest value for the object
        /// should the cache not contain the key.</para>
        /// <para>Observable.Start is the most straightforward way (though not the
        /// most efficient!) to implement this Func.</para>
        /// </param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the deserialized object from
        /// the cache.</returns>
        [RequiresUnreferencedCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
        public IObservable<T?> GetOrFetchObject<T>(string key, Func<IObservable<T>> fetchFunc, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentExceptionHelper.ThrowIfNull(fetchFunc);

            // Try to get from cache first. When a cache miss occurs (either key not found or
            // expired), we need to fetch the data. We use RequestCache to deduplicate concurrent
            // requests for the same key, but we should only clear the RequestCache when we're sure
            // the cache entry has expired, not just on any cache miss.
            return blobCache.GetObject<T>(key).Catch<T?, Exception>(_ =>
                RequestCache.GetOrCreateRequest(key, () =>
                    fetchFunc().SelectMany(value =>
                        blobCache.InsertObject(key, value, absoluteExpiration)
                            .SelectConstant(value)
                            .Take(1)))); // Ensure we only take one result
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
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="fetchFunc">A Func which will asynchronously return
        /// the latest value for the object should the cache not contain the
        /// key. </param>
        /// <returns>A Future result representing the deserialized object from
        /// the cache.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
        public IObservable<T?> GetOrFetchObject<T>(string key, Func<Task<T>> fetchFunc) =>
            blobCache.GetOrFetchObject(key, fetchFunc, (DateTimeOffset?)null);

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
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="fetchFunc">A Func which will asynchronously return
        /// the latest value for the object should the cache not contain the
        /// key. </param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the deserialized object from
        /// the cache.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetOrFetchObject requires types to be preserved for serialization.")]
        public IObservable<T?> GetOrFetchObject<T>(string key, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration) =>
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
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="fetchFunc">A Func which will return
        /// the latest value for the object should the cache not contain the
        /// key. </param>
        /// <returns>A Future result representing the deserialized object from
        /// the cache.</returns>
        [RequiresUnreferencedCode("Using GetOrCreateObject requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetOrCreateObject requires types to be preserved for serialization.")]
        public IObservable<T?> GetOrCreateObject<T>(string key, Func<T> fetchFunc)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.GetObject<T>(key).Catch<T?, Exception>(_ =>
                {
                    var value = fetchFunc();
                    return blobCache.InsertObject(key, value).SelectConstant(value);
                });
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
        /// This also means that await'ing this method is a Bad Idea(tm), always
        /// use Subscribe.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of item to get.</typeparam>
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc">A method to fetch a observable.</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        public IObservable<T?> GetAndFetchLatest<T>(string key, Func<IObservable<T>> fetchFunc) =>
            blobCache.GetAndFetchLatest(key, fetchFunc, (Func<DateTimeOffset, bool>?)null, (DateTimeOffset?)null, false, (Func<T, bool>?)null);

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
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc">A method to fetch a observable.</param>
        /// <param name="fetchPredicate">An optional Func to determine whether
        /// the updated item should be fetched. If the cached version isn't found,
        /// this parameter is ignored and the item is always fetched.</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        public IObservable<T?> GetAndFetchLatest<T>(string key, Func<IObservable<T>> fetchFunc, Func<DateTimeOffset, bool>? fetchPredicate) =>
            blobCache.GetAndFetchLatest(key, fetchFunc, fetchPredicate, (DateTimeOffset?)null, false, (Func<T, bool>?)null);

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
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc">A method to fetch a observable.</param>
        /// <param name="fetchPredicate">An optional Func to determine whether
        /// the updated item should be fetched. If the cached version isn't found,
        /// this parameter is ignored and the item is always fetched.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        public IObservable<T?> GetAndFetchLatest<T>(string key, Func<IObservable<T>> fetchFunc, Func<DateTimeOffset, bool>? fetchPredicate, DateTimeOffset? absoluteExpiration) =>
            blobCache.GetAndFetchLatest(key, fetchFunc, fetchPredicate, absoluteExpiration, false, (Func<T, bool>?)null);

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
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc">A method to fetch a observable.</param>
        /// <param name="fetchPredicate">An optional Func to determine whether
        /// the updated item should be fetched. If the cached version isn't found,
        /// this parameter is ignored and the item is always fetched.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <param name="shouldInvalidateOnError">If this is true, the cache will
        /// be cleared when an exception occurs in fetchFunc.</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        public IObservable<T?> GetAndFetchLatest<T>(
            string key,
            Func<IObservable<T>> fetchFunc,
            Func<DateTimeOffset, bool>? fetchPredicate,
            DateTimeOffset? absoluteExpiration,
            bool shouldInvalidateOnError) =>
            blobCache.GetAndFetchLatest(key, fetchFunc, fetchPredicate, absoluteExpiration, shouldInvalidateOnError, (Func<T, bool>?)null);

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
        [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        public IObservable<T?> GetAndFetchLatest<T>(
            string key,
            Func<IObservable<T>> fetchFunc,
            Func<DateTimeOffset, bool>? fetchPredicate,
            DateTimeOffset? absoluteExpiration,
            bool shouldInvalidateOnError,
            Func<T, bool>? cacheValidationPredicate)
        {
            var fetch = Signal.Defer(() => blobCache.GetObjectCreatedAt<T>(key))
                .Select(x => SerializerHelpers.ShouldRefetchCachedValue(fetchPredicate, x))
                .Where(static x => x)
                .SelectMany(_ =>
                {
                    var fetchObs = fetchFunc().Catch<T, Exception>(ex =>
                    {
                        var shouldInvalidate = shouldInvalidateOnError
                            ? blobCache.InvalidateObject<T>(key)
                            : ImmutableReturnRxVoidSignal.Instance;
                        return shouldInvalidate.SelectMany(_ => new ImmediateThrowSignal<T>(ex));
                    });

                    return fetchObs
                        .SelectMany(x =>
                            cacheValidationPredicate is not null && !cacheValidationPredicate(x)
                                ? Signal.Return(default(T))
                                : blobCache.InvalidateObject<T>(key).SelectConstant(x))
                        .SelectMany(x =>
                            cacheValidationPredicate is not null && !cacheValidationPredicate(x!)
                                ? Signal.Return(default(T))
                                : blobCache.InsertObject(key, x, absoluteExpiration).SelectConstant(x));
                });

            var result = blobCache.GetObject<T>(key).Select(static x => (x, true))
                .Catch<(T?, bool), Exception>(static _ => Signal.Return((default(T), false)));

            return result.SelectMany(static x => x.Item2 ? Signal.Return(x.Item1) : ImmutableEmptySignal<T>.Instance)
                .Concat(fetch)
                .Multicast(new ReplaySignal<T?>())
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
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc">A method that will fetch the task.</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        public IObservable<T?> GetAndFetchLatest<T>(string key, Func<Task<T>> fetchFunc) =>
            blobCache.GetAndFetchLatest(key, fetchFunc, (Func<DateTimeOffset, bool>?)null, (DateTimeOffset?)null, false, (Func<T, bool>?)null);

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
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc">A method that will fetch the task.</param>
        /// <param name="fetchPredicate">An optional Func to determine whether
        /// the updated item should be fetched. If the cached version isn't found,
        /// this parameter is ignored and the item is always fetched.</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        public IObservable<T?> GetAndFetchLatest<T>(string key, Func<Task<T>> fetchFunc, Func<DateTimeOffset, bool>? fetchPredicate) =>
            blobCache.GetAndFetchLatest(key, fetchFunc, fetchPredicate, (DateTimeOffset?)null, false, (Func<T, bool>?)null);

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
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc">A method that will fetch the task.</param>
        /// <param name="fetchPredicate">An optional Func to determine whether
        /// the updated item should be fetched. If the cached version isn't found,
        /// this parameter is ignored and the item is always fetched.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        public IObservable<T?> GetAndFetchLatest<T>(string key, Func<Task<T>> fetchFunc, Func<DateTimeOffset, bool>? fetchPredicate, DateTimeOffset? absoluteExpiration) =>
            blobCache.GetAndFetchLatest(key, fetchFunc, fetchPredicate, absoluteExpiration, false, (Func<T, bool>?)null);

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
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc">A method that will fetch the task.</param>
        /// <param name="fetchPredicate">An optional Func to determine whether
        /// the updated item should be fetched. If the cached version isn't found,
        /// this parameter is ignored and the item is always fetched.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <param name="shouldInvalidateOnError">If this is true, the cache will
        /// be cleared when an exception occurs in fetchFunc.</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        public IObservable<T?> GetAndFetchLatest<T>(
            string key,
            Func<Task<T>> fetchFunc,
            Func<DateTimeOffset, bool>? fetchPredicate,
            DateTimeOffset? absoluteExpiration,
            bool shouldInvalidateOnError) =>
            blobCache.GetAndFetchLatest(key, fetchFunc, fetchPredicate, absoluteExpiration, shouldInvalidateOnError, (Func<T, bool>?)null);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [RequiresUnreferencedCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        [RequiresDynamicCode("Using GetAndFetchLatest requires types to be preserved for serialization.")]
        public IObservable<T?> GetAndFetchLatest<T>(
            string key,
            Func<Task<T>> fetchFunc,
            Func<DateTimeOffset, bool>? fetchPredicate,
            DateTimeOffset? absoluteExpiration,
            bool shouldInvalidateOnError,
            Func<T, bool>? cacheValidationPredicate) =>
                    blobCache.GetAndFetchLatest(key, () => fetchFunc().ToObservable(), fetchPredicate, absoluteExpiration, shouldInvalidateOnError, cacheValidationPredicate);

        /// <summary>
        /// Safely gets all keys from the cache with null-safety guards.
        /// This method provides a safe alternative to GetAllKeys() that prevents crashes on mobile platforms.
        /// </summary>
        /// <returns>An observable sequence of keys, guaranteed to not be null.</returns>
        public IObservable<string> GetAllKeysSafe()
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.GetAllKeys()
                .Where(static key => !string.IsNullOrEmpty(key)) // Filter out null/empty keys
                .Catch<string, Exception>(static ex =>
                {
                    // Log the exception and return empty sequence instead of crashing
                    System.Diagnostics.Debug.WriteLine($"GetAllKeysSafe caught exception: {ex.Message}");
                    return ImmutableEmptySignal<string>.Instance;
                });
        }

        /// <summary>
        /// Safely gets all keys for a specific type from the cache with null-safety guards.
        /// This method provides a safe alternative to GetAllKeys(Type) that prevents crashes on mobile platforms.
        /// </summary>
        /// <param name="type">The type to filter keys by.</param>
        /// <returns>An observable sequence of keys for the specified type, guaranteed to not be null.</returns>
        public IObservable<string> GetAllKeysSafe(Type type)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentExceptionHelper.ThrowIfNull(type);

            return blobCache.GetAllKeys(type)
                .Where(static key => !string.IsNullOrEmpty(key)) // Filter out null/empty keys
                .Catch<string, Exception>(static ex =>
                {
                    // Log the exception and return empty sequence instead of crashing
                    System.Diagnostics.Debug.WriteLine($"GetAllKeysSafe caught exception: {ex.Message}");
                    return ImmutableEmptySignal<string>.Instance;
                });
        }

        /// <summary>
        /// Safely gets all keys for a specific type from the cache with null-safety guards.
        /// This method provides a safe alternative to GetAllKeys() that prevents crashes on mobile platforms.
        /// </summary>
        /// <typeparam name="T">The type to filter keys by.</typeparam>
        /// <returns>An observable sequence of keys for the specified type, guaranteed to not be null.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public IObservable<string> GetAllKeysSafe<T>()
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.GetAllKeysSafe(typeof(T));
        }
    }

    /// <summary>Attempts to serialize an object with context and enhanced compatibility.</summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="cache">The cache.</param>
    /// <returns>
    /// The serialized data as byte array.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails.</exception>
    [RequiresUnreferencedCode("Serialization requires types to be preserved.")]
    [RequiresDynamicCode("Serialization requires types to be preserved.")]
    public static byte[] SerializeWithContext<T>(T value, IBlobCache cache)
    {
        ArgumentExceptionHelper.ThrowIfNull(cache);

        var serializer = cache.Serializer;

        try
        {
            // For DateTime objects, use the Universal Serializer Shim for better compatibility
            if (SerializerHelpers.IsDateTime(typeof(T)))
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
                $"{$"Failed to serialize object of type {typeof(T).Name}. "}Please ensure a CacheDatabase serializer package is referenced and properly initialized. {$"Error: {ex.Message}"}",
                ex);
        }
    }

    /// <summary>Attempts to deserialize data with context and enhanced compatibility.</summary>
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
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    [RequiresUnreferencedCode("Deserialization requires types to be preserved.")]
    [RequiresDynamicCode("Deserialization requires types to be preserved.")]
    public static T? DeserializeWithContext<T>(byte[] data, IBlobCache cache)
    {
        if (cache is null || data is null || data.Length == 0)
        {
            return default;
        }

        var serializer = cache.Serializer;

        try
        {
            // For DateTime objects, use the Universal Serializer Shim for better compatibility
            if (SerializerHelpers.IsDateTime(typeof(T)))
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
            // For critical DateTime failures, try the Universal Serializer Shim as a fallback.
            // UniversalSerializer.Deserialize swallows exceptions internally and returns
            // default rather than throwing, so no inner try/catch is needed here.
            if (SerializerHelpers.IsDateTimeOrDateTimeOffset(typeof(T)))
            {
                return UniversalSerializer.Deserialize<T>(data, serializer, cache.ForcedDateTimeKind);
            }

            throw new InvalidOperationException(
                $"Failed to deserialize data to type {typeof(T).Name}. Data length: {data.Length} bytes. Please ensure the data was serialized with a compatible serializer. Error: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Lazily replaces each pair's value with its serialized payload. Deferred so the receiving
    /// cache still decides when, and on which thread, serialization happens, matching the
    /// streaming contract of the bulk <c>Insert</c> overloads.
    /// </summary>
    /// <typeparam name="T">The type of the values being serialized.</typeparam>
    /// <param name="blobCache">The cache whose serializer performs the conversion.</param>
    /// <param name="keyValuePairs">The key-value pairs to serialize.</param>
    /// <returns>The pairs with each value replaced by its serialized bytes.</returns>
    [RequiresUnreferencedCode("Serialization requires types to be preserved.")]
    [RequiresDynamicCode("Serialization requires types to be preserved.")]
    internal static IEnumerable<KeyValuePair<string, byte[]>> SerializeValues<T>(IBlobCache blobCache, IEnumerable<KeyValuePair<string, T>> keyValuePairs)
    {
        foreach (var pair in keyValuePairs)
        {
            yield return new(pair.Key, blobCache.Serializer.Serialize(pair.Value));
        }
    }
}
