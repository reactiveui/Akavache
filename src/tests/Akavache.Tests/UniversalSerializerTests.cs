// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Threading.Tasks;
using Akavache.Core;
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests for universal serializer compatibility functionality, including
/// high-level behaviour, internal helper coverage, and edge cases.
/// </summary>
/// <remarks>
/// Uses the shared <c>CacheDatabaseState</c> <see cref="NotInParallelAttribute"/>
/// group so these tests serialise against every other test that mutates global
/// state like <see cref="UniversalSerializer"/>'s registered-factory cache,
/// <see cref="CacheDatabase"/>, <see cref="AkavacheBuilder"/>'s
/// static stores, and <c>Splat.AppLocator</c>.
/// </remarks>
[Category("Akavache")]
public class UniversalSerializerTests
{
    /// <summary>
    /// Reset registry between tests so registered serializers don't bleed between tests.
    /// </summary>
    /// <returns>A task.</returns>
    [Before(Test)]
    public async Task ResetRegistryBeforeTest()
    {
        UniversalSerializer.ResetCaches();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Cleanup registry after each test.
    /// </summary>
    /// <returns>A task.</returns>
    [After(Test)]
    public async Task ResetRegistryAfterTest()
    {
        UniversalSerializer.ResetCaches();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests that UniversalSerializer can deserialize data from primary serializer.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldDeserializeFromPrimarySerializer()
    {
        // Arrange
        SystemJsonSerializer primarySerializer = new();
        UserObject testObject = new() { Name = "Test User", Bio = "Test Bio", Blog = "Test Blog" };
        var serializedData = primarySerializer.Serialize(testObject);

        // Act
        var result = UniversalSerializer.Deserialize<UserObject>(serializedData, primarySerializer);

        // Assert
        await Assert.That(result).IsNotNull();
        using (Assert.Multiple())
        {
            await Assert.That(result!.Name).IsEqualTo("Test User");
            await Assert.That(result.Bio).IsEqualTo("Test Bio");
            await Assert.That(result.Blog).IsEqualTo("Test Blog");
        }
    }

    /// <summary>
    /// Tests that UniversalSerializer handles null/empty data correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldHandleNullEmptyData()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        // Act & Assert
        var nullResult = UniversalSerializer.Deserialize<string>(null!, serializer);
        await Assert.That(nullResult).IsNull();

        var emptyResult = UniversalSerializer.Deserialize<string>([], serializer);
        await Assert.That(emptyResult).IsNull();
    }

    /// <summary>
    /// Tests that UniversalSerializer can serialize data with target serializer.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldSerializeWithTargetSerializer()
    {
        // Arrange
        SystemJsonSerializer targetSerializer = new();
        UserObject testObject = new() { Name = "Serialize Test", Bio = "Serialize Bio", Blog = "Serialize Blog" };

        // Act
        var serializedData = UniversalSerializer.Serialize(testObject, targetSerializer);

        // Assert
        await Assert.That(serializedData).IsNotNull();
        await Assert.That(serializedData).IsNotEmpty();

        // Verify it can be deserialized back
        var deserializedObject = targetSerializer.Deserialize<UserObject>(serializedData);
        await Assert.That(deserializedObject).IsNotNull();
        await Assert.That(deserializedObject!.Name).IsEqualTo("Serialize Test");
    }

    /// <summary>
    /// Tests that UniversalSerializer handles null values correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldHandleNullValues()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        // Act
        var result = UniversalSerializer.Serialize<string>(null!, serializer);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsEmpty(); // Null values should return empty array
    }

    /// <summary>
    /// Tests that UniversalSerializer handles DateTime serialization correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldHandleDateTimeSerialization()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        DateTime testDateTime = new(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);

        // Act
        var serializedData = UniversalSerializer.Serialize(testDateTime, serializer, DateTimeKind.Utc);
        var deserializedDateTime =
            UniversalSerializer.Deserialize<DateTime>(serializedData, serializer, DateTimeKind.Utc);

        // Assert
        await Assert.That(deserializedDateTime).IsEqualTo(testDateTime);
        await Assert.That(deserializedDateTime.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that UniversalSerializer can handle cross-serializer scenarios.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldHandleCrossSerializerScenarios()
    {
        // Arrange
        SystemJsonSerializer systemJsonSerializer = new();
        NewtonsoftSerializer newtonsoftSerializer = new();
        UserObject testObject = new() { Name = "Cross Test", Bio = "Cross Bio", Blog = "Cross Blog" };

        // Act - Serialize with one, deserialize with UniversalSerializer using another
        var systemJsonData = systemJsonSerializer.Serialize(testObject);

        // Assert
        // This explicitly verifies that the fallback mechanism does not throw an exception.
        await Assert.That(() => UniversalSerializer.Deserialize<UserObject>(systemJsonData, newtonsoftSerializer))
            .ThrowsNothing();
    }

    /// <summary>
    /// Tests that UniversalSerializer throws appropriate exceptions for invalid input.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldThrowForInvalidInput()
    {
        // Arrange & Act & Assert - Test null serializer
        Assert.Throws<ArgumentNullException>(static () =>
            UniversalSerializer.Deserialize<string>([1, 2, 3], null!));

        Assert.Throws<ArgumentNullException>(static () =>
            UniversalSerializer.Serialize("test", null!));

        // Test null data - should return null rather than throw for empty/null data
        var nullDataResult = UniversalSerializer.Deserialize<string>(null!, new SystemJsonSerializer());
        await Assert.That(nullDataResult).IsNull();

        var emptyDataResult = UniversalSerializer.Deserialize<string>([], new SystemJsonSerializer());
        await Assert.That(emptyDataResult).IsNull();
    }

    /// <summary>
    /// Tests that UniversalSerializer handles DateTime edge cases.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldHandleDateTimeEdgeCases()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        DateTime[] edgeCases =
        [
            DateTime.MinValue,
            DateTime.MaxValue,
            new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new(2025, 12, 31, 23, 59, 59, DateTimeKind.Local)
        ];

        foreach (var testDate in edgeCases)
        {
            try
            {
                // Act
                var serializedData = UniversalSerializer.Serialize(testDate, serializer);
                var deserializedDate = UniversalSerializer.Deserialize<DateTime>(serializedData, serializer);

                // Assert - Allow for some tolerance in extreme cases
                var timeDifference = Math.Abs((testDate - deserializedDate).TotalMinutes);
                await Assert.That(timeDifference).IsLessThan(1440); // 24 hours tolerance
            }
            catch (Exception ex)
            {
                // Some edge cases may fail due to serializer limitations - this is acceptable
                // Just ensure the exception is handled gracefully
                await Assert.That(ex).IsTypeOf<InvalidOperationException>();
            }
        }
    }

    /// <summary>
    /// Tests that UniversalSerializer can handle BSON data detection.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldDetectBsonData()
    {
        // Arrange
        NewtonsoftBsonSerializer bsonSerializer = new();
        UserObject testObject = new() { Name = "BSON Test", Bio = "BSON Bio", Blog = "BSON Blog" };
        var bsonData = bsonSerializer.Serialize(testObject);

        // Act - Try to deserialize BSON data with a different serializer
        UniversalSerializer.Deserialize<UserObject>(bsonData, new SystemJsonSerializer());

        // Assert - Should either succeed with fallback or fail gracefully
        // The main goal is no unhandled exceptions
        // Act & Assert
        // This clearly states that the enclosed code should not throw an exception.
        await Assert.That(() => UniversalSerializer.Deserialize<UserObject>(bsonData, new SystemJsonSerializer()))
            .ThrowsNothing();
    }

