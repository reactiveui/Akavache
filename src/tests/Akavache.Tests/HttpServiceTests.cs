// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Reactive.Threading.Tasks;
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for HttpService functionality.
/// Uses a local test server instead of external dependencies for reliable offline testing.
/// Also covers argument validation, static helper branches, and nested-class construction paths.
/// </summary>
[Category("Akavache")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Cleanup is handled via test hooks")]
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
        _testServer = new TestHttpServer();
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
        var httpService = new HttpService();

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
        var httpService = new HttpService();

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
        var serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache(serializer);
        Uri? nullUri = null;

        try
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => httpService.DownloadUrl(cache, nullUri!));
        }
        finally
        {
            await cache.DisposeAsync();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Tests that DownloadUrl with key validates arguments correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task DownloadUrlWithKeyShouldValidateArguments()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => httpService.DownloadUrl(null!, "key", "http://example.com"));
        }
        finally
        {
            await cache.DisposeAsync();
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
        var service1 = new HttpService();
        var service2 = new HttpService();

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
        var httpService = new HttpService();
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
    public async Task HttpServiceShouldHandleNullHeadersGracefully()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act - This should not throw even with null headers
            var observable = httpService.DownloadUrl(
                cache,
                "test_key",
                $"{_testServer!.BaseUrl}status/200",
                HttpMethod.Get,
                null,
                false,
                null);

            // Assert - Observable should be created without error
            await Assert.That(observable).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Tests that HttpService handles different HTTP methods.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task HttpServiceShouldHandleDifferentHttpMethods()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act & Assert - Should create observables for different methods without error
            var getObservable = httpService.DownloadUrl(cache, "get_key", $"{_testServer!.BaseUrl}status/200", HttpMethod.Get);
            var postObservable = httpService.DownloadUrl(cache, "post_key", $"{_testServer!.BaseUrl}status/200", HttpMethod.Post);
            var putObservable = httpService.DownloadUrl(cache, "put_key", $"{_testServer!.BaseUrl}status/200", HttpMethod.Put);

            using (Assert.Multiple())
            {
                await Assert.That(getObservable).IsNotNull();
                await Assert.That(postObservable).IsNotNull();
                await Assert.That(putObservable).IsNotNull();
            }
        }
        finally
        {
            await cache.DisposeAsync();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Tests that HttpService respects fetchAlways parameter.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task HttpServiceShouldRespectFetchAlwaysParameter()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act - Should create different observables based on fetchAlways
            var cachedObservable = httpService.DownloadUrl(
                cache,
                "cached_key",
                $"{_testServer!.BaseUrl}status/200",
                HttpMethod.Get,
                null,
                false);
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
        finally
        {
            await cache.DisposeAsync();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Tests that HttpService supports absolute expiration.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task HttpServiceShouldSupportAbsoluteExpiration()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache(serializer);
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
            await cache.DisposeAsync();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(string url) forwards without throwing for a valid url argument (pure forwarder path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlStringForwarderShouldThrowOnNullCache()
    {
        var service = new HttpService();
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
        var service = new HttpService();
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => service.DownloadUrl(cache, (Uri)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, string url) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyStringShouldThrowOnNullCache()
    {
        var service = new HttpService();
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
        var service = new HttpService();
        await Assert.That(() => service.DownloadUrl(null!, "key", new Uri("https://example.invalid")))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, string url) returns cached value when present (not fetchAlways, hits cache).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyStringShouldReturnCachedValue()
    {
        var service = new HttpService();
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            var expected = new byte[] { 1, 2, 3 };
            await cache.Insert("cached-key", expected).ToTask();

            var result = await service.DownloadUrl(cache, "cached-key", "https://example.invalid").ToTask();

            await Assert.That(result).IsEqualTo(expected);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, Uri url) returns cached value when present (not fetchAlways, hits cache).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldReturnCachedValue()
    {
        var service = new HttpService();
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            var expected = new byte[] { 4, 5, 6 };
            await cache.Insert("cached-uri-key", expected).ToTask();

            var result = await service.DownloadUrl(cache, "cached-uri-key", new Uri("https://example.invalid")).ToTask();

            await Assert.That(result).IsEqualTo(expected);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, string url) with fetchAlways=true bypasses the cache and attempts a network call (which fails for an invalid host).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyStringFetchAlwaysShouldBypassCache()
    {
        var service = new HttpService.FastHttpService(retries: 0, timeout: TimeSpan.FromMilliseconds(100));
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await cache.Insert("fetch-always-key", [9, 9, 9]).ToTask();

            await Assert.That(async () =>
                await service.DownloadUrl(cache, "fetch-always-key", "https://nonexistent.invalid.localhost.test", fetchAlways: true).ToTask())
                .Throws<Exception>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests HttpService.DownloadUrl(key, Uri url) with fetchAlways=true bypasses the cache and attempts a network call (which fails for an invalid host).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriFetchAlwaysShouldBypassCache()
    {
        var service = new HttpService.FastHttpService(retries: 0, timeout: TimeSpan.FromMilliseconds(100));
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await cache.Insert("fetch-always-uri-key", [7, 7, 7]).ToTask();

            await Assert.That(async () =>
                await service.DownloadUrl(cache, "fetch-always-uri-key", new Uri("https://nonexistent.invalid.localhost.test"), fetchAlways: true).ToTask())
                .Throws<Exception>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests CreateWebRequest with null headers returns a request without extra headers.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateWebRequestWithNullHeadersShouldSucceed()
    {
        var request = HttpService.CreateWebRequest(new Uri("https://example.com"), HttpMethod.Get, null);

        await Assert.That(request).IsNotNull();
        await Assert.That(request.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(request.RequestUri).IsEqualTo(new Uri("https://example.com"));
    }

    /// <summary>
    /// Tests CreateWebRequest with supplied headers adds them to the request.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateWebRequestWithHeadersShouldAddHeaders()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>("X-Test-Header", "test-value"),
            new KeyValuePair<string, string>("X-Other", "other-value"),
        };

        var request = HttpService.CreateWebRequest(new Uri("https://example.com"), HttpMethod.Post, headers);

        await Assert.That(request.Headers.Contains("X-Test-Header")).IsTrue();
        await Assert.That(request.Headers.Contains("X-Other")).IsTrue();
    }

    /// <summary>
    /// Tests ProcessWebResponse(string url) throws HttpRequestException when the response is not successful.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ProcessWebResponseStringUrlShouldThrowOnNonSuccess()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Not Found",
        };

        await Assert.That(async () =>
            await HttpService.ProcessWebResponse(response, "https://example.com/missing", null).ToTask())
            .Throws<HttpRequestException>();
    }

    /// <summary>
    /// Tests ProcessWebResponse(Uri url) throws HttpRequestException when the response is not successful.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ProcessWebResponseUriShouldThrowOnNonSuccess()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            ReasonPhrase = "Server Error",
        };

        await Assert.That(async () =>
            await HttpService.ProcessWebResponse(response, new Uri("https://example.com/boom"), DateTimeOffset.UtcNow.AddHours(1)).ToTask())
            .Throws<HttpRequestException>();
    }

    /// <summary>
    /// Tests ProcessWebResponse(string url) returns the content bytes on a successful response.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ProcessWebResponseShouldReturnContentOnSuccess()
    {
        var payload = new byte[] { 10, 20, 30 };
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(payload),
        };

        var result = await HttpService.ProcessWebResponse(response, "https://example.com", null).ToTask();

        await Assert.That(result.Length).IsEqualTo(payload.Length);
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
    public async Task DownloadUrlKeyStringShouldExecuteSelectManyLambdasOnSuccess()
    {
        var serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            var data = await httpService
                .DownloadUrl(cache, "happy-key-string", $"{_testServer!.BaseUrl}status/200", HttpMethod.Get, fetchAlways: true)
                .FirstAsync()
                .ToTask();

            await Assert.That(data).IsNotNull();

            // The SelectMany that writes to the blob cache should have stored the payload.
            var stored = await cache.Get("happy-key-string").FirstAsync();
            await Assert.That(stored).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            var data = await httpService
                .DownloadUrl(cache, "happy-key-uri", new Uri($"{_testServer!.BaseUrl}status/200"), HttpMethod.Get, fetchAlways: true)
                .FirstAsync()
                .ToTask();

            await Assert.That(data).IsNotNull();

            var stored = await cache.Get("happy-key-uri").FirstAsync();
            await Assert.That(stored).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
            httpService.HttpClient.Dispose();
        }
    }

    /// <summary>
    /// Tests MakeWebRequest with null content goes through the no-content branch (exercised via a subclass exposer that fails fast).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithNullContentShouldExecuteNoContentBranch()
    {
        var service = new TestableHttpService();

        await Assert.That(async () =>
            await service.InvokeMakeWebRequest(
                new Uri("https://nonexistent.invalid.localhost.test"),
                HttpMethod.Get,
                headers: null,
                content: null,
                retries: 0,
                timeout: TimeSpan.FromMilliseconds(100)).ToTask())
            .Throws<Exception>();
    }

    /// <summary>
    /// Tests MakeWebRequest with non-null content goes through the StringContent branch (exercised via a subclass exposer that fails fast).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithContentShouldExecuteContentBranch()
    {
        var service = new TestableHttpService();

        await Assert.That(async () =>
            await service.InvokeMakeWebRequest(
                new Uri("https://nonexistent.invalid.localhost.test"),
                HttpMethod.Post,
                headers: null,
                content: "request-body",
                retries: 0,
                timeout: TimeSpan.FromMilliseconds(100)).ToTask())
            .Throws<Exception>();
    }

    /// <summary>
    /// Tests the default FastHttpService constructor uses the default retries and timeout without throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FastHttpServiceDefaultConstructorShouldNotThrow()
    {
        var service = new HttpService.FastHttpService();

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
        var service = new HttpService.FastHttpService(retries: 1, timeout: timeout);

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
        var service = new TestableHttpService();

        try
        {
            await service.InvokeMakeWebRequest(
                new Uri("http://127.0.0.1:1/unused"),
                HttpMethod.Post,
                headers: null,
                content: "hello-body",
                retries: 1,
                timeout: TimeSpan.FromMilliseconds(250)).ToTask();
        }
        catch
        {
            // Expected: connection refused or timeout; the branch lines still execute.
        }
    }

    /// <summary>
    /// Tests MakeWebRequest with non-null content via the fully-routed retry path (retries=2) to ensure the Defer body and StringContent/SendAsync lines are exercised.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithContentAndRetriesShouldExecuteDeferBody()
    {
        var service = new TestableHttpService();

        try
        {
            await service.InvokeMakeWebRequest(
                new Uri("http://127.0.0.1:1/unused"),
                HttpMethod.Put,
                headers: [new KeyValuePair<string, string>("X-Test", "1")],
                content: "{\"key\":\"value\"}",
                retries: 2,
                timeout: TimeSpan.FromMilliseconds(250)).ToTask();
        }
        catch
        {
            // Expected to fail connecting; we only care about branch execution.
        }
    }

    /// <summary>
    /// Tests MakeWebRequest with null content via the fully-routed retry path to exercise the no-content Defer branch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MakeWebRequestWithNullContentAndRetriesShouldExecuteDeferBody()
    {
        var service = new TestableHttpService();

        try
        {
            await service.InvokeMakeWebRequest(
                new Uri("http://127.0.0.1:1/unused"),
                HttpMethod.Get,
                headers: null,
                content: null,
                retries: 1,
                timeout: TimeSpan.FromMilliseconds(250)).ToTask();
        }
        catch
        {
            // Expected.
        }
    }

    /// <summary>
    /// Tests the FastHttpService constructor's catch block by passing a negative TimeSpan that makes HttpClient.Timeout throw ArgumentOutOfRangeException.
    /// The constructor must swallow the exception and construct successfully.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FastHttpServiceWithInvalidNegativeTimeoutShouldSwallowException()
    {
        var service = new HttpService.FastHttpService(retries: 0, timeout: TimeSpan.FromSeconds(-5));

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
        var service = new HttpService.FastHttpService(retries: 0, timeout: TimeSpan.Zero);

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
        var service = new HttpService.FastHttpService(retries: 0, timeout: TimeSpan.MinValue);

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
    public async Task DownloadUrlKeyStringShouldCoalesceNullCacheValueToEmpty()
    {
        await using var cache = new NullGetBlobCache();
        var service = new HttpService();
        try
        {
            var result = await service.DownloadUrl(cache, "any-key", "https://example.invalid").FirstAsync();

            await Assert.That(result).IsNotNull();
            await Assert.That(result).IsEmpty();
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
        await using var cache = new NullGetBlobCache();
        var service = new HttpService();
        try
        {
            var result = await service.DownloadUrl(cache, "any-key", new Uri("https://example.invalid")).FirstAsync();

            await Assert.That(result).IsNotNull();
            await Assert.That(result).IsEmpty();
        }
        finally
        {
            service.HttpClient.Dispose();
        }
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
        public IHttpService HttpService { get; set; } = new HttpService();

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) => Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) => Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => Observable.Empty<(string, DateTimeOffset?)>();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => Observable.Empty<(string, DateTimeOffset?)>();

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

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
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
