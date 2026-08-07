// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for the <see cref="SerializerExtensions"/> extension methods that expose
/// the AOT-safe <see cref="JsonTypeInfo{T}"/> serialization path on arbitrary
/// <see cref="ISerializer"/> instances.
/// </summary>
[Category("Akavache")]
public class ISerializerDefaultMethodTests
{
    /// <summary>The string carried through each extension round trip.</summary>
    private const string RoundTripPayload = "hello";

    /// <summary>Tests that calling the <see cref="JsonTypeInfo{T}"/> <c>Deserialize</c> extension on a non-System.Text.Json-backed serializer throws <see cref="NotSupportedException"/>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithJsonTypeInfoShouldThrowNotSupportedException()
    {
        MinimalSerializer serializer = new();
        var jsonTypeInfo = (JsonTypeInfo<string>)JsonSerializerOptions.Default.GetTypeInfo(typeof(string));

        await Assert.That(() => serializer.Deserialize([], jsonTypeInfo))
            .Throws<NotSupportedException>();
    }

    /// <summary>Tests that calling the <see cref="JsonTypeInfo{T}"/> <c>Serialize</c> extension on a non-System.Text.Json-backed serializer throws <see cref="NotSupportedException"/>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithJsonTypeInfoShouldThrowNotSupportedException()
    {
        MinimalSerializer serializer = new();
        var jsonTypeInfo = (JsonTypeInfo<string>)JsonSerializerOptions.Default.GetTypeInfo(typeof(string));

        await Assert.That(() => serializer.Serialize("test", jsonTypeInfo))
            .Throws<NotSupportedException>();
    }

    /// <summary>
    /// Tests that the exception messages from the extension methods include the
    /// serializer type name so failures are easy to diagnose.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExtensionExceptionsShouldIncludeTypeName()
    {
        MinimalSerializer serializer = new();
        var jsonTypeInfo = (JsonTypeInfo<string>)JsonSerializerOptions.Default.GetTypeInfo(typeof(string));

        var deserializeFailure = CaptureUnsupported(() => _ = serializer.Deserialize([], jsonTypeInfo));
        var serializeFailure = CaptureUnsupported(() => _ = serializer.Serialize("test", jsonTypeInfo));

        await Assert.That(deserializeFailure).IsNotNull();
        await Assert.That(serializeFailure).IsNotNull();
        await Assert.That(deserializeFailure!.Message).Contains(nameof(MinimalSerializer));
        await Assert.That(serializeFailure!.Message).Contains(nameof(MinimalSerializer));
    }

    /// <summary>
    /// Tests that a <see cref="SystemJsonSerializer"/> instance routes through the
    /// concrete <see cref="JsonTypeInfo{T}"/> method via the extension, round-tripping
    /// a value without throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExtensionShouldRoundTripThroughSystemJsonSerializer()
    {
        SystemJsonSerializer serializer = new();
        var jsonTypeInfo = (JsonTypeInfo<string>)JsonSerializerOptions.Default.GetTypeInfo(typeof(string));

        var bytes = serializer.Serialize(RoundTripPayload, jsonTypeInfo);
        var value = serializer.Deserialize(bytes, jsonTypeInfo);

        await Assert.That(value).IsEqualTo(RoundTripPayload);
    }

    /// <summary>
    /// Tests that a <see cref="SystemJsonBsonSerializer"/> instance routes through
    /// the concrete <see cref="JsonTypeInfo{T}"/> method via the extension,
    /// round-tripping a value without throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExtensionShouldRoundTripThroughSystemJsonBsonSerializer()
    {
        SystemJsonBsonSerializer serializer = new();
        var jsonTypeInfo = (JsonTypeInfo<string>)JsonSerializerOptions.Default.GetTypeInfo(typeof(string));

        var bytes = serializer.Serialize(RoundTripPayload, jsonTypeInfo);
        var value = serializer.Deserialize(bytes, jsonTypeInfo);

        await Assert.That(value).IsEqualTo(RoundTripPayload);
    }

    /// <summary>Runs <paramref name="call"/> and returns the <see cref="NotSupportedException"/> it raised, or <see langword="null"/> when it completed.</summary>
    /// <param name="call">The call expected to reject the AOT metadata path.</param>
    /// <returns>The captured exception, if any.</returns>
    private static NotSupportedException? CaptureUnsupported(Action call)
    {
        try
        {
            call();
            return null;
        }
        catch (NotSupportedException ex)
        {
            return ex;
        }
    }

    /// <summary>A minimal <see cref="ISerializer"/> implementation used to drive the <see cref="NotSupportedException"/> fallback path of the extension.</summary>
    private sealed class MinimalSerializer : ISerializer
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        [RequiresUnreferencedCode("Test only.")]
        [RequiresDynamicCode("Test only.")]
        public T? Deserialize<T>(byte[] bytes) => default;

        /// <inheritdoc/>
        [RequiresUnreferencedCode("Test only.")]
        [RequiresDynamicCode("Test only.")]
        public byte[] Serialize<T>(T item) => [];
    }
}
