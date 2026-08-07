// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests for HttpExtensions covering null/empty argument validation and stream paths.</summary>
[Category("Akavache")]
public class HttpExtensionsTests
{
    /// <summary>A well-formed absolute URL used wherever a test only needs a syntactically valid address.</summary>
    private const string SampleUrl = "https://example.com";

    /// <summary>How long a test waits for a <c>WriteAsyncRx</c> subscription to signal success or failure.</summary>
    private const int WriteCompletionTimeoutSeconds = 5;

    /// <summary>Tests WriteAsyncRx throws on null stream.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WriteAsyncRxShouldThrowOnNullStream()
    {
        byte[] data = [1, 2, 3];

        await Assert.That(() => HttpExtensions.WriteAsyncRx(null!, data, 0, data.Length))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WriteAsyncRx writes to a memory stream.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WriteAsyncRxShouldWriteToStream()
    {
        await using MemoryStream stream = new();
        byte[] data = [1, 2, 3, 4, 5];

        ManualResetEventSlim mre = new(false);
        _ = stream.WriteAsyncRx(data, 0, data.Length).Subscribe(
            _ => mre.Set(),
            _ => mre.Set());
        _ = mre.Wait(TimeSpan.FromSeconds(WriteCompletionTimeoutSeconds));

        await Assert.That(stream.Length).IsEqualTo(data.Length);
    }

    /// <summary>Tests WriteAsyncRx propagates exceptions when writing fails.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WriteAsyncRxShouldPropagateException()
    {
        await using ThrowingStream stream = new();

        Exception? error = null;
        ManualResetEventSlim mre = new(false);
        _ = stream.WriteAsyncRx([1], 0, 1).Subscribe(
            _ => mre.Set(),
            ex =>
            {
                error = ex;
                mre.Set();
            });
        _ = mre.Wait(TimeSpan.FromSeconds(WriteCompletionTimeoutSeconds));
        await Assert.That(error).IsNotNull();
    }

    /// <summary>Tests DownloadUrl(string url) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlStringShouldThrowOnNullCache() =>
        await Assert.That(static () => HttpExtensions.DownloadUrl(null!, SampleUrl))
            .Throws<ArgumentNullException>();

    /// <summary>Tests DownloadUrl(string url) throws on null url.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlStringShouldThrowOnNullUrl()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl((string)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DownloadUrl(string url) throws on empty url.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlStringShouldThrowOnEmptyUrl()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl(string.Empty))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DownloadUrl(Uri url) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlUriShouldThrowOnNullCache() =>
        await Assert.That(static () => HttpExtensions.DownloadUrl(null!, new Uri(SampleUrl)))
            .Throws<ArgumentNullException>();

    /// <summary>Tests DownloadUrl(Uri url) throws on null url.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlUriShouldThrowOnNullUrl()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl((Uri)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DownloadUrl(key, string url) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldThrowOnNullCache() =>
        await Assert.That(static () => HttpExtensions.DownloadUrl(null!, "key", SampleUrl))
            .Throws<ArgumentNullException>();

    /// <summary>Tests DownloadUrl(key, string url) throws on null key.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldThrowOnNullKey()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl(null!, SampleUrl))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DownloadUrl(key, string url) throws on empty key.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldThrowOnEmptyKey()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl(string.Empty, SampleUrl))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DownloadUrl(key, string url) throws on null url.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldThrowOnNullUrl()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl("key", (string)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DownloadUrl(key, string url) throws on empty url.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlKeyStringShouldThrowOnEmptyUrl()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl("key", string.Empty))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DownloadUrl(key, Uri url) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldThrowOnNullCache() =>
        await Assert.That(static () => HttpExtensions.DownloadUrl(null!, "key", new Uri(SampleUrl)))
            .Throws<ArgumentNullException>();

    /// <summary>Tests DownloadUrl(key, Uri url) throws on null key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldThrowOnNullKey()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl(null!, new Uri(SampleUrl)))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DownloadUrl(key, Uri url) throws on empty key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldThrowOnEmptyKey()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl(string.Empty, new Uri(SampleUrl)))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DownloadUrl(key, Uri url) throws on null url.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlKeyUriShouldThrowOnNullUrl()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.DownloadUrl("key", (Uri)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests WriteAsyncRx handles EndWrite throwing.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WriteAsyncRxShouldHandleEndWriteFailure()
    {
        await using EndWriteThrowingStream stream = new();
        byte[] data = [1, 2, 3];

        Exception? error = null;
        ManualResetEventSlim mre = new(false);
        _ = stream.WriteAsyncRx(data, 0, data.Length).Subscribe(
            _ => mre.Set(),
            ex =>
            {
                error = ex;
                mre.Set();
            });
        _ = mre.Wait(TimeSpan.FromSeconds(WriteCompletionTimeoutSeconds));
        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>A stream whose <see cref="BeginWrite"/> always throws, used to exercise the BeginWrite failure path.</summary>
    private sealed class ThrowingStream : MemoryStream
    {
        /// <inheritdoc/>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            throw new InvalidOperationException("Throwing stream");
    }

    /// <summary>A stream that allows BeginWrite to succeed but throws on EndWrite, exercising the inner catch block of WriteAsyncRx.</summary>
    private sealed class EndWriteThrowingStream : MemoryStream
    {
        /// <inheritdoc/>
        public override void EndWrite(IAsyncResult asyncResult) =>
            throw new InvalidOperationException("EndWrite failure");
    }
}
