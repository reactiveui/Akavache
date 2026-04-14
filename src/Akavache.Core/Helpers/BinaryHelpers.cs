// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET6_0_OR_GREATER
using System.Buffers.Binary;
#endif
using System.Runtime.CompilerServices;

namespace Akavache.Helpers;

/// <summary>
/// Endian-explicit binary helpers wrapping <c>System.Buffers.Binary.BinaryPrimitives</c>
/// where available, with a hand-rolled little-endian decoder for legacy targets that lack the
/// <c>System.Buffers.Binary</c> namespace. Aggressively inlined so the dispatch and bit-shift
/// reconstruction collapse to a single load on the hot path.
/// </summary>
internal static class BinaryHelpers
{
#if NET5_0_OR_GREATER
    /// <summary>Gets the ASCII whitespace byte set used by <see cref="StartsWithJsonOpener"/>'s
    /// fast-path span trim — space, tab, LF, CR. Stored as a <c>u8</c> literal so the bytes
    /// live in the assembly's data section, zero runtime allocation.</summary>
    private static ReadOnlySpan<byte> AsciiWhitespace => " \t\n\r"u8;
#endif

    /// <summary>
    /// Reads a little-endian 32-bit signed integer from the given byte array starting at
    /// <paramref name="offset"/>. The caller is responsible for ensuring the array contains
    /// at least four bytes from <paramref name="offset"/>.
    /// </summary>
    /// <param name="data">The source byte array.</param>
    /// <param name="offset">The starting offset.</param>
    /// <returns>The decoded little-endian 32-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32LittleEndian(byte[] data, int offset = 0) =>
#if NET6_0_OR_GREATER
        BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset));
#else
        BitConverter.IsLittleEndian
            ? BitConverter.ToInt32(data, offset)
            : (data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
#endif

    /// <summary>
    /// Reads a little-endian 64-bit signed integer from the given byte array starting at
    /// <paramref name="offset"/>. The caller is responsible for ensuring the array contains
    /// at least eight bytes from <paramref name="offset"/>.
    /// </summary>
    /// <param name="data">The source byte array.</param>
    /// <param name="offset">The starting offset.</param>
    /// <returns>The decoded little-endian 64-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadInt64LittleEndian(byte[] data, int offset = 0) =>
#if NET6_0_OR_GREATER
        BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(offset));
#else
        BitConverter.IsLittleEndian
            ? BitConverter.ToInt64(data, offset)
            : ((long)data[offset]
                | ((long)data[offset + 1] << 8)
                | ((long)data[offset + 2] << 16)
                | ((long)data[offset + 3] << 24)
                | ((long)data[offset + 4] << 32)
                | ((long)data[offset + 5] << 40)
                | ((long)data[offset + 6] << 48)
                | ((long)data[offset + 7] << 56));
#endif

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="data"/>, after skipping leading ASCII
    /// whitespace, starts with a JSON opener (<c>{</c> or <c>[</c>). Used by serializer format
    /// probes that need to tell JSON and BSON payloads apart without allocating a decoded string.
    /// </summary>
    /// <param name="data">The raw payload bytes.</param>
    /// <returns><see langword="true"/> if the first non-whitespace byte is <c>{</c> or <c>[</c>.</returns>
    public static bool StartsWithJsonOpener(byte[] data)
    {
        if (data is null)
        {
            return false;
        }

#if NET5_0_OR_GREATER
        // On modern runtimes, MemoryExtensions.TrimStart(ReadOnlySpan<byte>, ReadOnlySpan<byte>)
        // can be vectorised by the JIT. Let the BCL do the whitespace skip in bulk.
        var trimmed = data.AsSpan().TrimStart(AsciiWhitespace);
        return !trimmed.IsEmpty && (trimmed[0] is (byte)'{' or (byte)'[');
#else
        // Scalar fallback for net462 / netstandard2.0 targets without span TrimStart(span).
        for (var i = 0; i < data.Length; i++)
        {
            var b = data[i];
            if (b is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r')
            {
                continue;
            }

            return b is (byte)'{' or (byte)'[';
        }

        return false;
#endif
    }
}
