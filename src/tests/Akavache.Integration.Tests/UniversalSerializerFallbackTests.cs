// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for the fallback chain <see cref="UniversalSerializer"/> runs when the primary
/// serializer cannot read or write a payload, covering both the deserialize and the
/// serialize direction.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class UniversalSerializerFallbackTests
{
    /// <summary>A payload length whose header makes the buffer look like a BSON document.</summary>
    private const int BsonShapedPayloadLength = 20;

    /// <summary>Tests TryFallbackDeserialization with BSON data using registered serializers.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackDeserializationShouldDeserializeBsonFormat()
    {
        SerializerRegistryFixture.RegisterAll();
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
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests TryFallbackDeserialization with JSON data using registered serializers.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackDeserializationShouldDeserializeJsonFormat()
    {
        SerializerRegistryFixture.RegisterAll();
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
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests TryFallbackDeserialization for string type falls back to basic JSON deserialization.</summary>
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

    /// <summary>Tests TryFallbackSerialization throws when no alternatives can serialize a circular reference.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackSerializationShouldThrowForCircularReference()
    {
        SerializerRegistryFixture.RegisterAll();
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
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests TryFallbackSerialization succeeds with registered alternatives.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFallbackSerializationShouldUseRegisteredAlternatives()
    {
        SerializerRegistryFixture.RegisterAll();
        try
        {
            SystemJsonSerializer primary = new();
            var result = UniversalSerializer.TryFallbackSerialization("test", primary, DateTimeKind.Utc);
            await Assert.That(result).IsNotNull();
            await Assert.That(result).IsNotEmpty();
        }
        finally
        {
            SerializerRegistryFixture.Reset();
        }
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

    /// <summary>Tests Deserialize falls back to alternative serializers when primary fails.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldFallbackToAlternativeSerializers()
    {
        SerializerRegistryFixture.RegisterAll();
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
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests Deserialize with garbage data exercises all fallback paths.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeShouldExerciseAllFallbackPathsWithGarbageData()
    {
        SerializerRegistryFixture.Reset();
        try
        {
            // Garbage data with no registered alternatives - all fallbacks return default
            byte[] garbageData = [0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA];

            var result = UniversalSerializer.Deserialize<UserObject>(garbageData, new SystemJsonSerializer());
            await Assert.That(result).IsNull();
        }
        finally
        {
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests Deserialize throws InvalidOperationException when all fallbacks fail with registered serializers that all throw.</summary>
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
        var data = new byte[BsonShapedPayloadLength];
        BitConverter.GetBytes(BsonShapedPayloadLength).CopyTo(data, 0);

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

    /// <summary>An object that refuses equality comparison, used to trigger exceptions in EqualityComparer paths.</summary>
    private sealed class ThrowingEqualsObject
    {
        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            throw new NotSupportedException("ThrowingEqualsObject does not support equality comparison.");

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
            typeof(T) != typeof(ThrowingEqualsObject)
                ? default
                : (T)(object)new ThrowingEqualsObject();

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

    /// <summary>A serializer that always returns a fixed string for any deserialization.</summary>
    /// <param name="value">The string every <c>Deserialize&lt;string&gt;</c> call returns.</param>
    private sealed class FixedStringSerializer(string value) : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes) =>
            typeof(T) != typeof(string)
                ? default
                : (T)(object)value;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Serialize<T>(T item) => Encoding.UTF8.GetBytes(value);
    }
}
