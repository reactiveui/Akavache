// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json.Bson;

namespace Akavache;

/// <summary>
/// IBlobCache is the core interface on which Akavache is built, it is an
/// interface describing an asynchronous persistent key-value store.
/// </summary>
public interface IBlobCache : IDisposable
{
    /// <summary>
    /// Gets an Observable that fires after the Dispose completes successfully,
    /// since there is no such thing as an AsyncDispose().
    /// </summary>
    IObservable<Unit> Shutdown { get; }

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
    /// By default, <see cref="BsonReader"/> uses a <see cref="DateTimeKind"/> of <see cref="DateTimeKind.Local"/> and <see cref="BsonWriter"/>
    /// uses <see cref="DateTimeKind.Utc"/>. Thus, DateTimes are serialized as UTC but deserialized as local time. To force BSON readers to
    /// use some other <c>DateTimeKind</c>, you can set this value.
    /// </para>
    /// </remarks>
    DateTimeKind? ForcedDateTimeKind { get; set; }

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
    /// Retrieve a value from the key-value cache. If the key is not in
    /// the cache, this method should return an IObservable which
    /// OnError's with KeyNotFoundException.
    /// </summary>
    /// <param name="key">The key to return asynchronously.</param>
    /// <returns>A Future result representing the byte data.</returns>
    IObservable<byte[]> Get(string key);

    /// <summary>
    /// Return all keys in the cache. Note that this method is normally
    /// for diagnostic / testing purposes, and that it is not guaranteed
    /// to be accurate with respect to in-flight requests.
    /// </summary>
    /// <returns>A list of valid keys for the cache.</returns>
    IObservable<IEnumerable<string>> GetAllKeys();

    /// <summary>
    /// Returns the time that the key was added to the cache, or returns
    /// null if the key isn't in the cache.
    /// </summary>
    /// <param name="key">The key to return the date for.</param>
    /// <returns>The date the key was created on.</returns>
    IObservable<DateTimeOffset?> GetCreatedAt(string key);

    /// <summary>
    /// This method guarantees that all in-flight inserts have completed
    /// and any indexes have been written to disk.
    /// </summary>
    /// <returns>A signal indicating when the flush is complete.</returns>
    IObservable<Unit> Flush();

    /// <summary>
    /// Remove a key from the cache. If the key doesn't exist, this method
    /// should do nothing and return (*not* throw KeyNotFoundException).
    /// </summary>
    /// <param name="key">The key to remove from the cache.</param>
    /// <returns>A signal indicating when the invalidate is complete.</returns>
    IObservable<Unit> Invalidate(string key);

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
}