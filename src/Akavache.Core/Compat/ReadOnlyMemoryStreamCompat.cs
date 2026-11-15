// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Helpers;
#else
namespace Akavache.Helpers;
#endif

/// <summary>Opens seekable, read-only streams over existing byte arrays without copying them.</summary>
internal static class ReadOnlyMemoryStreamCompat
{
    /// <summary>Creates an independent stream whose contents share the supplied array.</summary>
    /// <param name="buffer">The bytes to read; callers must keep them unchanged while reading.</param>
    /// <returns>A read-only stream positioned at the start of the buffer.</returns>
    internal static Stream OpenRead(byte[] buffer)
    {
        ArgumentValidation.ThrowIfNull(buffer);

#if NET11_0_OR_GREATER
        return new ReadOnlyMemoryStream(buffer);
#else
        return new MemoryStream(buffer, writable: false);
#endif
    }
}
