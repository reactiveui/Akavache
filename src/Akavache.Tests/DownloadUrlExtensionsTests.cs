// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Sockets;
using System.Reactive.Threading.Tasks;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for download URL extension methods and HTTP functionality.
/// Uses a local test server instead of external dependencies for reliable offline testing.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class DownloadUrlExtensionsTests
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
    /// Tests that DownloadUrl extension method works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task DownloadUrlShouldWorkCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        CacheDatabase.Initialize(() => serializer);

        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache(serializer);

            try
            {
                // Act - Use local test server instead of external service
                var testUrl = $"{_testServer!.BaseUrl}html";
                var bytes = await cache.DownloadUrl(testUrl).FirstAsync();

                // Assert
                Assert.That(bytes.Length, Is.GreaterThan(0));
                
                // Verify content is HTML as expected
                var content = System.Text.Encoding.UTF8.GetString(bytes);
                Assert.That(content, Does.Contain("<html>"));
                Assert.That(content, Does.Contain("Test Content"));
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that DownloadUrl with Uri overload works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task DownloadUrlWithUriShouldWorkCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        CacheDatabase.Initialize(() => serializer);

        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache(serializer);

            try
            {
                // Act - Use local test server
                var uri = new Uri($"{_testServer!.BaseUrl}html");
                var bytes = await cache.DownloadUrl(uri).FirstAsync();

                // Assert
                Assert.Multiple(() =>
                {
                    Assert.That(bytes.Length, Is.GreaterThan(0));
                    
                    var content = System.Text.Encoding.UTF8.GetString(bytes);
                    Assert.That(content, Does.Contain("<html>"));
                    Assert.That(content, Does.Contain("Test Content"));
                });
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that DownloadUrl with key stores the data correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task DownloadUrlWithKeyShouldStoreData()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        CacheDatabase.Initialize(() => serializer);

        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache(serializer);

            try
            {
                // Act - Use local test server
                const string key = "downloaded_content";
                var testUrl = $"{_testServer!.BaseUrl}html";
                await cache.DownloadUrl(key, testUrl).FirstAsync();

                // Assert - verify data was stored
                var storedBytes = await cache.Get(key);
                Assert.Multiple(() =>
                {
                    Assert.That(storedBytes.Length, Is.GreaterThan(0));
                    
                    var content = System.Text.Encoding.UTF8.GetString(storedBytes);
                    Assert.That(content, Does.Contain("<html>"));
                    Assert.That(content, Does.Contain("Test Content"));
                });
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that DownloadUrl with Uri and key stores the data correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task DownloadUrlWithUriAndKeyShouldStoreData()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        CacheDatabase.Initialize(() => serializer);

        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache(serializer);

            try
            {
                // Act - Use local test server
                const string key = "downloaded_uri_content";
                var uri = new Uri($"{_testServer!.BaseUrl}html");
                await cache.DownloadUrl(key, uri).FirstAsync();

                // Assert - verify data was stored
                var storedBytes = await cache.Get(key);
                Assert.Multiple(() =>
                {
                    Assert.That(storedBytes.Length, Is.GreaterThan(0));
                    
                    var content = System.Text.Encoding.UTF8.GetString(storedBytes);
                    Assert.That(content, Does.Contain("<html>"));
                    Assert.That(content, Does.Contain("Test Content"));
                });
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that DownloadUrl handles invalid URLs gracefully.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task DownloadUrlShouldHandleInvalidUrls()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        CacheDatabase.Initialize(() => serializer);

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act & Assert - Use a truly invalid URL that will definitely cause an exception
            // Using an invalid scheme or malformed URL that HttpClient will reject
            var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await cache.DownloadUrl("http://definitely-invalid-domain-that-does-not-exist-12345.invalid").FirstAsync());

            // Verify it's a network-related exception or HTTP-related exception
            Assert.True(
                exception is HttpRequestException ||
                exception is SocketException ||
                exception is TaskCanceledException ||
                exception is TimeoutException ||
                exception is UriFormatException ||
                exception is ArgumentException,
                $"Expected network-related exception, got {exception.GetType().Name}: {exception.Message}");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that DownloadUrl argument validation works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task DownloadUrlShouldValidateArguments()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Test null cache argument validation
            IBlobCache? nullCache = null;
            
            Assert.Multiple(() =>
            {
                // Test null argument validations
                Assert.Throws<ArgumentNullException>(() => nullCache!.DownloadUrl("http://example.com"));
                Assert.Throws<ArgumentNullException>(() => cache.DownloadUrl((Uri)null!));
                Assert.Throws<ArgumentNullException>(() => cache.DownloadUrl((string)null!));
                Assert.Throws<ArgumentNullException>(() => cache.DownloadUrl((string)null!, "http://example.com"));
            });

            // Test empty/whitespace strings
            var emptyUrlException = await Assert.ThrowsAnyAsync<Exception>(async () => await cache.DownloadUrl(string.Empty).FirstAsync());
            var whitespaceUrlException = await Assert.ThrowsAnyAsync<Exception>(async () => await cache.DownloadUrl("   ").FirstAsync());

            Assert.Multiple(() =>
            {
                // Accept various exception types for empty URL
                Assert.That(
                    emptyUrlException is ArgumentException ||
                    emptyUrlException is UriFormatException ||
                    emptyUrlException is InvalidOperationException,
                    $"Expected ArgumentException or UriFormatException for empty URL, got {emptyUrlException.GetType().Name}");

                // Accept various exception types for whitespace URL
                Assert.That(
                    whitespaceUrlException is ArgumentException ||
                    whitespaceUrlException is UriFormatException ||
                    whitespaceUrlException is InvalidOperationException,
                    $"Expected ArgumentException or UriFormatException for whitespace URL, got {whitespaceUrlException.GetType().Name}");
            });
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that multiple concurrent downloads work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ConcurrentDownloadsShouldWork()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        CacheDatabase.Initialize(() => serializer);

        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache(serializer);

            try
            {
                // Act - Use local test server for all concurrent downloads
                var tasks = new[]
                {
                        cache.DownloadUrl("content1", $"{_testServer!.BaseUrl}html").FirstAsync().ToTask(),
                        cache.DownloadUrl("content2", $"{_testServer!.BaseUrl}json").FirstAsync().ToTask(),
                        cache.DownloadUrl("content3", $"{_testServer!.BaseUrl}user-agent").FirstAsync().ToTask()
                };

                await Task.WhenAll(tasks);

                // Assert - verify all downloads completed and data was stored
                Assert.Multiple(() =>
                {
                    foreach (var task in tasks)
                    {
                        Assert.That(task.IsCompletedSuccessfully, Is.True);
                    }
                });

                // Verify data was stored with expected content
                var content1 = await cache.Get("content1");
                var content2 = await cache.Get("content2");
                var content3 = await cache.Get("content3");

                Assert.Multiple(() =>
                {
                    Assert.That(content1.Length, Is.GreaterThan(0));
                    Assert.That(content2.Length, Is.GreaterThan(0));
                    Assert.That(content3.Length, Is.GreaterThan(0));
                    
                    // Verify content types
                    var content1Text = System.Text.Encoding.UTF8.GetString(content1);
                    var content2Text = System.Text.Encoding.UTF8.GetString(content2);
                    var content3Text = System.Text.Encoding.UTF8.GetString(content3);
                    
                    Assert.That(content1Text, Does.Contain("<html>"));
                    Assert.That(content2Text, Does.Contain("\"key\""));
                    Assert.That(content3Text, Does.Contain("user-agent"));
                });
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }
}
