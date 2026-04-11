// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="SerializerExtensions"/> extension methods that expose
/// the AOT-safe <see cref="JsonTypeInfo{T}"/> serialization path on arbitrary
/// <see cref="ISerializer"/> instances.
/// </summary>
[Category("Akavache")]
public class ISerializerDefaultMethodTests
{
    /// <summary>
    /// Tests that calling the <see cref="JsonTypeInfo{T}"/> <c>Deserialize</c>
    /// extension on a non-System.Text.Json-backed serializer throws
    /// <see cref="NotSupportedException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithJsonTypeInfoShouldThrowNotSupportedException()
    {
        ISerializer serializer = new MinimalSerializer();
        var jsonTypeInfo = (JsonTypeInfo<string>)JsonSerializerOptions.Default.GetTypeInfo(typeof(string));

        await Assert.That(() => serializer.Deserialize([], jsonTypeInfo))
            .Throws<NotSupportedException>();
    }

    /// <summary>
    /// Tests that calling the <see cref="JsonTypeInfo{T}"/> <c>Serialize</c>
    /// extension on a non-System.Text.Json-backed serializer throws
    /// <see cref="NotSupportedException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithJsonTypeInfoShouldThrowNotSupportedException()
    {
        ISerializer serializer = new MinimalSerializer();
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
        ISerializer serializer = new MinimalSerializer();
        var jsonTypeInfo = (JsonTypeInfo<string>)JsonSerializerOptions.Default.GetTypeInfo(typeof(string));

        try
        {
            serializer.Deserialize([], jsonTypeInfo);
        }
        catch (NotSupportedException ex)
        {
            await Assert.That(ex.Message).Contains(nameof(MinimalSerializer));
        }

        try
        {
            serializer.Serialize("test", jsonTypeInfo);
        }
        catch (NotSupportedException ex)
        {
            await Assert.That(ex.Message).Contains(nameof(MinimalSerializer));
        }
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
        ISerializer serializer = new SystemJsonSerializer();
        var jsonTypeInfo = (JsonTypeInfo<string>)JsonSerializerOptions.Default.GetTypeInfo(typeof(string));

        var bytes = serializer.Serialize("hello", jsonTypeInfo);
        var value = serializer.Deserialize(bytes, jsonTypeInfo);

        await Assert.That(value).IsEqualTo("hello");
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
        ISerializer serializer = new SystemJsonBsonSerializer();
        var jsonTypeInfo = (JsonTypeInfo<string>)JsonSerializerOptions.Default.GetTypeInfo(typeof(string));

        var bytes = serializer.Serialize("hello", jsonTypeInfo);
        var value = serializer.Deserialize(bytes, jsonTypeInfo);

        await Assert.That(value).IsEqualTo("hello");
    }

    /// <summary>
    /// A minimal <see cref="ISerializer"/> implementation used to drive the
    /// <see cref="NotSupportedException"/> fallback path of the extension.
    /// </summary>
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
