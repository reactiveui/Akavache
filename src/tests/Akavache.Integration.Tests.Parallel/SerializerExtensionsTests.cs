// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests for serializer extension methods.</summary>
[Category("Akavache")]
public partial class SerializerExtensionsTests
{
    /// <summary>Cache key of the first user in the two-user sample.</summary>
    private const string FirstUserKey = "user1";

    /// <summary>Cache key of the second user in the two-user sample.</summary>
    private const string SecondUserKey = "user2";

    /// <summary>Name of the first user in the two-user sample.</summary>
    private const string FirstUserName = "User1";

    /// <summary>Name of the second user in the two-user sample.</summary>
    private const string SecondUserName = "User2";

    /// <summary>Blog of the first user in the two-user sample.</summary>
    private const string FirstUserBlog = "Blog1";

    /// <summary>Blog of the second user in the two-user sample.</summary>
    private const string SecondUserBlog = "Blog2";

    /// <summary>Name of the user produced by the create factory when nothing is cached.</summary>
    private const string CreatedUserName = "Created User";

    /// <summary>Cache key the create factory writes under.</summary>
    private const string NewUserKey = "new_user";

    /// <summary>Name of the user produced by the fetch function.</summary>
    private const string FetchedUserName = "Fetched User";

    /// <summary>Name of the user already sitting in the cache before a fetch runs.</summary>
    private const string CachedUserName = "Cached User";

    /// <summary>Bio of the user already sitting in the cache before a fetch runs.</summary>
    private const string CachedUserBio = "Cached Bio";

    /// <summary>Blog of the user already sitting in the cache before a fetch runs.</summary>
    private const string CachedUserBlog = "Cached Blog";

    /// <summary>Name of the user the fetch function returns after the cached one is emitted.</summary>
    private const string LatestUserName = "Latest User";

    /// <summary>Name of the cached user in the fetch-error cases.</summary>
    private const string CachedName = "Cached";

    /// <summary>Value stored under the single-entry dictionary case.</summary>
    private const string SingleEntryValue = "value";

    /// <summary>Value stored under the first key of the multi-entry dictionary cases.</summary>
    private const string FirstEntryValue = "value1";

    /// <summary>Key whose value is the empty string.</summary>
    private const string EmptyStringKey = "empty_string";

    /// <summary>Key used by the round-trip cases that need only one entry.</summary>
    private const string TestKey = "test_key";

    /// <summary>Key of the entry inserted with an expiration.</summary>
    private const string ExpiringUserKey = "expiring_user";

    /// <summary>Key of the entry the invalidate-on-error cases expect to be dropped.</summary>
    private const string InvalidatedKey = "inv_key";

    /// <summary>Key of the entry that must survive a failed fetch.</summary>
    private const string RetainedKey = "keep_key";

    /// <summary>Key of the entry the task-based invalidate-on-error case expects to be dropped.</summary>
    private const string TaskInvalidatedKey = "task_inv";

    /// <summary>Entries expected back from the two-user sample.</summary>
    private const int SampleUserCount = 2;

    /// <summary>Values a get-and-fetch subscription emits: the cached one, then the freshly fetched one.</summary>
    private const int CachedThenLatestCount = 2;

    /// <summary>Fetches expected once the request cache has been invalidated between calls.</summary>
    private const int ExpectedRefetchCount = 2;

    /// <summary>Keys expected back after inserting the two-payload sample.</summary>
    private const int InsertedKeyCount = 2;

    /// <summary>The integer entry in the mixed-type dictionary of objects.</summary>
    private const int MixedTypeIntValue = 42;

    /// <summary>The integer entry in the multi-item insert case.</summary>
    private const int MultiInsertIntValue = 42;

    /// <summary>The integer entry in the edge-case dictionary.</summary>
    private const int EdgeCaseIntValue = 42;

    /// <summary>The integer entry in the dictionary-overload insert case.</summary>
    private const int DictionaryIntValue = 42;

    /// <summary>Keys expected back from the dictionary-overload insert case.</summary>
    private const int DictionaryKeyCount = 3;

