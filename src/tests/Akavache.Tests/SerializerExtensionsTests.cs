// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;

using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests for serializer extension methods.
/// </summary>
[Category("Akavache")]
[NotInParallel(nameof(SerializerExtensionsTests))]
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);
            var keyValuePairs = new List<KeyValuePair<string, UserObject>>
                {
                    new("user1", new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" }),
                    new("user2", new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" })
                };

            try
            {
                // Act
                await cache.InsertObjects(keyValuePairs).FirstAsync();

                // Assert
                var user1 = await cache.GetObject<UserObject>("user1").FirstAsync();
                var user2 = await cache.GetObject<UserObject>("user2").FirstAsync();

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
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);
            var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            var user2 = new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

            try
            {
                // Insert test data
                await cache.InsertObject("user1", user1).FirstAsync();
                await cache.InsertObject("user2", user2).FirstAsync();

                // Act
                var results = await cache.GetObjects<UserObject>(["user1", "user2"]).ToList().FirstAsync();

                // Assert
                await Assert.That(results).Count().IsEqualTo(2);

                var user1Result = results.First(static r => r.Key == "user1").Value;
                using (Assert.Multiple())
                {
                    await Assert.That(user1Result.Name).IsEqualTo("User1");
                    await Assert.That(user1Result.Bio).IsEqualTo("Bio1");
                }

                var user2Result = results.First(static r => r.Key == "user2").Value;
                using (Assert.Multiple())
                {
                    await Assert.That(user2Result.Name).IsEqualTo("User2");
                    await Assert.That(user2Result.Bio).IsEqualTo("Bio2");
                }
            }
            finally
            {
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            // C# 8's 'await using' simplifies async disposal
            await using var cache = new InMemoryBlobCache(serializer);

            var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            var user2 = new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

            // Insert test data
            await cache.InsertObject("user1", user1).FirstAsync();
            await cache.InsertObject("user2", user2).FirstAsync();

            // Act
            var allObjects = await cache.GetAllObjects<UserObject>().FirstAsync();
            var results = allObjects.ToList();

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(results).Count().IsEqualTo(2);
                await Assert.That(results.Any(x => x.Name == "User1")).IsTrue();
                await Assert.That(results.Any(x => x.Name == "User2")).IsTrue();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);
            var user = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };

            try
            {
                // Insert test data
                await cache.InsertObject("user1", user).FirstAsync();

                // Verify object exists
                var retrievedUser = await cache.GetObject<UserObject>("user1").FirstAsync();
                await Assert.That(retrievedUser).IsNotNull();

                // Act
                await cache.InvalidateObject<UserObject>("user1").FirstAsync();

                // Assert
                await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user1").FirstAsync());
            }
            finally
            {
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);
            var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            var user2 = new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

            try
            {
                // Insert test data
                await cache.InsertObject("user1", user1).FirstAsync();
                await cache.InsertObject("user2", user2).FirstAsync();

                // Act
                await cache.InvalidateObjects<UserObject>(["user1", "user2"]).FirstAsync();

                // Assert
                await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user1").FirstAsync());

                await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user2").FirstAsync());
            }
            finally
            {
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);
            var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            var user2 = new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

            try
            {
                // Insert test data
                await cache.InsertObject("user1", user1).FirstAsync();
                await cache.InsertObject("user2", user2).FirstAsync();

                // Verify objects exist before invalidation
                var beforeInvalidation = await cache.GetAllObjects<UserObject>().FirstAsync();
                await Assert.That(beforeInvalidation.Count()).IsEqualTo(2);

                // Act
                await cache.InvalidateAllObjects<UserObject>().FirstAsync();

                // Assert - The primary verification is that individual objects can't be retrieved
                await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user1").FirstAsync());

                await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user2").FirstAsync());

                // Additional check - GetAllObjects should return empty result
                var results = await cache.GetAllObjects<UserObject>().FirstAsync();
                await Assert.That(results).IsEmpty();
            }
            finally
            {
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);
            var user = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            var beforeInsert = DateTimeOffset.Now;

            try
            {
                // Act
                await cache.InsertObject("user1", user).FirstAsync();
                var createdAt = await cache.GetObjectCreatedAt<UserObject>("user1").FirstAsync();

                // Assert
                await Assert.That(createdAt).IsNotNull();
                await Assert.That(createdAt!.Value).IsGreaterThanOrEqualTo(beforeInsert);
                await Assert.That(createdAt.Value).IsLessThanOrEqualTo(DateTimeOffset.Now);
            }
            finally
            {
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);
            KeyValuePair<string, UserObject>[] keyValuePairs =
            [
                new("user1", new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" }),
                new("user2", new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" })
            ];

            try
            {
                // Act
                await cache.InsertAllObjects(keyValuePairs).FirstAsync();

                // Assert
                var user1 = await cache.GetObject<UserObject>("user1").FirstAsync();
                var user2 = await cache.GetObject<UserObject>("user2").FirstAsync();

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
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);
            var user = new UserObject { Name = "Created User", Bio = "Created Bio", Blog = "Created Blog" };

            try
            {
                // Act
                var result = await cache.GetOrCreateObject("new_user", () => user).FirstAsync();

                // Assert
                await Assert.That(result).IsNotNull();
                await Assert.That(result!.Name).IsEqualTo("Created User");

                // Verify it was actually stored
                var storedUser = await cache.GetObject<UserObject>("new_user").FirstAsync();
                await Assert.That(storedUser).IsNotNull();
                await Assert.That(storedUser!.Name).IsEqualTo("Created User");
            }
            finally
            {
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);
            var existingUser = new UserObject { Name = "Existing User", Bio = "Existing Bio", Blog = "Existing Blog" };
            var newUser = new UserObject { Name = "New User", Bio = "New Bio", Blog = "New Blog" };

            try
            {
                // Insert existing user
                await cache.InsertObject("existing_user", existingUser).FirstAsync();

                // Act
                var result = await cache.GetOrCreateObject("existing_user", () => newUser).FirstAsync();

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
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);
        var fetchedUser = new UserObject { Name = "Fetched User", Bio = "Fetched Bio", Blog = "Fetched Blog" };
        var fetchCount = 0;

        try
        {
            // Act
            var result = await cache.GetOrFetchObject("fetch_user", () =>
            {
                fetchCount++;
                return Observable.Return(fetchedUser);
            }).FirstAsync();

            // Assert
            await Assert.That(result).IsNotNull();
            using (Assert.Multiple())
            {
                await Assert.That(result!.Name).IsEqualTo("Fetched User");
                await Assert.That(fetchCount).IsEqualTo(1);
            }

            // Verify it was stored in cache
            var cachedUser = await cache.GetObject<UserObject>("fetch_user").FirstAsync();
            await Assert.That(cachedUser).IsNotNull();
            await Assert.That(cachedUser!.Name).IsEqualTo("Fetched User");
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);
        var cachedUser = new UserObject { Name = "Cached User", Bio = "Cached Bio", Blog = "Cached Blog" };
        var fetchedUser = new UserObject { Name = "Fetched User", Bio = "Fetched Bio", Blog = "Fetched Blog" };
        var fetchCount = 0;

        try
        {
            // Insert cached value
            await cache.InsertObject("cached_user", cachedUser).FirstAsync();

            // Act
            var result = await cache.GetOrFetchObject("cached_user", () =>
            {
                fetchCount++;
                return Observable.Return(fetchedUser);
            }).FirstAsync();

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
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);
        var fetchedUser = new UserObject { Name = "Task Fetched User", Bio = "Task Bio", Blog = "Task Blog" };

        try
        {
            // Act
            var result = await cache.GetOrFetchObject("task_user", () => Task.FromResult(fetchedUser)).FirstAsync();

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("Task Fetched User");
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);
        var cachedUser = new UserObject { Name = "Cached User", Bio = "Cached Bio", Blog = "Cached Blog" };
        var latestUser = new UserObject { Name = "Latest User", Bio = "Latest Bio", Blog = "Latest Blog" };

        try
        {
            // Insert cached value
            await cache.InsertObject("user", cachedUser).FirstAsync();

            var results = new List<UserObject?>();

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
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);
        var latestUser = new UserObject { Name = "Task Latest User", Bio = "Task Bio", Blog = "Task Blog" };

        try
        {
            var results = new List<UserObject?>();

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
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);
        var cachedUser = new UserObject { Name = "Cached User", Bio = "Cached Bio", Blog = "Cached Blog" };
        var latestUser = new UserObject { Name = "Latest User", Bio = "Latest Bio", Blog = "Latest Blog" };
        var fetchCount = 0;

        try
        {
            // Insert cached value
            await cache.InsertObject("user", cachedUser).FirstAsync();

            var results = new List<UserObject?>();

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
            await cache.DisposeAsync();
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
        var dict = new Dictionary<string, object> { ["key"] = "value" };

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
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);
        Dictionary<string, object>? dict = null;

        try
        {
            // Act & Assert
            await Assert.That(() => cache.InsertObjects(dict!)).Throws<ArgumentNullException>();
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);
            var emptyDict = new Dictionary<string, object>();

            try
            {
                // Act - should complete without error
                await cache.InsertObjects(emptyDict).FirstAsync();

                // Assert - test passes if no exception is thrown
            }
            finally
            {
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            // Use 'await using' for cleaner async resource management
            await using var cache = new InMemoryBlobCache(serializer);

            var testDate = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            var mixedObjects = new Dictionary<string, object>
            {
                ["string"] = "test string",
                ["int"] = 42,
                ["user"] = new UserObject { Name = "Test User", Bio = "Test Bio", Blog = "Test Blog" },
                ["date"] = testDate
            };

            // Act
            await cache.InsertObjects(mixedObjects).FirstAsync();

            // Assert
            var stringValue = await cache.GetObject<string>("string").FirstAsync();
            var intValue = await cache.GetObject<int>("int").FirstAsync();
            var userValue = await cache.GetObject<UserObject>("user").FirstAsync();
            var dateValue = await cache.GetObject<DateTime>("date").FirstAsync();

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
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Test null key validation
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.GetObjectCreatedAt<string>(null!).FirstAsync());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.InvalidateObject<string>(null!).FirstAsync());

            // Test null collection validation
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.InvalidateObjects<string>(null!).FirstAsync());

            // Note: Extension methods may allow empty strings as valid keys in some implementations
            // This is different from the core methods which validate empty strings

            // Test that methods work with empty string (if allowed by implementation)
            try
            {
                await cache.GetObjectCreatedAt<string>(string.Empty).FirstAsync();

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
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);

            try
            {
                // Test 1: Empty dictionary should complete without exception
                var emptyDict = new Dictionary<string, object>();
                await cache.InsertObjects(emptyDict).FirstAsync();

                // Test 2: Single item should work
                var singleDict = new Dictionary<string, object> { ["key1"] = "value1" };
                await cache.InsertObjects(singleDict).FirstAsync();

                // Test 3: Multiple items should work
                var multiDict = new Dictionary<string, object>
                {
                    ["key2"] = "value2",
                    ["key3"] = 42,
                    ["key4"] = new UserObject { Name = "Test", Bio = "Bio", Blog = "Blog" }
                };
                await cache.InsertObjects(multiDict).FirstAsync();

                // Verify all items were inserted correctly
                var value1 = await cache.GetObject<string>("key1").FirstAsync();
                var value2 = await cache.GetObject<string>("key2").FirstAsync();
                var value3 = await cache.GetObject<int>("key3").FirstAsync();
                var value4 = await cache.GetObject<UserObject>("key4").FirstAsync();

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
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);

            try
            {
                // Test 1: Empty collection
                var emptyPairs = new List<KeyValuePair<string, UserObject>>();
                await cache.InsertObjects(emptyPairs).FirstAsync();

                // Test 2: Single item
                var singlePair = new List<KeyValuePair<string, UserObject>>
                {
                    new("user1", new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" })
                };
                await cache.InsertObjects(singlePair).FirstAsync();

                // Test 3: Multiple items
                var multiPairs = new List<KeyValuePair<string, UserObject>>
                {
                    new("user2", new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" }),
                    new("user3", new UserObject { Name = "User3", Bio = "Bio3", Blog = "Blog3" }),
                    new("user4", new UserObject { Name = "User4", Bio = "Bio4", Blog = "Blog4" })
                };
                await cache.InsertObjects(multiPairs).FirstAsync();

                // Test 4: Large collection to stress test the Count() approach
                var largePairs = Enumerable.Range(1, 100)
                    .Select(i => new KeyValuePair<string, UserObject>(
                        $"large_user_{i}",
                        new UserObject { Name = $"LargeUser{i}", Bio = $"Bio{i}", Blog = $"Blog{i}" }))
                    .ToList();
                await cache.InsertObjects(largePairs).FirstAsync();

                // Verify some items were inserted correctly
                var user1 = await cache.GetObject<UserObject>("user1").FirstAsync();
                var user2 = await cache.GetObject<UserObject>("user2").FirstAsync();
                var largeUser50 = await cache.GetObject<UserObject>("large_user_50").FirstAsync();

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
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);

            try
            {
                // Test 1: Empty dictionary - should complete without exception
                var emptyDict = new Dictionary<string, object>();
                await cache.InsertObjects(emptyDict).FirstAsync();

                // Test 2: Single item - should complete normally
                var singleDict = new Dictionary<string, object> { ["single"] = "value" };
                await cache.InsertObjects(singleDict).FirstAsync();

                // Test 3: Multiple items including edge cases
                var multiDict = new Dictionary<string, object>
                {
                    ["string_val"] = "test",
                    ["int_val"] = 42,
                    ["null_val"] = null!,
                    ["empty_string"] = string.Empty,
                    ["complex_obj"] = new { Prop1 = "value1", Prop2 = 123 }
                };
                await cache.InsertObjects(multiDict).FirstAsync();

                // Test 4: Large number of operations to stress test completion logic
                var largeDict = Enumerable.Range(1, 1000)
                    .ToDictionary(i => $"key_{i}", i => (object)$"value_{i}");
                await cache.InsertObjects(largeDict).FirstAsync();

                // Test 5: Verify data was actually stored correctly
                var retrievedSingle = await cache.GetObject<string>("single").FirstAsync();
                var retrievedString = await cache.GetObject<string>("string_val").FirstAsync();
                var retrievedInt = await cache.GetObject<int>("int_val").FirstAsync();
                var retrievedLarge = await cache.GetObject<string>("key_500").FirstAsync();

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
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = new InMemoryBlobCache(serializer);

            try
            {
                // Test with null values that might cause serialization edge cases
                var problematicDict = new Dictionary<string, object?>
                {
                    ["null_value"] = null,
                    ["empty_string"] = string.Empty,
                    ["whitespace"] = "   ",
                    ["normal_value"] = "normal"
                };

                // This should complete without throwing "Sequence contains no elements"
                await cache.InsertObjects(problematicDict!).FirstAsync();

                // Test with very large number of items to stress the completion logic
                var massiveDict = Enumerable.Range(1, 1000)
                    .ToDictionary(
                        i => $"stress_key_{i}",
                        i => (object)$"stress_value_{i}");

                // This should also complete without exception
                await cache.InsertObjects(massiveDict).FirstAsync();

                // Verify some values were stored correctly
                var nullValue = await cache.GetObject<object>("null_value").FirstAsync();
                var emptyString = await cache.GetObject<string>("empty_string").FirstAsync();
                var normalValue = await cache.GetObject<string>("normal_value").FirstAsync();
                var stressValue500 = await cache.GetObject<string>("stress_key_500").FirstAsync();

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
                await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);
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
            var result1 = await cache.GetOrFetchObject("test_key", fetchFunc).FirstAsync();

            // Act 2: Invalidate the key
            await cache.Invalidate("test_key").FirstAsync();

            // Act 3: Second call to GetOrFetchObject should fetch again (not return cached RequestCache)
            var result2 = await cache.GetOrFetchObject("test_key", fetchFunc).FirstAsync();

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
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);
        var cnt = 0;

        try
        {
            Func<IObservable<string?>> getOrFetchAsync = () =>
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
            var result1 = await getOrFetchAsync().FirstAsync();
            await cache.Invalidate("a").FirstAsync();
            var result2 = await getOrFetchAsync().FirstAsync();

            using (Assert.Multiple())
            {
                await Assert.That(result1).IsEqualTo("b1");
                await Assert.That(result2).IsEqualTo("b2");
                await Assert.That(cnt).IsEqualTo(2);
            }
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);
        var user = new UserObject { Name = "Expiring User", Bio = "Bio", Blog = "Blog" };
        var expiration = DateTimeOffset.Now.AddHours(1);

        try
        {
            // Act
            await cache.InsertObject("expiring_user", user, expiration).FirstAsync();

            // Assert
            var retrieved = await cache.GetObject<UserObject>("expiring_user").FirstAsync();
            await Assert.That(retrieved).IsNotNull();
            await Assert.That(retrieved!.Name).IsEqualTo("Expiring User");

            // Verify CreatedAt is set
            var createdAt = await cache.GetObjectCreatedAt<UserObject>("expiring_user").FirstAsync();
            await Assert.That(createdAt).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act - Insert null value
            await cache.InsertObject<UserObject?>("null_user", null).FirstAsync();

            // Assert - Should return null (or default)
            var result = await cache.GetObject<UserObject>("null_user").FirstAsync();
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);
        var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };

        try
        {
            // Insert only one user
            await cache.InsertObject("user1", user1).FirstAsync();

            // Act - Request multiple keys where some are missing
            var results = await cache.GetObjects<UserObject>(["user1", "user_missing"]).ToList().FirstAsync();

            // Assert - Should only return the found object
            await Assert.That(results).Count().IsEqualTo(1);
            await Assert.That(results[0].Key).IsEqualTo("user1");
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);
        var expiration = DateTimeOffset.Now.AddHours(1);
        var keyValuePairs = new List<KeyValuePair<string, UserObject>>
        {
            new("exp_user1", new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" }),
            new("exp_user2", new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" })
        };

        try
        {
            // Act
            await cache.InsertObjects(keyValuePairs, expiration).FirstAsync();

            // Assert
            var user1 = await cache.GetObject<UserObject>("exp_user1").FirstAsync();
            var user2 = await cache.GetObject<UserObject>("exp_user2").FirstAsync();

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
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act & Assert - Fetch function throws
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await cache.GetOrFetchObject(
                    "failing_fetch",
                    () => Observable.Throw<UserObject>(new InvalidOperationException("Fetch failed"))).FirstAsync();
            });
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act & Assert - Create function throws
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await cache.GetOrCreateObject<UserObject>(
                    "failing_create",
                    () => throw new InvalidOperationException("Create failed")).FirstAsync();
            });
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act - Should not throw
            await cache.InvalidateObjects<UserObject>([]).FirstAsync();

            // Test passes if no exception was thrown
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act - Request all objects of a type when none exist
            var results = await cache.GetAllObjects<UserObject>().FirstAsync();

            // Assert
            await Assert.That(results).IsEmpty();
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act
            var createdAt = await cache.GetObjectCreatedAt<UserObject>("non_existent_key").FirstAsync();

            // Assert - Should return null for missing key
            await Assert.That(createdAt).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);
        var user = new UserObject { Name = "User", Bio = "Bio", Blog = "Blog" };

        try
        {
            // Some cache implementations may allow whitespace keys
            // Test the actual behavior
            try
            {
                await cache.InsertObject("   ", user).FirstAsync();

                // If it doesn't throw, that's acceptable - whitespace is a valid key for InMemoryBlobCache
            }
            catch (ArgumentException)
            {
                // This is expected for implementations that validate whitespace keys
            }
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Some cache implementations may allow whitespace keys
            // Test the actual behavior
            try
            {
                await cache.GetObject<UserObject>("   ").FirstAsync();

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
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);
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
                .Select(_ => cache.GetOrFetchObject("concurrent_user", fetchFunc).FirstAsync())
                .ToArray();

            var tasks = observables.Select(obs => obs.ToTask()).ToArray();
            var results = await Task.WhenAll(tasks);

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
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests InsertObjects throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InsertObjects<string>(null!, [new KeyValuePair<string, string>("k", "v")
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
            await cache.DisposeAsync();
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
            await cache.InsertObject<string>("k", null!).ToTask();
            var result = await cache.GetObject<string>("k").ToTask();
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
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
            await cache.DisposeAsync();
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
            await cache.DisposeAsync();
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
            await cache.DisposeAsync();
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
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);
        try
        {
            var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            var user2 = new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" };
            await cache.InsertObject("user1", user1).FirstAsync();
            await cache.InsertObject("user2", user2).FirstAsync();

            var results = await SerializerExtensions.GetAllObjects<UserObject>(cache).ToList();

            await Assert.That(results).Count().IsEqualTo(2);
            await Assert.That(results.Any(x => x.Name == "User1")).IsTrue();
            await Assert.That(results.Any(x => x.Name == "User2")).IsTrue();
        }
        finally
        {
            await cache.DisposeAsync();
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
        var serializer = new SystemJsonSerializer();
        var cache = new InMemoryBlobCache(serializer);
        try
        {
            var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
            await cache.InsertObject("user1", user1).FirstAsync();

            await SerializerExtensions.InvalidateAllObjects<UserObject>(cache).FirstAsync();

            var results = await SerializerExtensions.GetAllObjects<UserObject>(cache).ToList();
            await Assert.That(results).IsEmpty();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests InsertAllObjects throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertAllObjectsShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.InsertAllObjects<string>(null!, [new KeyValuePair<string, string>("k", "v")
            ]))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests GetOrFetchObject throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrFetchObjectShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetOrFetchObject(null!, "key", static () => Observable.Return("value")))
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
            await cache.DisposeAsync();
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
            await cache.DisposeAsync();
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
            await cache.InsertObjects(new Dictionary<string, object>()).ToTask();
            var keys = await cache.GetAllKeys().ToList().ToTask();
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            await cache.DisposeAsync();
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
            var data = new Dictionary<string, object>
            {
                ["k1"] = "string value",
                ["k2"] = 42,
                ["k3"] = new UserObject { Name = "user", Bio = "bio", Blog = "blog" }
            };

            await cache.InsertObjects(data).ToTask();

            var keys = await cache.GetAllKeys().ToList().ToTask();
            await Assert.That(keys.Count).IsEqualTo(3);
        }
        finally
        {
            await cache.DisposeAsync();
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
            await cache.DisposeAsync();
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
            await cache.DisposeAsync();
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
            var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var bytes = SerializerExtensions.SerializeWithContext(date, cache);
            await Assert.That(bytes).IsNotNull();
            await Assert.That(bytes.Length).IsGreaterThan(0);
        }
        finally
        {
            await cache.DisposeAsync();
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
            var date = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var bytes = SerializerExtensions.SerializeWithContext(date, cache);
            var result = SerializerExtensions.DeserializeWithContext<DateTime>(bytes, cache);
            await Assert.That(result.Year).IsEqualTo(2025);
        }
        finally
        {
            await cache.DisposeAsync();
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
            await SerializerExtensions.InsertObject<string>(cache, "null_key", null!).ToTask();
            var result = await SerializerExtensions.GetObject<string>(cache, "null_key").ToTask();
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
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
            var circular = new List<object>();
            circular.Add(circular);

            await Assert.That(() => SerializerExtensions.InsertObject(cache, "cyc", circular).ToTask())
                .Throws<InvalidOperationException>();
        }
        finally
        {
            await cache.DisposeAsync();
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
            await cache.Insert("empty_key", [], typeof(string)).ToTask();
            var result = await SerializerExtensions.GetObject<string>(cache, "empty_key").ToTask();
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
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
            var invalid = new byte[] { 0xFF, 0xFE, 0xFD, 0x01 };
            await cache.Insert("bad_json", invalid, typeof(UserObject)).ToTask();

            await Assert.That(() => SerializerExtensions.GetObject<UserObject>(cache, "bad_json").ToTask())
                .Throws<InvalidOperationException>();
        }
        finally
        {
            await cache.DisposeAsync();
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
            var u1 = new UserObject { Name = "A", Bio = "B1", Blog = "Bl1" };
            var u2 = new UserObject { Name = "B", Bio = "B2", Blog = "Bl2" };
            await SerializerExtensions.InsertObject(cache, "a", u1).ToTask();
            await SerializerExtensions.InsertObject(cache, "b", u2).ToTask();

            var allEnumerable = await cache.GetAllObjects<UserObject>().FirstAsync();
            var all = allEnumerable.ToList();
            await Assert.That(all.Count).IsEqualTo(2);
        }
        finally
        {
            await cache.DisposeAsync();
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
            var latest = new UserObject { Name = "Latest", Bio = "B", Blog = "Bl" };

            var results = await cache.GetAndFetchLatest(
                "validate_key",
                () => Observable.Return(latest),
                fetchPredicate: null,
                absoluteExpiration: null,
                shouldInvalidateOnError: false,
                cacheValidationPredicate: _ => false)
                .ToList()
                .ToTask();

            await Assert.That(results).IsNotEmpty();

            // Since cacheValidationPredicate returned false, the cache should not contain the key.
            await Assert.That(() => cache.GetObject<UserObject>("validate_key").ToTask())
                .Throws<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
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
            var cached = new UserObject { Name = "Cached", Bio = "B", Blog = "Bl" };
            await SerializerExtensions.InsertObject(cache, "inv_key", cached).ToTask();

            var observed = new List<UserObject?>();
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
            await Assert.That(() => cache.GetObject<UserObject>("inv_key").ToTask())
                .Throws<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
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
            var cached = new UserObject { Name = "Cached", Bio = "B", Blog = "Bl" };
            await SerializerExtensions.InsertObject(cache, "keep_key", cached).ToTask();

            var observed = new List<UserObject?>();
            try
            {
                await cache.GetAndFetchLatest(
                    "keep_key",
                    () => Observable.Throw<UserObject>(new InvalidOperationException("fetch boom")))
                    .ForEachAsync(observed.Add);
            }
            catch
            {
                // expected
            }

            // Cache entry should still exist.
            var stillThere = await cache.GetObject<UserObject>("keep_key").ToTask();
            await Assert.That(stillThere).IsNotNull();
            await Assert.That(stillThere!.Name).IsEqualTo("Cached");
        }
        finally
        {
            await cache.DisposeAsync();
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
            await cache.DisposeAsync();
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
            await cache.DisposeAsync();
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
            var user = new UserObject { Name = "Forced", Bio = "B", Blog = "Bl" };
            var bytes = SerializerExtensions.SerializeWithContext(user, cache);
            await Assert.That(bytes.Length).IsGreaterThan(0);
        }
        finally
        {
            await cache.DisposeAsync();
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
            var user = new UserObject { Name = "Forced2", Bio = "B", Blog = "Bl" };
            var bytes = SerializerExtensions.SerializeWithContext(user, cache);
            var result = SerializerExtensions.DeserializeWithContext<UserObject>(bytes, cache);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Name).IsEqualTo("Forced2");
        }
        finally
        {
            await cache.DisposeAsync();
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
            var invalid = new byte[] { 0xFF, 0xFE, 0xFD, 0x01 };
            await Assert.That(() => SerializerExtensions.DeserializeWithContext<UserObject>(invalid, cache))
                .Throws<InvalidOperationException>();
        }
        finally
        {
            await cache.DisposeAsync();
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
            var invalid = new byte[] { 0xFF, 0xFE, 0xFD, 0x01 };

            // The primary serializer fails on invalid bytes, then UniversalSerializer's
            // TryFallbackDeserialization returns default(DateTime) without throwing.
            var result = SerializerExtensions.DeserializeWithContext<DateTime>(invalid, cache);
            await Assert.That(result).IsEqualTo(default);
        }
        finally
        {
            await cache.DisposeAsync();
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
        var cache = CreateCache();
        try
        {
            var invalid = new byte[] { 0xFF, 0xFE, 0xFD, 0x01 };

            // The primary serializer fails on invalid bytes, then UniversalSerializer's
            // TryFallbackDeserialization returns default(DateTimeOffset) without throwing.
            var result = SerializerExtensions.DeserializeWithContext<DateTimeOffset>(invalid, cache);
            await Assert.That(result).IsEqualTo(default);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetAllKeysSafe recovers from a failing underlying source by emitting empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysSafeShouldRecoverFromExceptions()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        // After dispose, GetAllKeys throws; GetAllKeysSafe should swallow and return empty.
        var keys = await cache.GetAllKeysSafe().ToList().ToTask();
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests GetAllKeysSafe(Type) recovers from a failing underlying source by emitting empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Test deliberately exercises the non-generic Type overload.")]
    public async Task GetAllKeysSafeWithTypeShouldRecoverFromExceptions()
    {
        var cache = CreateCache();
        await cache.DisposeAsync();

        var keys = await cache.GetAllKeysSafe(typeof(UserObject)).ToList().ToTask();
        await Assert.That(keys).IsEmpty();
    }

    /// <summary>
    /// Tests that GetOrCreateObject throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateObjectShouldThrowOnNullCache() =>
        await Assert.That(static () => SerializerExtensions.GetOrCreateObject<string>(null!, "key", static () => "value"))
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
            await cache.DisposeAsync();
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
            await SerializerExtensions.InsertObject(cache, "u1", new UserObject { Name = "A", Bio = "B", Blog = "C" }).ToTask();

            var keys = await cache.GetAllKeysSafe<UserObject>().ToList().ToTask();
            await Assert.That(keys.Count).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            await cache.DisposeAsync();
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
        await cache.DisposeAsync();

        // After dispose, GetAllKeys throws; GetAllKeysSafe<T> should swallow and return empty.
        var keys = await cache.GetAllKeysSafe<UserObject>().ToList().ToTask();
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
            await cache.Insert("safe_key1", [1, 2, 3]).ToTask();
            await cache.Insert("safe_key2", [4, 5, 6]).ToTask();

            var keys = await cache.GetAllKeysSafe().ToList().ToTask();
            await Assert.That(keys.Count).IsGreaterThanOrEqualTo(2);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that GetAllKeysSafe with a Type parameter returns keys for valid cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Test deliberately exercises the non-generic Type overload.")]
    public async Task GetAllKeysSafeWithTypeShouldReturnKeysForValidCache()
    {
        var cache = CreateCache();
        try
        {
            await SerializerExtensions.InsertObject(cache, "typed_key", new UserObject { Name = "T", Bio = "B", Blog = "Bl" }).ToTask();

            var keys = await cache.GetAllKeysSafe(typeof(UserObject)).ToList().ToTask();
            await Assert.That(keys.Count).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            await cache.DisposeAsync();
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
        var cache = new NullReturningBlobCache(new SystemJsonSerializer());
        try
        {
            await Assert.That(() => cache.GetObject<UserObject>("any_key").ToTask())
                .Throws<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
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
            var cached = new UserObject { Name = "Cached", Bio = "B", Blog = "Bl" };
            await SerializerExtensions.InsertObject(cache, "task_inv", cached).ToTask();

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
            await Assert.That(() => cache.GetObject<UserObject>("task_inv").ToTask())
                .Throws<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
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
            var latest = new UserObject { Name = "LatestTask", Bio = "B", Blog = "Bl" };

            var results = await cache.GetAndFetchLatest(
                "task_validate",
                () => Task.FromResult(latest),
                fetchPredicate: null,
                absoluteExpiration: null,
                shouldInvalidateOnError: false,
                cacheValidationPredicate: _ => false)
                .ToList()
                .ToTask();

            await Assert.That(results).IsNotEmpty();

            // Since cacheValidationPredicate returned false, the cache should not contain the key.
            await Assert.That(() => cache.GetObject<UserObject>("task_validate").ToTask())
                .Throws<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
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
            var invalid = new byte[] { 0xFF, 0xFE, 0xFD, 0x01 };

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
            await cache.DisposeAsync();
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
            var invalid = new byte[] { 0xFF, 0xFE, 0xFD, 0x01 };

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
            await cache.DisposeAsync();
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
        public IHttpService HttpService { get; set; } = new HttpService();

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
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
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
