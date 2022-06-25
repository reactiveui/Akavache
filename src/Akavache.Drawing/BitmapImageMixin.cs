// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;

using Splat;

namespace Akavache;

/// <summary>
/// Provides extension methods associated with the <see cref="IBitmap" /> interface.
/// </summary>
public static class BitmapImageMixin
{
    /// <summary>
    /// Load an image from the blob cache.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from.</param>
    /// <param name="key">The key to look up in the cache.</param>
    /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
    /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
    /// <returns>A Future result representing the bitmap image. blobCache
    /// Observable is guaranteed to be returned on the UI thread.</returns>
    public static IObservable<IBitmap> LoadImage(this IBlobCache blobCache, string key, float? desiredWidth = null, float? desiredHeight = null) => blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.Get(key)
            .SelectMany(ThrowOnBadImageBuffer)
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));

    /// <summary>
    /// A combination of DownloadUrl and LoadImage, this method fetches an
    /// image from a remote URL (using the cached value if possible) and
    /// returns the image.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from if available.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
    /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
    /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the bitmap image. blobCache
    /// Observable is guaranteed to be returned on the UI thread.</returns>
    public static IObservable<IBitmap> LoadImageFromUrl(this IBlobCache blobCache, string url, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null) => blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.DownloadUrl(url, null, fetchAlways, absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer)
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));

    /// <summary>
    /// A combination of DownloadUrl and LoadImage, this method fetches an
    /// image from a remote URL (using the cached value if possible) and
    /// returns the image.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from if available.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
    /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
    /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the bitmap image. blobCache
    /// Observable is guaranteed to be returned on the UI thread.</returns>
    public static IObservable<IBitmap> LoadImageFromUrl(this IBlobCache blobCache, Uri url, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null) => blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.DownloadUrl(url, null, fetchAlways, absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer)
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));

    /// <summary>
    /// A combination of DownloadUrl and LoadImage, this method fetches an
    /// image from a remote URL (using the cached value if possible) and
    /// returns the image.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from if available.</param>
    /// <param name="key">The key to store with.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
    /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
    /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the bitmap image. blobCache
    /// Observable is guaranteed to be returned on the UI thread.</returns>
    public static IObservable<IBitmap> LoadImageFromUrl(this IBlobCache blobCache, string key, string url, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null) => blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.DownloadUrl(key, url, null, fetchAlways, absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer)
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));

    /// <summary>
    /// A combination of DownloadUrl and LoadImage, this method fetches an
    /// image from a remote URL (using the cached value if possible) and
    /// returns the image.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from if available.</param>
    /// <param name="key">The key to store with.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
    /// <param name="desiredWidth">Optional desired width, if not specified will be the default size.</param>
    /// <param name="desiredHeight">Optional desired height, if not specified will be the default size.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the bitmap image. blobCache
    /// Observable is guaranteed to be returned on the UI thread.</returns>
    public static IObservable<IBitmap> LoadImageFromUrl(this IBlobCache blobCache, string key, Uri url, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null) => blobCache is null
            ? throw new ArgumentNullException(nameof(blobCache))
            : blobCache.DownloadUrl(key, url, null, fetchAlways, absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer)
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));

    /// <summary>
    /// Converts bad image buffers into an exception.
    /// </summary>
    /// <param name="compressedImage">The compressed image buffer to check.</param>
    /// <returns>The byte[], or OnError if the buffer is corrupt (empty or
    /// too small).</returns>
    public static IObservable<byte[]> ThrowOnBadImageBuffer(byte[] compressedImage) =>
        (compressedImage is null || compressedImage.Length < 64) ?
            Observable.Throw<byte[]>(new InvalidOperationException("Invalid Image")) :
            Observable.Return(compressedImage);

    private static IObservable<IBitmap> BytesToImage(byte[] compressedImage, float? desiredWidth, float? desiredHeight)
    {
        using var ms = new MemoryStream(compressedImage);
        return BitmapLoader.Current.Load(ms, desiredWidth, desiredHeight).ToObservable()
            .SelectMany(bitmap => bitmap is not null ?
                Observable.Return(bitmap) :
                Observable.Throw<IBitmap>(new IOException("Failed to load the bitmap!")));
    }
}
