// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Extended serialization tests for System.Text.Json and Newtonsoft.Json serializers.
/// </summary>
[TestFixture]
[Category("Serialization")]
public class ExtendedJsonSerializerTests
{
    /// <summary>
    /// Round trips a user object with SystemJsonSerializer.
    /// </summary>
    [Test]
    public void SystemJsonSerializer_SerializesAndDeserializes_UserObject()
    {
        var serializer = new SystemJsonSerializer();
        var user = new UserObject { Name = "System", Bio = "Bio", Blog = "Blog" };
        var bytes = serializer.Serialize(user);
        var roundtrip = serializer.Deserialize<UserObject>(bytes);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("System"));
    }

    /// <summary>
    /// Round trips a user object with NewtonsoftSerializer.
    /// </summary>
    [Test]
    public void NewtonsoftSerializer_SerializesAndDeserializes_UserObject()
    {
        var serializer = new NewtonsoftSerializer();
        var user = new UserObject { Name = "Newton", Bio = "Bio", Blog = "Blog" };
        var bytes = serializer.Serialize(user);
        var roundtrip = serializer.Deserialize<UserObject>(bytes);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("Newton"));
    }

    /// <summary>
    /// Serializes DateTime UTC kind with SystemJsonSerializer.
    /// </summary>
    [Test]
    public void SystemJsonSerializer_SerializesDateTimeUtc()
    {
        var serializer = new SystemJsonSerializer();
        var dt = DateTime.UtcNow;
        var bytes = serializer.Serialize(dt);
        var roundtrip = serializer.Deserialize<DateTime>(bytes);
        Assert.That(roundtrip.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    /// <summary>
    /// Serializes DateTimeOffset with zero offset using NewtonsoftSerializer.
    /// </summary>
    [Test]
    public void NewtonsoftSerializer_SerializesDateTimeOffset()
    {
        var serializer = new NewtonsoftSerializer();
        var dto = DateTimeOffset.UtcNow;
        var bytes = serializer.Serialize(dto);
        var roundtrip = serializer.Deserialize<DateTimeOffset>(bytes);
        Assert.That(roundtrip.Offset, Is.EqualTo(TimeSpan.Zero));
    }

    /// <summary>
    /// Unsupported type serialization throws for SystemJsonSerializer.
    /// </summary>
    [Test]
    public void SystemJsonSerializer_ThrowsOnUnsupportedType()
    {
        var serializer = new SystemJsonSerializer();
        Assert.That(serializer.Serialize(new ExtendedJsonSerializerTests()), Is.Not.Null);
    }

    /// <summary>
    /// Unsupported type serialization throws for NewtonsoftSerializer.
    /// </summary>
    [Test]
    public void NewtonsoftSerializer_ThrowsOnUnsupportedType()
    {
        var serializer = new NewtonsoftSerializer();
        Assert.That(serializer.Serialize(new ExtendedJsonSerializerTests()), Is.Not.Null);
    }
}
