// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for the GetAllKeysSafe methods that provide safe alternatives to GetAllKeys()
/// to prevent crashes on mobile platforms.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class GetAllKeysSafeExtensionsTests
{
    private InMemoryBlobCache _cache = null!;

    /// <summary>
    /// Sets up the test cache before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _cache = new InMemoryBlobCache(new SystemJsonSerializer());
    }

    /// <summary>
    /// Cleans up the test cache after each test.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    [TearDown]
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
        Assert.That(keys, Is.Empty);
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
        Assert.That(keys, Has.Count.EqualTo(2));
        Assert.That(keys, Does.Contain("key1"));
        Assert.That(keys, Does.Contain("key2"));
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
        Assert.That(keys, Is.Empty);
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stringKeys, Has.Count.EqualTo(1));
            Assert.That(stringKeys.First(), Does.Contain("test_string"));
            Assert.That(intKeys, Has.Count.EqualTo(1));
            Assert.That(intKeys.First(), Does.Contain("test_int"));
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
        Assert.That(keys, Is.Empty);
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
        Assert.That(stringKeys, Has.Count.EqualTo(1));
        Assert.That(stringKeys.First(), Does.Contain("test_string"));
    }

    /// <summary>
    /// Tests that GetAllKeysSafe throws ArgumentNullException for null cache.
    /// </summary>
    [Test]
    public void GetAllKeysSafe_ShouldThrowForNullCache()
    {
        // Arrange
        IBlobCache? nullCache = null;

        // Act & Assert
        Assert.That(() => nullCache!.GetAllKeysSafe(), Throws.ArgumentNullException);
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with type throws ArgumentNullException for null cache.
    /// </summary>
    [Test]
    public void GetAllKeysSafe_WithType_ShouldThrowForNullCache()
    {
        // Arrange
        IBlobCache? nullCache = null;

        // Act & Assert
        Assert.That(() => nullCache!.GetAllKeysSafe(typeof(string)), Throws.ArgumentNullException);
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with type throws ArgumentNullException for null type.
    /// </summary>
    [Test]
    public void GetAllKeysSafe_WithType_ShouldThrowForNullType()
    {
        // Act & Assert
        Assert.That(() => _cache.GetAllKeysSafe(null!), Throws.ArgumentNullException);
    }

    /// <summary>
    /// Tests that generic GetAllKeysSafe throws ArgumentNullException for null cache.
    /// </summary>
    [Test]
    public void GetAllKeysSafe_Generic_ShouldThrowForNullCache()
    {
        // Arrange
        IBlobCache? nullCache = null;

        // Act & Assert
        Assert.That(() => nullCache!.GetAllKeysSafe<string>(), Throws.ArgumentNullException);
    }
}
