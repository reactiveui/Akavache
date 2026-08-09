// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for the top-level <see cref="UniversalSerializer"/> serialize and deserialize
/// entry points, including null and empty payload handling, cross-serializer round trips
/// and the candidate-deserialization helper.
/// </summary>
/// <remarks>
/// The whole assembly runs under one unkeyed <see cref="NotInParallelAttribute"/> group,
/// so these tests serialise against every other test that mutates global state like
/// <see cref="UniversalSerializer"/>'s registered-factory cache, <see cref="CacheDatabase"/>,
/// <see cref="AkavacheBuilder"/>'s static stores, and <c>Splat.AppLocator</c>.
/// </remarks>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class UniversalSerializerTests
{
    /// <summary>The item count carried in the metadata of the complex test object.</summary>
    private const int MetadataItemCount = 42;

    /// <summary>The integer value round-tripped through a serializer with no DateTime handling.</summary>
    private const int RoundTripIntegerValue = 42;

    /// <summary>Tests that UniversalSerializer can deserialize data from primary serializer.</summary>
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

    /// <summary>Tests that UniversalSerializer handles null/empty data correctly.</summary>
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

    /// <summary>Tests that UniversalSerializer can serialize data with target serializer.</summary>
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

    /// <summary>Tests that UniversalSerializer handles null values correctly.</summary>
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

    /// <summary>Tests that UniversalSerializer can handle cross-serializer scenarios.</summary>
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

    /// <summary>Tests that UniversalSerializer throws appropriate exceptions for invalid input.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldThrowForInvalidInput()
    {
        // Arrange & Act & Assert - Test null serializer
        byte[] arbitraryPayload = [1, 2, 3];

        _ = Assert.Throws<ArgumentNullException>(() =>
            UniversalSerializer.Deserialize<string>(arbitraryPayload, null!));

        _ = Assert.Throws<ArgumentNullException>(static () =>
            UniversalSerializer.Serialize("test", null!));

        // Test null data - should return null rather than throw for empty/null data
        var nullDataResult = UniversalSerializer.Deserialize<string>(null!, new SystemJsonSerializer());
        await Assert.That(nullDataResult).IsNull();

        var emptyDataResult = UniversalSerializer.Deserialize<string>([], new SystemJsonSerializer());
        await Assert.That(emptyDataResult).IsNull();
    }

    /// <summary>Tests that UniversalSerializer can handle BSON data detection.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldDetectBsonData()
    {
        // Arrange
        NewtonsoftBsonSerializer bsonSerializer = new();
        UserObject testObject = new() { Name = "BSON Test", Bio = "BSON Bio", Blog = "BSON Blog" };
        var bsonData = bsonSerializer.Serialize(testObject);

        // Act - Try to deserialize BSON data with a different serializer
        _ = UniversalSerializer.Deserialize<UserObject>(bsonData, new SystemJsonSerializer());

        // Assert - Should either succeed with fallback or fail gracefully
        // The main goal is no unhandled exceptions
        // Act & Assert
        // This clearly states that the enclosed code should not throw an exception.
        await Assert.That(() => UniversalSerializer.Deserialize<UserObject>(bsonData, new SystemJsonSerializer()))
            .ThrowsNothing();
    }

    /// <summary>Tests that UniversalSerializer can handle JSON data detection.</summary>
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

    /// <summary>Tests that UniversalSerializer handles serialization failures gracefully.</summary>
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
            _ = Assert.Throws<InvalidOperationException>(() => UniversalSerializer.Serialize(circularRef, serializer));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that UniversalSerializer can handle complex object hierarchies.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task UniversalSerializerShouldHandleComplexObjects()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        int[] numberSequence = [1, 2, 3, 4, 5];
        ComplexGraphPayload complexObject = new()
        {
            User = new UserObject { Name = "Complex User", Bio = "Complex Bio", Blog = "Complex Blog" },
            Date = TimeProvider.System.GetUtcNow().UtcDateTime,
            Numbers = numberSequence,
            Metadata = new Dictionary<string, object> { ["version"] = "1.0", ["enabled"] = true, ["count"] = MetadataItemCount },
        };

        // Act
        var serializedData = UniversalSerializer.Serialize(complexObject, serializer);

        // The graph is not round-tripped back, so just verify serialization succeeds
        await Assert.That(serializedData).IsNotNull();
        await Assert.That(serializedData).IsNotEmpty();
    }

    /// <summary>Tests <see cref="UniversalSerializer.TryDeserializeCandidate{T}"/> returns <see langword="false"/> for null raw data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeCandidateShouldReturnFalseForNullRawData()
    {
        var succeeded =
            UniversalSerializer.TryDeserializeCandidate<UserObject>(null, new SystemJsonSerializer(), out var result);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests <see cref="UniversalSerializer.TryDeserializeCandidate{T}"/> returns <see langword="false"/> for a zero-length raw data array.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeCandidateShouldReturnFalseForEmptyRawData()
    {
        var succeeded =
            UniversalSerializer.TryDeserializeCandidate<UserObject>([], new SystemJsonSerializer(), out var result);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests <see cref="UniversalSerializer.TryDeserializeCandidate{T}"/> returns <see langword="false"/> when the deserialized value equals the default of its type parameter.</summary>
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

    /// <summary>Tests <see cref="UniversalSerializer.TryDeserializeCandidate{T}"/> returns <see langword="true"/> and emits the value when deserialization succeeds with a non-default value.</summary>
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
    /// failing serializer through <c>UniversalSerializer.Deserialize&lt;T&gt;</c>'s
    /// internal fallback and returns <see langword="false"/> when no alternatives
    /// resolve the value. Exception propagation is no longer wrapped inside
    /// <c>TryDeserializeCandidate</c> itself because <c>Deserialize</c> is already
    /// exception-safe.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeserializeCandidateShouldReturnFalseWhenDeserializerFailsWithNoAlternatives()
    {
        byte[] unparseablePayload = [1, 2, 3, 4];

        var succeeded =
            UniversalSerializer.TryDeserializeCandidate<UserObject>(
                unparseablePayload,
                new ThrowingSerializer(),
                out var result);

        await Assert.That(succeeded).IsFalse();
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests Serialize throws InvalidOperationException for circular references.</summary>
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
    /// Tests Deserialize with non-DateTime, non-null result from primary serializer
    /// (exercises the return result path on line 61 without entering DateTime validation).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldReturnNonDateTimeResultDirectly()
    {
        SystemJsonSerializer serializer = new();
        var data = serializer.Serialize(RoundTripIntegerValue);

        var result = UniversalSerializer.Deserialize<int>(data, serializer);
        await Assert.That(result).IsEqualTo(RoundTripIntegerValue);
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

    /// <summary>Tests Deserialize with forced kind on non-DateTime type (exercises line 46-49 and line 55 false branch).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldSetForcedKindEvenForNonDateTimeTypes()
    {
        SystemJsonSerializer serializer = new();
        var data = serializer.Serialize("hello");
        var result = UniversalSerializer.Deserialize<string>(data, serializer, DateTimeKind.Utc);
        await Assert.That(result).IsEqualTo("hello");
    }

    /// <summary>An object graph mixing a nested reference type, a date, an array and a dictionary.</summary>
    private sealed record ComplexGraphPayload
    {
        /// <summary>Gets the nested reference-typed member.</summary>
        public UserObject? User { get; init; }

        /// <summary>Gets the date member, which forces the DateTime handling path during serialization.</summary>
        public DateTime Date { get; init; }

        /// <summary>Gets the array member.</summary>
        public IReadOnlyList<int>? Numbers { get; init; }

        /// <summary>Gets the loosely typed metadata member.</summary>
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    }
}
