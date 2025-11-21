// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System directives first per style rules
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Skeleton tests for BSON serializers edge cases.
/// </summary>
[TestFixture]
[Category("Serialization")]
public class BsonSerializerEdgeCaseTests
{
    /// <summary>
    /// Verifies the SystemTextJson BSON serializer round trips a simple object.
    /// </summary>
    [Test]
    public void SystemTextJsonBsonSerializer_SerializesAndDeserializesSimpleObject()
    {
        var serializer = new SystemJsonBsonSerializer();
        var user = new UserObject { Name = "BsonUser", Bio = "Bio", Blog = "Blog" };
        var data = serializer.Serialize(user);
        var roundtrip = serializer.Deserialize<UserObject>(data);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("BsonUser"));
    }

    /// <summary>
    /// Verifies the Newtonsoft BSON serializer round trips a simple object.
    /// </summary>
    [Test]
    public void NewtonsoftBsonSerializer_SerializesAndDeserializesSimpleObject()
    {
        var serializer = new NewtonsoftBsonSerializer();
        var user = new UserObject { Name = "NewtonUser", Bio = "Bio", Blog = "Blog" };
        var data = serializer.Serialize(user);
        var roundtrip = serializer.Deserialize<UserObject>(data);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Name, Is.EqualTo("NewtonUser"));
    }

    /// <summary>
    /// Ensures circular references throw when attempting serialization.
    /// </summary>
    [Test]
    public void BsonSerializer_ThrowsOnCircularReference()
    {
        var serializer = new NewtonsoftBsonSerializer();
        var list = new List<object>();
        list.Add(list); // circular reference
        Assert.Throws<JsonSerializationException>(() => serializer.Serialize(list));
    }

    /// <summary>
    /// Ensures invalid BSON data causes a controlled failure during deserialization.
    /// </summary>
    [Test]
    public void BsonSerializer_GracefullyHandlesInvalidDataDuringDeserialize()
    {
        var serializer = new SystemJsonBsonSerializer();

        // random invalid BSON-like bytes
        var invalid = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE };
        Assert.That(serializer.Deserialize<UserObject>(invalid), Is.Null);
    }
}
