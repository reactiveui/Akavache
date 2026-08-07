// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for the <c>DownloadUrl</c> cache extensions that omit arguments and let the extension fill
/// them in. Each test pins the request the overload produces and the effect of the value it supplied
/// for the argument the caller left out.
/// </summary>
[Category("Akavache")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2234:Pass System.Uri objects instead of strings",
    Justification = "These tests exist to exercise the string-URL overloads of the public Akavache API. "
        + "Each is paired with a Uri twin, so calling the Uri overload here would delete the only "
        + "coverage the string overloads have.")]
public class HttpExtensionsDownloadUrlDefaultsTests
{
    /// <summary>An absolute URL whose <see cref="Uri"/> form round-trips to the same text, so the cache key it produces is predictable.</summary>
    private const string SampleUrl = "http://localhost/data";

    /// <summary>The cache key supplied to the overloads that take one.</summary>
    private const string ExplicitKey = "explicit-key";

    /// <summary>How far in the past the expiration-taking overloads are pointed, so the stored entry lapses at once.</summary>
    private const int LapsedMinutes = 1;

    /// <summary>The <see cref="Uri"/> form of <see cref="SampleUrl"/>.</summary>
    private static readonly Uri SampleUri = new(SampleUrl);

    /// <summary>Headers a caller supplies, to show the caller's own collection is what reaches the request.</summary>
    private static readonly KeyValuePair<string, string>[] SampleHeaders = [new("X-Akavache-Test", "forwarded")];

    /// <summary>A method-only download sends no headers, and the cache serves the repeat because it did not force a fetch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlStringWithMethodShouldSendNoHeadersAndServeRepeatFromCache() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, _) => cache.DownloadUrl(SampleUrl, HttpMethod.Post),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: null);

    /// <summary>A download given headers sends them, and the cache serves the repeat because it did not force a fetch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlStringWithHeadersShouldSendThemAndServeRepeatFromCache() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, _) => cache.DownloadUrl(SampleUrl, HttpMethod.Post, SampleHeaders),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A download told to always fetch bypasses the cached entry on every call and stores the fresh response without an expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlStringWithFetchAlwaysShouldRefetchOnEveryCall() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, _) => cache.DownloadUrl(SampleUrl, HttpMethod.Put, SampleHeaders, true),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Put,
            expectedHeaders: SampleHeaders);

    /// <summary>A download that states only the fetch-always flag falls back to GET and sends no headers.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlStringWithOnlyFetchAlwaysShouldRefetchUsingGet() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, _) => cache.DownloadUrl(SampleUrl, true),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);

    /// <summary>A method-only URI download sends no headers, and the cache serves the repeat because it did not force a fetch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithMethodShouldSendNoHeadersAndServeRepeatFromCache() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, _) => cache.DownloadUrl(SampleUri, HttpMethod.Post),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: null);

    /// <summary>A URI download given headers sends them, and the cache serves the repeat because it did not force a fetch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithHeadersShouldSendThemAndServeRepeatFromCache() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, _) => cache.DownloadUrl(SampleUri, HttpMethod.Post, SampleHeaders),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A URI download told to always fetch bypasses the cached entry on every call and stores the fresh response without an expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithFetchAlwaysShouldRefetchOnEveryCall() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, _) => cache.DownloadUrl(SampleUri, HttpMethod.Put, SampleHeaders, true),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Put,
            expectedHeaders: SampleHeaders);

    /// <summary>A URI download that states only the fetch-always flag falls back to GET and sends no headers.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithOnlyFetchAlwaysShouldRefetchUsingGet() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, _) => cache.DownloadUrl(SampleUri, true),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);

    /// <summary>A keyed download with only a method sends no headers and stores the response under the caller's key, which then serves the repeat.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyStringWithMethodShouldSendNoHeadersAndCacheUnderTheKey() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, _) => cache.DownloadUrl(ExplicitKey, SampleUrl, HttpMethod.Post),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: null);

    /// <summary>A keyed download given headers sends them and stores the response under the caller's key, which then serves the repeat.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyStringWithHeadersShouldSendThemAndCacheUnderTheKey() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, _) => cache.DownloadUrl(ExplicitKey, SampleUrl, HttpMethod.Post, SampleHeaders),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A keyed download told to always fetch bypasses the cached entry on every call and stores the fresh response without an expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyStringWithFetchAlwaysShouldRefetchOnEveryCall() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, _) => cache.DownloadUrl(ExplicitKey, SampleUrl, HttpMethod.Put, SampleHeaders, true),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Put,
            expectedHeaders: SampleHeaders);

    /// <summary>A keyed URI download with only a method sends no headers and stores the response under the caller's key, which then serves the repeat.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyUriWithMethodShouldSendNoHeadersAndCacheUnderTheKey() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, _) => cache.DownloadUrl(ExplicitKey, SampleUri, HttpMethod.Post),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: null);

    /// <summary>A keyed URI download given headers sends them and stores the response under the caller's key, which then serves the repeat.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyUriWithHeadersShouldSendThemAndCacheUnderTheKey() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, _) => cache.DownloadUrl(ExplicitKey, SampleUri, HttpMethod.Post, SampleHeaders),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A keyed URI download told to always fetch bypasses the cached entry on every call and stores the fresh response without an expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyUriWithFetchAlwaysShouldRefetchOnEveryCall() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, _) => cache.DownloadUrl(ExplicitKey, SampleUri, HttpMethod.Put, SampleHeaders, true),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Put,
            expectedHeaders: SampleHeaders);

    /// <summary>A keyed download given only an expiration fetches with GET and stores the response with that expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyStringWithExpirationShouldFetchWithGetAndApplyTheExpiration() =>
        DownloadOverloadAssertions.AssertCachedEntryAlreadyExpired(
            static (cache, _) => cache.DownloadUrl(ExplicitKey, SampleUrl, LapsedMoment()),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);

    /// <summary>A keyed URI download given only an expiration fetches with GET and stores the response with that expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyUriWithExpirationShouldFetchWithGetAndApplyTheExpiration() =>
        DownloadOverloadAssertions.AssertCachedEntryAlreadyExpired(
            static (cache, _) => cache.DownloadUrl(ExplicitKey, SampleUri, LapsedMoment()),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);

    /// <summary>A URI download given only an expiration fetches with GET and stores the response under the URL with that expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithExpirationShouldFetchWithGetAndApplyTheExpiration() =>
        DownloadOverloadAssertions.AssertCachedEntryAlreadyExpired(
            static (cache, _) => cache.DownloadUrl(SampleUri, LapsedMoment()),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);

    /// <summary>Gets a moment that has already passed, so an entry stored with it is expired on arrival.</summary>
    /// <returns>An expiration in the past.</returns>
    private static DateTimeOffset LapsedMoment() => TimeProvider.System.GetUtcNow().AddMinutes(-LapsedMinutes);
}
