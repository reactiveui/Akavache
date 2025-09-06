// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

#if NET462_OR_GREATER
using System.Net.Http;
#endif

namespace Akavache;

/// <summary>
/// Provides extension methods for setting cache expiration times based on relative time intervals from the current time.
/// </summary>
public static class RelativeTimeExtensions
{
    /// <summary>
    /// Inserts an item into the cache with expiration based on a relative time span.
    /// </summary>
    /// <param name="blobCache">The blob cache to insert the item into.</param>
    /// <param name="key">The key to associate with the cache entry.</param>
    /// <param name="data">The data to store in the cache entry.</param>
    /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
    /// <returns>An observable that signals when the item is added to the cache.</returns>
    public static IObservable<Unit> Insert(this IBlobCache blobCache, string key, byte[] data, TimeSpan expiration) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.Insert(key, data, blobCache.Scheduler.Now + expiration);

    /// <summary>
    /// Inserts an object into the cache with expiration based on a relative time span.
    /// </summary>
    /// <param name="blobCache">The blob cache to insert the item into.</param>
    /// <param name="key">The key to associate with the cache entry.</param>
    /// <param name="value">The object to serialize and store in the cache.</param>
    /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
    /// <typeparam name="T">The type of object to insert.</typeparam>
    /// <returns>An observable that signals when the item is added to the cache.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using InsertObject requires types to be preserved for serialization")]
    [RequiresDynamicCode("Using InsertObject requires types to be preserved for serialization")]
#endif
    public static IObservable<Unit> InsertObject<T>(this IBlobCache blobCache, string key, T value, TimeSpan expiration) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.InsertObject(key, value, blobCache.Scheduler.Now + expiration);

    /// <summary>
    /// Downloads data from the specified URL if it is not already in the cache, with expiration based on a relative time span.
    /// </summary>
    /// <param name="blobCache">The blob cache to store the downloaded data.</param>
    /// <param name="url">The URL to download if not already in the cache.</param>
    /// <param name="httpMethod">The HTTP method to use for the request.</param>
    /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="fetchAlways">A value indicating whether to always fetch from the web, bypassing the cache.</param>
    /// <returns>An observable that emits the downloaded data when available.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, string url, HttpMethod httpMethod, TimeSpan expiration, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.DownloadUrl(url, httpMethod, headers, fetchAlways, blobCache.Scheduler.Now + expiration);

    /// <summary>
    /// Downloads data from the specified URL if it is not already in the cache, with expiration based on a relative time span.
    /// </summary>
    /// <param name="blobCache">The blob cache to store the downloaded data.</param>
    /// <param name="url">The URL to download if not already in the cache.</param>
    /// <param name="httpMethod">The HTTP method to use for the request.</param>
    /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="fetchAlways">A value indicating whether to always fetch from the web, bypassing the cache.</param>
    /// <returns>An observable that emits the downloaded data when available.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, Uri url, HttpMethod httpMethod, TimeSpan expiration, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.DownloadUrl(url, httpMethod, headers, fetchAlways, blobCache.Scheduler.Now + expiration);

    /// <summary>
    /// Saves a username and password.
    /// </summary>
    /// <param name="blobCache">The blob cache to insert the item into.</param>
    /// <param name="user">The username to store.</param>
    /// <param name="password">The password to store.</param>
    /// <param name="host">The host to store against.</param>
    /// <param name="expiration">A timespan that will be added to the current DateTime.</param>
    /// <returns>A observable which will signal when the item is added.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using SaveLogin requires types to be preserved for serialization")]
    [RequiresDynamicCode("Using SaveLogin requires types to be preserved for serialization")]
#endif
    public static IObservable<Unit> SaveLogin(this ISecureBlobCache blobCache, string user, string password, string host, TimeSpan expiration) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.SaveLogin(user, password, host, blobCache.Scheduler.Now + expiration);

    /// <summary>
    /// Updates the expiration date for an existing cache entry without reading or writing the cached data.
    /// This is useful when a server returns a NotModified response and you want to extend the cache expiration.
    /// </summary>
    /// <param name="blobCache">The blob cache containing the item.</param>
    /// <param name="key">The key of the cache entry to update.</param>
    /// <param name="expiration">A timespan that will be added to the current DateTime.</param>
    /// <returns>A signal indicating when the operation is complete.</returns>
    public static IObservable<Unit> UpdateExpiration(this IBlobCache blobCache, string key, TimeSpan expiration) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.UpdateExpiration(key, blobCache.Scheduler.Now + expiration);

    /// <summary>
    /// Updates the expiration date for an existing cache entry without reading or writing the cached data.
    /// This is useful when a server returns a NotModified response and you want to extend the cache expiration.
    /// </summary>
    /// <param name="blobCache">The blob cache containing the item.</param>
    /// <param name="key">The key of the cache entry to update.</param>
    /// <param name="type">The type of the cached object.</param>
    /// <param name="expiration">A timespan that will be added to the current DateTime.</param>
    /// <returns>A signal indicating when the operation is complete.</returns>
    public static IObservable<Unit> UpdateExpiration(this IBlobCache blobCache, string key, Type type, TimeSpan expiration) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.UpdateExpiration(key, type, blobCache.Scheduler.Now + expiration);

    /// <summary>
    /// Updates the expiration date for multiple existing cache entries without reading or writing the cached data.
    /// This is useful when a server returns a NotModified response and you want to extend the cache expiration.
    /// </summary>
    /// <param name="blobCache">The blob cache containing the items.</param>
    /// <param name="keys">The keys of the cache entries to update.</param>
    /// <param name="expiration">A timespan that will be added to the current DateTime.</param>
    /// <returns>A signal indicating when the operation is complete.</returns>
    public static IObservable<Unit> UpdateExpiration(this IBlobCache blobCache, IEnumerable<string> keys, TimeSpan expiration) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.UpdateExpiration(keys, blobCache.Scheduler.Now + expiration);

    /// <summary>
    /// Updates the expiration date for multiple existing cache entries without reading or writing the cached data.
    /// This is useful when a server returns a NotModified response and you want to extend the cache expiration.
    /// </summary>
    /// <param name="blobCache">The blob cache containing the items.</param>
    /// <param name="keys">The keys of the cache entries to update.</param>
    /// <param name="type">The type of the cached objects.</param>
    /// <param name="expiration">A timespan that will be added to the current DateTime.</param>
    /// <returns>A signal indicating when the operation is complete.</returns>
    public static IObservable<Unit> UpdateExpiration(this IBlobCache blobCache, IEnumerable<string> keys, Type type, TimeSpan expiration) =>
        blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.UpdateExpiration(keys, type, blobCache.Scheduler.Now + expiration);
}
