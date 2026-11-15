// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Checks the stream contract on modern and legacy implementations.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class ReadOnlyMemoryStreamCompatTests
{
    /// <summary>Reads the original buffer, supports seeking and reports end of input.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenRead_ReadsAndSeeks(CancellationToken cancellationToken)
    {
        byte[] bytes = [10, 20, 30];
        await using var stream = ReadOnlyMemoryStreamCompat.OpenRead(bytes);
        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(stream.CanSeek).IsTrue();
        await Assert.That(stream.CanWrite).IsFalse();
        await Assert.That(stream.Length).IsEqualTo(bytes.LongLength);
        await Assert.That(stream.Position).IsEqualTo(0L);

        var output = new byte[bytes.Length];
        var count = await stream.ReadAsync(output, cancellationToken).ConfigureAwait(false);
        await Assert.That(count).IsEqualTo(bytes.Length);
        await Assert.That(output).IsEquivalentTo(bytes);
        await Assert.That(stream.ReadByte()).IsEqualTo(-1);
        await Assert.That(stream.Seek(-1, SeekOrigin.End)).IsEqualTo(bytes.LongLength - 1);
        await Assert.That(stream.ReadByte()).IsEqualTo((int)bytes[^1]);
    }

    /// <summary>Rejects writes and resizing without changing the supplied array.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenRead_RejectsMutation()
    {
        byte[] bytes = [10];
        await using var stream = ReadOnlyMemoryStreamCompat.OpenRead(bytes);
        var original = bytes[0];
        await Assert.That(() => stream.WriteByte(byte.MaxValue)).Throws<NotSupportedException>();
        await Assert.That(() => stream.SetLength(0)).Throws<NotSupportedException>();
        await Assert.That(bytes[0]).IsEqualTo(original);
    }

    /// <summary>Creates separate positions over the same array without a defensive copy.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenRead_SharesBufferWithIndependentPositions()
    {
        byte[] bytes = [10, 20];
        await using var first = ReadOnlyMemoryStreamCompat.OpenRead(bytes);
        await using var second = ReadOnlyMemoryStreamCompat.OpenRead(bytes);
        bytes[0] = byte.MaxValue;
        await Assert.That(first.ReadByte()).IsEqualTo((int)bytes[0]);
        await Assert.That(second.Position).IsEqualTo(0L);
        await Assert.That(second.ReadByte()).IsEqualTo((int)bytes[0]);
    }

    /// <summary>Accepts empty input and rejects a null array consistently across targets.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenRead_ValidatesInput()
    {
        await using var empty = ReadOnlyMemoryStreamCompat.OpenRead([]);
        await Assert.That(empty.Length).IsEqualTo(0L);
        await Assert.That(empty.ReadByte()).IsEqualTo(-1);
        await Assert.That(static () => ReadOnlyMemoryStreamCompat.OpenRead(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Disposal prevents further reads without modifying the backing array.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenRead_DisposalClosesStream()
    {
        byte[] bytes = [10];
        var original = bytes[0];
        var stream = ReadOnlyMemoryStreamCompat.OpenRead(bytes);
        await stream.DisposeAsync().ConfigureAwait(false);
        await Assert.That(() => stream.ReadByte()).Throws<ObjectDisposedException>();
        await Assert.That(bytes[0]).IsEqualTo(original);
    }
}
