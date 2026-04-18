// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// System directives first per style rules
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;
using Newtonsoft.Json;

namespace Akavache.Integration.Tests;

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
        SystemJsonBsonSerializer serializer = new();
        UserObject user = new() { Name = "BsonUser", Bio = "Bio", Blog = "Blog" };
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
        NewtonsoftBsonSerializer serializer = new();
        UserObject user = new() { Name = "NewtonUser", Bio = "Bio", Blog = "Blog" };
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
    public Task BsonSerializer_ThrowsOnCircularReference()
    {
        try
        {
            NewtonsoftBsonSerializer serializer = new();
            List<object> list = [];
            list.Add(list); // circular reference
            Assert.Throws<JsonSerializationException>(() => serializer.Serialize(list));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Ensures invalid BSON data causes a controlled failure during deserialization.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task BsonSerializer_GracefullyHandlesInvalidDataDuringDeserialize()
    {
        SystemJsonBsonSerializer serializer = new();

        // random invalid BSON-like bytes
        byte[] invalid = [0x00, 0x01, 0x02, 0xFF, 0xFE];
        await Assert.That(serializer.Deserialize<UserObject>(invalid)).IsNull();
    }
}
