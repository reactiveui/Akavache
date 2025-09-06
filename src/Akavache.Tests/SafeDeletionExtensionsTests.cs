// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for the new safe deletion methods added to address cache deletion crashes on mobile platforms.
/// These tests focus on the new Remove() and TryRemove() extension methods.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class SafeDeletionExtensionsTests
{
    private InMemoryBlobCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new InMemoryBlobCache(new SystemJsonSerializer());
    }

    [TearDown]
    public async Task TearDown()
    {
        await _cache.DisposeAsync();
    }

    [Test]
    public async Task Remove_ShouldRemoveExistingKey()
    {
        // Arrange
        const string key = "test_key";
        var data = new byte[] { 1, 2, 3, 4 };
        await _cache.Insert(key, data).FirstAsync();

        // Act
        await _cache.Remove(key).FirstAsync();

        // Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _cache.Get(key).FirstAsync());
    }

    [Test]
    public async Task Remove_ShouldNotThrowForNonExistentKey()
    {
        // Arrange
        const string key = "non_existent_key";

        // Act & Assert - should not throw
        await _cache.Remove(key).FirstAsync();
    }

    [Test]
    public async Task RemoveTyped_ShouldRemoveExistingTypedObject()
    {
        // Arrange
        const string key = "test_object";
        var testObject = new TestClass { Name = "Test", Value = 42 };
        await _cache.InsertObject(key, testObject).FirstAsync();

        // Act
        await _cache.Remove<TestClass>(key).FirstAsync();

        // Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _cache.GetObject<TestClass>(key).FirstAsync());
    }

    [Test]
    public async Task RemoveTyped_ShouldNotThrowForNonExistentKey()
    {
        // Arrange
        const string key = "non_existent_object";

        // Act & Assert - should not throw
        await _cache.Remove<TestClass>(key).FirstAsync();
    }

    [Test]
    public async Task RemoveMultiple_ShouldRemoveAllExistingKeys()
    {
        // Arrange
        var keys = new[] { "key1", "key2", "key3" };
        var data = new byte[] { 1, 2, 3 };

        foreach (var key in keys)
        {
            await _cache.Insert(key, data).FirstAsync();
        }

        // Act
        await _cache.Remove(keys).FirstAsync();

        // Assert
        foreach (var key in keys)
        {
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await _cache.Get(key).FirstAsync());
        }
    }

    [Test]
    public async Task RemoveMultipleTyped_ShouldRemoveAllExistingObjects()
    {
        // Arrange
        var keys = new[] { "obj1", "obj2", "obj3" };
        var testObject = new TestClass { Name = "Test", Value = 42 };

        foreach (var key in keys)
        {
            await _cache.InsertObject(key, testObject).FirstAsync();
        }

        // Act
        await _cache.Remove<TestClass>(keys).FirstAsync();

        // Assert
        foreach (var key in keys)
        {
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await _cache.GetObject<TestClass>(key).FirstAsync());
        }
    }

    [Test]
    public async Task TryRemove_ShouldReturnTrueForExistingKey()
    {
        // Arrange
        const string key = "test_key";
        var data = new byte[] { 1, 2, 3, 4 };
        await _cache.Insert(key, data).FirstAsync();

        // Act
        var result = await _cache.TryRemove(key).FirstAsync();

        // Assert
        Assert.That(result, Is.True);
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _cache.Get(key).FirstAsync());
    }

    [Test]
    public async Task TryRemove_ShouldReturnFalseForNonExistentKey()
    {
        // Arrange
        const string key = "non_existent_key";

        // Act
        var result = await _cache.TryRemove(key).FirstAsync();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task TryRemoveTyped_ShouldReturnTrueForExistingObject()
    {
        // Arrange
        const string key = "test_object";
        var testObject = new TestClass { Name = "Test", Value = 42 };
        await _cache.InsertObject(key, testObject).FirstAsync();

        // Act
        var result = await _cache.TryRemove<TestClass>(key).FirstAsync();

        // Assert
        Assert.That(result, Is.True);
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _cache.GetObject<TestClass>(key).FirstAsync());
    }

    [Test]
    public async Task TryRemoveTyped_ShouldReturnFalseForNonExistentObject()
    {
        // Arrange
        const string key = "non_existent_object";

        // Act
        var result = await _cache.TryRemove<TestClass>(key).FirstAsync();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetAllKeysSafe_ShouldReturnKeysWithoutCrashing()
    {
        // Arrange
        var keys = new[] { "key1", "key2", "key3" };
        var data = new byte[] { 1, 2, 3 };

        foreach (var key in keys)
        {
            await _cache.Insert(key, data).FirstAsync();
        }

        // Act
        var retrievedKeys = await _cache.GetAllKeysSafe().ToList().FirstAsync();

        // Assert
        Assert.That(retrievedKeys, Has.Count.EqualTo(3));
        foreach (var key in keys)
        {
            Assert.That(retrievedKeys, Does.Contain(key));
        }
    }

    [Test]
    public async Task GetAllKeysSafeTyped_ShouldReturnTypedKeysWithoutCrashing()
    {
        // Arrange
        var testObject = new TestClass { Name = "Test", Value = 42 };
        var keys = new[] { "obj1", "obj2", "obj3" };

        foreach (var key in keys)
        {
            await _cache.InsertObject(key, testObject).FirstAsync();
        }

        // Also insert some untyped data that shouldn't appear in results
        await _cache.Insert("untyped_key", new byte[] { 1, 2, 3 }).FirstAsync();

        // Act
        var retrievedKeys = await _cache.GetAllKeysSafe<TestClass>().ToList().FirstAsync();

        // Assert
        Assert.That(retrievedKeys, Has.Count.EqualTo(3));
        foreach (var key in keys)
        {
            Assert.That(retrievedKeys, Does.Contain(key));
        }
        Assert.That(retrievedKeys, Does.Not.Contain("untyped_key"));
    }

    [Test]
    public void Remove_ShouldThrowForNullCache()
    {
        // Arrange
        IBlobCache? nullCache = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullCache!.Remove("key"));
    }

    [Test]
    public void Remove_ShouldThrowForNullKey()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.Remove(null!));
        Assert.Throws<ArgumentException>(() => _cache.Remove(""));
        Assert.Throws<ArgumentException>(() => _cache.Remove("   "));
    }

    [Test]
    public void RemoveTyped_ShouldThrowForNullCache()
    {
        // Arrange
        IBlobCache? nullCache = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullCache!.Remove<TestClass>("key"));
    }

    [Test]
    public void RemoveTyped_ShouldThrowForNullKey()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.Remove<TestClass>(null!));
        Assert.Throws<ArgumentException>(() => _cache.Remove<TestClass>(""));
        Assert.Throws<ArgumentException>(() => _cache.Remove<TestClass>("   "));
    }

    [Test]
    public void TryRemove_ShouldThrowForNullCache()
    {
        // Arrange
        IBlobCache? nullCache = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullCache!.TryRemove("key"));
    }

    [Test]
    public void TryRemove_ShouldThrowForNullKey()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.TryRemove(null!));
        Assert.Throws<ArgumentException>(() => _cache.TryRemove(""));
        Assert.Throws<ArgumentException>(() => _cache.TryRemove("   "));
    }

    [Test]
    public void GetAllKeysSafe_ShouldThrowForNullCache()
    {
        // Arrange
        IBlobCache? nullCache = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullCache!.GetAllKeysSafe());
    }

    /// <summary>
    /// Test class for object serialization tests.
    /// </summary>
    public class TestClass
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }

        public override bool Equals(object? obj) =>
            obj is TestClass other && Name == other.Name && Value == other.Value;

        public override int GetHashCode() => HashCode.Combine(Name, Value);
    }
}
