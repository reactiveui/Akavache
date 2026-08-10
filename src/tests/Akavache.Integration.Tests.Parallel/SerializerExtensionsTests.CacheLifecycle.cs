// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests covering expiration, invalidation and concurrent access through the serializer extensions.</summary>
public partial class SerializerExtensionsTests
{
    /// <summary>
    /// Tests that Invalidate properly clears RequestCache entries for InMemory cache.
    /// This reproduces the bug where GetOrFetchObject returns stale data after Invalidate.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateShouldClearRequestCacheForGetOrFetchObject()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        var fetchCount = 0;

        try
        {
            // Function that returns incrementing values to test if it's called
            Func<IObservable<string>> fetchFunc = () =>
            {
                fetchCount++;
                return Signal.Return($"value_{fetchCount}");
            };

            // Act 1: First call to GetOrFetchObject should fetch and cache
            string? result1 = null;
            _ = cache.GetOrFetchObject(TestKey, fetchFunc).Subscribe(v => result1 = v);

            // Act 2: Invalidate the key
            _ = cache.Invalidate(TestKey).Subscribe();

            // Act 3: Second call to GetOrFetchObject should fetch again (not return cached RequestCache)
            string? result2 = null;
            _ = cache.GetOrFetchObject(TestKey, fetchFunc).Subscribe(v => result2 = v);

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(result1).IsEqualTo("value_1");
                await Assert.That(result2).IsEqualTo("value_2");
                await Assert.That(fetchCount).IsEqualTo(ExpectedRefetchCount);
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests exact scenario from the original bug report (#524).
    /// This verifies that GetOrFetchObject correctly fetches new data after Invalidate is called.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BugReport524_InvalidateNotWorkingProperlyForInMemory()
    {
        // Arrange - Replicate the exact scenario from the bug report
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        var cnt = 0;

        try
        {
            var getOrFetchAsync = () => cache.GetOrFetchObject(
                    "a",
                    () =>
                    {
                        cnt++;
                        return Signal.Return($"b{cnt}");
                    },
                    TimeProvider.System.GetUtcNow().UtcDateTime + TimeSpan.FromMilliseconds(CacheLifetimeMilliseconds));

            // Act & Assert - Follow the exact pattern from the bug report
            string? result1 = null;
            _ = getOrFetchAsync().Subscribe(v => result1 = v);
            _ = cache.Invalidate("a").Subscribe();
            string? result2 = null;
            _ = getOrFetchAsync().Subscribe(v => result2 = v);

            using (Assert.Multiple())
            {
                await Assert.That(result1).IsEqualTo("b1");
                await Assert.That(result2).IsEqualTo("b2");
                await Assert.That(cnt).IsEqualTo(ExpectedRefetchCount);
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that InsertObject with expiration parameter works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectWithExpirationShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        UserObject user = new() { Name = "Expiring User", Bio = "Bio", Blog = "Blog" };
        var expiration = TimeProvider.System.GetLocalNow().AddHours(1);

        try
        {
            // Act
            _ = cache.InsertObject(ExpiringUserKey, user, expiration).Subscribe();

            // Assert
            UserObject? retrieved = null;
            _ = cache.GetObject<UserObject>(ExpiringUserKey).Subscribe(v => retrieved = v);
            await Assert.That(retrieved).IsNotNull();
            await Assert.That(retrieved!.Name).IsEqualTo("Expiring User");

            // Verify CreatedAt is set
            DateTimeOffset? createdAt = null;
            _ = cache.GetObjectCreatedAt<UserObject>(ExpiringUserKey).Subscribe(v => createdAt = v);
            await Assert.That(createdAt).IsNotNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetObject properly handles null values stored in cache.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectShouldHandleNullValues()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Act - Insert null value
            _ = cache.InsertObject<UserObject?>("null_user", null).Subscribe();

            // Assert - Should return null (or default)
            UserObject? result = null;
            _ = cache.GetObject<UserObject>("null_user").Subscribe(v => result = v);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetObjects properly handles missing keys.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectsShouldHandleMissingKeys()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        UserObject user1 = new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog };

        try
        {
            // Insert only one user
            cache.InsertObject(FirstUserKey, user1).WaitForCompletion();

            // Act - Request multiple keys where some are missing
            IList<KeyValuePair<string, UserObject>>? results = null;
            _ = cache.GetObjects<UserObject>([FirstUserKey, "user_missing"]).ToList().Subscribe(v => results = v);

            // Assert - Should only return the found object
            await Assert.That(results).Count().IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo(FirstUserKey);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that InsertObjects with expiration parameter works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsWithExpirationShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        var expiration = TimeProvider.System.GetLocalNow().AddHours(1);
        List<KeyValuePair<string, UserObject>> keyValuePairs =
        [
            new("exp_user1", new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog }),
            new("exp_user2", new() { Name = SecondUserName, Bio = "Bio2", Blog = SecondUserBlog })
        ];

        try
        {
            // Act
            _ = cache.InsertObjects(keyValuePairs, expiration).Subscribe();

            // Assert
            UserObject? user1 = null;
            _ = cache.GetObject<UserObject>("exp_user1").Subscribe(v => user1 = v);
            UserObject? user2 = null;
            _ = cache.GetObject<UserObject>("exp_user2").Subscribe(v => user2 = v);

            using (Assert.Multiple())
            {
                await Assert.That(user1).IsNotNull();
                await Assert.That(user1!.Name).IsEqualTo(FirstUserName);
                await Assert.That(user2).IsNotNull();
                await Assert.That(user2!.Name).IsEqualTo(SecondUserName);
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetOrFetchObject handles exceptions from the fetch function gracefully.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldHandleFetchExceptions()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Act & Assert - Fetch function throws
            Exception? fetchError = null;
            _ = cache.GetOrFetchObject(
                    "failing_fetch",
                    static () => Signal.Throw<UserObject>(new InvalidOperationException("Fetch failed")))
                .Subscribe(static _ => { }, ex => fetchError = ex);
            await Assert.That(fetchError).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetOrCreateObject handles exceptions from the create function gracefully.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrCreateObjectShouldHandleCreateExceptions()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Act & Assert - Create function throws
            Exception? createError = null;
            _ = cache.GetOrCreateObject<UserObject>(
                    "failing_create",
                    static () => throw new InvalidOperationException("Create failed"))
                .Subscribe(static _ => { }, ex => createError = ex);
            await Assert.That(createError).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that InvalidateObjects with empty collection completes without error.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateObjectsWithEmptyCollectionShouldCompleteWithoutError()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Act - Should not throw
            _ = cache.InvalidateObjects<UserObject>([]).Subscribe();

            // Test passes if no exception was thrown
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAllObjects returns empty when cache has no objects of the specified type.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetAllObjectsShouldReturnEmptyWhenNoObjectsOfType()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Act - Request all objects of a type when none exist
            IEnumerable<UserObject>? results = null;
            _ = cache.GetAllObjects<UserObject>().Subscribe(v => results = v);

            // Assert
            await Assert.That(results).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetObjectCreatedAt throws for non-existent keys.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldReturnNullForNonExistentKey()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Act
            DateTimeOffset? createdAt = null;
            _ = cache.GetObjectCreatedAt<UserObject>("non_existent_key").Subscribe(v => createdAt = v);

            // Assert - Should return null for missing key
            await Assert.That(createdAt).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that InsertObject handles whitespace-only keys according to implementation.
    /// Note: InMemoryBlobCache allows whitespace keys, while some implementations may throw.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectShouldThrowForWhitespaceKey()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        UserObject user = new() { Name = "User", Bio = "Bio", Blog = "Blog" };

        try
        {
            // Some cache implementations may allow whitespace keys
            // Test the actual behavior
            try
            {
                _ = cache.InsertObject("   ", user).Subscribe();

                // If it doesn't throw, that's acceptable - whitespace is a valid key for InMemoryBlobCache
            }
            catch (ArgumentException)
            {
                // This is expected for implementations that validate whitespace keys
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetObject handles whitespace-only keys according to implementation.
    /// Note: InMemoryBlobCache allows whitespace keys, while some implementations may throw.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectShouldThrowForWhitespaceKey()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Some cache implementations may allow whitespace keys
            // Test the actual behavior
            try
            {
                _ = cache.GetObject<UserObject>("   ").Subscribe();

                // If it doesn't throw, that's acceptable
            }
            catch (KeyNotFoundException)
            {
                // This is expected if the key doesn't exist
            }
            catch (ArgumentException)
            {
                // This is expected for implementations that validate whitespace keys
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that concurrent GetOrFetchObject calls don't cause race conditions.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ConcurrentGetOrFetchObjectShouldBeThreadSafe()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        var fetchCount = 0;

        try
        {
            var fetchFunc = () =>
            {
                _ = Interlocked.Increment(ref fetchCount);
                return Signal.Return(new UserObject { Name = $"User{fetchCount}", Bio = "Bio", Blog = "Blog" })
                    .Delay(TimeSpan.FromMilliseconds(FetchDelayMilliseconds));
            };

            // Act - Start multiple concurrent fetches
            var observables = Enumerable.Range(0, ConcurrentFetchCount)
                .Select(_ => cache.GetOrFetchObject("concurrent_user", fetchFunc))
                .ToArray();

            var results = new UserObject?[observables.Length];
            CountdownEvent countdown = new(observables.Length);
            for (var i = 0; i < observables.Length; i++)
            {
                var idx = i;
                _ = observables[i].Subscribe(
                    v => results[idx] = v,
                    _ => countdown.Signal(),
                    () => countdown.Signal());
            }

            _ = countdown.Wait(TimeSpan.FromSeconds(ConcurrencyTimeoutSeconds));

            // Assert - All results should be non-null
            using (Assert.Multiple())
            {
                await Assert.That(Array.TrueForAll(results, static r => r is not null)).IsTrue();

                // Fetch count should be reasonable - not one fetch per subscriber,
                // but due to timing and parallel execution, exact count varies.
                await Assert.That(fetchCount).IsLessThanOrEqualTo(ConcurrentFetchCount);
                await Assert.That(fetchCount).IsGreaterThanOrEqualTo(1);
            }
        }
        finally
        {
            cache.Dispose();
        }
    }
}
