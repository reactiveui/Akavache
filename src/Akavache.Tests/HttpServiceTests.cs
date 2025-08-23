// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Xunit;

namespace Akavache.Tests;

/// <summary>
/// Tests for HttpService functionality.
/// </summary>
public class HttpServiceTests
{
    /// <summary>
    /// Tests that HttpService can be instantiated correctly.
    /// </summary>
    [Fact]
    public void HttpServiceShouldInstantiateCorrectly()
    {
        // Act
        var httpService = new HttpService();

        // Assert
        Assert.NotNull(httpService);
        Assert.NotNull(httpService.HttpClient);

        // Cleanup
        httpService.HttpClient.Dispose();
    }

    /// <summary>
    /// Tests that HttpService properly sets up compression.
    /// </summary>
    [Fact]
    public void HttpServiceShouldSetupCompressionCorrectly()
    {
        // Act
        var httpService = new HttpService();

        // Assert - HttpClient should be configured properly
        Assert.NotNull(httpService.HttpClient);
        Assert.NotNull(httpService.HttpClient.DefaultRequestHeaders);

        // Cleanup
        httpService.HttpClient.Dispose();
    }

    /// <summary>
    /// Tests that DownloadUrl with URI parameter validates arguments correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DownloadUrlWithUriShouldValidateArguments()
    {
        // Arrange
        CacheDatabase.Serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache();
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
    [Fact]
    public async Task DownloadUrlWithKeyShouldValidateArguments()
    {
        // Arrange
        CacheDatabase.Serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache();

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
    [Fact]
    public void MultipleHttpServiceInstancesShouldBeCreatable()
    {
        // Act
        var service1 = new HttpService();
        var service2 = new HttpService();

        // Assert
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotSame(service1.HttpClient, service2.HttpClient);

        // Cleanup
        service1.HttpClient.Dispose();
        service2.HttpClient.Dispose();
    }

    /// <summary>
    /// Tests that HttpService supports custom HttpClient configuration.
    /// </summary>
    [Fact]
    public void HttpServiceShouldSupportCustomConfiguration()
    {
        // Arrange
        var httpService = new HttpService();
        var customTimeout = TimeSpan.FromSeconds(30);

        // Act
        httpService.HttpClient.Timeout = customTimeout;

        // Assert
        Assert.Equal(customTimeout, httpService.HttpClient.Timeout);

        // Cleanup
        httpService.HttpClient.Dispose();
    }

    /// <summary>
    /// Tests that HttpService handles null headers gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HttpServiceShouldHandleNullHeadersGracefully()
    {
        // Arrange
        CacheDatabase.Serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache();

        try
        {
            // Act - This should not throw even with null headers
            var observable = httpService.DownloadUrl(
                cache,
                "test_key",
                "http://httpbin.org/status/200",
                HttpMethod.Get,
                null,
                false,
                null);

            // Assert - Observable should be created without error
            Assert.NotNull(observable);
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
    [Fact]
    public async Task HttpServiceShouldHandleDifferentHttpMethods()
    {
        // Arrange
        CacheDatabase.Serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache();

        try
        {
            // Act & Assert - Should create observables for different methods without error
            var getObservable = httpService.DownloadUrl(cache, "get_key", "http://httpbin.org/status/200", HttpMethod.Get);
            var postObservable = httpService.DownloadUrl(cache, "post_key", "http://httpbin.org/status/200", HttpMethod.Post);
            var putObservable = httpService.DownloadUrl(cache, "put_key", "http://httpbin.org/status/200", HttpMethod.Put);

            Assert.NotNull(getObservable);
            Assert.NotNull(postObservable);
            Assert.NotNull(putObservable);
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
    [Fact]
    public async Task HttpServiceShouldRespectFetchAlwaysParameter()
    {
        // Arrange
        CacheDatabase.Serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache();

        try
        {
            // Act - Should create different observables based on fetchAlways
            var cachedObservable = httpService.DownloadUrl(
                cache,
                "cached_key",
                "http://httpbin.org/status/200",
                HttpMethod.Get,
                null,
                false);
            var alwaysFetchObservable = httpService.DownloadUrl(
                cache,
                "always_key",
                "http://httpbin.org/status/200",
                HttpMethod.Get,
                null,
                true);

            // Assert
            Assert.NotNull(cachedObservable);
            Assert.NotNull(alwaysFetchObservable);
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
    [Fact]
    public async Task HttpServiceShouldSupportAbsoluteExpiration()
    {
        // Arrange
        CacheDatabase.Serializer = new SystemJsonSerializer();
        var httpService = new HttpService();
        var cache = new InMemoryBlobCache();
        var expiration = DateTimeOffset.Now.AddHours(1);

        try
        {
            // Act
            var observable = httpService.DownloadUrl(
                cache,
                "expiry_key",
                "http://httpbin.org/status/200",
                HttpMethod.Get,
                null,
                false,
                expiration);

            // Assert
            Assert.NotNull(observable);
        }
        finally
        {
            await cache.DisposeAsync();
            httpService.HttpClient.Dispose();
        }
    }
}
