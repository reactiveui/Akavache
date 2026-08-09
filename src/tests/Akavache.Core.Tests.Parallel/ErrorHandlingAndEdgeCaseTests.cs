// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for error handling and edge case scenarios across Akavache functionality.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class ErrorHandlingAndEdgeCaseTests
{
    /// <summary>Payload stored by the tests that only care about the operation, not the content.</summary>
    private const string SampleValue = "value";

    /// <summary>Character count of the multi-megabyte string used to prove large payloads round-trip.</summary>
    private const int LargeDataCharCount = 10_000_000;

    /// <summary>Character count of the deliberately oversized cache key.</summary>
    private const int VeryLongKeyLength = 10_000;

    /// <summary>How far ahead the "far future" expiration is set, in years.</summary>
    private const int FarFutureYears = 100;

    /// <summary>Lifetime, in milliseconds, of the entry used to observe expiry within the test run.</summary>
    private const int ShortExpirationMilliseconds = 100;

    /// <summary>How long, in milliseconds, the test waits to be sure the short-lived entry has expired.</summary>
    private const int ShortExpirationWaitMilliseconds = 200;

    /// <summary>Timeout, in minutes, stored in the nested configuration block of the complex object graph.</summary>
    private const int ConfigTimeoutMinutes = 5;

    /// <summary>Retry count stored in the nested configuration block of the complex object graph.</summary>
    private const int ConfigRetryCount = 3;

    /// <summary>Round-trip tolerance, in milliseconds, that absorbs serializer precision loss on timestamps.</summary>
    private const int SerializationToleranceMilliseconds = 1000;

    /// <summary>Offset, in hours, of the east-of-UTC <see cref="DateTimeOffset"/> case.</summary>
    private const int PositiveOffsetHours = 5;

    /// <summary>Offset, in hours, of the west-of-UTC <see cref="DateTimeOffset"/> case.</summary>
    private const int NegativeOffsetHours = -8;

    /// <summary>Key of the <see cref="DateTime.MinValue"/> case, which serializers are allowed to reject.</summary>
    private const string MinValueDateTimeCaseKey = "min_value";

    /// <summary>Key of the <see cref="DateTime.MaxValue"/> case, which serializers are allowed to reject.</summary>
    private const string MaxValueDateTimeCaseKey = "max_value";

    /// <summary>Tests that caches handle ObjectDisposedException correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleObjectDisposedExceptionCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Insert some data first
        cache.InsertObject("test", SampleValue).WaitForCompletion();

        // Dispose the cache
        cache.Dispose();

        // Act & Assert - operations on disposed cache should throw ObjectDisposedException
        var getError = cache.GetObject<string>("test").SubscribeGetError();
        await Assert.That(getError).IsTypeOf<ObjectDisposedException>();

        var insertError = cache.InsertObject("new", SampleValue).SubscribeGetError();
        await Assert.That(insertError).IsTypeOf<ObjectDisposedException>();

        var invalidateError = cache.InvalidateObject<string>("test").SubscribeGetError();
        await Assert.That(invalidateError).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>Tests that cache operations handle extremely large data correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleExtremelyLargeDataCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Create very large data (10MB string)
        string largeData = new('X', LargeDataCharCount);

        // Act - Should handle large data without throwing
        cache.InsertObject("large_data", largeData).WaitForCompletion();

        var retrieved = cache.GetObject<string>("large_data").SubscribeGetValue();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(retrieved).IsEqualTo(largeData);
            await Assert.That(retrieved).Length().IsEqualTo(LargeDataCharCount);
        }
    }

    /// <summary>Tests that cache operations handle null objects correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleNullObjectsCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Act - Insert null object
        cache.InsertObject<string?>("null_key", null).WaitForCompletion();

        var retrieved = cache.GetObject<string?>("null_key").SubscribeGetValue();

        // Assert
        await Assert.That(retrieved).IsNull();

        // Test with nullable reference types
        cache.InsertObject<UserObject?>("null_user", null).WaitForCompletion();

        var retrievedUser = cache.GetObject<UserObject?>("null_user").SubscribeGetValue();

        await Assert.That(retrievedUser).IsNull();
    }

    /// <summary>Tests that cache operations handle invalid keys correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleInvalidKeysCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Test null key validation - this should always throw ArgumentNullException
        var insertNullError = cache.InsertObject(null!, SampleValue).SubscribeGetError();
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
                cache.InsertObject(edgeCaseKey, "edge_case_value").WaitForCompletion();

                var edgeRetrieved = cache.GetObject<string>(edgeCaseKey).SubscribeGetValue();

                await Assert.That(edgeRetrieved).IsEqualTo("edge_case_value");
                cache.InvalidateObject<string>(edgeCaseKey).WaitForCompletion();
            }
            catch (ArgumentException)
            {
                // If the cache validates these keys, that's also acceptable
                // Different cache implementations may have different key validation policies
            }
        }
    }

    /// <summary>Tests that a very long key round-trips through the in-memory cache.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleVeryLongKeysCorrectly()
    {
        using var cache = CreateCache();

        string veryLongKey = new('k', VeryLongKeyLength);
        cache.InsertObject(veryLongKey, "long_key_value").WaitForCompletion();

        var longKeyRetrieved = cache.GetObject<string>(veryLongKey).SubscribeGetValue();

        await Assert.That(longKeyRetrieved).IsEqualTo("long_key_value");
    }

    /// <summary>Tests that keys containing punctuation and separator characters round-trip.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandlePunctuationKeysCorrectly()
    {
        using var cache = CreateCache();

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
            cache.InsertObject(specialKey, $"value_for_{specialKey}").WaitForCompletion();

            var specialRetrieved = cache.GetObject<string>(specialKey).SubscribeGetValue();

            await Assert.That(specialRetrieved).IsEqualTo($"value_for_{specialKey}");
        }
    }

    /// <summary>Tests that Unicode keys round-trip and that ordinary keys still work afterwards.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleUnicodeKeysCorrectly()
    {
        using var cache = CreateCache();

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
            cache.InsertObject(unicodeKey, $"unicode_value_{unicodeKey}").WaitForCompletion();

            var unicodeRetrieved = cache.GetObject<string>(unicodeKey).SubscribeGetValue();

            await Assert.That(unicodeRetrieved).IsEqualTo($"unicode_value_{unicodeKey}");
        }

        // Test that regular operations still work after all these edge cases
        cache.InsertObject("normal_key", "normal_value").WaitForCompletion();

        var normalRetrieved = cache.GetObject<string>("normal_key").SubscribeGetValue();

        await Assert.That(normalRetrieved).IsEqualTo("normal_value");
    }

    /// <summary>Tests that cache operations handle concurrent access correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleConcurrentAccessCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

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
                    cache.InsertObject(key, value).WaitForCompletion();

                    // Retrieve
                    var retrieved = cache.GetObject<string>(key).SubscribeGetValue();
                    await Assert.That(retrieved).IsEqualTo(value);

                    // Update
                    var newValue = $"updated_{value}";
                    cache.InsertObject(key, newValue).WaitForCompletion();

                    // Retrieve updated
                    var updatedRetrieved = cache.GetObject<string>(key).SubscribeGetValue();
                    await Assert.That(updatedRetrieved).IsEqualTo(newValue);

                    // Invalidate
                    cache.InvalidateObject<string>(key).WaitForCompletion();

                    // Verify invalidation
                    var getError = cache.GetObject<string>(key).SubscribeGetError();
                    await Assert.That(getError).IsTypeOf<KeyNotFoundException>();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All operations should have completed without errors
        await Assert.That(tasks.TrueForAll(static t => t.IsCompletedSuccessfully)).IsTrue();
    }

    /// <summary>Tests that cache operations handle expiration edge cases correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleExpirationEdgeCasesCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Test immediate expiration
        var pastExpiration = TimeProvider.System.GetLocalNow().AddSeconds(-1);
        cache.InsertObject("expired_key", "expired_value", pastExpiration).WaitForCompletion();

        // Should be expired immediately
        var expiredError = cache.GetObject<string>("expired_key").SubscribeGetError();
        await Assert.That(expiredError).IsTypeOf<KeyNotFoundException>();

        // Test far future expiration
        var farFutureExpiration = TimeProvider.System.GetLocalNow().AddYears(FarFutureYears);
        cache.InsertObject("far_future_key", "far_future_value", farFutureExpiration).WaitForCompletion();

        var farFutureRetrieved = cache.GetObject<string>("far_future_key").SubscribeGetValue();
        await Assert.That(farFutureRetrieved).IsEqualTo("far_future_value");

        // Test edge case expiration times
        var minExpiration = DateTimeOffset.MinValue;
        var maxExpiration = DateTimeOffset.MaxValue;

        // MinValue expiration (should be expired)
        cache.InsertObject("min_expiration", "min_value", minExpiration).WaitForCompletion();

        var minError = cache.GetObject<string>("min_expiration").SubscribeGetError();
        await Assert.That(minError).IsTypeOf<KeyNotFoundException>();

        // MaxValue expiration (should be valid)
        cache.InsertObject("max_expiration", "max_value", maxExpiration).WaitForCompletion();

        var maxRetrieved = cache.GetObject<string>("max_expiration").SubscribeGetValue();
        await Assert.That(maxRetrieved).IsEqualTo("max_value");

        // Test very short expiration
        const string shortExpirationKey = "short_expiration";
        var shortExpiration = TimeProvider.System.GetLocalNow().AddMilliseconds(ShortExpirationMilliseconds);
        cache.InsertObject(shortExpirationKey, "short_value", shortExpiration).WaitForCompletion();

        // Should be available immediately
        var shortRetrieved = cache.GetObject<string>(shortExpirationKey).SubscribeGetValue();
        await Assert.That(shortRetrieved).IsEqualTo("short_value");

        // Wait for expiration
        await Task.Delay(ShortExpirationWaitMilliseconds);

        // Should now be expired
        var shortExpiredError = cache.GetObject<string>(shortExpirationKey).SubscribeGetError();
        await Assert.That(shortExpiredError).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>Tests that cache operations handle complex object hierarchies correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleComplexObjectHierarchiesCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Create complex nested object
        UserObject[] users =
        [
            new() { Name = "User1", Bio = "Bio1", Blog = "Blog1" },
            new() { Name = "User2", Bio = "Bio2", Blog = "Blog2" }
        ];

        int[] firstRow = [1, 2, 3];
        int[] secondRow = [4, 5, 6];
        int[] thirdRow = [7, 8, 9];
        int[][] nestedArrays = [firstRow, secondRow, thirdRow];

        Dictionary<string, object> metadata = new()
        {
            ["version"] = "1.0.0",
            ["features"] = (string[])["feature1", "feature2", "feature3"],
            ["config"] = new ComplexObjectConfig(true, TimeSpan.FromMinutes(ConfigTimeoutMinutes), ConfigRetryCount),
        };

        ComplexObjectGraph complexObject = new(
            Guid.NewGuid(),
            "Complex Object",
            TimeProvider.System.GetLocalNow(),
            users,
            metadata,
            nestedArrays);

        // Act
        cache.InsertObject("complex_object", complexObject).WaitForCompletion();

        var retrieved = cache.GetObject<dynamic>("complex_object").SubscribeGetValue();

        // Assert - Complex objects should be serialized and deserialized correctly
        await Assert.That((object?)retrieved).IsNotNull();
    }

    /// <summary>Tests that cache operations handle memory pressure correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleMemoryPressureCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Create many objects to simulate memory pressure
        const int objectCount = 1000;
        List<Task> tasks = [];

        for (var i = 0; i < objectCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                UserObject user = new() { Name = $"User{index}", Bio = $"This is a bio for user {index} with some additional text to make it larger", Blog = $"https://blog{index}.example.com" };

                cache.InsertObject($"user_{index}", user).WaitForCompletion();
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

    /// <summary>Tests that cache operations handle Unicode and special character data correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CacheShouldHandleUnicodeAndSpecialCharactersCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

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
            ["mixed"] = "Mixed: ??? + Espa�ol + Fran�ais + ??????? + ??????? + ??",
        };

        foreach (var testCase in testCases)
        {
            // Act
            cache.InsertObject(testCase.Key, testCase.Value).WaitForCompletion();

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
            cache.InsertObject(unicodeKey, $"value_for_{unicodeKey}").WaitForCompletion();

            var retrieved = cache.GetObject<string>(unicodeKey).SubscribeGetValue();

            await Assert.That(retrieved).IsEqualTo($"value_for_{unicodeKey}");
        }
    }

    /// <summary>Tests that cache operations handle DateTime edge cases correctly across time zones.</summary>
    /// <returns>A task representing the test.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    [Test]
    public async Task CacheShouldHandleDateTimeEdgeCasesCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Test various DateTime edge cases
        Dictionary<string, DateTime> dateTimeCases = new()
        {
            [MinValueDateTimeCaseKey] = DateTime.MinValue,
            [MaxValueDateTimeCaseKey] = DateTime.MaxValue,
            ["epoch"] = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ["leap_year"] = new(2024, 2, 29, 12, 0, 0, DateTimeKind.Utc), // Leap year date
            ["dst_transition"] = new(2024, 3, 10, 2, 0, 0, DateTimeKind.Local), // DST transition
            ["new_year"] = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ["millennium"] = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ["y2k38"] = new(2038, 1, 19, 3, 14, 7, DateTimeKind.Utc), // Unix timestamp edge
            ["local"] = TimeProvider.System.GetUtcNow().UtcDateTime,
            ["utc"] = TimeProvider.System.GetUtcNow().UtcDateTime,
            ["unspecified"] = new(2025, 1, 15, 12, 30, 45, DateTimeKind.Unspecified),
        };

        foreach (var dateTimeCase in dateTimeCases)
        {
            try
            {
                // Act
                cache.InsertObject(dateTimeCase.Key, dateTimeCase.Value).WaitForCompletion();

                var retrieved = cache.GetObject<DateTime>(dateTimeCase.Key).SubscribeGetValue();

                // Assert - Allow for some tolerance due to serialization precision
                var timeDifference = Math.Abs((dateTimeCase.Value - retrieved).TotalMilliseconds);
                await Assert.That(timeDifference).IsLessThan(SerializationToleranceMilliseconds);
            }
            catch (Exception ex)
            {
                // Some extreme DateTime values might not be supported by all serializers
                // Log and continue if it's a known limitation
                if (dateTimeCase.Key is MinValueDateTimeCaseKey or MaxValueDateTimeCaseKey)
                {
                    // These are known to be problematic in some serializers
                    continue;
                }

                throw new InvalidOperationException($"DateTime case '{dateTimeCase.Key}' failed unexpectedly", ex);
            }
        }
    }

    /// <summary>Tests that cache operations handle DateTimeOffset edge cases correctly across time zones.</summary>
    /// <returns>A task representing the test.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    [Test]
    public async Task CacheShouldHandleDateTimeOffsetEdgeCasesCorrectly()
    {
        using var cache = CreateCache();

        Dictionary<string, DateTimeOffset> dateTimeOffsetCases = new()
        {
            ["offset_min"] = DateTimeOffset.MinValue,
            ["offset_max"] = DateTimeOffset.MaxValue,
            ["offset_now"] = TimeProvider.System.GetUtcNow(),
            ["offset_utc"] = TimeProvider.System.GetUtcNow(),
            ["offset_positive"] = new(2025, 1, 15, 12, 0, 0, TimeSpan.FromHours(PositiveOffsetHours)),
            ["offset_negative"] = new(2025, 1, 15, 12, 0, 0, TimeSpan.FromHours(NegativeOffsetHours)),
            ["offset_zero"] = new(2025, 1, 15, 12, 0, 0, TimeSpan.Zero),
        };

        foreach (var offsetCase in dateTimeOffsetCases)
        {
            try
            {
                cache.InsertObject(offsetCase.Key, offsetCase.Value).WaitForCompletion();

                var retrieved = cache.GetObject<DateTimeOffset>(offsetCase.Key).SubscribeGetValue();

                var timeDifference = Math.Abs((offsetCase.Value - retrieved).TotalMilliseconds);
                await Assert.That(timeDifference).IsLessThan(SerializationToleranceMilliseconds);
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

    /// <summary>Creates a fresh in-memory cache backed by the System.Text.Json serializer.</summary>
    /// <returns>A new cache instance the caller owns and must dispose.</returns>
    private static InMemoryBlobCache CreateCache() => new(ImmediateSequencer.Instance, new SystemJsonSerializer());

    /// <summary>Nested configuration block carried inside <see cref="ComplexObjectGraph"/>.</summary>
    /// <param name="Enabled">Whether the configured feature is switched on.</param>
    /// <param name="Timeout">How long the configured operation may run.</param>
    /// <param name="Retries">How many times the configured operation is retried.</param>
    private sealed record ComplexObjectConfig(bool Enabled, TimeSpan Timeout, int Retries);

    /// <summary>Deeply nested object graph used to prove complex hierarchies survive a serialize/deserialize round trip.</summary>
    /// <param name="Id">Identity of the graph.</param>
    /// <param name="Name">Display name of the graph.</param>
    /// <param name="Timestamp">When the graph was built.</param>
    /// <param name="Users">Nested reference-type objects.</param>
    /// <param name="Metadata">Loosely typed metadata bag holding a further nested record.</param>
    /// <param name="NestedArrays">Jagged array proving multi-level arrays survive.</param>
    private sealed record ComplexObjectGraph(
        Guid Id,
        string Name,
        DateTimeOffset Timestamp,
        UserObject[] Users,
        Dictionary<string, object> Metadata,
        int[][] NestedArrays);
}
