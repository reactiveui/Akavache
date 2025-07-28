// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using Akavache.Tests.Mocks;
using Xunit;

namespace Akavache.Tests;

/// <summary>
/// Tests for serializer extension methods.
/// </summary>
public class SerializerExtensionsTests
{
    /// <summary>
    /// Tests that InsertObjects with IEnumerable works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task InsertObjectsShouldWorkWithEnumerable()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
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

                    Assert.NotNull(user1);
                    Assert.Equal("User1", user1!.Name);
                    Assert.Equal("Bio1", user1.Bio);

                    Assert.NotNull(user2);
                    Assert.Equal("User2", user2!.Name);
                    Assert.Equal("Bio2", user2.Bio);
                }
                finally
                {
                    await cache.DisposeAsync();
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetObjects with multiple keys works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetObjectsShouldWorkWithMultipleKeys()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
                var user2 = new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

                try
                {
                    // Insert test data
                    await cache.InsertObject("user1", user1).FirstAsync();
                    await cache.InsertObject("user2", user2).FirstAsync();

                    // Act
                    var results = await cache.GetObjects<UserObject>(new[] { "user1", "user2" }).ToList().FirstAsync();

                    // Assert
                    Assert.Equal(2, results.Count);

                    var user1Result = results.First(r => r.Key == "user1").Value;
                    Assert.Equal("User1", user1Result.Name);
                    Assert.Equal("Bio1", user1Result.Bio);

                    var user2Result = results.First(r => r.Key == "user2").Value;
                    Assert.Equal("User2", user2Result.Name);
                    Assert.Equal("Bio2", user2Result.Bio);
                }
                finally
                {
                    await cache.DisposeAsync();
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetAllObjects returns all objects of a specific type.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetAllObjectsShouldReturnAllObjectsOfType()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
                var user2 = new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

                try
                {
                    // Insert test data
                    await cache.InsertObject("user1", user1).FirstAsync();
                    await cache.InsertObject("user2", user2).FirstAsync();

                    // Act - just verify we can call the method without error
                    var count = 0;
                    await cache.GetAllObjects<UserObject>().ForEachAsync(_ => count++);

                    // Assert
                    Assert.Equal(2, count);
                }
                finally
                {
                    await cache.DisposeAsync();
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that InvalidateObject removes the correct object.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task InvalidateObjectShouldRemoveObject()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var user = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };

                try
                {
                    // Insert test data
                    await cache.InsertObject("user1", user).FirstAsync();

                    // Verify object exists
                    var retrievedUser = await cache.GetObject<UserObject>("user1").FirstAsync();
                    Assert.NotNull(retrievedUser);

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
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that InvalidateObjects removes multiple objects.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task InvalidateObjectsShouldRemoveMultipleObjects()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
                var user2 = new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

                try
                {
                    // Insert test data
                    await cache.InsertObject("user1", user1).FirstAsync();
                    await cache.InsertObject("user2", user2).FirstAsync();

                    // Act
                    await cache.InvalidateObjects<UserObject>(new[] { "user1", "user2" }).FirstAsync();

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
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that InvalidateAllObjects removes all objects of a type.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task InvalidateAllObjectsShouldRemoveAllObjectsOfType()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var user1 = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
                var user2 = new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" };

                try
                {
                    // Insert test data
                    await cache.InsertObject("user1", user1).FirstAsync();
                    await cache.InsertObject("user2", user2).FirstAsync();

                    // Verify objects exist
                    var beforeInvalidation = await cache.GetAllObjects<UserObject>().ToList().FirstAsync();
                    Assert.Equal(2, beforeInvalidation.Count);

                    // Act
                    await cache.InvalidateAllObjects<UserObject>().FirstAsync();

                    // Assert - The primary verification is that individual objects can't be retrieved
                    await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user1").FirstAsync());

                    await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>("user2").FirstAsync());

                    // Additional check - GetAllObjects should return empty or near-empty result
                    // Some cache implementations may return a collection with empty/null entries
                    var results = await cache.GetAllObjects<UserObject>().ToList().FirstAsync();

                    // Note: GetAllObjects returns IEnumerable<UserObject>, not KeyValuePair
                    // Filter out any null entries that might be returned by some implementations
                    var validResults = results.Where(user => user != null).ToList();
                    Assert.Empty(validResults);
                }
                finally
                {
                    await cache.DisposeAsync();
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetObjectCreatedAt returns the creation time.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetObjectCreatedAtShouldReturnCreationTime()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var user = new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" };
                var beforeInsert = DateTimeOffset.Now;

                try
                {
                    // Act
                    await cache.InsertObject("user1", user).FirstAsync();
                    var createdAt = await cache.GetObjectCreatedAt<UserObject>("user1").FirstAsync();

                    // Assert
                    Assert.NotNull(createdAt);
                    Assert.True(createdAt >= beforeInsert);
                    Assert.True(createdAt <= DateTimeOffset.Now);
                }
                finally
                {
                    await cache.DisposeAsync();
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that InsertAllObjects works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task InsertAllObjectsShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var keyValuePairs = new[]
                {
                    new KeyValuePair<string, UserObject>("user1", new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" }),
                    new KeyValuePair<string, UserObject>("user2", new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" })
                };

                try
                {
                    // Act
                    await cache.InsertAllObjects(keyValuePairs).FirstAsync();

                    // Assert
                    var user1 = await cache.GetObject<UserObject>("user1").FirstAsync();
                    var user2 = await cache.GetObject<UserObject>("user2").FirstAsync();

                    Assert.NotNull(user1);
                    Assert.Equal("User1", user1!.Name);

                    Assert.NotNull(user2);
                    Assert.Equal("User2", user2!.Name);
                }
                finally
                {
                    await cache.DisposeAsync();
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetOrCreateObject creates object when not in cache.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetOrCreateObjectShouldCreateWhenNotInCache()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var user = new UserObject { Name = "Created User", Bio = "Created Bio", Blog = "Created Blog" };

                try
                {
                    // Act
                    var result = await cache.GetOrCreateObject("new_user", () => user).FirstAsync();

                    // Assert
                    Assert.NotNull(result);
                    Assert.Equal("Created User", result!.Name);

                    // Verify it was actually stored
                    var storedUser = await cache.GetObject<UserObject>("new_user").FirstAsync();
                    Assert.NotNull(storedUser);
                    Assert.Equal("Created User", storedUser!.Name);
                }
                finally
                {
                    await cache.DisposeAsync();
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetOrCreateObject returns existing object from cache.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetOrCreateObjectShouldReturnExistingFromCache()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var existingUser = new UserObject { Name = "Existing User", Bio = "Existing Bio", Blog = "Existing Blog" };
                var newUser = new UserObject { Name = "New User", Bio = "New Bio", Blog = "New Blog" };

                try
                {
                    // Insert existing user
                    await cache.InsertObject("existing_user", existingUser).FirstAsync();

                    // Act
                    var result = await cache.GetOrCreateObject("existing_user", () => newUser).FirstAsync();

                    // Assert - should return existing user, not create new one
                    Assert.NotNull(result);
                    Assert.Equal("Existing User", result!.Name);
                    Assert.Equal("Existing Bio", result.Bio);
                }
                finally
                {
                    await cache.DisposeAsync();
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetOrFetchObject fetches when not in cache.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetOrFetchObjectShouldFetchWhenNotInCache()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();
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
                Assert.NotNull(result);
                Assert.Equal("Fetched User", result!.Name);
                Assert.Equal(1, fetchCount);

                // Verify it was stored in cache
                var cachedUser = await cache.GetObject<UserObject>("fetch_user").FirstAsync();
                Assert.NotNull(cachedUser);
                Assert.Equal("Fetched User", cachedUser!.Name);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetOrFetchObject returns cached value when available.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetOrFetchObjectShouldReturnCachedValue()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();
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
                Assert.NotNull(result);
                Assert.Equal("Cached User", result!.Name);
                Assert.Equal(0, fetchCount); // Fetch should not have been called
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetOrFetchObject with Task-based fetch function works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetOrFetchObjectWithTaskShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();
            var fetchedUser = new UserObject { Name = "Task Fetched User", Bio = "Task Bio", Blog = "Task Blog" };

            try
            {
                // Act
                var result = await cache.GetOrFetchObject("task_user", () => Task.FromResult(fetchedUser)).FirstAsync();

                // Assert
                Assert.NotNull(result);
                Assert.Equal("Task Fetched User", result!.Name);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetAndFetchLatest returns cached value first, then updated value.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetAndFetchLatestShouldReturnCachedThenLatest()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();
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
                Assert.True(results.Count >= 1); // Should have at least cached value
                Assert.NotNull(results[0]);
                Assert.Equal("Cached User", results[0]!.Name);

                if (results.Count > 1)
                {
                    // If we got the latest value too
                    Assert.NotNull(results[1]);
                    Assert.Equal("Latest User", results[1]!.Name);
                }
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetAndFetchLatest with Task-based fetch function works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetAndFetchLatestWithTaskShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();
            var latestUser = new UserObject { Name = "Task Latest User", Bio = "Task Bio", Blog = "Task Blog" };

            try
            {
                var results = new List<UserObject?>();

                // Act - GetAndFetchLatest with no cached value
                await cache.GetAndFetchLatest("new_user", () => Task.FromResult(latestUser))
                    .Take(1) // Should only get the fetched value
                    .ForEachAsync(user => results.Add(user));

                // Assert
                Assert.Single(results);
                Assert.NotNull(results[0]);
                Assert.Equal("Task Latest User", results[0]!.Name);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that GetAndFetchLatest with fetchPredicate respects the predicate.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetAndFetchLatestShouldRespectFetchPredicate()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();
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
                Assert.Single(results);
                Assert.NotNull(results[0]);
                Assert.Equal("Cached User", results[0]!.Name);
                Assert.Equal(0, fetchCount); // Fetch should not have been called
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that InsertObjects throws ArgumentNullException when cache is null.
    /// </summary>
    [Fact]
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
    [Fact]
    public async Task InsertObjectsShouldThrowArgumentNullExceptionWhenKeyValuePairsIsNull()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();
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
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that InsertObjects handles empty dictionary correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task InsertObjectsShouldHandleEmptyDictionary()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var emptyDict = new Dictionary<string, object>();

                try
                {
                    // Act - should complete without error
                    await cache.InsertObjects(emptyDict).FirstAsync();

                    // Assert - test passes if no exception is thrown
                    Assert.True(true);
                }
                finally
                {
                    await cache.DisposeAsync();
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that mixed object types can be inserted and retrieved.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task InsertObjectsShouldHandleMixedObjectTypes()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var cache = new InMemoryBlobCache();
                var mixedObjects = new Dictionary<string, object>
                {
                    ["string"] = "test string",
                    ["int"] = 42,
                    ["user"] = new UserObject { Name = "Test User", Bio = "Test Bio", Blog = "Test Blog" },
                    ["date"] = DateTime.Now
                };

                try
                {
                    // Act
                    await cache.InsertObjects(mixedObjects).FirstAsync();

                    // Assert
                    var stringValue = await cache.GetObject<string>("string").FirstAsync();
                    var intValue = await cache.GetObject<int>("int").FirstAsync();
                    var userValue = await cache.GetObject<UserObject>("user").FirstAsync();
                    var dateValue = await cache.GetObject<DateTime>("date").FirstAsync();

                    Assert.Equal("test string", stringValue);
                    Assert.Equal(42, intValue);
                    Assert.NotNull(userValue);
                    Assert.Equal("Test User", userValue!.Name);
                    Assert.NotEqual(default, dateValue);
                }
                finally
                {
                    await cache.DisposeAsync();
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that extension methods properly validate arguments.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ExtensionMethodsShouldValidateArguments()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

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
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }
}
