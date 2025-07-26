﻿// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace ReactiveMarbles.CacheDatabase.Core;

/// <summary>
/// A interface that represents a service for providing HTTP functionality.
/// </summary>
public interface IHttpService
{
    /// <summary>
    /// Gets a observable for a download.
    /// </summary>
    /// <param name="blobCache">The blob cache where to get the value from if available.</param>
    /// <param name="url">The url where to get the resource if not available in the cache.</param>
    /// <param name="method">The http method.</param>
    /// <param name="headers">The headers to use in the HTTP action.</param>
    /// <param name="fetchAlways">If we should just fetch and not bother checking the cache first.</param>
    /// <param name="absoluteExpiration">A optional expiration date time.</param>
    /// <returns>A observable that signals when there is byte data.</returns>
    IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Gets a observable for a download.
    /// </summary>
    /// <param name="blobCache">The blob cache where to get the value from if available.</param>
    /// <param name="url">The url where to get the resource if not available in the cache.</param>
    /// <param name="method">The http method.</param>
    /// <param name="headers">The headers to use in the HTTP action.</param>
    /// <param name="fetchAlways">If we should just fetch and not bother checking the cache first.</param>
    /// <param name="absoluteExpiration">A optional expiration date time.</param>
    /// <returns>A observable that signals when there is byte data.</returns>
    IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Gets a observable for a download.
    /// </summary>
    /// <param name="blobCache">The blob cache where to get the value from if available.</param>
    /// <param name="key">The key to use for the download cache entry.</param>
    /// <param name="url">The url where to get the resource if not available in the cache.</param>
    /// <param name="method">The http method.</param>
    /// <param name="headers">The headers to use in the HTTP action.</param>
    /// <param name="fetchAlways">If we should just fetch and not bother checking the cache first.</param>
    /// <param name="absoluteExpiration">A optional expiration date time.</param>
    /// <returns>A observable that signals when there is byte data.</returns>
    IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null);

    /// <summary>
    /// Gets a observable for a download.
    /// </summary>
    /// <param name="blobCache">The blob cache where to get the value from if available.</param>
    /// <param name="key">The key to use for the download cache entry.</param>
    /// <param name="url">The url where to get the resource if not available in the cache.</param>
    /// <param name="method">The http method.</param>
    /// <param name="headers">The headers to use in the HTTP action.</param>
    /// <param name="fetchAlways">If we should just fetch and not bother checking the cache first.</param>
    /// <param name="absoluteExpiration">A optional expiration date time.</param>
    /// <returns>A observable that signals when there is byte data.</returns>
    IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null);
}
