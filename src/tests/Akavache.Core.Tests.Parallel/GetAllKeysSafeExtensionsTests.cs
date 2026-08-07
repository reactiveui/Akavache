// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>
/// Tests for the GetAllKeysSafe methods that provide safe alternatives to GetAllKeys()
/// to prevent crashes on mobile platforms.
/// </summary>
[Category("Akavache")]
public class GetAllKeysSafeExtensionsTests
{
    /// <summary>Key of the entry stored as a <see cref="string"/>, used to prove type filtering.</summary>
    private const string StringEntryKey = "test_string";

    /// <summary>Key of the entry stored as an <see cref="int"/>, used to prove type filtering.</summary>
    private const string IntEntryKey = "test_int";

    /// <summary>Value stored under <see cref="IntEntryKey"/>.</summary>
    private const int IntEntryValue = 42;

    /// <summary>How many keys a cache populated by two inserts is expected to report.</summary>
    private const int PopulatedCacheKeyCount = 2;

    /// <summary>Tests that GetAllKeysSafe returns an empty list for an empty cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_ShouldReturnEmptyForEmptyCache()
    {
        using var cache = CreateCache();
        var keys = cache.GetAllKeysSafe().ToList().SubscribeGetValue();
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>Tests that GetAllKeysSafe returns all keys when cache is populated.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_ShouldReturnKeysForPopulatedCache()
    {
        using var cache = CreateCache();
        byte[] firstPayload = [1, 2, 3];
        byte[] secondPayload = [4, 5, 6];
        cache.Insert("key1", firstPayload).SubscribeAndComplete();
        cache.Insert("key2", secondPayload).SubscribeAndComplete();

        var keys = cache.GetAllKeysSafe().ToList().SubscribeGetValue();

        await Assert.That(keys).Count().IsEqualTo(PopulatedCacheKeyCount);
        await Assert.That(keys!).Contains("key1");
        await Assert.That(keys).Contains("key2");
    }

    /// <summary>Tests that GetAllKeysSafe with type returns an empty list for an empty cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Test deliberately exercises the non-generic Type overload.")]
    public async Task GetAllKeysSafe_WithType_ShouldReturnEmptyForEmptyCache()
    {
        using var cache = CreateCache();
        var keys = cache.GetAllKeysSafe(typeof(string)).ToList().SubscribeGetValue();
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>Tests that GetAllKeysSafe with type returns keys filtered by the specified type.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Test deliberately exercises the non-generic Type overload.")]
    public async Task GetAllKeysSafe_WithType_ShouldReturnKeysForSpecificType()
    {
        using var cache = CreateCache();
        cache.InsertObject(StringEntryKey, "value").SubscribeAndComplete();
        cache.InsertObject(IntEntryKey, IntEntryValue).SubscribeAndComplete();

        var stringKeys = cache.GetAllKeysSafe(typeof(string)).ToList().SubscribeGetValue();
        var intKeys = cache.GetAllKeysSafe(typeof(int)).ToList().SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(stringKeys).Count().IsEqualTo(1);
            await Assert.That(stringKeys![0]).Contains(StringEntryKey);
            await Assert.That(intKeys).Count().IsEqualTo(1);
            await Assert.That(intKeys![0]).Contains(IntEntryKey);
        }
    }

    /// <summary>Tests that generic GetAllKeysSafe returns an empty list for an empty cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_Generic_ShouldReturnEmptyForEmptyCache()
    {
        using var cache = CreateCache();
        var keys = cache.GetAllKeysSafe<string>().ToList().SubscribeGetValue();
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>Tests that generic GetAllKeysSafe returns keys filtered by the specified generic type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_Generic_ShouldReturnKeysForSpecificType()
    {
        using var cache = CreateCache();
        cache.InsertObject(StringEntryKey, "value").SubscribeAndComplete();
        cache.InsertObject(IntEntryKey, IntEntryValue).SubscribeAndComplete();

        var stringKeys = cache.GetAllKeysSafe<string>().ToList().SubscribeGetValue();

        await Assert.That(stringKeys).Count().IsEqualTo(1);
        await Assert.That(stringKeys![0]).Contains(StringEntryKey);
    }

    /// <summary>Tests that GetAllKeysSafe throws ArgumentNullException for null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_ShouldThrowForNullCache()
    {
        IBlobCache? nullCache = null;
        await Assert.That(() => nullCache!.GetAllKeysSafe()).Throws<ArgumentNullException>();
    }

    /// <summary>Tests that GetAllKeysSafe with type throws ArgumentNullException for null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Test deliberately exercises the non-generic Type overload.")]
    public async Task GetAllKeysSafe_WithType_ShouldThrowForNullCache()
    {
        IBlobCache? nullCache = null;
        await Assert.That(() => nullCache!.GetAllKeysSafe(typeof(string))).Throws<ArgumentNullException>();
    }

    /// <summary>Tests that GetAllKeysSafe with type throws ArgumentNullException for null type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_WithType_ShouldThrowForNullType()
    {
        using var cache = CreateCache();
        await Assert.That(() => cache.GetAllKeysSafe(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Tests that generic GetAllKeysSafe throws ArgumentNullException for null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafe_Generic_ShouldThrowForNullCache()
    {
        IBlobCache? nullCache = null;
        await Assert.That(() => nullCache!.GetAllKeysSafe<string>()).Throws<ArgumentNullException>();
    }

    /// <summary>Creates a fresh in-memory cache with ImmediateScheduler.</summary>
    /// <returns>A new <see cref="InMemoryBlobCache"/>.</returns>
    private static InMemoryBlobCache CreateCache() => new(ImmediateSequencer.Instance, new SystemJsonSerializer());
}
