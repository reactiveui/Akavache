// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
#if NET462_OR_GREATER
using System.Net.Http;
#endif
using Akavache.Helpers;

namespace Akavache;

/// <summary>
/// Provides a default implementation of HTTP service functionality for Akavache.
/// </summary>
public class HttpService : IHttpService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpService"/> class.
    /// </summary>
    public HttpService()
    {
        HttpClientHandler handler = new()
        {
            CheckCertificateRevocationList = true,
        };
        if (handler.SupportsAutomaticDecompression)
        {
            handler.AutomaticDecompression = DecompressionMethods.GZip |
                                             DecompressionMethods.Deflate;
        }

        HttpClient = new(handler);
    }

    /// <summary>
    /// Gets or sets the HTTP client used for making web requests.
    /// </summary>
    public HttpClient HttpClient { get; set; }

    /// <inheritdoc />
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) =>
        blobCache.DownloadUrl(url, url, method, headers, fetchAlways, absoluteExpiration);

    /// <inheritdoc />
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(url);
        return blobCache.DownloadUrl(url.ToString(), url, method, headers, fetchAlways, absoluteExpiration);
    }

    /// <inheritdoc />
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        method ??= HttpMethod.Get;

        var doFetch = MakeWebRequest(new(url), method, headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
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

    /// <inheritdoc />
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        method ??= HttpMethod.Get;

        var doFetch = MakeWebRequest(url, method, headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
        var fetchAndCache = doFetch.SelectMany(x => blobCache.Insert(key, x, absoluteExpiration).Select(_ => x));

        IObservable<byte[]> ret;
        if (!fetchAlways)
        {
            ret = blobCache.Get(key).Catch(fetchAndCache).Select(x => x ?? []);
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
    /// Builds an <see cref="HttpRequestMessage"/> for the specified URI, method, and headers.
    /// </summary>
    /// <param name="uri">The target URI.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="headers">Optional request headers.</param>
    /// <returns>A configured request message.</returns>
    internal static HttpRequestMessage CreateWebRequest(Uri uri, HttpMethod method, IEnumerable<KeyValuePair<string, string>>? headers)
    {
        HttpRequestMessage request = new(method, uri);

        if (headers is not null)
        {
            foreach (var x in headers)
            {
                request.Headers.TryAddWithoutValidation(x.Key, x.Value);
            }
        }

        return request;
    }

    /// <summary>
    /// Reads the response body as a byte array, throwing if the response status indicates failure.
    /// </summary>
    /// <param name="responseMessage">The HTTP response to process.</param>
    /// <param name="url">The original request URL, used in error messages.</param>
    /// <param name="absoluteExpiration">The requested absolute expiration, used in error messages.</param>
    /// <returns>An observable that emits the response bytes.</returns>
    [SuppressMessage("Style", "IDE0200:Remove unnecessary lambda expression", Justification = "Method-group conversion of ReadAsByteArrayAsync is ambiguous on net6+ where an overload accepting a CancellationToken exists.")]
    internal static IObservable<byte[]> ProcessWebResponse(HttpResponseMessage responseMessage, string url, DateTimeOffset? absoluteExpiration) =>
        !responseMessage.IsSuccessStatusCode
            ? Observable.Throw<byte[]>(new HttpRequestException($"[{responseMessage.StatusCode}] Http Failure to {url} with expiry {absoluteExpiration}: {responseMessage.ReasonPhrase}"))
            : Observable.FromAsync(() => responseMessage.Content.ReadAsByteArrayAsync());

    /// <summary>
    /// Reads the response body as a byte array, throwing if the response status indicates failure.
    /// </summary>
    /// <param name="responseMessage">The HTTP response to process.</param>
    /// <param name="url">The original request URI, used in error messages.</param>
    /// <param name="absoluteExpiration">The requested absolute expiration, used in error messages.</param>
    /// <returns>An observable that emits the response bytes.</returns>
    internal static IObservable<byte[]> ProcessWebResponse(HttpResponseMessage responseMessage, Uri url, DateTimeOffset? absoluteExpiration) => ProcessWebResponse(responseMessage, url.ToString(), absoluteExpiration);

    /// <summary>
    /// Makes a web request to the specified URI.
    /// </summary>
    /// <param name="uri">The URI to make the request to.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="content">Optional content to send with the request.</param>
    /// <param name="retries">The number of retry attempts for failed requests.</param>
    /// <param name="timeout">The timeout duration for the request.</param>
    /// <returns>An observable that emits the HTTP response message.</returns>
    protected internal virtual IObservable<HttpResponseMessage> MakeWebRequest(
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

    /// <summary>
    /// Provides a fast-failing HTTP service that reduces retries and timeouts to speed up tests.
    /// </summary>
    public class FastHttpService : HttpService
    {
        /// <summary>The number of retry attempts configured for outgoing requests.</summary>
        private readonly int _retries;

        /// <summary>The request timeout configured for outgoing requests.</summary>
        private readonly TimeSpan _timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastHttpService"/> class.
        /// </summary>
        /// <param name="retries">The number of retry attempts to use (default is 0).</param>
        /// <param name="timeout">The timeout duration to use (default is 2 seconds).</param>
        public FastHttpService(int retries = 0, TimeSpan? timeout = null)
        {
            _retries = retries;
            _timeout = timeout ?? TimeSpan.FromSeconds(2);

            // Also set HttpClient.Timeout so HttpClient honors the same bound.
            try
            {
                HttpClient.Timeout = _timeout;
            }
            catch
            {
                // ignore if platform HttpClient doesn't allow timeout
            }
        }

        /// <inheritdoc />
        protected internal override IObservable<HttpResponseMessage> MakeWebRequest(
            Uri uri,
            HttpMethod method,
            IEnumerable<KeyValuePair<string, string>>? headers = null,
            string? content = null,
            int retries = 3,
            TimeSpan? timeout = null) =>

            // Force the configured fast retries/timeout
            base.MakeWebRequest(uri, method, headers, content, _retries, _timeout);
    }
}
