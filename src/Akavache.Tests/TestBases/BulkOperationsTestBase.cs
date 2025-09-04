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
/// A base class for tests about bulk operations.
/// </summary>
[NonParallelizable]
public abstract class BulkOperationsTestBase : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Tests if Get with multiple keys work correctly.
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
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            byte[] data = [0x10, 0x20, 0x30];
            string[] keys = ["Foo", "Bar", "Baz"];

            await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data).FirstAsync()));

            Assert.That(await fixture.GetAllKeys().ToList().FirstAsync(), Has.Count.EqualTo(keys.Length));

            var allData = await fixture.Get(keys).ToList().FirstAsync();

            Assert.That(allData, Has.Count.EqualTo(keys.Length));
            Assert.That(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]), Is.True);
        }
    }

    /// <summary>
    /// Tests to make sure that Get invalidates all the old keys.
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
            byte[] data = [0x10, 0x20, 0x30];
            string[] keys = ["Foo", "Bar", "Baz"];

            await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data, DateTimeOffset.MinValue).FirstAsync()));

            var allData = await fixture.Get(keys).ToList().FirstAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(allData, Is.Empty);
                Assert.That(await fixture.GetAllKeys().ToList().FirstAsync(), Is.Empty);
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
            byte[] data = [0x10, 0x20, 0x30];
            string[] keys = ["Foo", "Bar", "Baz"];

            await fixture.Insert(keys.ToDictionary(k => k, v => data)).FirstAsync();

            Assert.That(await fixture.GetAllKeys().ToList().FirstAsync(), Has.Count.EqualTo(keys.Length));

            var allData = await fixture.Get(keys).ToList().FirstAsync();

            Assert.That(allData, Has.Count.EqualTo(keys.Length));
            Assert.That(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]), Is.True);
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
            byte[] data = [0x10, 0x20, 0x30];
            string[] keys = ["Foo", "Bar", "Baz"];

            await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data).FirstAsync()));

            Assert.That(await fixture.GetAllKeys().ToList().FirstAsync(), Has.Count.EqualTo(keys.Length));

            await fixture.Invalidate(keys).FirstAsync();

            Assert.That(await fixture.GetAllKeys().ToList().FirstAsync(), Is.Empty);
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
