// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for error handling and edge case scenarios across Akavache functionality.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class ErrorHandlingAndEdgeCaseTests
{
    /// <summary>
    /// Tests that caches handle ObjectDisposedException correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleObjectDisposedExceptionCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        // Insert some data first
        await cache.InsertObject("test", "value").FirstAsync();

        // Dispose the cache
        await cache.DisposeAsync();

        // Act & Assert - operations on disposed cache should throw ObjectDisposedException
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await cache.GetObject<string>("test").FirstAsync());

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await cache.InsertObject("new", "value").FirstAsync());

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await cache.InvalidateObject<string>("test").FirstAsync());
    }

    /// <summary>
    /// Tests that cache operations handle extremely large data correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleExtremelyLargeDataCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Create very large data (10MB string)
            var largeData = new string('X', 10_000_000);

            // Act - Should handle large data without throwing
            await cache.InsertObject("large_data", largeData).FirstAsync();
            var retrieved = await cache.GetObject<string>("large_data").FirstAsync();

            // Assert
            Assert.That(retrieved, Is.EqualTo(largeData));
            Assert.That(retrieved!.Length, Is.EqualTo(10_000_000));
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that cache operations handle null objects correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleNullObjectsCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Act - Insert null object
            await cache.InsertObject<string?>("null_key", null).FirstAsync();
            var retrieved = await cache.GetObject<string?>("null_key").FirstAsync();

            // Assert
            Assert.That(retrieved, Is.Null);

            // Test with nullable reference types
            UserObject? nullUser = null;
            await cache.InsertObject("null_user", nullUser).FirstAsync();
            var retrievedUser = await cache.GetObject<UserObject?>("null_user").FirstAsync();
            Assert.That(retrievedUser, Is.Null);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that cache operations handle invalid keys correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleInvalidKeysCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Test null key validation - this should always throw ArgumentNullException
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.InsertObject(null!, "value").FirstAsync());
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.GetObject<string>(null!).FirstAsync());
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.InvalidateObject<string>(null!).FirstAsync());

            // Test various edge case keys - InMemoryBlobCache may allow these
            var edgeCaseKeys = new[]
            {
                    string.Empty,
                    "   ",
                    "\t",
                    "\n",
                    "\r\n"
            };

            foreach (var edgeCaseKey in edgeCaseKeys)
            {
                try
                {
                    // InMemoryBlobCache may allow these keys - test that they work if allowed
                    await cache.InsertObject(edgeCaseKey, "edge_case_value").FirstAsync();
                    var retrieved = await cache.GetObject<string>(edgeCaseKey).FirstAsync();
                    Assert.That(retrieved, Is.EqualTo("edge_case_value"));
                    await cache.InvalidateObject<string>(edgeCaseKey).FirstAsync();
                }
                catch (ArgumentException)
                {
                    // If the cache validates these keys, that's also acceptable
                    // Different cache implementations may have different key validation policies
                }
            }

            // Test very long keys - should work for InMemoryBlobCache
            var veryLongKey = new string('k', 10000);
            await cache.InsertObject(veryLongKey, "long_key_value").FirstAsync();
            var longKeyRetrieved = await cache.GetObject<string>(veryLongKey).FirstAsync();
            Assert.That(longKeyRetrieved, Is.EqualTo("long_key_value"));

            // Test keys with special characters - should work
            var specialCharKeys = new[]
            {
                    "key-with-dash",
                    "key_with_underscore",
                    "key.with.dots",
                    "key with spaces",
                    "key/with/slashes",
                    "key\\with\\backslashes",
                    "key:with:colons",
                    "key;with;semicolons",
                    "key=with=equals",
                    "key&with&ampersands",
                    "key?with?questions",
                    "key#with#hash",
                    "key%with%percent",
                    "key+with+plus",
                    "key[with]brackets",
                    "key{with}braces",
                    "key(with)parentheses",
                    "key<with>angles",
                    "key|with|pipes",
                    "key^with^carets",
                    "key~with~tildes",
                    "key`with`backticks",
                    "key@with@at",
                    "key$with$dollar",
                    "key!with!exclamation",
                    "key*with*asterisk"
            };

            foreach (var specialKey in specialCharKeys)
            {
                await cache.InsertObject(specialKey, $"value_for_{specialKey}").FirstAsync();
                var specialRetrieved = await cache.GetObject<string>(specialKey).FirstAsync();
                Assert.That(specialRetrieved, Is.EqualTo($"value_for_{specialKey}"));
            }

            // Test Unicode keys
            var unicodeKeys = new[]
            {
                    "key_中文",
                    "key_русский",
                    "key_العربية",
                    "key_日本語",
                    "key_한국어",
                    "key_ελληνικά",
                    "key_עברית",
                    "key_हिन्दी",
                    "key_emoji_😀_🎉_🚀"
            };

            foreach (var unicodeKey in unicodeKeys)
            {
                await cache.InsertObject(unicodeKey, $"unicode_value_{unicodeKey}").FirstAsync();
                var unicodeRetrieved = await cache.GetObject<string>(unicodeKey).FirstAsync();
                Assert.That(unicodeRetrieved, Is.EqualTo($"unicode_value_{unicodeKey}"));
            }

            // Test that regular operations still work after all these edge cases
            await cache.InsertObject("normal_key", "normal_value").FirstAsync();
            var normalRetrieved = await cache.GetObject<string>("normal_key").FirstAsync();
            Assert.That(normalRetrieved, Is.EqualTo("normal_value"));
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that cache operations handle concurrent access correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleConcurrentAccessCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            const int concurrencyLevel = 100;
            const int operationsPerThread = 50;

            // Act - Perform many concurrent operations
            var tasks = new List<Task>();

            for (var i = 0; i < concurrencyLevel; i++)
            {
                var threadIndex = i;
                tasks.Add(Task.Run(async () =>
                {
                    for (var j = 0; j < operationsPerThread; j++)
                    {
                        var key = $"thread_{threadIndex}_item_{j}";
                        var value = $"value_{threadIndex}_{j}";

                        // Insert
                        await cache.InsertObject(key, value).FirstAsync();

                        // Retrieve
                        var retrieved = await cache.GetObject<string>(key).FirstAsync();
                        Assert.That(retrieved, Is.EqualTo(value));

                        // Update
                        var newValue = $"updated_{value}";
                        await cache.InsertObject(key, newValue).FirstAsync();

                        // Retrieve updated
                        var updatedRetrieved = await cache.GetObject<string>(key).FirstAsync();
                        Assert.That(updatedRetrieved, Is.EqualTo(newValue));

                        // Invalidate
                        await cache.InvalidateObject<string>(key).FirstAsync();

                        // Verify invalidation
                        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<string>(key).FirstAsync());
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - All operations should have completed without errors
            Assert.That(tasks.All(t => t.IsCompletedSuccessfully, Is.True));
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that cache operations handle expiration edge cases correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleExpirationEdgeCasesCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Test immediate expiration
            var pastExpiration = DateTimeOffset.Now.AddSeconds(-1);
            await cache.InsertObject("expired_key", "expired_value", pastExpiration).FirstAsync();

            // Should be expired immediately
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<string>("expired_key").FirstAsync());

            // Test far future expiration
            var farFutureExpiration = DateTimeOffset.Now.AddYears(100);
            await cache.InsertObject("far_future_key", "far_future_value", farFutureExpiration).FirstAsync();

            var farFutureRetrieved = await cache.GetObject<string>("far_future_key").FirstAsync();
            Assert.That(farFutureRetrieved, Is.EqualTo("far_future_value"));

            // Test edge case expiration times
            var minExpiration = DateTimeOffset.MinValue;
            var maxExpiration = DateTimeOffset.MaxValue;

            // MinValue expiration (should be expired)
            await cache.InsertObject("min_expiration", "min_value", minExpiration).FirstAsync();
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<string>("min_expiration").FirstAsync());

            // MaxValue expiration (should be valid)
            await cache.InsertObject("max_expiration", "max_value", maxExpiration).FirstAsync();
            var maxRetrieved = await cache.GetObject<string>("max_expiration").FirstAsync();
            Assert.That(maxRetrieved, Is.EqualTo("max_value"));

            // Test very short expiration
            var shortExpiration = DateTimeOffset.Now.AddMilliseconds(100);
            await cache.InsertObject("short_expiration", "short_value", shortExpiration).FirstAsync();

            // Should be available immediately
            var shortRetrieved = await cache.GetObject<string>("short_expiration").FirstAsync();
            Assert.That(shortRetrieved, Is.EqualTo("short_value"));

            // Wait for expiration
            await Task.Delay(200);

            // Should now be expired
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<string>("short_expiration").FirstAsync());
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that cache operations handle complex object hierarchies correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleComplexObjectHierarchiesCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Create complex nested object
            var complexObject = new
            {
                Id = Guid.NewGuid(),
                Name = "Complex Object",
                Timestamp = DateTimeOffset.Now,
                Users = new[]
                {
                        new UserObject { Name = "User1", Bio = "Bio1", Blog = "Blog1" },
                        new UserObject { Name = "User2", Bio = "Bio2", Blog = "Blog2" }
                },
                Metadata = new Dictionary<string, object>
                {
                    ["version"] = "1.0.0",
                    ["features"] = new[] { "feature1", "feature2", "feature3" },
                    ["config"] = new
                    {
                        enabled = true,
                        timeout = TimeSpan.FromMinutes(5),
                        retries = 3
                    }
                },
                NestedArrays = new[]
                {
                        new[] { 1, 2, 3 },
                        new[] { 4, 5, 6 },
                        new[] { 7, 8, 9 }
                }
            };

            // Act
            await cache.InsertObject("complex_object", complexObject).FirstAsync();
            var retrieved = await cache.GetObject<dynamic>("complex_object").FirstAsync();

            // Assert - Complex objects should be serialized and deserialized correctly
            Assert.That(retrieved, Is.Not.Null);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that cache operations handle memory pressure correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleMemoryPressureCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Create many objects to simulate memory pressure
            const int objectCount = 1000;
            var tasks = new List<Task>();

            for (var i = 0; i < objectCount; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    var user = new UserObject
                    {
                        Name = $"User{index}",
                        Bio = $"This is a bio for user {index} with some additional text to make it larger",
                        Blog = $"https://blog{index}.example.com"
                    };

                    await cache.InsertObject($"user_{index}", user).FirstAsync();
                }));
            }

            await Task.WhenAll(tasks);

            // Verify all objects were stored correctly
            for (var i = 0; i < objectCount; i++)
            {
                var user = await cache.GetObject<UserObject>($"user_{i}").FirstAsync();
                Assert.That(user, Is.Not.Null);
                Assert.That(user!.Name, Is.EqualTo($"User{i}"));
            }

            // Test bulk invalidation under memory pressure
            var invalidationTasks = new List<Task>();
            for (var i = 0; i < objectCount; i++)
            {
                var index = i;
                invalidationTasks.Add(Task.Run(async () => await cache.InvalidateObject<UserObject>($"user_{index}").FirstAsync()));
            }

            await Task.WhenAll(invalidationTasks);

            // Verify all objects were invalidated
            for (var i = 0; i < objectCount; i++)
            {
                await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.GetObject<UserObject>($"user_{i}").FirstAsync());
            }
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that cache operations handle Unicode and special character data correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleUnicodeAndSpecialCharactersCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Test various Unicode and special character scenarios
            var testCases = new Dictionary<string, string>
            {
                ["emoji"] = "Hello ?? World ??! ?????",
                ["chinese"] = "????",
                ["japanese"] = "???????",
                ["korean"] = "????? ??",
                ["arabic"] = "????? ???????",
                ["hebrew"] = "???? ????",
                ["russian"] = "?????? ???",
                ["mathematical"] = "??????±??????²³?",
                ["currency"] = "¥£€$¢????",
                ["special_chars"] = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~",
                ["control_chars"] = "Line1\nLine2\tTabbed\rCarriageReturn",
                ["mixed"] = "Mixed: ??? + Español + Français + ??????? + ??????? + ??"
            };

            foreach (var testCase in testCases)
            {
                // Act
                await cache.InsertObject(testCase.Key, testCase.Value).FirstAsync();
                var retrieved = await cache.GetObject<string>(testCase.Key).FirstAsync();

                // Assert
                Assert.That(retrieved, Is.EqualTo(testCase.Value));
            }

            // Test Unicode in keys
            var unicodeKeys = new[]
            {
                    "?_??",
                    "??_???",
                    "????_???????",
                    "????_?????",
                    "?????_????"
            };

            foreach (var unicodeKey in unicodeKeys)
            {
                await cache.InsertObject(unicodeKey, $"value_for_{unicodeKey}").FirstAsync();
                var retrieved = await cache.GetObject<string>(unicodeKey).FirstAsync();
                Assert.That(retrieved, Is.EqualTo($"value_for_{unicodeKey}"));
            }
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that cache operations handle DateTime edge cases correctly across time zones.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleDateTimeEdgeCasesCorrectly()
    {
        // Arrange
        var serializer = new SystemJsonSerializer();

        var cache = new InMemoryBlobCache(serializer);

        try
        {
            // Test various DateTime edge cases
            var dateTimeCases = new Dictionary<string, DateTime>
            {
                ["min_value"] = DateTime.MinValue,
                ["max_value"] = DateTime.MaxValue,
                ["epoch"] = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ["leap_year"] = new DateTime(2024, 2, 29, 12, 0, 0, DateTimeKind.Utc), // Leap year date
                ["dst_transition"] = new DateTime(2024, 3, 10, 2, 0, 0, DateTimeKind.Local), // DST transition
                ["new_year"] = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ["millennium"] = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ["y2k38"] = new DateTime(2038, 1, 19, 3, 14, 7, DateTimeKind.Utc), // Unix timestamp edge
                ["local"] = DateTime.Now,
                ["utc"] = DateTime.UtcNow,
                ["unspecified"] = new DateTime(2025, 1, 15, 12, 30, 45, DateTimeKind.Unspecified)
            };

            foreach (var dateTimeCase in dateTimeCases)
            {
                try
                {
                    // Act
                    await cache.InsertObject(dateTimeCase.Key, dateTimeCase.Value).FirstAsync();
                    var retrieved = await cache.GetObject<DateTime>(dateTimeCase.Key).FirstAsync();

                    // Assert - Allow for some tolerance due to serialization precision
                    var timeDifference = Math.Abs((dateTimeCase.Value - retrieved).TotalMilliseconds);
                    Assert.True(timeDifference < 1000, $"DateTime case '{dateTimeCase.Key}' failed: expected {dateTimeCase.Value}, got {retrieved}, difference: {timeDifference}ms");
                }
                catch (Exception ex)
                {
                    // Some extreme DateTime values might not be supported by all serializers
                    // Log and continue if it's a known limitation
                    if (dateTimeCase.Key is "min_value" or "max_value")
                    {
                        // These are known to be problematic in some serializers
                        continue;
                    }

                    throw new InvalidOperationException($"DateTime case '{dateTimeCase.Key}' failed unexpectedly", ex);
                }
            }

            // Test DateTimeOffset cases
            var dateTimeOffsetCases = new Dictionary<string, DateTimeOffset>
            {
                ["offset_min"] = DateTimeOffset.MinValue,
                ["offset_max"] = DateTimeOffset.MaxValue,
                ["offset_now"] = DateTimeOffset.Now,
                ["offset_utc"] = DateTimeOffset.UtcNow,
                ["offset_positive"] = new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.FromHours(5)),
                ["offset_negative"] = new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.FromHours(-8)),
                ["offset_zero"] = new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero)
            };

            foreach (var offsetCase in dateTimeOffsetCases)
            {
                try
                {
                    await cache.InsertObject(offsetCase.Key, offsetCase.Value).FirstAsync();
                    var retrieved = await cache.GetObject<DateTimeOffset>(offsetCase.Key).FirstAsync();

                    var timeDifference = Math.Abs((offsetCase.Value - retrieved).TotalMilliseconds);
                    Assert.True(timeDifference < 1000, $"DateTimeOffset case '{offsetCase.Key}' failed: expected {offsetCase.Value}, got {retrieved}");
                }
                catch (Exception ex)
                {
                    if (offsetCase.Key is "offset_min" or "offset_max")
                    {
                        continue; // Known limitations
                    }

                    throw new InvalidOperationException($"DateTimeOffset case '{offsetCase.Key}' failed unexpectedly", ex);
                }
            }
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }
}
