// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET462_OR_GREATER
using System.Net.Http;
#endif

namespace Akavache;

/// <summary>
/// Provides extension methods for downloading data from URLs with expiration based on relative time intervals from the current time.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2234:Pass System.Uri objects instead of strings",
    Justification = "The string overloads are long-standing public API. Each is paired with a Uri overload it forwards to, so the Uri form is always available.")]
public static class RelativeTimeDownloadExtensions
{
    /// <summary>Extension members for <c>IBlobCache</c>.</summary>
    /// <param name="blobCache">The blob cache to store the downloaded data.</param>
    extension(IBlobCache blobCache)
    {
        /// <summary>Downloads data from the specified URL if it is not already in the cache, with expiration based on a relative time span.</summary>
        /// <param name="url">The URL to download if not already in the cache.</param>
        /// <param name="httpMethod">The HTTP method to use for the request.</param>
        /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
        /// <returns>An observable that emits the downloaded data when available.</returns>
        public IObservable<byte[]> DownloadUrl(string url, HttpMethod httpMethod, TimeSpan expiration) =>
            blobCache.DownloadUrl(url, httpMethod, expiration, (IEnumerable<KeyValuePair<string, string>>?)null, false);

        /// <summary>Downloads data from the specified URL if it is not already in the cache, with expiration based on a relative time span.</summary>
        /// <param name="url">The URL to download if not already in the cache.</param>
        /// <param name="httpMethod">The HTTP method to use for the request.</param>
        /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
        /// <param name="headers">Optional HTTP headers to include in the request.</param>
        /// <returns>An observable that emits the downloaded data when available.</returns>
        public IObservable<byte[]> DownloadUrl(string url, HttpMethod httpMethod, TimeSpan expiration, IEnumerable<KeyValuePair<string, string>>? headers) =>
            blobCache.DownloadUrl(url, httpMethod, expiration, headers, false);

        /// <summary>Downloads data from the specified URL if it is not already in the cache, with expiration based on a relative time span.</summary>
        /// <param name="url">The URL to download if not already in the cache.</param>
        /// <param name="httpMethod">The HTTP method to use for the request.</param>
        /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
        /// <param name="headers">Optional HTTP headers to include in the request.</param>
        /// <param name="fetchAlways">A value indicating whether to always fetch from the web, bypassing the cache.</param>
        /// <returns>An observable that emits the downloaded data when available.</returns>
        public IObservable<byte[]> DownloadUrl(string url, HttpMethod httpMethod, TimeSpan expiration, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.DownloadUrl(url, httpMethod, headers, fetchAlways, blobCache.Scheduler.Now + expiration);
        }

        /// <summary>Downloads data from the specified URL if it is not already in the cache, with expiration based on a relative time span.</summary>
        /// <param name="url">The URL to download if not already in the cache.</param>
        /// <param name="httpMethod">The HTTP method to use for the request.</param>
        /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
        /// <returns>An observable that emits the downloaded data when available.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url, HttpMethod httpMethod, TimeSpan expiration) =>
            blobCache.DownloadUrl(url, httpMethod, expiration, (IEnumerable<KeyValuePair<string, string>>?)null, false);

        /// <summary>Downloads data from the specified URL if it is not already in the cache, with expiration based on a relative time span.</summary>
        /// <param name="url">The URL to download if not already in the cache.</param>
        /// <param name="httpMethod">The HTTP method to use for the request.</param>
        /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
        /// <param name="headers">Optional HTTP headers to include in the request.</param>
        /// <returns>An observable that emits the downloaded data when available.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url, HttpMethod httpMethod, TimeSpan expiration, IEnumerable<KeyValuePair<string, string>>? headers) =>
            blobCache.DownloadUrl(url, httpMethod, expiration, headers, false);

        /// <summary>Downloads data from the specified URL if it is not already in the cache, with expiration based on a relative time span.</summary>
        /// <param name="url">The URL to download if not already in the cache.</param>
        /// <param name="httpMethod">The HTTP method to use for the request.</param>
        /// <param name="expiration">A time span that will be added to the current time to determine expiration.</param>
        /// <param name="headers">Optional HTTP headers to include in the request.</param>
        /// <param name="fetchAlways">A value indicating whether to always fetch from the web, bypassing the cache.</param>
        /// <returns>An observable that emits the downloaded data when available.</returns>
        public IObservable<byte[]> DownloadUrl(Uri url, HttpMethod httpMethod, TimeSpan expiration, IEnumerable<KeyValuePair<string, string>>? headers, bool fetchAlways)
        {
            ArgumentExceptionHelper.ThrowIfNull(blobCache);
            return blobCache.DownloadUrl(url, httpMethod, headers, fetchAlways, blobCache.Scheduler.Now + expiration);
        }
    }
}
