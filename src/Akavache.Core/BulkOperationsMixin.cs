// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace Akavache
{
    /// <summary>
    /// Extension methods for the <see cref="IBlobCache"/> that provide bulk operations.
    /// </summary>
    public static class BulkOperationsMixin
    {
        /// <summary>
        /// Gets a dictionary filled with the specified keys with their corresponding values.
        /// </summary>
        /// <param name="blobCache">The blob cache to extract the values from.</param>
        /// <param name="keys">The keys to get the values for.</param>
        /// <returns>A observable with the specified values.</returns>
        public static IObservable<IDictionary<string, byte[]>> Get(this IBlobCache blobCache, IEnumerable<string> keys)
        {
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }

            if (blobCache is IBulkBlobCache bulkCache)
            {
                return bulkCache.Get(keys);
            }

            return keys.ToObservable()
                .SelectMany(x =>
                {
                    return blobCache.Get(x)
                        .Select(y => new KeyValuePair<string, byte[]>(x, y))
                        .Catch<KeyValuePair<string, byte[]>, KeyNotFoundException>(_ => Observable.Empty<KeyValuePair<string, byte[]>>());
                })
                .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Inserts the specified key/value pairs into the blob.
        /// </summary>
        /// <param name="blobCache">The blob cache to insert the values to.</param>
        /// <param name="keyValuePairs">The key/value to insert.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A observable which signals when complete.</returns>
        public static IObservable<Unit> Insert(this IBlobCache blobCache, IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }

            if (blobCache is IBulkBlobCache bulkCache)
            {
                return bulkCache.Insert(keyValuePairs, absoluteExpiration);
            }

            return keyValuePairs.ToObservable()
                .SelectMany(x => blobCache.Insert(x.Key, x.Value, absoluteExpiration))
                .TakeLast(1);
        }

        /// <summary>
        /// Gets a dictionary filled with the specified keys with their corresponding created <see cref="DateTimeOffset"/>
        /// if it's available.
        /// </summary>
        /// <param name="blobCache">The blob cache to extract the values from.</param>
        /// <param name="keys">The keys to get the values for.</param>
        /// <returns>A observable with the specified values.</returns>
        public static IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(this IBlobCache blobCache, IEnumerable<string> keys)
        {
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }

            if (blobCache is IBulkBlobCache bulkCache)
            {
                return bulkCache.GetCreatedAt(keys);
            }

            return keys.ToObservable()
                .SelectMany(x => blobCache.GetCreatedAt(x).Select(y => new { Key = x, Value = y }))
                .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Invalidates all the entries at the specified keys, causing them in future to have to be re-fetched.
        /// </summary>
        /// <param name="blobCache">The blob cache to invalidate values from.</param>
        /// <param name="keys">The keys to invalid.</param>
        /// <returns>A observable which signals when complete.</returns>
        public static IObservable<Unit> Invalidate(this IBlobCache blobCache, IEnumerable<string> keys)
        {
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }

            if (blobCache is IBulkBlobCache bulkCache)
            {
                return bulkCache.Invalidate(keys);
            }

            return keys.ToObservable()
               .SelectMany(blobCache.Invalidate)
               .TakeLast(1);
        }

        /// <summary>
        /// Gets a dictionary filled with the specified keys with their corresponding values.
        /// </summary>
        /// <typeparam name="T">The type of item to get.</typeparam>
        /// <param name="blobCache">The blob cache to extract the values from.</param>
        /// <param name="keys">The keys to get the values for.</param>
        /// <returns>A observable with the specified values.</returns>
        public static IObservable<IDictionary<string, T>> GetObjects<T>(this IBlobCache blobCache, IEnumerable<string> keys)
        {
            if (blobCache is IObjectBulkBlobCache bulkCache)
            {
                return bulkCache.GetObjects<T>(keys);
            }

            return keys.ToObservable()
                .SelectMany(x => blobCache.GetObject<T>(x)
                    .Where(y => y is not null)
                    .Select(y => new KeyValuePair<string, T>(x, y!))
                    .Catch<KeyValuePair<string, T>, KeyNotFoundException>(_ => Observable.Empty<KeyValuePair<string, T>>()))
                .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Inserts the specified key/value pairs into the blob.
        /// </summary>
        /// <typeparam name="T">The type of item to insert.</typeparam>
        /// <param name="blobCache">The blob cache to insert the values to.</param>
        /// <param name="keyValuePairs">The key/value to insert.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A observable which signals when complete.</returns>
        public static IObservable<Unit> InsertObjects<T>(this IBlobCache blobCache, IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }

            if (blobCache is IObjectBulkBlobCache bulkCache)
            {
                return bulkCache.InsertObjects(keyValuePairs, absoluteExpiration);
            }

            return keyValuePairs.ToObservable()
                .SelectMany(x => blobCache.InsertObject(x.Key, x.Value, absoluteExpiration))
                .TakeLast(1);
        }

        /// <summary>
        /// Invalidates all the entries at the specified keys, causing them in future to have to be re-fetched.
        /// </summary>
        /// <typeparam name="T">The type of item to invalidate.</typeparam>
        /// <param name="blobCache">The blob cache to invalidate values from.</param>
        /// <param name="keys">The keys to invalid.</param>
        /// <returns>A observable which signals when complete.</returns>
        public static IObservable<Unit> InvalidateObjects<T>(this IBlobCache blobCache, IEnumerable<string> keys)
        {
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }

            if (blobCache is IObjectBulkBlobCache bulkCache)
            {
                return bulkCache.InvalidateObjects<T>(keys);
            }

            return keys.ToObservable()
               .SelectMany(blobCache.InvalidateObject<T>)
               .TakeLast(1);
        }
    }
}
