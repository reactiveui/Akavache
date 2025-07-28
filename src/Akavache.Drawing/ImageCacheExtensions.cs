// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Splat;

namespace Akavache.Drawing;

/// <summary>
/// Advanced image caching and manipulation extensions.
/// </summary>
public static class ImageCacheExtensions
{
    /// <summary>
    /// Load multiple images from the cache with specified keys.
    /// </summary>
    /// <param name="blobCache">The blob cache to load images from.</param>
    /// <param name="keys">The keys to look up in the cache.</param>
    /// <param name="desiredWidth">Optional desired width for all images.</param>
    /// <param name="desiredHeight">Optional desired height for all images.</param>
    /// <returns>An observable sequence of key-bitmap pairs.</returns>
    public static IObservable<KeyValuePair<string, IBitmap>> LoadImages(this IBlobCache blobCache, IEnumerable<string> keys, float? desiredWidth = null, float? desiredHeight = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return keys.ToObservable()
            .SelectMany(key => blobCache.LoadImage(key, desiredWidth, desiredHeight)
                .Select(bitmap => new KeyValuePair<string, IBitmap>(key, bitmap))
                .Catch<KeyValuePair<string, IBitmap>, Exception>(_ => Observable.Empty<KeyValuePair<string, IBitmap>>()));
    }

    /// <summary>
    /// Preload and cache images from multiple URLs.
    /// </summary>
    /// <param name="blobCache">The blob cache to store images in.</param>
    /// <param name="urls">The URLs to download and cache.</param>
    /// <param name="absoluteExpiration">Optional expiration date for cached images.</param>
    /// <returns>An observable that completes when all images are cached.</returns>
    public static IObservable<Unit> PreloadImagesFromUrls(this IBlobCache blobCache, IEnumerable<string> urls, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return urls.ToObservable()
            .SelectMany(url => blobCache.DownloadUrl(url, absoluteExpiration: absoluteExpiration)
                .Catch<byte[], Exception>(_ => Observable.Empty<byte[]>()))
            .Select(_ => Unit.Default)
            .DefaultIfEmpty(Unit.Default)
            .TakeLast(1);
    }

    /// <summary>
    /// Load an image with automatic fallback to a default image if loading fails.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from.</param>
    /// <param name="key">The key to look up in the cache.</param>
    /// <param name="fallbackImageBytes">Default image bytes to use if loading fails.</param>
    /// <param name="desiredWidth">Optional desired width.</param>
    /// <param name="desiredHeight">Optional desired height.</param>
    /// <returns>The loaded image or the fallback image.</returns>
    public static IObservable<IBitmap> LoadImageWithFallback(this IBlobCache blobCache, string key, byte[] fallbackImageBytes, float? desiredWidth = null, float? desiredHeight = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (fallbackImageBytes is null)
        {
            throw new ArgumentNullException(nameof(fallbackImageBytes));
        }

        return blobCache.LoadImage(key, desiredWidth, desiredHeight)
            .Catch<IBitmap, Exception>(_ => BytesToImage(fallbackImageBytes, desiredWidth, desiredHeight));
    }

    /// <summary>
    /// Load an image from URL with automatic fallback to a default image if loading fails.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="fallbackImageBytes">Default image bytes to use if loading fails.</param>
    /// <param name="fetchAlways">If we should always fetch the image from the URL.</param>
    /// <param name="desiredWidth">Optional desired width.</param>
    /// <param name="desiredHeight">Optional desired height.</param>
    /// <param name="absoluteExpiration">Optional expiration date.</param>
    /// <returns>The loaded image or the fallback image.</returns>
    public static IObservable<IBitmap> LoadImageFromUrlWithFallback(this IBlobCache blobCache, string url, byte[] fallbackImageBytes, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (fallbackImageBytes is null)
        {
            throw new ArgumentNullException(nameof(fallbackImageBytes));
        }

        return blobCache.LoadImageFromUrl(url, fetchAlways, desiredWidth, desiredHeight, absoluteExpiration)
            .Catch<IBitmap, Exception>(_ => BytesToImage(fallbackImageBytes, desiredWidth, desiredHeight));
    }

    /// <summary>
    /// Create a thumbnail version of an image and cache it separately.
    /// </summary>
    /// <param name="blobCache">The blob cache to store the thumbnail in.</param>
    /// <param name="sourceKey">The key of the source image.</param>
    /// <param name="thumbnailKey">The key to store the thumbnail under.</param>
    /// <param name="thumbnailWidth">The desired thumbnail width.</param>
    /// <param name="thumbnailHeight">The desired thumbnail height.</param>
    /// <param name="absoluteExpiration">Optional expiration date for the thumbnail.</param>
    /// <returns>An observable that completes when the thumbnail is created and cached.</returns>
    public static IObservable<Unit> CreateAndCacheThumbnail(this IBlobCache blobCache, string sourceKey, string thumbnailKey, float thumbnailWidth, float thumbnailHeight, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.LoadImage(sourceKey, thumbnailWidth, thumbnailHeight)
            .SelectMany(thumbnail => blobCache.SaveImage(thumbnailKey, thumbnail, absoluteExpiration));
    }

    /// <summary>
    /// Get the size information of a cached image without fully loading it.
    /// </summary>
    /// <param name="blobCache">The blob cache containing the image.</param>
    /// <param name="key">The key of the image.</param>
    /// <returns>An observable containing the image size information.</returns>
    public static IObservable<Size> GetImageSize(this IBlobCache blobCache, string key)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.Get(key)
            .SelectMany(bytes => bytes != null ? BitmapImageExtensions.ThrowOnBadImageBuffer(bytes) : Observable.Throw<byte[]>(new InvalidOperationException("Image data is null")))
            .SelectMany(bytes =>
            {
                using var ms = new MemoryStream(bytes);
                return Observable.FromAsync(async () =>
                {
                    var bitmap = await BitmapLoader.Current.Load(ms, null, null);
                    return bitmap != null ? new Size(bitmap.Width, bitmap.Height) : throw new InvalidOperationException("Failed to load image for size detection");
                });
            });
    }

    /// <summary>
    /// Clear all cached images that match a specific pattern.
    /// </summary>
    /// <param name="blobCache">The blob cache to clear images from.</param>
    /// <param name="keyPattern">A function to determine if a key should be invalidated.</param>
    /// <returns>An observable that completes when all matching images are cleared.</returns>
    public static IObservable<Unit> ClearImageCache(this IBlobCache blobCache, Func<string, bool> keyPattern)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (keyPattern is null)
        {
            throw new ArgumentNullException(nameof(keyPattern));
        }

        return blobCache.GetAllKeys()
            .Where(keyPattern)
            .SelectMany(key => blobCache.Invalidate(key))
            .DefaultIfEmpty(Unit.Default)
            .TakeLast(1);
    }

    private static IObservable<IBitmap> BytesToImage(byte[] compressedImage, float? desiredWidth, float? desiredHeight)
    {
        using var ms = new MemoryStream(compressedImage);
        return Observable.FromAsync(async () =>
        {
            var bitmap = await BitmapLoader.Current.Load(ms, desiredWidth, desiredHeight);
            return bitmap ?? throw new IOException("Failed to load the bitmap!");
        });
    }
}
