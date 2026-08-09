// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>
/// Buffer guards and format probes behind the image extensions. These validate a byte array rather
/// than acting on a receiver, so they stay helpers instead of being published as extensions on
/// <see cref="byte"/>[] for every consumer.
/// </summary>
internal static class ImageBufferHelpers
{
    /// <summary>Offset of the WebP format marker, past the RIFF header and the four-byte chunk size.</summary>
    private const int WebPMarkerOffset = 8;

    /// <summary>Smallest buffer that can carry both WebP markers.</summary>
    private const int WebPPrefixLength = 12;

    /// <summary>Gets the RIFF container header that a WebP file opens with.</summary>
    private static ReadOnlySpan<byte> RiffHeader => "RIFF"u8;

    /// <summary>Gets the format marker that follows the RIFF chunk size in a WebP file.</summary>
    private static ReadOnlySpan<byte> WebPHeader => "WEBP"u8;

    /// <summary>
    /// Emits <paramref name="compressedImage"/> through an observable, or signals an
    /// <see cref="InvalidOperationException"/> when the buffer is corrupt — that is,
    /// <see langword="null"/> or smaller than the 64-byte minimum.
    /// </summary>
    /// <param name="compressedImage">The compressed image buffer to validate.</param>
    /// <returns>An observable that emits the byte array if valid, or signals an error if the buffer is corrupt.</returns>
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

    /// <summary>Returns true if the image data is in WebP format.</summary>
    /// <param name="imageBytes">The image bytes.</param>
    /// <returns>True if it is WebP.</returns>
    internal static bool IsWebP(byte[] imageBytes) =>
        imageBytes.Length >= WebPPrefixLength
        && imageBytes.AsSpan(0, RiffHeader.Length).SequenceEqual(RiffHeader)
        && imageBytes.AsSpan(WebPMarkerOffset, WebPHeader.Length).SequenceEqual(WebPHeader);
}
