// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

namespace Akavache;

/// <summary>
/// A set of methods associated with accessing HTTP resources for a blob cache.
/// </summary>
public class AkavacheHttpMixin : IAkavacheHttpMixin
{
    private static IAkavacheHttpClientFactory HttpFactoryClient => Locator.Current.GetService<IAkavacheHttpClientFactory>() ?? throw new InvalidOperationException("Unable to resolve IAkavacheHttpClientFactory, probably Akavache is not initialized.");

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.DownloadUrl(url, url, headers, fetchAlways, absoluteExpiration);
    }

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (url is null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        return blobCache.DownloadUrl(url.ToString(), url, headers, fetchAlways, absoluteExpiration);
    }

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        var doFetch = MakeWebRequest(HttpMethod.Get, new Uri(url), headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
        var fetchAndCache = doFetch.SelectMany(x => blobCache.Insert(key, x, absoluteExpiration).Select(_ => x));

        IObservable<byte[]> ret;
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
        return conn;
    }

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        var doFetch = MakeWebRequest(HttpMethod.Get, url, headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
        var fetchAndCache = doFetch.SelectMany(x => blobCache.Insert(key, x, absoluteExpiration).Select(_ => x));

        IObservable<byte[]> ret;
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
        return conn;
    }

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, HttpMethod method, string url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.DownloadUrl(method, url, url, headers, fetchAlways, absoluteExpiration);
    }

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, HttpMethod method, Uri url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (url is null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        return blobCache.DownloadUrl(method, url.ToString(), url, headers, fetchAlways, absoluteExpiration);
    }

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, HttpMethod method, string key, string url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        var doFetch = MakeWebRequest(method, new Uri(url), headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
        var fetchAndCache = doFetch.SelectMany(x => blobCache.Insert(key, x, absoluteExpiration).Select(_ => x));

        IObservable<byte[]> ret;
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
        return conn;
    }

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, HttpMethod method, string key, Uri url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        var doFetch = MakeWebRequest(method, url, headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
        var fetchAndCache = doFetch.SelectMany(x => blobCache.Insert(key, x, absoluteExpiration).Select(_ => x));

        IObservable<byte[]> ret;
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
        return conn;
    }

    private static IObservable<HttpResponseMessage> MakeWebRequest(
        HttpMethod method,
        Uri uri,
        IDictionary<string, string>? headers = null,
        int retries = 3,
        TimeSpan? timeout = null)
    {
        var request = Observable.Defer(() =>
        {
            var client = HttpFactoryClient.CreateClient(nameof(AkavacheHttpMixin));

            var request = CreateWebRequest(method, uri, headers);

            return Observable.FromAsync(() => client.SendAsync(request), BlobCache.TaskpoolScheduler);
        });

        return request.Timeout(timeout ?? TimeSpan.FromSeconds(15), BlobCache.TaskpoolScheduler).Retry(retries);
    }

    private static HttpRequestMessage CreateWebRequest(HttpMethod method, Uri uri, IDictionary<string, string>? headers)
    {
        var requestMessage = new HttpRequestMessage(method, uri);
        if (headers is not null)
        {
            foreach (var x in headers)
            {
                requestMessage.Headers.TryAddWithoutValidation(x.Key, x.Value);
            }
        }

        return requestMessage;
    }

    private static IObservable<byte[]> ProcessWebResponse(HttpResponseMessage response, string url, DateTimeOffset? absoluteExpiration)
    {
        if (!response.IsSuccessStatusCode)
        {
            return Observable.Throw<byte[]>(new HttpRequestException("Invalid response: " + url + " reason " + response.ReasonPhrase + " with expiry: " + absoluteExpiration));
        }

        return Observable.FromAsync(() => response.Content.ReadAsByteArrayAsync());
    }

    private static IObservable<byte[]> ProcessWebResponse(HttpResponseMessage response, Uri url, DateTimeOffset? absoluteExpiration) => ProcessWebResponse(response, url.ToString(), absoluteExpiration);
}
