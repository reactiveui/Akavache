// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Akavache.Tests;
using Akavache.Tests.Helpers;

namespace Akavache.Integration.Tests;

/// <summary>
/// Tests for the DownloadUrl extension methods on <see cref="IBlobCache"/>.
/// Uses a local <see cref="TestHttpServer"/> to avoid external network dependencies.
/// </summary>
[Category("Akavache")]
[NotInParallel("CacheDatabaseState")]
public class DownloadUrlExtensionsTests
{
    /// <summary>
    /// DownloadUrl(string) should throw on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlShouldValidateArguments()
    {
        IBlobCache? nullCache = null;

        // Null cache
        await Assert.That(() => nullCache!.DownloadUrl("http://example.com")).Throws<ArgumentNullException>();

        // Null/empty URL
        using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        await Assert.That(() => cache.DownloadUrl((string)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => cache.DownloadUrl(string.Empty)).Throws<ArgumentException>();

        // Null URI
        await Assert.That(() => cache.DownloadUrl((Uri)null!)).Throws<ArgumentNullException>();

        // Key+URL nulls
        await Assert.That(() => cache.DownloadUrl(null!, "http://example.com")).Throws<ArgumentNullException>();
        await Assert.That(() => cache.DownloadUrl("key", (string)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => cache.DownloadUrl("key", (Uri)null!)).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// DownloadUrl(string) should download and cache content from a local server.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlStringShouldDownloadAndCache()
    {
        using var server = new TestHttpServer();
        server.SetupDefaultResponses();

        using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        var bytes = cache.DownloadUrl(server.BaseUrl + "html").WaitForValue();

        await Assert.That(bytes).IsNotNull();
        await Assert.That(bytes).IsNotEmpty();
    }

    /// <summary>
    /// DownloadUrl(Uri) should download and cache content from a local server.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlUriShouldDownloadAndCache()
    {
        using var server = new TestHttpServer();
        server.SetupDefaultResponses();

        using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        Uri uri = new(server.BaseUrl + "html");
        var bytes = cache.DownloadUrl(uri).WaitForValue();

        await Assert.That(bytes).IsNotNull();
        await Assert.That(bytes).IsNotEmpty();
    }

    /// <summary>
    /// DownloadUrl(key, string) should store content under the explicit key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyStringShouldStoreUnderExplicitKey()
    {
        using var server = new TestHttpServer();
        server.SetupDefaultResponses();

        using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        const string key = "my-download-key";
        cache.DownloadUrl(key, server.BaseUrl + "html").WaitForValue();

        var storedBytes = cache.Get(key).SubscribeGetValue();
        await Assert.That(storedBytes).IsNotNull();
        await Assert.That(storedBytes).IsNotEmpty();
    }

    /// <summary>
    /// DownloadUrl(key, Uri) should store content under the explicit key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldStoreUnderExplicitKey()
    {
        using var server = new TestHttpServer();
        server.SetupDefaultResponses();

        using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        const string key = "my-uri-key";
        Uri uri = new(server.BaseUrl + "html");
        cache.DownloadUrl(key, uri).WaitForValue();

        var storedBytes = cache.Get(key).SubscribeGetValue();
        await Assert.That(storedBytes).IsNotNull();
        await Assert.That(storedBytes).IsNotEmpty();
    }

    /// <summary>
    /// DownloadUrl with fetchAlways=true (string key + string url) should bypass cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyStringFetchAlwaysShouldBypassCache()
    {
        using var server = new TestHttpServer();
        server.SetupDefaultResponses();

        using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        const string key = "fetch-always-key";

        // First download — populates cache
        cache.DownloadUrl(key, server.BaseUrl + "html", fetchAlways: false).WaitForValue();

        // Change server response
        server.SetupResponse("/html", "<html><body>Updated</body></html>");

        // Second download with fetchAlways — should get updated content
        var bytes = cache.DownloadUrl(key, server.BaseUrl + "html", fetchAlways: true).WaitForValue();

        await Assert.That(bytes).IsNotNull();
        await Assert.That(bytes).IsNotEmpty();
    }

    /// <summary>
    /// DownloadUrl with fetchAlways=true (string key + Uri) should bypass cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriFetchAlwaysShouldBypassCache()
    {
        using var server = new TestHttpServer();
        server.SetupDefaultResponses();

        using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        const string key = "fetch-always-uri-key";
        Uri uri = new(server.BaseUrl + "html");

        cache.DownloadUrl(key, uri, fetchAlways: false).WaitForValue();

        server.SetupResponse("/html", "<html><body>Updated</body></html>");

        var bytes = cache.DownloadUrl(key, uri, fetchAlways: true).WaitForValue();

        await Assert.That(bytes).IsNotNull();
        await Assert.That(bytes).IsNotEmpty();
    }

    /// <summary>
    /// Multiple concurrent downloads to different keys should all complete.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlMultipleKeysShouldAllSucceed()
    {
        using var server = new TestHttpServer();
        server.SetupResponse("/content1", "Content One");
        server.SetupResponse("/content2", "Content Two");
        server.SetupResponse("/content3", "Content Three");

        using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());

        cache.DownloadUrl("content1", server.BaseUrl + "content1").WaitForValue();
        cache.DownloadUrl("content2", server.BaseUrl + "content2").WaitForValue();
        cache.DownloadUrl("content3", server.BaseUrl + "content3").WaitForValue();

        var c1 = cache.Get("content1").SubscribeGetValue();
        var c2 = cache.Get("content2").SubscribeGetValue();
        var c3 = cache.Get("content3").SubscribeGetValue();

        await Assert.That(c1).IsNotNull();
        await Assert.That(c2).IsNotNull();
        await Assert.That(c3).IsNotNull();
    }
}
