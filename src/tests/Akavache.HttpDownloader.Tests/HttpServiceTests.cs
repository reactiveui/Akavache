// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for HttpService functionality.
/// Uses a local test server instead of external dependencies for reliable offline testing.
/// Also covers argument validation, static helper branches, and nested-class construction paths.
/// </summary>
[Category("Akavache")]
public class HttpServiceTests
{
    /// <summary>A host that resolves but is reserved for documentation, so no request can succeed against it.</summary>
    private const string UnreachableHostUrl = "https://example.invalid";

    /// <summary>A host that cannot resolve at all, used to force a DNS failure quickly.</summary>
    private const string UnresolvableHostUrl = "https://nonexistent.invalid.localhost.test";

    /// <summary>A well-formed absolute URL used wherever a test only needs a syntactically valid address.</summary>
    private const string SampleUrl = "https://example.com";

    /// <summary>A loopback address on a port nothing listens on, so the connection is refused immediately.</summary>
    private const string RefusedConnectionUrl = "http://127.0.0.1:1/unused";

    /// <summary>How long a test waits for an HTTP observable to emit, fail, or complete.</summary>
    private const int ObservableCompletionTimeoutSeconds = 30;

    /// <summary>How long a test waits for a blob cache read to emit.</summary>
    private const int CacheReadTimeoutSeconds = 10;

    /// <summary>Timeout assigned to an <see cref="HttpClient"/> purely to prove the value round-trips.</summary>
    private const int CustomClientTimeoutSeconds = 30;

    /// <summary>Timeout assigned to a fast service so a network attempt gives up almost immediately.</summary>
    private const int FailFastTimeoutMilliseconds = 100;

    /// <summary>Timeout used by the retry tests, long enough for a retry to be attempted before giving up.</summary>
    private const int RetryAttemptTimeoutMilliseconds = 250;

    /// <summary>Timeout supplied to a fast service to prove the constructor applies it verbatim.</summary>
    private const int AppliedTimeoutSeconds = 5;

    /// <summary>A negative timeout, which <see cref="HttpClient.Timeout"/> rejects, exercising the constructor's catch block.</summary>
    private const int RejectedNegativeTimeoutSeconds = -5;

    /// <summary>Retry count handed to a fast service, chosen so a retry is observable as a second send.</summary>
    private const int FastServiceRetryCount = 2;

    /// <summary>A retry count a caller states on a fast service, which the fast service is expected to ignore.</summary>
    private const int IgnoredCallerRetryCount = 99;

    /// <summary>A timeout a caller states on a fast service, which the fast service is expected to ignore.</summary>
    private const int IgnoredCallerTimeoutMinutes = 10;

    /// <summary>Handler behind <see cref="RetryCountingClient"/>, counting the attempts a retry policy makes.</summary>
    private static readonly CountingHttpMessageHandler RetryCountingHandler = new();

    /// <summary>A client that fails every request, shared because a client is meant to outlive a single call.</summary>
    private static readonly HttpClient RetryCountingClient = new(RetryCountingHandler);

    /// <summary>Local HTTP test server used to serve canned responses to the SUT.</summary>
    private TestHttpServer? _testServer;

    /// <summary>Sets up the test fixture with a local HTTP server.</summary>
    [Before(Test)]
    public void OneTimeSetUp()
    {
        _testServer = new();
        _testServer.SetupDefaultResponses();
    }

    /// <summary>Cleans up the test fixture.</summary>
    [After(Test)]
    public void OneTimeTearDown() => _testServer?.Dispose();

    /// <summary>Tests that HttpService can be instantiated correctly.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task HttpServiceShouldInstantiateCorrectly()
    {
        // Act
        HttpService httpService = new();

        // Assert
        await Assert.That(httpService).IsNotNull();
        await Assert.That(httpService.HttpClient).IsNotNull();

        // Cleanup
        httpService.HttpClient.Dispose();
    }

