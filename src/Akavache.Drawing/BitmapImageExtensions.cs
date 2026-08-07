// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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
        public IObservable<IBitmap> LoadImage(string key) =>
            blobCache.LoadImage(key, (float?)null, (float?)null);

        /// <summary>Load an image from the blob cache.</summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
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
                    ThrowOnNullOrBadImageBuffer,
                    x => BytesToImage(x, desiredWidth, desiredHeight));
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
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
                .SelectManyThen(ThrowOnBadImageBuffer, x => BytesToImage(x, desiredWidth, desiredHeight));
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the bitmap image. blobCache
        /// Observable is guaranteed to be returned on the UI thread.</returns>
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
                .SelectManyThen(ThrowOnBadImageBuffer, x => BytesToImage(x, desiredWidth, desiredHeight));
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
                .SelectManyThen(ThrowOnBadImageBuffer, x => BytesToImage(x, desiredWidth, desiredHeight));
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
                .SelectManyThen(ThrowOnBadImageBuffer, x => BytesToImage(x, desiredWidth, desiredHeight));
        }

        /// <summary>Save an image to the blob cache.</summary>
        /// <param name="key">The key to associate with the image.</param>
        /// <param name="image">The bitmap image to save.</param>
        /// <returns>A Future result representing the completion of the save operation.</returns>
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

    /// <summary>
    /// Emits <paramref name="compressedImage"/> through an observable, or signals an
    /// <see cref="InvalidOperationException"/> when the buffer is corrupt — that is,
    /// <see langword="null"/> or smaller than the 64-byte minimum.
    /// </summary>
    /// <param name="compressedImage">The compressed image buffer to check.</param>
    /// <returns>An observable emitting the buffer, or signalling an error when invalid.</returns>
    internal static IObservable<byte[]> ThrowOnBadImageBuffer(byte[]? compressedImage) =>
        compressedImage is null || compressedImage.Length < 64
            ? new ImmediateThrowSignal<byte[]>(new InvalidOperationException("Invalid Image"))
            : Signal.Return(compressedImage);

    /// <summary>
    /// Routes a potentially null byte buffer from a blob cache through the
    /// bad-image guard, emitting a descriptive <c>"Image data is null"</c> error
    /// when the buffer itself is <see langword="null"/>.
    /// </summary>
    /// <param name="bytes">The bytes returned by the blob cache, possibly <see langword="null"/>.</param>
    /// <returns>An observable emitting <paramref name="bytes"/>, or an error.</returns>
    internal static IObservable<byte[]> ThrowOnNullOrBadImageBuffer(byte[]? bytes) =>
        bytes is null
            ? new ImmediateThrowSignal<byte[]>(new InvalidOperationException("Image data is null"))
            : ThrowOnBadImageBuffer(bytes);

    /// <summary>Converts a compressed image byte array into an <see cref="IBitmap"/> using Splat's ambient <see cref="BitmapLoader.Current"/>.</summary>
    /// <remarks>
    /// Throws <see cref="IOException"/> when the loader returns
    /// <see langword="null"/>.
    /// </remarks>
    /// <param name="compressedImage">The compressed image bytes.</param>
    /// <param name="desiredWidth">Optional desired width.</param>
    /// <param name="desiredHeight">Optional desired height.</param>
    /// <returns>An observable emitting the decoded bitmap.</returns>
    internal static IObservable<IBitmap> BytesToImage(byte[] compressedImage, float? desiredWidth, float? desiredHeight) =>
        Signal.FromAsync(async () =>
        {
#if NETFRAMEWORK
            using var ms = new MemoryStream(compressedImage, writable: false);
#else
            await using MemoryStream ms = new(compressedImage, writable: false);
#endif
            var bitmap = await BitmapLoader.Current.Load(ms, desiredWidth, desiredHeight).ConfigureAwait(false);
            return bitmap ?? throw new IOException("Failed to load the bitmap!");
        });
}
