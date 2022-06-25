// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// A BlobCache implementation that can handle bulk operations with objects.
/// </summary>
public interface IObjectBulkBlobCache : IObjectBlobCache, IBulkBlobCache
{
    /// <summary>
    /// Insert several objects into the cache, via the JSON serializer.
    /// Similarly to InsertAll, partial inserts should not happen.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="keyValuePairs">The data to insert into the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
    IObservable<Unit> InsertObjects<T>(IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Get several objects from the cache and deserialize it via the JSON
    /// serializer.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="keys">The key to look up in the cache.</param>
    /// <returns>A Future result representing the object in the cache.</returns>
    IObservable<IDictionary<string, T>> GetObjects<T>(IEnumerable<string> keys);

    /// <summary>
    /// Invalidates several objects from the cache. It is important that the Type
    /// Parameter for this method be correct, and you cannot use
    /// IBlobCache.Invalidate to perform the same task.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="keys">The key to invalidate.</param>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    IObservable<Unit> InvalidateObjects<T>(IEnumerable<string> keys);
}