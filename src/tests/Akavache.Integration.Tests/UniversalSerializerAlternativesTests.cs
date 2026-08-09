// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for how <see cref="UniversalSerializer"/> discovers the serializers registered as
/// alternatives to the primary one, classifies them as BSON or plain JSON, and walks that list
/// when the primary serializer cannot read a payload.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class UniversalSerializerAlternativesTests
{
    /// <summary>The number of registered serializers left once the primary is excluded.</summary>
    private const int ExpectedAlternativeCount = 3;

    /// <summary>The string payload round-tripped through the alternative serializers.</summary>
    private const string GreetingPayload = "hello";

    /// <summary>Tests RegisterSerializer throws on null factory.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RegisterSerializerShouldThrowOnNullFactory() =>
        await Assert.That(static () => UniversalSerializer.RegisterSerializer(null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests ResetCaches clears registered serializers.</summary>
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
    /// Verifies that <see cref="UniversalSerializer.ResetCaches"/> clears both the BSON and the
    /// plain-Newtonsoft caches — a subsequent probe re-runs the classification logic.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResetCachesShouldClearSerializerKindCaches()
    {
        _ = UniversalSerializer.IsBsonSerializer(new SystemJsonBsonSerializer());
        _ = UniversalSerializer.IsPlainNewtonsoftSerializer(new NewtonsoftSerializer());

        UniversalSerializer.ResetCaches();

        // After reset the next probe should still produce a correct answer — this validates the
        // classifier doesn't hand back a stale or corrupted result after the caches were cleared.
        await Assert.That(UniversalSerializer.IsBsonSerializer(new SystemJsonBsonSerializer())).IsTrue();
        await Assert.That(UniversalSerializer.IsPlainNewtonsoftSerializer(new NewtonsoftSerializer())).IsTrue();
    }

    /// <summary>Tests GetAvailableAlternativeSerializers excludes the primary serializer when registered.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAvailableAlternativeSerializersShouldExcludePrimary()
    {
        SerializerRegistryFixture.RegisterAll();
        try
        {
            SystemJsonSerializer primary = new();
            var alternatives = UniversalSerializer.GetAvailableAlternativeSerializers(primary);

            var hasSystemJson = alternatives.Exists(static s => s.GetType() == typeof(SystemJsonSerializer));
            await Assert.That(hasSystemJson).IsFalse();
        }
        finally
        {
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests GetAvailableAlternativeSerializers returns registered alternatives.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAvailableAlternativeSerializersShouldReturnRegisteredAlternatives()
    {
        SerializerRegistryFixture.RegisterAll();
        try
        {
            SystemJsonSerializer primary = new();
            var alternatives = UniversalSerializer.GetAvailableAlternativeSerializers(primary);

            // 4 registered minus the SystemJsonSerializer primary = 3
            await Assert.That(alternatives.Count).IsEqualTo(ExpectedAlternativeCount);
        }
        finally
        {
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests GetAvailableAlternativeSerializers swallows factory exceptions.</summary>
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

    /// <summary>Tests TryAlternativeSerializers returns default with no registered alternatives.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldReturnDefaultWhenNoAlternatives()
    {
        SerializerRegistryFixture.Reset();
        try
        {
            SystemJsonSerializer primary = new();
            byte[] data = [0xFF, 0xFE];

            var result = UniversalSerializer.TryAlternativeSerializers<string>(data, primary, null);
            await Assert.That(result).IsNull();
        }
        finally
        {
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests TryAlternativeSerializers can deserialize with registered alternatives and forced DateTime kind.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldUseRegisteredAlternativesWithDateTimeKind()
    {
        SerializerRegistryFixture.RegisterAll();
        try
        {
            // Use Newtonsoft to serialize, then ask UniversalSerializer to find alternatives
            NewtonsoftSerializer newtonsoftSerializer = new();
            UserObject testObj = new() { Name = "Alt", Bio = "Bio", Blog = "Blog" };
            var data = newtonsoftSerializer.Serialize(testObj);

            // Exclude Newtonsoft as primary, expect a registered alternative to handle it
            var result =
                UniversalSerializer.TryAlternativeSerializers<UserObject>(
                    data,
                    newtonsoftSerializer,
                    DateTimeKind.Utc);

            // The path is exercised; the result depends on whether SystemJsonSerializer can read Newtonsoft JSON
            // (it can for objects without DateTime fields)
            await Assert.That(result).IsNotNull();
        }
        finally
        {
            SerializerRegistryFixture.Reset();
        }
    }

    /// <summary>Tests TryAlternativeSerializers handles DateTimeOffset MinValue/MaxValue paths.</summary>
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
        var data = "\0\0"u8.ToArray();

        var result = UniversalSerializer.TryAlternativeSerializers<DateTime>(data, primary, null);
        await Assert.That(result).IsEqualTo(DateTime.MinValue);
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

    /// <summary>Tests TryAlternativeSerializers catch block when alt serializer throws.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAlternativeSerializersShouldContinueWhenAltSerializerThrows()
    {
        UniversalSerializer.RegisterSerializer(static () => new ThrowingSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());

        ThrowingSerializer primary = new();
        var data = new SystemJsonSerializer().Serialize(GreetingPayload);

        var result = UniversalSerializer.TryAlternativeSerializers<string>(data, primary, null);
        await Assert.That(result).IsEqualTo(GreetingPayload);
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
        var data = new SystemJsonSerializer().Serialize(GreetingPayload);

        var result = UniversalSerializer.TryAlternativeSerializers<string>(data, primary, null);
        await Assert.That(result).IsEqualTo(GreetingPayload);
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

    /// <summary>A fake serializer that returns DateTime.MinValue for all deserializations.</summary>
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
}
