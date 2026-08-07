// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers.Binary;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Core;
#else
namespace Akavache.Core;
#endif

/// <summary>Helpers for identifying BSON-shaped payloads.</summary>
internal static class BsonDataHelper
{
    /// <summary>Size of the little-endian int32 document-length prefix that opens every BSON document. A payload whose declared length does not exceed its own prefix cannot be a document.</summary>
    private const int DocumentLengthPrefixBytes = 4;

    /// <summary>
    /// How far the declared document length may exceed the buffer before the payload is rejected.
    /// Callers hand over buffers that were sometimes framed or padded by the storage layer, so an
    /// exact match is too strict for a "might be BSON" probe.
    /// </summary>
    private const int DeclaredLengthTolerance = 100;

    /// <summary>Checks if data might be BSON format.</summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if data might be BSON.</returns>
    internal static bool IsPotentialBsonData(byte[] data)
    {
        if (data.Length < 5)
        {
            return false;
        }

        var documentLength = BinaryPrimitives.ReadInt32LittleEndian(data);
        return documentLength > DocumentLengthPrefixBytes && documentLength <= data.Length + DeclaredLengthTolerance;
    }
}
