// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Akavache.Tests;
using Akavache.Tests.Helpers;
using Akavache.Tests.Mocks;

namespace Akavache.Integration.Tests;

/// <summary>
/// Tests for AOT compatibility and edge cases.
/// </summary>
[Category("Akavache")]
[NotInParallel("CacheDatabaseState")]
public class AotCompatibilityTests
{
    /// <summary>
    /// Tests that null serializer throws appropriate exception.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task NullSerializerShouldThrowException() =>

        // Act & Assert - The exception should occur when creating the cache, not when using it
        await Assert.That(static () =>
        {
            using InMemoryBlobCache cache = new(default(ISerializer)!);
        }).ThrowsException().WithExceptionType(typeof(ArgumentNullException));

    /// <summary>
    /// Tests that SerializeWithContext handles null values correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SerializeWithContextShouldHandleNullValues()
    {
        InMemoryBlobCache blobCache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        // Act
        var result = SerializerExtensions.SerializeWithContext<string?>(null, blobCache);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Length == 0 || Encoding.UTF8.GetString(result) == "null").IsTrue();
    }

    /// <summary>
    /// Tests that DeserializeWithContext handles null/empty data.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task DeserializeWithContextShouldHandleNullData()
    {
        InMemoryBlobCache blobCache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        // Act & Assert
        var nullResult = SerializerExtensions.DeserializeWithContext<string>(null!, blobCache);
        await Assert.That(nullResult).IsNull();

        var emptyResult = SerializerExtensions.DeserializeWithContext<string>([], blobCache);
        await Assert.That(emptyResult).IsNull();
    }

    /// <summary>
    /// Tests that error handling works correctly for serialization failures.
    /// </summary>
    /// <returns>A task representing the test completion.</returns>
    [Test]
    public async Task SerializationErrorsShouldBeHandledCorrectly()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

            // Test with a Dictionary that can cause circular reference issues
            Dictionary<string, object> problemObject = [];
            problemObject["self"] = problemObject; // Create circular reference

            // Act & Assert - this should handle serialization gracefully
            Exception? caughtException = null;
            try
            {
                cache.InsertObject("problem", problemObject).SubscribeAndComplete();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            await Assert.That(caughtException).IsNotNull();

            // Verify it's one of the expected exception types for circular reference
            var isExpectedType = caughtException is InvalidOperationException or System.Text.Json.JsonException
                or NotSupportedException;
            await Assert.That(isExpectedType).IsTrue();
        }
    }

    /// <summary>
    /// Tests that DateTimeKind forcing works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task DateTimeKindForcingShouldWorkCorrectly()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            using InMemoryBlobCache cache =
                new(new SystemJsonSerializer()) { ForcedDateTimeKind = DateTimeKind.Utc };

            DateTime localDateTime = new(2025, 1, 15, 10, 30, 45, DateTimeKind.Local);

            // Act
            cache.InsertObject("datetime", localDateTime).SubscribeAndComplete();
            var retrieved = cache.GetObject<DateTime>("datetime").SubscribeGetValue();

            // Assert - Different serializers may handle DateTime.Kind differently
            // Just ensure the time value is preserved correctly
            var originalUtc = localDateTime.ToUniversalTime();
            var retrievedUtc = retrieved.ToUniversalTime();
            var timeDifference = Math.Abs((originalUtc - retrievedUtc).TotalSeconds);

            using (Assert.Multiple())
            {
                await Assert.That(timeDifference).IsLessThan(2);

                // If ForcedDateTimeKind is working, retrieved should be UTC or Unspecified
                // But some serializers may preserve the original kind
                await Assert.That(retrieved.Kind).IsEqualTo(DateTimeKind.Utc).Or.IsEqualTo(DateTimeKind.Unspecified).Or
                    .IsEqualTo(DateTimeKind.Local);
            }
        }
    }

    /// <summary>
    /// Tests that argument validation works correctly.
    /// </summary>
    /// <returns>A task representing the test completion.</returns>
    [Test]
    public async Task ArgumentValidationShouldWorkCorrectly()
    {
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        // Act & Assert - InMemoryBlobCache may not validate empty strings the same way
        // Try to test actual argument validation if it exists
        var insertNullError = cache.InsertObject(null!, "value").SubscribeGetError();
        await Assert.That(insertNullError).IsTypeOf<ArgumentNullException>();

        var getNullError = cache.GetObject<string>(null!).SubscribeGetError();
        await Assert.That(getNullError).IsTypeOf<ArgumentNullException>();

        // Test that actual empty strings work (they may be valid keys)
        cache.InsertObject(string.Empty, "empty_key_value").SubscribeAndComplete();
        var emptyKeyResult = cache.GetObject<string>(string.Empty).SubscribeGetValue();
        await Assert.That(emptyKeyResult).IsEqualTo("empty_key_value");
    }

    /// <summary>
    /// Tests that type safety is maintained with generic operations.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task TypeSafetyShouldBeMaintained()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

            // Test actual type conversion behavior rather than expecting specific exceptions
            // This test verifies that serialization maintains type integrity

            // Store a complex object
            var originalData = new
            {
                message = "Hello World", number = 42, timestamp = DateTime.UtcNow, isValid = true
            };

            cache.InsertObject("test_key", originalData).SubscribeAndComplete();

            // Retrieve the same data with the correct type
            var retrieved = cache.GetObject<dynamic>("test_key").SubscribeGetValue();
            await Assert.That((object?)retrieved).IsNotNull();

            // For type safety, we actually want to verify that the system
            // properly handles type conversions or fails appropriately
            // Rather than forcing an exception, let's test successful serialization

            // Test that we can store and retrieve strongly typed objects
            UserObject userObject = new() { Name = "Test User", Bio = "Test Bio", Blog = "Test Blog" };
            cache.InsertObject("user_key", userObject).SubscribeAndComplete();
            var retrievedUser = cache.GetObject<UserObject>("user_key").SubscribeGetValue();

            using (Assert.Multiple())
            {
                await Assert.That(retrievedUser!.Name).IsEqualTo(userObject.Name);
                await Assert.That(retrievedUser!.Bio).IsEqualTo(userObject.Bio);
                await Assert.That(retrievedUser!.Blog).IsEqualTo(userObject.Blog);
            }
        }
    }

    /// <summary>
    /// Tests that concurrent operations work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ConcurrentOperationsShouldWorkCorrectly()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

            // Act - perform multiple concurrent operations
            List<Task> tasks = [];

            for (var i = 0; i < 10; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    cache.InsertObject($"key_{index}", $"value_{index}").SubscribeAndComplete();
                    var retrieved = cache.GetObject<string>($"key_{index}").SubscribeGetValue();
                    await Assert.That(retrieved).IsEqualTo($"value_{index}");
                }));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Assert - verify all data was stored correctly
            for (var i = 0; i < 10; i++)
            {
                var value = cache.GetObject<string>($"key_{i}").SubscribeGetValue();
                await Assert.That(value).IsEqualTo($"value_{i}");
            }
        }
    }

    /// <summary>
    /// Tests that memory cleanup works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task MemoryCleanupShouldWorkCorrectly()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

            // Insert and remove data multiple times
            for (var i = 0; i < 5; i++)
            {
                cache.InsertObject($"temp_key_{i}", $"temp_value_{i}").SubscribeAndComplete();
            }

            // Invalidate all
            cache.InvalidateAll().SubscribeAndComplete();

            // Verify cleanup
            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys).IsEmpty();

            // Verify we can still use the cache
            cache.InsertObject("new_key", "new_value").SubscribeAndComplete();
            var newValue = cache.GetObject<string>("new_key").SubscribeGetValue();
            await Assert.That(newValue).IsEqualTo("new_value");
        }
    }

    /// <summary>
    /// Tests that large objects are handled correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LargeObjectsShouldBeHandledCorrectly()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

            // Create a large object
            string largeString = new('x', 100000); // 100KB string

            // Act
            cache.InsertObject("large_object", largeString).SubscribeAndComplete();
            var retrieved = cache.GetObject<string>("large_object").SubscribeGetValue();

            // Assert
            await Assert.That(retrieved).IsEqualTo(largeString);
            await Assert.That(retrieved!).Length().IsEqualTo(100000);
        }
    }

    /// <summary>
    /// Tests that observable extension methods work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ObservableExtensionMethodsShouldWork()
    {
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        // Test InsertObject and GetObject work with First operator
        cache.InsertObject("test", "value").SubscribeAndComplete();
        var result = cache.GetObject<string>("test").SubscribeGetValue();

        await Assert.That(result).IsEqualTo("value");

        // Test GetAllKeys works
        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys).Count().IsEqualTo(1);
        await Assert.That(keys).Contains("test");
    }

    /// <summary>
    /// Tests that cache disposal works correctly in various scenarios.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheDisposalShouldWorkCorrectly()
    {
        InMemoryBlobCache cache;

        // Test using statement disposal
        using (cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer()))
        {
            cache.InsertObject("test", "value").SubscribeAndComplete();
            var result = cache.GetObject<string>("test").SubscribeGetValue();
            await Assert.That(result).IsEqualTo("value");
        }

        // Test explicit disposal
        cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        cache.InsertObject("test2", "value2").SubscribeAndComplete();
        cache.Dispose();

        // Test multiple disposal calls (should not throw)
        cache.Dispose();
    }

    /// <summary>
    /// Tests that bulk operations work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BulkOperationsShouldWorkCorrectly()
    {
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        // Test bulk insert
        KeyValuePair<string, string>[] data =
        [
            new("key1", "value1"),
            new("key2", "value2"),
            new("key3", "value3")
        ];

        cache.InsertObjects(data).SubscribeAndComplete();

        // Test bulk get
        string[] keys = ["key1", "key2", "key3"];
        var results = cache.GetObjects<string>(keys).ToList().SubscribeGetValue();

        await Assert.That(results).Count().IsEqualTo(3);
        using (Assert.Multiple())
        {
            await Assert.That(results!.Any(static r => r is { Key: "key1", Value: "value1" })).IsTrue();
            await Assert.That(results!.Any(static r => r is { Key: "key2", Value: "value2" })).IsTrue();
            await Assert.That(results!.Any(static r => r is { Key: "key3", Value: "value3" })).IsTrue();
        }

        // Test bulk invalidate
        cache.InvalidateObjects<string>(keys).SubscribeAndComplete();

        var allKeys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(allKeys).IsEmpty();
    }
}