    /// <summary>Tests that HttpService properly sets up compression.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task HttpServiceShouldSetupCompressionCorrectly()
    {
        // Act
        HttpService httpService = new();

        // Assert - HttpClient should be configured properly
        using (Assert.Multiple())
        {
            await Assert.That(httpService.HttpClient).IsNotNull();
            await Assert.That(httpService.HttpClient.DefaultRequestHeaders).IsNotNull();
        }

        // Cleanup
        httpService.HttpClient.Dispose();
    }

    /// <summary>Tests that DownloadUrl with URI parameter validates arguments correctly.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task DownloadUrlWithUriShouldValidateArguments()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        HttpService httpService = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        Uri? nullUri = null;

        try
        {
            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => httpService.DownloadUrl(cache, nullUri!));
        }
        finally
        {
            cache.Dispose();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>Tests that DownloadUrl with key validates arguments correctly.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlWithKeyShouldValidateArguments()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        HttpService httpService = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => httpService.DownloadUrl(null!, "key", "http://example.com"));
        }
        finally
        {
            cache.Dispose();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>Tests that multiple HttpService instances can be created.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task MultipleHttpServiceInstancesShouldBeCreatable()
    {
        // Arrange & Act
        // Use 'using' to ensure services (and their HttpClients) are always disposed
        HttpService service1 = new();
        HttpService service2 = new();

        // Assert
        // 'Assert.Multiple' ensures all assertions run before the test fails
        using (Assert.Multiple())
        {
            await Assert.That(service1).IsNotNull();
            await Assert.That(service2).IsNotNull();
            await Assert.That(service1.HttpClient).IsNotSameReferenceAs(service2.HttpClient);
        }

        // Cleanup
        service1.HttpClient.Dispose();

        service2.HttpClient.Dispose();
    }

    /// <summary>Tests that HttpService supports custom HttpClient configuration.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task HttpServiceShouldSupportCustomConfiguration()
    {
        // Arrange
        HttpService httpService = new();
        var customTimeout = TimeSpan.FromSeconds(CustomClientTimeoutSeconds);

        // Act
        httpService.HttpClient.Timeout = customTimeout;

        // Assert
        await Assert.That(httpService.HttpClient.Timeout).IsEqualTo(customTimeout);

        // Cleanup
        httpService.HttpClient.Dispose();
    }

    /// <summary>Tests that HttpService handles null headers gracefully.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task HttpServiceShouldHandleNullHeadersGracefully()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        HttpService httpService = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Act - This should not throw even with null headers
            var observable = httpService.DownloadUrl(
                cache,
                "test_key",
                $"{_testServer!.BaseUrl}status/200",
                HttpMethod.Get);

            // Assert - Observable should be created without error
            await Assert.That(observable).IsNotNull();
        }
        finally
        {
            cache.Dispose();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>Tests that HttpService handles different HTTP methods.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task HttpServiceShouldHandleDifferentHttpMethods()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        HttpService httpService = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Act & Assert - Should create observables for different methods without error
            var getObservable =
                httpService.DownloadUrl(cache, "get_key", $"{_testServer!.BaseUrl}status/200", HttpMethod.Get);
            var postObservable =
                httpService.DownloadUrl(cache, "post_key", $"{_testServer!.BaseUrl}status/200", HttpMethod.Post);
            var putObservable =
                httpService.DownloadUrl(cache, "put_key", $"{_testServer!.BaseUrl}status/200", HttpMethod.Put);

            using (Assert.Multiple())
            {
                await Assert.That(getObservable).IsNotNull();
                await Assert.That(postObservable).IsNotNull();
                await Assert.That(putObservable).IsNotNull();
            }
        }
        finally
        {
            cache.Dispose();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>Tests that HttpService respects fetchAlways parameter.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task HttpServiceShouldRespectFetchAlwaysParameter()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using HttpService httpService = new();
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Act - Should create different observables based on fetchAlways
        var cachedObservable = httpService.DownloadUrl(
            cache,
            "cached_key",
            $"{_testServer!.BaseUrl}status/200",
            HttpMethod.Get);
        var alwaysFetchObservable = httpService.DownloadUrl(
            cache,
            "always_key",
            $"{_testServer!.BaseUrl}status/200",
            HttpMethod.Get,
            null,
            true);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(cachedObservable).IsNotNull();
            await Assert.That(alwaysFetchObservable).IsNotNull();
        }
    }

    /// <summary>Tests that HttpService supports absolute expiration.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task HttpServiceShouldSupportAbsoluteExpiration()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        HttpService httpService = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        var expiration = TimeProvider.System.GetLocalNow().AddHours(1);

        try
        {
            // Act
            var observable = httpService.DownloadUrl(
                cache,
                "expiry_key",
                $"{_testServer!.BaseUrl}status/200",
                HttpMethod.Get,
                null,
                false,
                expiration);

            // Assert
            await Assert.That(observable).IsNotNull();
        }
        finally
        {
            cache.Dispose();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>Tests HttpService.DownloadUrl(string url) forwards without throwing for a valid url argument (pure forwarder path).</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlStringForwarderShouldThrowOnNullCache()
    {
        HttpService service = new();
        await Assert.That(() => service.DownloadUrl(null!, UnreachableHostUrl))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests HttpService.DownloadUrl(Uri url) throws on null Uri.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlUriShouldThrowOnNullUri()
    {
        HttpService service = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => service.DownloadUrl(cache, (Uri)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests HttpService.DownloadUrl(key, string url) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldThrowOnNullCache()
    {
        HttpService service = new();
        await Assert.That(() => service.DownloadUrl(null!, "key", UnreachableHostUrl))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests HttpService.DownloadUrl(key, Uri url) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldThrowOnNullCache()
    {
        HttpService service = new();
        await Assert.That(() => service.DownloadUrl(null!, "key", new Uri(UnreachableHostUrl)))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests HttpService.DownloadUrl(key, string url) returns cached value when present (not fetchAlways, hits cache).</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldReturnCachedValue()
    {
        HttpService service = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            byte[] expected = [1, 2, 3];
            cache.Insert("cached-key", expected).SubscribeAndComplete();

            var result = service.DownloadUrl(cache, "cached-key", UnreachableHostUrl).SubscribeGetValue();

            await Assert.That(result).IsEqualTo(expected);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests HttpService.DownloadUrl(key, Uri url) returns cached value when present (not fetchAlways, hits cache).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldReturnCachedValue()
    {
        HttpService service = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            byte[] expected = [4, 5, 6];
            cache.Insert("cached-uri-key", expected).SubscribeAndComplete();

            var result = service.DownloadUrl(cache, "cached-uri-key", new Uri(UnreachableHostUrl))
                .SubscribeGetValue();

            await Assert.That(result).IsEqualTo(expected);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, string url) with fetchAlways=true bypasses the cache and attempts a network call (which fails for an invalid host).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringFetchAlwaysShouldBypassCache()
    {
        HttpService.FastHttpService service = new(retries: 0, timeout: TimeSpan.FromMilliseconds(FailFastTimeoutMilliseconds));
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        cache.Insert("fetch-always-key", "\t\t\t"u8.ToArray()).SubscribeAndComplete();

        Exception? error = null;
        ManualResetEventSlim mre = new(false);
        _ = service.DownloadUrl(
            cache,
            "fetch-always-key",
            UnresolvableHostUrl,
            fetchAlways: true).Subscribe(
            static _ => { },
            ex =>
            {
                error = ex;
                mre.Set();
            },
            () => mre.Set());
        _ = mre.Wait(TimeSpan.FromSeconds(ObservableCompletionTimeoutSeconds));
        await Assert.That(error).IsNotNull();
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, Uri url) with fetchAlways=true bypasses the cache and attempts a network call (which fails for an invalid host).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriFetchAlwaysShouldBypassCache()
    {
        HttpService.FastHttpService service = new(retries: 0, timeout: TimeSpan.FromMilliseconds(FailFastTimeoutMilliseconds));
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        byte[] stalePayload = [7, 7, 7];
        cache.Insert("fetch-always-uri-key", stalePayload).SubscribeAndComplete();

        Exception? error = null;
        ManualResetEventSlim mre = new(false);
        _ = service.DownloadUrl(
            cache,
            "fetch-always-uri-key",
            new Uri(UnresolvableHostUrl),
            fetchAlways: true).Subscribe(
            static _ => { },
            ex =>
            {
                error = ex;
                mre.Set();
            },
            () => mre.Set());
        _ = mre.Wait(TimeSpan.FromSeconds(ObservableCompletionTimeoutSeconds));
        await Assert.That(error).IsNotNull();
    }

    /// <summary>Tests CreateWebRequest with null headers returns a request without extra headers.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateWebRequestWithNullHeadersShouldSucceed()
    {
        var request = HttpService.CreateWebRequest(new(SampleUrl), HttpMethod.Get, null);

        await Assert.That(request).IsNotNull();
        await Assert.That(request.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(request.RequestUri).IsEqualTo(new(SampleUrl));
    }

    /// <summary>Tests CreateWebRequest with supplied headers adds them to the request.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateWebRequestWithHeadersShouldAddHeaders()
    {
        KeyValuePair<string, string>[] headers =
        [
            new("X-Test-Header", "test-value"),
            new("X-Other", "other-value")
        ];

        var request = HttpService.CreateWebRequest(new(SampleUrl), HttpMethod.Post, headers);

        await Assert.That(request.Headers.Contains("X-Test-Header")).IsTrue();
        await Assert.That(request.Headers.Contains("X-Other")).IsTrue();
    }

    /// <summary>Tests ProcessWebResponse(string url) throws HttpRequestException when the response is not successful.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task ProcessWebResponseStringUrlShouldThrowOnNonSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NotFound);
        response.ReasonPhrase = "Not Found";

        var error = HttpService.ProcessWebResponse(response, "https://example.com/missing", null)
            .SubscribeGetError();
        await Assert.That(error).IsTypeOf<HttpRequestException>();
    }

    /// <summary>Tests ProcessWebResponse(Uri url) throws HttpRequestException when the response is not successful.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ProcessWebResponseUriShouldThrowOnNonSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.InternalServerError);
        response.ReasonPhrase = "Server Error";

        var error = HttpService
            .ProcessWebResponse(response, new Uri("https://example.com/boom"), TimeProvider.System.GetUtcNow().AddHours(1))
            .SubscribeGetError();
        await Assert.That(error).IsTypeOf<HttpRequestException>();
    }

    /// <summary>Tests ProcessWebResponse(string url) returns the content bytes on a successful response.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task ProcessWebResponseShouldReturnContentOnSuccess()
    {
        byte[] payload = [10, 20, 30];
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(payload);

        var result = HttpService.ProcessWebResponse(response, SampleUrl, null)
            .SubscribeGetValue();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Length).IsEqualTo(payload.Length);
        await Assert.That(result.SequenceEqual(payload)).IsTrue();
    }

    /// <summary>
    /// Exercises the happy path through
    /// <see cref="HttpService.DownloadUrl(IBlobCache, string, string, HttpMethod?, IEnumerable{KeyValuePair{string, string}}?, bool, DateTimeOffset?)"/>
    /// so the compiler-generated <c>SelectMany</c> lambda bodies (one per stage) actually
    /// execute against emitted values. Existing tests only verify the observable exists
    /// without subscribing, so the lambda bodies remained uncovered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldExecuteSelectManyLambdasOnSuccess()
    {
        SystemJsonSerializer serializer = new();
        HttpService httpService = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            byte[]? data = null;
            ManualResetEventSlim mre = new(false);
            _ = httpService
                .DownloadUrl(
                    cache,
                    "happy-key-string",
                   $"{_testServer!.BaseUrl}status/200",
                   HttpMethod.Get,
                   (IEnumerable<KeyValuePair<string, string>>?)null,
                   true).Subscribe(
                    v =>
                    {
                        data = v;
                        mre.Set();
                    },
                    _ => mre.Set());
            _ = mre.Wait(TimeSpan.FromSeconds(ObservableCompletionTimeoutSeconds));

            await Assert.That(data).IsNotNull();

            // The SelectMany that writes to the blob cache should have stored the payload.
            byte[]? stored = null;
            ManualResetEventSlim mre2 = new(false);
            _ = cache.Get("happy-key-string").Subscribe(
                v =>
                {
                    stored = v;
                    mre2.Set();
                },
                _ => mre2.Set());
            _ = mre2.Wait(TimeSpan.FromSeconds(CacheReadTimeoutSeconds));
            await Assert.That(stored).IsNotNull();
        }
        finally
        {
            cache.Dispose();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Analogue of <see cref="DownloadUrlKeyStringShouldExecuteSelectManyLambdasOnSuccess"/>
    /// for the <see cref="Uri"/> overload of
    /// <see cref="HttpService.DownloadUrl(IBlobCache, string, Uri, HttpMethod?, IEnumerable{KeyValuePair{string, string}}?, bool, DateTimeOffset?)"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldExecuteSelectManyLambdasOnSuccess()
    {
        SystemJsonSerializer serializer = new();
        HttpService httpService = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            byte[]? data = null;
            ManualResetEventSlim mre = new(false);
            _ = httpService
                .DownloadUrl(
                    cache,
                    "happy-key-uri",
                   new Uri($"{_testServer!.BaseUrl}status/200"),
                   HttpMethod.Get,
                   (IEnumerable<KeyValuePair<string, string>>?)null,
                   true).Subscribe(
                    v =>
                    {
                        data = v;
                        mre.Set();
                    },
                    _ => mre.Set());
            _ = mre.Wait(TimeSpan.FromSeconds(ObservableCompletionTimeoutSeconds));

            await Assert.That(data).IsNotNull();

            byte[]? stored = null;
            ManualResetEventSlim mre2 = new(false);
            _ = cache.Get("happy-key-uri").Subscribe(
                v =>
                {
                    stored = v;
                    mre2.Set();
                },
                _ => mre2.Set());
            _ = mre2.Wait(TimeSpan.FromSeconds(CacheReadTimeoutSeconds));
            await Assert.That(stored).IsNotNull();
        }
        finally
        {
            cache.Dispose();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Tests MakeWebRequest with null content goes through the no-content branch (exercised via a subclass exposer that fails fast).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public Task MakeWebRequestWithNullContentShouldExecuteNoContentBranch()
    {
        TestableHttpService service = new();

        ManualResetEventSlim mre = new(false);
        _ = service.InvokeMakeWebRequest(
                new(UnresolvableHostUrl),
                HttpMethod.Get,
                headers: null,
                content: null,
                retries: 0,
                timeout: TimeSpan.FromMilliseconds(FailFastTimeoutMilliseconds))
            .Subscribe(
                _ => mre.Set(),
                _ => mre.Set(),
                () => mre.Set());
        _ = mre.Wait(TimeSpan.FromSeconds(ObservableCompletionTimeoutSeconds));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests MakeWebRequest with non-null content goes through the StringContent branch (exercised via a subclass exposer that fails fast).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public Task MakeWebRequestWithContentShouldExecuteContentBranch()
    {
        TestableHttpService service = new();

        ManualResetEventSlim mre = new(false);
        _ = service.InvokeMakeWebRequest(
                new(UnresolvableHostUrl),
                HttpMethod.Post,
                headers: null,
                content: "request-body",
                retries: 0,
                timeout: TimeSpan.FromMilliseconds(FailFastTimeoutMilliseconds))
            .Subscribe(
                _ => mre.Set(),
                _ => mre.Set(),
                mre.Set);
        _ = mre.Wait(TimeSpan.FromSeconds(ObservableCompletionTimeoutSeconds));
        return Task.CompletedTask;
    }

    /// <summary>Tests the default FastHttpService constructor uses the default retries and timeout without throwing.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FastHttpServiceDefaultConstructorShouldNotThrow()
    {
        HttpService.FastHttpService service = new();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.HttpClient).IsNotNull();
    }

    /// <summary>
    /// Tests the FastHttpService constructor with explicit retries and timeout applies them to the underlying HttpClient without throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FastHttpServiceWithCustomRetriesAndTimeoutShouldNotThrow()
    {
        var timeout = TimeSpan.FromSeconds(AppliedTimeoutSeconds);
        HttpService.FastHttpService service = new(retries: 1, timeout: timeout);

        await Assert.That(service).IsNotNull();
        await Assert.That(service.HttpClient.Timeout).IsEqualTo(timeout);
    }

    /// <summary>
    /// A fast service given only a retry count falls back to the fast default timeout, and the retry
    /// count it was constructed with is the one requests actually use — the caller's own retry count
    /// and timeout are ignored, so the send count reflects the constructed value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FastHttpServiceWithRetriesOnlyShouldUseFastDefaultTimeoutAndApplyThoseRetries()
    {
        HttpService.FastHttpService service = new(retries: FastServiceRetryCount);

        await Assert.That(service.HttpClient.Timeout).IsEqualTo(HttpService.FastHttpService.FastDefaultTimeout);

        // Route the service through a client that fails every attempt and counts them. The service is
        // deliberately left undisposed: its own client is released here, and disposing the service
        // would take the shared counting client down with it.
        service.HttpClient.Dispose();
        service.HttpClient = RetryCountingClient;

        var error = service.MakeWebRequest(
                new(RefusedConnectionUrl),
                HttpMethod.Get,
                (IEnumerable<KeyValuePair<string, string>>?)null,
                (string?)null,
                IgnoredCallerRetryCount,
                TimeSpan.FromMinutes(IgnoredCallerTimeoutMinutes))
            .WaitForError();

        using (Assert.Multiple())
        {
            await Assert.That(error).IsNotNull();
            await Assert.That(RetryCountingHandler.SendCount).IsEqualTo(FastServiceRetryCount);
        }
    }

    /// <summary>
    /// Tests MakeWebRequest with non-null content subscribes and executes the StringContent assignment and SendAsync call.
    /// Uses retries >= 1 so Retry() actually subscribes to the Defer observable and runs its body.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithContentShouldAssignStringContentAndSend()
    {
        TestableHttpService service = new();

        ManualResetEventSlim mre = new(false);
        _ = service.InvokeMakeWebRequest(
                new(RefusedConnectionUrl),
                HttpMethod.Post,
                headers: null,
                content: "hello-body",
                retries: 1,
                timeout: TimeSpan.FromMilliseconds(RetryAttemptTimeoutMilliseconds))
            .Subscribe(
                _ => mre.Set(),
                _ => mre.Set(),
                mre.Set);
        _ = mre.Wait(TimeSpan.FromSeconds(ObservableCompletionTimeoutSeconds));
    }

    /// <summary>
    /// Tests MakeWebRequest with non-null content via the fully-routed retry path (retries=2) to ensure the Defer body and StringContent/SendAsync lines are exercised.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithContentAndRetriesShouldExecuteDeferBody()
    {
        TestableHttpService service = new();

        ManualResetEventSlim mre = new(false);
        _ = service.InvokeMakeWebRequest(
                new(RefusedConnectionUrl),
                HttpMethod.Put,
                headers: [new("X-Test", "1")],
                content: "{\"key\":\"value\"}",
                retries: 2,
                timeout: TimeSpan.FromMilliseconds(RetryAttemptTimeoutMilliseconds))
            .Subscribe(
                _ => mre.Set(),
                _ => mre.Set(),
                mre.Set);
        _ = mre.Wait(TimeSpan.FromSeconds(ObservableCompletionTimeoutSeconds));
    }

    /// <summary>Tests MakeWebRequest with null content via the fully-routed retry path to exercise the no-content Defer branch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithNullContentAndRetriesShouldExecuteDeferBody()
    {
        TestableHttpService service = new();

        ManualResetEventSlim mre = new(false);
        _ = service.InvokeMakeWebRequest(
                new(RefusedConnectionUrl),
                HttpMethod.Get,
                headers: null,
                content: null,
                retries: 1,
                timeout: TimeSpan.FromMilliseconds(RetryAttemptTimeoutMilliseconds))
            .Subscribe(
                _ => mre.Set(),
                _ => mre.Set(),
                mre.Set);
        _ = mre.Wait(TimeSpan.FromSeconds(ObservableCompletionTimeoutSeconds));
    }

    /// <summary>
    /// Tests the FastHttpService constructor's catch block by passing a negative TimeSpan that makes HttpClient.Timeout throw ArgumentOutOfRangeException.
    /// The constructor must swallow the exception and construct successfully.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FastHttpServiceWithInvalidNegativeTimeoutShouldSwallowException()
    {
        HttpService.FastHttpService service = new(retries: 0, timeout: TimeSpan.FromSeconds(RejectedNegativeTimeoutSeconds));

        await Assert.That(service).IsNotNull();
        await Assert.That(service.HttpClient).IsNotNull();
    }

    /// <summary>
    /// Tests the FastHttpService constructor's catch block by passing TimeSpan.Zero that makes HttpClient.Timeout throw ArgumentOutOfRangeException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FastHttpServiceWithZeroTimeoutShouldSwallowException()
    {
        HttpService.FastHttpService service = new(retries: 0, timeout: TimeSpan.Zero);

        await Assert.That(service).IsNotNull();
        await Assert.That(service.HttpClient).IsNotNull();
    }

    /// <summary>
    /// Tests the FastHttpService constructor's catch block by passing TimeSpan.MinValue that makes HttpClient.Timeout throw ArgumentOutOfRangeException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FastHttpServiceWithMinValueTimeoutShouldSwallowException()
    {
        HttpService.FastHttpService service = new(retries: 0, timeout: TimeSpan.MinValue);

        await Assert.That(service).IsNotNull();
        await Assert.That(service.HttpClient).IsNotNull();
    }

    /// <summary>
    /// Exercises the null branch of <c>x ?? []</c> in
    /// <see cref="HttpService.DownloadUrl(IBlobCache, string, string, HttpMethod?, IEnumerable{KeyValuePair{string, string}}?, bool, DateTimeOffset?)"/>
    /// (<c>fetchAlways: false</c> path) by using a stub
    /// <see cref="IBlobCache"/> whose <c>Get</c> emits a null byte array.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldCoalesceNullCacheValueToEmpty()
    {
        using NullGetBlobCache cache = new();
        HttpService service = new();
        try
        {
            var result = service.DownloadUrl(cache, "any-key", UnreachableHostUrl).SubscribeGetValue();

            await Assert.That(result).IsNotNull();
            await Assert.That(result!).IsEmpty();
        }
        finally
        {
            service.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Exercises the null branch of <c>x ?? []</c> in the <see cref="Uri"/> overload of
    /// <see cref="HttpService.DownloadUrl(IBlobCache, string, Uri, HttpMethod?, IEnumerable{KeyValuePair{string, string}}?, bool, DateTimeOffset?)"/>
    /// using the same null-emitting cache stub.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldCoalesceNullCacheValueToEmpty()
    {
        using NullGetBlobCache cache = new();
        HttpService service = new();
        try
        {
            var result = service.DownloadUrl(cache, "any-key", new Uri(UnreachableHostUrl)).SubscribeGetValue();

            await Assert.That(result).IsNotNull();
            await Assert.That(result!).IsEmpty();
        }
        finally
        {
            service.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Calling Dispose twice is idempotent — the second call takes the early-return
    /// path at the <c>Interlocked.Exchange</c> guard (line 185, already-disposed branch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DisposeTwiceShouldBeIdempotent()
    {
        HttpService service = new();
        service.Dispose();
        service.Dispose();

        await Assert.That(service).IsNotNull();
    }

    /// <summary>Calling <c>Dispose(false)</c> takes the <c>!isDisposing</c> early-return path (line 185), leaving the HttpClient alive.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DisposeWithDisposingFalseShouldNotDisposeHttpClient()
    {
        DisposeTestableHttpService service = new();
        service.InvokeDispose(disposing: false);

        // HttpClient should still be usable because managed resources were not released.
        await Assert.That(service.HttpClient).IsNotNull();
        await Assert.That(service.HttpClient.Timeout).IsGreaterThan(TimeSpan.Zero);

        // Clean up properly.
        service.Dispose();
    }

    /// <summary>A handler that fails every request and counts the attempts, so a retry policy shows up as a send count.</summary>
    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>The number of sends this handler has been asked for.</summary>
        private int _sendCount;

        /// <summary>Gets the number of sends this handler has been asked for.</summary>
        public int SendCount => Volatile.Read(ref _sendCount);

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _ = Interlocked.Increment(ref _sendCount);
            return Task.FromException<HttpResponseMessage>(new HttpRequestException("The counting handler refuses every request."));
        }
    }

    /// <summary>Exposes the protected <c>Dispose(bool)</c> method so the <c>isDisposing: false</c> path can be exercised directly.</summary>
    private sealed class DisposeTestableHttpService : HttpService
    {
        /// <summary>Invokes the protected <see cref="HttpService.Dispose(bool)"/> method.</summary>
        /// <param name="disposing">Whether managed resources should be released.</param>
        public void InvokeDispose(bool disposing) => Dispose(disposing);
    }

    /// <summary>Exposes the protected MakeWebRequest for direct testing of its content-branch logic.</summary>
    private sealed class TestableHttpService : HttpService
    {
        /// <summary>Invokes the protected <c>MakeWebRequest</c> method.</summary>
        /// <param name="uri">The target URI.</param>
        /// <param name="method">The HTTP method.</param>
        /// <param name="headers">Optional request headers.</param>
        /// <param name="content">Optional request body content.</param>
        /// <param name="retries">The number of retry attempts.</param>
        /// <param name="timeout">The optional request timeout.</param>
        /// <returns>An observable that emits the HTTP response.</returns>
        public IObservable<HttpResponseMessage> InvokeMakeWebRequest(
            Uri uri,
            HttpMethod method,
            IEnumerable<KeyValuePair<string, string>>? headers,
            string? content,
            int retries,
            TimeSpan? timeout) =>
            MakeWebRequest(uri, method, headers, content, retries, timeout);
    }

    /// <summary>
    /// Minimal <see cref="IBlobCache"/> whose <c>Get(key)</c> yields a single null
    /// byte array. Used to drive the null-coalesce branches in
    /// <see cref="HttpService.DownloadUrl(IBlobCache, string, string, HttpMethod?, IEnumerable{KeyValuePair{string, string}}?, bool, DateTimeOffset?)"/>.
    /// </summary>
    private sealed class NullGetBlobCache : IBlobCache
    {
        /// <inheritdoc/>
        public ISerializer Serializer { get; } = new SystemJsonSerializer();

        /// <inheritdoc/>
        public ISequencer Scheduler => ImmediateSequencer.Instance;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) => Signal.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) => Signal.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) =>
            Signal.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) =>
            Signal.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) =>
            Signal.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => Signal.Empty<string>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => Signal.Empty<string>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => Signal.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) =>
            Signal.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
            Signal.Empty<(string Key, DateTimeOffset? Time)>();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
            Signal.Empty<(string Key, DateTimeOffset? Time)>();

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(string key, byte[] data) =>
            Insert(key, data, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(string key, byte[] data, Type type) =>
            Insert(key, data, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid>
            Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs) =>
            Insert(keyValuePairs, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            DateTimeOffset? absoluteExpiration) => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type) =>
            Insert(keyValuePairs, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            Type type,
            DateTimeOffset? absoluteExpiration) => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(string key) => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(string key, Type type) => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(IEnumerable<string> keys) => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(IEnumerable<string> keys, Type type) => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> InvalidateAll() => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> InvalidateAll(Type type) => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> Flush() => InvalidateAll();

        /// <inheritdoc/>
        public IObservable<RxVoid> Flush(Type type) => InvalidateAll(type);

        /// <inheritdoc/>
        public IObservable<RxVoid> Vacuum() => Flush();

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(
            IEnumerable<string> keys,
            Type type,
            DateTimeOffset? absoluteExpiration) => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
