// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using NUnit.Framework;

namespace Akavache.Tests.TestBases;

/// <summary>
/// Base class for tests associated with object based bulk operations.
/// </summary>
[NonParallelizable]
public abstract class ObjectBulkOperationsTestBase : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Sets up the test with the specified serializer type.
    /// </summary>
    /// <param name="serializerType">The type of serializer to use for this test.</param>
    /// <returns>The configured serializer instance.</returns>
    public static ISerializer SetupTestSerializer(Type? serializerType)
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

    /// <summary>
    /// Tests to make sure that Get works with multiple key types.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task GetShouldWorkWithMultipleKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        Assert.That(serializer, Is.Not.Null);
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            var data = Tuple.Create("Foo", 4);
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data).FirstAsync()));

            Assert.That((await fixture.GetAllKeys().ToList().FirstAsync()).Count, Is.EqualTo(keys.Length));

            var allData = await fixture.GetObjects<Tuple<string, int>>(keys).ToList().FirstAsync();

            Assert.That(allData.Count, Is.EqualTo(keys.Length));
            Assert.That(allData.All(x => x.Value.Item1 == data.Item1 && x.Value.Item2 == data.Item2, Is.True));
        }
    }

    /// <summary>
    /// Tests to make sure that Get works with multiple key types.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task GetShouldInvalidateOldKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            var data = Tuple.Create("Foo", 4);
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data, DateTimeOffset.MinValue).FirstAsync()));

            var allData = await fixture.GetObjects<Tuple<string, int>>(keys).ToList().FirstAsync();
            Assert.That(allData.Count, Is.EqualTo(0));
            Assert.That((await fixture.GetAllKeys().ToList().FirstAsync()).Count, Is.EqualTo(0));
        }
    }

    /// <summary>
    /// Tests to make sure that insert works with multiple keys.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task InsertShouldWorkWithMultipleKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            var data = Tuple.Create("Foo", 4);
            var keys = new[] { "Foo", "Bar", "Baz", };

            await fixture.InsertObjects(keys.ToDictionary(k => k, v => data)).FirstAsync();

            Assert.That((await fixture.GetAllKeys().ToList().FirstAsync()).Count, Is.EqualTo(keys.Length));

            var allData = await fixture.GetObjects<Tuple<string, int>>(keys).ToList().FirstAsync();

            Assert.That(allData.Count, Is.EqualTo(keys.Length));
            Assert.That(allData.All(x => x.Value.Item1 == data.Item1 && x.Value.Item2 == data.Item2, Is.True));
        }
    }

    /// <summary>
    /// Invalidate should be able to trash multiple keys.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task InvalidateShouldTrashMultipleKeys(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            var data = Tuple.Create("Foo", 4);
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data).FirstAsync()));

            Assert.That((await fixture.GetAllKeys().ToList().FirstAsync()).Count, Is.EqualTo(keys.Length));

            await fixture.InvalidateObjects<Tuple<string, int>>(keys).FirstAsync();

            Assert.That((await fixture.GetAllKeys().ToList().FirstAsync()).Count, Is.EqualTo(0));
        }
    }

    /// <summary>
    /// Tests to make sure that InvalidateObjects works with the correct type parameter.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task InvalidateObjectsShouldOnlyInvalidateCorrectType(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            var tupleData = Tuple.Create("Foo", 4);
            var stringData = "TestString";
            var keys = new[] { "Key1", "Key2", "Key3" };

            // Insert both tuples and strings with same keys
            await Task.WhenAll(keys.Select(async key => await fixture.InsertObject(key, tupleData).FirstAsync()));
            await Task.WhenAll(keys.Select(async key => await fixture.InsertObject($"str_{key}", stringData).FirstAsync()));

            Assert.That((await fixture.GetAllKeys().ToList().FirstAsync()).Count, Is.EqualTo(6));

            // Invalidate only the tuple objects
            await fixture.InvalidateObjects<Tuple<string, int>>(keys).FirstAsync();

            // Should still have the string objects
            var remainingKeys = await fixture.GetAllKeys().ToList().FirstAsync();
            Assert.That(remainingKeys.Count, Is.EqualTo(3));
            Assert.That(remainingKeys.All(k => k.StartsWith("str_", Is.True)));
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
}
