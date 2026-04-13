// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET6_0_OR_GREATER
using System.Buffers.Binary;
#endif
using System.Runtime.CompilerServices;

namespace Akavache.Helpers;

/// <summary>
/// Endian-explicit binary helpers wrapping <see cref="System.Buffers.Binary.BinaryPrimitives"/>
/// where available, with a hand-rolled little-endian decoder for legacy targets that lack the
/// <c>System.Buffers.Binary</c> namespace. Aggressively inlined so the dispatch and bit-shift
/// reconstruction collapse to a single load on the hot path.
/// </summary>
internal static class BinaryHelpers
{
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
}
