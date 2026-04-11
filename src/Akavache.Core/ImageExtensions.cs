// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Helpers;

namespace Akavache;

/// <summary>
/// Extension methods for working with images and bitmaps in the cache.
/// </summary>
public static class ImageExtensions
{
    /// <summary>
    /// Loads image data from the blob cache as raw bytes.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from.</param>
    /// <param name="key">The cache key to look up.</param>
    /// <returns>An observable that emits the image bytes.</returns>
    public static IObservable<byte[]> LoadImageBytes(this IBlobCache blobCache, string key)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        return blobCache.Get(key)
            .SelectMany(static bytes => ThrowOnNullOrBadImageBuffer(bytes));
    }

    /// <summary>
    /// Downloads an image from a remote URL and returns the image bytes.
    /// This method combines DownloadUrl and LoadImageBytes functionality,
    /// using cached values when possible.
    /// </summary>
    /// <param name="blobCache">The blob cache to store and retrieve the image data.</param>
    /// <param name="url">The URL to download the image from.</param>
    /// <param name="fetchAlways">A value indicating whether to always fetch the image from the URL, bypassing the cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date for the cached image data.</param>
    /// <returns>An observable that emits the image bytes.</returns>
    public static IObservable<byte[]> LoadImageBytesFromUrl(this IBlobCache blobCache, string url, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        ArgumentExceptionHelper.ThrowIfNull(url);

        return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(static bytes => ThrowOnBadImageBuffer(bytes));
    }

    /// <summary>
    /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
    /// image from a remote URL (using the cached value if possible) and
    /// returns the image bytes.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from if available.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the image bytes.</returns>
    public static IObservable<byte[]> LoadImageBytesFromUrl(this IBlobCache blobCache, Uri url, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        ArgumentExceptionHelper.ThrowIfNull(url);

        return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(static bytes => ThrowOnBadImageBuffer(bytes));
    }

    /// <summary>
    /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
    /// image from a remote URL (using the cached value if possible) and
    /// returns the image bytes.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from if available.</param>
    /// <param name="key">The key to store with.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the image bytes.</returns>
    public static IObservable<byte[]> LoadImageBytesFromUrl(this IBlobCache blobCache, string key, string url, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        ArgumentExceptionHelper.ThrowIfNull(url);

        return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(static bytes => ThrowOnBadImageBuffer(bytes));
    }

    /// <summary>
    /// A combination of DownloadUrl and LoadImageBytes, this method fetches an
    /// image from a remote URL (using the cached value if possible) and
    /// returns the image bytes.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from if available.</param>
    /// <param name="key">The key to store with.</param>
    /// <param name="url">The URL to download.</param>
    /// <param name="fetchAlways">If we should always fetch the image from the URL even if we have one in the blob.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the image bytes.</returns>
    public static IObservable<byte[]> LoadImageBytesFromUrl(this IBlobCache blobCache, string key, Uri url, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);

        ArgumentExceptionHelper.ThrowIfNull(url);

        return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(static bytes => ThrowOnBadImageBuffer(bytes));
    }

    /// <summary>
    /// Validates that the provided bytes represent a valid image format by checking file headers.
    /// </summary>
    /// <param name="imageBytes">The image bytes to validate.</param>
    /// <returns><c>true</c> if the bytes appear to be a valid image format; otherwise, <c>false</c>.</returns>
    public static bool IsValidImageFormat(this byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length < 4)
        {
            return false;
        }

        // Check for common image format headers
        // PNG: 89 50 4E 47
        if (imageBytes.Length >= 4 && imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
        {
            return true;
        }

        // JPEG: FF D8 FF
        if (imageBytes.Length >= 3 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF)
        {
            return true;
        }

        // GIF: 47 49 46
        if (imageBytes.Length >= 3 && imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
        {
            return true;
        }

        // BMP: 42 4D
        if (imageBytes.Length >= 2 && imageBytes[0] == 0x42 && imageBytes[1] == 0x4D)
        {
            return true;
        }

        // WebP: 52 49 46 46 ... 57 45 42 50
        if (imageBytes.Length >= 12 &&
            imageBytes[0] == 0x52 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x46 &&
            imageBytes[8] == 0x57 && imageBytes[9] == 0x45 && imageBytes[10] == 0x42 && imageBytes[11] == 0x50)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Emits <paramref name="compressedImage"/> through an observable, or signals
    /// an <see cref="InvalidOperationException"/> when the buffer is corrupt
    /// (<see langword="null"/> or smaller than the 64-byte minimum).
    /// </summary>
    /// <param name="compressedImage">The compressed image buffer to validate.</param>
    /// <returns>An observable that emits the byte array if valid, or signals an error if the buffer is corrupt.</returns>
    internal static IObservable<byte[]> ThrowOnBadImageBuffer(byte[]? compressedImage) =>
        compressedImage is null || compressedImage.Length < 64 ?
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
}
