// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Integration.Tests;

/// <summary>
/// An <see cref="IHttpService"/> stand-in that reproduces the cache-first contract of the real
/// service without any network I/O. When <c>fetchAlways</c> is not set it serves whatever the
/// blob cache already holds under the key and only produces the download payload on a miss;
/// when it is set the payload is produced unconditionally. The payload is deliberately a
/// different length from the images the tests seed, so the byte count a bitmap loader receives
/// says which of the two paths ran. The <c>fetchAlways</c> and <c>absoluteExpiration</c>
/// arguments of the most recent call are recorded so a test can pin down the values a
/// forwarding overload supplied on the caller's behalf.
/// </summary>
internal sealed class CacheBackedHttpService : IHttpService
{
    /// <summary>Byte length of the payload a simulated download produces. Above the 64-byte minimum a decodable image must meet, and different from the images the tests seed.</summary>
    internal const int DownloadedPayloadLength = 96;

    /// <summary>The payload every simulated download produces.</summary>
    private static readonly byte[] DownloadedPayload = new byte[DownloadedPayloadLength];

    /// <summary>Gets the number of simulated downloads performed so far.</summary>
    internal int DownloadCount { get; private set; }

    /// <summary>Gets the <c>fetchAlways</c> argument of the most recent download call, or <see langword="null"/> when no call has been made.</summary>
    internal bool? LastFetchAlways { get; private set; }

    /// <summary>Gets the <c>absoluteExpiration</c> argument of the most recent download call.</summary>
    internal DateTimeOffset? LastAbsoluteExpiration { get; private set; }

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url) =>
        DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method) =>
        DownloadUrl(blobCache, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
        DownloadUrl(blobCache, url, method, headers, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
        DownloadUrl(blobCache, url, method, headers, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(
        IBlobCache blobCache,
        string url,
        HttpMethod? method,
        IEnumerable<KeyValuePair<string, string>>? headers,
        bool fetchAlways,
        DateTimeOffset? absoluteExpiration) =>
        Download(blobCache, url, fetchAlways, absoluteExpiration);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, bool fetchAlways) =>
        DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url) =>
        DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method) =>
        DownloadUrl(blobCache, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
        DownloadUrl(blobCache, url, method, headers, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
        DownloadUrl(blobCache, url, method, headers, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(
        IBlobCache blobCache,
        Uri url,
        HttpMethod? method,
        IEnumerable<KeyValuePair<string, string>>? headers,
        bool fetchAlways,
        DateTimeOffset? absoluteExpiration) =>
        Download(blobCache, url?.ToString() ?? string.Empty, fetchAlways, absoluteExpiration);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, bool fetchAlways) =>
        DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url) =>
        DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method) =>
        DownloadUrl(blobCache, key, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
        DownloadUrl(blobCache, key, url, method, headers, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
        DownloadUrl(blobCache, key, url, method, headers, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(
        IBlobCache blobCache,
        string key,
        string url,
        HttpMethod? method,
        IEnumerable<KeyValuePair<string, string>>? headers,
        bool fetchAlways,
        DateTimeOffset? absoluteExpiration) =>
        Download(blobCache, key, fetchAlways, absoluteExpiration);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, bool fetchAlways) =>
        DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url) =>
        DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method) =>
        DownloadUrl(blobCache, key, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
        DownloadUrl(blobCache, key, url, method, headers, false, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
        DownloadUrl(blobCache, key, url, method, headers, fetchAlways, (DateTimeOffset?)null);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(
        IBlobCache blobCache,
        string key,
        Uri url,
        HttpMethod? method,
        IEnumerable<KeyValuePair<string, string>>? headers,
        bool fetchAlways,
        DateTimeOffset? absoluteExpiration) =>
        Download(blobCache, key, fetchAlways, absoluteExpiration);

    /// <inheritdoc/>
    public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, bool fetchAlways) =>
        DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

    /// <summary>Records the request arguments and serves the response from the cache unless the caller demanded a fresh fetch.</summary>
    /// <param name="blobCache">The cache the request was made against.</param>
    /// <param name="key">The cache key the response is stored under.</param>
    /// <param name="fetchAlways">Whether the caller asked for the cache to be bypassed.</param>
    /// <param name="absoluteExpiration">The expiration the caller asked for.</param>
    /// <returns>An observable emitting either the cached bytes or the simulated download payload.</returns>
    private IObservable<byte[]> Download(IBlobCache blobCache, string key, bool fetchAlways, DateTimeOffset? absoluteExpiration)
    {
        LastFetchAlways = fetchAlways;
        LastAbsoluteExpiration = absoluteExpiration;

        var fetch = Observable.Defer(() =>
        {
            DownloadCount++;
            return Observable.Return(DownloadedPayload);
        });

        return fetchAlways
            ? fetch
            : blobCache.Get(key).Select(static bytes => bytes ?? []).Catch(fetch);
    }
}
