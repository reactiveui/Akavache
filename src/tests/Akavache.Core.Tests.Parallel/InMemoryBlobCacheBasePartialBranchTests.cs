// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for partial branch coverage in <see cref="InMemoryBlobCacheBase"/>.
/// </summary>
[Category("Akavache")]
public class InMemoryBlobCacheBasePartialBranchTests
{
    /// <summary>
    /// Inserting an empty collection to the untyped Insert overload returns the cached
    /// Unit observable without entering the lock (line 74 empty-input guard).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Insert_EmptyCollection_ReturnsUnitWithoutLock()
    {
        SystemJsonSerializer serializer = new();
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        var result = cache.Insert(
            []).SubscribeGetValue();

        await Assert.That(result).IsEqualTo(Unit.Default);
    }

    /// <summary>
    /// Inserting an empty collection to the typed Insert overload returns the cached
    /// Unit observable without entering the lock (line 121 empty-input guard).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Insert_EmptyCollectionTyped_ReturnsUnitWithoutLock()
    {
        SystemJsonSerializer serializer = new();
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        var result = cache.Insert(
            [],
            typeof(string)).SubscribeGetValue();

        await Assert.That(result).IsEqualTo(Unit.Default);
    }

    /// <summary>
    /// Double-dispose exercises the Interlocked.CompareExchange branch at line 822
    /// where the second dispose is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_CalledTwice_IsIdempotent()
    {
        SystemJsonSerializer serializer = new();
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, serializer);

        cache.Dispose();
        cache.Dispose();

        // After dispose, operations should throw ObjectDisposedException.
        var error = cache.Insert("key", [1, 2, 3]).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Inserting a non-empty collection followed by an empty one verifies both branches
    /// of the empty-input guard at line 74 are exercised.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Insert_NonEmptyThenEmpty_BothBranches()
    {
        SystemJsonSerializer serializer = new();
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Non-empty collection — takes the normal path.
        cache.Insert(
            [new KeyValuePair<string, byte[]>("k1", [1, 2, 3])]).SubscribeAndComplete();

        // Empty collection — takes the early-return guard.
        var result = cache.Insert(
            []).SubscribeGetValue();

        await Assert.That(result).IsEqualTo(Unit.Default);

        // Verify the first insert actually worked.
        var value = cache.Get("k1").SubscribeGetValue();
        await Assert.That(value).IsNotNull();
    }

    /// <summary>
    /// Inserting a non-empty typed collection followed by an empty one verifies both
    /// branches of the empty-input guard at line 121 are exercised.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Insert_NonEmptyTypedThenEmpty_BothBranches()
    {
        SystemJsonSerializer serializer = new();
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Non-empty typed collection — takes the normal path.
        cache.Insert(
            [new KeyValuePair<string, byte[]>("k1", [1, 2, 3])],
            typeof(string)).SubscribeAndComplete();

        // Empty typed collection — takes the early-return guard.
        var result = cache.Insert(
            [],
            typeof(string)).SubscribeGetValue();

        await Assert.That(result).IsEqualTo(Unit.Default);
    }

    /// <summary>
    /// Passing an <see cref="IEnumerable{T}"/> that is NOT an <see cref="ICollection{T}"/>
    /// (e.g. a LINQ Select projection) bypasses the empty-input guard at line 74 and
    /// enters the normal scheduling path, exercising the "not an ICollection" branch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Insert_NonCollectionEnumerable_BypassesEmptyGuard()
    {
        SystemJsonSerializer serializer = new();
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // A Select() projection is IEnumerable but not ICollection, so the
        // pattern match `keyValuePairs is ICollection { Count: 0 }` is false.
        var source = new[] { new KeyValuePair<string, byte[]>("k1", [1, 2]) }
            .Select(x => x);

        cache.Insert(source).SubscribeAndComplete();

        var value = cache.Get("k1").SubscribeGetValue();
        await Assert.That(value).IsNotNull();
    }

    /// <summary>
    /// Passing an <see cref="IEnumerable{T}"/> that is NOT an <see cref="ICollection{T}"/>
    /// to the typed Insert overload bypasses the empty-input guard at line 121 and
    /// enters the normal scheduling path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Insert_NonCollectionEnumerableTyped_BypassesEmptyGuard()
    {
        SystemJsonSerializer serializer = new();
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        var source = new[] { new KeyValuePair<string, byte[]>("k1", [1, 2]) }
            .Select(x => x);

        cache.Insert(source, typeof(string)).SubscribeAndComplete();

        var value = cache.Get("k1", typeof(string)).SubscribeGetValue();
        await Assert.That(value).IsNotNull();
    }
}
