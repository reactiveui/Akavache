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

/// <summary>
/// Buffer guards and decode steps shared by the bitmap and image-cache extensions. These act on a
/// byte buffer rather than a receiver, so they stay helpers instead of being published as
/// extensions on <see cref="byte"/>[]. Kept internal-but-testable so the decode path can be
/// exercised against a mocked <see cref="BitmapLoader"/> without a full blob-cache pipeline.
/// </summary>
internal static class BitmapHelpers
{
    /// <summary>
    /// Emits <paramref name="compressedImage"/> through an observable, or signals an
    /// <see cref="InvalidOperationException"/> when the buffer is corrupt — that is,
    /// <see langword="null"/> or smaller than the 64-byte minimum.
    /// </summary>
    /// <param name="compressedImage">The compressed image buffer to check.</param>
    /// <returns>An observable emitting the buffer, or signalling an error when invalid.</returns>
    internal static IObservable<byte[]> ThrowOnBadImageBuffer(byte[]? compressedImage) =>
        compressedImage is null || compressedImage.Length < 64
            ? new ImmediateThrowSignal<byte[]>(new InvalidOperationException("Invalid Image"))
            : Signal.Return(compressedImage);

    /// <summary>
    /// Routes a potentially null byte buffer from a blob cache through the
    /// bad-image guard, emitting a descriptive <c>"Image data is null"</c> error
    /// when the buffer itself is <see langword="null"/>.
    /// </summary>
    /// <param name="bytes">The bytes returned by the blob cache, possibly <see langword="null"/>.</param>
    /// <returns>An observable emitting <paramref name="bytes"/>, or an error.</returns>
    internal static IObservable<byte[]> ThrowOnNullOrBadImageBuffer(byte[]? bytes) =>
        bytes is null
            ? new ImmediateThrowSignal<byte[]>(new InvalidOperationException("Image data is null"))
            : ThrowOnBadImageBuffer(bytes);

    /// <summary>Converts a compressed image byte array into an <see cref="IBitmap"/> using Splat's ambient <see cref="BitmapLoader.Current"/>.</summary>
    /// <remarks>
    /// Throws <see cref="IOException"/> when the loader returns
    /// <see langword="null"/>.
    /// </remarks>
    /// <param name="compressedImage">The compressed image bytes.</param>
    /// <param name="desiredWidth">Optional desired width.</param>
    /// <param name="desiredHeight">Optional desired height.</param>
    /// <returns>An observable emitting the decoded bitmap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IObservable<IBitmap> BytesToImage(byte[] compressedImage, float? desiredWidth, float? desiredHeight) =>
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
    /// <param name="bytes">The encoded image bytes.</param>
    /// <returns>An observable that emits the image size.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IObservable<Size> LoadBitmapSize(byte[] bytes) =>
        Signal.FromAsync(async () =>
        {
#if NETFRAMEWORK
            using var ms = new MemoryStream(bytes, writable: false);
#else
            await using var ms = new MemoryStream(bytes, writable: false);
#endif
            var bitmap = await BitmapLoader.Current.Load(ms, null, null).ConfigureAwait(false);
            return bitmap is not null ? new Size(bitmap.Width, bitmap.Height) : throw new InvalidOperationException("Failed to load image for size detection");
        });
}
