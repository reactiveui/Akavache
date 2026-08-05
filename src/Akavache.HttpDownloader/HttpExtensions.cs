// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET462_OR_GREATER
using System.Net.Http;
#endif
using System.Runtime.CompilerServices;

using Akavache.Helpers;

namespace Akavache;

/// <summary>Provides extension methods for handling HTTP operations and stream operations.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2234:Pass System.Uri objects instead of strings",
    Justification = "The string overloads are long-standing public API. Each is paired with a Uri overload it forwards to, so the Uri form is always available.")]
public static class HttpExtensions
{
    /// <summary>Per-cache HTTP service associations. Thread-safe, does not root the cache.</summary>
    private static readonly ConditionalWeakTable<IBlobCache, IHttpService> HttpServices = new();

    /// <summary>Extension members for <c>IBlobCache</c>.</summary>
    /// <param name="blobCache">The cache to associate the service with.</param>
    extension(IBlobCache blobCache)
    {
        /// <summary>Associates an <see cref="IHttpService"/> with a cache instance so the <c>DownloadUrl</c> extensions can locate the service.</summary>
        /// <param name="httpService">The HTTP service to use for downloads.</param>
        public void SetHttpService(IHttpService httpService)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentExceptionHelper.ThrowIfNull(httpService);

            _ = HttpServices.Remove(blobCache);
            HttpServices.Add(blobCache, httpService);
        }

        /// <summary>Downloads a URL under an explicit cache key, using the default method and no extra headers.</summary>
        /// <param name="key">The cache key to store the response under.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">Whether to bypass the cache and always fetch.</param>
        /// <param name="absoluteExpiration">An optional expiration time for the cached response.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(string key, string url, bool fetchAlways, DateTimeOffset? absoluteExpiration) =>
            blobCache.DownloadUrl(key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, absoluteExpiration);

        /// <summary>Downloads a URL and caches the result, using the default method and no extra headers.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">Whether to bypass the cache and always fetch.</param>
        /// <param name="absoluteExpiration">An optional expiration time for the cached response.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(string url, bool fetchAlways, DateTimeOffset? absoluteExpiration) =>
            blobCache.DownloadUrl(url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, absoluteExpiration);

        /// <summary>Downloads a URL under an explicit cache key, using the default method and no extra headers.</summary>
        /// <param name="key">The cache key to store the response under.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">Whether to bypass the cache and always fetch.</param>
        /// <param name="absoluteExpiration">An optional expiration time for the cached response.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(string key, Uri url, bool fetchAlways, DateTimeOffset? absoluteExpiration) =>
            blobCache.DownloadUrl(key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, absoluteExpiration);

        /// <summary>Downloads a URL and caches the result, using the default method and no extra headers.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">Whether to bypass the cache and always fetch.</param>
        /// <param name="absoluteExpiration">An optional expiration time for the cached response.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url, bool fetchAlways, DateTimeOffset? absoluteExpiration) =>
            blobCache.DownloadUrl(url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, absoluteExpiration);

        /// <summary>Downloads a URL under an explicit cache key and gives the cached response an expiry.</summary>
        /// <param name="key">The cache key to store the response under.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="absoluteExpiration">An optional expiration time for the cached response.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(string key, string url, DateTimeOffset? absoluteExpiration) =>
            blobCache.DownloadUrl(key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, absoluteExpiration);

        /// <summary>Downloads a URL and gives the cached response an expiry.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="absoluteExpiration">An optional expiration time for the cached response.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(string url, DateTimeOffset? absoluteExpiration) =>
            blobCache.DownloadUrl(url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, absoluteExpiration);

        /// <summary>Downloads a URL under an explicit cache key and gives the cached response an expiry.</summary>
        /// <param name="key">The cache key to store the response under.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="absoluteExpiration">An optional expiration time for the cached response.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(string key, Uri url, DateTimeOffset? absoluteExpiration) =>
            blobCache.DownloadUrl(key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, absoluteExpiration);

        /// <summary>Downloads a URL and gives the cached response an expiry.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="absoluteExpiration">An optional expiration time for the cached response.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url, DateTimeOffset? absoluteExpiration) =>
            blobCache.DownloadUrl(url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, absoluteExpiration);

