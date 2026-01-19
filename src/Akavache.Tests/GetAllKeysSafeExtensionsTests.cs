// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for the GetAllKeysSafe methods that provide safe alternatives to GetAllKeys()
/// to prevent crashes on mobile platforms.
/// </summary>
[Category("Akavache")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Cleanup is handled via test hooks")]
public class GetAllKeysSafeExtensionsTests
{
    private InMemoryBlobCache _cache = null!;

    /// <summary>
    /// Sets up the test cache before each test.
    /// </summary>
    [Before(Test)]
    public void SetUp()
    {
        _cache = new InMemoryBlobCache(new SystemJsonSerializer());
    }

    /// <summary>
    /// Cleans up the test cache after each test.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    [After(Test)]
    public async Task TearDown()
    {
        await _cache.DisposeAsync();
    }

    /// <summary>
    /// Tests that GetAllKeysSafe returns an empty list for an empty cache.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetAllKeysSafe_ShouldReturnEmptyForEmptyCache()
    {
        // Act
        var keys = await _cache.GetAllKeysSafe().ToList().FirstAsync();

        // Assert
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests that GetAllKeysSafe returns all keys when cache is populated.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetAllKeysSafe_ShouldReturnKeysForPopulatedCache()
    {
        // Arrange
        await _cache.Insert("key1", new byte[] { 1, 2, 3 }).FirstAsync();
        await _cache.Insert("key2", new byte[] { 4, 5, 6 }).FirstAsync();

        // Act
        var keys = await _cache.GetAllKeysSafe().ToList().FirstAsync();

        // Assert
        await Assert.That(keys).Count().IsEqualTo(2);
        await Assert.That(keys).Contains("key1");
        await Assert.That(keys).Contains("key2");
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with type returns an empty list for an empty cache.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetAllKeysSafe_WithType_ShouldReturnEmptyForEmptyCache()
    {
        // Act
        var keys = await _cache.GetAllKeysSafe(typeof(string)).ToList().FirstAsync();

        // Assert
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with type returns keys filtered by the specified type.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetAllKeysSafe_WithType_ShouldReturnKeysForSpecificType()
    {
        // Arrange
        await _cache.InsertObject("test_string", "value").FirstAsync();
        await _cache.InsertObject("test_int", 42).FirstAsync();

        // Act
        var stringKeys = await _cache.GetAllKeysSafe(typeof(string)).ToList().FirstAsync();
        var intKeys = await _cache.GetAllKeysSafe(typeof(int)).ToList().FirstAsync();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(stringKeys).Count().IsEqualTo(1);
            await Assert.That(stringKeys.First()).Contains("test_string");
            await Assert.That(intKeys).Count().IsEqualTo(1);
            await Assert.That(intKeys.First()).Contains("test_int");
        }
    }

    /// <summary>
    /// Tests that generic GetAllKeysSafe returns an empty list for an empty cache.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetAllKeysSafe_Generic_ShouldReturnEmptyForEmptyCache()
    {
        // Act
        var keys = await _cache.GetAllKeysSafe<string>().ToList().FirstAsync();

        // Assert
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests that generic GetAllKeysSafe returns keys filtered by the specified generic type.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetAllKeysSafe_Generic_ShouldReturnKeysForSpecificType()
    {
        // Arrange
        await _cache.InsertObject("test_string", "value").FirstAsync();
        await _cache.InsertObject("test_int", 42).FirstAsync();

        // Act
        var stringKeys = await _cache.GetAllKeysSafe<string>().ToList().FirstAsync();

        // Assert
        await Assert.That(stringKeys).Count().IsEqualTo(1);
        await Assert.That(stringKeys.First()).Contains("test_string");
    }

    /// <summary>
    /// Tests that GetAllKeysSafe throws ArgumentNullException for null cache.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetAllKeysSafe_ShouldThrowForNullCache()
    {
        // Arrange
        IBlobCache? nullCache = null;

        // Act & Assert
        await Assert.That(() => nullCache!.GetAllKeysSafe()).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with type throws ArgumentNullException for null cache.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetAllKeysSafe_WithType_ShouldThrowForNullCache()
    {
        // Arrange
        IBlobCache? nullCache = null;

        // Act & Assert
        await Assert.That(() => nullCache!.GetAllKeysSafe(typeof(string))).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with type throws ArgumentNullException for null type.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetAllKeysSafe_WithType_ShouldThrowForNullType()
    {
        // Act & Assert
        await Assert.That(() => _cache.GetAllKeysSafe(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that generic GetAllKeysSafe throws ArgumentNullException for null cache.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetAllKeysSafe_Generic_ShouldThrowForNullCache()
    {
        // Arrange
        IBlobCache? nullCache = null;

        // Act & Assert
        await Assert.That(() => nullCache!.GetAllKeysSafe<string>()).Throws<ArgumentNullException>();
    }
}
