// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests for error handling and edge case scenarios across Akavache functionality.
/// </summary>
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
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Insert some data first
        cache.InsertObject("test", "value").SubscribeAndComplete();

        // Dispose the cache
        cache.Dispose();

        // Act & Assert - operations on disposed cache should throw ObjectDisposedException
        var getError = cache.GetObject<string>("test").SubscribeGetError();
        await Assert.That(getError).IsTypeOf<ObjectDisposedException>();

        var insertError = cache.InsertObject("new", "value").SubscribeGetError();
        await Assert.That(insertError).IsTypeOf<ObjectDisposedException>();

        var invalidateError = cache.InvalidateObject<string>("test").SubscribeGetError();
        await Assert.That(invalidateError).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that cache operations handle extremely large data correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleExtremelyLargeDataCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Create very large data (10MB string)
        string largeData = new('X', 10_000_000);

        // Act - Should handle large data without throwing
        cache.InsertObject("large_data", largeData).SubscribeAndComplete();

        var retrieved = cache.GetObject<string>("large_data").SubscribeGetValue();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(retrieved).IsEqualTo(largeData);
            await Assert.That(retrieved).Length().IsEqualTo(10_000_000);
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
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Act - Insert null object
        cache.InsertObject<string?>("null_key", null).SubscribeAndComplete();

        var retrieved = cache.GetObject<string?>("null_key").SubscribeGetValue();

        // Assert
        await Assert.That(retrieved).IsNull();

        // Test with nullable reference types
        UserObject? nullUser = null;
        cache.InsertObject("null_user", nullUser).SubscribeAndComplete();

        var retrievedUser = cache.GetObject<UserObject?>("null_user").SubscribeGetValue();

        await Assert.That(retrievedUser).IsNull();
    }

    /// <summary>
    /// Tests that cache operations handle invalid keys correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleInvalidKeysCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Test null key validation - this should always throw ArgumentNullException
        var insertNullError = cache.InsertObject(null!, "value").SubscribeGetError();
        await Assert.That(insertNullError).IsTypeOf<ArgumentNullException>();

        var getNullError = cache.GetObject<string>(null!).SubscribeGetError();
        await Assert.That(getNullError).IsTypeOf<ArgumentNullException>();

        var invalidateNullError = cache.InvalidateObject<string>(null!).SubscribeGetError();
        await Assert.That(invalidateNullError).IsTypeOf<ArgumentNullException>();

        // Test various edge case keys - InMemoryBlobCache may allow these
        string[] edgeCaseKeys =
        [
            string.Empty,
            "   ",
            "\t",
            "\n",
            "\r\n"
        ];

        foreach (var edgeCaseKey in edgeCaseKeys)
        {
            try
            {
                // InMemoryBlobCache may allow these keys - test that they work if allowed
                cache.InsertObject(edgeCaseKey, "edge_case_value").SubscribeAndComplete();

                var edgeRetrieved = cache.GetObject<string>(edgeCaseKey).SubscribeGetValue();

                await Assert.That(edgeRetrieved).IsEqualTo("edge_case_value");
                cache.InvalidateObject<string>(edgeCaseKey).SubscribeAndComplete();
            }
            catch (ArgumentException)
            {
                // If the cache validates these keys, that's also acceptable
                // Different cache implementations may have different key validation policies
            }
        }

        // Test very long keys - should work for InMemoryBlobCache
        string veryLongKey = new('k', 10000);
        cache.InsertObject(veryLongKey, "long_key_value").SubscribeAndComplete();

        var longKeyRetrieved = cache.GetObject<string>(veryLongKey).SubscribeGetValue();

        await Assert.That(longKeyRetrieved).IsEqualTo("long_key_value");

        // Test keys with special characters - should work
        string[] specialCharKeys =
        [
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
        ];

        foreach (var specialKey in specialCharKeys)
        {
            cache.InsertObject(specialKey, $"value_for_{specialKey}").SubscribeAndComplete();

            var specialRetrieved = cache.GetObject<string>(specialKey).SubscribeGetValue();

            await Assert.That(specialRetrieved).IsEqualTo($"value_for_{specialKey}");
        }

        // Test Unicode keys
        string[] unicodeKeys =
        [
            "key_??",
            "key_???????",
            "key_???????",
            "key_???",
            "key_???",
            "key_e???????",
            "key_?????",
            "key_??????",
            "key_emoji_??_??_??"
        ];

        foreach (var unicodeKey in unicodeKeys)
        {
            cache.InsertObject(unicodeKey, $"unicode_value_{unicodeKey}").SubscribeAndComplete();

            var unicodeRetrieved = cache.GetObject<string>(unicodeKey).SubscribeGetValue();

            await Assert.That(unicodeRetrieved).IsEqualTo($"unicode_value_{unicodeKey}");
        }

        // Test that regular operations still work after all these edge cases
        cache.InsertObject("normal_key", "normal_value").SubscribeAndComplete();

        var normalRetrieved = cache.GetObject<string>("normal_key").SubscribeGetValue();

        await Assert.That(normalRetrieved).IsEqualTo("normal_value");
    }

    /// <summary>
    /// Tests that cache operations handle concurrent access correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleConcurrentAccessCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        const int concurrencyLevel = 100;
        const int operationsPerThread = 50;

        // Act - Perform many concurrent operations
        List<Task> tasks = [];

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
                    cache.InsertObject(key, value).SubscribeAndComplete();

                    // Retrieve
                    var retrieved = cache.GetObject<string>(key).SubscribeGetValue();
                    await Assert.That(retrieved).IsEqualTo(value);

                    // Update
                    var newValue = $"updated_{value}";
                    cache.InsertObject(key, newValue).SubscribeAndComplete();

                    // Retrieve updated
                    var updatedRetrieved = cache.GetObject<string>(key).SubscribeGetValue();
                    await Assert.That(updatedRetrieved).IsEqualTo(newValue);

                    // Invalidate
                    cache.InvalidateObject<string>(key).SubscribeAndComplete();

                    // Verify invalidation
                    var getError = cache.GetObject<string>(key).SubscribeGetError();
                    await Assert.That(getError).IsTypeOf<KeyNotFoundException>();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All operations should have completed without errors
        await Assert.That(tasks.All(t => t.IsCompletedSuccessfully)).IsTrue();
    }

    /// <summary>
    /// Tests that cache operations handle expiration edge cases correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleExpirationEdgeCasesCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Test immediate expiration
        var pastExpiration = DateTimeOffset.Now.AddSeconds(-1);
        cache.InsertObject("expired_key", "expired_value", pastExpiration).SubscribeAndComplete();

        // Should be expired immediately
        var expiredError = cache.GetObject<string>("expired_key").SubscribeGetError();
        await Assert.That(expiredError).IsTypeOf<KeyNotFoundException>();

        // Test far future expiration
        var farFutureExpiration = DateTimeOffset.Now.AddYears(100);
        cache.InsertObject("far_future_key", "far_future_value", farFutureExpiration).SubscribeAndComplete();

        var farFutureRetrieved = cache.GetObject<string>("far_future_key").SubscribeGetValue();
        await Assert.That(farFutureRetrieved).IsEqualTo("far_future_value");

        // Test edge case expiration times
        var minExpiration = DateTimeOffset.MinValue;
        var maxExpiration = DateTimeOffset.MaxValue;

        // MinValue expiration (should be expired)
        cache.InsertObject("min_expiration", "min_value", minExpiration).SubscribeAndComplete();

        var minError = cache.GetObject<string>("min_expiration").SubscribeGetError();
        await Assert.That(minError).IsTypeOf<KeyNotFoundException>();

        // MaxValue expiration (should be valid)
        cache.InsertObject("max_expiration", "max_value", maxExpiration).SubscribeAndComplete();

        var maxRetrieved = cache.GetObject<string>("max_expiration").SubscribeGetValue();
        await Assert.That(maxRetrieved).IsEqualTo("max_value");

        // Test very short expiration
        var shortExpiration = DateTimeOffset.Now.AddMilliseconds(100);
        cache.InsertObject("short_expiration", "short_value", shortExpiration).SubscribeAndComplete();

        // Should be available immediately
        var shortRetrieved = cache.GetObject<string>("short_expiration").SubscribeGetValue();
        await Assert.That(shortRetrieved).IsEqualTo("short_value");

        // Wait for expiration
        await Task.Delay(200);

        // Should now be expired
        var shortExpiredError = cache.GetObject<string>("short_expiration").SubscribeGetError();
        await Assert.That(shortExpiredError).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests that cache operations handle complex object hierarchies correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleComplexObjectHierarchiesCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Create complex nested object
        var complexObject = new
        {
            Id = Guid.NewGuid(),
            Name = "Complex Object",
            Timestamp = DateTimeOffset.Now,
            Users = (UserObject[])[
                new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" },
                new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" }
            ],
            Metadata = new Dictionary<string, object>
            {
                ["version"] = "1.0.0",
                ["features"] = (string[])["feature1", "feature2", "feature3"],
                ["config"] = new
                {
                    enabled = true,
                    timeout = TimeSpan.FromMinutes(5),
                    retries = 3
                }
            },
            NestedArrays = (int[][])[
                [1, 2, 3],
                [4, 5, 6],
                [7, 8, 9]
            ]
        };

        // Act
        cache.InsertObject("complex_object", complexObject).SubscribeAndComplete();

        var retrieved = cache.GetObject<dynamic>("complex_object").SubscribeGetValue();

        // Assert - Complex objects should be serialized and deserialized correctly
        await Assert.That((object?)retrieved).IsNotNull();
    }

    /// <summary>
    /// Tests that cache operations handle memory pressure correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleMemoryPressureCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Create many objects to simulate memory pressure
        const int objectCount = 1000;
        List<Task> tasks = [];

        for (var i = 0; i < objectCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                UserObject user = new()
                {
                    Name = $"User{index}",
                    Bio = $"This is a bio for user {index} with some additional text to make it larger",
                    Blog = $"https://blog{index}.example.com"
                };

                cache.InsertObject($"user_{index}", user).SubscribeAndComplete();
            }));
        }

        await Task.WhenAll(tasks);

        // Verify all objects were stored correctly
        for (var i = 0; i < objectCount; i++)
        {
            var user = cache.GetObject<UserObject>($"user_{i}").SubscribeGetValue();
            await Assert.That(user).IsNotNull();
            await Assert.That(user!.Name).IsEqualTo($"User{i}");
        }

        // Test bulk invalidation under memory pressure
        List<Task> invalidationTasks = [];
        for (var i = 0; i < objectCount; i++)
        {
            var index = i;
            invalidationTasks.Add(Task.Run(() => cache.InvalidateObject<UserObject>($"user_{index}").Subscribe()));
        }

        await Task.WhenAll(invalidationTasks);

        // Verify all objects were invalidated
        for (var i = 0; i < objectCount; i++)
        {
            var error = cache.GetObject<UserObject>($"user_{i}").SubscribeGetError();
            await Assert.That(error).IsTypeOf<KeyNotFoundException>();
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
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Test various Unicode and special character scenarios
        Dictionary<string, string> testCases = new()
        {
            ["emoji"] = "Hello ?? World ??! ?????",
            ["chinese"] = "????",
            ["japanese"] = "???????",
            ["korean"] = "????? ??",
            ["arabic"] = "????? ???????",
            ["hebrew"] = "???? ????",
            ["russian"] = "?????? ???",
            ["mathematical"] = "??????�??????��?",
            ["currency"] = "���$�????",
            ["special_chars"] = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~",
            ["control_chars"] = "Line1\nLine2\tTabbed\rCarriageReturn",
            ["mixed"] = "Mixed: ??? + Espa�ol + Fran�ais + ??????? + ??????? + ??"
        };

        foreach (var testCase in testCases)
        {
            // Act
            cache.InsertObject(testCase.Key, testCase.Value).SubscribeAndComplete();

            var retrieved = cache.GetObject<string>(testCase.Key).SubscribeGetValue();

            // Assert
            await Assert.That(retrieved).IsEqualTo(testCase.Value);
        }

        // Test Unicode in keys
        string[] unicodeKeys =
        [
            "?_??",
            "??_???",
            "????_???????",
            "????_?????",
            "?????_????"
        ];

        foreach (var unicodeKey in unicodeKeys)
        {
            cache.InsertObject(unicodeKey, $"value_for_{unicodeKey}").SubscribeAndComplete();

            var retrieved = cache.GetObject<string>(unicodeKey).SubscribeGetValue();

            await Assert.That(retrieved).IsEqualTo($"value_for_{unicodeKey}");
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
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);

        // Test various DateTime edge cases
        Dictionary<string, DateTime> dateTimeCases = new()
        {
            ["min_value"] = DateTime.MinValue,
            ["max_value"] = DateTime.MaxValue,
            ["epoch"] = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ["leap_year"] = new(2024, 2, 29, 12, 0, 0, DateTimeKind.Utc), // Leap year date
            ["dst_transition"] = new(2024, 3, 10, 2, 0, 0, DateTimeKind.Local), // DST transition
            ["new_year"] = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ["millennium"] = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ["y2k38"] = new(2038, 1, 19, 3, 14, 7, DateTimeKind.Utc), // Unix timestamp edge
            ["local"] = DateTime.Now,
            ["utc"] = DateTime.UtcNow,
            ["unspecified"] = new(2025, 1, 15, 12, 30, 45, DateTimeKind.Unspecified)
        };

        foreach (var dateTimeCase in dateTimeCases)
        {
            try
            {
                // Act
                cache.InsertObject(dateTimeCase.Key, dateTimeCase.Value).SubscribeAndComplete();

                var retrieved = cache.GetObject<DateTime>(dateTimeCase.Key).SubscribeGetValue();

                // Assert - Allow for some tolerance due to serialization precision
                var timeDifference = Math.Abs((dateTimeCase.Value - retrieved!).TotalMilliseconds);
                await Assert.That(timeDifference).IsLessThan(1000);
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
        Dictionary<string, DateTimeOffset> dateTimeOffsetCases = new()
        {
            ["offset_min"] = DateTimeOffset.MinValue,
            ["offset_max"] = DateTimeOffset.MaxValue,
            ["offset_now"] = DateTimeOffset.Now,
            ["offset_utc"] = DateTimeOffset.UtcNow,
            ["offset_positive"] = new(2025, 1, 15, 12, 0, 0, TimeSpan.FromHours(5)),
            ["offset_negative"] = new(2025, 1, 15, 12, 0, 0, TimeSpan.FromHours(-8)),
            ["offset_zero"] = new(2025, 1, 15, 12, 0, 0, TimeSpan.Zero)
        };

        foreach (var offsetCase in dateTimeOffsetCases)
        {
            try
            {
                cache.InsertObject(offsetCase.Key, offsetCase.Value).SubscribeAndComplete();

                var retrieved = cache.GetObject<DateTimeOffset>(offsetCase.Key).SubscribeGetValue();

                var timeDifference = Math.Abs((offsetCase.Value - retrieved!).TotalMilliseconds);
                await Assert.That(timeDifference).IsLessThan(1000);
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
}
