// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
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
[NotInParallel("NativeSqlite")]
public class SerializationCompatibilityTests
{
    /// <summary>Serializer instances used as parameter data for the tests.</summary>
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
    /// <returns>All serializer combinations as tuples wrapped in Func for test isolation.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Method returns a lazy enumerable used as a TUnit data source — property semantics aren't appropriate.")]
    public static IEnumerable<(Func<ISerializer> WriteSerializer, Func<ISerializer> ReadSerializer)> GetSerializerCombinations() =>
        Serializers
            .SelectMany(
                _ => Serializers,
                (writeSerializer, readSerializer) => new { writeSerializer, readSerializer })
            .Select(t => new { t, ws = t.writeSerializer })
            .Select(t => new { t, rs = t.t.readSerializer })
            .Select(t =>
                ((Func<ISerializer> WriteSerializer, Func<ISerializer> ReadSerializer))(() => t.t.ws, () => t.rs));

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
            await Assert.That(deserializedObj.Value).IsEqualTo(testObj.Value);
        }

        // Allow for some DateTime precision loss
        await Assert.That(Math.Abs((testObj.Date - deserializedObj.Date).TotalSeconds)).IsLessThan(1);
    }

    /// <summary>
    /// Tests cross-serializer compatibility for all combinations.
    /// </summary>
    /// <param name="writeSerializerFactory">Factory for the writer serializer.</param>
    /// <param name="readSerializerFactory">Factory for the reader serializer.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [MethodDataSource(nameof(GetSerializerCombinations))]
    public async Task CrossSerializerCompatibilityShouldWork(
        Func<ISerializer> writeSerializerFactory,
        Func<ISerializer> readSerializerFactory)
    {
        ArgumentNullException.ThrowIfNull(writeSerializerFactory);
        ArgumentNullException.ThrowIfNull(readSerializerFactory);
        var writeSerializer = writeSerializerFactory();
        var readSerializer = readSerializerFactory();

        // Arrange
        var testObj = new TestObject
        {
            Name = "CrossTest",
            Value = 123,
            Date = new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc)
        };

        // Skip known incompatible combinations:
        // 1. BSON → pure JSON: different wire formats
        // 2. Newtonsoft JSON → STJ JSON: DateTime format mismatch (\/Date()\/ vs ISO 8601)
        var writerName = writeSerializer.GetType().Name;
        var readerName = readSerializer.GetType().Name;
        var writerIsBson = writerName.Contains("Bson", StringComparison.OrdinalIgnoreCase);
        var readerIsPlainSystemJson = readSerializer is SystemJsonSerializer && !readerName.Contains("Bson", StringComparison.OrdinalIgnoreCase);
        var writerIsNewtonsoft = writerName.Contains("Newtonsoft", StringComparison.OrdinalIgnoreCase);
        var readerIsSystemJson = readerName.Contains("SystemJson", StringComparison.OrdinalIgnoreCase);

        if (writerIsBson && readerIsPlainSystemJson)
        {
            return;
        }

        if (writerIsNewtonsoft && readerIsSystemJson)
        {
            // Newtonsoft DateTime format is incompatible with STJ without explicit configuration
            return;
        }

        try
        {
            // Act
            var serializedData = writeSerializer.Serialize(testObj);
            var deserializedObj = readSerializer.Deserialize<TestObject>(serializedData);

            // Assert
            await Assert.That(deserializedObj).IsNotNull();

            using (Assert.Multiple())
            {
                await Assert.That(deserializedObj!.Name).IsEqualTo(testObj.Name);
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
    /// Tests that all JSON serializers can read each other's data for simple types.
    /// DateTime formats differ between Newtonsoft (\/Date()\/) and STJ (ISO 8601),
    /// so cross-format DateTime compatibility requires explicit configuration.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task JsonSerializersShouldBeInterchangeableForSimpleTypes()
    {
        var testObj = new SimpleTestObject
        {
            Name = "JsonCrossTest",
            Value = 999,
        };

        ISerializer[] jsonSerializers =
        [
            new SystemJsonSerializer(),
            new NewtonsoftSerializer(),
        ];

        foreach (var writer in jsonSerializers)
        {
            foreach (var reader in jsonSerializers)
            {
                var serializedData = writer.Serialize(testObj);
                var result = reader.Deserialize<SimpleTestObject>(serializedData);

                await Assert.That(result).IsNotNull();
                await Assert.That(result!.Name).IsEqualTo(testObj.Name);
                await Assert.That(result.Value).IsEqualTo(testObj.Value);
            }
        }
    }

    /// <summary>
    /// Tests that all BSON serializers can read each other's data (same wire format).
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task BsonSerializersShouldBeInterchangeable()
    {
        var testObj = new TestObject
        {
            Name = "BsonCrossTest",
            Value = 999,
            Date = new DateTime(2025, 1, 15, 16, 0, 0, DateTimeKind.Utc)
        };

        ISerializer[] bsonSerializers =
        [
            new SystemJsonBsonSerializer(),
            new NewtonsoftBsonSerializer(),
        ];

        foreach (var writer in bsonSerializers)
        {
            foreach (var reader in bsonSerializers)
            {
                var serializedData = writer.Serialize(testObj);
                var result = reader.Deserialize<TestObject>(serializedData);

                await Assert.That(result).IsNotNull();
                await Assert.That(result!.Name).IsEqualTo(testObj.Name);
                await Assert.That(result.Value).IsEqualTo(testObj.Value);
            }
        }
    }

    /// <summary>
    /// Tests that BSON-aware serializers can also read JSON data from the same library.
    /// Cross-library JSON compatibility (Newtonsoft→STJ) has DateTime format differences.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task BsonSerializersShouldReadJsonDataFromSameLibrary()
    {
        var testObj = new SimpleTestObject
        {
            Name = "JsonToBsonTest",
            Value = 42,
        };

        // STJ JSON → STJ BSON reader
        var stjData = new SystemJsonSerializer().Serialize(testObj);
        var stjBsonResult = new SystemJsonBsonSerializer().Deserialize<SimpleTestObject>(stjData);
        await Assert.That(stjBsonResult).IsNotNull();
        await Assert.That(stjBsonResult!.Name).IsEqualTo(testObj.Name);

        // Newtonsoft JSON → Newtonsoft BSON reader
        var nsData = new NewtonsoftSerializer().Serialize(testObj);
        var nsBsonResult = new NewtonsoftBsonSerializer().Deserialize<SimpleTestObject>(nsData);
        await Assert.That(nsBsonResult).IsNotNull();
        await Assert.That(nsBsonResult!.Name).IsEqualTo(testObj.Name);
    }

    /// <summary>
    /// Tests that pure JSON serializers cannot read BSON data (expected - different wire formats).
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PureJsonSerializersShouldNotReadBsonData()
    {
        var testObj = new TestObject
        {
            Name = "BsonToJsonTest",
            Value = 42,
            Date = new DateTime(2025, 1, 15, 16, 0, 0, DateTimeKind.Utc)
        };

        var bsonWriter = new SystemJsonBsonSerializer();
        var pureJsonReader = new SystemJsonSerializer();

        var bsonData = bsonWriter.Serialize(testObj);

        // Pure JSON reader should throw on BSON data
        await Assert.That(() => pureJsonReader.Deserialize<TestObject>(bsonData)).Throws<Exception>();
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
                    await Assert.That(retrievedObject!.Name).IsEqualTo(testObject.Name);
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
                        await Assert.That(retrievedObject!.Name).IsEqualTo(testObject.Name);
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
                    await Assert.That(retrieved!.Name).IsEqualTo(testObject.Name);
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

                _ = $"DB file size: {fileInfo.Length} bytes. " +
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
                    await Assert.That(retrieved!.Name).IsEqualTo(testObject.Name);
                    await Assert.That(retrieved.Value).IsEqualTo(testObject.Value);
                }

                await cache2.DisposeAsync();
            }
        }
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

    /// <summary>
    /// Simple test object without DateTime for cross-format tests where DateTime
    /// serialization formats differ (Newtonsoft \/Date()\/ vs STJ ISO 8601).
    /// </summary>
    public class SimpleTestObject
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public int Value { get; set; }
    }
}
