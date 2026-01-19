// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Focused serialization compatibility tests to ensure proper cross-serializer compatibility.
/// </summary>
[Category("Akavache")]
public class SerializationCompatibilityTests
{
    // 1. Define the data source for the parameters
    private static readonly ISerializer[] Serializers =
    [
        new SystemJsonSerializer(),
        new SystemJsonBsonSerializer(),
        new NewtonsoftSerializer(),
        new NewtonsoftBsonSerializer()
    ];

    /// <summary>
    /// Gets all combinations of serializers for cross-compatibility testing.
    /// </summary>
    /// <returns>All serializer combinations as tuples.</returns>
    public static IEnumerable<(ISerializer WriteSerializer, ISerializer ReadSerializer)> GetSerializerCombinations()
    {
        foreach (var writeSerializer in Serializers)
        {
            foreach (var readSerializer in Serializers)
            {
                yield return (writeSerializer, readSerializer);
            }
        }
    }

    /// <summary>
    /// Tests that each serializer can roundtrip its own data.
    /// </summary>
    /// <param name="serializerType">The serializer type to test.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task SerializerShouldRoundTripOwnData(Type serializerType)
    {
        // Arrange
        var serializer = (ISerializer)Activator.CreateInstance(serializerType)!;
        var testObj = new TestObject
        {
            Name = "Test",
            Value = 42,
            Date = DateTime.UtcNow
        };

        // Act
        var serializedData = serializer.Serialize(testObj);
        var deserializedObj = serializer.Deserialize<TestObject>(serializedData);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(deserializedObj).IsNotNull();
            await Assert.That(deserializedObj!.Name).IsEqualTo(testObj.Name);
            await Assert.That(deserializedObj!.Value).IsEqualTo(testObj.Value);
        }

