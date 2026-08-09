// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>Provides extension methods for setting cache expiration times based on relative time intervals from the current time.</summary>
public static class RelativeTimeExtensions
{
    /// <summary>Extension members for <c>IBlobCache</c>.</summary>
    /// <param name="blobCache">The blob cache to insert the item into.</param>
    extension(IBlobCache blobCache)
    {
        /// <summary>Inserts an item into the cache with expiration based on a relative time span.</summary>
        /// <param name="key">The key to associate with the cache entry.</param>
        /// <param name="data">The data to store in the cache entry.</param>
        /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
        /// <returns>An observable that signals when the item is added to the cache.</returns>
        public IObservable<RxVoid> Insert(string key, byte[] data, TimeSpan expiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.Insert(key, data, blobCache.Scheduler.Now + expiration);
        }

        /// <summary>Inserts an object into the cache with expiration based on a relative time span.</summary>
        /// <param name="key">The key to associate with the cache entry.</param>
        /// <param name="value">The object to serialize and store in the cache.</param>
        /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
        /// <typeparam name="T">The type of object to insert.</typeparam>
        /// <returns>An observable that signals when the item is added to the cache.</returns>
        [RequiresUnreferencedCode("Using InsertObject requires types to be preserved for serialization")]
        [RequiresDynamicCode("Using InsertObject requires types to be preserved for serialization")]
        public IObservable<RxVoid> InsertObject<T>(string key, T value, TimeSpan expiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.InsertObject(key, value, blobCache.Scheduler.Now + expiration);
        }

        /// <summary>
        /// Updates the expiration date for an existing cache entry without reading or writing the cached data.
        /// This is useful when a server returns a NotModified response and you want to extend the cache expiration.
        /// </summary>
        /// <param name="key">The key of the cache entry to update.</param>
        /// <param name="expiration">A timespan that will be added to the current DateTime.</param>
        /// <returns>A signal indicating when the operation is complete.</returns>
        public IObservable<RxVoid> UpdateExpiration(string key, TimeSpan expiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.UpdateExpiration(key, blobCache.Scheduler.Now + expiration);
        }

        /// <summary>
        /// Updates the expiration date for an existing cache entry without reading or writing the cached data.
        /// This is useful when a server returns a NotModified response and you want to extend the cache expiration.
        /// </summary>
        /// <param name="key">The key of the cache entry to update.</param>
        /// <param name="type">The type of the cached object.</param>
        /// <param name="expiration">A timespan that will be added to the current DateTime.</param>
        /// <returns>A signal indicating when the operation is complete.</returns>
        public IObservable<RxVoid> UpdateExpiration(string key, Type type, TimeSpan expiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.UpdateExpiration(key, type, blobCache.Scheduler.Now + expiration);
        }

        /// <summary>
        /// Updates the expiration date for multiple existing cache entries without reading or writing the cached data.
        /// This is useful when a server returns a NotModified response and you want to extend the cache expiration.
        /// </summary>
        /// <param name="keys">The keys of the cache entries to update.</param>
        /// <param name="expiration">A timespan that will be added to the current DateTime.</param>
        /// <returns>A signal indicating when the operation is complete.</returns>
        public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, TimeSpan expiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.UpdateExpiration(keys, blobCache.Scheduler.Now + expiration);
        }

        /// <summary>
        /// Updates the expiration date for multiple existing cache entries without reading or writing the cached data.
        /// This is useful when a server returns a NotModified response and you want to extend the cache expiration.
        /// </summary>
        /// <param name="keys">The keys of the cache entries to update.</param>
        /// <param name="type">The type of the cached objects.</param>
        /// <param name="expiration">A timespan that will be added to the current DateTime.</param>
        /// <returns>A signal indicating when the operation is complete.</returns>
        public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, Type type, TimeSpan expiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.UpdateExpiration(keys, type, blobCache.Scheduler.Now + expiration);
        }
    }

    /// <summary>Extension members for <c>ISecureBlobCache</c>.</summary>
    /// <param name="blobCache">The blob cache to insert the item into.</param>
    extension(ISecureBlobCache blobCache)
    {
        /// <summary>Saves a username and password.</summary>
        /// <param name="user">The username to store.</param>
        /// <param name="password">The password to store.</param>
        /// <param name="host">The host to store against.</param>
        /// <param name="expiration">A timespan that will be added to the current DateTime.</param>
        /// <returns>A observable which will signal when the item is added.</returns>
        [RequiresUnreferencedCode("Using SaveLogin requires types to be preserved for serialization")]
        [RequiresDynamicCode("Using SaveLogin requires types to be preserved for serialization")]
        public IObservable<RxVoid> SaveLogin(string user, string password, string host, TimeSpan expiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.SaveLogin(user, password, host, blobCache.Scheduler.Now + expiration);
        }
    }
}
