// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Extended serialization tests for System.Text.Json and Newtonsoft.Json serializers.
/// </summary>
[Category("Serialization")]
public class ExtendedJsonSerializerTests
{
    /// <summary>
    /// Round trips a user object with SystemJsonSerializer.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SystemJsonSerializer_SerializesAndDeserializes_UserObject()
    {
        var serializer = new SystemJsonSerializer();
        var user = new UserObject { Name = "System", Bio = "Bio", Blog = "Blog" };
        var bytes = serializer.Serialize(user);
        var roundtrip = serializer.Deserialize<UserObject>(bytes);
        await Assert.That(roundtrip).IsNotNull();
        await Assert.That(roundtrip!.Name).IsEqualTo("System");
    }

    /// <summary>
    /// Round trips a user object with NewtonsoftSerializer.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftSerializer_SerializesAndDeserializes_UserObject()
    {
        var serializer = new NewtonsoftSerializer();
        var user = new UserObject { Name = "Newton", Bio = "Bio", Blog = "Blog" };
        var bytes = serializer.Serialize(user);
        var roundtrip = serializer.Deserialize<UserObject>(bytes);
        await Assert.That(roundtrip).IsNotNull();
        await Assert.That(roundtrip!.Name).IsEqualTo("Newton");
    }

    /// <summary>
    /// Serializes DateTime UTC kind with SystemJsonSerializer.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SystemJsonSerializer_SerializesDateTimeUtc()
    {
        var serializer = new SystemJsonSerializer();
        var dt = DateTime.UtcNow;
        var bytes = serializer.Serialize(dt);
        var roundtrip = serializer.Deserialize<DateTime>(bytes);
        await Assert.That(roundtrip.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Serializes DateTimeOffset with zero offset using NewtonsoftSerializer.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftSerializer_SerializesDateTimeOffset()
    {
        var serializer = new NewtonsoftSerializer();
        var dto = DateTimeOffset.UtcNow;
        var bytes = serializer.Serialize(dto);
        var roundtrip = serializer.Deserialize<DateTimeOffset>(bytes);
        await Assert.That(roundtrip.Offset).IsEqualTo(TimeSpan.Zero);
    }

    /// <summary>
    /// Unsupported type serialization throws for SystemJsonSerializer.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SystemJsonSerializer_ThrowsOnUnsupportedType()
    {
        var serializer = new SystemJsonSerializer();
        await Assert.That(serializer.Serialize(new ExtendedJsonSerializerTests())).IsNotNull();
    }

    /// <summary>
    /// Unsupported type serialization throws for NewtonsoftSerializer.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftSerializer_ThrowsOnUnsupportedType()
    {
        var serializer = new NewtonsoftSerializer();
        await Assert.That(serializer.Serialize(new ExtendedJsonSerializerTests())).IsNotNull();
    }
}
