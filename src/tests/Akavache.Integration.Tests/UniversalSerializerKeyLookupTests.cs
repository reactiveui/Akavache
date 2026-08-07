// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests for the alternative-key lookup <see cref="UniversalSerializer"/> performs when a
/// value was cached under a type-prefixed or otherwise decorated form of the requested key.
/// </summary>
[Category("Akavache")]
public class UniversalSerializerKeyLookupTests
{
    /// <summary>The cache key callers ask for when exercising alternative-key lookup.</summary>
    private const string RequestedCacheKey = "my_key";

    /// <summary>A cache key that matches none of the alternative-key rules.</summary>
    private const string UnmatchedCacheKey = "unrelated";

    /// <summary>Tests TryFindDataWithAlternativeKeys functionality.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task UniversalSerializerShouldTryAlternativeKeys()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        UserObject testObject = new() { Name = "Alt Key Test", Bio = "Alt Bio", Blog = "Alt Blog" };

        try
        {
            // Store object with prefixed key
            _ = cache.InsertObject("test_key", testObject).Subscribe();

            // Act - Try to find with alternative keys
            var result = await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<UserObject>(
                cache,
                "test_key",
                serializer);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("Alt Key Test");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests <see cref="UniversalSerializer.FindKeyCandidates{T}"/> includes a key that is exactly the requested key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FindKeyCandidatesShouldIncludeExactKey()
    {
        var candidates = UniversalSerializer.FindKeyCandidates<UserObject>(
            [RequestedCacheKey, "other"],
            RequestedCacheKey);

        await Assert.That(candidates).Contains(RequestedCacheKey);
        await Assert.That(candidates.Contains("other")).IsFalse();
    }

    /// <summary>Tests <see cref="UniversalSerializer.FindKeyCandidates{T}"/> includes a type-prefixed key (<c>Namespace.Type___key</c>) when that key is present in the cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FindKeyCandidatesShouldIncludeTypePrefixedKey()
    {
        var typePrefixed = $"{typeof(UserObject).FullName}___{RequestedCacheKey}";
        var candidates = UniversalSerializer.FindKeyCandidates<UserObject>(
            [typePrefixed, UnmatchedCacheKey],
            RequestedCacheKey);

        await Assert.That(candidates).Contains(typePrefixed);
    }

    /// <summary>Tests <see cref="UniversalSerializer.FindKeyCandidates{T}"/> includes keys ending with <c>___{requestedKey}</c> even when the prefix does not match any known type shape.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FindKeyCandidatesShouldIncludeCustomTripleUnderscoreSuffixKey()
    {
        var candidates = UniversalSerializer.FindKeyCandidates<UserObject>(
            ["unknown.Type___my_key", UnmatchedCacheKey],
            RequestedCacheKey);

        await Assert.That(candidates).Contains("unknown.Type___my_key");
        await Assert.That(candidates.Contains(UnmatchedCacheKey)).IsFalse();
    }

    /// <summary>Tests <see cref="UniversalSerializer.FindKeyCandidates{T}"/> includes keys that only end with the requested key (no <c>___</c> separator).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FindKeyCandidatesShouldIncludePlainSuffixKey()
    {
        var candidates = UniversalSerializer.FindKeyCandidates<UserObject>(
            ["prefix-my_key", "something_else"],
            RequestedCacheKey);

        await Assert.That(candidates).Contains("prefix-my_key");
        await Assert.That(candidates.Contains("something_else")).IsFalse();
    }

    /// <summary>Tests <see cref="UniversalSerializer.FindKeyCandidates{T}"/> returns an empty list when no keys match any of the criteria.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FindKeyCandidatesShouldReturnEmptyWhenNoMatches()
    {
        var candidates = UniversalSerializer.FindKeyCandidates<UserObject>(
            ["alpha", "beta", "gamma"],
            RequestedCacheKey);

        await Assert.That(candidates).IsEmpty();
    }

    /// <summary>Tests TryFindDataWithAlternativeKeys with null cache returns default.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultForNullCache()
    {
        var result =
            await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<string>(null!, "key", new SystemJsonSerializer());
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests TryFindDataWithAlternativeKeys with null key returns default.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultForNullKey()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<string>(
                    cache,
                    null!,
                    new SystemJsonSerializer());
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests TryFindDataWithAlternativeKeys with null serializer returns default.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultForNullSerializer()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            var result = await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<string>(cache, "key", null!);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests TryFindDataWithAlternativeKeys with empty cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultForEmptyCache()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<string>(
                    cache,
                    "nonexistent",
                    new SystemJsonSerializer());
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests TryFindDataWithAlternativeKeys finds entry under type-prefixed key.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldFindByTypePrefix()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        try
        {
            UserObject testObj = new() { Name = "found", Bio = "bio", Blog = "blog" };
            var prefixedKey = $"{typeof(UserObject).FullName}___{RequestedCacheKey}";
            var bytes = serializer.Serialize(testObj);
            _ = cache.Insert(prefixedKey, bytes).Subscribe();

            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<UserObject>(
                    cache,
                    RequestedCacheKey,
                    serializer);

            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("found");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests TryFindDataWithAlternativeKeys finds entry under short-name prefix.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldFindByShortNamePrefix()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        try
        {
            UserObject testObj = new() { Name = "found2", Bio = "bio", Blog = "blog" };
            const string shortKey = $"{nameof(UserObject)}___my_key2";
            var bytes = serializer.Serialize(testObj);
            _ = cache.Insert(shortKey, bytes).Subscribe();

            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<UserObject>(cache, "my_key2", serializer);

            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("found2");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests TryFindDataWithAlternativeKeys returns default when entry exists but is empty.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultForEmptyEntry()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        try
        {
            _ = cache.Insert("empty_key", []).Subscribe();

            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<UserObject>(cache, "empty_key", serializer);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests TryFindDataWithAlternativeKeys outer catch when cache throws on GetAllKeys (disposed cache).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultWhenGetAllKeysThrows()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        cache.Dispose();

        // Disposed cache's GetAllKeys returns an observable that throws ObjectDisposedException.
        // This exercises the outer catch block (lines 209-212).
        var result =
            await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<UserObject>(
                cache,
                "some_key",
                new SystemJsonSerializer());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys inner catch when cache.Get throws for a candidate key.
    /// Uses a custom cache wrapper whose Get throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldReturnDefaultWhenGetThrowsInnerCatch()
    {
        InMemoryBlobCache inner = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            // Insert a key whose key is the raw "lookup" name so it is matched by EndsWith.
            _ = inner.Insert("lookup_inner_catch", [0x01, 0x02]).Subscribe();

            ThrowingGetCacheWrapper wrapper = new(inner);
            var result =
                await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<UserObject>(
                    wrapper,
                    "lookup_inner_catch",
                    new SystemJsonSerializer());
            await Assert.That(result).IsNull();
        }
        finally
        {
            inner.Dispose();
        }
    }

    /// <summary>
    /// Tests TryFindDataWithAlternativeKeys line 201 false branch: deserialization succeeds
    /// but the result equals default for a value type, so the method continues searching.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataWithAlternativeKeysShouldSkipDefaultValueResults()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        try
        {
            // Store a default int (0) under a key that will match
            var zeroBytes = serializer.Serialize(0);
            _ = cache.Insert("val_key", zeroBytes).Subscribe();

            // TryFindDataWithAlternativeKeys deserializes to 0 (== default(int)), which hits
            // the false branch of the null/default check on line 197-198, causing it to continue.
            var result = await UniversalSerializer.TryFindDataWithAlternativeKeysAsync<int>(cache, "val_key", serializer);
            await Assert.That(result).IsEqualTo(0);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// A blob cache wrapper that delegates to an inner cache but throws on Get(string).
    /// Used to exercise the inner catch in TryFindDataWithAlternativeKeys.
    /// </summary>
    /// <param name="inner">The cache that receives every call except the single-key <c>Get</c> overloads.</param>
    private sealed class ThrowingGetCacheWrapper(IBlobCache inner) : IBlobCache
    {
        /// <inheritdoc/>
        public ISerializer Serializer => inner.Serializer;

        /// <inheritdoc/>
        public ISequencer Scheduler => inner.Scheduler;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind
        {
            get => inner.ForcedDateTimeKind;
            set => inner.ForcedDateTimeKind = value;
        }

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs) =>
            Insert(keyValuePairs, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            DateTimeOffset? absoluteExpiration) =>
                inner.Insert(keyValuePairs, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(string key, byte[] data) =>
            Insert(key, data, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration) =>
            inner.Insert(key, data, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type) =>
            Insert(keyValuePairs, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            Type type,
            DateTimeOffset? absoluteExpiration) =>
                inner.Insert(keyValuePairs, type, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(string key, byte[] data, Type type) =>
            Insert(key, data, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid>
            Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration) =>
            inner.Insert(key, data, type, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) =>
            Signal.Throw<byte[]?>(new InvalidOperationException("Throwing Get"));

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => inner.Get(keys);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) =>
            Signal.Throw<byte[]?>(new InvalidOperationException("Throwing Get"));

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) =>
            inner.Get(keys, type);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => inner.GetAll(type);

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => inner.GetAllKeys();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => inner.GetAllKeys(type);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
            inner.GetCreatedAt(keys);

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => inner.GetCreatedAt(key);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
            inner.GetCreatedAt(keys, type);

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => inner.GetCreatedAt(key, type);

        /// <inheritdoc/>
        public IObservable<RxVoid> Flush() => inner.Flush();

        /// <inheritdoc/>
        public IObservable<RxVoid> Flush(Type type) => inner.Flush(type);

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(string key) => inner.Invalidate(key);

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(string key, Type type) => inner.Invalidate(key, type);

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(IEnumerable<string> keys) => inner.Invalidate(keys);

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(IEnumerable<string> keys, Type type) => inner.Invalidate(keys, type);

        /// <inheritdoc/>
        public IObservable<RxVoid> InvalidateAll(Type type) => inner.InvalidateAll(type);

        /// <inheritdoc/>
        public IObservable<RxVoid> InvalidateAll() => inner.InvalidateAll();

        /// <inheritdoc/>
        public IObservable<RxVoid> Vacuum() => inner.Vacuum();

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
            inner.UpdateExpiration(key, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
            inner.UpdateExpiration(key, type, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
            inner.UpdateExpiration(keys, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(
            IEnumerable<string> keys,
            Type type,
            DateTimeOffset? absoluteExpiration) => inner.UpdateExpiration(keys, type, absoluteExpiration);

        /// <inheritdoc/>
        public void Dispose()
        {
            // Caller owns inner cache; do not dispose twice.
        }
    }
}
