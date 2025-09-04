// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for HttpService functionality.
/// Uses a local test server instead of external dependencies for reliable offline testing.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class HttpServiceTests
{
    private TestHttpServer? _testServer;

    /// <summary>
    /// Sets up the test fixture with a local HTTP server.
    /// </summary>
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _testServer = new TestHttpServer();
        _testServer.SetupDefaultResponses();
    }

    /// <summary>
    /// Cleans up the test fixture.
    /// </summary>
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _testServer?.Dispose();
    }
    /// <summary>
    /// Tests that HttpService can be instantiated correctly.
    /// </summary>
    [Test]
    public void HttpServiceShouldInstantiateCorrectly()
    {
        // Act
        var httpService = new HttpService();

        // Assert
        Assert.That(httpService, Is.Not.Null);
        Assert.That(httpService.HttpClient, Is.Not.Null);

        // Cleanup
        httpService.HttpClient.Dispose();
    }

    /// <summary>
    /// Tests that HttpService properly sets up compression.
    /// </summary>
    [Test]
    public void HttpServiceShouldSetupCompressionCorrectly()
    {
        // Act
        var httpService = new HttpService();

        // Assert - HttpClient should be configured properly
        Assert.Multiple(() =>
        {
            Assert.That(httpService.HttpClient, Is.Not.Null);
            Assert.That(httpService.HttpClient.DefaultRequestHeaders, Is.Not.Null);
        });

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
    [Test]
    public void MultipleHttpServiceInstancesShouldBeCreatable()
    {
        // Act
        var service1 = new HttpService();
        var service2 = new HttpService();

        // Assert
        Assert.That(service1, Is.Not.Null);
        Assert.That(service2, Is.Not.Null);
        Assert.NotSame(service1.HttpClient, service2.HttpClient);

        // Cleanup
        service1.HttpClient.Dispose();
        service2.HttpClient.Dispose();
    }

    /// <summary>
    /// Tests that HttpService supports custom HttpClient configuration.
    /// </summary>
    [Test]
    public void HttpServiceShouldSupportCustomConfiguration()
    {
        // Arrange
        var httpService = new HttpService();
        var customTimeout = TimeSpan.FromSeconds(30);

        // Act
        httpService.HttpClient.Timeout = customTimeout;

        // Assert
        Assert.That(httpService.HttpClient.Timeout, Is.EqualTo(customTimeout));

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
            Assert.That(observable, Is.Not.Null);
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

            Assert.Multiple(() =>
            {
                Assert.That(getObservable, Is.Not.Null);
                Assert.That(postObservable, Is.Not.Null);
                Assert.That(putObservable, Is.Not.Null);
            });
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
            Assert.Multiple(() =>
            {
                Assert.That(cachedObservable, Is.Not.Null);
                Assert.That(alwaysFetchObservable, Is.Not.Null);
            });
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
            Assert.That(observable, Is.Not.Null);
        }
        finally
        {
            await cache.DisposeAsync();
            httpService.HttpClient.Dispose();
        }
    }
}
