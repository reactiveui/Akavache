// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Skeleton tests for HttpService error handling.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("HTTP")]
public class HttpServiceErrorHandlingTests
{
    /// <summary>Ensures DownloadUrl surfaces failure via observable error channel.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task HttpExtensions_FetchUrl_HandlesFailure()
    {
        FakeHttpService service = new();
        SystemJsonSerializer serializer = new();
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        Exception? captured = null;
        _ = service.DownloadUrl(cache, "http://invalid").Subscribe(static _ => { }, ex => captured = ex);
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured).IsTypeOf<HttpRequestException>();
    }

    /// <summary>Fake implementation throwing for all calls.</summary>
    private sealed class FakeHttpService : IHttpService
    {
        /// <summary>The message carried by the <see cref="HttpRequestException"/> that every download raises.</summary>
        private const string FailureMessage = "Simulated failure";

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url) =>
            DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method) =>
            DownloadUrl(blobCache, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
            DownloadUrl(blobCache, url, method, headers, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
            DownloadUrl(blobCache, url, method, headers, fetchAlways, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(
            IBlobCache blobCache,
            string url,
            HttpMethod? method,
            IEnumerable<KeyValuePair<string, string>>? headers,
            bool fetchAlways,
            DateTimeOffset? absoluteExpiration) =>
            Signal.Throw<byte[]>(new HttpRequestException(FailureMessage));

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, bool fetchAlways) =>
            DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url) =>
            DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method) =>
            DownloadUrl(blobCache, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
            DownloadUrl(blobCache, url, method, headers, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
            DownloadUrl(blobCache, url, method, headers, fetchAlways, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(
            IBlobCache blobCache,
            Uri url,
            HttpMethod? method,
            IEnumerable<KeyValuePair<string, string>>? headers,
            bool fetchAlways,
            DateTimeOffset? absoluteExpiration) =>
            Signal.Throw<byte[]>(new HttpRequestException(FailureMessage));

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, bool fetchAlways) =>
            DownloadUrl(blobCache, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url) =>
            DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method) =>
            DownloadUrl(blobCache, key, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
            DownloadUrl(blobCache, key, url, method, headers, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
            DownloadUrl(blobCache, key, url, method, headers, fetchAlways, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(
            IBlobCache blobCache,
            string key,
            string url,
            HttpMethod? method,
            IEnumerable<KeyValuePair<string, string>>? headers,
            bool fetchAlways,
            DateTimeOffset? absoluteExpiration) =>
            Signal.Throw<byte[]>(new HttpRequestException(FailureMessage));

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, bool fetchAlways) =>
            DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url) =>
            DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method) =>
            DownloadUrl(blobCache, key, url, method, (IEnumerable<KeyValuePair<string, string>>?)null, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers) =>
            DownloadUrl(blobCache, key, url, method, headers, false, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways) =>
            DownloadUrl(blobCache, key, url, method, headers, fetchAlways, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(
            IBlobCache blobCache,
            string key,
            Uri url,
            HttpMethod? method,
            IEnumerable<KeyValuePair<string, string>>? headers,
            bool fetchAlways,
            DateTimeOffset? absoluteExpiration) =>
            Signal.Throw<byte[]>(new HttpRequestException(FailureMessage));

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, bool fetchAlways) =>
            DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);
    }
}
