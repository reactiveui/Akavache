// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Tests;

namespace Akavache.Integration.Tests;

/// <summary>
/// Tests for the <see cref="HttpService"/> download overloads that omit arguments and let the service
/// fill them in. Each test pins the request the overload produces and the effect of the value it
/// supplied for the argument the caller left out.
/// </summary>
[Category("Akavache")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2234:Pass System.Uri objects instead of strings",
    Justification = "These tests exist to exercise the string-URL overloads of the public Akavache API. "
        + "Each is paired with a Uri twin, so calling the Uri overload here would delete the only "
        + "coverage the string overloads have.")]
public class HttpServiceDownloadUrlDefaultsTests
{
    /// <summary>An absolute URL whose <see cref="Uri"/> form round-trips to the same text, so the cache key it produces is predictable.</summary>
    private const string SampleUrl = "http://localhost/data";

    /// <summary>The cache key supplied to the overloads that take one.</summary>
    private const string ExplicitKey = "explicit-key";

    /// <summary>A request body supplied to the <c>MakeWebRequest</c> overloads that take one.</summary>
    private const string RequestBody = "request-body";

    /// <summary>A retry count a caller states explicitly, to show it survives the forward untouched.</summary>
    private const int StatedRetryCount = 5;

    /// <summary>The <see cref="Uri"/> form of <see cref="SampleUrl"/>.</summary>
    private static readonly Uri SampleUri = new(SampleUrl);

    /// <summary>Headers a caller supplies, to show the caller's own collection is what reaches the request.</summary>
    private static readonly KeyValuePair<string, string>[] SampleHeaders = [new("X-Akavache-Test", "forwarded")];

    /// <summary>A method-only download sends no headers, and the cache serves the repeat because it did not force a fetch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlStringWithMethodShouldSendNoHeadersAndServeRepeatFromCache() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, service) => service.DownloadUrl(cache, SampleUrl, HttpMethod.Post),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: null);

