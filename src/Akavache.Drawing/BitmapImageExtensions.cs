// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Helpers;

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
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        return blobCache.Get(key)
            .SelectMany(static bytes => ThrowOnNullOrBadImageBuffer(bytes))
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
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(static bytes => ThrowOnBadImageBuffer(bytes))
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
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(static bytes => ThrowOnBadImageBuffer(bytes))
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
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(static bytes => ThrowOnBadImageBuffer(bytes))
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
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(static bytes => ThrowOnBadImageBuffer(bytes))
            .SelectMany(x => BytesToImage(x, desiredWidth, desiredHeight));
    }

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
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        if (image is null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        return image.ImageToBytes()
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
#if NETSTANDARD2_0 || NET462_OR_GREATER
            using var stream = new MemoryStream();
#else
            await using var stream = new MemoryStream();
#endif
            await image.Save(CompressedBitmapFormat.Png, 1.0f, stream);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Emits <paramref name="compressedImage"/> through an observable, or signals
    /// an <see cref="InvalidOperationException"/> when the buffer is corrupt
    /// (<see langword="null"/> or smaller than the 64-byte minimum).
    /// </summary>
    /// <param name="compressedImage">The compressed image buffer to check.</param>
    /// <returns>An observable emitting the buffer, or signalling an error when invalid.</returns>
    internal static IObservable<byte[]> ThrowOnBadImageBuffer(byte[]? compressedImage) =>
        (compressedImage is null || compressedImage.Length < 64) ?
            Observable.Throw<byte[]>(new InvalidOperationException("Invalid Image")) :
            Observable.Return(compressedImage);

    /// <summary>
    /// Routes a potentially null byte buffer from a blob cache through the
    /// bad-image guard, emitting a descriptive <c>"Image data is null"</c> error
    /// when the buffer itself is <see langword="null"/>.
    /// </summary>
    /// <param name="bytes">The bytes returned by the blob cache, possibly <see langword="null"/>.</param>
    /// <returns>An observable emitting <paramref name="bytes"/>, or an error.</returns>
    internal static IObservable<byte[]> ThrowOnNullOrBadImageBuffer(byte[]? bytes) =>
        bytes is null
            ? Observable.Throw<byte[]>(new InvalidOperationException("Image data is null"))
            : ThrowOnBadImageBuffer(bytes);

    /// <summary>
    /// Converts a compressed image byte array into an <see cref="IBitmap"/> using
    /// Splat's ambient <see cref="BitmapLoader.Current"/>.
    /// </summary>
    /// <remarks>
    /// Throws <see cref="IOException"/> when the loader returns
    /// <see langword="null"/>.
    /// </remarks>
    /// <param name="compressedImage">The compressed image bytes.</param>
    /// <param name="desiredWidth">Optional desired width.</param>
    /// <param name="desiredHeight">Optional desired height.</param>
    /// <returns>An observable emitting the decoded bitmap.</returns>
    internal static IObservable<IBitmap> BytesToImage(byte[] compressedImage, float? desiredWidth, float? desiredHeight) =>
        Observable.FromAsync(async () =>
        {
#if NETSTANDARD2_0 || NET462_OR_GREATER
            using var ms = new MemoryStream(compressedImage);
#else
            await using var ms = new MemoryStream(compressedImage);
#endif
            var bitmap = await BitmapLoader.Current.Load(ms, desiredWidth, desiredHeight);
            return bitmap ?? throw new IOException("Failed to load the bitmap!");
        });
}
