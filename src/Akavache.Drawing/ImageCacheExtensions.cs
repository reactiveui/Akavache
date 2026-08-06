// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;

using Splat;

namespace Akavache.Drawing;

/// <summary>Advanced image caching and manipulation extensions.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2234:Pass System.Uri objects instead of strings",
    Justification = "The string overloads are long-standing public API. Each is paired with a Uri overload it forwards to, so the Uri form is always available.")]
public static class ImageCacheExtensions
{
    /// <summary>Extension members for <c>IBlobCache</c>.</summary>
    /// <param name="blobCache">The blob cache to load images from.</param>
    extension(IBlobCache blobCache)
    {
        /// <summary>Load multiple images from the cache with specified keys.</summary>
        /// <param name="keys">The keys to look up in the cache.</param>
        /// <returns>An observable sequence of key-bitmap pairs.</returns>
        public IObservable<KeyValuePair<string, IBitmap>> LoadImages(IEnumerable<string> keys) =>
            blobCache.LoadImages(keys, (float?)null, (float?)null);

        /// <summary>Load multiple images from the cache with specified keys.</summary>
        /// <param name="keys">The keys to look up in the cache.</param>
        /// <param name="desiredWidth">Optional desired width for all images.</param>
        /// <returns>An observable sequence of key-bitmap pairs.</returns>
        public IObservable<KeyValuePair<string, IBitmap>> LoadImages(IEnumerable<string> keys, float? desiredWidth) =>
            blobCache.LoadImages(keys, desiredWidth, (float?)null);

        /// <summary>Load multiple images from the cache with specified keys.</summary>
        /// <param name="keys">The keys to look up in the cache.</param>
        /// <param name="desiredWidth">Optional desired width for all images.</param>
        /// <param name="desiredHeight">Optional desired height for all images.</param>
        /// <returns>An observable sequence of key-bitmap pairs.</returns>
        public IObservable<KeyValuePair<string, IBitmap>> LoadImages(IEnumerable<string> keys, float? desiredWidth, float? desiredHeight)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return keys.ToObservable()
                .SelectMany(key => blobCache.LoadImage(key, desiredWidth, desiredHeight)
                    .Select(bitmap => new KeyValuePair<string, IBitmap>(key, bitmap))
                    .Catch<KeyValuePair<string, IBitmap>, Exception>(static _ => Observable.Empty<KeyValuePair<string, IBitmap>>()));
        }

        /// <summary>Preload and cache images from multiple URLs.</summary>
        /// <param name="urls">The URLs to download and cache.</param>
        /// <returns>An observable that completes when all images are cached.</returns>
        public IObservable<Unit> PreloadImagesFromUrls(IEnumerable<string> urls) =>
            blobCache.PreloadImagesFromUrls(urls, (DateTimeOffset?)null);

        /// <summary>Preload and cache images from multiple URLs.</summary>
        /// <param name="urls">The URLs to download and cache.</param>
        /// <param name="absoluteExpiration">Optional expiration date for cached images.</param>
        /// <returns>An observable that completes when all images are cached.</returns>
        public IObservable<Unit> PreloadImagesFromUrls(IEnumerable<string> urls, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return urls.ToObservable()
                .SelectMany(url => blobCache.DownloadUrl(url, absoluteExpiration: absoluteExpiration)
                    .Catch<byte[], Exception>(static _ => Observable.Empty<byte[]>()))
                .SelectUnit()
                .DefaultIfEmpty(Unit.Default)
                .TakeLast(1);
        }

        /// <summary>Load an image with automatic fallback to a default image if loading fails.</summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <param name="fallbackImageBytes">Default image bytes to use if loading fails.</param>
        /// <returns>The loaded image or the fallback image.</returns>
        public IObservable<IBitmap> LoadImageWithFallback(string key, byte[] fallbackImageBytes) =>
            blobCache.LoadImageWithFallback(key, fallbackImageBytes, (float?)null, (float?)null);

        /// <summary>Load an image with automatic fallback to a default image if loading fails.</summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <param name="fallbackImageBytes">Default image bytes to use if loading fails.</param>
        /// <param name="desiredWidth">Optional desired width.</param>
        /// <returns>The loaded image or the fallback image.</returns>
        public IObservable<IBitmap> LoadImageWithFallback(string key, byte[] fallbackImageBytes, float? desiredWidth) =>
            blobCache.LoadImageWithFallback(key, fallbackImageBytes, desiredWidth, (float?)null);

