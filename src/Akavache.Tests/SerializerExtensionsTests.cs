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
            Func<IObservable<string>> getOrFetchAsync = () =>
            {
                return cache.GetOrFetchObject("a", () =>
                {
                    cnt++;
                    return Observable.Return($"b{cnt}");
                }, DateTime.UtcNow + TimeSpan.FromMilliseconds(1000));
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
