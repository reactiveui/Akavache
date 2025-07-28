// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// IBlobCache is the core database interface, it is an
/// interface describing an asynchronous persistent key-value store.
/// </summary>
public interface IBlobCache : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the IScheduler used to defer operations. By default, this is
    /// BlobCache.TaskpoolScheduler.
    /// </summary>
    IScheduler Scheduler { get; }

    /// <summary>
    /// Gets or sets the DateTimeKind handling for BSON readers to be forced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, BsonReader uses a <see cref="DateTimeKind"/> of <see cref="DateTimeKind.Local"/> and see BsonWriter
    /// uses <see cref="DateTimeKind.Utc"/>. Thus, DateTimes are serialized as UTC but deserialized as local time. To force BSON readers to
    /// use some other <c>DateTimeKind</c>, you can set this value.
    /// </para>
    /// </remarks>
    DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <summary>
    /// Inserts the specified key/value pairs into the blob.
    /// </summary>
    /// <param name="keyValuePairs">The key/value to insert.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A observable which signals when complete.</returns>
    IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Insert a blob into the cache with the specified key and expiration
    /// date.
    /// </summary>
    /// <param name="key">The key to use for the data.</param>
    /// <param name="data">The data to save in the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.
    /// After the specified date, the key-value pair should be removed.</param>
    /// <returns>A signal to indicate when the key has been inserted.</returns>
    IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Inserts the specified key/value pairs into the blob.
    /// </summary>
    /// <param name="keyValuePairs">The key/value to insert.</param>
    /// <param name="type">The type.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A observable which signals when complete.</returns>
    IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Insert a blob into the cache with the specified key and expiration
    /// date.
    /// </summary>
    /// <param name="key">The key to use for the data.</param>
    /// <param name="data">The data to save in the cache.</param>
    /// <param name="type">The type.</param>
    /// <param name="absoluteExpiration">An optional expiration date.
    /// After the specified date, the key-value pair should be removed.</param>
    /// <returns>A signal to indicate when the key has been inserted.</returns>
    IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Retrieve a value from the key-value cache. If the key is not in
    /// the cache, this method should return an IObservable which
    /// OnError's with KeyNotFoundException.
    /// </summary>
    /// <param name="key">The key to return asynchronously.</param>
    /// <returns>A Future result representing the byte data.</returns>
    IObservable<byte[]?> Get(string key);

    /// <summary>
    /// Gets a observable of key value pairs with the specified keys with their corresponding values.
    /// </summary>
    /// <param name="keys">The keys to get the values for.</param>
    /// <returns>A observable with the specified values.</returns>
    IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys);

    /// <summary>
    /// Retrieve a value from the key-value cache. If the key is not in
    /// the cache, this method should return an IObservable which
    /// OnError's with KeyNotFoundException.
    /// </summary>
    /// <param name="key">The key to return asynchronously.</param>
    /// <param name="type">The type.</param>
    /// <returns>A Future result representing the byte data.</returns>
    IObservable<byte[]?> Get(string key, Type type);

    /// <summary>
    /// Gets a observable of key value pairs with the specified keys with their corresponding values.
    /// </summary>
    /// <param name="keys">The keys to get the values for.</param>
    /// <param name="type">The type.</param>
    /// <returns>A observable with the specified values.</returns>
    IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type);

    /// <summary>
    /// Gets a observable of key value pairs with the specified keys with their corresponding values.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>A observable with the specified values.</returns>
    IObservable<KeyValuePair<string, byte[]>> GetAll(Type type);

    /// <summary>
    /// Return all keys in the cache. Note that this method is normally
    /// for diagnostic / testing purposes, and that it is not guaranteed
    /// to be accurate with respect to in-flight requests.
    /// </summary>
    /// <returns>A list of valid keys for the cache.</returns>
    IObservable<string> GetAllKeys();

    /// <summary>
    /// Return all keys in the cache. Note that this method is normally
    /// for diagnostic / testing purposes, and that it is not guaranteed
    /// to be accurate with respect to in-flight requests.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>A list of valid keys for the cache.</returns>
    IObservable<string> GetAllKeys(Type type);

    /// <summary>
    /// Gets a observable of key value pairs with the specified keys with their corresponding created <see cref="DateTimeOffset"/>
    /// if it's available.
    /// </summary>
    /// <param name="keys">The keys to get the values for.</param>
    /// <returns>A observable with the specified values.</returns>
    IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys);

    /// <summary>
    /// Returns the time that the key was added to the cache, or returns
    /// null if the key isn't in the cache.
    /// </summary>
    /// <param name="key">The key to return the date for.</param>
    /// <returns>The date the key was created on.</returns>
    IObservable<DateTimeOffset?> GetCreatedAt(string key);

    /// <summary>
    /// Gets a observable of key value pairs with the specified keys with their corresponding created <see cref="DateTimeOffset"/>
    /// if it's available.
    /// </summary>
    /// <param name="keys">The keys to get the values for.</param>
    /// <param name="type">The type.</param>
    /// <returns>A observable with the specified values.</returns>
    IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type);

    /// <summary>
    /// Returns the time that the key was added to the cache, or returns
    /// null if the key isn't in the cache.
    /// </summary>
    /// <param name="key">The key to return the date for.</param>
    /// <param name="type">The type.</param>
    /// <returns>The date the key was created on.</returns>
    IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type);

    /// <summary>
    /// This method guarantees that all in-flight inserts have completed
    /// and any indexes have been written to disk.
    /// </summary>
    /// <returns>A signal indicating when the flush is complete.</returns>
    IObservable<Unit> Flush();

    /// <summary>
    /// This method guarantees that all in-flight inserts have completed
    /// and any indexes have been written to disk.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>A signal indicating when the flush is complete.</returns>
    IObservable<Unit> Flush(Type type);

    /// <summary>
    /// Remove a key from the cache. If the key doesn't exist, this method
    /// should do nothing and return (*not* throw KeyNotFoundException).
    /// </summary>
    /// <param name="key">The key to remove from the cache.</param>
    /// <returns>A signal indicating when the invalidate is complete.</returns>
    IObservable<Unit> Invalidate(string key);

    /// <summary>
    /// Remove a key from the cache. If the key doesn't exist, this method
    /// should do nothing and return (*not* throw KeyNotFoundException).
    /// </summary>
    /// <param name="key">The key to remove from the cache.</param>
    /// <param name="type">The type.</param>
    /// <returns>A signal indicating when the invalidate is complete.</returns>
    IObservable<Unit> Invalidate(string key, Type type);

    /// <summary>
    /// Invalidates all the entries at the specified keys, causing them in future to have to be re-fetched.
    /// </summary>
    /// <param name="keys">The keys to invalid.</param>
    /// <returns>A observable which signals when complete.</returns>
    IObservable<Unit> Invalidate(IEnumerable<string> keys);

    /// <summary>
    /// Invalidates all entries for the specified type.
    /// </summary>
    /// <param name="type">The type to invalidate.</param>
    /// <returns>A signal indicating when the invalidate is complete.</returns>
    IObservable<Unit> InvalidateAll(Type type);

    /// <summary>
    /// Invalidates all the entries at the specified keys, causing them in future to have to be re-fetched.
    /// </summary>
    /// <param name="keys">The keys to invalid.</param>
    /// <param name="type">The type to invalidate.</param>
    /// <returns>A observable which signals when complete.</returns>
    IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type);

    /// <summary>
    /// Invalidate all entries in the cache (i.e. clear it). Note that
    /// this method is blocking and incurs a significant performance
    /// penalty if used while the cache is being used on other threads.
    /// </summary>
    /// <returns>A signal indicating when the invalidate is complete.</returns>
    IObservable<Unit> InvalidateAll();

    /// <summary>
    /// This method eagerly removes all expired keys from the blob cache, as
    /// well as does any cleanup operations that makes sense (Hint: on SQLite3
    /// it does a Vacuum).
    /// </summary>
    /// <returns>A signal indicating when the operation is complete.</returns>
    IObservable<Unit> Vacuum();

    /// <summary>
    /// Exception helpers for implementers of the class.
    /// </summary>
    public static class ExceptionHelpers
    {
        /// <summary>
        /// Throws an key not found exception in an observable.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="key">The key not found.</param>
        /// <param name="innerException">The inner exception if any.</param>
        /// <returns>The observable.</returns>
        public static IObservable<T> ObservableThrowKeyNotFoundException<T>(string key, Exception? innerException = null) =>
            Observable.Throw<T>(
                new KeyNotFoundException($"The given key '{key}' was not present in the cache.", innerException));

        /// <summary>
        /// Throws an exception that the object is disposed.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="obj">The object name that is disposed.</param>
        /// <param name="innerException">The inner exception if any.</param>
        /// <returns>The observable.</returns>
        public static IObservable<T> ObservableThrowObjectDisposedException<T>(string obj, Exception? innerException = null) =>
            Observable.Throw<T>(
                new ObjectDisposedException($"The cache '{obj}' was disposed.", innerException));
    }
}
