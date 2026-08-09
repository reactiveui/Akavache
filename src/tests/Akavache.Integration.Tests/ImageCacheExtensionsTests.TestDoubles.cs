// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Splat;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Bitmap and HTTP test doubles used by the Akavache.Drawing ImageCacheExtensions tests.</summary>
public partial class ImageCacheExtensionsTests
{
    /// <summary>Mock bitmap implementation for testing.</summary>
    private sealed class MockBitmap : IBitmap
    {
        /// <summary>Width this mock reports for every bitmap it produces.</summary>
        private const float ReportedWidthPixels = 100F;

        /// <summary>Height this mock reports for every bitmap it produces.</summary>
        private const float ReportedHeightPixels = 200F;

        /// <inheritdoc/>
        public float Width => ReportedWidthPixels;

        /// <inheritdoc/>
        public float Height => ReportedHeightPixels;

        /// <inheritdoc/>
        public Task Save(CompressedBitmapFormat format, float quality, Stream target)
        {
            byte[] mockPngData = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
            return target.WriteAsync(mockPngData, 0, mockPngData.Length);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }

    /// <summary>Mock bitmap loader implementation for testing.</summary>
    private sealed class MockBitmapLoader : IBitmapLoader
    {
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<IBitmap?> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(new MockBitmap());

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBitmap Create(float width, float height) => new MockBitmap();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<IBitmap?> LoadFromResource(string source, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(new MockBitmap());
    }

    /// <summary>A test-local HTTP service that immediately errors to avoid real network I/O.</summary>
    private sealed class ThrowingHttpService : IHttpService
    {
        /// <summary>Message carried by the failure every download call produces.</summary>
        private const string DownloadFailureMessage = "Test HTTP failure";

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
            Signal.Throw<byte[]>(new HttpRequestException(DownloadFailureMessage));

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
            Signal.Throw<byte[]>(new HttpRequestException(DownloadFailureMessage));

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
            Signal.Throw<byte[]>(new HttpRequestException(DownloadFailureMessage));

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
            Signal.Throw<byte[]>(new HttpRequestException(DownloadFailureMessage));

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, bool fetchAlways) =>
            DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);
    }

    /// <summary>A test-local HTTP service that returns a successful byte payload without real network I/O.</summary>
    private sealed class SuccessHttpService : IHttpService
    {
        /// <summary>Fixed byte payload returned from every download call.</summary>
        private static readonly byte[] Payload = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

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
            Signal.Return(Payload);

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
            Signal.Return(Payload);

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
            Signal.Return(Payload);

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
            Signal.Return(Payload);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, bool fetchAlways) =>
            DownloadUrl(blobCache, key, url, (HttpMethod?)null, (IEnumerable<KeyValuePair<string, string>>?)null, fetchAlways, (DateTimeOffset?)null);
    }

    /// <summary>A bitmap loader that always returns a null bitmap to exercise error paths.</summary>
    private sealed class NullBitmapLoader : IBitmapLoader
    {
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<IBitmap?> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBitmap Create(float width, float height) => new MockBitmap();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<IBitmap?> LoadFromResource(string source, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(null);
    }

    /// <summary>
    /// A bitmap loader that captures the arguments passed to <c>Load</c> so tests can
    /// assert the caller forwarded the expected dimensions and stream payload.
    /// </summary>
    private sealed class SizeCapturingBitmapLoader : IBitmapLoader
    {
        /// <summary>Gets the <c>desiredWidth</c> argument from the most recent <c>Load</c> call.</summary>
        public float? LastWidth { get; private set; }

        /// <summary>Gets the <c>desiredHeight</c> argument from the most recent <c>Load</c> call.</summary>
        public float? LastHeight { get; private set; }

        /// <summary>Gets the byte length of the stream supplied to the most recent <c>Load</c> call.</summary>
        public long LastStreamLength { get; private set; }

        /// <inheritdoc/>
        public Task<IBitmap?> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight)
        {
            LastWidth = desiredWidth;
            LastHeight = desiredHeight;
            LastStreamLength = sourceStream.Length;
            return Task.FromResult<IBitmap?>(new MockBitmap());
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBitmap Create(float width, float height) => new MockBitmap();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<IBitmap?> LoadFromResource(string source, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(new MockBitmap());
    }

    /// <summary>Helper to restore the bitmap loader after a test.</summary>
    /// <param name="original">The loader that was ambient before the test replaced it, reinstated on dispose.</param>
    private sealed class LoaderRestorer(IBitmapLoader? original) : IDisposable
    {
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => RestoreBitmapLoader(original);
    }
}
