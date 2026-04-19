// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using Akavache.SystemTextJson;
using Akavache.Tests;
using Akavache.Tests.Helpers;

namespace Akavache.Integration.Tests;

/// <summary>
/// Tests for HttpService functionality.
/// Uses a local test server instead of external dependencies for reliable offline testing.
/// Also covers argument validation, static helper branches, and nested-class construction paths.
/// </summary>
[Category("Akavache")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "Cleanup is handled via test hooks")]
public class HttpServiceTests
{
    /// <summary>
    /// Local HTTP test server used to serve canned responses to the SUT.
    /// </summary>
    private TestHttpServer? _testServer;

    /// <summary>
    /// Sets up the test fixture with a local HTTP server.
    /// </summary>
    [Before(Test)]
    public void OneTimeSetUp()
    {
        _testServer = new();
        _testServer.SetupDefaultResponses();
    }

    /// <summary>
    /// Cleans up the test fixture.
    /// </summary>
    [After(Test)]
    public void OneTimeTearDown() => _testServer?.Dispose();

    /// <summary>
    /// Tests that HttpService can be instantiated correctly.
    /// </summary>
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

    /// <summary>
    /// Tests that HttpService properly sets up compression.
    /// </summary>
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

    /// <summary>
    /// Tests that DownloadUrl with URI parameter validates arguments correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task DownloadUrlWithUriShouldValidateArguments()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        HttpService httpService = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        Uri? nullUri = null;

