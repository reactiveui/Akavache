// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Akavache;

/// <summary>
/// A default http service.
/// </summary>
public class HttpService : IHttpService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpService"/> class.
    /// </summary>
    public HttpService()
    {
        var handler = new HttpClientHandler();
        if (handler.SupportsAutomaticDecompression)
        {
            handler.AutomaticDecompression = DecompressionMethods.GZip |
                                             DecompressionMethods.Deflate;
        }

        HttpClient = new HttpClient(handler);
    }

    /// <summary>
    /// Gets or sets the client.
    /// </summary>
    public HttpClient HttpClient { get; set; }

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. The URL itself is used as the key.
    /// </summary>
    /// <param name="blobCache">The blob cache associated with the action.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method = default, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) =>
        blobCache.DownloadUrl(url, url, method, headers, fetchAlways, absoluteExpiration);

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. The URL itself is used as the key.
    /// </summary>
    /// <param name="blobCache">The blob cache associated with the action.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method = default, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) => url is null
            ? throw new ArgumentNullException(nameof(url))
            : blobCache.DownloadUrl(url.ToString(), url, method, headers, fetchAlways, absoluteExpiration);

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. An explicit key is provided rather than the URL itself.
    /// </summary>
    /// <param name="blobCache">The blob cache associated with the action.</param>
    /// <param name="key">The key to store with.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method = default, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        method ??= HttpMethod.Get;

        var doFetch = MakeWebRequest(new Uri(url), method, headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
        var fetchAndCache = doFetch.SelectMany(x => blobCache.Insert(key, x, absoluteExpiration).Select(_ => x));

        IObservable<byte[]?> ret;
        if (!fetchAlways)
        {
            ret = blobCache.Get(key).Catch(fetchAndCache);
        }
        else
        {
            ret = fetchAndCache;
        }

        var conn = ret.PublishLast();
        conn.Connect();
        return conn.Select(x => x ?? []);
    }

    /// <summary>
    /// Download data from an HTTP URL and insert the result into the
    /// cache. If the data is already in the cache, this returns
    /// a cached value. An explicit key is provided rather than the URL itself.
    /// </summary>
    /// <param name="blobCache">The blob cache associated with the action.</param>
    /// <param name="key">The key to store with.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="headers">An optional Dictionary containing the HTTP
    /// request headers.</param>
    /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>The data downloaded from the URL.</returns>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method = default, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        method ??= HttpMethod.Get;

        var doFetch = MakeWebRequest(url, method, headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
        var fetchAndCache = doFetch.SelectMany(x => blobCache.Insert(key, x, absoluteExpiration).Select(_ => x));

        IObservable<byte[]> ret;
        if (!fetchAlways)
        {
            ret = blobCache.Get(key).Catch(fetchAndCache).Select(x => x ?? Array.Empty<byte>());
        }
        else
        {
            ret = fetchAndCache;
        }

        var conn = ret.PublishLast();
        conn.Connect();
        return conn;
    }

    /// <summary>
    /// Makes a web request.
    /// </summary>
    /// <param name="uri">The URI.</param>
    /// <param name="method">The type of method.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="content">The contents.</param>
    /// <param name="retries">The number of retries.</param>
    /// <param name="timeout">A timeout time span.</param>
    /// <returns>The web response.</returns>
    protected virtual IObservable<HttpResponseMessage> MakeWebRequest(
        Uri uri,
        HttpMethod method,
        IEnumerable<KeyValuePair<string, string>>? headers = null,
        string? content = null,
        int retries = 3,
        TimeSpan? timeout = null)
    {
        var request = Observable.Defer(() =>
        {
            var httpRequest = CreateWebRequest(uri, method, headers);

            if (content is null)
            {
                return Observable.FromAsync(() => HttpClient.SendAsync(httpRequest));
            }

            httpRequest.Content = new StringContent(content);

            return Observable.FromAsync(() => HttpClient.SendAsync(httpRequest));
        });

        return request.Timeout(timeout ?? TimeSpan.FromSeconds(15), CacheDatabase.TaskpoolScheduler).Retry(retries);
    }

    private static HttpRequestMessage CreateWebRequest(Uri uri, HttpMethod method, IEnumerable<KeyValuePair<string, string>>? headers)
    {
        var request = new HttpRequestMessage(method, uri);

        if (headers is not null)
        {
            foreach (var x in headers)
            {
                request.Headers.TryAddWithoutValidation(x.Key, x.Value);
            }
        }

        return request;
    }

    private static IObservable<byte[]> ProcessWebResponse(HttpResponseMessage responseMessage, string url, DateTimeOffset? absoluteExpiration)
    {
        if (!responseMessage.IsSuccessStatusCode)
        {
            return Observable.Throw<byte[]>(new HttpRequestException($"[{responseMessage.StatusCode.ToString()}] Http Failure to {url} with expiry {absoluteExpiration.ToString()}: {responseMessage.ReasonPhrase}"));
        }

        return Observable.FromAsync(() => responseMessage.Content.ReadAsByteArrayAsync());
    }

    private static IObservable<byte[]> ProcessWebResponse(HttpResponseMessage responseMessage, Uri url, DateTimeOffset? absoluteExpiration) => ProcessWebResponse(responseMessage, url.ToString(), absoluteExpiration);
}
