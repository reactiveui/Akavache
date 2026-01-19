// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System directives first per style rules
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;
using Newtonsoft.Json;

namespace Akavache.Tests;

/// <summary>
/// Skeleton tests for BSON serializers edge cases.
/// </summary>
[Category("Serialization")]
public class BsonSerializerEdgeCaseTests
{
    /// <summary>
    /// Verifies the SystemTextJson BSON serializer round trips a simple object.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonBsonSerializer_SerializesAndDeserializesSimpleObject()
    {
        var serializer = new SystemJsonBsonSerializer();
        var user = new UserObject { Name = "BsonUser", Bio = "Bio", Blog = "Blog" };
        var data = serializer.Serialize(user);
        var roundtrip = serializer.Deserialize<UserObject>(data);
        await Assert.That(roundtrip).IsNotNull();
        await Assert.That(roundtrip!.Name).IsEqualTo("BsonUser");
    }

    /// <summary>
    /// Verifies the Newtonsoft BSON serializer round trips a simple object.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftBsonSerializer_SerializesAndDeserializesSimpleObject()
    {
        var serializer = new NewtonsoftBsonSerializer();
        var user = new UserObject { Name = "NewtonUser", Bio = "Bio", Blog = "Blog" };
        var data = serializer.Serialize(user);
        var roundtrip = serializer.Deserialize<UserObject>(data);
        await Assert.That(roundtrip).IsNotNull();
        await Assert.That(roundtrip!.Name).IsEqualTo("NewtonUser");
    }

    /// <summary>
    /// Ensures circular references throw when attempting serialization.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task BsonSerializer_ThrowsOnCircularReference()
    {
        var serializer = new NewtonsoftBsonSerializer();
        var list = new List<object>();
        list.Add(list); // circular reference
        Assert.Throws<JsonSerializationException>(() => serializer.Serialize(list));
    }

    /// <summary>
    /// Ensures invalid BSON data causes a controlled failure during deserialization.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task BsonSerializer_GracefullyHandlesInvalidDataDuringDeserialize()
    {
        var serializer = new SystemJsonBsonSerializer();

        // random invalid BSON-like bytes
        var invalid = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE };
        await Assert.That(serializer.Deserialize<UserObject>(invalid)).IsNull();
    }
}
