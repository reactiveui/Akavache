// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

namespace Akavache;

/// <summary>
/// Set of extension methods that provide Http functionality to the <see cref="IBlobCache"/> interface.
/// </summary>
public static class HttpMixinExtensions
{
    private static IAkavacheHttpMixin HttpMixin => Locator.Current.GetService<IAkavacheHttpMixin>() ?? throw new InvalidOperationException("Unable to resolve IAkavacheHttpMixin, probably Akavache is not initialized.");

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. The URL itself is used as the key.
    /// </summary>
    /// <param name="blobCache">The blob cache to perform the operation on.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, string url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) =>
        HttpMixin.DownloadUrl(blobCache, new Uri(url), headers, fetchAlways, absoluteExpiration);

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. The URL itself is used as the key.
    /// </summary>
    /// <param name="blobCache">The blob cache to perform the operation on.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, Uri url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) =>
        HttpMixin.DownloadUrl(blobCache, url, headers, fetchAlways, absoluteExpiration);

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. An explicit key is provided rather than the URL itself.
    /// </summary>
    /// <param name="blobCache">The blob cache to perform the operation on.</param>
    /// <param name="key">The key to store with.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, string key, string url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) =>
        HttpMixin.DownloadUrl(blobCache, key, new Uri(url), headers, fetchAlways, absoluteExpiration);

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. An explicit key is provided rather than the URL itself.
    /// </summary>
    /// <param name="blobCache">The blob cache to perform the operation on.</param>
    /// <param name="key">The key to store with.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, string key, Uri url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) =>
        HttpMixin.DownloadUrl(blobCache, key, url, headers, fetchAlways, absoluteExpiration);

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. The URL itself is used as the key.
    /// </summary>
    /// <param name="blobCache">The blob cache to perform the operation on.</param>
    /// <param name="method">The type of HTTP Method.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, HttpMethod method, string url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) =>
        HttpMixin.DownloadUrl(blobCache, method, new Uri(url), headers, fetchAlways, absoluteExpiration);

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. The URL itself is used as the key.
    /// </summary>
    /// <param name="blobCache">The blob cache to perform the operation on.</param>
    /// <param name="method">The type of HTTP Method.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, HttpMethod method, Uri url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) =>
        HttpMixin.DownloadUrl(blobCache, method, url, headers, fetchAlways, absoluteExpiration);

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. An explicit key is provided rather than the URL itself.
    /// </summary>
    /// <param name="blobCache">The blob cache to perform the operation on.</param>
    /// <param name="method">The type of HTTP Method.</param>
    /// <param name="key">The key to store with.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, HttpMethod method, string key, string url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) =>
        HttpMixin.DownloadUrl(blobCache, method, key, new Uri(url), headers, fetchAlways, absoluteExpiration);

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. An explicit key is provided rather than the URL itself.
    /// </summary>
    /// <param name="blobCache">The blob cache to perform the operation on.</param>
    /// <param name="method">The type of HTTP Method.</param>
    /// <param name="key">The key to store with.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, HttpMethod method, string key, Uri url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) =>
        HttpMixin.DownloadUrl(blobCache, method, key, url, headers, fetchAlways, absoluteExpiration);
}
