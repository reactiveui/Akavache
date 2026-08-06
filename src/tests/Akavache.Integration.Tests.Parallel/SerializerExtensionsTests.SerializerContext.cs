// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;

namespace Akavache.Integration.Tests;

/// <summary>Tests covering serialization through a cache's serializer context and the safe key enumerations.</summary>
public partial class SerializerExtensionsTests
{
    /// <summary>Tests SerializeWithContext throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithContextShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.SerializeWithContext(SingleEntryValue, null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests DeserializeWithContext returns default for null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldReturnDefaultForNullCache()
    {
        byte[] payload = [1, 2, 3];
        var result = SerializerExtensions.DeserializeWithContext<string>(payload, null!);
        await Assert.That(result).IsNull();
    }

    /// <summary>Tests DeserializeWithContext returns default for null data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldReturnDefaultForNullData()
    {
        var cache = CreateCache();
        try
        {
            var result = SerializerExtensions.DeserializeWithContext<string>(null!, cache);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DeserializeWithContext returns default for empty data.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldReturnDefaultForEmptyData()
    {
        var cache = CreateCache();
        try
        {
            var result = SerializerExtensions.DeserializeWithContext<string>([], cache);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests SerializeWithContext handles DateTime via UniversalSerializer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithContextShouldHandleDateTime()
    {
        var cache = CreateCache();
        try
        {
            DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var bytes = SerializerExtensions.SerializeWithContext(date, cache);
            await Assert.That(bytes).IsNotNull();
            await Assert.That(bytes.Length).IsGreaterThan(0);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DeserializeWithContext handles DateTime via UniversalSerializer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldHandleDateTime()
    {
        var cache = CreateCache();
        try
        {
            DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var bytes = SerializerExtensions.SerializeWithContext(date, cache);
            var result = SerializerExtensions.DeserializeWithContext<DateTime>(bytes, cache);
            await Assert.That(result.Year).IsEqualTo(SampleYear);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that SerializerExtensions.InsertObject extension stores empty bytes for a null value.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectExtensionShouldStoreEmptyBytesForNullValue()
    {
        var cache = CreateCache();
        try
        {
            // Call extension explicitly to bypass instance-method shadowing.
            _ = SerializerExtensions.InsertObject<string>(cache, "null_key", null!).Subscribe();
            string? result = null;
            _ = SerializerExtensions.GetObject<string>(cache, "null_key").Subscribe(v => result = v);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that SerializerExtensions.InsertObject wraps serialization failures in InvalidOperationException.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectExtensionShouldWrapSerializationFailure()
    {
        var cache = CreateCache();
        try
        {
            // Circular reference causes System.Text.Json to throw.
            List<object> circular = [];
            circular.Add(circular);

            await Assert.That(() => SerializerExtensions.InsertObject(cache, "cyc", circular).Subscribe())
                .Throws<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that SerializerExtensions.GetObject returns default for an empty byte marker.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectExtensionShouldReturnDefaultForEmptyBytes()
    {
        var cache = CreateCache();
        try
        {
            // Store an empty byte array under a typed key to trigger the empty-length branch.
            _ = cache.Insert("empty_key", [], typeof(string)).Subscribe();
            string? result = null;
            _ = SerializerExtensions.GetObject<string>(cache, "empty_key").Subscribe(v => result = v);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that SerializerExtensions.GetObject wraps deserialization failures in InvalidOperationException.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectExtensionShouldWrapDeserializationFailure()
    {
        var cache = CreateCache();
        try
        {
            // Store invalid JSON bytes under a typed key so deserialization fails.
            byte[] invalid = [0xFF, 0xFE, 0xFD, 0x01];
            _ = cache.Insert("bad_json", invalid, typeof(UserObject)).Subscribe();

            Exception? error = null;
            _ = SerializerExtensions.GetObject<UserObject>(cache, "bad_json").Subscribe(static _ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAllObjects returns all stored objects of the requested type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllObjectsShouldReturnStoredObjects()
    {
        var cache = CreateCache();
        try
        {
            UserObject u1 = new() { Name = "A", Bio = "B1", Blog = "Bl1" };
            UserObject u2 = new() { Name = "B", Bio = "B2", Blog = "Bl2" };
            _ = SerializerExtensions.InsertObject(cache, "a", u1).Subscribe();
            _ = SerializerExtensions.InsertObject(cache, "b", u2).Subscribe();

            IEnumerable<UserObject>? allEnumerable = null;
            _ = cache.GetAllObjects<UserObject>().Subscribe(v => allEnumerable = v);
            var all = allEnumerable!.ToList();
            await Assert.That(all.Count).IsEqualTo(SampleUserCount);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAndFetchLatest with cacheValidationPredicate returning false skips caching.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestShouldSkipCachingWhenValidationFails()
    {
        var cache = CreateCache();
        try
        {
            UserObject latest = new() { Name = "Latest", Bio = "B", Blog = "Bl" };

            IList<UserObject?>? results = null;
            _ = cache.GetAndFetchLatest(
                    "validate_key",
                    () => Observable.Return(latest),
                    fetchPredicate: null,
                    absoluteExpiration: null,
                    shouldInvalidateOnError: false,
                    cacheValidationPredicate: static _ => false)
                .ToList()
                .Subscribe(v => results = v);

            await Assert.That(results).IsNotEmpty();

            // Since cacheValidationPredicate returned false, the cache should not contain the key.
            Exception? error = null;
            _ = cache.GetObject<UserObject>("validate_key").Subscribe(static _ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAndFetchLatest invalidates the cache on fetch error when shouldInvalidateOnError is true.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestShouldInvalidateOnErrorWhenRequested()
    {
        var cache = CreateCache();
        try
        {
            UserObject cached = new() { Name = CachedName, Bio = "B", Blog = "Bl" };
            _ = SerializerExtensions.InsertObject(cache, InvalidatedKey, cached).Subscribe();

            List<UserObject?> observed = [];
            Exception? caught = null;

            try
            {
                await cache.GetAndFetchLatest(
                        InvalidatedKey,
                        static () => Observable.Throw<UserObject>(new InvalidOperationException("fetch boom")),
                        fetchPredicate: null,
                        absoluteExpiration: null,
                        shouldInvalidateOnError: true)
                    .ForEachAsync(observed.Add);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            await Assert.That(caught).IsNotNull();

            // Cache entry should have been invalidated.
            Exception? error = null;
            _ = cache.GetObject<UserObject>(InvalidatedKey).Subscribe(static _ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAndFetchLatest without shouldInvalidateOnError preserves the cached value on fetch error.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestShouldNotInvalidateOnErrorByDefault()
    {
        var cache = CreateCache();
        try
        {
            UserObject cached = new() { Name = CachedName, Bio = "B", Blog = "Bl" };
            _ = SerializerExtensions.InsertObject(cache, RetainedKey, cached).Subscribe();

            List<UserObject?> observed = [];
            try
            {
                await cache.GetAndFetchLatest(
                        RetainedKey,
                        static () => Observable.Throw<UserObject>(new InvalidOperationException("fetch boom")))
                    .ForEachAsync(observed.Add);
            }
            catch (InvalidOperationException)
            {
                // The fetch was set up to fail; the cached value must survive it.
            }

            // Cache entry should still exist.
            UserObject? stillThere = null;
            _ = cache.GetObject<UserObject>(RetainedKey).Subscribe(v => stillThere = v);
            await Assert.That(stillThere).IsNotNull();
            await Assert.That(stillThere!.Name).IsEqualTo(CachedName);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that the observable-fetch overload taking an expiration but no invalidation flag
    /// leaves the cached entry alone when the fetch fails — it supplies
    /// <c>shouldInvalidateOnError: false</c> on the caller's behalf.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestWithExpirationShouldRetainCachedValueWhenFetchFails()
    {
        var cache = CreateCache();
        try
        {
            const string key = "expiry_retained";
            UserObject cached = new() { Name = CachedName, Bio = "B", Blog = "Bl" };
            _ = SerializerExtensions.InsertObject(cache, key, cached).Subscribe();

            Exception? caught = null;
            try
            {
                await cache.GetAndFetchLatest(
                        key,
                        static () => Observable.Throw<UserObject>(new InvalidOperationException("expiring fetch boom")),
                        fetchPredicate: null,
                        absoluteExpiration: TimeProvider.System.GetUtcNow().AddHours(1))
                    .ForEachAsync(static _ => { });
            }
            catch (InvalidOperationException ex)
            {
                caught = ex;
            }

            await Assert.That(caught).IsNotNull();

            // The overload defaults shouldInvalidateOnError to false, so the entry survives.
            UserObject? stillThere = null;
            _ = cache.GetObject<UserObject>(key).Subscribe(v => stillThere = v);
            await Assert.That(stillThere).IsNotNull();
            await Assert.That(stillThere!.Name).IsEqualTo(CachedName);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that the task-fetch overload taking only a fetch predicate caches what it fetches —
    /// it supplies <c>cacheValidationPredicate: null</c> on the caller's behalf — and then lets
    /// the predicate veto a second fetch now that a cached timestamp exists.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestTaskOverloadWithFetchPredicateShouldCacheFetchedValueAndHonourFreshVerdict()
    {
        var cache = CreateCache();
        try
        {
            const string key = "task_predicate";
            UserObject latest = new() { Name = LatestUserName, Bio = "B", Blog = "Bl" };
            var fetchCount = 0;
            var fetchFunc = () =>
            {
                fetchCount++;
                return Task.FromResult(latest);
            };

            // Nothing is cached, so there is no timestamp for the predicate to judge and the fetch runs anyway.
            List<UserObject?> firstPass = [];
            await cache.GetAndFetchLatest(key, fetchFunc, fetchPredicate: static _ => false)
                .ForEachAsync(firstPass.Add);

            await Assert.That(firstPass).Count().IsEqualTo(1);

            // No cacheValidationPredicate was supplied, so the fetched value was written to the cache.
            UserObject? cachedNow = null;
            _ = cache.GetObject<UserObject>(key).Subscribe(v => cachedNow = v);
            await Assert.That(cachedNow).IsNotNull();
            await Assert.That(cachedNow!.Name).IsEqualTo(LatestUserName);

            // Now there is a timestamp, the predicate declares it fresh and no second fetch happens.
            List<UserObject?> secondPass = [];
            await cache.GetAndFetchLatest(key, fetchFunc, fetchPredicate: static _ => false)
                .ForEachAsync(secondPass.Add);

            using (Assert.Multiple())
            {
                await Assert.That(fetchCount).IsEqualTo(1);
                await Assert.That(secondPass).Count().IsEqualTo(1);
            }

            await Assert.That(secondPass[0]!.Name).IsEqualTo(LatestUserName);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that the task-fetch overload taking a fetch predicate and an expiration caches what
    /// it fetches (no cache-validation predicate is supplied) and keeps that entry when a later
    /// fetch through the same overload fails (no invalidate-on-error is supplied either).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestTaskOverloadWithExpirationShouldCacheFetchedValueAndRetainItWhenFetchFails()
    {
        var cache = CreateCache();
        try
        {
            const string key = "task_expiry";
            UserObject latest = new() { Name = LatestUserName, Bio = "B", Blog = "Bl" };
            var expiration = TimeProvider.System.GetUtcNow().AddHours(1);

            List<UserObject?> fetched = [];
            await cache.GetAndFetchLatest(
                    key,
                    () => Task.FromResult(latest),
                    fetchPredicate: null,
                    absoluteExpiration: expiration)
                .ForEachAsync(fetched.Add);

            await Assert.That(fetched).Count().IsEqualTo(1);

            // No cacheValidationPredicate was supplied, so the fetched value was written to the cache.
            UserObject? cachedNow = null;
            _ = cache.GetObject<UserObject>(key).Subscribe(v => cachedNow = v);
            await Assert.That(cachedNow).IsNotNull();
            await Assert.That(cachedNow!.Name).IsEqualTo(LatestUserName);

            Exception? caught = null;
            try
            {
                await cache.GetAndFetchLatest(
                        key,
                        static () => Task.FromException<UserObject>(new InvalidOperationException("task fetch boom")),
                        fetchPredicate: null,
                        absoluteExpiration: expiration)
                    .ForEachAsync(static _ => { });
            }
            catch (InvalidOperationException ex)
            {
                caught = ex;
            }

            await Assert.That(caught).IsNotNull();

            // The overload defaults shouldInvalidateOnError to false, so the entry survives.
            UserObject? stillThere = null;
            _ = cache.GetObject<UserObject>(key).Subscribe(v => stillThere = v);
            await Assert.That(stillThere).IsNotNull();
            await Assert.That(stillThere!.Name).IsEqualTo(LatestUserName);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests SerializeWithContext handles nullable DateTime via UniversalSerializer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithContextShouldHandleNullableDateTime()
    {
        var cache = CreateCache();
        try
        {
            DateTime? date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var bytes = SerializerExtensions.SerializeWithContext(date, cache);
            await Assert.That(bytes).IsNotNull();
            await Assert.That(bytes.Length).IsGreaterThan(0);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DeserializeWithContext handles nullable DateTime via UniversalSerializer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldHandleNullableDateTime()
    {
        var cache = CreateCache();
        try
        {
            DateTime? date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var bytes = SerializerExtensions.SerializeWithContext(date, cache);
            var result = SerializerExtensions.DeserializeWithContext<DateTime?>(bytes, cache);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Value.Year).IsEqualTo(SampleYear);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests SerializeWithContext applies ForcedDateTimeKind for non-DateTime types.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithContextShouldApplyForcedDateTimeKindForNonDateTime()
    {
        var cache = CreateCache();
        try
        {
            cache.ForcedDateTimeKind = DateTimeKind.Utc;
            UserObject user = new() { Name = "Forced", Bio = "B", Blog = "Bl" };
            var bytes = SerializerExtensions.SerializeWithContext(user, cache);
            await Assert.That(bytes.Length).IsGreaterThan(0);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DeserializeWithContext applies ForcedDateTimeKind for non-DateTime types.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldApplyForcedDateTimeKindForNonDateTime()
    {
        var cache = CreateCache();
        try
        {
            cache.ForcedDateTimeKind = DateTimeKind.Utc;
            UserObject user = new() { Name = "Forced2", Bio = "B", Blog = "Bl" };
            var bytes = SerializerExtensions.SerializeWithContext(user, cache);
            var result = SerializerExtensions.DeserializeWithContext<UserObject>(bytes, cache);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("Forced2");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests DeserializeWithContext wraps serializer failures in InvalidOperationException.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldWrapSerializerFailure()
    {
        var cache = CreateCache();
        try
        {
            byte[] invalid = [0xFF, 0xFE, 0xFD, 0x01];
            await Assert.That(() => SerializerExtensions.DeserializeWithContext<UserObject>(invalid, cache))
                .Throws<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests DeserializeWithContext falls back to UniversalSerializer for DateTime failures
    /// and returns the default value when fallback deserialization also produces default.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldFallbackForDateTimeFailure()
    {
        var cache = CreateCache();
        try
        {
            byte[] invalid = [0xFF, 0xFE, 0xFD, 0x01];

            // The primary serializer fails on invalid bytes, then UniversalSerializer's
            // TryFallbackDeserialization returns default(DateTime) without throwing.
            var result = SerializerExtensions.DeserializeWithContext<DateTime>(invalid, cache);
            await Assert.That(result).IsEqualTo(default);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests DeserializeWithContext falls back for DateTimeOffset failures
    /// and returns the default value when fallback deserialization also produces default.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldFallbackForDateTimeOffsetFailure()
    {
        using var cache = CreateCache();
        byte[] invalid = [0xFF, 0xFE, 0xFD, 0x01];

        // The primary serializer fails on invalid bytes, then UniversalSerializer's
        // TryFallbackDeserialization returns default(DateTimeOffset) without throwing.
        var result = SerializerExtensions.DeserializeWithContext<DateTimeOffset>(invalid, cache);
        await Assert.That(result).IsEqualTo(default);
    }

    /// <summary>Tests GetAllKeysSafe recovers from a failing underlying source by emitting empty.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeShouldRecoverFromExceptions()
    {
        var cache = CreateCache();
        cache.Dispose();

        // After dispose, GetAllKeys throws; GetAllKeysSafe should swallow and return empty.
        IList<string>? keys = null;
        _ = cache.GetAllKeysSafe().ToList().Subscribe(v => keys = v);
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>Tests GetAllKeysSafe(Type) recovers from a failing underlying source by emitting empty.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2263:Prefer generic overload when type is known",
        Justification = "Test deliberately exercises the non-generic Type overload.")]
    public async Task GetAllKeysSafeWithTypeShouldRecoverFromExceptions()
    {
        var cache = CreateCache();
        cache.Dispose();

        IList<string>? keys = null;
        _ = cache.GetAllKeysSafe(typeof(UserObject)).ToList().Subscribe(v => keys = v);
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>Tests that GetOrCreateObject throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateObjectShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetOrCreateObject(null!, "key", static () => SingleEntryValue))
            .Throws<ArgumentNullException>();

    /// <summary>Tests that GetAllKeysSafe throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetAllKeysSafe(null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests that GetAllKeysSafe with Type throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeWithTypeShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetAllKeysSafe(null!, typeof(string)))
            .Throws<ArgumentNullException>();

    /// <summary>Tests that GetAllKeysSafe with Type throws ArgumentNullException when type is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeWithTypeShouldThrowOnNullType()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => cache.GetAllKeysSafe(null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that generic GetAllKeysSafe throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeGenericShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetAllKeysSafe<string>(null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests that the generic GetAllKeysSafe returns keys for a specific type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeGenericShouldReturnKeysForType()
    {
        var cache = CreateCache();
        try
        {
            _ = SerializerExtensions.InsertObject(cache, "u1", new UserObject { Name = "A", Bio = "B", Blog = "C" })
                .Subscribe();

            IList<string>? keys = null;
            _ = cache.GetAllKeysSafe<UserObject>().ToList().Subscribe(v => keys = v);
            await Assert.That(keys!.Count).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that the generic GetAllKeysSafe recovers from exceptions by returning empty.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeGenericShouldRecoverFromExceptions()
    {
        var cache = CreateCache();
        cache.Dispose();

        // After dispose, GetAllKeys throws; GetAllKeysSafe<T> should swallow and return empty.
        IList<string>? keys = null;
        _ = cache.GetAllKeysSafe<UserObject>().ToList().Subscribe(v => keys = v);
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>Tests that GetAllKeysSafe filters out null and empty keys from a valid cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeShouldReturnKeysFromValidCache()
    {
        var cache = CreateCache();
        try
        {
            byte[] firstPayload = [1, 2, 3];
            byte[] secondPayload = [4, 5, 6];
            _ = cache.Insert("safe_key1", firstPayload).Subscribe();
            _ = cache.Insert("safe_key2", secondPayload).Subscribe();

            IList<string>? keys = null;
            _ = cache.GetAllKeysSafe().ToList().Subscribe(v => keys = v);
            await Assert.That(keys!.Count).IsGreaterThanOrEqualTo(InsertedKeyCount);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAllKeysSafe with a Type parameter returns keys for valid cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2263:Prefer generic overload when type is known",
        Justification = "Test deliberately exercises the non-generic Type overload.")]
    public async Task GetAllKeysSafeWithTypeShouldReturnKeysForValidCache()
    {
        var cache = CreateCache();
        try
        {
            _ = SerializerExtensions.InsertObject(cache, "typed_key", new UserObject { Name = "T", Bio = "B", Blog = "Bl" })
                .Subscribe();

            IList<string>? keys = null;
            _ = cache.GetAllKeysSafe(typeof(UserObject)).ToList().Subscribe(v => keys = v);
            await Assert.That(keys!.Count).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetObject throws KeyNotFoundException when the underlying cache returns null bytes.
    /// This covers the null byte array guard branch inside GetObject's Select lambda.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectShouldThrowKeyNotFoundWhenCacheReturnsNullBytes()
    {
        NullReturningBlobCache cache = new(new SystemJsonSerializer());
        try
        {
            Exception? error = null;
            _ = cache.GetObject<UserObject>("any_key").Subscribe(static _ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAndFetchLatest Task overload with shouldInvalidateOnError invalidates cache on error.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestTaskOverloadShouldInvalidateOnError()
    {
        var cache = CreateCache();
        try
        {
            UserObject cached = new() { Name = CachedName, Bio = "B", Blog = "Bl" };
            _ = SerializerExtensions.InsertObject(cache, TaskInvalidatedKey, cached).Subscribe();

            Exception? caught = null;
            try
            {
                await cache.GetAndFetchLatest(
                        TaskInvalidatedKey,
                        static () => Task.FromException<UserObject>(new InvalidOperationException("task fetch boom")),
                        fetchPredicate: null,
                        absoluteExpiration: null,
                        shouldInvalidateOnError: true)
                    .ForEachAsync(static _ => { });
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            await Assert.That(caught).IsNotNull();

            // Cache entry should have been invalidated.
            Exception? error = null;
            _ = cache.GetObject<UserObject>(TaskInvalidatedKey).Subscribe(static _ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAndFetchLatest Task overload with cacheValidationPredicate returning false skips caching.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestTaskOverloadShouldSkipCachingWhenValidationFails()
    {
        var cache = CreateCache();
        try
        {
            UserObject latest = new() { Name = "LatestTask", Bio = "B", Blog = "Bl" };

            IList<UserObject?>? results = null;
            _ = cache.GetAndFetchLatest(
                    "task_validate",
                    () => Task.FromResult(latest),
                    fetchPredicate: null,
                    absoluteExpiration: null,
                    shouldInvalidateOnError: false,
                    cacheValidationPredicate: static _ => false)
                .ToList()
                .Subscribe(v => results = v);

            await Assert.That(results).IsNotEmpty();

            // Since cacheValidationPredicate returned false, the cache should not contain the key.
            Exception? error = null;
            _ = cache.GetObject<UserObject>("task_validate").Subscribe(static _ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that DeserializeWithContext returns null for nullable DateTime when the
    /// primary serializer fails and the UniversalSerializer fallback also cannot
    /// deserialize the invalid data (returning default instead of throwing).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldFallbackForNullableDateTimeFailure()
    {
        var cache = CreateCache();
        try
        {
            byte[] invalid = [0xFF, 0xFE, 0xFD, 0x01];

            // The primary serializer throws on invalid data. DeserializeWithContext detects
            // DateTime? and routes to UniversalSerializer.Deserialize<DateTime?>, which
            // catches the primary failure and tries fallback. With no registered fallback
            // serializers and data too short for BSON/JSON detection, the fallback returns
            // default (null for DateTime?).
            var result = SerializerExtensions.DeserializeWithContext<DateTime?>(invalid, cache);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that DeserializeWithContext returns null for nullable DateTimeOffset when the
    /// primary serializer fails and the fallback cannot deserialize the invalid data.
    /// Unlike non-nullable DateTimeOffset, the nullable variant goes through the
    /// UniversalSerializer path which returns default (null) instead of throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldFallbackForNullableDateTimeOffsetFailure()
    {
        var cache = CreateCache();
        try
        {
            byte[] invalid = [0xFF, 0xFE, 0xFD, 0x01];

            // The primary serializer throws on invalid data. DeserializeWithContext detects
            // DateTimeOffset? and routes to UniversalSerializer.Deserialize<DateTimeOffset?>,
            // which catches the primary failure and tries fallback. With no registered fallback
            // serializers and data too short for BSON/JSON detection, the fallback returns
            // default (null for DateTimeOffset?).
            var result = SerializerExtensions.DeserializeWithContext<DateTimeOffset?>(invalid, cache);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies <see cref="SerializerExtensions.ShouldRefetchCachedValue"/> returns <c>true</c>
    /// when no fetch predicate is supplied — the helper short-circuits and the cached value
    /// is always considered stale.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldRefetchCachedValueShouldReturnTrueWhenPredicateIsNull()
    {
        var result = SerializerExtensions.ShouldRefetchCachedValue(null, TimeProvider.System.GetUtcNow());

        await Assert.That(result).IsTrue();
    }

    /// <summary>Verifies <see cref="SerializerExtensions.ShouldRefetchCachedValue"/> returns <c>true</c> when the cache has no creation timestamp, regardless of the predicate.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldRefetchCachedValueShouldReturnTrueWhenCreatedAtIsNull()
    {
        var result = SerializerExtensions.ShouldRefetchCachedValue(static _ => false, null);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Verifies <see cref="SerializerExtensions.ShouldRefetchCachedValue"/> defers to the
    /// predicate's verdict when both the predicate and timestamp are present and the
    /// predicate accepts the timestamp.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldRefetchCachedValueShouldHonourPredicateWhenItReturnsTrue()
    {
        var result = SerializerExtensions.ShouldRefetchCachedValue(static _ => true, TimeProvider.System.GetUtcNow());

        await Assert.That(result).IsTrue();
    }

    /// <summary>Verifies <see cref="SerializerExtensions.ShouldRefetchCachedValue"/> returns <c>false</c> when the predicate rejects the timestamp — the cached value is considered fresh.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldRefetchCachedValueShouldHonourPredicateWhenItReturnsFalse()
    {
        var result = SerializerExtensions.ShouldRefetchCachedValue(static _ => false, TimeProvider.System.GetUtcNow());

        await Assert.That(result).IsFalse();
    }

    /// <summary>Creates a new instance of an in-memory blob cache with the specified scheduler and serializer.</summary>
    /// <returns>A new instance of the in-memory blob cache.</returns>
    private static InMemoryBlobCache CreateCache() =>
        new(ImmediateScheduler.Instance, new SystemJsonSerializer());
}
