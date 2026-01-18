// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Sockets;
using System.Reactive.Threading.Tasks;

using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Akavache.Tests;

/// <summary>
/// Tests for download URL extension methods and HTTP functionality.
/// Uses a local test server instead of external dependencies for reliable offline testing.
/// </summary>
[Category("Akavache")]
[NotInParallel]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Cleanup is handled via test hooks")]
public class DownloadUrlExtensionsTests
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
                await Assert.That(bytes).IsNotEmpty();

                // Verify content is HTML as expected
                var content = Encoding.UTF8.GetString(bytes);
                await Assert.That(content).Contains("<html>");
                await Assert.That(content).Contains("Test Content");
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
                using (Assert.Multiple())
                {
                    await Assert.That(bytes).IsNotEmpty();

                    var content = Encoding.UTF8.GetString(bytes);
                    await Assert.That(content).Contains("<html>");
                    await Assert.That(content).Contains("Test Content");
                }
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
                using (Assert.Multiple())
                {
                    await Assert.That(storedBytes).IsNotEmpty();

                    var content = Encoding.UTF8.GetString(storedBytes);
                    await Assert.That(content).Contains("<html>");
                    await Assert.That(content).Contains("Test Content");
                }
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
                using (Assert.Multiple())
                {
                    await Assert.That(storedBytes).IsNotEmpty();

                    var content = Encoding.UTF8.GetString(storedBytes);
                    await Assert.That(content).Contains("<html>");
                    await Assert.That(content).Contains("Test Content");
                }
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
            Exception? caughtException = null;
            try
            {
                await cache.DownloadUrl("http://definitely-invalid-domain-that-does-not-exist-12345.invalid").FirstAsync();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            await Assert.That(caughtException).IsNotNull();

            // Verify it's one of the expected network-related exceptions
            var isExpectedType = caughtException is HttpRequestException
                || caughtException is SocketException
                || caughtException is TaskCanceledException
                || caughtException is TimeoutException
                || caughtException is UriFormatException
                || caughtException is ArgumentException;
            await Assert.That(isExpectedType).IsTrue();
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

            using (Assert.Multiple())
            {
                // Test null argument validations
                await Assert.That(() => nullCache!.DownloadUrl("http://example.com")).Throws<ArgumentNullException>();
                await Assert.That(() => cache.DownloadUrl((Uri)null!)).Throws<ArgumentNullException>();
                await Assert.That(() => cache.DownloadUrl((string)null!)).Throws<ArgumentNullException>();
                await Assert.That(() => cache.DownloadUrl(null!, "http://example.com")).Throws<ArgumentNullException>();
            }

            // Test empty/whitespace strings
            Exception? emptyUrlException = null;
            try
            {
                await cache.DownloadUrl(string.Empty).FirstAsync();
            }
            catch (Exception ex)
            {
                emptyUrlException = ex;
            }

            await Assert.That(emptyUrlException).IsNotNull();
            var isEmptyUrlExpected = emptyUrlException is ArgumentException
                || emptyUrlException is UriFormatException
                || emptyUrlException is InvalidOperationException;
            await Assert.That(isEmptyUrlExpected).IsTrue();

            Exception? whitespaceUrlException = null;
            try
            {
                await cache.DownloadUrl("   ").FirstAsync();
            }
            catch (Exception ex)
            {
                whitespaceUrlException = ex;
            }

            await Assert.That(whitespaceUrlException).IsNotNull();
            var isWhitespaceUrlExpected = whitespaceUrlException is ArgumentException
                || whitespaceUrlException is UriFormatException
                || whitespaceUrlException is InvalidOperationException;
            await Assert.That(isWhitespaceUrlExpected).IsTrue();
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
                Task<byte[]>[] tasks =
                [
                    cache.DownloadUrl("content1", $"{_testServer!.BaseUrl}html").FirstAsync().ToTask(),
                    cache.DownloadUrl("content2", $"{_testServer!.BaseUrl}json").FirstAsync().ToTask(),
                    cache.DownloadUrl("content3", $"{_testServer!.BaseUrl}user-agent").FirstAsync().ToTask()
                ];

                await Task.WhenAll(tasks);

                // Assert - verify all downloads completed and data was stored
                using (Assert.Multiple())
                {
                    foreach (var task in tasks)
                    {
                        await Assert.That(task.IsCompletedSuccessfully).IsTrue();
                    }
                }

                // Verify data was stored with expected content
                var content1 = await cache.Get("content1");
                var content2 = await cache.Get("content2");
                var content3 = await cache.Get("content3");

                using (Assert.Multiple())
                {
                    await Assert.That(content1).IsNotEmpty();
                    await Assert.That(content2).IsNotEmpty();
                    await Assert.That(content3).IsNotEmpty();

                    // Verify content types
                    var content1Text = Encoding.UTF8.GetString(content1);
                    var content2Text = Encoding.UTF8.GetString(content2);
                    var content3Text = Encoding.UTF8.GetString(content3);

                    await Assert.That(content1Text).Contains("<html>");
                    await Assert.That(content2Text).Contains("\"key\"");
                    await Assert.That(content3Text).Contains("user-agent");
                }
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }
}