        /// <summary>Load an image with automatic fallback to a default image if loading fails.</summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <param name="fallbackImageBytes">Default image bytes to use if loading fails.</param>
        /// <param name="desiredWidth">Optional desired width.</param>
        /// <param name="desiredHeight">Optional desired height.</param>
        /// <returns>The loaded image or the fallback image.</returns>
        public IObservable<IBitmap> LoadImageWithFallback(string key, byte[] fallbackImageBytes, float? desiredWidth, float? desiredHeight)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentExceptionHelper.ThrowIfNull(fallbackImageBytes);

            return blobCache.LoadImage(key, desiredWidth, desiredHeight)
                .Catch<IBitmap, Exception>(_ => BytesToImage(fallbackImageBytes, desiredWidth, desiredHeight));
        }

        /// <summary>Load an image from URL with automatic fallback to a default image if loading fails.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fallbackImageBytes">Default image bytes to use if loading fails.</param>
        /// <returns>The loaded image or the fallback image.</returns>
        public IObservable<IBitmap> LoadImageFromUrlWithFallback(string url, byte[] fallbackImageBytes) =>
            blobCache.LoadImageFromUrlWithFallback(url, fallbackImageBytes, false, (float?)null, (float?)null, (DateTimeOffset?)null);

        /// <summary>Load an image from URL with automatic fallback to a default image if loading fails.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fallbackImageBytes">Default image bytes to use if loading fails.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL.</param>
        /// <returns>The loaded image or the fallback image.</returns>
        public IObservable<IBitmap> LoadImageFromUrlWithFallback(string url, byte[] fallbackImageBytes, bool fetchAlways) =>
            blobCache.LoadImageFromUrlWithFallback(url, fallbackImageBytes, fetchAlways, (float?)null, (float?)null, (DateTimeOffset?)null);

        /// <summary>Load an image from URL with automatic fallback to a default image if loading fails.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fallbackImageBytes">Default image bytes to use if loading fails.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL.</param>
        /// <param name="desiredWidth">Optional desired width.</param>
        /// <returns>The loaded image or the fallback image.</returns>
        public IObservable<IBitmap> LoadImageFromUrlWithFallback(string url, byte[] fallbackImageBytes, bool fetchAlways, float? desiredWidth) =>
            blobCache.LoadImageFromUrlWithFallback(url, fallbackImageBytes, fetchAlways, desiredWidth, (float?)null, (DateTimeOffset?)null);

        /// <summary>Load an image from URL with automatic fallback to a default image if loading fails.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fallbackImageBytes">Default image bytes to use if loading fails.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL.</param>
        /// <param name="desiredWidth">Optional desired width.</param>
        /// <param name="desiredHeight">Optional desired height.</param>
        /// <returns>The loaded image or the fallback image.</returns>
        public IObservable<IBitmap> LoadImageFromUrlWithFallback(string url, byte[] fallbackImageBytes, bool fetchAlways, float? desiredWidth, float? desiredHeight) =>
            blobCache.LoadImageFromUrlWithFallback(url, fallbackImageBytes, fetchAlways, desiredWidth, desiredHeight, (DateTimeOffset?)null);

        /// <summary>Load an image from URL with automatic fallback to a default image if loading fails.</summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="fallbackImageBytes">Default image bytes to use if loading fails.</param>
        /// <param name="fetchAlways">If we should always fetch the image from the URL.</param>
        /// <param name="desiredWidth">Optional desired width.</param>
        /// <param name="desiredHeight">Optional desired height.</param>
        /// <param name="absoluteExpiration">Optional expiration date.</param>
        /// <returns>The loaded image or the fallback image.</returns>
        public IObservable<IBitmap> LoadImageFromUrlWithFallback(string url, byte[] fallbackImageBytes, bool fetchAlways, float? desiredWidth, float? desiredHeight, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentExceptionHelper.ThrowIfNull(fallbackImageBytes);

            return blobCache.LoadImageFromUrl(url, fetchAlways, desiredWidth, desiredHeight, absoluteExpiration)
                .Catch<IBitmap, Exception>(_ => BytesToImage(fallbackImageBytes, desiredWidth, desiredHeight));
        }

        /// <summary>Create a thumbnail version of an image and cache it separately.</summary>
        /// <param name="sourceKey">The key of the source image.</param>
        /// <param name="thumbnailKey">The key to store the thumbnail under.</param>
        /// <param name="thumbnailWidth">The desired thumbnail width.</param>
        /// <param name="thumbnailHeight">The desired thumbnail height.</param>
        /// <returns>An observable that completes when the thumbnail is created and cached.</returns>
        public IObservable<Unit> CreateAndCacheThumbnail(string sourceKey, string thumbnailKey, float thumbnailWidth, float thumbnailHeight) =>
            blobCache.CreateAndCacheThumbnail(sourceKey, thumbnailKey, thumbnailWidth, thumbnailHeight, (DateTimeOffset?)null);

        /// <summary>Create a thumbnail version of an image and cache it separately.</summary>
        /// <param name="sourceKey">The key of the source image.</param>
        /// <param name="thumbnailKey">The key to store the thumbnail under.</param>
        /// <param name="thumbnailWidth">The desired thumbnail width.</param>
        /// <param name="thumbnailHeight">The desired thumbnail height.</param>
        /// <param name="absoluteExpiration">Optional expiration date for the thumbnail.</param>
        /// <returns>An observable that completes when the thumbnail is created and cached.</returns>
        public IObservable<Unit> CreateAndCacheThumbnail(string sourceKey, string thumbnailKey, float thumbnailWidth, float thumbnailHeight, DateTimeOffset? absoluteExpiration)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.LoadImage(sourceKey, thumbnailWidth, thumbnailHeight)
                .SelectMany(thumbnail => blobCache.SaveImage(thumbnailKey, thumbnail, absoluteExpiration));
        }

        /// <summary>Get the size information of a cached image without fully loading it.</summary>
        /// <param name="key">The key of the image.</param>
        /// <returns>An observable containing the image size information.</returns>
        public IObservable<Size> GetImageSize(string key)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);

            return blobCache.Get(key)
                .SelectMany(BitmapImageExtensions.ThrowOnNullOrBadImageBuffer)
                .SelectMany(LoadBitmapSize);
        }

        /// <summary>Clear all cached images that match a specific pattern.</summary>
        /// <param name="keyPattern">A function to determine if a key should be invalidated.</param>
        /// <returns>An observable that completes when all matching images are cleared.</returns>
        public IObservable<Unit> ClearImageCache(Func<string, bool> keyPattern)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            ArgumentExceptionHelper.ThrowIfNull(keyPattern);

            return blobCache.GetAllKeys()
                .Where(keyPattern)
                .SelectMany(blobCache.Invalidate)
                .DefaultIfEmpty(Unit.Default)
                .TakeLast(1);
        }
    }

    /// <summary>
    /// Decodes <paramref name="compressedImage"/> into an <see cref="IBitmap"/> via the
    /// ambient <see cref="BitmapLoader.Current"/>. Pulled out as an <c>internal static</c>
    /// helper so the bitmap-decode path can be unit-tested in isolation against a mocked
    /// loader without needing a full blob-cache pipeline.
    /// </summary>
    /// <param name="compressedImage">The encoded image bytes.</param>
    /// <param name="desiredWidth">Optional target width for the decoded bitmap.</param>
    /// <param name="desiredHeight">Optional target height for the decoded bitmap.</param>
    /// <returns>An observable that emits the decoded bitmap or fails with <see cref="IOException"/>.</returns>
    internal static IObservable<IBitmap> BytesToImage(byte[] compressedImage, float? desiredWidth, float? desiredHeight) =>
        Observable.FromAsync(async () =>
        {
#if NETFRAMEWORK
            using var ms = new MemoryStream(compressedImage, writable: false);
#else
            await using MemoryStream ms = new(compressedImage, writable: false);
#endif
            var bitmap = await BitmapLoader.Current.Load(ms, desiredWidth, desiredHeight).ConfigureAwait(false);
            return bitmap ?? throw new IOException("Failed to load the bitmap!");
        });

    /// <summary>Loads a bitmap from raw bytes and returns its dimensions.</summary>
    /// <param name="bytes">The encoded image bytes.</param>
    /// <returns>An observable that emits the image size.</returns>
    internal static IObservable<Size> LoadBitmapSize(byte[] bytes) =>
        Observable.FromAsync(async () =>
        {
#if NETFRAMEWORK
            using var ms = new MemoryStream(bytes, writable: false);
#else
            await using var ms = new MemoryStream(bytes, writable: false);
#endif
            var bitmap = await BitmapLoader.Current.Load(ms, null, null).ConfigureAwait(false);
            return bitmap is not null ? new Size(bitmap.Width, bitmap.Height) : throw new InvalidOperationException("Failed to load image for size detection");
        });
}
