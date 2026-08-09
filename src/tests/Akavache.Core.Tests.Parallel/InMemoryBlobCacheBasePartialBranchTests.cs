// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for partial branch coverage in <see cref="InMemoryBlobCacheBase"/>.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
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
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        var result = cache.Insert(
            []).SubscribeGetValue();

        await Assert.That(result).IsEqualTo(RxVoid.Default);
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
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        var result = cache.Insert(
            [],
            typeof(string)).SubscribeGetValue();

        await Assert.That(result).IsEqualTo(RxVoid.Default);
    }

    /// <summary>Double-dispose exercises the Interlocked.CompareExchange branch at line 822 where the second dispose is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_CalledTwice_IsIdempotent()
    {
        SystemJsonSerializer serializer = new();
        var cache = new InMemoryBlobCache(ImmediateSequencer.Instance, serializer);

        cache.Dispose();
        cache.Dispose();

        // After dispose, operations should throw ObjectDisposedException.
        byte[] payload = [1, 2, 3];
        var error = cache.Insert("key", payload).SubscribeGetError();
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
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Non-empty collection — takes the normal path.
        byte[] payload = [1, 2, 3];
        cache.Insert(
            [new KeyValuePair<string, byte[]>("k1", payload)]).WaitForCompletion();

        // Empty collection — takes the early-return guard.
        var result = cache.Insert(
            []).SubscribeGetValue();

        await Assert.That(result).IsEqualTo(RxVoid.Default);

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
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Non-empty typed collection — takes the normal path.
        byte[] payload = [1, 2, 3];
        cache.Insert(
            [new KeyValuePair<string, byte[]>("k1", payload)],
            typeof(string)).WaitForCompletion();

        // Empty typed collection — takes the early-return guard.
        var result = cache.Insert(
            [],
            typeof(string)).SubscribeGetValue();

        await Assert.That(result).IsEqualTo(RxVoid.Default);
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
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // A Select() projection is IEnumerable but not ICollection, so the
        // pattern match `keyValuePairs is ICollection { Count: 0 }` is false.
        // .Select(x => x) intentionally wraps the array in a non-ICollection IEnumerable to bypass the Count guard.
        byte[] payload = [1, 2];
        var source = new[] { new KeyValuePair<string, byte[]>("k1", payload) }
            .Select(static x => x);

        cache.Insert(source).WaitForCompletion();

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
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // .Select(x => x) intentionally wraps the array in a non-ICollection IEnumerable to bypass the Count guard.
        byte[] payload = [1, 2];
        var source = new[] { new KeyValuePair<string, byte[]>("k1", payload) }
            .Select(static x => x);

        cache.Insert(source, typeof(string)).WaitForCompletion();

        var value = cache.Get("k1", typeof(string)).SubscribeGetValue();
        await Assert.That(value).IsNotNull();
    }
}
