// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Linq;
using Akavache.Core;

namespace Akavache.Tests;

/// <summary>
/// Skeleton tests for HttpService error handling.
/// </summary>
[Category("HTTP")]
public class HttpServiceErrorHandlingTests
{
    /// <summary>
    /// Ensures DownloadUrl surfaces failure via observable error channel.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task HttpExtensions_FetchUrl_HandlesFailure()
    {
        var service = new FakeHttpService();
        var serializer = new Akavache.SystemTextJson.SystemJsonSerializer();
        using var cache = new InMemoryBlobCache(serializer);
        Exception? captured = null;
        service.DownloadUrl(cache, "http://invalid").Subscribe(_ => { }, ex => captured = ex);
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured).IsTypeOf<HttpRequestException>();
    }

    /// <summary>
    /// Fake implementation throwing for all calls.
    /// </summary>
    private sealed class FakeHttpService : IHttpService
    {
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) => Observable.Throw<byte[]>(new HttpRequestException("Simulated failure"));

        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) => Observable.Throw<byte[]>(new HttpRequestException("Simulated failure"));

        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) => Observable.Throw<byte[]>(new HttpRequestException("Simulated failure"));

        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, HttpMethod? method = null, IEnumerable<KeyValuePair<string, string>>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null) => Observable.Throw<byte[]>(new HttpRequestException("Simulated failure"));
    }
}
