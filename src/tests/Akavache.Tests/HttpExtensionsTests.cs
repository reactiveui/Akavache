// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Reactive.Threading.Tasks;
using Akavache.Core;
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for HttpExtensions covering null/empty argument validation and stream paths.
/// </summary>
[Category("Akavache")]
public class HttpExtensionsTests
{
    /// <summary>
    /// Tests WriteAsyncRx throws on null stream.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WriteAsyncRxShouldThrowOnNullStream() =>
        await Assert.That(static () => HttpExtensions.WriteAsyncRx(null!, [1, 2, 3], 0, 3))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WriteAsyncRx writes to a memory stream.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WriteAsyncRxShouldWriteToStream()
    {
        using var stream = new MemoryStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };

        await stream.WriteAsyncRx(data, 0, data.Length).ToTask();

        await Assert.That(stream.Length).IsEqualTo(5);
    }

    /// <summary>
    /// Tests WriteAsyncRx propagates exceptions when writing fails.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WriteAsyncRxShouldPropagateException()
    {
        await using var stream = new ThrowingStream();

        await Assert.That(async () => await stream.WriteAsyncRx([1], 0, 1).ToTask())
            .Throws<Exception>();
    }

    /// <summary>
    /// Tests DownloadUrl(string url) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlStringShouldThrowOnNullCache() =>
        await Assert.That(static () => HttpExtensions.DownloadUrl(null!, "https://example.com"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests DownloadUrl(string url) throws on null url.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlStringShouldThrowOnNullUrl()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl((string)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests DownloadUrl(string url) throws on empty url.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlStringShouldThrowOnEmptyUrl()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl(string.Empty))
                .Throws<ArgumentException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests DownloadUrl(Uri url) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlUriShouldThrowOnNullCache() =>
        await Assert.That(static () => HttpExtensions.DownloadUrl(null!, new Uri("https://example.com")))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests DownloadUrl(Uri url) throws on null url.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlUriShouldThrowOnNullUrl()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl((Uri)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests DownloadUrl(key, string url) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyStringShouldThrowOnNullCache() =>
        await Assert.That(static () => HttpExtensions.DownloadUrl(null!, "key", "https://example.com"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests DownloadUrl(key, string url) throws on null key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyStringShouldThrowOnNullKey()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl(null!, "https://example.com"))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests DownloadUrl(key, string url) throws on empty key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyStringShouldThrowOnEmptyKey()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl(string.Empty, "https://example.com"))
                .Throws<ArgumentException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests DownloadUrl(key, string url) throws on null url.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyStringShouldThrowOnNullUrl()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl("key", (string)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests DownloadUrl(key, string url) throws on empty url.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyStringShouldThrowOnEmptyUrl()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl("key", string.Empty))
                .Throws<ArgumentException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests DownloadUrl(key, Uri url) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldThrowOnNullCache() =>
        await Assert.That(static () => HttpExtensions.DownloadUrl(null!, "key", new Uri("https://example.com")))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests DownloadUrl(key, Uri url) throws on null key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldThrowOnNullKey()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl(null!, new Uri("https://example.com")))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests DownloadUrl(key, Uri url) throws on empty key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldThrowOnEmptyKey()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl(string.Empty, new Uri("https://example.com")))
                .Throws<ArgumentException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests DownloadUrl(key, Uri url) throws on null url.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldThrowOnNullUrl()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl("key", (Uri)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests WriteAsyncRx handles EndWrite throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WriteAsyncRxShouldHandleEndWriteFailure()
    {
        await using var stream = new EndWriteThrowingStream();
        var data = new byte[] { 1, 2, 3 };

        await Assert.That(async () => await stream.WriteAsyncRx(data, 0, data.Length).ToTask())
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// A stream whose <see cref="BeginWrite"/> always throws, used to exercise the BeginWrite failure path.
    /// </summary>
    private sealed class ThrowingStream : MemoryStream
    {
        /// <inheritdoc/>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            throw new InvalidOperationException("Throwing stream");
    }

    /// <summary>
    /// A stream that allows BeginWrite to succeed but throws on EndWrite,
    /// exercising the inner catch block of WriteAsyncRx.
    /// </summary>
    private sealed class EndWriteThrowingStream : MemoryStream
    {
        /// <inheritdoc/>
        public override void EndWrite(IAsyncResult asyncResult) =>
            throw new InvalidOperationException("EndWrite failure");
    }
}
