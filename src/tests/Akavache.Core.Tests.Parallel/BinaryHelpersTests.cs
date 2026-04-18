// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="BinaryHelpers"/> little-endian binary decoders. The hand-rolled
/// fallback path used on legacy targets contains real bit-shifting logic that is exercised
/// here against well-known constant byte sequences.
/// </summary>
[Category("Akavache")]
public class BinaryHelpersTests
{
    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt32LittleEndian"/> decodes zero correctly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt32LittleEndianShouldDecodeZero()
    {
        byte[] data = [.. "\0\0\0\0"u8];

        var result = BinaryHelpers.ReadInt32LittleEndian(data);

        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt32LittleEndian"/> decodes the value 1 from a
    /// little-endian byte sequence.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt32LittleEndianShouldDecodeOne()
    {
        byte[] data = [0x01, 0x00, 0x00, 0x00];

        var result = BinaryHelpers.ReadInt32LittleEndian(data);

        await Assert.That(result).IsEqualTo(1);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt32LittleEndian"/> decodes a value where every
    /// byte is non-zero, exercising every byte position in the bit-shift reconstruction.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt32LittleEndianShouldDecodeAllByteLanes()
    {
        // Little-endian 0x04030201 → bytes 0x01, 0x02, 0x03, 0x04.
        byte[] data = [0x01, 0x02, 0x03, 0x04];

        var result = BinaryHelpers.ReadInt32LittleEndian(data);

        await Assert.That(result).IsEqualTo(0x04030201);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt32LittleEndian"/> decodes
    /// <see cref="int.MinValue"/> correctly. The high bit of the most-significant byte must be
    /// preserved as the sign bit through the shift reconstruction.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt32LittleEndianShouldDecodeIntMinValue()
    {
        var data = BitConverter.IsLittleEndian
            ? BitConverter.GetBytes(int.MinValue)
            : [0x00, 0x00, 0x00, 0x80];

        var result = BinaryHelpers.ReadInt32LittleEndian(data);

        await Assert.That(result).IsEqualTo(int.MinValue);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt32LittleEndian"/> decodes
    /// <see cref="int.MaxValue"/> correctly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt32LittleEndianShouldDecodeIntMaxValue()
    {
        var data = BitConverter.IsLittleEndian
            ? BitConverter.GetBytes(int.MaxValue)
            : [0xFF, 0xFF, 0xFF, 0x7F];

        var result = BinaryHelpers.ReadInt32LittleEndian(data);

        await Assert.That(result).IsEqualTo(int.MaxValue);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt32LittleEndian"/> decodes -1 (all bits set)
    /// correctly, exercising the OR-fold of every byte through the sign bit.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt32LittleEndianShouldDecodeNegativeOne()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF];

        var result = BinaryHelpers.ReadInt32LittleEndian(data);

        await Assert.That(result).IsEqualTo(-1);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt32LittleEndian"/> reads from the supplied
    /// offset rather than always starting at index 0.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt32LittleEndianShouldHonourOffset()
    {
        // Pre-pad with junk bytes so the offset matters.
        byte[] data = [0xAA, 0xBB, 0xCC, 0xDD, 0x01, 0x02, 0x03, 0x04];

        var result = BinaryHelpers.ReadInt32LittleEndian(data, offset: 4);

        await Assert.That(result).IsEqualTo(0x04030201);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt32LittleEndian"/> agrees with
    /// <see cref="BitConverter.ToInt32(byte[], int)"/> on a randomized set of integer values
    /// when running on a little-endian platform.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt32LittleEndianShouldAgreeWithBitConverterOnLittleEndianPlatforms()
    {
        if (!BitConverter.IsLittleEndian)
        {
            return; // Skip on big-endian — BitConverter would disagree by definition.
        }

        // Deterministic LCG so the test vectors are reproducible without using
        // System.Random (CA5394). The constants are the Numerical Recipes LCG —
        // good enough spread for round-trip coverage.
        var state = 42UL;
        for (var i = 0; i < 100; i++)
        {
            state = unchecked((state * 1103515245UL) + 12345UL);
            var expected = unchecked((int)state);
            var bytes = BitConverter.GetBytes(expected);

            var actual = BinaryHelpers.ReadInt32LittleEndian(bytes);

            await Assert.That(actual).IsEqualTo(expected);
        }
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt64LittleEndian"/> decodes zero correctly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt64LittleEndianShouldDecodeZero()
    {
        var data = new byte[8];

        var result = BinaryHelpers.ReadInt64LittleEndian(data);

        await Assert.That(result).IsEqualTo(0L);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt64LittleEndian"/> decodes the value 1.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt64LittleEndianShouldDecodeOne()
    {
        byte[] data = [0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        var result = BinaryHelpers.ReadInt64LittleEndian(data);

        await Assert.That(result).IsEqualTo(1L);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt64LittleEndian"/> decodes a value where every
    /// byte is non-zero, exercising every byte lane in the bit-shift reconstruction.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt64LittleEndianShouldDecodeAllByteLanes()
    {
        // Little-endian 0x0807060504030201 → bytes 0x01..0x08.
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        var result = BinaryHelpers.ReadInt64LittleEndian(data);

        await Assert.That(result).IsEqualTo(0x0807060504030201L);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt64LittleEndian"/> decodes
    /// <see cref="long.MinValue"/> correctly, preserving the sign bit through the high byte.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt64LittleEndianShouldDecodeLongMinValue()
    {
        var data = BitConverter.IsLittleEndian
            ? BitConverter.GetBytes(long.MinValue)
            : [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80];

        var result = BinaryHelpers.ReadInt64LittleEndian(data);

        await Assert.That(result).IsEqualTo(long.MinValue);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt64LittleEndian"/> decodes
    /// <see cref="long.MaxValue"/> correctly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt64LittleEndianShouldDecodeLongMaxValue()
    {
        var data = BitConverter.IsLittleEndian
            ? BitConverter.GetBytes(long.MaxValue)
            : [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F];

        var result = BinaryHelpers.ReadInt64LittleEndian(data);

        await Assert.That(result).IsEqualTo(long.MaxValue);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt64LittleEndian"/> decodes -1 (all bits set).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt64LittleEndianShouldDecodeNegativeOne()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

        var result = BinaryHelpers.ReadInt64LittleEndian(data);

        await Assert.That(result).IsEqualTo(-1L);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt64LittleEndian"/> reads from the supplied
    /// offset rather than always starting at index 0.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt64LittleEndianShouldHonourOffset()
    {
        byte[] data = [0xAA, 0xBB, 0xCC, 0xDD, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        var result = BinaryHelpers.ReadInt64LittleEndian(data, offset: 4);

        await Assert.That(result).IsEqualTo(0x0807060504030201L);
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.ReadInt64LittleEndian"/> agrees with
    /// <see cref="BitConverter.ToInt64(byte[], int)"/> on a randomized set of long values
    /// when running on a little-endian platform.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadInt64LittleEndianShouldAgreeWithBitConverterOnLittleEndianPlatforms()
    {
        if (!BitConverter.IsLittleEndian)
        {
            return; // Skip on big-endian — BitConverter would disagree by definition.
        }

        // Deterministic LCG so the test vectors are reproducible without using
        // System.Random (CA5394).
        var state = 99UL;
        var buffer = new byte[8];
        for (var i = 0; i < 100; i++)
        {
            state = unchecked((state * 6364136223846793005UL) + 1442695040888963407UL);
            var expected = unchecked((long)state);
            BitConverter.GetBytes(expected).CopyTo(buffer, 0);

            var actual = BinaryHelpers.ReadInt64LittleEndian(buffer);

            await Assert.That(actual).IsEqualTo(expected);
        }
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.StartsWithJsonOpener"/> returns <see langword="false"/>
    /// for a null input without throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task StartsWithJsonOpenerShouldReturnFalseForNullInput() =>
        await Assert.That(BinaryHelpers.StartsWithJsonOpener(null!)).IsFalse();

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.StartsWithJsonOpener"/> returns <see langword="false"/>
    /// for an empty buffer (no non-whitespace byte to classify).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task StartsWithJsonOpenerShouldReturnFalseForEmptyBuffer() =>
        await Assert.That(BinaryHelpers.StartsWithJsonOpener([])).IsFalse();

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.StartsWithJsonOpener"/> returns <see langword="false"/>
    /// for a buffer containing only ASCII whitespace (no JSON opener to find).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task StartsWithJsonOpenerShouldReturnFalseForWhitespaceOnly() =>
        await Assert.That(BinaryHelpers.StartsWithJsonOpener([.. " \t\n\r"u8])).IsFalse();

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.StartsWithJsonOpener"/> recognises the two JSON
    /// opener bytes after skipping leading ASCII whitespace.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task StartsWithJsonOpenerShouldRecogniseJsonOpenersAfterWhitespace()
    {
        await Assert.That(BinaryHelpers.StartsWithJsonOpener([.. "{}"u8])).IsTrue();
        await Assert.That(BinaryHelpers.StartsWithJsonOpener([.. "[]"u8])).IsTrue();
        await Assert.That(BinaryHelpers.StartsWithJsonOpener([.. "  {"u8])).IsTrue();
        await Assert.That(BinaryHelpers.StartsWithJsonOpener([.. "\t\n\r["u8])).IsTrue();
    }

    /// <summary>
    /// Verifies <see cref="BinaryHelpers.StartsWithJsonOpener"/> rejects a non-JSON payload
    /// whose first non-whitespace byte is neither <c>{</c> nor <c>[</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task StartsWithJsonOpenerShouldRejectNonJsonPayloads()
    {
        await Assert.That(BinaryHelpers.StartsWithJsonOpener([0x05, 0x00, 0x00, 0x00])).IsFalse();
        await Assert.That(BinaryHelpers.StartsWithJsonOpener([(byte)'"'])).IsFalse();
        await Assert.That(BinaryHelpers.StartsWithJsonOpener([.. " x"u8])).IsFalse();
    }
}
