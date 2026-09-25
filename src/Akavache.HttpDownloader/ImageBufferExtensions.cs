// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>Internal guards that validate raw image buffers before they are handed to callers.</summary>
internal static class ImageBufferExtensions
{
    /// <summary>Extension members for <c>byte[]?</c>.</summary>
    /// <param name="buffer">The image buffer to validate; may be <see langword="null"/>.</param>
    extension(byte[]? buffer)
    {
        /// <summary>
        /// Emits <paramref name="buffer"/> through an observable, or signals an
        /// <see cref="InvalidOperationException"/> when the buffer is corrupt — that is,
        /// <see langword="null"/> or smaller than the 64-byte minimum.
        /// </summary>
        /// <returns>An observable that emits the byte array if valid, or signals an error if the buffer is corrupt.</returns>
        internal IObservable<byte[]> ThrowOnBadImageBuffer() =>
            buffer is null || buffer.Length < 64
                ? new ImmediateThrowSignal<byte[]>(new InvalidOperationException("Invalid Image"))
                : Signal.Return(buffer);

        /// <summary>
        /// Routes a potentially null byte buffer from a blob cache through the
        /// bad-image guard, emitting a descriptive <c>"Image data is null"</c> error
        /// when the buffer itself is <see langword="null"/>.
        /// </summary>
        /// <returns>An observable emitting <paramref name="buffer"/>, or an error.</returns>
        internal IObservable<byte[]> ThrowOnNullOrBadImageBuffer() =>
            buffer is null
                ? new ImmediateThrowSignal<byte[]>(new InvalidOperationException("Image data is null"))
                : buffer.ThrowOnBadImageBuffer();
    }
}
