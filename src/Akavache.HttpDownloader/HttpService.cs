// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
#if NET462_OR_GREATER
using System.Net.Http;
#endif

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>Provides a default implementation of HTTP service functionality for Akavache.</summary>
[System.Diagnostics.DebuggerDisplay("{HttpClient}")]
[SuppressMessage(
    "Usage",
    "CA2234:Pass System.Uri objects instead of strings",
    Justification = "The string overloads are long-standing public API. Each is paired with a Uri overload it forwards to, so the Uri form is always available.")]
public class HttpService : IHttpService, IDisposable
{
    /// <summary>Retry count applied when a caller does not state one.</summary>
    internal const int DefaultRetryCount = 3;

    /// <summary>Request timeout applied when a caller does not state one.</summary>
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    /// <summary>1 if disposed, 0 otherwise.</summary>
    private int _disposed;

    /// <summary>Initializes a new instance of the <see cref="HttpService"/> class.</summary>
    public HttpService()
    {
        HttpClientHandler handler = new() { CheckCertificateRevocationList = true, };
        if (handler.SupportsAutomaticDecompression)
        {
            handler.AutomaticDecompression = DecompressionMethods.GZip
                                             | DecompressionMethods.Deflate;
        }

        HttpClient = new(handler);
    }

    /// <summary>Gets or sets the HTTP client used for making web requests.</summary>
    public HttpClient HttpClient { get; set; }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url) =>
        DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method) =>
        DownloadUrl(blobCache, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
        DownloadUrl(blobCache, url, method, headers, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
        DownloadUrl(blobCache, url, method, headers, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(
        IBlobCache blobCache,
        string url,
        HttpMethod? method,
        IEnumerable<KeyValuePair<string, string>>? headers,
        bool fetchAlways,
        DateTimeOffset? absoluteExpiration) =>
        blobCache.DownloadUrl(url, url, method, headers, fetchAlways, absoluteExpiration);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, bool fetchAlways) =>
        DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url) =>
        DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method) =>
        DownloadUrl(blobCache, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
        DownloadUrl(blobCache, url, method, headers, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
        DownloadUrl(blobCache, url, method, headers, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc />
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways, DateTimeOffset? absoluteExpiration)
    {
        ArgumentExceptionHelper.ThrowIfNull(url);
        return blobCache.DownloadUrl(url.ToString(), url, method, headers, fetchAlways, absoluteExpiration);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, bool fetchAlways) =>
        DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url) =>
        DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method) =>
        DownloadUrl(blobCache, key, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
        DownloadUrl(blobCache, key, url, method, headers, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
        DownloadUrl(blobCache, key, url, method, headers, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc />
    public IObservable<byte[]> DownloadUrl(
        IBlobCache blobCache,
        string key,
        string url,
        HttpMethod? method,
        IEnumerable<KeyValuePair<string, string>>? headers,
        bool fetchAlways,
        DateTimeOffset? absoluteExpiration)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        method ??= HttpMethod.Get;

        var doFetch = MakeWebRequest(new(url), method, headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
        var fetchAndCache = doFetch.SelectMany(x => new SelectConstantObservable<RxVoid, byte[]>(blobCache.Insert(key, x, absoluteExpiration), x));

        var ret = !fetchAlways ? blobCache.Get(key).Catch<byte[]?, Exception>(_ => fetchAndCache) : fetchAndCache;

        var conn = ret.Multicast(new AsyncSignal<byte[]?>());
        _ = conn.Connect();
        return conn.Select(static x => x ?? []);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, bool fetchAlways) =>
        DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url) =>
        DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method) =>
        DownloadUrl(blobCache, key, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
        DownloadUrl(blobCache, key, url, method, headers, false, (DateTimeOffset?)null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
        DownloadUrl(blobCache, key, url, method, headers, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc />
    public IObservable<byte[]> DownloadUrl(
        IBlobCache blobCache,
        string key,
        Uri url,
        HttpMethod? method,
        IEnumerable<KeyValuePair<string, string>>? headers,
        bool fetchAlways,
        DateTimeOffset? absoluteExpiration)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        method ??= HttpMethod.Get;

        var doFetch = MakeWebRequest(url, method, headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
        var fetchAndCache = doFetch.SelectMany(x => new SelectConstantObservable<RxVoid, byte[]>(blobCache.Insert(key, x, absoluteExpiration), x));

        var ret = !fetchAlways ? blobCache.Get(key).Catch<byte[]?, Exception>(_ => fetchAndCache).Select(static x => x ?? []) : fetchAndCache;

        var conn = ret.Multicast(new AsyncSignal<byte[]>());
        _ = conn.Connect();
        return conn;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, bool fetchAlways) =>
        DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Builds an <see cref="HttpRequestMessage"/> for the specified URI, method, and headers.</summary>
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
                _ = request.Headers.TryAddWithoutValidation(x.Key, x.Value);
            }
        }

        return request;
    }

    /// <summary>Reads the response body as a byte array, throwing if the response status indicates failure.</summary>
    /// <param name="responseMessage">The HTTP response to process.</param>
    /// <param name="url">The original request URL, used in error messages.</param>
    /// <param name="absoluteExpiration">The requested absolute expiration, used in error messages.</param>
    /// <returns>An observable that emits the response bytes.</returns>
    internal static IObservable<byte[]> ProcessWebResponse(HttpResponseMessage responseMessage, string url, DateTimeOffset? absoluteExpiration) =>
        !responseMessage.IsSuccessStatusCode
            ? new ImmediateThrowSignal<byte[]>(new HttpRequestException($"[{responseMessage.StatusCode}] Http Failure to {url} with expiry {absoluteExpiration}: {responseMessage.ReasonPhrase}"))
            : Signal.FromAsync(() => responseMessage.Content.ReadAsByteArrayAsync());

    /// <summary>Reads the response body as a byte array, throwing if the response status indicates failure.</summary>
    /// <param name="responseMessage">The HTTP response to process.</param>
    /// <param name="url">The original request URI, used in error messages.</param>
    /// <param name="absoluteExpiration">The requested absolute expiration, used in error messages.</param>
    /// <returns>An observable that emits the response bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IObservable<byte[]> ProcessWebResponse(HttpResponseMessage responseMessage, Uri url, DateTimeOffset? absoluteExpiration) =>
        ProcessWebResponse(responseMessage, url?.OriginalString, absoluteExpiration);

    /// <summary>Makes a web request to the specified URI.</summary>
    /// <param name="uri">The URI to make the request to.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <returns>An observable that emits the HTTP response message.</returns>
    protected internal virtual IObservable<HttpResponseMessage> MakeWebRequest(Uri uri, HttpMethod method) =>
        MakeWebRequest(uri, method, (IEnumerable<KeyValuePair<string, string>>?)null, (string?)null, DefaultRetryCount, (TimeSpan?)null);

    /// <summary>Makes a web request to the specified URI.</summary>
    /// <param name="uri">The URI to make the request to.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <returns>An observable that emits the HTTP response message.</returns>
    protected internal virtual IObservable<HttpResponseMessage> MakeWebRequest(Uri uri, HttpMethod method, IEnumerable<KeyValuePair<string, string>>? headers) =>
        MakeWebRequest(uri, method, headers, (string?)null, DefaultRetryCount, (TimeSpan?)null);

    /// <summary>Makes a web request to the specified URI.</summary>
    /// <param name="uri">The URI to make the request to.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="content">Optional content to send with the request.</param>
    /// <returns>An observable that emits the HTTP response message.</returns>
    protected internal virtual IObservable<HttpResponseMessage> MakeWebRequest(Uri uri, HttpMethod method, IEnumerable<KeyValuePair<string, string>>? headers, string? content) =>
        MakeWebRequest(uri, method, headers, content, DefaultRetryCount, (TimeSpan?)null);

    /// <summary>Makes a web request to the specified URI.</summary>
    /// <param name="uri">The URI to make the request to.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="content">Optional content to send with the request.</param>
    /// <param name="retries">The number of retry attempts for failed requests.</param>
    /// <returns>An observable that emits the HTTP response message.</returns>
    protected internal virtual IObservable<HttpResponseMessage> MakeWebRequest(Uri uri, HttpMethod method, IEnumerable<KeyValuePair<string, string>>? headers, string? content, int retries) =>
        MakeWebRequest(uri, method, headers, content, retries, (TimeSpan?)null);

    /// <summary>Makes a web request to the specified URI.</summary>
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
        IEnumerable<KeyValuePair<string, string>>? headers,
        string? content,
        int retries,
        TimeSpan? timeout)
    {
        var request = Signal.Defer(() =>
        {
            var httpRequest = CreateWebRequest(uri, method, headers);

            if (content is null)
            {
                return Signal.FromAsync(() => HttpClient.SendAsync(httpRequest));
            }

            httpRequest.Content = new StringContent(content);

            return Signal.FromAsync(() => HttpClient.SendAsync(httpRequest));
        });

        var timedRequest = request.Timeout(timeout ?? DefaultTimeout, CacheDatabase.TaskpoolScheduler);

        // retries is the total number of attempts, but Retry counts re-subscriptions after the
        // first one, so the initial attempt has to come off the top.
        return retries > 0 ? timedRequest.Retry(retries - 1) : timedRequest;
    }

    /// <summary>Releases the resources used by the <see cref="HttpService"/>.</summary>
    /// <param name="isDisposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing)
    {
        if (!isDisposing || Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        HttpClient.Dispose();
    }

    /// <summary>Provides a fast-failing HTTP service that reduces retries and timeouts to speed up tests.</summary>
    [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
    public class FastHttpService : HttpService
    {
        /// <summary>Retry count this fast variant applies when a caller does not state one.</summary>
        internal const int FastDefaultRetryCount = 0;

        /// <summary>Timeout this fast variant applies when a caller does not state one.</summary>
        internal static readonly TimeSpan FastDefaultTimeout = TimeSpan.FromSeconds(2);

        /// <summary>The number of retry attempts configured for outgoing requests.</summary>
        private readonly int _retries;

        /// <summary>The request timeout configured for outgoing requests.</summary>
        private readonly TimeSpan _timeout;

        /// <summary>Initializes a new instance of the <see cref="FastHttpService"/> class that does not retry and times out after two seconds.</summary>
        public FastHttpService()
            : this(FastDefaultRetryCount, FastDefaultTimeout)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="FastHttpService"/> class that times out after two seconds.</summary>
        /// <param name="retries">The number of retry attempts to use.</param>
        public FastHttpService(int retries)
            : this(retries, FastDefaultTimeout)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="FastHttpService"/> class.</summary>
        /// <param name="retries">The number of retry attempts to use.</param>
        /// <param name="timeout">The timeout duration to use.</param>
        public FastHttpService(int retries, TimeSpan timeout)
        {
            _retries = retries;
            _timeout = timeout;

            // Also bound the client itself so it honors the same timeout. The client is the one the
            // base constructor just created, so it can neither have issued a request nor been
            // disposed; the only way the assignment fails is the value itself being out of range,
            // and then the timeout enforced in MakeWebRequest still holds.
            try
            {
                HttpClient.Timeout = _timeout;
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        /// <inheritdoc />
        /// <remarks>The caller's retry count and timeout are ignored; the fast variant exists to bound how long a test can wait.</remarks>
        protected internal override IObservable<HttpResponseMessage> MakeWebRequest(
            Uri uri,
            HttpMethod method,
            IEnumerable<KeyValuePair<string, string>>? headers,
            string? content,
            int retries,
            TimeSpan? timeout) =>
            base.MakeWebRequest(uri, method, headers, content, _retries, _timeout);
    }
}
