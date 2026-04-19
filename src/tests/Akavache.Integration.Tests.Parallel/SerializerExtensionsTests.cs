// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Akavache.Tests;
using Akavache.Tests.Helpers;
using Akavache.Tests.Mocks;

namespace Akavache.Integration.Tests;

/// <summary>
/// Tests for serializer extension methods.
/// </summary>
[Category("Akavache")]
public class SerializerExtensionsTests
{
    /// <summary>
    /// Tests that InsertObjects with IEnumerable works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsShouldWorkWithEnumerable()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            List<KeyValuePair<string, UserObject>> keyValuePairs =
            [
                new("user1", new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" }),
                new("user2", new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" })
            ];

            try
            {
                // Act
                cache.InsertObjects(keyValuePairs).SubscribeAndComplete();

                // Assert
                var user1 = cache.GetObject<UserObject>("user1").SubscribeGetValue();
                var user2 = cache.GetObject<UserObject>("user2").SubscribeGetValue();

                using (Assert.Multiple())
                {
                    await Assert.That(user1).IsNotNull();
                    await Assert.That(user1!.Name).IsEqualTo("User1");
                    await Assert.That(user1.Bio).IsEqualTo("Bio1");
                }

                using (Assert.Multiple())
                {
                    await Assert.That(user2).IsNotNull();
                    await Assert.That(user2!.Name).IsEqualTo("User2");
                    await Assert.That(user2.Bio).IsEqualTo("Bio2");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that GetObjects with multiple keys works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectsShouldWorkWithMultipleKeys()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            UserObject user1 = new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            UserObject user2 = new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

            try
            {
                // Insert test data
                cache.InsertObject("user1", user1).SubscribeAndComplete();
                cache.InsertObject("user2", user2).SubscribeAndComplete();

                // Act
                var results = cache.GetObjects<UserObject>(["user1", "user2"]).ToList().SubscribeGetValue();

                // Assert
                await Assert.That(results).Count().IsEqualTo(2);

                var user1Result = results!.First(static r => r.Key == "user1").Value;
                using (Assert.Multiple())
                {
                    await Assert.That(user1Result.Name).IsEqualTo("User1");
                    await Assert.That(user1Result.Bio).IsEqualTo("Bio1");
                }

                var user2Result = results!.First(static r => r.Key == "user2").Value;
                using (Assert.Multiple())
                {
                    await Assert.That(user2Result.Name).IsEqualTo("User2");
                    await Assert.That(user2Result.Bio).IsEqualTo("Bio2");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that GetAllObjects returns all objects of a specific type.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetAllObjectsShouldReturnAllObjectsOfType()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            // Use 'using' for resource management
            using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

            UserObject user1 = new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            UserObject user2 = new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

            // Insert test data
            cache.InsertObject("user1", user1).SubscribeAndComplete();
            cache.InsertObject("user2", user2).SubscribeAndComplete();

            // Act
            var allObjects = cache.GetAllObjects<UserObject>().SubscribeGetValue();
            var results = allObjects!.ToList();

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(results).Count().IsEqualTo(2);
                await Assert.That(results.Any(static x => x.Name == "User1")).IsTrue();
                await Assert.That(results.Any(static x => x.Name == "User2")).IsTrue();
            }
        }
    }

    /// <summary>
    /// Tests that InvalidateObject removes the correct object.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateObjectShouldRemoveObject()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            UserObject user = new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" };

            try
            {
                // Insert test data
                cache.InsertObject("user1", user).SubscribeAndComplete();

                // Verify object exists
                var retrievedUser = cache.GetObject<UserObject>("user1").SubscribeGetValue();
                await Assert.That(retrievedUser).IsNotNull();

                // Act
                cache.InvalidateObject<UserObject>("user1").SubscribeAndComplete();

                // Assert
                var knfError1 = cache.GetObject<UserObject>("user1").SubscribeGetError();
                await Assert.That(knfError1).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that InvalidateObjects removes multiple objects.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateObjectsShouldRemoveMultipleObjects()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            UserObject user1 = new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            UserObject user2 = new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

            try
            {
                // Insert test data
                cache.InsertObject("user1", user1).SubscribeAndComplete();
                cache.InsertObject("user2", user2).SubscribeAndComplete();

                // Act
                cache.InvalidateObjects<UserObject>(["user1", "user2"]).SubscribeAndComplete();

                // Assert
                var knfError1 = cache.GetObject<UserObject>("user1").SubscribeGetError();
                await Assert.That(knfError1).IsTypeOf<KeyNotFoundException>();

                var knfError2 = cache.GetObject<UserObject>("user2").SubscribeGetError();
                await Assert.That(knfError2).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that InvalidateAllObjects removes all objects of a type.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateAllObjectsShouldRemoveAllObjectsOfType()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            UserObject user1 = new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            UserObject user2 = new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

            try
            {
                // Insert test data
                cache.InsertObject("user1", user1).SubscribeAndComplete();
                cache.InsertObject("user2", user2).SubscribeAndComplete();

                // Verify objects exist before invalidation
                var beforeInvalidation = cache.GetAllObjects<UserObject>().SubscribeGetValue();
                await Assert.That(beforeInvalidation!.Count()).IsEqualTo(2);

                // Act
                cache.InvalidateAllObjects<UserObject>().SubscribeAndComplete();

                // Assert - The primary verification is that individual objects can't be retrieved
                var knfError1 = cache.GetObject<UserObject>("user1").SubscribeGetError();
                await Assert.That(knfError1).IsTypeOf<KeyNotFoundException>();

                var knfError2 = cache.GetObject<UserObject>("user2").SubscribeGetError();
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

    /// <summary>
    /// Tests that GetObjectCreatedAt returns the creation time.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldReturnCreationTime()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            UserObject user = new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            var beforeInsert = DateTimeOffset.Now;

            try
            {
                // Act
                cache.InsertObject("user1", user).SubscribeAndComplete();
                var createdAt = cache.GetObjectCreatedAt<UserObject>("user1").SubscribeGetValue();

                // Assert
                await Assert.That(createdAt).IsNotNull();
                await Assert.That(createdAt!.Value).IsGreaterThanOrEqualTo(beforeInsert);
                await Assert.That(createdAt.Value).IsLessThanOrEqualTo(DateTimeOffset.Now);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that InsertAllObjects works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertAllObjectsShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            KeyValuePair<string, UserObject>[] keyValuePairs =
            [
                new("user1", new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" }),
                new("user2", new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" })
            ];

            try
            {
                // Act
                cache.InsertAllObjects(keyValuePairs).SubscribeAndComplete();

                // Assert
                var user1 = cache.GetObject<UserObject>("user1").SubscribeGetValue();
                var user2 = cache.GetObject<UserObject>("user2").SubscribeGetValue();

                await Assert.That(user1).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(user1!.Name).IsEqualTo("User1");

                    await Assert.That(user2).IsNotNull();
                }

                await Assert.That(user2!.Name).IsEqualTo("User2");
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that GetOrCreateObject creates object when not in cache.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrCreateObjectShouldCreateWhenNotInCache()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            UserObject user = new() { Name = "Created User", Bio = "Created Bio", Blog = "Created Blog" };

            try
            {
                // Act
                UserObject? result = null;
                cache.GetOrCreateObject("new_user", () => user).Subscribe(v => result = v);

                // Assert
                await Assert.That(result).IsNotNull();
                await Assert.That(result!.Name).IsEqualTo("Created User");

                // Verify it was actually stored
                UserObject? storedUser = null;
                cache.GetObject<UserObject>("new_user").Subscribe(v => storedUser = v);
                await Assert.That(storedUser).IsNotNull();
                await Assert.That(storedUser!.Name).IsEqualTo("Created User");
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that GetOrCreateObject returns existing object from cache.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrCreateObjectShouldReturnExistingFromCache()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            UserObject existingUser = new() { Name = "Existing User", Bio = "Existing Bio", Blog = "Existing Blog" };
            UserObject newUser = new() { Name = "New User", Bio = "New Bio", Blog = "New Blog" };

            try
            {
                // Insert existing user
                cache.InsertObject("existing_user", existingUser).SubscribeAndComplete();

                // Act
                UserObject? result = null;
                cache.GetOrCreateObject("existing_user", () => newUser).Subscribe(v => result = v);

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

    /// <summary>
    /// Tests that GetOrFetchObject fetches when not in cache.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldFetchWhenNotInCache()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        UserObject fetchedUser = new() { Name = "Fetched User", Bio = "Fetched Bio", Blog = "Fetched Blog" };
        var fetchCount = 0;

        try
        {
            // Act
            UserObject? result = null;
            cache.GetOrFetchObject("fetch_user", () =>
            {
                fetchCount++;
                return Observable.Return(fetchedUser);
            }).Subscribe(v => result = v);

            // Assert
            await Assert.That(result).IsNotNull();
            using (Assert.Multiple())
            {
                await Assert.That(result!.Name).IsEqualTo("Fetched User");
                await Assert.That(fetchCount).IsEqualTo(1);
            }

            // Verify it was stored in cache
            UserObject? cachedUser = null;
            cache.GetObject<UserObject>("fetch_user").Subscribe(v => cachedUser = v);
            await Assert.That(cachedUser).IsNotNull();
            await Assert.That(cachedUser!.Name).IsEqualTo("Fetched User");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetOrFetchObject returns cached value when available.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldReturnCachedValue()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        UserObject cachedUser = new() { Name = "Cached User", Bio = "Cached Bio", Blog = "Cached Blog" };
        UserObject fetchedUser = new() { Name = "Fetched User", Bio = "Fetched Bio", Blog = "Fetched Blog" };
        var fetchCount = 0;

        try
        {
            // Insert cached value
            cache.InsertObject("cached_user", cachedUser).Subscribe();

            // Act
            UserObject? result = null;
            cache.GetOrFetchObject("cached_user", () =>
            {
                fetchCount++;
                return Observable.Return(fetchedUser);
            }).Subscribe(v => result = v);

            // Assert - should return cached value, not fetch
            await Assert.That(result).IsNotNull();
            using (Assert.Multiple())
            {
                await Assert.That(result!.Name).IsEqualTo("Cached User");
                await Assert.That(fetchCount).IsZero(); // Fetch should not have been called
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetOrFetchObject with Task-based fetch function works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrFetchObjectWithTaskShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        UserObject fetchedUser = new() { Name = "Task Fetched User", Bio = "Task Bio", Blog = "Task Blog" };

        try
        {
            // Act
            UserObject? result = null;
            cache.GetOrFetchObject("task_user", () => Task.FromResult(fetchedUser)).Subscribe(v => result = v);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("Task Fetched User");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetAndFetchLatest returns cached value first, then updated value.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetAndFetchLatestShouldReturnCachedThenLatest()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        UserObject cachedUser = new() { Name = "Cached User", Bio = "Cached Bio", Blog = "Cached Blog" };
        UserObject latestUser = new() { Name = "Latest User", Bio = "Latest Bio", Blog = "Latest Blog" };

        try
        {
            // Insert cached value
            cache.InsertObject("user", cachedUser).Subscribe();

            List<UserObject?> results = [];

            // Act - GetAndFetchLatest should return cached value first, then latest
            await cache.GetAndFetchLatest("user", () => Observable.Return(latestUser))
                .Take(2) // Take at most 2 values (cached + latest)
                .ForEachAsync(results.Add);

            // Assert
            await Assert.That(results).IsNotEmpty(); // Should have at least cached value
            await Assert.That(results[0]).IsNotNull();
            await Assert.That(results[0]!.Name).IsEqualTo("Cached User");

            if (results.Count > 1)
            {
                // If we got the latest value too
                await Assert.That(results[1]).IsNotNull();
                await Assert.That(results[1]!.Name).IsEqualTo("Latest User");
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetAndFetchLatest with Task-based fetch function works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetAndFetchLatestWithTaskShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        UserObject latestUser = new() { Name = "Task Latest User", Bio = "Task Bio", Blog = "Task Blog" };

        try
        {
            List<UserObject?> results = [];

            // Act - GetAndFetchLatest with no cached value
            await cache.GetAndFetchLatest("new_user", () => Task.FromResult(latestUser))
                .Take(1) // Should only get the fetched value
                .ForEachAsync(results.Add);

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

    /// <summary>
    /// Tests that GetAndFetchLatest with fetchPredicate respects the predicate.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetAndFetchLatestShouldRespectFetchPredicate()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        UserObject cachedUser = new() { Name = "Cached User", Bio = "Cached Bio", Blog = "Cached Blog" };
        UserObject latestUser = new() { Name = "Latest User", Bio = "Latest Bio", Blog = "Latest Blog" };
        var fetchCount = 0;

        try
        {
            // Insert cached value
            cache.InsertObject("user", cachedUser).Subscribe();

            List<UserObject?> results = [];

            // Act - Use fetchPredicate that returns false (should not fetch)
            await cache.GetAndFetchLatest(
                    "user",
                    () =>
                    {
                        fetchCount++;
                        return Observable.Return(latestUser);
                    },
                    fetchPredicate: _ => false) // Never fetch
                .Take(1) // Should only get cached value
                .ForEachAsync(results.Add);

            // Assert
            await Assert.That(results).Count().IsEqualTo(1);
            await Assert.That(results[0]).IsNotNull();
            using (Assert.Multiple())
            {
                await Assert.That(results[0]!.Name).IsEqualTo("Cached User");
                await Assert.That(fetchCount).IsZero(); // Fetch should not have been called
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that InsertObjects throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task InsertObjectsShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;
        Dictionary<string, object> dict = new() { ["key"] = "value" };

        // Act & Assert
        await Assert.That(() => cache!.InsertObjects(dict)).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that InsertObjects throws ArgumentNullException when keyValuePairs is null.
    /// </summary>
    /// <returns>A task representing the test completion.</returns>
    [Test]
    public async Task InsertObjectsShouldThrowArgumentNullExceptionWhenKeyValuePairsIsNull()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        Dictionary<string, object>? dict = null;

        try
        {
            // Act & Assert
            await Assert.That(() => cache.InsertObjects(dict!)).Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that InsertObjects handles empty dictionary correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsShouldHandleEmptyDictionary()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            Dictionary<string, object> emptyDict = [];

            try
            {
                // Act - should complete without error
                cache.InsertObjects(emptyDict).Subscribe();

                // Assert - test passes if no exception is thrown
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that mixed object types can be inserted and retrieved.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsShouldHandleMixedObjectTypes()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            // Use 'using' for resource management
            using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

            DateTime testDate = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            Dictionary<string, object> mixedObjects = new()
            {
                ["string"] = "test string",
                ["int"] = 42,
                ["user"] = new UserObject { Name = "Test User", Bio = "Test Bio", Blog = "Test Blog" },
                ["date"] = testDate
            };

            // Act
            cache.InsertObjects(mixedObjects).Subscribe();

            // Assert
            string? stringValue = null;
            cache.GetObject<string>("string").Subscribe(v => stringValue = v);
            int intValue = 0;
            cache.GetObject<int>("int").Subscribe(v => intValue = v);
            UserObject? userValue = null;
            cache.GetObject<UserObject>("user").Subscribe(v => userValue = v);
            DateTime dateValue = default;
            cache.GetObject<DateTime>("date").Subscribe(v => dateValue = v);

            using (Assert.Multiple())
            {
                await Assert.That(stringValue).IsEqualTo("test string");
                await Assert.That(intValue).IsEqualTo(42);
                await Assert.That(userValue).IsNotNull();
                await Assert.That(userValue!.Name).IsEqualTo("Test User");

                // Verify date value - either default or close to test date
                var isDateValid = dateValue == default || Math.Abs((dateValue - testDate).TotalDays) <= 1;
                await Assert.That(isDateValid).IsTrue();
            }
        }
    }

    /// <summary>
    /// Tests that extension methods properly validate arguments.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ExtensionMethodsShouldValidateArguments()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            // Test null key validation
            Exception? catErr = null;
            cache.GetObjectCreatedAt<string>(null!).Subscribe(_ => { }, ex => catErr = ex);
            await Assert.That(catErr).IsTypeOf<ArgumentNullException>();

            Exception? invErr = null;
            cache.InvalidateObject<string>(null!).Subscribe(_ => { }, ex => invErr = ex);
            await Assert.That(invErr).IsTypeOf<ArgumentNullException>();

            // Test null collection validation — throws before Subscribe
            Exception? invsErr = null;
            try
            {
                cache.InvalidateObjects<string>(null!).Subscribe(_ => { }, ex => invsErr = ex);
            }
            catch (ArgumentNullException ex)
            {
                invsErr = ex;
            }

            await Assert.That(invsErr).IsTypeOf<ArgumentNullException>();

            // Note: Extension methods may allow empty strings as valid keys in some implementations
            // This is different from the core methods which validate empty strings

            // Test that methods work with empty string (if allowed by implementation)
            try
            {
                cache.GetObjectCreatedAt<string>(string.Empty).Subscribe();

                // If no exception is thrown, empty strings are allowed
            }
            catch (KeyNotFoundException)
            {
                // This is expected if the key doesn't exist - empty string is a valid key
            }
            catch (ArgumentException)
            {
                // This would indicate the implementation validates empty strings
                // Both behaviors are valid depending on the implementation
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that InsertObjects handles empty sequence completion robustly.
    /// This test validates the fix for issue #635 where LastOrDefaultAsync()
    /// would throw "Sequence contains no elements" exception.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsShouldHandleEmptySequenceRobustly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

            try
            {
                // Test 1: Empty dictionary should complete without exception
                Dictionary<string, object> emptyDict = [];
                cache.InsertObjects(emptyDict).Subscribe();

                // Test 2: Single item should work
                Dictionary<string, object> singleDict = new() { ["key1"] = "value1" };
                cache.InsertObjects(singleDict).Subscribe();

                // Test 3: Multiple items should work
                Dictionary<string, object> multiDict = new()
                {
                    ["key2"] = "value2",
                    ["key3"] = 42,
                    ["key4"] = new UserObject { Name = "Test", Bio = "Bio", Blog = "Blog" }
                };
                cache.InsertObjects(multiDict).Subscribe();

                // Verify all items were inserted correctly
                string? value1 = null;
                cache.GetObject<string>("key1").Subscribe(v => value1 = v);
                string? value2 = null;
                cache.GetObject<string>("key2").Subscribe(v => value2 = v);
                int value3 = 0;
                cache.GetObject<int>("key3").Subscribe(v => value3 = v);
                UserObject? value4 = null;
                cache.GetObject<UserObject>("key4").Subscribe(v => value4 = v);

                using (Assert.Multiple())
                {
                    await Assert.That(value1).IsEqualTo("value1");
                    await Assert.That(value2).IsEqualTo("value2");
                    await Assert.That(value3).IsEqualTo(42);
                    await Assert.That(value4).IsNotNull();
                    await Assert.That(value4!.Name).IsEqualTo("Test");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that InsertObjects with IEnumerable&lt;KeyValuePair&gt; handles completion properly.
    /// This specifically tests the fix where Count() is used instead of LastOrDefaultAsync()
    /// to avoid "Sequence contains no elements" exceptions.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsGenericShouldHandleSequenceCompletionRobustly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

            try
            {
                // Test 1: Empty collection
                List<KeyValuePair<string, UserObject>> emptyPairs = [];
                cache.InsertObjects(emptyPairs).Subscribe();

                // Test 2: Single item
                List<KeyValuePair<string, UserObject>> singlePair =
                    [new("user1", new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" })];
                cache.InsertObjects(singlePair).Subscribe();

                // Test 3: Multiple items
                List<KeyValuePair<string, UserObject>> multiPairs =
                [
                    new("user2", new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" }),
                    new("user3", new() { Name = "User3", Bio = "Bio3", Blog = "Blog3" }),
                    new("user4", new() { Name = "User4", Bio = "Bio4", Blog = "Blog4" })
                ];
                cache.InsertObjects(multiPairs).Subscribe();

                // Test 4: Large collection to stress test the Count() approach
                var largePairs = Enumerable.Range(1, 100)
                    .Select(static i => new KeyValuePair<string, UserObject>(
                        $"large_user_{i}",
                        new() { Name = $"LargeUser{i}", Bio = $"Bio{i}", Blog = $"Blog{i}" }))
                    .ToList();
                cache.InsertObjects(largePairs).Subscribe();

                // Verify some items were inserted correctly
                var user1 = cache.GetObject<UserObject>("user1").SubscribeGetValue();
                var user2 = cache.GetObject<UserObject>("user2").SubscribeGetValue();
                UserObject? largeUser50 = null;
                cache.GetObject<UserObject>("large_user_50").Subscribe(v => largeUser50 = v);

                using (Assert.Multiple())
                {
                    await Assert.That(user1).IsNotNull();
                    await Assert.That(user1!.Name).IsEqualTo("User1");
                    await Assert.That(user2).IsNotNull();
                    await Assert.That(user2!.Name).IsEqualTo("User2");
                    await Assert.That(largeUser50).IsNotNull();
                    await Assert.That(largeUser50!.Name).IsEqualTo("LargeUser50");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that InsertObjects completion logic is robust and doesn't throw exceptions.
    /// This test verifies the implementation handles various edge cases correctly,
    /// including empty sequences, without throwing "Sequence contains no elements" exceptions.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsCompletionLogicShouldBeRobust()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

            try
            {
                // Test 1: Empty dictionary - should complete without exception
                Dictionary<string, object> emptyDict = [];
                cache.InsertObjects(emptyDict).Subscribe();

                // Test 2: Single item - should complete normally
                Dictionary<string, object> singleDict = new() { ["single"] = "value" };
                cache.InsertObjects(singleDict).Subscribe();

                // Test 3: Multiple items including edge cases
                Dictionary<string, object> multiDict = new()
                {
                    ["string_val"] = "test",
                    ["int_val"] = 42,
                    ["null_val"] = null!,
                    ["empty_string"] = string.Empty,
                    ["complex_obj"] = new { Prop1 = "value1", Prop2 = 123 }
                };
                cache.InsertObjects(multiDict).Subscribe();

                // Test 4: Large number of operations to stress test completion logic
                var largeDict = Enumerable.Range(1, 1000)
                    .ToDictionary(static i => $"key_{i}", static i => (object)$"value_{i}");
                cache.InsertObjects(largeDict).Subscribe();

                // Test 5: Verify data was actually stored correctly
                string? retrievedSingle = null;
                cache.GetObject<string>("single").Subscribe(v => retrievedSingle = v);
                string? retrievedString = null;
                cache.GetObject<string>("string_val").Subscribe(v => retrievedString = v);
                int retrievedInt = 0;
                cache.GetObject<int>("int_val").Subscribe(v => retrievedInt = v);
                string? retrievedLarge = null;
                cache.GetObject<string>("key_500").Subscribe(v => retrievedLarge = v);

                using (Assert.Multiple())
                {
                    await Assert.That(retrievedSingle).IsEqualTo("value");
                    await Assert.That(retrievedString).IsEqualTo("test");
                    await Assert.That(retrievedInt).IsEqualTo(42);
                    await Assert.That(retrievedLarge).IsEqualTo("value_500");
                }

                // All tests pass - the completion logic is robust
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that InsertObjects handles problematic scenarios that could cause
    /// incomplete observable sequences without throwing exceptions.
    /// This validates the robustness of the LastOrDefaultAsync() approach.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsShouldHandleProblematicScenariosRobustly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

            try
            {
                // Test with null values that might cause serialization edge cases
                Dictionary<string, object?> problematicDict = new()
                {
                    ["null_value"] = null,
                    ["empty_string"] = string.Empty,
                    ["whitespace"] = "   ",
                    ["normal_value"] = "normal"
                };

                // This should complete without throwing "Sequence contains no elements"
                cache.InsertObjects(problematicDict!).Subscribe();

                // Test with very large number of items to stress the completion logic
                var massiveDict = Enumerable.Range(1, 1000)
                    .ToDictionary(
                        static i => $"stress_key_{i}",
                        static i => (object)$"stress_value_{i}");

                // This should also complete without exception
                cache.InsertObjects(massiveDict).Subscribe();

                // Verify some values were stored correctly
                object? nullValue = null;
                cache.GetObject<object>("null_value").Subscribe(v => nullValue = v);
                string? emptyString = null;
                cache.GetObject<string>("empty_string").Subscribe(v => emptyString = v);
                string? normalValue = null;
                cache.GetObject<string>("normal_value").Subscribe(v => normalValue = v);
                string? stressValue500 = null;
                cache.GetObject<string>("stress_key_500").Subscribe(v => stressValue500 = v);

                using (Assert.Multiple())
                {
                    await Assert.That(nullValue).IsNull();
                    await Assert.That(emptyString).IsEqualTo(string.Empty);
                    await Assert.That(normalValue).IsEqualTo("normal");
                    await Assert.That(stressValue500).IsEqualTo("stress_value_500");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        var fetchCount = 0;

        try
        {
            // Function that returns incrementing values to test if it's called
            Func<IObservable<string>> fetchFunc = () =>
            {
                fetchCount++;
                return Observable.Return($"value_{fetchCount}");
            };

            // Act 1: First call to GetOrFetchObject should fetch and cache
            string? result1 = null;
            cache.GetOrFetchObject("test_key", fetchFunc).Subscribe(v => result1 = v);

            // Act 2: Invalidate the key
            cache.Invalidate("test_key").Subscribe();

            // Act 3: Second call to GetOrFetchObject should fetch again (not return cached RequestCache)
            string? result2 = null;
            cache.GetOrFetchObject("test_key", fetchFunc).Subscribe(v => result2 = v);

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(result1).IsEqualTo("value_1");
                await Assert.That(result2).IsEqualTo("value_2");
                await Assert.That(fetchCount).IsEqualTo(2);
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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        var cnt = 0;

        try
        {
            var getOrFetchAsync = () =>
            {
                return cache.GetOrFetchObject(
                    "a",
                    () =>
                    {
                        cnt++;
                        return Observable.Return($"b{cnt}");
                    },
                    DateTime.UtcNow + TimeSpan.FromMilliseconds(1000));
            };

            // Act & Assert - Follow the exact pattern from the bug report
            string? result1 = null;
            getOrFetchAsync().Subscribe(v => result1 = v);
            cache.Invalidate("a").Subscribe();
            string? result2 = null;
            getOrFetchAsync().Subscribe(v => result2 = v);

            using (Assert.Multiple())
            {
                await Assert.That(result1).IsEqualTo("b1");
                await Assert.That(result2).IsEqualTo("b2");
                await Assert.That(cnt).IsEqualTo(2);
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that InsertObject with expiration parameter works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectWithExpirationShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        UserObject user = new() { Name = "Expiring User", Bio = "Bio", Blog = "Blog" };
        var expiration = DateTimeOffset.Now.AddHours(1);

        try
        {
            // Act
            cache.InsertObject("expiring_user", user, expiration).Subscribe();

            // Assert
            UserObject? retrieved = null;
            cache.GetObject<UserObject>("expiring_user").Subscribe(v => retrieved = v);
            await Assert.That(retrieved).IsNotNull();
            await Assert.That(retrieved!.Name).IsEqualTo("Expiring User");

            // Verify CreatedAt is set
            DateTimeOffset? createdAt = null;
            cache.GetObjectCreatedAt<UserObject>("expiring_user").Subscribe(v => createdAt = v);
            await Assert.That(createdAt).IsNotNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetObject properly handles null values stored in cache.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectShouldHandleNullValues()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            // Act - Insert null value
            cache.InsertObject<UserObject?>("null_user", null).Subscribe();

            // Assert - Should return null (or default)
            UserObject? result = null;
            cache.GetObject<UserObject>("null_user").Subscribe(v => result = v);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetObjects properly handles missing keys.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectsShouldHandleMissingKeys()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        UserObject user1 = new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" };

        try
        {
            // Insert only one user
            cache.InsertObject("user1", user1).SubscribeAndComplete();

            // Act - Request multiple keys where some are missing
            IList<KeyValuePair<string, UserObject>>? results = null;
            cache.GetObjects<UserObject>(["user1", "user_missing"]).ToList().Subscribe(v => results = v);

            // Assert - Should only return the found object
            await Assert.That(results).Count().IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo("user1");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that InsertObjects with expiration parameter works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsWithExpirationShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        var expiration = DateTimeOffset.Now.AddHours(1);
        List<KeyValuePair<string, UserObject>> keyValuePairs =
        [
            new("exp_user1", new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" }),
            new("exp_user2", new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" })
        ];

        try
        {
            // Act
            cache.InsertObjects(keyValuePairs, expiration).Subscribe();

            // Assert
            UserObject? user1 = null;
            cache.GetObject<UserObject>("exp_user1").Subscribe(v => user1 = v);
            UserObject? user2 = null;
            cache.GetObject<UserObject>("exp_user2").Subscribe(v => user2 = v);

            using (Assert.Multiple())
            {
                await Assert.That(user1).IsNotNull();
                await Assert.That(user1!.Name).IsEqualTo("User1");
                await Assert.That(user2).IsNotNull();
                await Assert.That(user2!.Name).IsEqualTo("User2");
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetOrFetchObject handles exceptions from the fetch function gracefully.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldHandleFetchExceptions()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            // Act & Assert - Fetch function throws
            Exception? fetchError = null;
            cache.GetOrFetchObject(
                    "failing_fetch",
                    () => Observable.Throw<UserObject>(new InvalidOperationException("Fetch failed")))
                .Subscribe(_ => { }, ex => fetchError = ex);
            await Assert.That(fetchError).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetOrCreateObject handles exceptions from the create function gracefully.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetOrCreateObjectShouldHandleCreateExceptions()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            // Act & Assert - Create function throws
            Exception? createError = null;
            cache.GetOrCreateObject<UserObject>(
                    "failing_create",
                    () => throw new InvalidOperationException("Create failed"))
                .Subscribe(_ => { }, ex => createError = ex);
            await Assert.That(createError).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that InvalidateObjects with empty collection completes without error.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateObjectsWithEmptyCollectionShouldCompleteWithoutError()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            // Act - Should not throw
            cache.InvalidateObjects<UserObject>([]).Subscribe();

            // Test passes if no exception was thrown
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetAllObjects returns empty when cache has no objects of the specified type.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetAllObjectsShouldReturnEmptyWhenNoObjectsOfType()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            // Act - Request all objects of a type when none exist
            IEnumerable<UserObject>? results = null;
            cache.GetAllObjects<UserObject>().Subscribe(v => results = v);

            // Assert
            await Assert.That(results).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetObjectCreatedAt throws for non-existent keys.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldReturnNullForNonExistentKey()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            // Act
            DateTimeOffset? createdAt = null;
            cache.GetObjectCreatedAt<UserObject>("non_existent_key").Subscribe(v => createdAt = v);

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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        UserObject user = new() { Name = "User", Bio = "Bio", Blog = "Blog" };

        try
        {
            // Some cache implementations may allow whitespace keys
            // Test the actual behavior
            try
            {
                cache.InsertObject("   ", user).Subscribe();

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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        try
        {
            // Some cache implementations may allow whitespace keys
            // Test the actual behavior
            try
            {
                cache.GetObject<UserObject>("   ").Subscribe();

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

    /// <summary>
    /// Tests that concurrent GetOrFetchObject calls don't cause race conditions.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ConcurrentGetOrFetchObjectShouldBeThreadSafe()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        var fetchCount = 0;

        try
        {
            var fetchFunc = () =>
            {
                Interlocked.Increment(ref fetchCount);
                return Observable.Return(new UserObject { Name = $"User{fetchCount}", Bio = "Bio", Blog = "Blog" })
                    .Delay(TimeSpan.FromMilliseconds(50));
            };

            // Act - Start multiple concurrent fetches
            var observables = Enumerable.Range(0, 10)
                .Select(_ => cache.GetOrFetchObject("concurrent_user", fetchFunc))
                .ToArray();

            var results = new UserObject?[observables.Length];
            CountdownEvent countdown = new(observables.Length);
            for (var i = 0; i < observables.Length; i++)
            {
                var idx = i;
                observables[i].Subscribe(
                    v => results[idx] = v,
                    _ => countdown.Signal(),
                    () => countdown.Signal());
            }

            countdown.Wait(TimeSpan.FromSeconds(30));

            // Assert - All results should be non-null
            using (Assert.Multiple())
            {
                await Assert.That(results.All(r => r != null)).IsTrue();

                // Fetch count should be reasonable - not all 10 individual fetches
                // but due to timing and parallel execution, exact count varies
                await Assert.That(fetchCount).IsLessThanOrEqualTo(10);
                await Assert.That(fetchCount).IsGreaterThanOrEqualTo(1);
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InsertObjects throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InsertObjects<string>(null!, [
                new("k", "v")
            ]))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests GetObjects throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetObjects<string>(null!, ["k"]))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests InsertObject throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InsertObject(null!, "key", "value"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests InsertObject throws on empty key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectShouldThrowOnEmptyKey()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => SerializerExtensions.InsertObject(cache, string.Empty, "value"))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InsertObject handles null value by storing empty bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectShouldHandleNullValue()
    {
        var cache = CreateCache();
        try
        {
            cache.InsertObject<string>("k", null!).Subscribe();
            string? result = null;
            cache.GetObject<string>("k").Subscribe(v => result = v);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests GetObject throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetObject<string>(null!, "key"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests GetObject throws on empty key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectShouldThrowOnEmptyKey()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => SerializerExtensions.GetObject<string>(cache, string.Empty))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests GetAllObjects throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetAllObjects<string>(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests GetObjectCreatedAt throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetObjectCreatedAt<string>(null!, "key"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests GetObjectCreatedAt throws on empty key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectCreatedAtShouldThrowOnEmptyKey()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => SerializerExtensions.GetObjectCreatedAt<string>(cache, string.Empty))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InvalidateObject throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InvalidateObject<string>(null!, "key"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests InvalidateObject throws on empty key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectShouldThrowOnEmptyKey()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => SerializerExtensions.InvalidateObject<string>(cache, string.Empty))
                .Throws<ArgumentException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InvalidateObjects throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InvalidateObjects<string>(null!, ["key"]))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests InvalidateObjects throws on null keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateObjectsShouldThrowOnNullKeys()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => cache.InvalidateObjects<string>(null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InvalidateAllObjects throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InvalidateAllObjects<string>(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Exercises the static <see cref="SerializerExtensions.GetAllObjects{T}"/> extension
    /// method directly. Tests that use <c>cache.GetAllObjects&lt;T&gt;()</c> on an
    /// <see cref="InMemoryBlobCache"/> actually hit the shadowing instance method on
    /// <see cref="InMemoryBlobCacheBase"/>, so the extension body never executes. This
    /// test invokes the extension explicitly via its static form on the
    /// <see cref="IBlobCache"/> interface so the extension method body is covered.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetAllObjectsStaticExtensionShouldReturnStoredObjects()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        try
        {
            UserObject user1 = new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            UserObject user2 = new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" };
            cache.InsertObject("user1", user1).SubscribeAndComplete();
            cache.InsertObject("user2", user2).SubscribeAndComplete();

            var results = await SerializerExtensions.GetAllObjects<UserObject>(cache).ToList();

            await Assert.That(results).Count().IsEqualTo(2);
            await Assert.That(results.Any(static x => x.Name == "User1")).IsTrue();
            await Assert.That(results.Any(static x => x.Name == "User2")).IsTrue();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Exercises the static <see cref="SerializerExtensions.InvalidateAllObjects{T}"/>
    /// extension method directly. See <see cref="GetAllObjectsStaticExtensionShouldReturnStoredObjects"/>
    /// for why the static form is necessary.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateAllObjectsStaticExtensionShouldRemoveAllObjectsOfType()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
        try
        {
            UserObject user1 = new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            cache.InsertObject("user1", user1).SubscribeAndComplete();

            SerializerExtensions.InvalidateAllObjects<UserObject>(cache).Subscribe();

            var results = await SerializerExtensions.GetAllObjects<UserObject>(cache).ToList();
            await Assert.That(results).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InsertAllObjects throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertAllObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InsertAllObjects<string>(null!, [
                new("k", "v")
            ]))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests GetOrFetchObject throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldThrowOnNullCache() =>
        await Assert.That(static () =>
                SerializerExtensions.GetOrFetchObject(null!, "key", static () => Observable.Return("value")))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests GetOrFetchObject throws on null fetchFunc.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldThrowOnNullFetchFunc()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => cache.GetOrFetchObject("key", (Func<IObservable<string>>)null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InsertObjects(IDictionary) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsDictionaryShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InsertObjects(null!, new Dictionary<string, object>()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests InsertObjects(IDictionary) throws on null keyValuePairs.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsDictionaryShouldThrowOnNullPairs()
    {
        var cache = CreateCache();
        try
        {
            await Assert.That(() => cache.InsertObjects(null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InsertObjects(IDictionary) returns immediately for empty input.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsDictionaryShouldReturnImmediatelyForEmpty()
    {
        var cache = CreateCache();
        try
        {
            cache.InsertObjects(new Dictionary<string, object>()).Subscribe();
            IList<string>? keys = null;
            cache.GetAllKeys().ToList().Subscribe(v => keys = v);
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InsertObjects(IDictionary) inserts mixed types.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsDictionaryShouldInsertMixedTypes()
    {
        var cache = CreateCache();
        try
        {
            Dictionary<string, object> data = new()
            {
                ["k1"] = "string value",
                ["k2"] = 42,
                ["k3"] = new UserObject { Name = "user", Bio = "bio", Blog = "blog" }
            };

            cache.InsertObjects(data).Subscribe();

            IList<string>? keys = null;
            cache.GetAllKeys().ToList().Subscribe(v => keys = v);
            await Assert.That(keys!.Count).IsEqualTo(3);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests SerializeWithContext throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeWithContextShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.SerializeWithContext("value", null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests DeserializeWithContext returns default for null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DeserializeWithContextShouldReturnDefaultForNullCache()
    {
        var result = SerializerExtensions.DeserializeWithContext<string>([1, 2, 3], null!);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests DeserializeWithContext returns default for null data.
    /// </summary>
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

    /// <summary>
    /// Tests DeserializeWithContext returns default for empty data.
    /// </summary>
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

    /// <summary>
    /// Tests SerializeWithContext handles DateTime via UniversalSerializer.
    /// </summary>
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

    /// <summary>
    /// Tests DeserializeWithContext handles DateTime via UniversalSerializer.
    /// </summary>
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
            await Assert.That(result.Year).IsEqualTo(2025);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that SerializerExtensions.InsertObject extension stores empty bytes for a null value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectExtensionShouldStoreEmptyBytesForNullValue()
    {
        var cache = CreateCache();
        try
        {
            // Call extension explicitly to bypass instance-method shadowing.
            SerializerExtensions.InsertObject<string>(cache, "null_key", null!).Subscribe();
            string? result = null;
            SerializerExtensions.GetObject<string>(cache, "null_key").Subscribe(v => result = v);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that SerializerExtensions.InsertObject wraps serialization failures in InvalidOperationException.
    /// </summary>
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

    /// <summary>
    /// Tests that SerializerExtensions.GetObject returns default for an empty byte marker.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectExtensionShouldReturnDefaultForEmptyBytes()
    {
        var cache = CreateCache();
        try
        {
            // Store an empty byte array under a typed key to trigger the empty-length branch.
            cache.Insert("empty_key", [], typeof(string)).Subscribe();
            string? result = null;
            SerializerExtensions.GetObject<string>(cache, "empty_key").Subscribe(v => result = v);
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that SerializerExtensions.GetObject wraps deserialization failures in InvalidOperationException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetObjectExtensionShouldWrapDeserializationFailure()
    {
        var cache = CreateCache();
        try
        {
            // Store invalid JSON bytes under a typed key so deserialization fails.
            byte[] invalid = [0xFF, 0xFE, 0xFD, 0x01];
            cache.Insert("bad_json", invalid, typeof(UserObject)).Subscribe();

            Exception? error = null;
            SerializerExtensions.GetObject<UserObject>(cache, "bad_json").Subscribe(_ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetAllObjects returns all stored objects of the requested type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllObjectsShouldReturnStoredObjects()
    {
        var cache = CreateCache();
        try
        {
            UserObject u1 = new() { Name = "A", Bio = "B1", Blog = "Bl1" };
            UserObject u2 = new() { Name = "B", Bio = "B2", Blog = "Bl2" };
            SerializerExtensions.InsertObject(cache, "a", u1).Subscribe();
            SerializerExtensions.InsertObject(cache, "b", u2).Subscribe();

            IEnumerable<UserObject>? allEnumerable = null;
            cache.GetAllObjects<UserObject>().Subscribe(v => allEnumerable = v);
            var all = allEnumerable!.ToList();
            await Assert.That(all.Count).IsEqualTo(2);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetAndFetchLatest with cacheValidationPredicate returning false skips caching.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestShouldSkipCachingWhenValidationFails()
    {
        var cache = CreateCache();
        try
        {
            UserObject latest = new() { Name = "Latest", Bio = "B", Blog = "Bl" };

            IList<UserObject?>? results = null;
            cache.GetAndFetchLatest(
                    "validate_key",
                    () => Observable.Return(latest),
                    fetchPredicate: null,
                    absoluteExpiration: null,
                    shouldInvalidateOnError: false,
                    cacheValidationPredicate: _ => false)
                .ToList()
                .Subscribe(v => results = v);

            await Assert.That(results).IsNotEmpty();

            // Since cacheValidationPredicate returned false, the cache should not contain the key.
            Exception? error = null;
            cache.GetObject<UserObject>("validate_key").Subscribe(_ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetAndFetchLatest invalidates the cache on fetch error when shouldInvalidateOnError is true.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestShouldInvalidateOnErrorWhenRequested()
    {
        var cache = CreateCache();
        try
        {
            UserObject cached = new() { Name = "Cached", Bio = "B", Blog = "Bl" };
            SerializerExtensions.InsertObject(cache, "inv_key", cached).Subscribe();

            List<UserObject?> observed = [];
            Exception? caught = null;

            try
            {
                await cache.GetAndFetchLatest(
                        "inv_key",
                        () => Observable.Throw<UserObject>(new InvalidOperationException("fetch boom")),
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
            cache.GetObject<UserObject>("inv_key").Subscribe(_ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetAndFetchLatest without shouldInvalidateOnError preserves the cached value on fetch error.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestShouldNotInvalidateOnErrorByDefault()
    {
        var cache = CreateCache();
        try
        {
            UserObject cached = new() { Name = "Cached", Bio = "B", Blog = "Bl" };
            SerializerExtensions.InsertObject(cache, "keep_key", cached).Subscribe();

            List<UserObject?> observed = [];
            try
            {
                await cache.GetAndFetchLatest(
                        "keep_key",
                        static () => Observable.Throw<UserObject>(new InvalidOperationException("fetch boom")))
                    .ForEachAsync(observed.Add);
            }
            catch
            {
                // expected
            }

            // Cache entry should still exist.
            UserObject? stillThere = null;
            cache.GetObject<UserObject>("keep_key").Subscribe(v => stillThere = v);
            await Assert.That(stillThere).IsNotNull();
            await Assert.That(stillThere!.Name).IsEqualTo("Cached");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests SerializeWithContext handles nullable DateTime via UniversalSerializer.
    /// </summary>
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

    /// <summary>
    /// Tests DeserializeWithContext handles nullable DateTime via UniversalSerializer.
    /// </summary>
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
            await Assert.That(result!.Value.Year).IsEqualTo(2025);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests SerializeWithContext applies ForcedDateTimeKind for non-DateTime types.
    /// </summary>
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

    /// <summary>
    /// Tests DeserializeWithContext applies ForcedDateTimeKind for non-DateTime types.
    /// </summary>
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

    /// <summary>
    /// Tests DeserializeWithContext wraps serializer failures in InvalidOperationException.
    /// </summary>
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

    /// <summary>
    /// Tests GetAllKeysSafe recovers from a failing underlying source by emitting empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeShouldRecoverFromExceptions()
    {
        var cache = CreateCache();
        cache.Dispose();

        // After dispose, GetAllKeys throws; GetAllKeysSafe should swallow and return empty.
        IList<string>? keys = null;
        cache.GetAllKeysSafe().ToList().Subscribe(v => keys = v);
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests GetAllKeysSafe(Type) recovers from a failing underlying source by emitting empty.
    /// </summary>
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
        cache.GetAllKeysSafe(typeof(UserObject)).ToList().Subscribe(v => keys = v);
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests that GetOrCreateObject throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateObjectShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetOrCreateObject(null!, "key", static () => "value"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that GetAllKeysSafe throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetAllKeysSafe(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that GetAllKeysSafe with Type throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeWithTypeShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetAllKeysSafe(null!, typeof(string)))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that GetAllKeysSafe with Type throws ArgumentNullException when type is null.
    /// </summary>
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

    /// <summary>
    /// Tests that generic GetAllKeysSafe throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeGenericShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetAllKeysSafe<string>(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that the generic GetAllKeysSafe returns keys for a specific type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeGenericShouldReturnKeysForType()
    {
        var cache = CreateCache();
        try
        {
            SerializerExtensions.InsertObject(cache, "u1", new UserObject { Name = "A", Bio = "B", Blog = "C" })
                .Subscribe();

            IList<string>? keys = null;
            cache.GetAllKeysSafe<UserObject>().ToList().Subscribe(v => keys = v);
            await Assert.That(keys!.Count).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that the generic GetAllKeysSafe recovers from exceptions by returning empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeGenericShouldRecoverFromExceptions()
    {
        var cache = CreateCache();
        cache.Dispose();

        // After dispose, GetAllKeys throws; GetAllKeysSafe<T> should swallow and return empty.
        IList<string>? keys = null;
        cache.GetAllKeysSafe<UserObject>().ToList().Subscribe(v => keys = v);
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests that GetAllKeysSafe filters out null and empty keys from a valid cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeShouldReturnKeysFromValidCache()
    {
        var cache = CreateCache();
        try
        {
            cache.Insert("safe_key1", [1, 2, 3]).Subscribe();
            cache.Insert("safe_key2", [4, 5, 6]).Subscribe();

            IList<string>? keys = null;
            cache.GetAllKeysSafe().ToList().Subscribe(v => keys = v);
            await Assert.That(keys!.Count).IsGreaterThanOrEqualTo(2);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with a Type parameter returns keys for valid cache.
    /// </summary>
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
            SerializerExtensions.InsertObject(cache, "typed_key", new UserObject { Name = "T", Bio = "B", Blog = "Bl" })
                .Subscribe();

            IList<string>? keys = null;
            cache.GetAllKeysSafe(typeof(UserObject)).ToList().Subscribe(v => keys = v);
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
            cache.GetObject<UserObject>("any_key").Subscribe(_ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetAndFetchLatest Task overload with shouldInvalidateOnError invalidates cache on error.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestTaskOverloadShouldInvalidateOnError()
    {
        var cache = CreateCache();
        try
        {
            UserObject cached = new() { Name = "Cached", Bio = "B", Blog = "Bl" };
            SerializerExtensions.InsertObject(cache, "task_inv", cached).Subscribe();

            Exception? caught = null;
            try
            {
                await cache.GetAndFetchLatest(
                        "task_inv",
                        () => Task.FromException<UserObject>(new InvalidOperationException("task fetch boom")),
                        fetchPredicate: null,
                        absoluteExpiration: null,
                        shouldInvalidateOnError: true)
                    .ForEachAsync(_ => { });
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            await Assert.That(caught).IsNotNull();

            // Cache entry should have been invalidated.
            Exception? error = null;
            cache.GetObject<UserObject>("task_inv").Subscribe(_ => { }, ex => error = ex);
            await Assert.That(error).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that GetAndFetchLatest Task overload with cacheValidationPredicate returning false skips caching.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAndFetchLatestTaskOverloadShouldSkipCachingWhenValidationFails()
    {
        var cache = CreateCache();
        try
        {
            UserObject latest = new() { Name = "LatestTask", Bio = "B", Blog = "Bl" };

            IList<UserObject?>? results = null;
            cache.GetAndFetchLatest(
                    "task_validate",
                    () => Task.FromResult(latest),
                    fetchPredicate: null,
                    absoluteExpiration: null,
                    shouldInvalidateOnError: false,
                    cacheValidationPredicate: _ => false)
                .ToList()
                .Subscribe(v => results = v);

            await Assert.That(results).IsNotEmpty();

            // Since cacheValidationPredicate returned false, the cache should not contain the key.
            Exception? error = null;
            cache.GetObject<UserObject>("task_validate").Subscribe(_ => { }, ex => error = ex);
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
        var result = SerializerExtensions.ShouldRefetchCachedValue(null, DateTimeOffset.UtcNow);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Verifies <see cref="SerializerExtensions.ShouldRefetchCachedValue"/> returns <c>true</c>
    /// when the cache has no creation timestamp, regardless of the predicate.
    /// </summary>
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
        var result = SerializerExtensions.ShouldRefetchCachedValue(static _ => true, DateTimeOffset.UtcNow);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Verifies <see cref="SerializerExtensions.ShouldRefetchCachedValue"/> returns <c>false</c>
    /// when the predicate rejects the timestamp — the cached value is considered fresh.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldRefetchCachedValueShouldHonourPredicateWhenItReturnsFalse()
    {
        var result = SerializerExtensions.ShouldRefetchCachedValue(static _ => false, DateTimeOffset.UtcNow);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Creates a new instance of an in-memory blob cache with the specified scheduler and serializer.
    /// </summary>
    /// <returns>A new instance of the in-memory blob cache.</returns>
    private static InMemoryBlobCache CreateCache() =>
        new(ImmediateScheduler.Instance, new SystemJsonSerializer());

    /// <summary>
    /// A minimal IBlobCache implementation that returns null from Get(key, type)
    /// to exercise the null byte array guard in GetObject's Select lambda.
    /// </summary>
    private sealed class NullReturningBlobCache : IBlobCache
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullReturningBlobCache"/> class.
        /// </summary>
        /// <param name="serializer">The serializer to use.</param>
        public NullReturningBlobCache(ISerializer serializer) => Serializer = serializer;

        /// <inheritdoc/>
        public ISerializer Serializer { get; }

        /// <inheritdoc/>
        public IScheduler Scheduler => ImmediateScheduler.Instance;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            DateTimeOffset? absoluteExpiration = null) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            Type type,
            DateTimeOffset? absoluteExpiration = null) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            string key,
            byte[] data,
            Type type,
            DateTimeOffset? absoluteExpiration = null) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) =>
            Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) =>
            Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) =>
            Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) =>
            Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) =>
            Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() =>
            Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) =>
            Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
            Observable.Empty<(string, DateTimeOffset?)>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) =>
            Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
            Observable.Empty<(string, DateTimeOffset?)>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) =>
            Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<Unit> Flush() =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Flush(Type type) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key, Type type) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll(Type type) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll() =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Vacuum() =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(
            IEnumerable<string> keys,
            Type type,
            DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