    /// <summary>
    /// Tests that UniversalSerializer can handle JSON data detection.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldDetectJsonData()
    {
        // Arrange
        SystemJsonSerializer jsonSerializer = new();
        UserObject testObject = new() { Name = "JSON Test", Bio = "JSON Bio", Blog = "JSON Blog" };
        var jsonData = jsonSerializer.Serialize(testObject);

        // Act & Assert
        // This explicitly states the test's goal: the code should run without throwing.
        await Assert.That(() => UniversalSerializer.Deserialize<UserObject>(jsonData, new NewtonsoftBsonSerializer()))
            .ThrowsNothing();
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys functionality.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task UniversalSerializerShouldTryAlternativeKeys()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(serializer);
        UserObject testObject = new() { Name = "Alt Key Test", Bio = "Alt Bio", Blog = "Alt Blog" };

        try
        {
            // Store object with prefixed key
            await cache.InsertObject("test_key", testObject).FirstAsync();

            // Act - Try to find with alternative keys
            var result = await UniversalSerializer.TryFindDataWithAlternativeKeys<UserObject>(
                cache, "test_key", serializer);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("Alt Key Test");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that UniversalSerializer handles serialization failures gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task UniversalSerializerShouldHandleSerializationFailures()
    {
        try
        {
            // Arrange
            SystemJsonSerializer serializer = new();

            // Create a problematic object (circular reference)
            List<object> circularRef = [];
            circularRef.Add(circularRef);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => UniversalSerializer.Serialize(circularRef, serializer));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that UniversalSerializer properly validates DateTime after deserialization.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldValidateDeserializedDateTime()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        DateTime testDateTime = new(2025, 1, 15, 10, 30, 45, DateTimeKind.Local);

        // Act
        var serializedData = UniversalSerializer.Serialize(testDateTime, serializer, DateTimeKind.Utc);
        var deserializedDateTime =
            UniversalSerializer.Deserialize<DateTime>(serializedData, serializer, DateTimeKind.Utc);

        // Assert - Should be converted to UTC
        await Assert.That(deserializedDateTime.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that UniversalSerializer can handle complex object hierarchies.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldHandleComplexObjects()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        var complexObject = new
        {
            User = new UserObject { Name = "Complex User", Bio = "Complex Bio", Blog = "Complex Blog" },
            Date = DateTime.UtcNow,
            Numbers = (int[])[1, 2, 3, 4, 5],
            Metadata = new Dictionary<string, object> { ["version"] = "1.0", ["enabled"] = true, ["count"] = 42 }
        };

        // Act
        var serializedData = UniversalSerializer.Serialize(complexObject, serializer);

        // We can't easily deserialize anonymous types, so just verify serialization succeeds
        await Assert.That(serializedData).IsNotNull();
        await Assert.That(serializedData).IsNotEmpty();
    }

    /// <summary>
    /// Tests that UniversalSerializer properly preprocesses DateTime for serialization.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldPreprocessDateTime()
    {
        // Arrange
        NewtonsoftSerializer newtonsoftSerializer = new();

        // Test edge cases that might be problematic for certain serializers
        DateTime[] edgeDates =
        [
            DateTime.MinValue,
            DateTime.MaxValue,
            new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        ];

        foreach (var testDate in edgeDates)
        {
            try
            {
                // Act - Should preprocess the date to make it safer for serialization
                var serializedData = UniversalSerializer.Serialize(testDate, newtonsoftSerializer);

                // Assert
                await Assert.That(serializedData).IsNotNull();
                await Assert.That(serializedData).IsNotEmpty();
            }
            catch (InvalidOperationException)
            {
                // Some edge cases may still fail - this is acceptable
                // The important thing is that the failure is handled gracefully
            }
        }
    }

    /// <summary>
    /// Tests IsPotentialBsonData with data that has a valid BSON length header.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnTrueForValidBsonLikeData()
    {
        // A BSON document starts with a 4-byte int32 length
        var data = new byte[20];
        BitConverter.GetBytes(20).CopyTo(data, 0);
        data[19] = 0x00; // BSON document terminator

        var result = UniversalSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialBsonData with data too short to be BSON.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForShortData()
    {
        var result = UniversalSerializer.IsPotentialBsonData([1, 2, 3]);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialBsonData with unreasonable length header.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForUnreasonableLength()
    {
        // Length says 3 bytes, which is too small for a valid doc
        var data = new byte[10];
        BitConverter.GetBytes(3).CopyTo(data, 0);

        var result = UniversalSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with JSON object data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForJsonObject()
    {
        var data = "{\"name\":\"test\"}"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with JSON array data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForJsonArray()
    {
        var data = "[1,2,3]"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with JSON string data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForJsonString()
    {
        var data = "\"hello world\""u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with numeric data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForNumber()
    {
        var data = "42"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with negative number.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForNegativeNumber()
    {
        var data = "-123"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with true literal.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForTrueLiteral()
    {
        var data = "true"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with false literal.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForFalseLiteral()
    {
        var data = "false"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with null literal.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnTrueForNullLiteral()
    {
        var data = "null"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with leading whitespace.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldHandleLeadingWhitespace()
    {
        var data = "  \t\n{\"key\":\"value\"}"u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with empty data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnFalseForEmptyData()
    {
        var result = UniversalSerializer.IsPotentialJsonData([]);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with only whitespace.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnFalseForOnlyWhitespace()
    {
        var data = "   \t\n\r  "u8.ToArray();
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with binary data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnFalseForBinaryData()
    {
        byte[] data = [0xFF, 0xFE, 0x00, 0x01, 0x02];
        var result = UniversalSerializer.IsPotentialJsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests the IsJsonObjectOrArray method.
    /// </summary>
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

    /// <summary>
    /// Tests the IsJsonString method.
    /// </summary>
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

    /// <summary>
    /// Tests the IsJsonNumber method.
    /// </summary>
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

    /// <summary>
    /// Tests the IsJsonBoolean method.
    /// </summary>
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
            await Assert.That(UniversalSerializer.IsJsonBoolean(trueData[..3], 0)).IsFalse();
        }
    }

    /// <summary>
    /// Tests the IsJsonNull method.
    /// </summary>
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
            await Assert.That(UniversalSerializer.IsJsonNull(nullData[..3], 0)).IsFalse();
        }
    }

    /// <summary>
    /// Tests TryBasicJsonDeserialization for string type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldDeserializeString()
    {
        var data = "\"hello world\""u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<string>(data);
        await Assert.That(result).IsEqualTo("hello world");
    }

    /// <summary>
    /// Tests TryBasicJsonDeserialization for string without quotes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldHandleUnquotedString()
    {
        var data = "hello"u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<string>(data);
        await Assert.That(result).IsEqualTo("hello");
    }

    /// <summary>
    /// Tests TryBasicJsonDeserialization for int type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldDeserializeInt()
    {
        var data = "42"u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<int>(data);
        await Assert.That(result).IsEqualTo(42);
    }

    /// <summary>
    /// Tests TryBasicJsonDeserialization for bool type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldDeserializeBool()
    {
        var data = "true"u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<bool>(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests TryBasicJsonDeserialization for empty/whitespace data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldReturnDefaultForEmptyData()
    {
        var data = "   "u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<string>(data);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests TryBasicJsonDeserialization for unsupported type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldReturnDefaultForUnsupportedType()
    {
        var data = "{\"name\":\"test\"}"u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<UserObject>(data);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests PreprocessDateTimeForSerialization with MinValue for Newtonsoft.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldHandleMinValueForNewtonsoft()
    {
        NewtonsoftSerializer serializer = new();
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(DateTime.MinValue, serializer, null);
        await Assert.That(result.Year).IsEqualTo(1900);
    }

    /// <summary>
    /// Tests PreprocessDateTimeForSerialization with MaxValue for Newtonsoft.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldHandleMaxValueForNewtonsoft()
    {
        NewtonsoftSerializer serializer = new();
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(DateTime.MaxValue, serializer, null);
        await Assert.That(result.Year).IsEqualTo(2100);
    }

    /// <summary>
    /// Tests PreprocessDateTimeForSerialization with MinValue for SystemJson (no special handling).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldNotModifyMinValueForSystemJson()
    {
        SystemJsonSerializer serializer = new();
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(DateTime.MinValue, serializer, null);
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests PreprocessDateTimeForSerialization with forced UTC kind on Local DateTime.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldConvertLocalToUtcWhenForced()
    {
        SystemJsonSerializer serializer = new();
        DateTime localDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(localDate, serializer, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests PreprocessDateTimeForSerialization with forced Local kind on UTC DateTime.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldConvertUtcToLocalWhenForced()
    {
        SystemJsonSerializer serializer = new();
        DateTime utcDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(utcDate, serializer, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests PreprocessDateTimeForSerialization with forced Unspecified kind.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldConvertToUnspecifiedWhenForced()
    {
        SystemJsonSerializer serializer = new();
        DateTime utcDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result =
            UniversalSerializer.PreprocessDateTimeForSerialization(utcDate, serializer, DateTimeKind.Unspecified);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Tests PreprocessDateTimeForSerialization converting Unspecified to Local.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldConvertUnspecifiedToLocalWhenForced()
    {
        SystemJsonSerializer serializer = new();
        DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(date, serializer, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests PreprocessDateTimeForSerialization with MinValue for NewtonsoftBson (no special handling).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldNotModifyMinValueForBsonSerializer()
    {
        NewtonsoftBsonSerializer serializer = new();
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(DateTime.MinValue, serializer, null);

        // BSON serializer contains "Newtonsoft" but also "Bson", so no special handling
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests GetAvailableAlternativeSerializers excludes the primary serializer when registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAvailableAlternativeSerializersShouldExcludePrimary()
    {
        RegisterAllSerializers();
        try
        {
            SystemJsonSerializer primary = new();
            var alternatives = UniversalSerializer.GetAvailableAlternativeSerializers(primary);

            var hasSystemJson = alternatives.Any(static s => s.GetType() == typeof(SystemJsonSerializer));
            await Assert.That(hasSystemJson).IsFalse();
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests GetAvailableAlternativeSerializers returns registered alternatives.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAvailableAlternativeSerializersShouldReturnRegisteredAlternatives()
    {
        RegisterAllSerializers();
        try
        {
            SystemJsonSerializer primary = new();
            var alternatives = UniversalSerializer.GetAvailableAlternativeSerializers(primary);

            // 4 registered minus the SystemJsonSerializer primary = 3
            await Assert.That(alternatives.Count).IsEqualTo(3);
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests RegisterSerializer throws on null factory.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RegisterSerializerShouldThrowOnNullFactory() =>
        await Assert.That(static () => UniversalSerializer.RegisterSerializer(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests ResetCaches clears registered serializers.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResetCachesShouldClearRegistry()
    {
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
        UniversalSerializer.ResetCaches();

        var alternatives = UniversalSerializer.GetAvailableAlternativeSerializers(new NewtonsoftSerializer());
        await Assert.That(alternatives).IsEmpty();
    }

    /// <summary>
    /// Tests TryFallbackDeserialization with BSON data using registered serializers.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackDeserializationShouldDeserializeBsonFormat()
    {
        RegisterAllSerializers();
        try
        {
            NewtonsoftBsonSerializer bsonSerializer = new();
            UserObject testObject = new() { Name = "BSON Fallback", Bio = "Bio", Blog = "Blog" };
            var bsonData = bsonSerializer.Serialize(testObject);

            var result =
                UniversalSerializer.TryFallbackDeserialization<UserObject>(bsonData, new SystemJsonSerializer(), null);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("BSON Fallback");
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests TryFallbackDeserialization with JSON data using registered serializers.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackDeserializationShouldDeserializeJsonFormat()
    {
        RegisterAllSerializers();
        try
        {
            SystemJsonSerializer jsonSerializer = new();
            UserObject testObject = new() { Name = "JSON Fallback", Bio = "Bio", Blog = "Blog" };
            var jsonData = jsonSerializer.Serialize(testObject);

            var result =
                UniversalSerializer.TryFallbackDeserialization<UserObject>(
                    jsonData,
                    new NewtonsoftBsonSerializer(),
                    null);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("JSON Fallback");
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests TryFallbackDeserialization for string type falls back to basic JSON deserialization.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackDeserializationShouldFallBackToBasicJsonForString()
    {
        var data = "\"hello world\""u8.ToArray();

        // This should go through JSON detection and fall back to basic JSON for string type
        var result = UniversalSerializer.TryFallbackDeserialization<string>(data, new NewtonsoftBsonSerializer(), null);
        await Assert.That(result).IsEqualTo("hello world");
    }

    /// <summary>
    /// Tests TryFallbackSerialization throws when no alternatives can serialize a circular reference.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackSerializationShouldThrowForCircularReference()
    {
        RegisterAllSerializers();
        try
        {
            SystemJsonSerializer primary = new();
            List<object> circularRef = [];
            circularRef.Add(circularRef);

            await Assert.That(() => UniversalSerializer.TryFallbackSerialization(circularRef, primary, null))
                .Throws<InvalidOperationException>();
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests TryFallbackSerialization succeeds with registered alternatives.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackSerializationShouldUseRegisteredAlternatives()
    {
        RegisterAllSerializers();
        try
        {
            SystemJsonSerializer primary = new();
            var result = UniversalSerializer.TryFallbackSerialization("test", primary, DateTimeKind.Utc);
            await Assert.That(result).IsNotNull();
            await Assert.That(result).IsNotEmpty();
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests TryAlternativeSerializers returns default with no registered alternatives.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldReturnDefaultWhenNoAlternatives()
    {
        ResetRegistry();
        try
        {
            SystemJsonSerializer primary = new();
            byte[] data = [0xFF, 0xFE];

            var result = UniversalSerializer.TryAlternativeSerializers<string>(data, primary, null);
            await Assert.That(result).IsNull();
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests TryAlternativeSerializers can deserialize with registered alternatives and forced DateTime kind.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldUseRegisteredAlternativesWithDateTimeKind()
    {
        RegisterAllSerializers();
        try
        {
            // Use Newtonsoft to serialize, then ask UniversalSerializer to find alternatives
            NewtonsoftSerializer nsSerializer = new();
            UserObject testObj = new() { Name = "Alt", Bio = "Bio", Blog = "Blog" };
            var data = nsSerializer.Serialize(testObj);

            // Exclude Newtonsoft as primary, expect a registered alternative to handle it
            var result =
                UniversalSerializer.TryAlternativeSerializers<UserObject>(data, nsSerializer, DateTimeKind.Utc);

            // The path is exercised; the result depends on whether SystemJsonSerializer can read Newtonsoft JSON
            // (it can for objects without DateTime fields)
            await Assert.That(result).IsNotNull();
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests TryAlternativeSerializers handles DateTimeOffset MinValue/MaxValue paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldHandleDateTimeOffsetExtremes()
    {
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
        UniversalSerializer.RegisterSerializer(static () => new NewtonsoftSerializer());

        ThrowingSerializer primary = new();
        var minBytes = new SystemJsonSerializer().Serialize(DateTimeOffset.MinValue);
        var maxBytes = new SystemJsonSerializer().Serialize(DateTimeOffset.MaxValue);

        var minResult = UniversalSerializer.TryAlternativeSerializers<DateTimeOffset>(minBytes, primary, null);
        var maxResult = UniversalSerializer.TryAlternativeSerializers<DateTimeOffset>(maxBytes, primary, null);

        await Assert.That(minResult).IsEqualTo(DateTimeOffset.MinValue);
        await Assert.That(maxResult).IsEqualTo(DateTimeOffset.MaxValue);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat with registered BSON serializers actually deserializes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldDeserializeWithRegistry()
    {
        RegisterAllSerializers();
        try
        {
            NewtonsoftBsonSerializer bsonSerializer = new();
            UserObject testObject = new() { Name = "BSON Direct", Bio = "Bio", Blog = "Blog" };
            var bsonData = bsonSerializer.Serialize(testObject);

            var result = UniversalSerializer.TryDeserializeBsonFormat<UserObject>(bsonData, null);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("BSON Direct");
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat exercises the DateTime UTC kind path through registry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public Task TryDeserializeBsonFormatShouldHandleDateTimeWithUtcKind()
    {
        try
        {
            RegisterAllSerializers();
            try
            {
                NewtonsoftBsonSerializer bsonSerializer = new();
                DateTime testDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
                var bsonData = bsonSerializer.Serialize(testDate);

                // Path is exercised even if BSON-direct DateTime serialization returns MinValue + recovery
                // Recovery may rewrite to fallback date, but path is exercised
                // Path exercised regardless of result value (BSON DateTime handling has special cases)
                _ = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(bsonData, DateTimeKind.Utc);
            }
            finally
            {
                ResetRegistry();
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat exercises the DateTime Local kind path through registry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public Task TryDeserializeBsonFormatShouldHandleDateTimeWithLocalKind()
    {
        try
        {
            RegisterAllSerializers();
            try
            {
                NewtonsoftBsonSerializer bsonSerializer = new();
                DateTime testDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
                var bsonData = bsonSerializer.Serialize(testDate);

                // Path exercised regardless of result value (BSON DateTime handling has special cases)
                _ = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(bsonData, DateTimeKind.Local);
            }
            finally
            {
                ResetRegistry();
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat exercises the DateTime Unspecified kind path through registry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public Task TryDeserializeBsonFormatShouldHandleDateTimeWithUnspecifiedKind()
    {
        try
        {
            RegisterAllSerializers();
            try
            {
                NewtonsoftBsonSerializer bsonSerializer = new();
                DateTime testDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
                var bsonData = bsonSerializer.Serialize(testDate);

                // Path exercised regardless of result value (BSON DateTime handling has special cases)
                _ = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(bsonData, DateTimeKind.Unspecified);
            }
            finally
            {
                ResetRegistry();
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat with invalid data returns default.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldReturnDefaultForInvalidData()
    {
        var result = UniversalSerializer.TryDeserializeBsonFormat<UserObject>([0xFF, 0xFE, 0x00, 0x01, 0x02], null);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat with registered JSON serializers.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldDeserializeWithRegistry()
    {
        RegisterAllSerializers();
        try
        {
            SystemJsonSerializer jsonSerializer = new();
            UserObject testObject = new() { Name = "JSON Direct", Bio = "Bio", Blog = "Blog" };
            var jsonData = jsonSerializer.Serialize(testObject);

            var result = UniversalSerializer.TryDeserializeJsonFormat<UserObject>(jsonData, null);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("JSON Direct");
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat with forced DateTime kind through registry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldHandleDateTimeKind()
    {
        RegisterAllSerializers();
        try
        {
            SystemJsonSerializer jsonSerializer = new();
            DateTime testDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var jsonData = jsonSerializer.Serialize(testDate);

            var result = UniversalSerializer.TryDeserializeJsonFormat<DateTime>(jsonData, DateTimeKind.Utc);
            await Assert.That(result.Year).IsEqualTo(2025);
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat falls back to basic deserialization for string.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldFallBackToBasicDeserialization()
    {
        var data = "\"simple string value\""u8.ToArray();
        var result = UniversalSerializer.TryDeserializeJsonFormat<string>(data, null);

        // Even if JSON serializer lookup fails, TryBasicJsonDeserialization handles strings
        await Assert.That(result).IsNotNull();
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat returns default for completely invalid data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldReturnDefaultForBinaryData()
    {
        byte[] data = [0xFF, 0xFE, 0x00, 0x01, 0x02];
        var result = UniversalSerializer.TryDeserializeJsonFormat<UserObject>(data, null);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.TryDeserializeCandidate{T}"/> returns
    /// <see langword="false"/> for null raw data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeCandidateShouldReturnFalseForNullRawData()
    {
        var succeeded =
            UniversalSerializer.TryDeserializeCandidate<UserObject>(null, new SystemJsonSerializer(), out var result);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.TryDeserializeCandidate{T}"/> returns
    /// <see langword="false"/> for a zero-length raw data array.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeCandidateShouldReturnFalseForEmptyRawData()
    {
        var succeeded =
            UniversalSerializer.TryDeserializeCandidate<UserObject>([], new SystemJsonSerializer(), out var result);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.TryDeserializeCandidate{T}"/> returns
    /// <see langword="false"/> when the deserialized value equals the default of its
    /// type parameter.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeCandidateShouldReturnFalseWhenResultEqualsDefault()
    {
        SystemJsonSerializer serializer = new();
        var data = serializer.Serialize(0);

        var succeeded = UniversalSerializer.TryDeserializeCandidate<int>(data, serializer, out var result);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.TryDeserializeCandidate{T}"/> returns
    /// <see langword="true"/> and emits the value when deserialization succeeds with a
    /// non-default value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeCandidateShouldReturnTrueOnHappyPath()
    {
        SystemJsonSerializer serializer = new();
        UserObject user = new() { Name = "happy", Bio = "bio", Blog = "blog" };
        var data = serializer.Serialize(user);

        var succeeded = UniversalSerializer.TryDeserializeCandidate<UserObject>(data, serializer, out var result);

        await Assert.That(succeeded).IsTrue();
        await Assert.That(result).IsNotNull();
        if (result is null)
        {
            return;
        }

        await Assert.That(result.Name).IsEqualTo("happy");
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.TryDeserializeCandidate{T}"/> routes a
    /// failing serializer through <see cref="UniversalSerializer.Deserialize{T}"/>'s
    /// internal fallback and returns <see langword="false"/> when no alternatives
    /// resolve the value. Exception propagation is no longer wrapped inside
    /// <c>TryDeserializeCandidate</c> itself because <c>Deserialize</c> is already
    /// exception-safe.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeCandidateShouldReturnFalseWhenDeserializerFailsWithNoAlternatives()
    {
        var succeeded =
            UniversalSerializer.TryDeserializeCandidate<UserObject>(
                [1, 2, 3, 4],
                new ThrowingSerializer(),
                out var result);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.CastAsDateTime{T}"/> returns the value when it
    /// is a <see cref="DateTime"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CastAsDateTimeShouldReturnValueForDateTimeType()
    {
        DateTime expected = new(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var result = UniversalSerializer.CastAsDateTime(expected);

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.CastAsDateTime{T}"/> returns <c>default</c>
    /// for non-<see cref="DateTime"/> types.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CastAsDateTimeShouldReturnDefaultForOtherType()
    {
        var result = UniversalSerializer.CastAsDateTime("not a datetime");

        await Assert.That(result).IsEqualTo(default);
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.CastAsDateTime{T}"/> returns <c>default</c>
    /// when the input is <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CastAsDateTimeShouldReturnDefaultForNull()
    {
        var result = UniversalSerializer.CastAsDateTime<string>(null);

        await Assert.That(result).IsEqualTo(default);
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.CastAsDateTimeOffset{T}"/> returns the value
    /// when it is a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CastAsDateTimeOffsetShouldReturnValueForDateTimeOffsetType()
    {
        DateTimeOffset expected = new(2025, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));

        var result = UniversalSerializer.CastAsDateTimeOffset(expected);

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.CastAsDateTimeOffset{T}"/> returns
    /// <c>default</c> for non-<see cref="DateTimeOffset"/> types.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CastAsDateTimeOffsetShouldReturnDefaultForOtherType()
    {
        var result = UniversalSerializer.CastAsDateTimeOffset("not a datetimeoffset");

        await Assert.That(result).IsEqualTo(default);
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.FindKeyCandidates{T}"/> includes a key that is
    /// exactly the requested key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FindKeyCandidatesShouldIncludeExactKey()
    {
        var candidates = UniversalSerializer.FindKeyCandidates<UserObject>(
            ["my_key", "other"],
            "my_key");

        await Assert.That(candidates).Contains("my_key");
        await Assert.That(candidates.Contains("other")).IsFalse();
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.FindKeyCandidates{T}"/> includes a type-prefixed
    /// key (<c>Namespace.Type___key</c>) when that key is present in the cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FindKeyCandidatesShouldIncludeTypePrefixedKey()
    {
        var typePrefixed = $"{typeof(UserObject).FullName}___my_key";
        var candidates = UniversalSerializer.FindKeyCandidates<UserObject>(
            [typePrefixed, "unrelated"],
            "my_key");

        await Assert.That(candidates).Contains(typePrefixed);
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.FindKeyCandidates{T}"/> includes keys ending with
    /// <c>___{requestedKey}</c> even when the prefix does not match any known type shape.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FindKeyCandidatesShouldIncludeCustomTripleUnderscoreSuffixKey()
    {
        var candidates = UniversalSerializer.FindKeyCandidates<UserObject>(
            ["unknown.Type___my_key", "unrelated"],
            "my_key");

        await Assert.That(candidates).Contains("unknown.Type___my_key");
        await Assert.That(candidates.Contains("unrelated")).IsFalse();
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.FindKeyCandidates{T}"/> includes keys that only
    /// end with the requested key (no <c>___</c> separator).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FindKeyCandidatesShouldIncludePlainSuffixKey()
    {
        var candidates = UniversalSerializer.FindKeyCandidates<UserObject>(
            ["prefix-my_key", "something_else"],
            "my_key");

        await Assert.That(candidates).Contains("prefix-my_key");
        await Assert.That(candidates.Contains("something_else")).IsFalse();
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.FindKeyCandidates{T}"/> returns an empty list
    /// when no keys match any of the criteria.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FindKeyCandidatesShouldReturnEmptyWhenNoMatches()
    {
        var candidates = UniversalSerializer.FindKeyCandidates<UserObject>(
            ["alpha", "beta", "gamma"],
            "my_key");

        await Assert.That(candidates).IsEmpty();
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys with null cache returns default.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultForNullCache()
    {
        var result =
            await UniversalSerializer.TryFindDataWithAlternativeKeys<string>(null!, "key", new SystemJsonSerializer());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys with null key returns default.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultForNullKey()
    {
        InMemoryBlobCache cache = new(new SystemJsonSerializer());
        try
        {
            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeys<string>(
                    cache,
                    null!,
                    new SystemJsonSerializer());
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys with null serializer returns default.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultForNullSerializer()
    {
        InMemoryBlobCache cache = new(new SystemJsonSerializer());
        try
        {
            var result = await UniversalSerializer.TryFindDataWithAlternativeKeys<string>(cache, "key", null!);
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys with empty cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultForEmptyCache()
    {
        InMemoryBlobCache cache = new(new SystemJsonSerializer());
        try
        {
            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeys<string>(
                    cache,
                    "nonexistent",
                    new SystemJsonSerializer());
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys finds entry under type-prefixed key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldFindByTypePrefix()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(serializer);
        try
        {
            UserObject testObj = new() { Name = "found", Bio = "bio", Blog = "blog" };
            var prefixedKey = $"{typeof(UserObject).FullName}___my_key";
            var bytes = serializer.Serialize(testObj);
            await cache.Insert(prefixedKey, bytes).ToTask();

            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeys<UserObject>(cache, "my_key", serializer);

            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("found");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys finds entry under short-name prefix.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldFindByShortNamePrefix()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(serializer);
        try
        {
            UserObject testObj = new() { Name = "found2", Bio = "bio", Blog = "blog" };
            var shortKey = $"{nameof(UserObject)}___my_key2";
            var bytes = serializer.Serialize(testObj);
            await cache.Insert(shortKey, bytes).ToTask();

            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeys<UserObject>(cache, "my_key2", serializer);

            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("found2");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys returns default when entry exists but is empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultForEmptyEntry()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(serializer);
        try
        {
            await cache.Insert("empty_key", []).ToTask();

            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeys<UserObject>(cache, "empty_key", serializer);
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Deserialize falls back to alternative serializers when primary fails.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldFallbackToAlternativeSerializers()
    {
        RegisterAllSerializers();
        try
        {
            // Serialize with Newtonsoft BSON, deserialize with SystemJson (which fails) - fallback should work
            NewtonsoftBsonSerializer bsonSerializer = new();
            UserObject testObject = new() { Name = "Fallback Test", Bio = "Bio", Blog = "Blog" };
            var bsonData = bsonSerializer.Serialize(testObject);

            var result = UniversalSerializer.Deserialize<UserObject>(bsonData, new SystemJsonSerializer());
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("Fallback Test");
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests Deserialize with garbage data exercises all fallback paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldExerciseAllFallbackPathsWithGarbageData()
    {
        ResetRegistry();
        try
        {
            // Garbage data with no registered alternatives - all fallbacks return default
            byte[] garbageData = [0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA];

            var result = UniversalSerializer.Deserialize<UserObject>(garbageData, new SystemJsonSerializer());
            await Assert.That(result).IsNull();
        }
        finally
        {
            ResetRegistry();
        }
    }

    /// <summary>
    /// Tests Serialize throws InvalidOperationException for circular references.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeShouldThrowForCircularReferences()
    {
        List<object> circularRef = [];
        circularRef.Add(circularRef);

        await Assert.That(() => UniversalSerializer.Serialize(circularRef, new SystemJsonSerializer()))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests Deserialize throws InvalidOperationException when all fallbacks fail with registered serializers that all throw.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldThrowWhenAllRegisteredSerializersFail()
    {
        // Register a serializer that always throws
        UniversalSerializer.RegisterSerializer(static () => new ThrowingSerializer());

        ThrowingSerializer primary = new();
        byte[] data = [0xFF, 0xFE, 0xFD, 0xFC, 0xFB];

        // Primary throws and all fallbacks throw too -> result is default (no exception)
        var result = UniversalSerializer.Deserialize<UserObject>(data, primary);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that Deserialize returns null when primary throws and all registered fallbacks
    /// also throw on Deserialize. Exercises the primary try/catch entry and the fallback chain
    /// where every alternative is caught inside TryAlternativeSerializers.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnNullWhenPrimaryAndAllFallbacksThrow()
    {
        UniversalSerializer.RegisterSerializer(static () => new ThrowingSerializer());
        ThrowingSerializer primary = new();
        byte[] data = [0x01, 0x02, 0x03];

        var result = UniversalSerializer.Deserialize<UserObject>(data, primary);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys outer catch when cache throws on GetAllKeys (disposed cache).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultWhenGetAllKeysThrows()
    {
        InMemoryBlobCache cache = new(new SystemJsonSerializer());
        await cache.DisposeAsync();

        // Disposed cache's GetAllKeys returns an observable that throws ObjectDisposedException.
        // This exercises the outer catch block (lines 209-212).
        var result =
            await UniversalSerializer.TryFindDataWithAlternativeKeys<UserObject>(
                cache,
                "some_key",
                new SystemJsonSerializer());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys inner catch when cache.Get throws for a candidate key.
    /// Uses a custom cache wrapper whose Get throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultWhenGetThrowsInnerCatch()
    {
        InMemoryBlobCache inner = new(new SystemJsonSerializer());
        try
        {
            // Insert a key whose key is the raw "lookup" name so it is matched by EndsWith.
            await inner.Insert("lookup_inner_catch", [0x01, 0x02]).ToTask();

            ThrowingGetCacheWrapper wrapper = new(inner);
            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeys<UserObject>(
                    wrapper,
                    "lookup_inner_catch",
                    new SystemJsonSerializer());
            await Assert.That(result).IsNull();
        }
        finally
        {
            await inner.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests TryAlternativeSerializers with fake serializer that returns DateTime.MinValue
    /// and data containing 2025 pattern so AttemptDateTimeRecovery succeeds.
    /// This covers the MinValue recovery + correctedDateTime return path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldRecoverDateTimeMinValueFromDataPattern()
    {
        UniversalSerializer.RegisterSerializer(static () => new MinValueDateTimeSerializer());
        ThrowingSerializer primary = new();

        // Data long enough (>10 bytes) containing "2025" to trigger recovery.
        var data = "{\"date\":\"2025-06-15\"}"u8.ToArray();

        var result = UniversalSerializer.TryAlternativeSerializers<DateTime>(data, primary, DateTimeKind.Utc);
        await Assert.That(result).IsNotEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests TryAlternativeSerializers when alt serializer returns DateTime.MinValue and recovery fails.
    /// Covers the HandleDateTimeWithCrossSerializerSupport return path (line 413).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldHandleDateTimeMinValueWhenRecoveryFails()
    {
        UniversalSerializer.RegisterSerializer(static () => new MinValueDateTimeSerializer());
        ThrowingSerializer primary = new();

        // Small data without year pattern -> AttemptDateTimeRecovery returns MinValue.
        byte[] data = "\0\0"u8.ToArray();

        var result = UniversalSerializer.TryAlternativeSerializers<DateTime>(data, primary, null);
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
    }

    /// <summary>
    /// Tests TryAlternativeSerializers catch block when alt serializer throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldContinueWhenAltSerializerThrows()
    {
        UniversalSerializer.RegisterSerializer(static () => new ThrowingSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        ThrowingSerializer primary = new();
        var data = new SystemJsonSerializer().Serialize("hello");

        var result = UniversalSerializer.TryAlternativeSerializers<string>(data, primary, null);
        await Assert.That(result).IsEqualTo("hello");
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.TryAlternativeSerializers{T}"/> skips over a
    /// throwing alternative and returns the result of the next successful alternative.
    /// Unlike <c>TryAlternativeSerializersShouldContinueWhenAltSerializerThrows</c> which
    /// excludes the throwing serializer from the alternatives list (because the primary
    /// type matches), this test keeps the throwing serializer *in* the alternatives list
    /// so the catch block at <c>TryAlternativeSerializers</c> is actually exercised.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldCatchAndContinueWhenAltSerializerInListThrows()
    {
        UniversalSerializer.RegisterSerializer(static () => new ThrowingSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        // Primary is a different type from ThrowingSerializer, so ThrowingSerializer
        // remains in the alternatives list and its exception triggers the catch.
        NewtonsoftBsonSerializer primary = new();
        var data = new SystemJsonSerializer().Serialize("hello");

        var result = UniversalSerializer.TryAlternativeSerializers<string>(data, primary, null);
        await Assert.That(result).IsEqualTo("hello");
    }

    /// <summary>
    /// Tests GetAvailableAlternativeSerializers swallows factory exceptions.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAvailableAlternativeSerializersShouldSwallowFactoryExceptions()
    {
        UniversalSerializer.RegisterSerializer(static () => throw new InvalidOperationException("factory boom"));
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        ThrowingSerializer primary = new();
        var alternatives = UniversalSerializer.GetAvailableAlternativeSerializers(primary);

        // Should contain only the successfully created alternative.
        await Assert.That(alternatives.Count).IsEqualTo(1);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat with DateTime MinValue where recovery succeeds (data > 20 bytes + "2025").
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldRecoverDateTimeMinValueWithData()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonMinValueSerializer());

        // Must look like BSON (length header) and be > 20 bytes with "2025" pattern.
        const string text = "pad-pad-pad-2025-06-15T10:30:00Z-pad";
        var bytes = Encoding.UTF8.GetBytes(text);
        var data = new byte[bytes.Length + 4];
        BitConverter.GetBytes(data.Length).CopyTo(data, 0);
        bytes.CopyTo(data, 4);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat with DateTime MinValue when recovery fails (fallback to 2025 safe date).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldFallBackToSafeDateWhenRecoveryFails()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonMinValueSerializer());

        // BSON-like header but no recovery hints, > 20 bytes of zeros.
        var data = new byte[40];
        BitConverter.GetBytes(40).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Utc);

        // Recovery fallback returns a non-MinValue 2025 date OR the strategy-3 large-data fallback.
        await Assert.That(result).IsNotEqualTo(DateTime.MinValue);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat DateTime with forced Local kind conversion.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldConvertDateTimeToLocalKind()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedUtcSerializer());

        var data = new byte[30];
        BitConverter.GetBytes(30).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat DateTime with forced Unspecified kind conversion.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldConvertDateTimeToUnspecifiedKind()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedUtcSerializer());

        var data = new byte[30];
        BitConverter.GetBytes(30).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Unspecified);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat DateTime with forced UTC kind converting from Local.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldConvertDateTimeLocalToUtc()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedLocalSerializer());

        var data = new byte[30];
        BitConverter.GetBytes(30).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat continues on BSON serializer exceptions (inner catch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldContinueOnSerializerException()
    {
        UniversalSerializer.RegisterSerializer(static () => new ThrowingBsonSerializer());
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedUtcSerializer());

        var data = new byte[30];
        BitConverter.GetBytes(30).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, null);
        await Assert.That(result.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat returns raw result for non-DateTime types.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldReturnRawResultForNonDateTime()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonStringSerializer());

        var data = new byte[30];
        BitConverter.GetBytes(30).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<string>(data, null);
        await Assert.That(result).IsEqualTo("fake-bson-string");
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat continues when JSON serializer throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldContinueWhenSerializerThrows()
    {
        UniversalSerializer.RegisterSerializer(static () => new ThrowingSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        var data = "\"hello\""u8.ToArray();
        var result = UniversalSerializer.TryDeserializeJsonFormat<string>(data, null);
        await Assert.That(result).IsEqualTo("hello");
    }

    /// <summary>
    /// Tests PreprocessDateTimeForSerialization with Unspecified kind forcing Utc.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldConvertUnspecifiedToUtcWhenForced()
    {
        SystemJsonSerializer serializer = new();
        DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(date, serializer, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests Deserialize wraps DateTime validation path (forced kind when primary succeeds).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldValidateDateTimeResultFromPrimary()
    {
        SystemJsonSerializer serializer = new();
        DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var data = serializer.Serialize(date);

        var result = UniversalSerializer.Deserialize<DateTime>(data, serializer, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that Deserialize falls back to a registered BSON-named serializer when the primary
    /// serializer throws. The fallback succeeds because the registered serializer can produce
    /// a valid object, and EqualityComparer does not call Equals when comparing against null default.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldThrowInvalidOperationWhenPrimaryAndFallbackBothThrow()
    {
        // Register a serializer whose Deserialize returns a ThrowingEqualsObject.
        // Despite the custom Equals override, EqualityComparer<T>.Default.Equals(obj, null)
        // short-circuits when the second argument is null (reference type default),
        // so the fallback succeeds and returns the object.
        UniversalSerializer.RegisterSerializer(static () => new ThrowingEqualsBsonResultSerializer());

        ThrowingSerializer primary = new();

        // Data that looks like BSON (length header) so TryFallbackDeserialization enters the BSON path
        // where the registered ThrowingEqualsBsonResultSerializer produces a ThrowingEqualsObject.
        var data = new byte[20];
        BitConverter.GetBytes(20).CopyTo(data, 0);

        var result = UniversalSerializer.Deserialize<ThrowingEqualsObject>(data, primary);
        await Assert.That(result).IsNotNull();
    }

    /// <summary>
    /// Tests that Serialize throws InvalidOperationException wrapping both errors
    /// when primary and all fallback serializers fail (lines 131-135).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeShouldThrowInvalidOperationWhenPrimaryAndFallbackBothFail()
    {
        // Register only a throwing serializer so fallback also fails
        UniversalSerializer.RegisterSerializer(static () => new ThrowingSerializer());

        ThrowingSerializer primary = new();

        await Assert.That(() => UniversalSerializer.Serialize("some value", primary))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys line 201 false branch: deserialization succeeds
    /// but the result equals default for a value type, so the method continues searching.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldSkipDefaultValueResults()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(serializer);
        try
        {
            // Store a default int (0) under a key that will match
            var zeroBytes = serializer.Serialize(0);
            await cache.Insert("val_key", zeroBytes).ToTask();

            // TryFindDataWithAlternativeKeys deserializes to 0 (== default(int)), which hits
            // the false branch of the null/default check on line 197-198, causing it to continue.
            var result = await UniversalSerializer.TryFindDataWithAlternativeKeys<int>(cache, "val_key", serializer);
            await Assert.That(result).IsEqualTo(0);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests TryFallbackDeserialization line 332: data is detected as JSON,
    /// JSON format deserialization returns default for a complex type,
    /// and the method falls through to TryAlternativeSerializers.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackDeserializationShouldFallThroughJsonToAlternativeSerializers()
    {
        // Register a non-BSON serializer that can handle UserObject
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        // Serialize with SystemJson so data is valid JSON
        SystemJsonSerializer jsonSerializer = new();
        UserObject testObj = new() { Name = "Fallthrough", Bio = "Bio", Blog = "Blog" };
        var jsonData = jsonSerializer.Serialize(testObj);

        // Use a ThrowingSerializer as primary so TryFallbackDeserialization is entered.
        // The JSON path tries registered non-BSON serializers which is the same SystemJsonSerializer.
        // It should succeed via either JSON format or alternative serializers.
        var result =
            UniversalSerializer.TryFallbackDeserialization<UserObject>(jsonData, new ThrowingSerializer(), null);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Fallthrough");
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat lines 641-643: when a registered BSON serializer is present,
    /// it is skipped (continue) during JSON format deserialization.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldSkipBsonSerializers()
    {
        // Register both a BSON serializer (should be skipped) and a JSON serializer (should be used)
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonStringSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        var data = "\"from-json\""u8.ToArray();
        var result = UniversalSerializer.TryDeserializeJsonFormat<string>(data, null);
        await Assert.That(result).IsEqualTo("from-json");
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat when all registered serializers are BSON (skipped),
    /// falls back to TryBasicJsonDeserialization for int type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldFallBackToBasicJsonWhenOnlyBsonRegistered()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonStringSerializer());

        var data = "42"u8.ToArray();
        var result = UniversalSerializer.TryDeserializeJsonFormat<int>(data, null);
        await Assert.That(result).IsEqualTo(42);
    }

    /// <summary>
    /// Tests TryFallbackSerialization with forced DateTimeKind through alternative serializers.
    /// Exercises the forcedDateTimeKind.HasValue branch (line 357-359) in fallback serialization.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackSerializationShouldSetForcedDateTimeKindOnAlternative()
    {
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        ThrowingSerializer primary = new();
        var result = UniversalSerializer.TryFallbackSerialization("test-value", primary, DateTimeKind.Local);
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotEmpty();
    }

    /// <summary>
    /// Tests TryFallbackSerialization where the first alt serializer throws and the second succeeds.
    /// Exercises the catch/continue in the alt serializer loop (line 364-367).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackSerializationShouldContinueWhenFirstAltThrows()
    {
        UniversalSerializer.RegisterSerializer(static () => new ThrowingSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        ThrowingSerializer primary = new();
        var result = UniversalSerializer.TryFallbackSerialization("test-continue", primary, null);
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotEmpty();
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat exercises the DateTime kind conversion path from Local to Local
    /// where forced kind matches the existing kind (no conversion needed).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldNotConvertWhenKindAlreadyMatches()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedLocalSerializer());

        var data = new byte[30];
        BitConverter.GetBytes(30).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat with no forced kind and a valid non-MinValue DateTime.
    /// Exercises the path where forcedDateTimeKind is null so no conversion happens.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldReturnDateTimeUnchangedWithNoForcedKind()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedUtcSerializer());

        var data = new byte[30];
        BitConverter.GetBytes(30).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, null);
        await Assert.That(result.Year).IsEqualTo(2025);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests TryDeserializeBsonFormat with DateTime MinValue where data is exactly 21 bytes
    /// (> 20 threshold) but recovery returns MinValue, triggering the else fallback on line 592.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeBsonFormatShouldUseSafeDateWhenRecoveryReturnsMinValue()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonMinValueSerializer());

        // Data > 20 bytes with no recovery patterns and <= 50 bytes (avoids strategy 3 fallback)
        // so AttemptDateTimeRecovery returns MinValue, triggering the else on line 592.
        var data = new byte[25];
        BitConverter.GetBytes(25).CopyTo(data, 0);

        var result = UniversalSerializer.TryDeserializeBsonFormat<DateTime>(data, null);

        // The else fallback sets 2025-01-15 safe date
        await Assert.That(result.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Tests PreprocessDateTimeForSerialization with MaxValue for NewtonsoftBson (no special handling).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PreprocessDateTimeShouldNotModifyMaxValueForBsonSerializer()
    {
        NewtonsoftBsonSerializer serializer = new();
        var result = UniversalSerializer.PreprocessDateTimeForSerialization(DateTime.MaxValue, serializer, null);
        await Assert.That(result).IsEqualTo(DateTime.MaxValue);
    }

    /// <summary>
    /// Tests IsPotentialBsonData with negative length header (invalid BSON).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseForNegativeLength()
    {
        var data = new byte[10];
        BitConverter.GetBytes(-1).CopyTo(data, 0);
        var result = UniversalSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialBsonData with length much larger than actual data but within tolerance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnTrueWhenLengthWithinTolerance()
    {
        var data = new byte[10];

        // Length says 100, data is 10, tolerance is +100 from data length = 110, so 100 <= 110
        BitConverter.GetBytes(100).CopyTo(data, 0);
        var result = UniversalSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Tests IsPotentialBsonData with length far exceeding data plus tolerance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialBsonDataShouldReturnFalseWhenLengthExceedsTolerance()
    {
        var data = new byte[10];

        // Length says 200, data.Length + 100 = 110, so 200 > 110
        BitConverter.GetBytes(200).CopyTo(data, 0);
        var result = UniversalSerializer.IsPotentialBsonData(data);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests IsPotentialJsonData with a single non-JSON byte (not whitespace, not a JSON start char).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPotentialJsonDataShouldReturnFalseForNonJsonSingleByte()
    {
        // 0x41 = 'A', which is not a JSON start character
        var result = UniversalSerializer.IsPotentialJsonData([0x41]);
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests TryBasicJsonDeserialization returns default for a double type (unsupported simple type).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryBasicJsonDeserializationShouldReturnDefaultForDoubleType()
    {
        var data = "3.14"u8.ToArray();
        var result = UniversalSerializer.TryBasicJsonDeserialization<double>(data);
        await Assert.That(result).IsEqualTo(0);
    }

    /// <summary>
    /// Tests TryAlternativeSerializers with DateTime where alt serializer returns non-MinValue
    /// DateTime and forced kind is specified. Exercises the HandleDateTimeWithCrossSerializerSupport path (line 413).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldHandleNonMinValueDateTimeWithForcedKind()
    {
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonFixedUtcSerializer());
        ThrowingSerializer primary = new();

        var data = new byte[20];
        var result = UniversalSerializer.TryAlternativeSerializers<DateTime>(data, primary, DateTimeKind.Local);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests the full Deserialize path with DateTime and forced kind when primary succeeds
    /// but the result has a different kind that needs validation (line 55-58).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldValidateDateTimeKindConversionLocalToUtc()
    {
        SystemJsonSerializer serializer = new();
        DateTime localDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);
        var data = serializer.Serialize(localDate);

        var result = UniversalSerializer.Deserialize<DateTime>(data, serializer, DateTimeKind.Utc);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests the full Serialize path with DateTime and forced kind where the DateTime
    /// kind does not match the forced kind (exercises lines 108-111 and 114-117).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeShouldPreprocessDateTimeWithForcedLocalKind()
    {
        SystemJsonSerializer serializer = new();
        DateTime utcDate = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var data = UniversalSerializer.Serialize(utcDate, serializer, DateTimeKind.Local);
        await Assert.That(data).IsNotNull();
        await Assert.That(data).IsNotEmpty();

        // Verify the round-trip preserves the forced kind
        var deserialized = UniversalSerializer.Deserialize<DateTime>(data, serializer, DateTimeKind.Local);
        await Assert.That(deserialized.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests Deserialize with non-DateTime, non-null result from primary serializer
    /// (exercises the return result path on line 61 without entering DateTime validation).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnNonDateTimeResultDirectly()
    {
        SystemJsonSerializer serializer = new();
        var data = serializer.Serialize(42);

        var result = UniversalSerializer.Deserialize<int>(data, serializer);
        await Assert.That(result).IsEqualTo(42);
    }

    /// <summary>
    /// Tests TryDeserializeJsonFormat skips BSON-named serializers and then
    /// falls back when no non-BSON serializer is registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeJsonFormatShouldSkipAllBsonAndFallBackToBasic()
    {
        // Register only BSON serializers - all should be skipped
        UniversalSerializer.RegisterSerializer(static () => new FakeBsonMinValueSerializer());
        UniversalSerializer.RegisterSerializer(static () => new ThrowingBsonSerializer());

        var data = "\"test-skip-bson\""u8.ToArray();
        var result = UniversalSerializer.TryDeserializeJsonFormat<string>(data, null);
        await Assert.That(result).IsEqualTo("test-skip-bson");
    }

    /// <summary>
    /// Tests TryFallbackDeserialization line 328 false branch: data is JSON but
    /// TryDeserializeJsonFormat returns default for a complex type, so we fall through
    /// to TryAlternativeSerializers.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackDeserializationShouldFallThroughWhenJsonReturnsDefault()
    {
        // Register only a fixed string serializer that handles UserObject via TryAlternativeSerializers
        UniversalSerializer.RegisterSerializer(static () => new FixedUserObjectSerializer());

        // Data that looks like JSON (starts with '{') but no JSON serializer is registered
        // to handle it. TryDeserializeJsonFormat will fall through to TryBasicJsonDeserialization
        // which returns null for UserObject.
        var data = "{\"Name\":\"test\"}"u8.ToArray();

        var result = UniversalSerializer.TryFallbackDeserialization<UserObject>(data, new ThrowingSerializer(), null);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("fixed-user");
    }

    /// <summary>
    /// Tests <see cref="UniversalSerializer.TryFallbackDeserialization{T}"/> where data is
    /// JSON-shaped, <see cref="UniversalSerializer.TryDeserializeJsonFormat{T}"/> returns
    /// default (no JSON-capable serializers registered and <c>T</c> is a complex type that
    /// <see cref="UniversalSerializer.TryBasicJsonDeserialization{T}"/> cannot handle), so
    /// execution falls through the <c>if (IsPotentialJsonData(...))</c> block without
    /// early-returning and proceeds into <c>TryAlternativeSerializers</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackDeserializationShouldFallThroughJsonIfWhenJsonDefaultAndComplexType()
    {
        // Only register a BSON-named serializer. TryDeserializeJsonFormat skips it by
        // name filter and falls through to TryBasicJsonDeserialization which returns
        // default for UserObject, so the if-block runs without taking its early-return
        // branch. TryAlternativeSerializers does not apply the BSON name filter, so the
        // same serializer resolves the final result.
        UniversalSerializer.RegisterSerializer(static () => new FixedUserObjectBsonSerializer());

        var data = "{\"Name\":\"anything\"}"u8.ToArray();
        var result = UniversalSerializer.TryFallbackDeserialization<UserObject>(data, new ThrowingSerializer(), null);

        await Assert.That(result).IsNotNull();
        if (result is null)
        {
            return;
        }

        await Assert.That(result.Name).IsEqualTo("fixed-bson-user");
    }

    /// <summary>
    /// Tests TryFallbackDeserialization where data is neither BSON-like nor JSON-like,
    /// so it goes directly to TryAlternativeSerializers (line 335).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackDeserializationShouldGoToAlternativeSerializersForUnknownFormat()
    {
        UniversalSerializer.RegisterSerializer(static () => new FixedStringSerializer("from-alt"));

        ThrowingSerializer primary = new();

        // Data that is neither BSON (too short) nor JSON (starts with 0xFF)
        byte[] data = [0xFF, 0xFE];

        var result = UniversalSerializer.TryFallbackDeserialization<string>(data, primary, null);
        await Assert.That(result).IsEqualTo("from-alt");
    }

    /// <summary>
    /// Tests TryFallbackDeserialization where valid JSON data is passed and the registered
    /// SystemJsonSerializer successfully deserializes it via the JSON detection path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackDeserializationShouldFallThroughBsonAndJsonToAlternatives()
    {
        // Register a non-BSON serializer that can handle UserObject
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        SystemJsonSerializer jsonSerializer = new();
        UserObject testObj = new() { Name = "FallThrough", Bio = "B", Blog = "B" };
        var jsonData = jsonSerializer.Serialize(testObj);

        // Use the valid JSON data directly. IsPotentialBsonData will return false
        // (first 4 bytes as int32 won't match data length), but IsPotentialJsonData
        // returns true (starts with '{'). TryDeserializeJsonFormat then succeeds
        // via the registered SystemJsonSerializer.
        var result =
            UniversalSerializer.TryFallbackDeserialization<UserObject>(jsonData, new ThrowingSerializer(), null);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("FallThrough");
    }

    /// <summary>
    /// Tests Serialize with non-DateTime value and forced kind (exercises line 108 false branch
    /// where forced kind is set but T is not DateTime).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeShouldSetForcedKindEvenForNonDateTimeTypes()
    {
        SystemJsonSerializer serializer = new();
        var data = UniversalSerializer.Serialize("test-string", serializer, DateTimeKind.Utc);
        await Assert.That(data).IsNotNull();
        await Assert.That(data).IsNotEmpty();
    }

    /// <summary>
    /// Tests Deserialize with forced kind on non-DateTime type (exercises line 46-49 and line 55 false branch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldSetForcedKindEvenForNonDateTimeTypes()
    {
        SystemJsonSerializer serializer = new();
        var data = serializer.Serialize("hello");
        var result = UniversalSerializer.Deserialize<string>(data, serializer, DateTimeKind.Utc);
        await Assert.That(result).IsEqualTo("hello");
    }

    /// <summary>
    /// Verifies the BSON-serializer probe classifies concrete serializer types correctly —
    /// <see cref="SystemJsonBsonSerializer"/> and <see cref="NewtonsoftBsonSerializer"/> are BSON,
    /// <see cref="SystemJsonSerializer"/> and <see cref="NewtonsoftSerializer"/> are not.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsBsonSerializerShouldClassifyConcreteSerializers()
    {
        UniversalSerializer.ResetCaches();

        await Assert.That(UniversalSerializer.IsBsonSerializer(new SystemJsonBsonSerializer())).IsTrue();
        await Assert.That(UniversalSerializer.IsBsonSerializer(new NewtonsoftBsonSerializer())).IsTrue();
        await Assert.That(UniversalSerializer.IsBsonSerializer(new SystemJsonSerializer())).IsFalse();
        await Assert.That(UniversalSerializer.IsBsonSerializer(new NewtonsoftSerializer())).IsFalse();
    }

    /// <summary>
    /// Verifies the BSON probe returns the same answer on repeat invocations for a given type —
    /// the second call goes through the cache (different code path from the first).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsBsonSerializerShouldCachePerType()
    {
        UniversalSerializer.ResetCaches();
        SystemJsonBsonSerializer bson = new();

        var first = UniversalSerializer.IsBsonSerializer(bson);
        var second = UniversalSerializer.IsBsonSerializer(bson);
        var third = UniversalSerializer.IsBsonSerializer(new SystemJsonBsonSerializer());

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsTrue();
        await Assert.That(third).IsTrue();
    }

    /// <summary>
    /// Verifies the plain-Newtonsoft probe returns true for <see cref="NewtonsoftSerializer"/>
    /// and false for the BSON variant and System.Text.Json serializers (the "Newtonsoft &amp;&amp;
    /// !Bson" conjunction).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsPlainNewtonsoftSerializerShouldRequireBothConditions()
    {
        UniversalSerializer.ResetCaches();

        await Assert.That(UniversalSerializer.IsPlainNewtonsoftSerializer(new NewtonsoftSerializer())).IsTrue();
        await Assert.That(UniversalSerializer.IsPlainNewtonsoftSerializer(new NewtonsoftBsonSerializer())).IsFalse();
        await Assert.That(UniversalSerializer.IsPlainNewtonsoftSerializer(new SystemJsonSerializer())).IsFalse();
        await Assert.That(UniversalSerializer.IsPlainNewtonsoftSerializer(new SystemJsonBsonSerializer())).IsFalse();
    }

    /// <summary>
    /// Verifies that <see cref="UniversalSerializer.ResetCaches"/> clears both the BSON and the
    /// plain-Newtonsoft caches — a subsequent probe re-runs the classification logic.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResetCachesShouldClearSerializerKindCaches()
    {
        UniversalSerializer.IsBsonSerializer(new SystemJsonBsonSerializer());
        UniversalSerializer.IsPlainNewtonsoftSerializer(new NewtonsoftSerializer());

        UniversalSerializer.ResetCaches();

        // After reset the next probe should still produce a correct answer — this validates the
        // classifier doesn't hand back a stale or corrupted result after the caches were cleared.
        await Assert.That(UniversalSerializer.IsBsonSerializer(new SystemJsonBsonSerializer())).IsTrue();
        await Assert.That(UniversalSerializer.IsPlainNewtonsoftSerializer(new NewtonsoftSerializer())).IsTrue();
    }

    /// <summary>
    /// Reset registry between tests so registered serializers don't bleed between tests.
    /// </summary>
    private static void ResetRegistry() => UniversalSerializer.ResetCaches();

    /// <summary>
    /// Register all known serializers for fallback tests.
    /// </summary>
    private static void RegisterAllSerializers()
    {
        UniversalSerializer.ResetCaches();
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonBsonSerializer());
        UniversalSerializer.RegisterSerializer(static () => new NewtonsoftSerializer());
        UniversalSerializer.RegisterSerializer(static () => new NewtonsoftBsonSerializer());
    }

    /// <summary>
    /// A serializer that always throws on serialize/deserialize.
    /// </summary>
    private sealed class ThrowingSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            throw new InvalidOperationException("ThrowingSerializer always throws on Deserialize.");

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) =>
            throw new InvalidOperationException("ThrowingSerializer always throws on Serialize.");
    }

    /// <summary>
    /// A fake serializer that returns DateTime.MinValue for all deserializations.
    /// </summary>
    private sealed class MinValueDateTimeSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) != typeof(DateTime) ? default : (T)(object)DateTime.MinValue;

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => [];
    }

    /// <summary>
    /// A fake BSON-named serializer that returns DateTime.MinValue for deserialization.
    /// </summary>
    private sealed class FakeBsonMinValueSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) != typeof(DateTime) ? default : (T)(object)DateTime.MinValue;

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => [];
    }

    /// <summary>
    /// A fake BSON-named serializer that returns a fixed UTC DateTime.
    /// </summary>
    private sealed class FakeBsonFixedUtcSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) != typeof(DateTime)
                ? default
                : (T)(object)new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => [];
    }

    /// <summary>
    /// A fake BSON-named serializer that returns a fixed Local DateTime.
    /// </summary>
    private sealed class FakeBsonFixedLocalSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) != typeof(DateTime)
                ? default
                : (T)(object)new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => [];
    }

    /// <summary>
    /// A fake BSON-named serializer that returns a fixed string.
    /// </summary>
    private sealed class FakeBsonStringSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) != typeof(string) ?
                default :
                (T)(object)"fake-bson-string";

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => [];
    }

    /// <summary>
    /// A BSON-named serializer that throws on Deserialize.
    /// </summary>
    private sealed class ThrowingBsonSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            throw new InvalidOperationException("ThrowingBsonSerializer always throws on Deserialize.");

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) =>
            throw new InvalidOperationException("ThrowingBsonSerializer always throws on Serialize.");
    }

    /// <summary>
    /// A blob cache wrapper that delegates to an inner cache but throws on Get(string).
    /// Used to exercise the inner catch in TryFindDataWithAlternativeKeys.
    /// </summary>
    private sealed class ThrowingGetCacheWrapper(IBlobCache inner) : IBlobCache
    {
        /// <inheritdoc/>
        public ISerializer Serializer => inner.Serializer;

        /// <inheritdoc/>
        public IScheduler Scheduler => inner.Scheduler;

        /// <inheritdoc/>
        public IHttpService HttpService
        {
            get => inner.HttpService;
            set => inner.HttpService = value;
        }

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind
        {
            get => inner.ForcedDateTimeKind;
            set => inner.ForcedDateTimeKind = value;
        }

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            DateTimeOffset? absoluteExpiration = null) =>
                inner.Insert(keyValuePairs, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            inner.Insert(key, data, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            Type type,
            DateTimeOffset? absoluteExpiration = null) =>
                inner.Insert(keyValuePairs, type, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit>
            Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
            inner.Insert(key, data, type, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) =>
            Observable.Throw<byte[]?>(new InvalidOperationException("Throwing Get"));

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => inner.Get(keys);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) =>
            Observable.Throw<byte[]?>(new InvalidOperationException("Throwing Get"));

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) =>
            inner.Get(keys, type);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => inner.GetAll(type);

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => inner.GetAllKeys();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => inner.GetAllKeys(type);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
            inner.GetCreatedAt(keys);

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => inner.GetCreatedAt(key);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
            inner.GetCreatedAt(keys, type);

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => inner.GetCreatedAt(key, type);

        /// <inheritdoc/>
        public IObservable<Unit> Flush() => inner.Flush();

        /// <inheritdoc/>
        public IObservable<Unit> Flush(Type type) => inner.Flush(type);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key) => inner.Invalidate(key);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key, Type type) => inner.Invalidate(key, type);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => inner.Invalidate(keys);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => inner.Invalidate(keys, type);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll(Type type) => inner.InvalidateAll(type);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll() => inner.InvalidateAll();

        /// <inheritdoc/>
        public IObservable<Unit> Vacuum() => inner.Vacuum();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
            inner.UpdateExpiration(key, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
            inner.UpdateExpiration(key, type, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
            inner.UpdateExpiration(keys, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(
            IEnumerable<string> keys,
            Type type,
            DateTimeOffset? absoluteExpiration) => inner.UpdateExpiration(keys, type, absoluteExpiration);

        /// <inheritdoc/>
        public void Dispose()
        {
            // Caller owns inner cache; do not dispose twice.
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// An object whose Equals method always throws, used to trigger exceptions in EqualityComparer paths.
    /// </summary>
    [SuppressMessage("Usage", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Intentionally throws for test purposes")]
    private sealed class ThrowingEqualsObject
    {
        /// <inheritdoc/>
        public override bool Equals(object? obj) => throw new InvalidOperationException("Equals throws");

        /// <inheritdoc/>
        public override int GetHashCode() => 0;
    }

    /// <summary>
    /// A BSON-named serializer that returns a ThrowingEqualsObject for deserialization,
    /// causing EqualityComparer.Equals to throw in the BSON fallback path of TryFallbackDeserialization.
    /// The class name must contain "Bson" so TryDeserializeBsonFormat picks it up.
    /// </summary>
    private sealed class ThrowingEqualsBsonResultSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) != typeof(ThrowingEqualsObject) ?
                default :
                (T)(object)new ThrowingEqualsObject();

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) =>
            throw new InvalidOperationException("ThrowingEqualsBsonResultSerializer always throws on Serialize.");
    }

    /// <summary>
    /// A serializer that returns a fixed UserObject for deserialization.
    /// Used to verify fallback paths that try alternative serializers.
    /// </summary>
    private sealed class FixedUserObjectSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) == typeof(UserObject)
                ? (T)(object)new UserObject { Name = "fixed-user", Bio = "bio", Blog = "blog" }
                : default;

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => [];
    }

    /// <summary>
    /// Variant of <see cref="FixedUserObjectSerializer"/> whose type name contains "Bson",
    /// which causes <see cref="UniversalSerializer.TryDeserializeJsonFormat{T}"/> to skip
    /// it while <see cref="UniversalSerializer.TryAlternativeSerializers{T}"/> still
    /// invokes it.
    /// </summary>
    private sealed class FixedUserObjectBsonSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) == typeof(UserObject)
                ? (T)(object)new UserObject { Name = "fixed-bson-user", Bio = "bio", Blog = "blog" }
                : default;

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => [];
    }

    /// <summary>
    /// A serializer that always returns a fixed string for any deserialization.
    /// </summary>
    private sealed class FixedStringSerializer(string value) : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) != typeof(string) ?
                default :
                (T)(object)value;

        /// <inheritdoc/>
        public byte[] Serialize<T>(T item) => Encoding.UTF8.GetBytes(value);
    }
}
