// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET462_OR_GREATER
using System.Net.Http;
#endif

namespace Akavache;

/// <summary>
/// Represents a service that provides HTTP functionality for downloading and caching web resources.
/// </summary>
public interface IHttpService
{
    /// <summary>
    /// Downloads data from a URL and caches it, using the URL as the cache key.
    /// </summary>
    /// <param name="blobCache">The blob cache to store the downloaded data.</param>
    /// <param name="url">The URL to download from.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="fetchAlways">A value indicating whether to always fetch from the web, bypassing the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date for the cached data.</param>
    /// <returns>An observable that emits the downloaded byte data.</returns>
    IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Downloads data from a URL and caches it, using the URL as the cache key.
    /// </summary>
    /// <param name="blobCache">The blob cache to store the downloaded data.</param>
    /// <param name="url">The URL to download from.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="fetchAlways">A value indicating whether to always fetch from the web, bypassing the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date for the cached data.</param>
    /// <returns>An observable that emits the downloaded byte data.</returns>
    IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Downloads data from a URL and caches it using a custom cache key.
    /// </summary>
    /// <param name="blobCache">The blob cache to store the downloaded data.</param>
    /// <param name="key">The custom key to use for the cache entry.</param>
    /// <param name="url">The URL to download from.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="fetchAlways">A value indicating whether to always fetch from the web, bypassing the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date for the cached data.</param>
    /// <returns>An observable that emits the downloaded byte data.</returns>
    IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Downloads data from a URL and caches it using a custom cache key.
    /// </summary>
    /// <param name="blobCache">The blob cache to store the downloaded data.</param>
    /// <param name="key">The custom key to use for the cache entry.</param>
    /// <param name="url">The URL to download from.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="fetchAlways">A value indicating whether to always fetch from the web, bypassing the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date for the cached data.</param>
    /// <returns>An observable that emits the downloaded byte data.</returns>
    IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null);
}
