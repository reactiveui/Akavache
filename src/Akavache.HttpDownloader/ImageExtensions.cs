// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>Extension methods for working with images and bitmaps in the cache.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2234:Pass System.Uri objects instead of strings",
    Justification = "The string overloads are long-standing public API. Each is paired with a Uri overload it forwards to, so the Uri form is always available.")]
public static class ImageExtensions
{
    /// <summary>The PNG header.</summary>
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47];

    /// <summary>The JPEG header.</summary>
    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF];

    /// <summary>Gets the GIF header.</summary>
    private static ReadOnlySpan<byte> GifHeader => "GIF"u8;

    /// <summary>Gets the BMP header.</summary>
    private static ReadOnlySpan<byte> BmpHeader => "BM"u8;

    /// <summary>Extension members for <c>IBlobCache</c>.</summary>
    /// <param name="blobCache">The blob cache to load the image from.</param>
    extension(IBlobCache blobCache)
    {
        /// <summary>Loads image data from the blob cache as raw bytes.</summary>
        /// <param name="key">The cache key to look up.</param>
        /// <returns>An observable that emits the image bytes.</returns>
        public IObservable<byte[]> LoadImageBytes(string key)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.Get(key)
                .SelectMany(ImageBufferHelpers.ThrowOnNullOrBadImageBuffer);
        }

        /// <summary>
        /// Downloads an image from a remote URL and returns the image bytes.
        /// This method combines DownloadUrl and LoadImageBytes functionality,
        /// using cached values when possible.
        /// </summary>
        /// <param name="url">The URL to download the image from.</param>
        /// <returns>An observable that emits the image bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> LoadImageBytesFromUrl(string url) =>
            blobCache.LoadImageBytesFromUrl(url, false, (DateTimeOffset?)null);

        /// <summary>
        /// Downloads an image from a remote URL and returns the image bytes.
        /// This method combines DownloadUrl and LoadImageBytes functionality,
        /// using cached values when possible.
        /// </summary>
        /// <param name="url">The URL to download the image from.</param>
        /// <param name="fetchAlways">A value indicating whether to always fetch the image from the URL, bypassing the cache.</param>
        /// <returns>An observable that emits the image bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> LoadImageBytesFromUrl(string url, bool fetchAlways) =>
            blobCache.LoadImageBytesFromUrl(url, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// Downloads an image from a remote URL and returns the image bytes.
        /// This method combines DownloadUrl and LoadImageBytes functionality,
        /// using cached values when possible.
        /// </summary>
        /// <param name="url">The URL to download the image from.</param>
        /// <param name="fetchAlways">A value indicating whether to always fetch the image from the URL, bypassing the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date for the cached image data.</param>
        /// <returns>An observable that emits the image bytes.</returns>
        public IObservable<byte[]> LoadImageBytesFromUrl(string url, bool fetchAlways, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            ArgumentExceptionHelper.ThrowIfNull(url);

            return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
                .SelectMany(ImageBufferHelpers.ThrowOnBadImageBuffer);
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image bytes.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the image bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> LoadImageBytesFromUrl(Uri url) =>
            blobCache.LoadImageBytesFromUrl(url, false, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image bytes.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <returns>A Future result representing the image bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> LoadImageBytesFromUrl(Uri url, bool fetchAlways) =>
            blobCache.LoadImageBytesFromUrl(url, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image bytes.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the image bytes.</returns>
        public IObservable<byte[]> LoadImageBytesFromUrl(Uri url, bool fetchAlways, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            ArgumentExceptionHelper.ThrowIfNull(url);

            return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
                .SelectMany(ImageBufferHelpers.ThrowOnBadImageBuffer);
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image bytes.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the image bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> LoadImageBytesFromUrl(string key, string url) =>
            blobCache.LoadImageBytesFromUrl(key, url, false, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image bytes.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <returns>A Future result representing the image bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> LoadImageBytesFromUrl(string key, string url, bool fetchAlways) =>
            blobCache.LoadImageBytesFromUrl(key, url, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image bytes.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the image bytes.</returns>
        public IObservable<byte[]> LoadImageBytesFromUrl(string key, string url, bool fetchAlways, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            ArgumentExceptionHelper.ThrowIfNull(url);

            return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
                .SelectMany(ImageBufferHelpers.ThrowOnBadImageBuffer);
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image bytes.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the image bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> LoadImageBytesFromUrl(string key, Uri url) =>
            blobCache.LoadImageBytesFromUrl(key, url, false, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image bytes.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <returns>A Future result representing the image bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]> LoadImageBytesFromUrl(string key, Uri url, bool fetchAlways) =>
            blobCache.LoadImageBytesFromUrl(key, url, fetchAlways, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image bytes.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the image bytes.</returns>
        public IObservable<byte[]> LoadImageBytesFromUrl(string key, Uri url, bool fetchAlways, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            ArgumentExceptionHelper.ThrowIfNull(url);

            return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
                .SelectMany(ImageBufferHelpers.ThrowOnBadImageBuffer);
        }
    }

    /// <summary>Extension members for <c>byte[]</c>.</summary>
    /// <param name="imageBytes">The image bytes to validate.</param>
    extension(byte[] imageBytes)
    {
        /// <summary>Validates that the provided bytes represent a valid image format by checking file headers.</summary>
        /// <returns><c>true</c> if the bytes appear to be a valid image format; otherwise, <c>false</c>.</returns>
        public bool IsValidImageFormat()
        {
            if (imageBytes is null || imageBytes.Length < 4)
            {
                return false;
            }

            var header = imageBytes.AsSpan();

            return header.StartsWith(PngHeader)
                   || header.StartsWith(JpegHeader)
                   || header.StartsWith(GifHeader)
                   || header.StartsWith(BmpHeader)
                   || ImageBufferHelpers.IsWebP(imageBytes);
        }
    }
}
