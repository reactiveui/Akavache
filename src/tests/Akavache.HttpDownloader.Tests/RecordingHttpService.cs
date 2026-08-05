// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Akavache.Integration.Tests;

/// <summary>
/// An <see cref="HttpService"/> that answers every request from memory instead of the network and
/// records the request it was asked to make, so a caller can see exactly which URI, method and
/// headers a download produced.
/// A request is recorded when it is issued — that is, when the fetch is subscribed to — rather than
/// when the fetch pipeline is composed, so a download answered from the cache leaves no record.
/// </summary>
internal sealed class RecordingHttpService : HttpService
{
    /// <summary>Length of each canned response body. 64 bytes is the smallest buffer the image helpers accept.</summary>
    internal const int ResponsePayloadLength = 64;

    /// <summary>Guards the recorded state, which a fetch may write from a scheduler thread.</summary>
    private readonly Lock _gate = new();

    /// <summary>The requests that have been issued, in the order they were issued.</summary>
    private readonly List<RecordedWebRequest> _issuedRequests = [];

    /// <summary>The responses handed out so far, owned by this service so they are disposed with it.</summary>
    private readonly List<HttpResponseMessage> _responses = [];

    /// <summary>Gets a snapshot of the requests this service has issued, in the order they were issued.</summary>
    internal IReadOnlyList<RecordedWebRequest> IssuedRequests
    {
        get
        {
            lock (_gate)
            {
                return [.. _issuedRequests];
            }
        }
    }

    /// <inheritdoc/>
    protected internal override IObservable<HttpResponseMessage> MakeWebRequest(
        Uri uri,
        HttpMethod method,
        IEnumerable<KeyValuePair<string, string>>? headers,
        string? content,
        int retries,
        TimeSpan? timeout) =>
        Observable.Defer(() => Observable.Return(Issue(uri, method, headers, content, retries, timeout)));

    /// <inheritdoc/>
    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            lock (_gate)
            {
                foreach (var response in _responses)
                {
                    response.Dispose();
                }

                _responses.Clear();
            }
        }

        base.Dispose(isDisposing);
    }

    /// <summary>Records a request as issued and builds the canned response that answers it.</summary>
    /// <param name="uri">The URI the request was made against.</param>
    /// <param name="method">The HTTP method the request used.</param>
    /// <param name="headers">The headers the request carried.</param>
    /// <param name="content">The body the request carried.</param>
    /// <param name="retries">The retry count the request was given.</param>
    /// <param name="timeout">The timeout the request was given.</param>
    /// <returns>A successful response whose body identifies the position of this request in the sequence.</returns>
    private HttpResponseMessage Issue(
        Uri uri,
        HttpMethod method,
        IEnumerable<KeyValuePair<string, string>>? headers,
        string? content,
        int retries,
        TimeSpan? timeout)
    {
        lock (_gate)
        {
            // Every response body differs from the last, so a caller can tell a fresh download from a cached one.
            var payload = new byte[ResponsePayloadLength];
            Array.Fill(payload, (byte)(_issuedRequests.Count + 1));

            HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) };
            _responses.Add(response);
            _issuedRequests.Add(new(uri, method, headers, content, retries, timeout, payload));

            return response;
        }
    }

    /// <summary>A request that was issued, together with the response body it was answered with.</summary>
    /// <param name="Uri">The URI the request was made against.</param>
    /// <param name="Method">The HTTP method the request used.</param>
    /// <param name="Headers">The headers the request carried, or <see langword="null"/> when the caller supplied none.</param>
    /// <param name="Content">The body the request carried, or <see langword="null"/> when the caller supplied none.</param>
    /// <param name="Retries">The retry count the request was given.</param>
    /// <param name="Timeout">The timeout the request was given, or <see langword="null"/> when the caller supplied none.</param>
    /// <param name="ResponsePayload">The body the request was answered with.</param>
    internal sealed record RecordedWebRequest(
        Uri Uri,
        HttpMethod Method,
        IEnumerable<KeyValuePair<string, string>>? Headers,
        string? Content,
        int Retries,
        TimeSpan? Timeout,
        byte[] ResponsePayload);
}
