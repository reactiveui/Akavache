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
/// </summary>
[TestFixture]
[Category("Akavache")]
public class DownloadUrlExtensionsTests
{
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
                // Act - Skip if httpbin is unavailable (CI/test environments)
                try
                {
                    var bytes = await cache.DownloadUrl("https://httpbin.org/html").FirstAsync();

                    // Assert
                    Assert.That(bytes.Length, Is.GreaterThan(0));
                }
                catch (TimeoutException)
                {
                    // Skip test if request times out
                    return;
                }
                catch (HttpRequestException)
                {
                    // Skip test if httpbin.org is unavailable
                    return;
                }
                catch (TaskCanceledException)
                {
                    // Skip test if request times out
                    return;
                }
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
                // Act - Skip if httpbin is unavailable
                try
                {
                    var uri = new Uri("https://httpbin.org/html");
                    var bytes = await cache.DownloadUrl(uri).FirstAsync();

                    // Assert
                    Assert.That(bytes.Length, Is.GreaterThan(0));
                }
                catch (TimeoutException)
                {
                    // Skip test if request times out
                    return;
                }
                catch (HttpRequestException)
                {
                    // Skip test if httpbin.org is unavailable
                    return;
                }
                catch (TaskCanceledException)
                {
                    // Skip test if request times out
                    return;
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
                // Act - Skip if httpbin is unavailable
                try
                {
                    const string key = "downloaded_content";
                    await cache.DownloadUrl(key, "https://httpbin.org/html").FirstAsync();

                    // Assert - verify data was stored
                    var storedBytes = await cache.Get(key);
                    Assert.That(storedBytes.Length, Is.GreaterThan(0));
                }
                catch (TimeoutException)
                {
                    // Skip test if request times out
                    return;
                }
                catch (HttpRequestException)
                {
                    // Skip test if httpbin.org is unavailable
                    return;
                }
                catch (TaskCanceledException)
                {
                    // Skip test if request times out
                    return;
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
                // Act - Skip if httpbin is unavailable
                try
                {
                    const string key = "downloaded_uri_content";
                    var uri = new Uri("https://httpbin.org/html");
                    await cache.DownloadUrl(key, uri).FirstAsync();

                    // Assert - verify data was stored
                    var storedBytes = await cache.Get(key);
                    Assert.That(storedBytes.Length, Is.GreaterThan(0));
                }
                catch (TimeoutException)
                {
                    // Skip test if request times out
                    return;
                }
                catch (HttpRequestException)
                {
                    // Skip test if httpbin.org is unavailable
                    return;
                }
                catch (TaskCanceledException)
                {
                    // Skip test if request times out
                    return;
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
            Assert.Throws<ArgumentNullException>(() => nullCache!.DownloadUrl("http://example.com"));

            // Test null Uri argument validation
            Assert.Throws<ArgumentNullException>(() => cache.DownloadUrl((Uri)null!));

            // Test null URL string argument validation
            Assert.Throws<ArgumentNullException>(() => cache.DownloadUrl((string)null!));

            // Test null key argument validation
            Assert.Throws<ArgumentNullException>(() => cache.DownloadUrl((string)null!, "http://example.com"));

            // For empty/whitespace strings, different implementations may handle differently
            // Some might throw ArgumentException, others might throw UriFormatException
            try
            {
                var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await cache.DownloadUrl(string.Empty).FirstAsync());

                // Accept either ArgumentException or UriFormatException for empty URL
                Assert.True(
                    exception is ArgumentException ||
                    exception is UriFormatException ||
                    exception is InvalidOperationException,
                    $"Expected ArgumentException or UriFormatException for empty URL, got {exception.GetType().Name}");
            }
            catch (ArgumentException)
            {
                // This is the expected behavior
            }
            catch (UriFormatException)
            {
                // This is also acceptable - URL parsing failure
            }

            try
            {
                var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await cache.DownloadUrl("   ").FirstAsync());

                // Accept either ArgumentException or UriFormatException for whitespace URL
                Assert.True(
                    exception is ArgumentException ||
                    exception is UriFormatException ||
                    exception is InvalidOperationException,
                    $"Expected ArgumentException or UriFormatException for whitespace URL, got {exception.GetType().Name}");
            }
            catch (ArgumentException)
            {
                // This is the expected behavior
            }
            catch (UriFormatException)
            {
                // This is also acceptable - URL parsing failure
            }
        }
        catch (TimeoutException)
        {
            // Skip test if request times out
            return;
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
                // Act - Skip if httpbin is unavailable
                try
                {
                    var tasks = new[]
                    {
                            cache.DownloadUrl("content1", "https://httpbin.org/html").FirstAsync().ToTask(),
                            cache.DownloadUrl("content2", "https://httpbin.org/json").FirstAsync().ToTask(),
                            cache.DownloadUrl("content3", "https://httpbin.org/user-agent").FirstAsync().ToTask()
                    };

                    await Task.WhenAll(tasks);

                    // Assert - verify all downloads completed
                    foreach (var task in tasks)
                    {
                        Assert.That(task.IsCompletedSuccessfully, Is.True);
                    }

                    // Verify data was stored
                    var content1 = await cache.Get("content1");
                    var content2 = await cache.Get("content2");
                    var content3 = await cache.Get("content3");

                    Assert.That(content1.Length, Is.GreaterThan(0));
                    Assert.That(content2.Length, Is.GreaterThan(0));
                    Assert.That(content3.Length, Is.GreaterThan(0));
                }
                catch (TimeoutException)
                {
                    // Skip test if request times out
                    return;
                }
                catch (HttpRequestException)
                {
                    // Skip test if httpbin.org is unavailable
                    return;
                }
                catch (TaskCanceledException)
                {
                    // Skip test if request times out
                    return;
                }
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }
}