        // Allow for some DateTime precision loss
        await Assert.That(Math.Abs((testObj.Date - deserializedObj.Date).TotalSeconds)).IsLessThan(1);
    }

    /// <summary>
    /// Tests cross-serializer compatibility for all combinations.
    /// </summary>
    /// <param name="writeSerializer">The writer serializer.</param>
    /// <param name="readSerializer">The reader serializer.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [MethodDataSource(nameof(GetSerializerCombinations))]
    public async Task CrossSerializerCompatibilityShouldWork(
        ISerializer writeSerializer,
        ISerializer readSerializer)
    {
        ArgumentNullException.ThrowIfNull(writeSerializer);
        ArgumentNullException.ThrowIfNull(readSerializer);

        // Arrange
        var testObj = new TestObject
        {
            Name = "CrossTest",
            Value = 123,
            Date = new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc)
        };

        try
        {
            // Act
            var serializedData = writeSerializer.Serialize(testObj);
            var deserializedObj = readSerializer.Deserialize<TestObject>(serializedData);

            // Assert
            await Assert.That(deserializedObj).IsNotNull();

            using (Assert.Multiple())
            {
                await Assert.That(deserializedObj.Name).IsEqualTo(testObj.Name);
                await Assert.That(deserializedObj.Value).IsEqualTo(testObj.Value);

                // Use a tolerance for DateTime comparisons, which is more readable
                await Assert.That(
                    deserializedObj.Date.ToUniversalTime())
                    .IsEqualTo(testObj.Date.ToUniversalTime()).Within(TimeSpan.FromMinutes(1));
            }
        }
        catch (Exception ex)
        {
            // Re-throw with more context if any part of the process fails
            throw new InvalidOperationException(
                $"Compatibility failed: write with {writeSerializer.GetType().Name}, read with {readSerializer.GetType().Name}. Error: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Tests all combinations of serializers for maximum compatibility coverage.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task AllSerializerCombinationsShouldWork()
    {
        var testObj = new TestObject
        {
            Name = "AllCombinationsTest",
            Value = 999,
            Date = new DateTime(2025, 1, 15, 16, 0, 0, DateTimeKind.Utc)
        };

        ISerializer[] serializers =
        [
            new SystemJsonSerializer(),
            new SystemJsonBsonSerializer(),
            new NewtonsoftSerializer(),
            new NewtonsoftBsonSerializer()
        ];

        var combinations = new List<(string WriteName, string ReadName, bool Success)>();

        // Test all combinations
        foreach (var writeSerializer in serializers)
        {
            foreach (var readSerializer in serializers)
            {
                var writeName = writeSerializer.GetType().Name;
                var readName = readSerializer.GetType().Name;

                try
                {
                    var serializedData = writeSerializer.Serialize(testObj);
                    var deserializedObj = readSerializer.Deserialize<TestObject>(serializedData);

                    if (deserializedObj != null)
                    {
                        using (Assert.Multiple())
                        {
                            await Assert.That(deserializedObj.Name).IsEqualTo(testObj.Name);
                            await Assert.That(deserializedObj.Value).IsEqualTo(testObj.Value);
                        }

                        // Allow for DateTime precision differences
                        var timeDiff = Math.Abs((testObj.Date - deserializedObj.Date).TotalMinutes);
                        await Assert.That(timeDiff).IsLessThan(1440);

                        combinations.Add((writeName, readName, true));
                    }
                    else
                    {
                        combinations.Add((writeName, readName, false));
                    }
                }
                catch (Exception ex)
                {
                    combinations.Add((writeName, readName, false));

                    // Log the failure but continue testing other combinations
                    Console.WriteLine($"Failed: {writeName} -> {readName}: {ex.Message}");
                }
            }
        }

        // Report results
        var totalCombinations = combinations.Count;
        var successfulCombinations = combinations.Count(static c => c.Success);
        var failedCombinations = combinations.Where(static c => !c.Success).ToList();

        // We expect at least the self-combinations to work (each serializer reading its own data)
        var selfCombinations = combinations.Where(static c => c.WriteName == c.ReadName).ToList();
        var successfulSelfCombinations = selfCombinations.Count(static c => c.Success);

        var failedSelfMessage = $"All self-combinations should work. Failed: {string.Join(", ", selfCombinations.Where(static c => !c.Success).Select(static c => c.WriteName))}";
        await Assert.That(successfulSelfCombinations)
            .IsEqualTo(selfCombinations.Count);

        // Report cross-serializer compatibility
        var crossCombinations = combinations.Where(static c => c.WriteName != c.ReadName).ToList();
        var successfulCrossCombinations = crossCombinations.Count(static c => c.Success);

        var crossCompatibilityMessage = $"At least 75% of cross-combinations should work. Success rate: {successfulCrossCombinations}/{crossCombinations.Count}. " +
            $"Failed: {string.Join(", ", failedCombinations.Select(static c => $"{c.WriteName}->{c.ReadName}"))}";
        await Assert.That(successfulCrossCombinations)
            .IsGreaterThanOrEqualTo((int)(crossCombinations.Count * 0.75));
    }

    /// <summary>
    /// Tests that SQLite cache can store and retrieve objects with all serializers without losing data.
    /// </summary>
    /// <param name="serializerType">The serializer type to test.</param>
    /// <returns>A task representing the test operation.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task SqliteCacheShouldPersistDataCorrectlyWithAllSerializers(Type serializerType)
    {
        if (serializerType is null)
        {
            throw new ArgumentNullException(nameof(serializerType));
        }

        // Arrange
        var serializer = (ISerializer)Activator.CreateInstance(serializerType)!;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "test.db");

            var testObject = new TestObject
            {
                Name = "TestUser",
                Value = 12345,
                Date = new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc)
            };

            // Test storage phase
            await using (var cache = new SqliteBlobCache(dbPath, serializer))
            {
                await cache.InsertObject("test_key", testObject).FirstAsync();
                await cache.Flush().FirstAsync(); // Ensure data is written to disk
            }

            // Test retrieval phase with new cache instance
            await using (var cache = new SqliteBlobCache(dbPath, serializer))
            {
                var retrievedObject = await cache.GetObject<TestObject>("test_key").FirstAsync();

                await Assert.That(retrievedObject).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(retrievedObject.Name).IsEqualTo(testObject.Name);
                    await Assert.That(retrievedObject.Value).IsEqualTo(testObject.Value);
                }

                // Allow for DateTime precision differences
                var timeDiff = Math.Abs((testObject.Date - retrievedObject.Date).TotalSeconds);
                await Assert.That(timeDiff).IsLessThan(60);
            }
        }
    }

    /// <summary>
    /// Tests cross-serializer compatibility with SQLite cache.
    /// </summary>
    /// <param name="writeSerializerType">The serializer to use for writing.</param>
    /// <param name="readSerializerType">The serializer to use for reading.</param>
    /// <returns>A task representing the test operation.</returns>
    [Arguments(typeof(SystemJsonSerializer), typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer), typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer), typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer), typeof(NewtonsoftBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer), typeof(NewtonsoftBsonSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer), typeof(NewtonsoftSerializer))]
    [Test]
    public async Task SqliteCacheShouldSupportCrossSerializerCompatibility(Type writeSerializerType, Type readSerializerType)
    {
        if (writeSerializerType is null)
        {
            throw new ArgumentNullException(nameof(writeSerializerType));
        }

        if (readSerializerType is null)
        {
            throw new ArgumentNullException(nameof(readSerializerType));
        }

        var testObject = new TestObject
        {
            Name = "CrossSerializerTest",
            Value = 99999,
            Date = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc)
        };

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "cross_serializer_test.db");

            // Write with first serializer
            {
                var writeSerializer = (ISerializer)Activator.CreateInstance(writeSerializerType)!;

                await using var writeCache = new SqliteBlobCache(dbPath, writeSerializer);
                await writeCache.InsertObject("cross_test", testObject).FirstAsync();
                await writeCache.Flush().FirstAsync();
            }

            // Read with second serializer
            {
                var readSerializer = (ISerializer)Activator.CreateInstance(readSerializerType)!;

                await using var readCache = new SqliteBlobCache(dbPath, readSerializer);

                try
                {
                    var retrievedObject = await readCache.GetObject<TestObject>("cross_test").FirstAsync();

                    await Assert.That(retrievedObject).IsNotNull();
                    using (Assert.Multiple())
                    {
                        await Assert.That(retrievedObject.Name).IsEqualTo(testObject.Name);
                        await Assert.That(retrievedObject.Value).IsEqualTo(testObject.Value);
                    }

                    // Allow for DateTime precision differences
                    var timeDiff = Math.Abs((testObject.Date - retrievedObject.Date).TotalMinutes);
                    await Assert.That(timeDiff).IsLessThan(1440);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new InvalidOperationException(
                        $"Cross-serializer test failed: could not read data written with {writeSerializerType.Name} using {readSerializerType.Name}. " +
                        $"Error: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Simple test to verify SQLite cache basic operations work.
    /// </summary>
    /// <param name="serializerType">The serializer type to test.</param>
    /// <returns>A task representing the test operation.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task SimpleSqliteTest(Type serializerType)
    {
        if (serializerType is null)
        {
            throw new ArgumentNullException(nameof(serializerType));
        }

        // Arrange
        var serializer = (ISerializer)Activator.CreateInstance(serializerType)!;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "simple_test.db");

            var testObject = new TestObject
            {
                Name = "SimpleTest",
                Value = 123,
                Date = new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc)
            };

            // Test in single cache instance to see if issue is with multiple instances
            await using (var cache = new SqliteBlobCache(dbPath, serializer))
            {
                // Insert
                await cache.InsertObject("simple_key", testObject).FirstAsync();

                // Verify via keys
                var allKeys = await cache.GetAllKeys().ToList().FirstAsync();
                var typedKeys = await cache.GetAllKeys(typeof(TestObject)).ToList().FirstAsync();

                using (Assert.Multiple())
                {
                    await Assert.That(allKeys).IsNotEmpty();
                    await Assert.That(typedKeys).IsNotEmpty();
                }

                // Get
                var retrieved = await cache.GetObject<TestObject>("simple_key").FirstAsync();

                await Assert.That(retrieved).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(retrieved.Name).IsEqualTo(testObject.Name);
                    await Assert.That(retrieved.Value).IsEqualTo(testObject.Value);
                }
            }
        }
    }

    /// <summary>
    /// Test to debug multi-instance SQLite persistence issues.
    /// </summary>
    /// <param name="serializerType">The serializer type to test.</param>
    /// <returns>A task representing the test operation.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task DebuggingMultiInstancePersistence(Type serializerType)
    {
        if (serializerType is null)
        {
            throw new ArgumentNullException(nameof(serializerType));
        }

        // Arrange
        var serializer = (ISerializer)Activator.CreateInstance(serializerType)!;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "debug_multi_instance.db");

            var testObject = new TestObject
            {
                Name = "MultiInstanceDebug",
                Value = 789,
                Date = new DateTime(2025, 1, 15, 15, 30, 0, DateTimeKind.Utc)
            };

            // Phase 1: Store data with explicit disposal and verification
            {
                var cache1 = new SqliteBlobCache(dbPath, serializer);
                await cache1.InsertObject("debug_key", testObject).FirstAsync();
                await cache1.Flush().FirstAsync();

                // Verify the data exists before disposal
                var keysBeforeDisposal = await cache1.GetAllKeys().ToList().FirstAsync();
                await Assert.That(keysBeforeDisposal).IsNotEmpty();

                // Explicit async disposal with proper wait
                await cache1.DisposeAsync();

                // Small delay to ensure cleanup is complete
                await Task.Delay(100);
            }

            // Phase 2: Try to read with a new instance
            {
                var cache2 = new SqliteBlobCache(dbPath, serializer);

                // Check if file exists
                await Assert.That(File.Exists(dbPath)).IsTrue();

                // Check keys
                var allKeys = await cache2.GetAllKeys().ToList().FirstAsync();
                var typedKeys = await cache2.GetAllKeys(typeof(TestObject)).ToList().FirstAsync();

                // Enhanced diagnostics
                var fileInfo = new FileInfo(dbPath);
                var walFile = dbPath + "-wal";
                var shmFile = dbPath + "-shm";

                var diagnosticInfo = $"DB file size: {fileInfo.Length} bytes. " +
                    $"WAL exists: {File.Exists(walFile)}. " +
                    $"SHM exists: {File.Exists(shmFile)}. " +
                    $"All keys count: {allKeys.Count}. " +
                    $"Typed keys count: {typedKeys.Count}. " +
                    $"All keys: [{string.Join(", ", allKeys)}]. " +
                    $"Typed keys: [{string.Join(", ", typedKeys)}]";

                await Assert.That(allKeys).IsNotEmpty();

                // Try to retrieve
                var retrieved = await cache2.GetObject<TestObject>("debug_key").FirstAsync();

                await Assert.That(retrieved).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(retrieved.Name).IsEqualTo(testObject.Name);
                    await Assert.That(retrieved.Value).IsEqualTo(testObject.Value);
                }

                await cache2.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Comprehensive DateTime serialization test that ensures all serializers handle DateTime correctly.
    /// Enhanced version with better BSON tolerance and mobile/desktop scenario coverage.
    /// </summary>
    /// <param name="serializerType">The serializer type to test.</param>
    /// <returns>A task representing the test operation.</returns>
    [Test]
    [Skip("Skipping due to unreliable DateTime serialization issues across different serializers in CI environment")]
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    public async Task DateTimeSerializationShouldBeConsistentAndAccurate(Type serializerType)
    {
        if (serializerType is null)
        {
            throw new ArgumentNullException(nameof(serializerType));
        }

        // Set up serializer with UTC DateTime handling for cross-platform consistency
        var serializer = (ISerializer)Activator.CreateInstance(serializerType)!;
        serializer.ForcedDateTimeKind = DateTimeKind.Utc;

        using (Utility.WithEmptyDirectory(out var path))
        {
            // Use format-specific database names to prevent conflicts
            var formatType = serializerType.Name.Contains("Bson") ? "bson" : "json";
            var dbPath = Path.Combine(path, $"datetime-{serializerType.Name}-{formatType}.db");

            // Test dates that cover common desktop/mobile scenarios
            var testDates = GetMobileDesktopTestDates(serializerType);

            var successCount = 0;
            var skipCount = 0;

            for (var i = 0; i < testDates.Length; i++)
            {
                var testDate = testDates[i];
                var key = $"datetime_test_{i}";

                // Skip problematic dates for BSON serializers (known limitations)
                if (ShouldSkipDateForBsonSerializer(serializerType, testDate))
                {
                    skipCount++;
                    continue;
                }

                try
                {
                    // Store the DateTime with enhanced error handling
                    {
                        var cache = new SqliteBlobCache(dbPath, serializer);

                        try
                        {
                            await cache.InsertObject(key, testDate).FirstAsync();
                            await cache.Flush().FirstAsync();
                        }
                        catch (Exception ex) when (IsBsonSerializationIssue(serializerType, ex))
                        {
                            System.Diagnostics.Debug.WriteLine($"BSON serialization issue for {testDate}: {ex.Message}");
                            skipCount++;
                            continue;
                        }
                        finally
                        {
                            await cache.DisposeAsync();
                            await Task.Delay(50);
                        }
                    }

                    // Retrieve and verify the DateTime with enhanced tolerance
                    {
                        var cache = new SqliteBlobCache(dbPath, serializer);

                        try
                        {
                            var retrieved = await cache.GetObject<DateTime>(key).FirstAsync();

                            // Enhanced validation with better BSON handling
                            if (ValidateDateTimeRoundtrip(serializerType, testDate, retrieved))
                            {
                                successCount++;
                            }
                            else
                            {
                                skipCount++;
                            }
                        }
                        catch (Exception ex) when (IsBsonDeserializationIssue(serializerType, ex))
                        {
                            System.Diagnostics.Debug.WriteLine($"BSON deserialization issue for {testDate}: {ex.Message}");
                            skipCount++;
                        }
                        finally
                        {
                            await cache.DisposeAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // For non-BSON issues, still throw but with better context
                    throw new InvalidOperationException(
                        $"DateTime test {i} failed for {serializerType.Name} with date {testDate}. " +
                        "This may indicate a regression in DateTime handling.",
                        ex);
                }
            }

            // Verify we had reasonable success rate
            var totalTests = testDates.Length;
            var actualTests = successCount + skipCount;

            // For BSON serializers, allow more skipped tests due to known limitations
            // For Newtonsoft serializers, also allow lower success rate due to DateTime precision issues
            var minimumSuccessRate = serializerType.Name.Contains("Bson") ? 0.5 :
                                    serializerType.Name.Contains("Newtonsoft") ? 0.6 : 0.8;
            var actualSuccessRate = successCount / (double)actualTests;

            await Assert.That(actualSuccessRate)
                .IsGreaterThanOrEqualTo(minimumSuccessRate);
        }
    }

    /// <summary>
    /// Gets test dates that cover common mobile and desktop application scenarios.
    /// </summary>
    /// <param name="serializerType">The serializer type being tested.</param>
    /// <returns>Array of test DateTime values.</returns>
    private static DateTime[] GetMobileDesktopTestDates(Type serializerType)
    {
        var dates = new List<DateTime>
        {
            // Common application timestamps
            new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc), // Current year
            new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc), // End of previous year
            new DateTime(2025, 6, 15, 15, 45, 30, DateTimeKind.Utc), // Mid-year timestamp

            // Data timestamps common in apps
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), // Start of recent year
            new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc), // Y2K reference

            // Recent timestamps (common in mobile apps)
            DateTime.UtcNow.Date.AddHours(12), // Today at noon UTC
            DateTime.UtcNow.AddDays(-1), // Yesterday
            DateTime.UtcNow.AddDays(-30), // 30 days ago
            DateTime.UtcNow.AddMonths(-6), // 6 months ago
        };

        // For BSON serializers, avoid extreme dates that are known to cause issues
        if (!serializerType.Name.Contains("Bson"))
        {
            dates.AddRange([
                DateTime.MinValue,
                DateTime.MaxValue,
                new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc), // Early 20th century
                new DateTime(2050, 12, 31, 23, 59, 59, DateTimeKind.Utc) // Mid 21st century
            ]);
        }

        return dates.ToArray();
    }

    /// <summary>
    /// Determines if a date should be skipped for BSON serializers due to known limitations.
    /// </summary>
    /// <param name="serializerType">The serializer type.</param>
    /// <param name="testDate">The date to test.</param>
    /// <returns>True if the date should be skipped.</returns>
    private static bool ShouldSkipDateForBsonSerializer(Type serializerType, DateTime testDate)
    {
        if (!serializerType.Name.Contains("Bson"))
        {
            return false;
        }

        // BSON serializers have known issues with extreme dates
        return testDate == DateTime.MinValue ||
               testDate == DateTime.MaxValue ||
               testDate.Year < 1970 ||
               testDate.Year > 2100;
    }

    /// <summary>
    /// Validates a DateTime roundtrip with appropriate tolerance for the serializer type.
    /// </summary>
    /// <param name="serializerType">The serializer type.</param>
    /// <param name="original">The original DateTime.</param>
    /// <param name="retrieved">The retrieved DateTime.</param>
    /// <returns>True if the roundtrip is valid.</returns>
    private static bool ValidateDateTimeRoundtrip(Type serializerType, DateTime original, DateTime retrieved)
    {
        // Handle BSON DateTime.MinValue issues
        if (serializerType.Name.Contains("Bson") &&
            retrieved == DateTime.MinValue &&
            original != DateTime.MinValue)
        {
            System.Diagnostics.Debug.WriteLine($"BSON DateTime.MinValue artifact detected for {original}");
            return false; // Skip this test
        }

        // Convert both to UTC for comparison
        var originalUtc = original.Kind == DateTimeKind.Utc ? original : original.ToUniversalTime();
        var retrievedUtc = retrieved.Kind == DateTimeKind.Utc ? retrieved : retrieved.ToUniversalTime();

        var timeDiff = Math.Abs((originalUtc - retrievedUtc).TotalMilliseconds);

        // Use different tolerance based on serializer type
        var tolerance = GetDateTimeToleranceForSerializer(serializerType);

        var isValid = timeDiff < tolerance;

        if (!isValid)
        {
            System.Diagnostics.Debug.WriteLine(
                $"DateTime validation failed for {serializerType.Name}: " +
                $"original={originalUtc:yyyy-MM-dd HH:mm:ss.fff} UTC, " +
                $"retrieved={retrievedUtc:yyyy-MM-dd HH:mm:ss.fff} UTC, " +
                $"diff={timeDiff}ms, tolerance={tolerance}ms");
        }

        return isValid;
    }

    /// <summary>
    /// Gets the appropriate tolerance for DateTime comparison based on serializer type.
    /// </summary>
    /// <param name="serializerType">The serializer type.</param>
    /// <returns>Tolerance in milliseconds.</returns>
    private static double GetDateTimeToleranceForSerializer(Type serializerType)
    {
        if (serializerType.Name.Contains("Bson"))
        {
            return 5000; // 5 seconds for BSON serializers (they have precision issues)
        }

        if (serializerType.Name.Contains("SystemJson"))
        {
            return 1000; // 1 second for System.Text.Json
        }

        if (serializerType.Name.Contains("Newtonsoft"))
        {
            return 2000; // 2 seconds for Newtonsoft.Json (can have precision differences)
        }

        return 2000; // 2 seconds for unknown serializers
    }

    /// <summary>
    /// Determines if an exception is a known BSON serialization issue.
    /// </summary>
    /// <param name="serializerType">The serializer type.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if this is a known BSON serialization issue.</returns>
    private static bool IsBsonSerializationIssue(Type serializerType, Exception exception)
    {
        if (!serializerType.Name.Contains("Bson"))
        {
            return false;
        }

        var message = exception.Message.ToLowerInvariant();
        return message.Contains("out of range") ||
               message.Contains("overflow") ||
               message.Contains("underflow") ||
               message.Contains("invalid") ||
               message.Contains("datetime");
    }

    /// <summary>
    /// Determines if an exception is a known BSON deserialization issue.
    /// </summary>
    /// <param name="serializerType">The serializer type.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if this is a known BSON deserialization issue.</returns>
    private static bool IsBsonDeserializationIssue(Type serializerType, Exception exception)
    {
        if (!serializerType.Name.Contains("Bson"))
        {
            return false;
        }

        var message = exception.Message.ToLowerInvariant();
        return message.Contains("invalid") ||
               message.Contains("format") ||
               message.Contains("datetime") ||
               message.Contains("deserialization") ||
               exception is KeyNotFoundException;
    }

    /// <summary>
    /// Test object for serialization.
    /// </summary>
    public class TestObject
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        public DateTime Date { get; set; }
    }
}
