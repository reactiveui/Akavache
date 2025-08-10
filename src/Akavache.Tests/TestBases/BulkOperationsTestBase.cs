// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Tests.Helpers;
using Xunit;

namespace Akavache.Tests.TestBases;

/// <summary>
/// A base class for tests about bulk operations.
/// </summary>
[Collection("Bulk Operations Tests")]
public abstract class BulkOperationsTestBase : IDisposable
{
    private readonly ISerializer? _originalSerializer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsTestBase"/> class.
    /// </summary>
    protected BulkOperationsTestBase()
    {
        // Store the original serializer to restore it after each test
        _originalSerializer = CacheDatabase.Serializer;
    }

    /// <summary>
    /// Tests if Get with multiple keys work correctly.
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
            var data = new byte[] { 0x10, 0x20, 0x30, };
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data).FirstAsync()));

            Assert.Equal(keys.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

            var allData = await fixture.Get(keys).ToList().FirstAsync();

            Assert.Equal(keys.Length, allData.Count);
            Assert.True(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]));
        }
    }

    /// <summary>
    /// Tests to make sure that Get invalidates all the old keys.
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
            var data = new byte[] { 0x10, 0x20, 0x30, };
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data, DateTimeOffset.MinValue).FirstAsync()));

            var allData = await fixture.Get(keys).ToList().FirstAsync();
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
            var data = new byte[] { 0x10, 0x20, 0x30, };
            var keys = new[] { "Foo", "Bar", "Baz", };

            await fixture.Insert(keys.ToDictionary(k => k, v => data)).FirstAsync();

            Assert.Equal(keys.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

            var allData = await fixture.Get(keys).ToList().FirstAsync();

            Assert.Equal(keys.Length, allData.Count);
            Assert.True(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]));
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
            var data = new byte[] { 0x10, 0x20, 0x30, };
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data).FirstAsync()));

            Assert.Equal(keys.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

            await fixture.Invalidate(keys).FirstAsync();

            Assert.Equal(0, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
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
