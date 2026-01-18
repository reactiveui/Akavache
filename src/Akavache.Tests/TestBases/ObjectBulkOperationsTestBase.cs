// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests.TestBases;

/// <summary>
/// Base class for tests associated with object based bulk operations.
/// </summary>
[NotInParallel]
public abstract class ObjectBulkOperationsTestBase : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Tests to make sure that Get works with multiple key types.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task GetShouldWorkWithMultipleKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        await Assert.That(serializer).IsNotNull();
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            var data = Tuple.Create("Foo", 4);
            string[] keys = ["Foo", "Bar", "Baz"];

            await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data).FirstAsync()));

            await Assert.That(await fixture.GetAllKeys().ToList().FirstAsync()).Count().IsEqualTo(keys.Length);

            var allData = await fixture.GetObjects<Tuple<string, int>>(keys).ToList().FirstAsync();

            await Assert.That(allData).Count().IsEqualTo(keys.Length);
            await Assert.That(allData.All(x => x.Value.Item1 == data.Item1 && x.Value.Item2 == data.Item2)).IsTrue();
        }
    }

    /// <summary>
    /// Tests to make sure that Get works with multiple key types.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task GetShouldInvalidateOldKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            var data = Tuple.Create("Foo", 4);
            string[] keys = ["Foo", "Bar", "Baz"];

            await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data, DateTimeOffset.MinValue).FirstAsync()));

            var allData = await fixture.GetObjects<Tuple<string, int>>(keys).ToList().FirstAsync();
            using (Assert.Multiple())
            {
                await Assert.That(allData).IsEmpty();
                await Assert.That(await fixture.GetAllKeys().ToList().FirstAsync()).IsEmpty();
            }
        }
    }

    /// <summary>
    /// Tests to make sure that insert works with multiple keys.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task InsertShouldWorkWithMultipleKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            var data = Tuple.Create("Foo", 4);
            string[] keys = ["Foo", "Bar", "Baz"];

            await fixture.InsertObjects(keys.ToDictionary(k => k, v => data)).FirstAsync();

            await Assert.That(await fixture.GetAllKeys().ToList().FirstAsync()).Count().IsEqualTo(keys.Length);

            var allData = await fixture.GetObjects<Tuple<string, int>>(keys).ToList().FirstAsync();

            await Assert.That(allData).Count().IsEqualTo(keys.Length);
            await Assert.That(allData.All(x => x.Value.Item1 == data.Item1 && x.Value.Item2 == data.Item2)).IsTrue();
        }
    }

    /// <summary>
    /// Invalidate should be able to trash multiple keys.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task InvalidateShouldTrashMultipleKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            var data = Tuple.Create("Foo", 4);
            string[] keys = ["Foo", "Bar", "Baz"];

            await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data).FirstAsync()));

            await Assert.That(await fixture.GetAllKeys().ToList().FirstAsync()).Count().IsEqualTo(keys.Length);

            await fixture.InvalidateObjects<Tuple<string, int>>(keys).FirstAsync();

            await Assert.That(await fixture.GetAllKeys().ToList().FirstAsync()).IsEmpty();
        }
    }

    /// <summary>
    /// Tests to make sure that InvalidateObjects works with the correct type parameter.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task InvalidateObjectsShouldOnlyInvalidateCorrectType(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            var tupleData = Tuple.Create("Foo", 4);
            var stringData = "TestString";
            string[] keys = ["Key1", "Key2", "Key3"];

            // Insert both tuples and strings with same keys
            await Task.WhenAll(keys.Select(async key => await fixture.InsertObject(key, tupleData).FirstAsync()));
            await Task.WhenAll(keys.Select(async key => await fixture.InsertObject($"str_{key}", stringData).FirstAsync()));

            await Assert.That(await fixture.GetAllKeys().ToList().FirstAsync()).Count().IsEqualTo(6);

            // Invalidate only the tuple objects
            await fixture.InvalidateObjects<Tuple<string, int>>(keys).FirstAsync();

            // Should still have the string objects
            var remainingKeys = await fixture.GetAllKeys().ToList().FirstAsync();
            await Assert.That(remainingKeys).Count().IsEqualTo(3);
            await Assert.That(remainingKeys.All(k => k.StartsWith("str_"))).IsTrue();
        }
    }

    /// <summary>
    /// Disposes the test base, restoring the original serializer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the <see cref="IBlobCache" /> we want to do the tests against.
    /// </summary>
    /// <param name="path">The path to the blob cache.</param>
    /// <param name="serializer">The serializer.</param>
    /// <returns>
    /// The blob cache for testing.
    /// </returns>
    protected abstract IBlobCache CreateBlobCache(string path, ISerializer serializer);

    /// <summary>
    /// Disposes resources.
    /// </summary>
    /// <param name="disposing">True to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Sets up the test with the specified serializer type.
    /// </summary>
    /// <param name="serializerType">The type of serializer to use for this test.</param>
    /// <returns>The configured serializer instance.</returns>
    private static ISerializer SetupTestSerializer(Type? serializerType)
    {
        // Clear any existing in-flight requests to ensure clean test state
        RequestCache.Clear();

        if (serializerType == typeof(NewtonsoftBsonSerializer))
        {
            // Register the Newtonsoft BSON serializer specifically
            return new NewtonsoftBsonSerializer();
        }
        else if (serializerType == typeof(SystemJsonBsonSerializer))
        {
            // Register the System.Text.Json BSON serializer specifically
            return new SystemJsonBsonSerializer();
        }
        else if (serializerType == typeof(NewtonsoftSerializer))
        {
            // Register the Newtonsoft JSON serializer
            return new NewtonsoftSerializer();
        }
        else if (serializerType == typeof(SystemJsonSerializer))
        {
            // Register the System.Text.Json serializer
            return new SystemJsonSerializer();
        }
        else
        {
            return null!;
        }
    }
}