        /// <summary>
        /// Downloads data from an HTTP URL and inserts the result into the cache.
        /// If the data is already in the cache, this returns a cached value.
        /// The URL itself is used as the cache key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <returns>An observable that emits the data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string url) =>
            blobCache.DownloadUrl(url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <summary>
        /// Downloads data from an HTTP URL and inserts the result into the cache.
        /// If the data is already in the cache, this returns a cached value.
        /// The URL itself is used as the cache key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <returns>An observable that emits the data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string url, HttpMethod? method) =>
            blobCache.DownloadUrl(url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <summary>
        /// Downloads data from an HTTP URL and inserts the result into the cache.
        /// If the data is already in the cache, this returns a cached value.
        /// The URL itself is used as the cache key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="headers">An optional collection containing HTTP request headers.</param>
        /// <returns>An observable that emits the data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
            blobCache.DownloadUrl(url, method, headers, false, (DateTimeOffset?)null);

        /// <summary>
        /// Downloads data from an HTTP URL and inserts the result into the cache.
        /// If the data is already in the cache, this returns a cached value.
        /// The URL itself is used as the cache key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="headers">An optional collection containing HTTP request headers.</param>
        /// <param name="fetchAlways">A value indicating whether to force a web request to always be issued, skipping the cache.</param>
        /// <returns>An observable that emits the data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
            blobCache.DownloadUrl(url, method, headers, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// Downloads data from an HTTP URL and inserts the result into the cache.
        /// If the data is already in the cache, this returns a cached value.
        /// The URL itself is used as the cache key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="headers">An optional collection containing HTTP request headers.</param>
        /// <param name="fetchAlways">A value indicating whether to force a web request to always be issued, skipping the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date for the cached data.</param>
        /// <returns>An observable that emits the data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentValidation.ThrowIfNullOrWhiteSpace(url);

            return GetHttpService(blobCache).DownloadUrl(blobCache, new Uri(url), method, headers, fetchAlways, absoluteExpiration);
        }

        /// <summary>Downloads a URL, optionally bypassing the cache, using the default method and no extra headers.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">Whether to bypass the cache and always fetch.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(string url, bool fetchAlways) =>
            blobCache.DownloadUrl(url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. The URL itself is used as the key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url) =>
            blobCache.DownloadUrl(url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. The URL itself is used as the key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url, HttpMethod? method) =>
            blobCache.DownloadUrl(url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. The URL itself is used as the key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP request headers.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
            blobCache.DownloadUrl(url, method, headers, false, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. The URL itself is used as the key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
            blobCache.DownloadUrl(url, method, headers, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. The URL itself is used as the key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentExceptionHelper.ThrowIfNull(url);

            return GetHttpService(blobCache).DownloadUrl(blobCache, url, method, headers, fetchAlways, absoluteExpiration);
        }

        /// <summary>Downloads a URL, optionally bypassing the cache, using the default method and no extra headers.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">Whether to bypass the cache and always fetch.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url, bool fetchAlways) =>
            blobCache.DownloadUrl(url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string key, string url) =>
            blobCache.DownloadUrl(key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string key, string url, HttpMethod? method) =>
            blobCache.DownloadUrl(key, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP request headers.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string key, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
            blobCache.DownloadUrl(key, url, method, headers, false, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string key, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
            blobCache.DownloadUrl(key, url, method, headers, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string key, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentValidation.ThrowIfNullOrWhiteSpace(key);
            ArgumentValidation.ThrowIfNullOrWhiteSpace(url);

            return GetHttpService(blobCache).DownloadUrl(blobCache, key, new Uri(url), method, headers, fetchAlways, absoluteExpiration);
        }

        /// <summary>Downloads a URL under an explicit cache key, optionally bypassing the cache.</summary>
        /// <param name="key">The cache key to store the response under.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">Whether to bypass the cache and always fetch.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(string key, string url, bool fetchAlways) =>
            blobCache.DownloadUrl(key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string key, Uri url) =>
            blobCache.DownloadUrl(key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string key, Uri url, HttpMethod? method) =>
            blobCache.DownloadUrl(key, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP request headers.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string key, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
            blobCache.DownloadUrl(key, url, method, headers, false, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string key, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
            blobCache.DownloadUrl(key, url, method, headers, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="method">The method.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(string key, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentValidation.ThrowIfNullOrWhiteSpace(key);
            ArgumentExceptionHelper.ThrowIfNull(url);

            return GetHttpService(blobCache).DownloadUrl(blobCache, key, url, method, headers, fetchAlways, absoluteExpiration);
        }

        /// <summary>Downloads a URL under an explicit cache key, optionally bypassing the cache.</summary>
        /// <param name="key">The cache key to store the response under.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">Whether to bypass the cache and always fetch.</param>
        /// <returns>An observable that emits the downloaded bytes.</returns>
        public IObservable<byte[]> DownloadUrl(string key, Uri url, bool fetchAlways) =>
            blobCache.DownloadUrl(key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);
    }

    /// <summary>Extension members for <c>Stream</c>.</summary>
    /// <param name="blobCache">The stream to write to.</param>
    extension(Stream blobCache)
    {
        /// <summary>Writes data to a stream asynchronously and returns an observable.</summary>
        /// <param name="data">The data to write to the stream.</param>
        /// <param name="start">The starting index in the data array.</param>
        /// <param name="length">The number of bytes to write.</param>
        /// <returns>An observable that signals when the write operation has completed.</returns>
        public IObservable<Unit> WriteAsyncRx(byte[] data, int start, int length)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            AsyncSubject<Unit> ret = new();

            try
            {
                _ = blobCache.BeginWrite(
                    data,
                    start,
                    length,
                    result =>
                    {
                        try
                        {
                            blobCache.EndWrite(result);
                            ret.OnNext(Unit.Default);
                            ret.OnCompleted();
                        }
                        catch (Exception ex)
                        {
                            ret.OnError(ex);
                        }
                    },
                    null);
            }
            catch (Exception ex)
            {
                ret.OnError(ex);
            }

            return ret;
        }
    }

    /// <summary>Gets the HTTP service associated with a cache, creating a default one if none was set.</summary>
    /// <param name="blobCache">The cache to look up the service for.</param>
    /// <returns>The associated HTTP service.</returns>
    private static IHttpService GetHttpService(IBlobCache blobCache) =>
        HttpServices.GetValue(blobCache, static _ => new HttpService());
}
