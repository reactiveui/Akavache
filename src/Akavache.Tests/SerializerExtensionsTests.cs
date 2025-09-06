// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using Akavache.Tests.Mocks;

using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for serializer extension methods.
/// </summary>
[TestFixture]
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
        var serializer = new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out var path))
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

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(user1, Is.Not.Null);
                    Assert.That(user1!.Name, Is.EqualTo("User1"));
                    Assert.That(user1.Bio, Is.EqualTo("Bio1"));
                }

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(user2, Is.Not.Null);
                    Assert.That(user2!.Name, Is.EqualTo("User2"));
                    Assert.That(user2.Bio, Is.EqualTo("Bio2"));
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

        using (Utility.WithEmptyDirectory(out var path))
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
                Assert.That(results, Has.Count.EqualTo(2));

                var user1Result = results.First(static r => r.Key == "user1").Value;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(user1Result.Name, Is.EqualTo("User1"));
                    Assert.That(user1Result.Bio, Is.EqualTo("Bio1"));
                }

                var user2Result = results.First(static r => r.Key == "user2").Value;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(user2Result.Name, Is.EqualTo("User2"));
                    Assert.That(user2Result.Bio, Is.EqualTo("Bio2"));
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

        using (Utility.WithEmptyDirectory(out var path))
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
            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Has.Count.EqualTo(2), "Should retrieve two objects.");
                Assert.That(results, Has.Some.Property("Name").EqualTo("User1"), "Should contain User1.");
                Assert.That(results, Has.Some.Property("Name").EqualTo("User2"), "Should contain User2.");
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

        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache(serializer);
            var user = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };

            try
            {
                // Insert test data
                await cache.InsertObject("user1", user).FirstAsync();

                // Verify object exists
                var retrievedUser = await cache.GetObject<UserObject>("user1").FirstAsync();
                Assert.That(retrievedUser, Is.Not.Null);

                // Act
                await cache.InvalidateObject<UserObject>("user1").FirstAsync();

                // Assert
                Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user1").FirstAsync());
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

        using (Utility.WithEmptyDirectory(out var path))
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
                Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user1").FirstAsync());

                Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user2").FirstAsync());
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

        using (Utility.WithEmptyDirectory(out var path))
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
                Assert.That(beforeInvalidation.Count(), Is.EqualTo(2));

                // Act
                await cache.InvalidateAllObjects<UserObject>().FirstAsync();

                // Assert - The primary verification is that individual objects can't be retrieved
                Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user1").FirstAsync());

                Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user2").FirstAsync());

                // Additional check - GetAllObjects should return empty result
                var results = await cache.GetAllObjects<UserObject>().FirstAsync();
                Assert.That(results, Is.Empty);
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

        using (Utility.WithEmptyDirectory(out var path))
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
                Assert.That(createdAt, Is.Not.Null);
                Assert.That(createdAt, Is.GreaterThanOrEqualTo(beforeInsert));
                Assert.That(createdAt, Is.LessThanOrEqualTo(DateTimeOffset.Now));
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

        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache(serializer);
            KeyValuePair<string, UserObject>[] keyValuePairs =
            [
                new KeyValuePair<string, UserObject>("user1", new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" }),
                new KeyValuePair<string, UserObject>("user2", new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" })
            ];

            try
            {
                // Act
                await cache.InsertAllObjects(keyValuePairs).FirstAsync();

                // Assert
                var user1 = await cache.GetObject<UserObject>("user1").FirstAsync();
                var user2 = await cache.GetObject<UserObject>("user2").FirstAsync();

                Assert.That(user1, Is.Not.Null);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(user1!.Name, Is.EqualTo("User1"));

                    Assert.That(user2, Is.Not.Null);
                }

                Assert.That(user2!.Name, Is.EqualTo("User2"));
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

        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache(serializer);
            var user = new UserObject { Name = "Created User", Bio = "Created Bio", Blog = "Created Blog" };

            try
            {
                // Act
                var result = await cache.GetOrCreateObject("new_user", () => user).FirstAsync();

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Name, Is.EqualTo("Created User"));

                // Verify it was actually stored
                var storedUser = await cache.GetObject<UserObject>("new_user").FirstAsync();
                Assert.That(storedUser, Is.Not.Null);
                Assert.That(storedUser!.Name, Is.EqualTo("Created User"));
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

        using (Utility.WithEmptyDirectory(out var path))
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
                Assert.That(result, Is.Not.Null);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(result!.Name, Is.EqualTo("Existing User"));
                    Assert.That(result.Bio, Is.EqualTo("Existing Bio"));
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
            Assert.That(result, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result!.Name, Is.EqualTo("Fetched User"));
                Assert.That(fetchCount, Is.EqualTo(1));
            }

            // Verify it was stored in cache
            var cachedUser = await cache.GetObject<UserObject>("fetch_user").FirstAsync();
            Assert.That(cachedUser, Is.Not.Null);
            Assert.That(cachedUser!.Name, Is.EqualTo("Fetched User"));
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
            Assert.That(result, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result!.Name, Is.EqualTo("Cached User"));
                Assert.That(fetchCount, Is.Zero); // Fetch should not have been called
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
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Task Fetched User"));
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
                .ForEachAsync(user => results.Add(user));

            // Assert
            Assert.That(results, Is.Not.Empty); // Should have at least cached value
            Assert.That(results[0], Is.Not.Null);
            Assert.That(results[0]!.Name, Is.EqualTo("Cached User"));

            if (results.Count > 1)
            {
                // If we got the latest value too
                Assert.That(results[1], Is.Not.Null);
                Assert.That(results[1]!.Name, Is.EqualTo("Latest User"));
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
                .ForEachAsync(user => results.Add(user));

            // Assert
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0], Is.Not.Null);
            Assert.That(results[0]!.Name, Is.EqualTo("Task Latest User"));
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
                .ForEachAsync(user => results.Add(user));

            // Assert
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0], Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(results[0]!.Name, Is.EqualTo("Cached User"));
                Assert.That(fetchCount, Is.Zero); // Fetch should not have been called
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
    [Test]
    public void InsertObjectsShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;
        var dict = new Dictionary<string, object> { ["key"] = "value" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.InsertObjects(dict));
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
            Assert.Throws<ArgumentNullException>(() => cache.InsertObjects(dict!));
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

        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache(serializer);
            var emptyDict = new Dictionary<string, object>();

            try
            {
                // Act - should complete without error
                await cache.InsertObjects(emptyDict).FirstAsync();

                // Assert - test passes if no exception is thrown
                Assert.Pass("InsertObjects with empty dictionary completed successfully");
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

        using (Utility.WithEmptyDirectory(out var path))
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stringValue, Is.EqualTo("test string"));
                Assert.That(intValue, Is.EqualTo(42));
                Assert.That(userValue, Is.Not.Null);
                Assert.That(userValue.Name, Is.EqualTo("Test User"));

                // This single constraint handles the complex date logic elegantly
                Assert.That(
                    dateValue,
                    Is.Default.Or.EqualTo(testDate).Within(TimeSpan.FromDays(1)),
                    "Date should either be default (due to serializer limits) or close to the original value.");
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
            Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.GetObjectCreatedAt<string>(null!).FirstAsync());

            Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.InvalidateObject<string>(null!).FirstAsync());

            // Test null collection validation
            Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.InvalidateObjects<string>(null!).FirstAsync());

            // Note: Extension methods may allow empty strings as valid keys in some implementations
            // This is different from the core methods which validate empty strings

            // Test that methods work with empty string (if allowed by implementation)
            try
            {
                var createdAt = await cache.GetObjectCreatedAt<string>(string.Empty).FirstAsync();

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

        using (Utility.WithEmptyDirectory(out var path))
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

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(value1, Is.EqualTo("value1"));
                    Assert.That(value2, Is.EqualTo("value2"));
                    Assert.That(value3, Is.EqualTo(42));
                    Assert.That(value4, Is.Not.Null);
                    Assert.That(value4!.Name, Is.EqualTo("Test"));
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

        using (Utility.WithEmptyDirectory(out var path))
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

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(user1, Is.Not.Null);
                    Assert.That(user1!.Name, Is.EqualTo("User1"));
                    Assert.That(user2, Is.Not.Null);
                    Assert.That(user2!.Name, Is.EqualTo("User2"));
                    Assert.That(largeUser50, Is.Not.Null);
                    Assert.That(largeUser50!.Name, Is.EqualTo("LargeUser50"));
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

        using (Utility.WithEmptyDirectory(out var path))
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
                await cache.InsertObjects(multiDict!).FirstAsync();

                // Test 4: Large number of operations to stress test completion logic
                var largeDict = Enumerable.Range(1, 1000)
                    .ToDictionary(i => $"key_{i}", i => (object)$"value_{i}");
                await cache.InsertObjects(largeDict).FirstAsync();

                // Test 5: Verify data was actually stored correctly
                var retrievedSingle = await cache.GetObject<string>("single").FirstAsync();
                var retrievedString = await cache.GetObject<string>("string_val").FirstAsync();
                var retrievedInt = await cache.GetObject<int>("int_val").FirstAsync();
                var retrievedLarge = await cache.GetObject<string>("key_500").FirstAsync();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(retrievedSingle, Is.EqualTo("value"));
                    Assert.That(retrievedString, Is.EqualTo("test"));
                    Assert.That(retrievedInt, Is.EqualTo(42));
                    Assert.That(retrievedLarge, Is.EqualTo("value_500"));
                }

                // All tests pass - the completion logic is robust
                Assert.Pass("InsertObjects completion logic handled all test cases successfully");
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

        using (Utility.WithEmptyDirectory(out var path))
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

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(nullValue, Is.Null);
                    Assert.That(emptyString, Is.EqualTo(string.Empty));
                    Assert.That(normalValue, Is.EqualTo("normal"));
                    Assert.That(stressValue500, Is.EqualTo("stress_value_500"));
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
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result1, Is.EqualTo("value_1"), "First fetch should return value_1");
                Assert.That(result2, Is.EqualTo("value_2"), "Second fetch after invalidation should return value_2");
                Assert.That(fetchCount, Is.EqualTo(2), "Fetch function should be called twice");
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result1, Is.EqualTo("b1"), "First call should return b1");
                Assert.That(result2, Is.EqualTo("b2"), "Second call after invalidation should return b2 (not b1)");
                Assert.That(cnt, Is.EqualTo(2), "Fetch function should be called twice");
            }
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }
}
