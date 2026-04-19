// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="BsonDataHelper"/>.
/// </summary>
[Category("Akavache")]
public class BsonDataHelperTests
{
    /// <summary>
    /// Data shorter than 5 bytes is not BSON (lines 22-23, branch at 21).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task IsPotentialBsonData_DataShorterThan5Bytes_ReturnsFalse()
    {
        var result = BsonDataHelper.IsPotentialBsonData([1, 2, 3, 4]);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Data with valid BSON header length returns true.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task IsPotentialBsonData_ValidBsonHeader_ReturnsTrue()
    {
        // A BSON document with length header of 10 (little-endian) and data length of 10.
        byte[] data = [10, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        var result = BsonDataHelper.IsPotentialBsonData(data);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Data with document length of 4 or less is not valid BSON.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task IsPotentialBsonData_DocumentLengthTooSmall_ReturnsFalse()
    {
        // Length header of 4 (little-endian) — not > 4, so returns false.
        byte[] data = [4, 0, 0, 0, 0];

        var result = BsonDataHelper.IsPotentialBsonData(data);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Data with document length exceeding data.Length + 100 is not valid BSON.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task IsPotentialBsonData_DocumentLengthExceedsDataPlusTolerance_ReturnsFalse()
    {
        // 5-byte array, length header claims 200 bytes — way beyond data.Length + 100.
        byte[] data = [200, 0, 0, 0, 0];

        var result = BsonDataHelper.IsPotentialBsonData(data);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Empty data array is shorter than 5 bytes and returns false.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task IsPotentialBsonData_EmptyArray_ReturnsFalse()
    {
        var result = BsonDataHelper.IsPotentialBsonData([]);

        await Assert.That(result).IsFalse();
    }
}