    /// <summary>A download given headers sends them, and the cache serves the repeat because it did not force a fetch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlStringWithHeadersShouldSendThemAndServeRepeatFromCache() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, service) => service.DownloadUrl(cache, SampleUrl, HttpMethod.Post, SampleHeaders),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A download told to always fetch bypasses the cached entry on every call and stores the fresh response without an expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlStringWithFetchAlwaysShouldRefetchOnEveryCall() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, service) => service.DownloadUrl(cache, SampleUrl, HttpMethod.Put, SampleHeaders, true),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Put,
            expectedHeaders: SampleHeaders);

    /// <summary>A download that states only the fetch-always flag falls back to GET and sends no headers.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlStringWithOnlyFetchAlwaysShouldRefetchUsingGet() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, service) => service.DownloadUrl(cache, SampleUrl, true),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);

    /// <summary>A method-only URI download sends no headers, and the cache serves the repeat because it did not force a fetch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithMethodShouldSendNoHeadersAndServeRepeatFromCache() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, service) => service.DownloadUrl(cache, SampleUri, HttpMethod.Post),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: null);

    /// <summary>A URI download given headers sends them, and the cache serves the repeat because it did not force a fetch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithHeadersShouldSendThemAndServeRepeatFromCache() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, service) => service.DownloadUrl(cache, SampleUri, HttpMethod.Post, SampleHeaders),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A URI download told to always fetch bypasses the cached entry on every call and stores the fresh response without an expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithFetchAlwaysShouldRefetchOnEveryCall() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, service) => service.DownloadUrl(cache, SampleUri, HttpMethod.Put, SampleHeaders, true),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Put,
            expectedHeaders: SampleHeaders);

    /// <summary>A URI download that states only the fetch-always flag falls back to GET and sends no headers.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlUriWithOnlyFetchAlwaysShouldRefetchUsingGet() =>
        DownloadOverloadAssertions.AssertEveryCallIssuesRequest(
            static (cache, service) => service.DownloadUrl(cache, SampleUri, true),
            expectedKey: SampleUrl,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Get,
            expectedHeaders: null);

    /// <summary>A keyed download given headers sends them and stores the response under the caller's key, which then serves the repeat.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyStringWithHeadersShouldSendThemAndCacheUnderTheKey() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, service) => service.DownloadUrl(cache, ExplicitKey, SampleUrl, HttpMethod.Post, SampleHeaders),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A keyed URI download with only a method sends no headers and stores the response under the caller's key, which then serves the repeat.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyUriWithMethodShouldSendNoHeadersAndCacheUnderTheKey() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, service) => service.DownloadUrl(cache, ExplicitKey, SampleUri, HttpMethod.Post),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: null);

    /// <summary>A keyed URI download given headers sends them and stores the response under the caller's key, which then serves the repeat.</summary>
    /// <returns>A task.</returns>
    [Test]
    public Task DownloadUrlKeyUriWithHeadersShouldSendThemAndCacheUnderTheKey() =>
        DownloadOverloadAssertions.AssertRepeatServedFromCache(
            static (cache, service) => service.DownloadUrl(cache, ExplicitKey, SampleUri, HttpMethod.Post, SampleHeaders),
            expectedKey: ExplicitKey,
            expectedUri: SampleUri,
            expectedMethod: HttpMethod.Post,
            expectedHeaders: SampleHeaders);

    /// <summary>A request made with only a URI and method carries no headers or body, and takes the service-wide retry count and timeout.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithUriAndMethodShouldUseTheServiceRetryCountAndTimeout()
    {
        using RecordingHttpService service = new();

        var response = service.MakeWebRequest(SampleUri, HttpMethod.Get).WaitForValue();

        await Assert.That(service.IssuedRequests.Count).IsEqualTo(1);
        var issued = service.IssuedRequests[0];
        using (Assert.Multiple())
        {
            await Assert.That(response).IsNotNull();
            await Assert.That(issued.Uri).IsEqualTo(SampleUri);
            await Assert.That(issued.Method).IsEqualTo(HttpMethod.Get);
            await Assert.That(issued.Headers).IsNull();
            await Assert.That(issued.Content).IsNull();
            await Assert.That(issued.Retries).IsEqualTo(HttpService.DefaultRetryCount);
            await Assert.That(issued.Timeout).IsNull();
        }
    }

    /// <summary>A request made with headers and a body carries both, and takes the service-wide retry count and timeout.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithContentShouldUseTheServiceRetryCountAndTimeout()
    {
        using RecordingHttpService service = new();

        var response = service.MakeWebRequest(SampleUri, HttpMethod.Post, SampleHeaders, RequestBody).WaitForValue();

        await Assert.That(service.IssuedRequests.Count).IsEqualTo(1);
        var issued = service.IssuedRequests[0];
        using (Assert.Multiple())
        {
            await Assert.That(response).IsNotNull();
            await Assert.That(issued.Method).IsEqualTo(HttpMethod.Post);
            await Assert.That(issued.Headers).IsSameReferenceAs(SampleHeaders);
            await Assert.That(issued.Content).IsEqualTo(RequestBody);
            await Assert.That(issued.Retries).IsEqualTo(HttpService.DefaultRetryCount);
            await Assert.That(issued.Timeout).IsNull();
        }
    }

    /// <summary>A request that states its own retry count keeps it, and still takes the service-wide timeout.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithRetriesShouldKeepThemAndUseTheServiceTimeout()
    {
        using RecordingHttpService service = new();

        var response = service
            .MakeWebRequest(SampleUri, HttpMethod.Put, SampleHeaders, RequestBody, StatedRetryCount)
            .WaitForValue();

        await Assert.That(service.IssuedRequests.Count).IsEqualTo(1);
        var issued = service.IssuedRequests[0];
        using (Assert.Multiple())
        {
            await Assert.That(response).IsNotNull();
            await Assert.That(issued.Method).IsEqualTo(HttpMethod.Put);
            await Assert.That(issued.Content).IsEqualTo(RequestBody);
            await Assert.That(issued.Retries).IsEqualTo(StatedRetryCount);
            await Assert.That(issued.Timeout).IsNull();
        }
    }
}
