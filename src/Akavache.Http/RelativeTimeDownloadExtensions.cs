// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET462_OR_GREATER
using System.Net.Http;
#endif

using Akavache.Helpers;

namespace Akavache;

/// <summary>
/// Relative-time convenience overloads for <see cref="HttpExtensions.DownloadUrl"/>.
/// </summary>
public static class RelativeTimeDownloadExtensions
{
    /// <summary>
    /// Downloads data from the specified URL if it is not already in the cache, with expiration based on a relative time span.
    /// </summary>
    /// <param name="blobCache">The blob cache to store the downloaded data.</param>
    /// <param name="url">The URL to download if not already in the cache.</param>
    /// <param name="httpMethod">The HTTP method to use for the request.</param>
    /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="fetchAlways">A value indicating whether to always fetch from the web, bypassing the cache.</param>
    /// <returns>An observable that emits the downloaded data when available.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, string url, HttpMethod httpMethod, TimeSpan expiration, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);
        return blobCache.DownloadUrl(url, httpMethod, headers, fetchAlways, blobCache.Scheduler.Now + expiration);
    }

    /// <summary>
    /// Downloads data from the specified URL if it is not already in the cache, with expiration based on a relative time span.
    /// </summary>
    /// <param name="blobCache">The blob cache to store the downloaded data.</param>
    /// <param name="url">The URL to download if not already in the cache.</param>
    /// <param name="httpMethod">The HTTP method to use for the request.</param>
    /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
    /// <param name="headers">Optional HTTP headers to include in the request.</param>
    /// <param name="fetchAlways">A value indicating whether to always fetch from the web, bypassing the cache.</param>
    /// <returns>An observable that emits the downloaded data when available.</returns>
    public static IObservable<byte[]> DownloadUrl(this IBlobCache blobCache, Uri url, HttpMethod httpMethod, TimeSpan expiration, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false)
    {
        ArgumentExceptionHelper.ThrowIfNull(blobCache);
        return blobCache.DownloadUrl(url, httpMethod, headers, fetchAlways, blobCache.Scheduler.Now + expiration);
    }
}
