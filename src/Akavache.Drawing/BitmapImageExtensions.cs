// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Splat;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Drawing;
#else
namespace Akavache.Drawing;
#endif

/// <summary>Provides extension methods associated with the <see cref="IBitmap" /> interface.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2234:Pass System.Uri objects instead of strings",
    Justification = "The string overloads are long-standing public API. Each is paired with a Uri overload it forwards to, so the Uri form is always available.")]
public static class BitmapImageExtensions
{
    /// <summary>Extension members for <c>IBitmap</c>.</summary>
    /// <param name="image">The bitmap image to convert.</param>
    extension(IBitmap image)
    {
        /// <summary>Convert an IBitmap to a byte array asynchronously.</summary>
        /// <returns>A Future result representing the byte array.</returns>
        public IObservable<byte[]> ImageToBytes()
        {
            ArgumentExceptionHelper.ThrowIfNull(image);

            return Signal.FromAsync(async () =>
            {
                // Pre-size the buffer to a typical small-PNG worst case so a sequence of regrowths
                // (starting at 256 and doubling) is avoided for anything under ~16 KB.
                const int InitialCapacity = 16 * 1024;
#if NETFRAMEWORK
                using var stream = new MemoryStream(InitialCapacity);
#else
                await using MemoryStream stream = new(InitialCapacity);
#endif
                await image.Save(CompressedBitmapFormat.Png, 1.0F, stream).ConfigureAwait(false);
                return stream.ToArray();
            });
        }
    }

    /// <summary>Extension members for <c>IBlobCache</c>.</summary>
    /// <param name="blobCache">The blob cache to load the image from.</param>
    extension(IBlobCache blobCache)
    {
        /// <summary>Load an image from the blob cache.</summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImage(string key) =>
            blobCache.LoadImage(key, (float?)null, (float?)null);

        /// <summary>Load an image from the blob cache.</summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImage(string key, float? desiredWidth) =>
            blobCache.LoadImage(key, desiredWidth, (float?)null);

        /// <summary>Load an image from the blob cache.</summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        public IObservable<IBitmap> LoadImage(string key, float? desiredWidth, float? desiredHeight)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.Get(key)
                .SelectManyThen(
                    BitmapHelpers.ThrowOnNullOrBadImageBuffer,
                    x => BitmapHelpers.BytesToImage(x, desiredWidth, desiredHeight));
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string url) =>
            blobCache.LoadImageFromUrl(url, false, (float?)null, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string url, bool fetchAlways) =>
            blobCache.LoadImageFromUrl(url, fetchAlways, (float?)null, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string url, bool fetchAlways, float? desiredWidth) =>
            blobCache.LoadImageFromUrl(url, fetchAlways, desiredWidth, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string url, bool fetchAlways, float? desiredWidth, float? desiredHeight) =>
            blobCache.LoadImageFromUrl(url, fetchAlways, desiredWidth, desiredHeight, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        public IObservable<IBitmap> LoadImageFromUrl(string url, bool fetchAlways, float? desiredWidth, float? desiredHeight, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
                .SelectManyThen(BitmapHelpers.ThrowOnBadImageBuffer, x => BitmapHelpers.BytesToImage(x, desiredWidth, desiredHeight));
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(Uri url) =>
            blobCache.LoadImageFromUrl(url, false, (float?)null, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(Uri url, bool fetchAlways) =>
            blobCache.LoadImageFromUrl(url, fetchAlways, (float?)null, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(Uri url, bool fetchAlways, float? desiredWidth) =>
            blobCache.LoadImageFromUrl(url, fetchAlways, desiredWidth, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(Uri url, bool fetchAlways, float? desiredWidth, float? desiredHeight) =>
            blobCache.LoadImageFromUrl(url, fetchAlways, desiredWidth, desiredHeight, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        public IObservable<IBitmap> LoadImageFromUrl(Uri url, bool fetchAlways, float? desiredWidth, float? desiredHeight, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
                .SelectManyThen(BitmapHelpers.ThrowOnBadImageBuffer, x => BitmapHelpers.BytesToImage(x, desiredWidth, desiredHeight));
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string key, string url) =>
            blobCache.LoadImageFromUrl(key, url, false, (float?)null, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string key, string url, bool fetchAlways) =>
            blobCache.LoadImageFromUrl(key, url, fetchAlways, (float?)null, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string key, string url, bool fetchAlways, float? desiredWidth) =>
            blobCache.LoadImageFromUrl(key, url, fetchAlways, desiredWidth, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string key, string url, bool fetchAlways, float? desiredWidth, float? desiredHeight) =>
            blobCache.LoadImageFromUrl(key, url, fetchAlways, desiredWidth, desiredHeight, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        public IObservable<IBitmap> LoadImageFromUrl(string key, string url, bool fetchAlways, float? desiredWidth, float? desiredHeight, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
                .SelectManyThen(BitmapHelpers.ThrowOnBadImageBuffer, x => BitmapHelpers.BytesToImage(x, desiredWidth, desiredHeight));
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string key, Uri url) =>
            blobCache.LoadImageFromUrl(key, url, false, (float?)null, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string key, Uri url, bool fetchAlways) =>
            blobCache.LoadImageFromUrl(key, url, fetchAlways, (float?)null, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string key, Uri url, bool fetchAlways, float? desiredWidth) =>
            blobCache.LoadImageFromUrl(key, url, fetchAlways, desiredWidth, (float?)null, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<IBitmap> LoadImageFromUrl(string key, Uri url, bool fetchAlways, float? desiredWidth, float? desiredHeight) =>
            blobCache.LoadImageFromUrl(key, url, fetchAlways, desiredWidth, desiredHeight, (DateTimeOffset?)null);

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        public IObservable<IBitmap> LoadImageFromUrl(string key, Uri url, bool fetchAlways, float? desiredWidth, float? desiredHeight, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
                .SelectManyThen(BitmapHelpers.ThrowOnBadImageBuffer, x => BitmapHelpers.BytesToImage(x, desiredWidth, desiredHeight));
        }

        /// <summary>Save an image to the blob cache.</summary>
        /// <param name="key">The key to associate with the image.</param>
        /// <param name="image">The bitmap image to save.</param>
        /// <returns>A Future result representing the completion of the save operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> SaveImage(string key, IBitmap image) =>
            blobCache.SaveImage(key, image, (DateTimeOffset?)null);

        /// <summary>Save an image to the blob cache.</summary>
        /// <param name="key">The key to associate with the image.</param>
        /// <param name="image">The bitmap image to save.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the completion of the save operation.</returns>
        public IObservable<RxVoid> SaveImage(string key, IBitmap image, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentExceptionHelper.ThrowIfNull(image);

            return image.ImageToBytes()
                .SelectMany(bytes => blobCache.Insert(key, bytes, absoluteExpiration));
        }
    }
}
