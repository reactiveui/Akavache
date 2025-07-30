// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// Extension methods for working with images and bitmaps in the cache.
/// </summary>
public static class ImageExtensions
{
    /// <summary>
    /// Load an image from the blob cache as raw bytes.
    /// </summary>
    /// <param name="blobCache">The blob cache to load the image from.</param>
    /// <param name="key">The key to look up in the cache.</param>
    /// <returns>A Future result representing the image bytes.</returns>
    public static IObservable<byte[]> LoadImageBytes(this IBlobCache blobCache, string key)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.Get(key)
            .SelectMany(static bytes => bytes != null ?
                bytes.ThrowOnBadImageBuffer() :
                Observable.Throw<byte[]>(new InvalidOperationException("Image data is null")));
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
    public static IObservable<byte[]> LoadImageBytesFromUrl(this IBlobCache blobCache, string url, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer);
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
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.DownloadUrl(url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer);
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
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer);
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
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.DownloadUrl(key, url, fetchAlways: fetchAlways, absoluteExpiration: absoluteExpiration)
            .SelectMany(ThrowOnBadImageBuffer);
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
    /// Validates that the provided bytes represent a valid image format.
    /// </summary>
    /// <param name="imageBytes">The image bytes to validate.</param>
    /// <returns>True if the bytes appear to be a valid image format.</returns>
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
}
