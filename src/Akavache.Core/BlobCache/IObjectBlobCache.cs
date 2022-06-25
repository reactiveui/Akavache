// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// A BlobCache implementation that can handle objects.
/// </summary>
public interface IObjectBlobCache : IBlobCache
{
    /// <summary>
    /// Insert an object into the cache, via the JSON serializer.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="value">The object to serialize.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
    IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Get an object from the cache and deserialize it via the JSON
    /// serializer.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="key">The key to look up in the cache.</param>
    /// <returns>A Future result representing the object in the cache.</returns>
    IObservable<T?> GetObject<T>(string key);

    /// <summary>
    /// Return all objects of a specific Type in the cache.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <returns>A Future result representing all objects in the cache
    /// with the specified Type.</returns>
    IObservable<IEnumerable<T>> GetAllObjects<T>();

    /// <summary>
    /// Returns the time that the object with the key was added to the cache, or returns
    /// null if the key isn't in the cache.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="key">The key to return the date for.</param>
    /// <returns>The date the key was created on.</returns>
    IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key);

    /// <summary>
    /// Invalidates a single object from the cache. It is important that the Type
    /// Parameter for this method be correct, and you cannot use
    /// IBlobCache.Invalidate to perform the same task.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    IObservable<Unit> InvalidateObject<T>(string key);

    /// <summary>
    /// Invalidates all objects of the specified type. To invalidate all
    /// objects regardless of type, use InvalidateAll.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <returns>
    /// A Future result representing the completion of the invalidation.</returns>
    IObservable<Unit> InvalidateAllObjects<T>();
}