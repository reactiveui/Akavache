// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// A interface that handles bulk add/remove/invalidate functionality over many key/value pairs.
/// </summary>
public interface IBulkBlobCache : IBlobCache
{
    /// <summary>
    /// Inserts several keys into the database at one time. If any individual
    /// insert fails, this operation should cancel the entire insert (i.e. it
    /// should *not* partially succeed).
    /// </summary>
    /// <param name="keyValuePairs">The keys and values to insert.</param>
    /// <param name="absoluteExpiration">An optional expiration date.
    /// After the specified date, the key-value pair should be removed.</param>
    /// <returns>A signal to indicate when the key has been inserted.</returns>
    IObservable<Unit> Insert(IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Retrieve several values from the key-value cache. If any of
    /// the keys are not in the cache, this method should return an
    /// IObservable which OnError's with KeyNotFoundException.
    /// </summary>
    /// <param name="keys">The keys to return asynchronously.</param>
    /// <returns>A Future result representing the byte data for each key.</returns>
    IObservable<IDictionary<string, byte[]>> Get(IEnumerable<string> keys);

    /// <summary>
    /// Returns the time that the keys were added to the cache, or returns
    /// null if the key isn't in the cache.
    /// </summary>
    /// <param name="keys">The keys to return the date for.</param>
    /// <returns>The date the key was created on.</returns>
    IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(IEnumerable<string> keys);

    /// <summary>
    /// Remove several keys from the cache. If the key doesn't exist, this method
    /// should do nothing and return (*not* throw KeyNotFoundException).
    /// </summary>
    /// <param name="keys">The key to remove from the cache.</param>
    /// <returns>A observable that signals when the operational is completed.</returns>
    IObservable<Unit> Invalidate(IEnumerable<string> keys);
}