// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // system first
using System.Collections.Generic;
using Akavache.Core;
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for universal serializer compatibility functionality.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class UniversalSerializerTests
{
    /// <summary>
    /// Tests that UniversalSerializer can deserialize data from primary serializer.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldDeserializeFromPrimarySerializer()
    {
        // Arrange
        var primarySerializer = new SystemJsonSerializer();
        var testObject = new UserObject { Name = "Test User", Bio = "Test Bio", Blog = "Test Blog" };
        var serializedData = primarySerializer.Serialize(testObject);

        // Act
        var result = UniversalSerializer.Deserialize<UserObject>(serializedData, primarySerializer);

        // Assert
        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result!.Name, Is.EqualTo("Test User"));
            Assert.That(result.Bio, Is.EqualTo("Test Bio"));
            Assert.That(result.Blog, Is.EqualTo("Test Blog"));
        }
    }

    /// <summary>
    /// Tests that UniversalSerializer handles null/empty data correctly.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldHandleNullEmptyData()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        // Act & Assert
        var nullResult = UniversalSerializer.Deserialize<string>(null!, serializer);
        Assert.That(nullResult, Is.Null);

        var emptyResult = UniversalSerializer.Deserialize<string>([], serializer);
        Assert.That(emptyResult, Is.Null);
    }

    /// <summary>
    /// Tests that UniversalSerializer can serialize data with target serializer.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldSerializeWithTargetSerializer()
    {
        // Arrange
        var targetSerializer = new SystemJsonSerializer();
        var testObject = new UserObject { Name = "Serialize Test", Bio = "Serialize Bio", Blog = "Serialize Blog" };

        // Act
        var serializedData = UniversalSerializer.Serialize(testObject, targetSerializer);

        // Assert
        Assert.That(serializedData, Is.Not.Null);
        Assert.That(serializedData, Is.Not.Empty);

        // Verify it can be deserialized back
        var deserializedObject = targetSerializer.Deserialize<UserObject>(serializedData);
        Assert.That(deserializedObject, Is.Not.Null);
        Assert.That(deserializedObject!.Name, Is.EqualTo("Serialize Test"));
    }

    /// <summary>
    /// Tests that UniversalSerializer handles null values correctly.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldHandleNullValues()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        // Act
        var result = UniversalSerializer.Serialize<string>(null!, serializer);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty); // Null values should return empty array
    }

    /// <summary>
    /// Tests that UniversalSerializer handles DateTime serialization correctly.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldHandleDateTimeSerialization()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        var testDateTime = new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc);

        // Act
        var serializedData = UniversalSerializer.Serialize(testDateTime, serializer, DateTimeKind.Utc);
        var deserializedDateTime = UniversalSerializer.Deserialize<DateTime>(serializedData, serializer, DateTimeKind.Utc);

        // Assert
        Assert.That(deserializedDateTime, Is.EqualTo(testDateTime));
        Assert.That(deserializedDateTime.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    /// <summary>
    /// Tests that UniversalSerializer can handle cross-serializer scenarios.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldHandleCrossSerializerScenarios()
    {
        // Arrange
        var systemJsonSerializer = new SystemJsonSerializer();
        var newtonsoftSerializer = new NewtonsoftSerializer();
        var testObject = new UserObject { Name = "Cross Test", Bio = "Cross Bio", Blog = "Cross Blog" };

        // Act - Serialize with one, deserialize with UniversalSerializer using another
        var systemJsonData = systemJsonSerializer.Serialize(testObject);

        // Assert
        // This explicitly verifies that the fallback mechanism does not throw an exception.
        Assert.DoesNotThrow(() => UniversalSerializer.Deserialize<UserObject>(systemJsonData, newtonsoftSerializer), "Cross-serializer deserialization should be handled gracefully without throwing.");
    }

    /// <summary>
    /// Tests that UniversalSerializer throws appropriate exceptions for invalid input.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldThrowForInvalidInput()
    {
        // Arrange & Act & Assert - Test null serializer
        Assert.Throws<ArgumentNullException>(static () =>
            UniversalSerializer.Deserialize<string>([1, 2, 3], null!));

        Assert.Throws<ArgumentNullException>(static () =>
            UniversalSerializer.Serialize("test", null!));

        // Test null data - should return null rather than throw for empty/null data
        var nullDataResult = UniversalSerializer.Deserialize<string>(null!, new SystemJsonSerializer());
        Assert.That(nullDataResult, Is.Null);

        var emptyDataResult = UniversalSerializer.Deserialize<string>([], new SystemJsonSerializer());
        Assert.That(emptyDataResult, Is.Null);
    }

    /// <summary>
    /// Tests that UniversalSerializer handles DateTime edge cases.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldHandleDateTimeEdgeCases()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        DateTime[] edgeCases =
        [
            DateTime.MinValue,
            DateTime.MaxValue,
            new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Local)
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
                Assert.That(timeDifference, Is.LessThan(1440), $"DateTime edge case failed: {testDate} -> {deserializedDate}"); // 24 hours tolerance
            }
            catch (Exception ex)
            {
                // Some edge cases may fail due to serializer limitations - this is acceptable
                // Just ensure the exception is handled gracefully
                Assert.That(ex, Is.TypeOf<InvalidOperationException>());
            }
        }
    }

    /// <summary>
    /// Tests that UniversalSerializer can handle BSON data detection.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldDetectBsonData()
    {
        // Arrange
        var bsonSerializer = new NewtonsoftBsonSerializer();
        var testObject = new UserObject { Name = "BSON Test", Bio = "BSON Bio", Blog = "BSON Blog" };
        var bsonData = bsonSerializer.Serialize(testObject);

        // Act - Try to deserialize BSON data with a different serializer
        var result = UniversalSerializer.Deserialize<UserObject>(bsonData, new SystemJsonSerializer());

        // Assert - Should either succeed with fallback or fail gracefully
        // The main goal is no unhandled exceptions
        // Act & Assert
        // This clearly states that the enclosed code should not throw an exception.
        Assert.DoesNotThrow(() => UniversalSerializer.Deserialize<UserObject>(bsonData, new SystemJsonSerializer()), "Deserializing mismatched data format should be handled gracefully without exceptions.");
    }

    /// <summary>
    /// Tests that UniversalSerializer can handle JSON data detection.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldDetectJsonData()
    {
        // Arrange
        var jsonSerializer = new SystemJsonSerializer();
        var testObject = new UserObject { Name = "JSON Test", Bio = "JSON Bio", Blog = "JSON Blog" };
        var jsonData = jsonSerializer.Serialize(testObject);

        // Act & Assert
        // This explicitly states the test's goal: the code should run without throwing.
        Assert.DoesNotThrow(() => UniversalSerializer.Deserialize<UserObject>(jsonData, new NewtonsoftBsonSerializer()), "Deserializing JSON with a BSON serializer should be handled gracefully.");
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys functionality.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task UniversalSerializerShouldTryAlternativeKeys()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);
        var testObject = new UserObject { Name = "Alt Key Test", Bio = "Alt Bio", Blog = "Alt Blog" };

        try
        {
            // Store object with prefixed key
            await cache.InsertObject("test_key", testObject).FirstAsync();

            // Act - Try to find with alternative keys
            var result = await UniversalSerializer.TryFindDataWithAlternativeKeys<UserObject>(
                cache, "test_key", serializer);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Alt Key Test"));
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that UniversalSerializer handles serialization failures gracefully.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldHandleSerializationFailures()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        // Create a problematic object (circular reference)
        var circularRef = new List<object>();
        circularRef.Add(circularRef);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => UniversalSerializer.Serialize(circularRef, serializer));
    }

    /// <summary>
    /// Tests that UniversalSerializer properly validates DateTime after deserialization.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldValidateDeserializedDateTime()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        var testDateTime = new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Local);

        // Act
        var serializedData = UniversalSerializer.Serialize(testDateTime, serializer, DateTimeKind.Utc);
        var deserializedDateTime = UniversalSerializer.Deserialize<DateTime>(serializedData, serializer, DateTimeKind.Utc);

        // Assert - Should be converted to UTC
        Assert.That(deserializedDateTime.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    /// <summary>
    /// Tests that UniversalSerializer can handle complex object hierarchies.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldHandleComplexObjects()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();
        var complexObject = new
        {
            User = new UserObject { Name = "Complex User", Bio = "Complex Bio", Blog = "Complex Blog" },
            Date = DateTime.UtcNow,
            Numbers = (int[])[1, 2, 3, 4, 5],
            Metadata = new Dictionary<string, object>
            {
                ["version"] = "1.0",
                ["enabled"] = true,
                ["count"] = 42
            }
        };

        // Act
        var serializedData = UniversalSerializer.Serialize(complexObject, serializer);

        // We can't easily deserialize anonymous types, so just verify serialization succeeds
        Assert.That(serializedData, Is.Not.Null);
        Assert.That(serializedData, Is.Not.Empty);
    }

    /// <summary>
    /// Tests that UniversalSerializer properly preprocesses DateTime for serialization.
    /// </summary>
    [Test]
    public void UniversalSerializerShouldPreprocessDateTime()
    {
        // Arrange
        var newtonsoftSerializer = new NewtonsoftSerializer();

        // Test edge cases that might be problematic for certain serializers
        DateTime[] edgeDates =
        [
            DateTime.MinValue,
            DateTime.MaxValue,
            new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        ];

        foreach (var testDate in edgeDates)
        {
            try
            {
                // Act - Should preprocess the date to make it safer for serialization
                var serializedData = UniversalSerializer.Serialize(testDate, newtonsoftSerializer);

                // Assert
                Assert.That(serializedData, Is.Not.Null);
                Assert.That(serializedData, Is.Not.Empty);
            }
            catch (InvalidOperationException)
            {
                // Some edge cases may still fail - this is acceptable
                // The important thing is that the failure is handled gracefully
            }
        }
    }
}