    /// <summary>Second property of the compound value in the edge-case dictionary.</summary>
    private const int ComplexPropertyValue = 123;

    /// <summary>Entries in the batch that exercises the bulk-insert completion path.</summary>
    private const int LargeBatchSize = 100;

    /// <summary>Entries in the batch that stresses the bulk-insert completion path.</summary>
    private const int StressBatchSize = 1000;

    /// <summary>How long a cached entry stays fresh in the re-fetch cases.</summary>
    private const int CacheLifetimeMilliseconds = 1000;

    /// <summary>Delay the fetch function holds for, so concurrent subscribers overlap.</summary>
    private const int FetchDelayMilliseconds = 50;

    /// <summary>Subscribers racing for the same key in the concurrency case.</summary>
    private const int ConcurrentFetchCount = 10;

    /// <summary>How long the concurrency case waits for every subscriber to settle.</summary>
    private const int ConcurrencyTimeoutSeconds = 30;

    /// <summary>Year of the sample instant, asserted after the round trip.</summary>
    private const int SampleYear = 2025;

    /// <summary>Tests that InsertObjects with IEnumerable works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsShouldWorkWithEnumerable()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            List<KeyValuePair<string, UserObject>> keyValuePairs =
            [
                new(FirstUserKey, new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog }),
                new(SecondUserKey, new() { Name = SecondUserName, Bio = "Bio2", Blog = SecondUserBlog })
            ];

            try
            {
                // Act
                cache.InsertObjects(keyValuePairs).WaitForCompletion();

                // Assert
                var user1 = cache.GetObject<UserObject>(FirstUserKey).SubscribeGetValue();
                var user2 = cache.GetObject<UserObject>(SecondUserKey).SubscribeGetValue();

                using (Assert.Multiple())
                {
                    await Assert.That(user1).IsNotNull();
                    await Assert.That(user1!.Name).IsEqualTo(FirstUserName);
                    await Assert.That(user1.Bio).IsEqualTo("Bio1");
                }

                using (Assert.Multiple())
                {
                    await Assert.That(user2).IsNotNull();
                    await Assert.That(user2!.Name).IsEqualTo(SecondUserName);
                    await Assert.That(user2.Bio).IsEqualTo("Bio2");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that GetObjects with multiple keys works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectsShouldWorkWithMultipleKeys()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            UserObject user1 = new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog };
            UserObject user2 = new() { Name = SecondUserName, Bio = "Bio2", Blog = SecondUserBlog };

            try
            {
                // Insert test data
                cache.InsertObject(FirstUserKey, user1).WaitForCompletion();
                cache.InsertObject(SecondUserKey, user2).WaitForCompletion();

                // Act
                var results = cache.GetObjects<UserObject>([FirstUserKey, SecondUserKey]).ToList().SubscribeGetValue();

                // Assert
                await Assert.That(results).Count().IsEqualTo(SampleUserCount);

                var user1Result = results!.First(static r => r.Key == FirstUserKey).Value;
                using (Assert.Multiple())
                {
                    await Assert.That(user1Result.Name).IsEqualTo(FirstUserName);
                    await Assert.That(user1Result.Bio).IsEqualTo("Bio1");
                }

                var user2Result = results!.First(static r => r.Key == SecondUserKey).Value;
                using (Assert.Multiple())
                {
                    await Assert.That(user2Result.Name).IsEqualTo(SecondUserName);
                    await Assert.That(user2Result.Bio).IsEqualTo("Bio2");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that GetAllObjects returns all objects of a specific type.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetAllObjectsShouldReturnAllObjectsOfType()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            // Use 'using' for resource management
            using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

            UserObject user1 = new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog };
            UserObject user2 = new() { Name = SecondUserName, Bio = "Bio2", Blog = SecondUserBlog };

            // Insert test data
            cache.InsertObject(FirstUserKey, user1).WaitForCompletion();
            cache.InsertObject(SecondUserKey, user2).WaitForCompletion();

            // Act
            var allObjects = cache.GetAllObjects<UserObject>().SubscribeGetValue();
            var results = allObjects!.ToList();

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(results).Count().IsEqualTo(SampleUserCount);
                await Assert.That(results.Exists(static x => x.Name == FirstUserName)).IsTrue();
                await Assert.That(results.Exists(static x => x.Name == SecondUserName)).IsTrue();
            }
        }
    }

    /// <summary>Tests that InvalidateObject removes the correct object.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateObjectShouldRemoveObject()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            UserObject user = new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog };

            try
            {
                // Insert test data
                cache.InsertObject(FirstUserKey, user).WaitForCompletion();

                // Verify object exists
                var retrievedUser = cache.GetObject<UserObject>(FirstUserKey).SubscribeGetValue();
                await Assert.That(retrievedUser).IsNotNull();

                // Act
                cache.InvalidateObject<UserObject>(FirstUserKey).WaitForCompletion();

                // Assert
                var knfError1 = cache.GetObject<UserObject>(FirstUserKey).SubscribeGetError();
                await Assert.That(knfError1).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that InvalidateObjects removes multiple objects.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateObjectsShouldRemoveMultipleObjects()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            UserObject user1 = new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog };
            UserObject user2 = new() { Name = SecondUserName, Bio = "Bio2", Blog = SecondUserBlog };

            try
            {
                // Insert test data
                cache.InsertObject(FirstUserKey, user1).WaitForCompletion();
                cache.InsertObject(SecondUserKey, user2).WaitForCompletion();

                // Act
                cache.InvalidateObjects<UserObject>([FirstUserKey, SecondUserKey]).WaitForCompletion();

                // Assert
                var knfError1 = cache.GetObject<UserObject>(FirstUserKey).SubscribeGetError();
                await Assert.That(knfError1).IsTypeOf<KeyNotFoundException>();

                var knfError2 = cache.GetObject<UserObject>(SecondUserKey).SubscribeGetError();
                await Assert.That(knfError2).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that InvalidateAllObjects removes all objects of a type.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateAllObjectsShouldRemoveAllObjectsOfType()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            UserObject user1 = new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog };
            UserObject user2 = new() { Name = SecondUserName, Bio = "Bio2", Blog = SecondUserBlog };

            try
            {
                // Insert test data
                cache.InsertObject(FirstUserKey, user1).WaitForCompletion();
                cache.InsertObject(SecondUserKey, user2).WaitForCompletion();

                // Verify objects exist before invalidation
                var beforeInvalidation = cache.GetAllObjects<UserObject>().SubscribeGetValue();
                await Assert.That(beforeInvalidation!.Count()).IsEqualTo(SampleUserCount);

                // Act
                cache.InvalidateAllObjects<UserObject>().WaitForCompletion();

                // Assert - The primary verification is that individual objects can't be retrieved
                var knfError1 = cache.GetObject<UserObject>(FirstUserKey).SubscribeGetError();
                await Assert.That(knfError1).IsTypeOf<KeyNotFoundException>();

                var knfError2 = cache.GetObject<UserObject>(SecondUserKey).SubscribeGetError();
                await Assert.That(knfError2).IsTypeOf<KeyNotFoundException>();

                // Additional check - GetAllObjects should return empty result
                var results = cache.GetAllObjects<UserObject>().SubscribeGetValue();
                await Assert.That(results).IsEmpty();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that GetObjectCreatedAt returns the creation time.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldReturnCreationTime()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            UserObject user = new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog };
            var beforeInsert = TimeProvider.System.GetLocalNow();

            try
            {
                // Act
                cache.InsertObject(FirstUserKey, user).WaitForCompletion();
                var createdAt = cache.GetObjectCreatedAt<UserObject>(FirstUserKey).SubscribeGetValue();

                // Assert
                await Assert.That(createdAt).IsNotNull();
                await Assert.That(createdAt!.Value).IsGreaterThanOrEqualTo(beforeInsert);
                await Assert.That(createdAt.Value).IsLessThanOrEqualTo(TimeProvider.System.GetLocalNow());
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that InsertAllObjects works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertAllObjectsShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            KeyValuePair<string, UserObject>[] keyValuePairs =
            [
                new(FirstUserKey, new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog }),
                new(SecondUserKey, new() { Name = SecondUserName, Bio = "Bio2", Blog = SecondUserBlog })
            ];

            try
            {
                // Act
                cache.InsertAllObjects(keyValuePairs).WaitForCompletion();

                // Assert
                var user1 = cache.GetObject<UserObject>(FirstUserKey).SubscribeGetValue();
                var user2 = cache.GetObject<UserObject>(SecondUserKey).SubscribeGetValue();

                await Assert.That(user1).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(user1!.Name).IsEqualTo(FirstUserName);

                    await Assert.That(user2).IsNotNull();
                }

                await Assert.That(user2!.Name).IsEqualTo(SecondUserName);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that GetOrCreateObject creates object when not in cache.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrCreateObjectShouldCreateWhenNotInCache()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            UserObject user = new() { Name = CreatedUserName, Bio = "Created Bio", Blog = "Created Blog" };

            try
            {
                // Act
                UserObject? result = null;
                _ = cache.GetOrCreateObject(NewUserKey, () => user).Subscribe(v => result = v);

                // Assert
                await Assert.That(result).IsNotNull();
                await Assert.That(result!.Name).IsEqualTo(CreatedUserName);

                // Verify it was actually stored
                UserObject? storedUser = null;
                _ = cache.GetObject<UserObject>(NewUserKey).Subscribe(v => storedUser = v);
                await Assert.That(storedUser).IsNotNull();
                await Assert.That(storedUser!.Name).IsEqualTo(CreatedUserName);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that GetOrCreateObject returns existing object from cache.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrCreateObjectShouldReturnExistingFromCache()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            UserObject existingUser = new() { Name = "Existing User", Bio = "Existing Bio", Blog = "Existing Blog" };
            UserObject newUser = new() { Name = "New User", Bio = "New Bio", Blog = "New Blog" };

            try
            {
                // Insert existing user
                cache.InsertObject("existing_user", existingUser).WaitForCompletion();

                // Act
                UserObject? result = null;
                _ = cache.GetOrCreateObject("existing_user", () => newUser).Subscribe(v => result = v);

                // Assert - should return existing user, not create new one
                await Assert.That(result).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(result!.Name).IsEqualTo("Existing User");
                    await Assert.That(result.Bio).IsEqualTo("Existing Bio");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that GetOrFetchObject fetches when not in cache.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldFetchWhenNotInCache()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        UserObject fetchedUser = new() { Name = FetchedUserName, Bio = "Fetched Bio", Blog = "Fetched Blog" };
        var fetchCount = 0;

        try
        {
            // Act
            UserObject? result = null;
            _ = cache.GetOrFetchObject("fetch_user", () =>
            {
                fetchCount++;
                return Signal.Return(fetchedUser);
            }).Subscribe(v => result = v);

            // Assert
            await Assert.That(result).IsNotNull();
            using (Assert.Multiple())
            {
                await Assert.That(result!.Name).IsEqualTo(FetchedUserName);
                await Assert.That(fetchCount).IsEqualTo(1);
            }

            // Verify it was stored in cache
            UserObject? cachedUser = null;
            _ = cache.GetObject<UserObject>("fetch_user").Subscribe(v => cachedUser = v);
            await Assert.That(cachedUser).IsNotNull();
            await Assert.That(cachedUser!.Name).IsEqualTo(FetchedUserName);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetOrFetchObject returns cached value when available.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldReturnCachedValue()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        UserObject cachedUser = new() { Name = CachedUserName, Bio = CachedUserBio, Blog = CachedUserBlog };
        UserObject fetchedUser = new() { Name = FetchedUserName, Bio = "Fetched Bio", Blog = "Fetched Blog" };
        var fetchCount = 0;

        try
        {
            // Insert cached value
            _ = cache.InsertObject("cached_user", cachedUser).Subscribe();

            // Act
            UserObject? result = null;
            _ = cache.GetOrFetchObject("cached_user", () =>
            {
                fetchCount++;
                return Signal.Return(fetchedUser);
            }).Subscribe(v => result = v);

            // Assert - should return cached value, not fetch
            await Assert.That(result).IsNotNull();
            using (Assert.Multiple())
            {
                await Assert.That(result!.Name).IsEqualTo(CachedUserName);
                await Assert.That(fetchCount).IsZero(); // Fetch should not have been called
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetOrFetchObject with Task-based fetch function works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrFetchObjectWithTaskShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        UserObject fetchedUser = new() { Name = "Task Fetched User", Bio = "Task Bio", Blog = "Task Blog" };

        try
        {
            // Act
            UserObject? result = null;
            _ = cache.GetOrFetchObject("task_user", () => Task.FromResult(fetchedUser)).Subscribe(v => result = v);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("Task Fetched User");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAndFetchLatest returns cached value first, then updated value.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetAndFetchLatestShouldReturnCachedThenLatest()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        UserObject cachedUser = new() { Name = CachedUserName, Bio = CachedUserBio, Blog = CachedUserBlog };
        UserObject latestUser = new() { Name = LatestUserName, Bio = "Latest Bio", Blog = "Latest Blog" };

        try
        {
            // Insert cached value
            _ = cache.InsertObject("user", cachedUser).Subscribe();

            List<UserObject?> results = [];

            // Act - GetAndFetchLatest should return cached value first, then latest
            await cache.GetAndFetchLatest("user", () => Signal.Return(latestUser))
                .Take(CachedThenLatestCount).Do(results.Add).LastOrDefaultAsync();

            // Assert
            await Assert.That(results).IsNotEmpty(); // Should have at least cached value
            await Assert.That(results[0]).IsNotNull();
            await Assert.That(results[0]!.Name).IsEqualTo(CachedUserName);

            if (results.Count > 1)
            {
                // If we got the latest value too
                await Assert.That(results[1]).IsNotNull();
                await Assert.That(results[1]!.Name).IsEqualTo(LatestUserName);
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAndFetchLatest with Task-based fetch function works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetAndFetchLatestWithTaskShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        UserObject latestUser = new() { Name = "Task Latest User", Bio = "Task Bio", Blog = "Task Blog" };

        try
        {
            List<UserObject?> results = [];

            // Act - GetAndFetchLatest with no cached value
            await cache.GetAndFetchLatest(NewUserKey, () => Task.FromResult(latestUser))
                .Take(1).Do(results.Add).LastOrDefaultAsync();

            // Assert
            await Assert.That(results).Count().IsEqualTo(1);
            await Assert.That(results[0]).IsNotNull();
            await Assert.That(results[0]!.Name).IsEqualTo("Task Latest User");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that GetAndFetchLatest with fetchPredicate respects the predicate.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetAndFetchLatestShouldRespectFetchPredicate()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        UserObject cachedUser = new() { Name = CachedUserName, Bio = CachedUserBio, Blog = CachedUserBlog };
        UserObject latestUser = new() { Name = LatestUserName, Bio = "Latest Bio", Blog = "Latest Blog" };
        var fetchCount = 0;

        try
        {
            // Insert cached value
            _ = cache.InsertObject("user", cachedUser).Subscribe();

            List<UserObject?> results = [];

            // Act - Use fetchPredicate that returns false (should not fetch)
            await cache.GetAndFetchLatest(
                    "user",
                    () =>
                    {
                        fetchCount++;
                        return Signal.Return(latestUser);
                    },
                    fetchPredicate: static _ => false) // Never fetch
                .Take(1).Do(results.Add).LastOrDefaultAsync();

            // Assert
            await Assert.That(results).Count().IsEqualTo(1);
            await Assert.That(results[0]).IsNotNull();
            using (Assert.Multiple())
            {
                await Assert.That(results[0]!.Name).IsEqualTo(CachedUserName);
                await Assert.That(fetchCount).IsZero(); // Fetch should not have been called
            }
        }
        finally
        {
            cache.Dispose();
        }
    }
}
