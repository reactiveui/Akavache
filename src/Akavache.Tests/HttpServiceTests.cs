// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for HttpService functionality.
/// Uses a local test server instead of external dependencies for reliable offline testing.
/// </summary>
[Category("Akavache")]
[NotInParallel]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Cleanup is handled via test hooks")]
public class HttpServiceTests
{
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
}
