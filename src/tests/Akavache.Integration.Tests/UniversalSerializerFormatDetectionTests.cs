// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for the byte-level probes <see cref="UniversalSerializer"/> uses to guess whether a
/// cached payload is BSON or JSON, and for the minimal JSON reader it falls back to for
/// primitive types.
/// </summary>
[Category("Akavache")]
public class UniversalSerializerFormatDetectionTests
{
    /// <summary>The integer value encoded by the numeric JSON payload these tests feed in.</summary>
    private const int EncodedIntegerValue = 42;

    /// <summary>A declared document length beyond the tolerance the BSON probe allows.</summary>
    private const int ExceedingToleranceDocumentLength = 200;

    /// <summary>The length of a JSON literal prefix that is one byte short of complete.</summary>
    private const int IncompleteLiteralByteCount = 3;

    /// <summary>A declared document length too small to describe a valid BSON document.</summary>
    private const int TooSmallBsonDocumentLength = 3;

    /// <summary>A declared document length that matches the buffer, so the BSON probe accepts it.</summary>
    private const int ValidBsonDocumentLength = 20;

    /// <summary>A declared document length larger than the buffer but inside the probe's tolerance.</summary>
    private const int WithinToleranceDocumentLength = 100;

    /// <summary>Tests IsPotentialBsonData with data that has a valid BSON length header.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnTrueForValidBsonLikeData()
    {
        // A BSON document starts with a 4-byte int32 length
        var data = new byte[ValidBsonDocumentLength];
        BitConverter.GetBytes(ValidBsonDocumentLength).CopyTo(data, 0);
        data[ValidBsonDocumentLength - 1] = 0x00; // BSON document terminator

        var result = UniversalSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialBsonData with data too short to be BSON.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForShortData()
    {
        byte[] shorterThanBsonHeader = [1, 2, 3];

        var result = UniversalSerializer.IsPotentialBsonData(shorterThanBsonHeader);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialBsonData with unreasonable length header.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForUnreasonableLength()
    {
        // Length says 3 bytes, which is too small for a valid doc
        var data = new byte[10];
        BitConverter.GetBytes(TooSmallBsonDocumentLength).CopyTo(data, 0);

        var result = UniversalSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialBsonData with negative length header (invalid BSON).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForNegativeLength()
    {
        var data = new byte[10];
        BitConverter.GetBytes(-1).CopyTo(data, 0);
        var result = UniversalSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialBsonData with length much larger than actual data but within tolerance.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnTrueWhenLengthWithinTolerance()
    {
        var data = new byte[10];

        // Length says 100, data is 10, tolerance is +100 from data length = 110, so 100 <= 110
        BitConverter.GetBytes(WithinToleranceDocumentLength).CopyTo(data, 0);
        var result = UniversalSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialBsonData with length far exceeding data plus tolerance.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseWhenLengthExceedsTolerance()
    {
        var data = new byte[10];

        // Length says 200, data.Length + 100 = 110, so 200 > 110
        BitConverter.GetBytes(ExceedingToleranceDocumentLength).CopyTo(data, 0);
        var result = UniversalSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialJsonData with JSON object data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForJsonObject()
    {
        var data = "{\"name\":\"test\"}"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialJsonData with JSON array data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForJsonArray()
    {
        var data = "[1,2,3]"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialJsonData with JSON string data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForJsonString()
    {
        var data = "\"hello world\""u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialJsonData with numeric data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForNumber()
    {
        var data = "42"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialJsonData with negative number.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForNegativeNumber()
    {
        var data = "-123"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialJsonData with true literal.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForTrueLiteral()
    {
        var data = "true"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialJsonData with false literal.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForFalseLiteral()
    {
        var data = "false"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialJsonData with null literal.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForNullLiteral()
    {
        var data = "null"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialJsonData with leading whitespace.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldHandleLeadingWhitespace()
    {
        var data = "  \t\n{\"key\":\"value\"}"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests IsPotentialJsonData with empty data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnFalseForEmptyData()
    {
        var result = UniversalSerializer.IsPotentialJsonData([]);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialJsonData with only whitespace.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnFalseForOnlyWhitespace()
    {
        var data = "   \t\n\r  "u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialJsonData with binary data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnFalseForBinaryData()
    {
        byte[] data = [0xFF, 0xFE, 0x00, 0x01, 0x02];
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests IsPotentialJsonData with a single non-JSON byte (not whitespace, not a JSON start char).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnFalseForNonJsonSingleByte()
    {
        // 0x41 = 'A', which is not a JSON start character
        var result = UniversalSerializer.IsPotentialJsonData([0x41]);
        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests the IsJsonObjectOrArray method.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsJsonObjectOrArrayShouldReturnTrueForCorrectCharacters()
    {
        using (Assert.Multiple())
        {
            await Assert.That(UniversalSerializer.IsJsonObjectOrArray((byte)'{')).IsTrue();
            await Assert.That(UniversalSerializer.IsJsonObjectOrArray((byte)'[')).IsTrue();
            await Assert.That(UniversalSerializer.IsJsonObjectOrArray((byte)'}')).IsFalse();
            await Assert.That(UniversalSerializer.IsJsonObjectOrArray((byte)']')).IsFalse();
            await Assert.That(UniversalSerializer.IsJsonObjectOrArray((byte)'a')).IsFalse();
        }
    }

    /// <summary>Tests the IsJsonString method.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsJsonStringShouldReturnTrueForQuote()
    {
        using (Assert.Multiple())
        {
            await Assert.That(UniversalSerializer.IsJsonString((byte)'"')).IsTrue();
            await Assert.That(UniversalSerializer.IsJsonString((byte)'\'')).IsFalse();
            await Assert.That(UniversalSerializer.IsJsonString((byte)'a')).IsFalse();
        }
    }

    /// <summary>Tests the IsJsonNumber method.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsJsonNumberShouldReturnTrueForDigitsAndMinus()
    {
        using (Assert.Multiple())
        {
            for (byte i = (byte)'0'; i <= (byte)'9'; i++)
            {
                await Assert.That(UniversalSerializer.IsJsonNumber(i)).IsTrue();
            }

            await Assert.That(UniversalSerializer.IsJsonNumber((byte)'-')).IsTrue();
            await Assert.That(UniversalSerializer.IsJsonNumber((byte)'+')).IsFalse();
            await Assert.That(UniversalSerializer.IsJsonNumber((byte)'a')).IsFalse();
        }
    }

    /// <summary>Tests the IsJsonBoolean method.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsJsonBooleanShouldReturnTrueForTrueAndFalse()
    {
        var trueData = "true"u8.ToArray();
        var falseData = "false"u8.ToArray();
        var notBoolData = "notbool"u8.ToArray();

        using (Assert.Multiple())
        {
            await Assert.That(UniversalSerializer.IsJsonBoolean(trueData, 0)).IsTrue();
            await Assert.That(UniversalSerializer.IsJsonBoolean(falseData, 0)).IsTrue();
            await Assert.That(UniversalSerializer.IsJsonBoolean(notBoolData, 0)).IsFalse();
            await Assert.That(UniversalSerializer.IsJsonBoolean(trueData[..IncompleteLiteralByteCount], 0)).IsFalse();
        }
    }

    /// <summary>Tests the IsJsonNull method.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsJsonNullShouldReturnTrueForNull()
    {
        var nullData = "null"u8.ToArray();
        var notNullData = "notnull"u8.ToArray();

        using (Assert.Multiple())
        {
            await Assert.That(UniversalSerializer.IsJsonNull(nullData, 0)).IsTrue();
            await Assert.That(UniversalSerializer.IsJsonNull(notNullData, 0)).IsFalse();
            await Assert.That(UniversalSerializer.IsJsonNull(nullData[..IncompleteLiteralByteCount], 0)).IsFalse();
        }
    }

    /// <summary>Tests TryBasicJsonDeserialization for string type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldDeserializeString()
    {
        var data = "\"hello world\""u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<string>(data);
        await Assert.That(result).IsEqualTo("hello world");
    }

    /// <summary>Tests TryBasicJsonDeserialization for string without quotes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldHandleUnquotedString()
    {
        var data = "hello"u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<string>(data);
        await Assert.That(result).IsEqualTo("hello");
    }

    /// <summary>Tests TryBasicJsonDeserialization for int type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldDeserializeInt()
    {
        var data = "42"u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<int>(data);
        await Assert.That(result).IsEqualTo(EncodedIntegerValue);
    }

    /// <summary>Tests TryBasicJsonDeserialization for bool type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldDeserializeBool()
    {
        var data = "true"u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<bool>(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>Tests TryBasicJsonDeserialization for empty/whitespace data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldReturnDefaultForEmptyData()
    {
        var data = "   "u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<string>(data);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests TryBasicJsonDeserialization for unsupported type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldReturnDefaultForUnsupportedType()
    {
        var data = "{\"name\":\"test\"}"u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<UserObject>(data);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests TryBasicJsonDeserialization returns default for a double type (unsupported simple type).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldReturnDefaultForDoubleType()
    {
        var data = "3.14"u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<double>(data);
        await Assert.That(result).IsEqualTo(0);
    }
}
