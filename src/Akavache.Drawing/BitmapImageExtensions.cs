// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using Splat;

namespace Akavache.Drawing;

/// <summary>
/// Provides extension methods associated with the <see cref="IBitmap" /> interface.
/// </summary>
public static class BitmapImageExtensions
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
    public static IObservable<IBitmap> LoadImage(this IBlobCache blobCache, string key, float? desiredWidth = null, float? desiredHeight = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.Get(key)
            .SelectMany(bytes => bytes != null ? ThrowOnBadImageBuffer(bytes) : Observable.Throw<byte[]>(new InvalidOperationException("Image data is null")))
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));
    }

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
    public static IObservable<IBitmap> LoadImageFromUrl(this IBlobCache blobCache, string url, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer)
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));
    }

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
    public static IObservable<IBitmap> LoadImageFromUrl(this IBlobCache blobCache, Uri url, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer)
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));
    }

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
    public static IObservable<IBitmap> LoadImageFromUrl(this IBlobCache blobCache, string key, string url, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer)
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));
    }

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
    public static IObservable<IBitmap> LoadImageFromUrl(this IBlobCache blobCache, string key, Uri url, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer)
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));
    }

    /// <summary>
    /// Converts bad image buffers into an exception.
    /// </summary>
    /// <param name="compressedImage">The compressed image buffer to check.</param>
    /// <returns>The byte[], or OnError if the buffer is corrupt (empty or
    /// too small).</returns>
    public static IObservable<byte[]> ThrowOnBadImageBuffer(this byte[] compressedImage) =>
        (compressedImage is null || compressedImage.Length < 64) ?
            Observable.Throw<byte[]>(new InvalidOperationException("Invalid Image")) :
            Observable.Return(compressedImage);

    /// <summary>
    /// Save an image to the blob cache.
    /// </summary>
    /// <param name="blobCache">The blob cache to save the image to.</param>
    /// <param name="key">The key to associate with the image.</param>
    /// <param name="image">The bitmap image to save.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the save operation.</returns>
    public static IObservable<Unit> SaveImage(this IBlobCache blobCache, string key, IBitmap image, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (image is null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        return ImageToBytes(image)
            .SelectMany(bytes => blobCache.Insert(key, bytes, absoluteExpiration));
    }

    /// <summary>
    /// Convert an IBitmap to a byte array asynchronously.
    /// </summary>
    /// <param name="image">The bitmap image to convert.</param>
    /// <returns>A Future result representing the byte array.</returns>
    public static IObservable<byte[]> ImageToBytes(this IBitmap image)
    {
        if (image is null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        return Observable.FromAsync(async () =>
        {
#if NETSTANDARD2_0
            using var stream = new MemoryStream();
#else
            await using var stream = new MemoryStream();
#endif
            await image.Save(CompressedBitmapFormat.Png, 1.0f, stream);
            return stream.ToArray();
        });
    }

    private static IObservable<IBitmap> BytesToImage(byte[] compressedImage, float? desiredWidth, float? desiredHeight) =>
        Observable.FromAsync(async () =>
        {
#if NETSTANDARD2_0
            using var ms = new MemoryStream(compressedImage);
#else
            await using var ms = new MemoryStream(compressedImage);
#endif
            var bitmap = await BitmapLoader.Current.Load(ms, desiredWidth, desiredHeight);
            return bitmap ?? throw new IOException("Failed to load the bitmap!");
        });
}
