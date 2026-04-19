// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers.Binary;

namespace Akavache.Core;

/// <summary>
/// Helpers for identifying BSON-shaped payloads.
/// </summary>
internal static class BsonDataHelper
{
    /// <summary>
    /// Checks if data might be BSON format.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if data might be BSON.</returns>
    internal static bool IsPotentialBsonData(byte[] data)
    {
        if (data.Length < 5)
        {
            return false;
        }

        var documentLength = BinaryPrimitives.ReadInt32LittleEndian(data);
        return documentLength > 4 && documentLength <= data.Length + 100;
    }
}