        try
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => httpService.DownloadUrl(cache, nullUri!));
        }
        finally
        {
            cache.Dispose();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Tests that DownloadUrl with key validates arguments correctly.
    /// </summary>
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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => httpService.DownloadUrl(null!, "key", "http://example.com"));
        }
        finally
        {
            cache.Dispose();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Tests that multiple HttpService instances can be created.
    /// </summary>
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

    /// <summary>
    /// Tests that HttpService supports custom HttpClient configuration.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task HttpServiceShouldSupportCustomConfiguration()
    {
        // Arrange
        HttpService httpService = new();
        var customTimeout = TimeSpan.FromSeconds(30);

        // Act
        httpService.HttpClient.Timeout = customTimeout;

        // Assert
        await Assert.That(httpService.HttpClient.Timeout).IsEqualTo(customTimeout);

        // Cleanup
        httpService.HttpClient.Dispose();
    }

    /// <summary>
    /// Tests that HttpService handles null headers gracefully.
    /// </summary>
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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

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

    /// <summary>
    /// Tests that HttpService handles different HTTP methods.
    /// </summary>
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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

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

    /// <summary>
    /// Tests that HttpService respects fetchAlways parameter.
    /// </summary>
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
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

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

    /// <summary>
    /// Tests that HttpService supports absolute expiration.
    /// </summary>
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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        var expiration = DateTimeOffset.Now.AddHours(1);

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

    /// <summary>
    /// Tests HttpService.DownloadUrl(string url) forwards without throwing for a valid url argument (pure forwarder path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlStringForwarderShouldThrowOnNullCache()
    {
        HttpService service = new();
        await Assert.That(() => service.DownloadUrl(null!, "https://example.invalid"))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(Uri url) throws on null Uri.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlUriShouldThrowOnNullUri()
    {
        HttpService service = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
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

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, string url) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldThrowOnNullCache()
    {
        HttpService service = new();
        await Assert.That(() => service.DownloadUrl(null!, "key", "https://example.invalid"))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, Uri url) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldThrowOnNullCache()
    {
        HttpService service = new();
        await Assert.That(() => service.DownloadUrl(null!, "key", new Uri("https://example.invalid")))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, string url) returns cached value when present (not fetchAlways, hits cache).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldReturnCachedValue()
    {
        HttpService service = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            byte[] expected = [1, 2, 3];
            cache.Insert("cached-key", expected).SubscribeAndComplete();

            var result = service.DownloadUrl(cache, "cached-key", "https://example.invalid").SubscribeGetValue();

            await Assert.That(result).IsEqualTo(expected);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, Uri url) returns cached value when present (not fetchAlways, hits cache).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldReturnCachedValue()
    {
        HttpService service = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            byte[] expected = [4, 5, 6];
            cache.Insert("cached-uri-key", expected).SubscribeAndComplete();

            var result = service.DownloadUrl(cache, "cached-uri-key", new Uri("https://example.invalid"))
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
        HttpService.FastHttpService service = new(retries: 0, timeout: TimeSpan.FromMilliseconds(100));
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        cache.Insert("fetch-always-key", "\t\t\t"u8.ToArray()).SubscribeAndComplete();

        Exception? error = null;
        ManualResetEventSlim mre = new(false);
        service.DownloadUrl(
            cache,
            "fetch-always-key",
            "https://nonexistent.invalid.localhost.test",
            fetchAlways: true).Subscribe(
            _ => { },
            ex =>
            {
                error = ex;
                mre.Set();
            },
            () => mre.Set());
        mre.Wait(TimeSpan.FromSeconds(30));
        await Assert.That(error).IsNotNull();
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, Uri url) with fetchAlways=true bypasses the cache and attempts a network call (which fails for an invalid host).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriFetchAlwaysShouldBypassCache()
    {
        HttpService.FastHttpService service = new(retries: 0, timeout: TimeSpan.FromMilliseconds(100));
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        cache.Insert("fetch-always-uri-key", [7, 7, 7]).SubscribeAndComplete();

        Exception? error = null;
        ManualResetEventSlim mre = new(false);
        service.DownloadUrl(
            cache,
            "fetch-always-uri-key",
            new Uri("https://nonexistent.invalid.localhost.test"),
            fetchAlways: true).Subscribe(
            _ => { },
            ex =>
            {
                error = ex;
                mre.Set();
            },
            () => mre.Set());
        mre.Wait(TimeSpan.FromSeconds(30));
        await Assert.That(error).IsNotNull();
    }

    /// <summary>
    /// Tests CreateWebRequest with null headers returns a request without extra headers.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateWebRequestWithNullHeadersShouldSucceed()
    {
        var request = HttpService.CreateWebRequest(new("https://example.com"), HttpMethod.Get, null);

        await Assert.That(request).IsNotNull();
        await Assert.That(request.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(request.RequestUri).IsEqualTo(new("https://example.com"));
    }

    /// <summary>
    /// Tests CreateWebRequest with supplied headers adds them to the request.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateWebRequestWithHeadersShouldAddHeaders()
    {
        KeyValuePair<string, string>[] headers =
        [
            new("X-Test-Header", "test-value"),
            new("X-Other", "other-value")
        ];

        var request = HttpService.CreateWebRequest(new("https://example.com"), HttpMethod.Post, headers);

        await Assert.That(request.Headers.Contains("X-Test-Header")).IsTrue();
        await Assert.That(request.Headers.Contains("X-Other")).IsTrue();
    }

    /// <summary>
    /// Tests ProcessWebResponse(string url) throws HttpRequestException when the response is not successful.
    /// </summary>
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

    /// <summary>
    /// Tests ProcessWebResponse(Uri url) throws HttpRequestException when the response is not successful.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ProcessWebResponseUriShouldThrowOnNonSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.InternalServerError);
        response.ReasonPhrase = "Server Error";

        var error = HttpService
            .ProcessWebResponse(response, new Uri("https://example.com/boom"), DateTimeOffset.UtcNow.AddHours(1))
            .SubscribeGetError();
        await Assert.That(error).IsTypeOf<HttpRequestException>();
    }

    /// <summary>
    /// Tests ProcessWebResponse(string url) returns the content bytes on a successful response.
    /// </summary>
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

        var result = HttpService.ProcessWebResponse(response, "https://example.com", null)
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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            byte[]? data = null;
            ManualResetEventSlim mre = new(false);
            httpService
                .DownloadUrl(
                    cache,
                    "happy-key-string",
                    $"{_testServer!.BaseUrl}status/200",
                    HttpMethod.Get,
                    fetchAlways: true).Subscribe(
                    v =>
                    {
                        data = v;
                        mre.Set();
                    },
                    _ => mre.Set());
            mre.Wait(TimeSpan.FromSeconds(30));

            await Assert.That(data).IsNotNull();

            // The SelectMany that writes to the blob cache should have stored the payload.
            byte[]? stored = null;
            ManualResetEventSlim mre2 = new(false);
            cache.Get("happy-key-string").Subscribe(
                v =>
                {
                    stored = v;
                    mre2.Set();
                },
                _ => mre2.Set());
            mre2.Wait(TimeSpan.FromSeconds(10));
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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            byte[]? data = null;
            ManualResetEventSlim mre = new(false);
            httpService
                .DownloadUrl(
                    cache,
                    "happy-key-uri",
                    new Uri($"{_testServer!.BaseUrl}status/200"),
                    HttpMethod.Get,
                    fetchAlways: true).Subscribe(
                    v =>
                    {
                        data = v;
                        mre.Set();
                    },
                    _ => mre.Set());
            mre.Wait(TimeSpan.FromSeconds(30));

            await Assert.That(data).IsNotNull();

            byte[]? stored = null;
            ManualResetEventSlim mre2 = new(false);
            cache.Get("happy-key-uri").Subscribe(
                v =>
                {
                    stored = v;
                    mre2.Set();
                },
                _ => mre2.Set());
            mre2.Wait(TimeSpan.FromSeconds(10));
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
        service.InvokeMakeWebRequest(
                new("https://nonexistent.invalid.localhost.test"),
                HttpMethod.Get,
                headers: null,
                content: null,
                retries: 0,
                timeout: TimeSpan.FromMilliseconds(100))
            .Subscribe(
                _ => mre.Set(),
                _ => mre.Set(),
                () => mre.Set());
        mre.Wait(TimeSpan.FromSeconds(30));
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
        service.InvokeMakeWebRequest(
                new("https://nonexistent.invalid.localhost.test"),
                HttpMethod.Post,
                headers: null,
                content: "request-body",
                retries: 0,
                timeout: TimeSpan.FromMilliseconds(100))
            .Subscribe(
                _ => mre.Set(),
                _ => mre.Set(),
                mre.Set);
        mre.Wait(TimeSpan.FromSeconds(30));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests the default FastHttpService constructor uses the default retries and timeout without throwing.
    /// </summary>
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
        var timeout = TimeSpan.FromSeconds(5);
        HttpService.FastHttpService service = new(retries: 1, timeout: timeout);

        await Assert.That(service).IsNotNull();
        await Assert.That(service.HttpClient.Timeout).IsEqualTo(timeout);
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
        service.InvokeMakeWebRequest(
                new("http://127.0.0.1:1/unused"),
                HttpMethod.Post,
                headers: null,
                content: "hello-body",
                retries: 1,
                timeout: TimeSpan.FromMilliseconds(250))
            .Subscribe(
                _ => mre.Set(),
                _ => mre.Set(),
                mre.Set);
        mre.Wait(TimeSpan.FromSeconds(30));
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
        service.InvokeMakeWebRequest(
                new("http://127.0.0.1:1/unused"),
                HttpMethod.Put,
                headers: [new("X-Test", "1")],
                content: "{\"key\":\"value\"}",
                retries: 2,
                timeout: TimeSpan.FromMilliseconds(250))
            .Subscribe(
                _ => mre.Set(),
                _ => mre.Set(),
                mre.Set);
        mre.Wait(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Tests MakeWebRequest with null content via the fully-routed retry path to exercise the no-content Defer branch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithNullContentAndRetriesShouldExecuteDeferBody()
    {
        TestableHttpService service = new();

        ManualResetEventSlim mre = new(false);
        service.InvokeMakeWebRequest(
                new("http://127.0.0.1:1/unused"),
                HttpMethod.Get,
                headers: null,
                content: null,
                retries: 1,
                timeout: TimeSpan.FromMilliseconds(250))
            .Subscribe(
                _ => mre.Set(),
                _ => mre.Set(),
                mre.Set);
        mre.Wait(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Tests the FastHttpService constructor's catch block by passing a negative TimeSpan that makes HttpClient.Timeout throw ArgumentOutOfRangeException.
    /// The constructor must swallow the exception and construct successfully.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FastHttpServiceWithInvalidNegativeTimeoutShouldSwallowException()
    {
        HttpService.FastHttpService service = new(retries: 0, timeout: TimeSpan.FromSeconds(-5));

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
            var result = service.DownloadUrl(cache, "any-key", "https://example.invalid").SubscribeGetValue();

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
            var result = service.DownloadUrl(cache, "any-key", new Uri("https://example.invalid")).SubscribeGetValue();

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

    /// <summary>
    /// Calling <c>Dispose(false)</c> takes the <c>!isDisposing</c> early-return path
    /// (line 185), leaving the HttpClient alive.
    /// </summary>
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

    /// <summary>
    /// Exposes the protected <c>Dispose(bool)</c> method so the <c>isDisposing: false</c>
    /// path can be exercised directly.
    /// </summary>
    private sealed class DisposeTestableHttpService : HttpService
    {
        /// <summary>
        /// Invokes the protected <see cref="HttpService.Dispose(bool)"/> method.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be released.</param>
        public void InvokeDispose(bool disposing) => Dispose(disposing);
    }

    /// <summary>
    /// Exposes the protected MakeWebRequest for direct testing of its content-branch logic.
    /// </summary>
    private sealed class TestableHttpService : HttpService
    {
        /// <summary>
        /// Invokes the protected <see cref="HttpService.MakeWebRequest"/> method.
        /// </summary>
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
        public IScheduler Scheduler => ImmediateScheduler.Instance;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) => Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) => Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) =>
            Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) =>
            Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) =>
            Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) =>
            Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
            Observable.Empty<(string, DateTimeOffset?)>();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
            Observable.Empty<(string, DateTimeOffset?)>();

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit>
            Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            Type type,
            DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key, Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll(Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Flush() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Flush(Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Vacuum() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(
            IEnumerable<string> keys,
            Type type,
            DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
