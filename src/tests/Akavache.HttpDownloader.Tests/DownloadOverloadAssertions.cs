// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Scenarios shared by the tests for the download overloads that exist only to forward to a fuller
/// overload. Each scenario drives one overload against a <see cref="RecordingHttpService"/>, so the
/// URI, method, headers, fetch-always flag and expiration the overload passes on are observable
/// rather than assumed.
/// </summary>
internal static class DownloadOverloadAssertions
{
    /// <summary>The number of requests a pair of calls issues when the cache is bypassed every time.</summary>
    private const int RequestsPerBypassedPair = 2;

    /// <summary>
    /// Runs a download twice and asserts the first call fetches while the second is answered from the
    /// cached entry — which holds only when the overload passed <c>fetchAlways: false</c> and stored
    /// the response without an expiration.
    /// </summary>
    /// <param name="download">Invokes the overload under test against the supplied cache and service.</param>
    /// <param name="expectedKey">The cache key the response is expected to be stored under.</param>
    /// <param name="expectedUri">The URI the fetch is expected to be made against.</param>
    /// <param name="expectedMethod">The HTTP method the fetch is expected to use.</param>
    /// <param name="expectedHeaders">The header collection the fetch is expected to carry, or <see langword="null"/>.</param>
    /// <returns>A task representing the assertion.</returns>
    internal static async Task AssertRepeatServedFromCache(
        Func<InMemoryBlobCache, RecordingHttpService, IObservable<byte[]>> download,
        string expectedKey,
        Uri expectedUri,
        HttpMethod expectedMethod,
        IEnumerable<KeyValuePair<string, string>>? expectedHeaders)
    {
        using RecordingHttpService service = new();
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        cache.SetHttpService(service);

        var downloaded = download(cache, service).WaitForValue();
        var issued = await AssertSingleIssuedRequest(service, expectedUri, expectedMethod, expectedHeaders);
        await Assert.That(downloaded).IsEquivalentTo(issued.ResponsePayload);

        var repeated = download(cache, service).WaitForValue();

        using (Assert.Multiple())
        {
            // The cache was consulted first, so the second call answers without issuing a request.
            await Assert.That(service.IssuedRequests.Count).IsEqualTo(1);
            await Assert.That(repeated).IsEquivalentTo(issued.ResponsePayload);

            // No expiration was applied, so the entry is still readable under the expected key.
            await Assert.That(cache.Get(expectedKey).WaitForValue()).IsEquivalentTo(issued.ResponsePayload);
        }
    }

    /// <summary>
    /// Runs a download twice and asserts both calls fetch — which holds only when the overload passed
    /// <c>fetchAlways: true</c> — and that the second response replaces the first under the same key.
    /// </summary>
    /// <param name="download">Invokes the overload under test against the supplied cache and service.</param>
    /// <param name="expectedKey">The cache key the response is expected to be stored under.</param>
    /// <param name="expectedUri">The URI each fetch is expected to be made against.</param>
    /// <param name="expectedMethod">The HTTP method each fetch is expected to use.</param>
    /// <param name="expectedHeaders">The header collection each fetch is expected to carry, or <see langword="null"/>.</param>
    /// <returns>A task representing the assertion.</returns>
    internal static async Task AssertEveryCallIssuesRequest(
        Func<InMemoryBlobCache, RecordingHttpService, IObservable<byte[]>> download,
        string expectedKey,
        Uri expectedUri,
        HttpMethod expectedMethod,
        IEnumerable<KeyValuePair<string, string>>? expectedHeaders)
    {
        using RecordingHttpService service = new();
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        cache.SetHttpService(service);

        var downloaded = download(cache, service).WaitForValue();
        var issued = await AssertSingleIssuedRequest(service, expectedUri, expectedMethod, expectedHeaders);
        await Assert.That(downloaded).IsEquivalentTo(issued.ResponsePayload);

        var repeated = download(cache, service).WaitForValue();

        using (Assert.Multiple())
        {
            // The entry cached by the first call was bypassed, so a second request went out.
            await Assert.That(service.IssuedRequests.Count).IsEqualTo(RequestsPerBypassedPair);

            var reissued = service.IssuedRequests[1];
            await Assert.That(reissued.Uri).IsEqualTo(expectedUri);
            await Assert.That(reissued.Method).IsEqualTo(expectedMethod);
            await Assert.That(repeated).IsEquivalentTo(reissued.ResponsePayload);

            // No expiration was applied, so the fresh response now sits under the expected key.
            await Assert.That(cache.Get(expectedKey).WaitForValue()).IsEquivalentTo(reissued.ResponsePayload);
        }
    }

    /// <summary>
    /// Runs a download whose caller asked for an expiration that has already passed, and asserts the
    /// response was written to the cache but is no longer readable — which holds only when the
    /// overload passed the caller's expiration on to the insert.
    /// </summary>
    /// <param name="download">Invokes the overload under test against the supplied cache and service.</param>
    /// <param name="expectedKey">The cache key the response is expected to be stored under.</param>
    /// <param name="expectedUri">The URI the fetch is expected to be made against.</param>
    /// <param name="expectedMethod">The HTTP method the fetch is expected to use.</param>
    /// <param name="expectedHeaders">The header collection the fetch is expected to carry, or <see langword="null"/>.</param>
    /// <returns>A task representing the assertion.</returns>
    internal static async Task AssertCachedEntryAlreadyExpired(
        Func<InMemoryBlobCache, RecordingHttpService, IObservable<byte[]>> download,
        string expectedKey,
        Uri expectedUri,
        HttpMethod expectedMethod,
        IEnumerable<KeyValuePair<string, string>>? expectedHeaders)
    {
        using RecordingHttpService service = new();
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        cache.SetHttpService(service);

        var downloaded = download(cache, service).WaitForValue();
        var issued = await AssertSingleIssuedRequest(service, expectedUri, expectedMethod, expectedHeaders);

        using (Assert.Multiple())
        {
            await Assert.That(downloaded).IsEquivalentTo(issued.ResponsePayload);

            // The response reached the cache: reading the creation stamp does not evict, so it still sees the entry.
            await Assert.That(cache.GetCreatedAt(expectedKey).WaitForValue()).IsNotNull();

            // It arrived already expired, so a read of the value finds nothing.
            await Assert.That(cache.Get(expectedKey).WaitForError()).IsTypeOf<KeyNotFoundException>();
        }
    }

    /// <summary>Asserts exactly one request was issued and that it carried the expected URI, method and headers.</summary>
    /// <param name="service">The service that issued the request.</param>
    /// <param name="expectedUri">The URI the request is expected to have been made against.</param>
    /// <param name="expectedMethod">The HTTP method the request is expected to have used.</param>
    /// <param name="expectedHeaders">The header collection the request is expected to have carried, or <see langword="null"/>.</param>
    /// <returns>The single issued request.</returns>
    private static async Task<RecordingHttpService.RecordedWebRequest> AssertSingleIssuedRequest(
        RecordingHttpService service,
        Uri expectedUri,
        HttpMethod expectedMethod,
        IEnumerable<KeyValuePair<string, string>>? expectedHeaders)
    {
        var issuedRequests = service.IssuedRequests;
        await Assert.That(issuedRequests.Count).IsEqualTo(1);

        var request = issuedRequests[0];
        using (Assert.Multiple())
        {
            await Assert.That(request.Uri).IsEqualTo(expectedUri);
            await Assert.That(request.Method).IsEqualTo(expectedMethod);
            await Assert.That(request.Headers).IsSameReferenceAs(expectedHeaders);
        }

        return request;
    }
}
