// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Tests.Helpers;
using Xunit;

namespace Akavache.Tests.TestBases;

/// <summary>
/// Base class for tests associated with object based bulk operations.
/// </summary>
[Collection("Object Bulk Operations Tests")]
public abstract class ObjectBulkOperationsTestBase : IDisposable
{
    private readonly ISerializer? _originalSerializer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectBulkOperationsTestBase"/> class.
    /// </summary>
    protected ObjectBulkOperationsTestBase()
    {
        // Store the original serializer to restore it after each test
        _originalSerializer = CacheDatabase.Serializer;
    }

    /// <summary>
    /// Tests to make sure that Get works with multiple key types.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GetShouldWorkWithMultipleKeys()
    {
        // Ensure the test uses the correct serializer
        EnsureTestSerializerSetup();

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var data = Tuple.Create("Foo", 4);
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data).FirstAsync()));

            Assert.Equal(keys.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

            var allData = await fixture.GetObjects<Tuple<string, int>>(keys).ToList().FirstAsync();

            Assert.Equal(keys.Length, allData.Count);
            Assert.True(allData.All(x => x.Value.Item1 == data.Item1 && x.Value.Item2 == data.Item2));
        }
    }

    /// <summary>
    /// Tests to make sure that Get works with multiple key types.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GetShouldInvalidateOldKeys()
    {
        // Ensure the test uses the correct serializer
        EnsureTestSerializerSetup();

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var data = Tuple.Create("Foo", 4);
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data, DateTimeOffset.MinValue).FirstAsync()));

            var allData = await fixture.GetObjects<Tuple<string, int>>(keys).ToList().FirstAsync();
            Assert.Equal(0, allData.Count);
            Assert.Equal(0, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
        }
    }

    /// <summary>
    /// Tests to make sure that insert works with multiple keys.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task InsertShouldWorkWithMultipleKeys()
    {
        // Ensure the test uses the correct serializer
        EnsureTestSerializerSetup();

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var data = Tuple.Create("Foo", 4);
            var keys = new[] { "Foo", "Bar", "Baz", };

            await fixture.InsertObjects(keys.ToDictionary(k => k, v => data)).FirstAsync();

            Assert.Equal(keys.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

            var allData = await fixture.GetObjects<Tuple<string, int>>(keys).ToList().FirstAsync();

            Assert.Equal(keys.Length, allData.Count);
            Assert.True(allData.All(x => x.Value.Item1 == data.Item1 && x.Value.Item2 == data.Item2));
        }
    }

    /// <summary>
    /// Invalidate should be able to trash multiple keys.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task InvalidateShouldTrashMultipleKeys()
    {
        // Ensure the test uses the correct serializer
        EnsureTestSerializerSetup();

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var data = Tuple.Create("Foo", 4);
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.InsertObject(v, data).FirstAsync()));

            Assert.Equal(keys.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

            await fixture.InvalidateObjects<Tuple<string, int>>(keys).FirstAsync();

            Assert.Equal(0, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
        }
    }

    /// <summary>
    /// Tests to make sure that InvalidateObjects works with the correct type parameter.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task InvalidateObjectsShouldOnlyInvalidateCorrectType()
    {
        // Ensure the test uses the correct serializer
        EnsureTestSerializerSetup();

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var tupleData = Tuple.Create("Foo", 4);
            var stringData = "TestString";
            var keys = new[] { "Key1", "Key2", "Key3" };

            // Insert both tuples and strings with same keys
            await Task.WhenAll(keys.Select(async key => await fixture.InsertObject(key, tupleData).FirstAsync()));
            await Task.WhenAll(keys.Select(async key => await fixture.InsertObject($"str_{key}", stringData).FirstAsync()));

            Assert.Equal(6, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

            // Invalidate only the tuple objects
            await fixture.InvalidateObjects<Tuple<string, int>>(keys).FirstAsync();

            // Should still have the string objects
            var remainingKeys = await fixture.GetAllKeys().ToList().FirstAsync();
            Assert.Equal(3, remainingKeys.Count);
            Assert.True(remainingKeys.All(k => k.StartsWith("str_")));
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
    /// Gets the <see cref="IBlobCache"/> we want to do the tests against.
    /// </summary>
    /// <param name="path">The path to the blob cache.</param>
    /// <returns>The blob cache for testing.</returns>
    protected abstract IBlobCache CreateBlobCache(string path);

    /// <summary>
    /// Sets up the test class serializer. This should be overridden by derived classes.
    /// </summary>
    protected virtual void SetupTestClassSerializer()
    {
        // Default implementation - derived classes should override this
    }

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
                // Restore the original serializer to prevent interference with other tests
                if (_originalSerializer != null)
                {
                    CacheDatabase.Serializer = _originalSerializer;
                }
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Ensures that the test serializer is properly set up before each test method.
    /// </summary>
    private void EnsureTestSerializerSetup()
    {
        // Call the setup method to ensure the correct serializer is in place
        // This handles cases where the global serializer might have been changed by other tests
        SetupTestClassSerializer();
    }
}
