// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Splat;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Drawing;
#else
namespace Akavache.Drawing;
#endif

/// <summary>Internal helpers that validate and decode raw image buffers into bitmaps.</summary>
internal static class BitmapBufferExtensions
{
    /// <summary>Extension members for <c>byte[]</c>.</summary>
    /// <param name="compressedImage">The encoded image bytes to decode.</param>
    extension(byte[] compressedImage)
    {
        /// <summary>
        /// Decodes <paramref name="compressedImage"/> into an <see cref="IBitmap"/> via the
        /// ambient <see cref="BitmapLoader.Current"/>. Kept as an internal helper so the
        /// bitmap-decode path can be unit-tested in isolation against a mocked loader
        /// without needing a full blob-cache pipeline.
        /// </summary>
        /// <param name="desiredWidth">Optional target width for the decoded bitmap.</param>
        /// <param name="desiredHeight">Optional target height for the decoded bitmap.</param>
        /// <returns>An observable that emits the decoded bitmap or fails with <see cref="IOException"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IObservable<IBitmap> BytesToImage(float? desiredWidth, float? desiredHeight) =>
            Signal.FromAsync(async () =>
            {
#if NETFRAMEWORK
                using var ms = new MemoryStream(compressedImage, writable: false);
#else
                await using MemoryStream ms = new(compressedImage, writable: false);
#endif
                var bitmap = await BitmapLoader.Current.Load(ms, desiredWidth, desiredHeight).ConfigureAwait(false);
                return bitmap ?? throw new IOException("Failed to load the bitmap!");
            });

        /// <summary>Loads a bitmap from raw bytes and returns its dimensions.</summary>
        /// <returns>An observable that emits the image size.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IObservable<Size> LoadBitmapSize() =>
            Signal.FromAsync(async () =>
            {
#if NETFRAMEWORK
                using var ms = new MemoryStream(compressedImage, writable: false);
#else
                await using MemoryStream ms = new(compressedImage, writable: false);
#endif
                var bitmap = await BitmapLoader.Current.Load(ms, null, null).ConfigureAwait(false);
                return bitmap is not null ? new Size(bitmap.Width, bitmap.Height) : throw new InvalidOperationException("Failed to load image for size detection");
            });
    }

    /// <summary>Extension members for <c>byte[]?</c>.</summary>
    /// <param name="buffer">The image buffer to validate; may be <see langword="null"/>.</param>
    extension(byte[]? buffer)
    {
        /// <summary>
        /// Emits <paramref name="buffer"/> through an observable, or signals an
        /// <see cref="InvalidOperationException"/> when the buffer is corrupt — that is,
        /// <see langword="null"/> or smaller than the 64-byte minimum.
        /// </summary>
        /// <returns>An observable emitting the buffer, or signalling an error when invalid.</returns>
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
