// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Integration.Tests;

/// <summary>
/// Tests for the <c>LoadImageBytesFromUrl</c> overloads that state a fetch flag and leave the
/// expiration to the extension. Each test pins the request the overload produces, the effect of the
/// fetch flag it passed on, and the fact that the image it stores carries no expiration.
/// </summary>
[Category("Akavache")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2234:Pass System.Uri objects instead of strings",
    Justification = "These tests exist to exercise the string-URL overloads of the public Akavache API. "
        + "Each is paired with a Uri twin, so calling the Uri overload here would delete the only "
        + "coverage the string overloads have.")]
public class LoadImageBytesFromUrlTests
{
    /// <summary>An absolute URL whose <see cref="Uri"/> form round-trips to the same text, so the cache key it produces is predictable.</summary>
    private const string SampleUrl = "http://localhost/image";

    /// <summary>The cache key supplied to the overloads that take one.</summary>
    private const string ExplicitKey = "explicit-image-key";

    /// <summary>The <see cref="Uri"/> form of <see cref="SampleUrl"/>.</summary>
    private static readonly Uri SampleUri = new(SampleUrl);

    /// <summary>An image load told to always fetch bypasses the cached image on every call and stores the fresh one without an expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task LoadImageBytesFromUrlStringWithFetchAlwaysShouldRefetchOnEveryCall() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, _) => cache.LoadImageBytesFromUrl(SampleUrl, true),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);

    /// <summary>A URI image load told to always fetch bypasses the cached image on every call and stores the fresh one without an expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task LoadImageBytesFromUrlUriWithFetchAlwaysShouldRefetchOnEveryCall() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, _) => cache.LoadImageBytesFromUrl(SampleUri, true),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);

    /// <summary>A keyed image load told to always fetch bypasses the cached image on every call and stores the fresh one under the caller's key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task LoadImageBytesFromUrlKeyStringWithFetchAlwaysShouldRefetchOnEveryCall() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, _) => cache.LoadImageBytesFromUrl(ExplicitKey, SampleUrl, true),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);

    /// <summary>A keyed URI image load told to always fetch bypasses the cached image on every call and stores the fresh one under the caller's key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task LoadImageBytesFromUrlKeyUriWithFetchAlwaysShouldRefetchOnEveryCall() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, _) => cache.LoadImageBytesFromUrl(ExplicitKey, SampleUri, true),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);
}
