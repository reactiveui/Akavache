// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for the GetAllKeysSafe methods that provide safe alternatives to GetAllKeys()
/// to prevent crashes on mobile platforms.
/// </summary>
[Category("Akavache")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "RCS1261:Resource can be disposed asynchronously", Justification = "Tests use synchronous Dispose to avoid async deadlocks.")]
public class GetAllKeysSafeExtensionsTests
{
    /// <summary>
    /// Tests that GetAllKeysSafe returns an empty list for an empty cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_ShouldReturnEmptyForEmptyCache()
    {
        using var cache = CreateCache();
        var keys = cache.GetAllKeysSafe().ToList().SubscribeGetValue();
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests that GetAllKeysSafe returns all keys when cache is populated.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_ShouldReturnKeysForPopulatedCache()
    {
        using var cache = CreateCache();
        cache.Insert("key1", [1, 2, 3]).SubscribeAndComplete();
        cache.Insert("key2", [4, 5, 6]).SubscribeAndComplete();

        var keys = cache.GetAllKeysSafe().ToList().SubscribeGetValue();

        await Assert.That(keys).Count().IsEqualTo(2);
        await Assert.That(keys!).Contains("key1");
        await Assert.That(keys).Contains("key2");
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with type returns an empty list for an empty cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Test deliberately exercises the non-generic Type overload.")]
    public async Task GetAllKeysSafe_WithType_ShouldReturnEmptyForEmptyCache()
    {
        using var cache = CreateCache();
        var keys = cache.GetAllKeysSafe(typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with type returns keys filtered by the specified type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Test deliberately exercises the non-generic Type overload.")]
    public async Task GetAllKeysSafe_WithType_ShouldReturnKeysForSpecificType()
    {
        using var cache = CreateCache();
        cache.InsertObject("test_string", "value").SubscribeAndComplete();
        cache.InsertObject("test_int", 42).SubscribeAndComplete();

        var stringKeys = cache.GetAllKeysSafe(typeof(string)).ToList().SubscribeGetValue();
        var intKeys = cache.GetAllKeysSafe(typeof(int)).ToList().SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(stringKeys).Count().IsEqualTo(1);
            await Assert.That(stringKeys![0]).Contains("test_string");
            await Assert.That(intKeys).Count().IsEqualTo(1);
            await Assert.That(intKeys![0]).Contains("test_int");
        }
    }

    /// <summary>
    /// Tests that generic GetAllKeysSafe returns an empty list for an empty cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_Generic_ShouldReturnEmptyForEmptyCache()
    {
        using var cache = CreateCache();
        var keys = cache.GetAllKeysSafe<string>().ToList().SubscribeGetValue();
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests that generic GetAllKeysSafe returns keys filtered by the specified generic type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_Generic_ShouldReturnKeysForSpecificType()
    {
        using var cache = CreateCache();
        cache.InsertObject("test_string", "value").SubscribeAndComplete();
        cache.InsertObject("test_int", 42).SubscribeAndComplete();

        var stringKeys = cache.GetAllKeysSafe<string>().ToList().SubscribeGetValue();

        await Assert.That(stringKeys).Count().IsEqualTo(1);
        await Assert.That(stringKeys![0]).Contains("test_string");
    }

    /// <summary>
    /// Tests that GetAllKeysSafe throws ArgumentNullException for null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_ShouldThrowForNullCache()
    {
        IBlobCache? nullCache = null;
        await Assert.That(() => nullCache!.GetAllKeysSafe()).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with type throws ArgumentNullException for null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Test deliberately exercises the non-generic Type overload.")]
    public async Task GetAllKeysSafe_WithType_ShouldThrowForNullCache()
    {
        IBlobCache? nullCache = null;
        await Assert.That(() => nullCache!.GetAllKeysSafe(typeof(string))).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with type throws ArgumentNullException for null type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_WithType_ShouldThrowForNullType()
    {
        using var cache = CreateCache();
        await Assert.That(() => cache.GetAllKeysSafe(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that generic GetAllKeysSafe throws ArgumentNullException for null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_Generic_ShouldThrowForNullCache()
    {
        IBlobCache? nullCache = null;
        await Assert.That(() => nullCache!.GetAllKeysSafe<string>()).Throws<ArgumentNullException>();
    }

    /// <summary>Creates a fresh in-memory cache with ImmediateScheduler.</summary>
    /// <returns>A new <see cref="InMemoryBlobCache"/>.</returns>
    private static InMemoryBlobCache CreateCache() => new(ImmediateScheduler.Instance, new SystemJsonSerializer());
}
