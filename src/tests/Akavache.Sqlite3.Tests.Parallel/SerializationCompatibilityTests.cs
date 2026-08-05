// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>Focused serialization compatibility tests to ensure proper cross-serializer compatibility.</summary>
[Category("Akavache")]
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

    /// <summary>The shape every serializer round-trip fixture in these tests carries.</summary>
    internal interface ISerializerFixture
    {
        /// <summary>Gets or sets the name.</summary>
        string? Name { get; set; }

        /// <summary>Gets or sets the value.</summary>
        int Value { get; set; }
    }

    /// <summary>A fixture that also carries a bare <see cref="DateTime"/>, which is what these tests are about.</summary>
    internal interface IDatedSerializerFixture : ISerializerFixture
    {
        /// <summary>Gets or sets the date.</summary>
        DateTime Date { get; set; }
    }

    /// <summary>Gets all combinations of serializers for cross-compatibility testing.</summary>
    /// <returns>All serializer combinations as tuples wrapped in Func for test isolation.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1024:Use properties where appropriate",
        Justification = "Method returns a lazy enumerable used as a TUnit data source — property semantics aren't appropriate.")]
    public static IEnumerable<(Func<ISerializer> WriteSerializer, Func<ISerializer> ReadSerializer)> GetSerializerCombinations()
    {
        foreach (var writeSerializer in Serializers)
        {
            foreach (var readSerializer in Serializers)
            {
                yield return (() => writeSerializer, () => readSerializer);
            }
        }
    }

    /// <summary>Tests that each serializer can roundtrip its own data.</summary>
    /// <param name="serializerType">The serializer type to test.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task SerializerShouldRoundTripOwnData(Type serializerType)
    {
        const int RoundTripValue = 42;

        // Arrange
        var serializer = (ISerializer)Activator.CreateInstance(serializerType)!;
        TestObject testObj = new() { Name = "Test", Value = RoundTripValue, Date = TimeProvider.System.GetUtcNow().UtcDateTime };

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

    /// <summary>Tests cross-serializer compatibility for all combinations.</summary>
    /// <param name="writeSerializerFactory">Factory for the writer serializer.</param>
    /// <param name="readSerializerFactory">Factory for the reader serializer.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [MethodDataSource(nameof(GetSerializerCombinations))]
    public async Task CrossSerializerCompatibilityShouldWork(
        Func<ISerializer> writeSerializerFactory,
        Func<ISerializer> readSerializerFactory)
    {
        const int CrossSerializerValue = 123;

        ArgumentNullException.ThrowIfNull(writeSerializerFactory);
        ArgumentNullException.ThrowIfNull(readSerializerFactory);
        var writeSerializer = writeSerializerFactory();
        var readSerializer = readSerializerFactory();

        // Arrange
        TestObject testObj = new() { Name = "CrossTest", Value = CrossSerializerValue, Date = new(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc) };

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
        const int InterchangeValue = 999;
        SimpleTestObject testObj = new() { Name = "JsonCrossTest", Value = InterchangeValue, };

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

    /// <summary>Tests that all BSON serializers can read each other's data (same wire format).</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task BsonSerializersShouldBeInterchangeable()
    {
        const int InterchangeValue = 999;
        TestObject testObj = new() { Name = "BsonCrossTest", Value = InterchangeValue, Date = new(2025, 1, 15, 16, 0, 0, DateTimeKind.Utc) };

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
        const int JsonToBsonValue = 42;
        SimpleTestObject testObj = new() { Name = "JsonToBsonTest", Value = JsonToBsonValue, };

        // STJ JSON → STJ BSON reader
        var systemJsonData = new SystemJsonSerializer().Serialize(testObj);
        var systemJsonBsonResult = new SystemJsonBsonSerializer().Deserialize<SimpleTestObject>(systemJsonData);
        await Assert.That(systemJsonBsonResult).IsNotNull();
        await Assert.That(systemJsonBsonResult!.Name).IsEqualTo(testObj.Name);

        // Newtonsoft JSON → Newtonsoft BSON reader
        var newtonsoftData = new NewtonsoftSerializer().Serialize(testObj);
        var newtonsoftBsonResult = new NewtonsoftBsonSerializer().Deserialize<SimpleTestObject>(newtonsoftData);
        await Assert.That(newtonsoftBsonResult).IsNotNull();
        await Assert.That(newtonsoftBsonResult!.Name).IsEqualTo(testObj.Name);
    }

    /// <summary>Tests that pure JSON serializers cannot read BSON data (expected - different wire formats).</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PureJsonSerializersShouldNotReadBsonData()
    {
        const int BsonToJsonValue = 42;
        TestObject testObj = new() { Name = "BsonToJsonTest", Value = BsonToJsonValue, Date = new(2025, 1, 15, 16, 0, 0, DateTimeKind.Utc) };

        SystemJsonBsonSerializer bsonWriter = new();
        SystemJsonSerializer pureJsonReader = new();

        var bsonData = bsonWriter.Serialize(testObj);

        // Pure JSON reader should throw on BSON data
        await Assert.That(() => pureJsonReader.Deserialize<TestObject>(bsonData)).Throws<Exception>();
    }

    /// <summary>Tests that SQLite cache can store and retrieve objects with all serializers without losing data.</summary>
    /// <param name="serializerType">The serializer type to test.</param>
    /// <returns>A task representing the test operation.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task SqliteCacheShouldPersistDataCorrectlyWithAllSerializers(Type serializerType)
    {
        const int PersistedValue = 12_345;
        const int MaxRoundTripDriftSeconds = 60;

        ArgumentExceptionHelper.ThrowIfNull(serializerType);

        // Arrange
        var serializer = (ISerializer)Activator.CreateInstance(serializerType)!;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "test.db");

            TestObject testObject = new() { Name = "TestUser", Value = PersistedValue, Date = new(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc) };

            // Test storage phase
            using (SqliteBlobCache writeCache = new(dbPath, serializer, ImmediateScheduler.Instance))
            {
                writeCache.InsertObject("test_key", testObject).WaitForCompletion();
                writeCache.Flush().WaitForCompletion(); // Ensure data is written to disk
            }

            // Test retrieval phase with new cache instance
            using SqliteBlobCache readCache = new(dbPath, serializer, ImmediateScheduler.Instance);
            var retrievedObject = readCache.GetObject<TestObject>("test_key").WaitForValue();

            await Assert.That(retrievedObject).IsNotNull();
            using (Assert.Multiple())
            {
                await Assert.That(retrievedObject!.Name).IsEqualTo(testObject.Name);
                await Assert.That(retrievedObject.Value).IsEqualTo(testObject.Value);
            }

            // Allow for DateTime precision differences
            var timeDiff = Math.Abs((testObject.Date - retrievedObject!.Date).TotalSeconds);
            await Assert.That(timeDiff).IsLessThan(MaxRoundTripDriftSeconds);
        }
    }

    /// <summary>Tests cross-serializer compatibility with SQLite cache.</summary>
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
        const int CrossSerializerValue = 99_999;
        const int MaxCrossSerializerDriftMinutes = 1440;

        ArgumentExceptionHelper.ThrowIfNull(writeSerializerType);
        ArgumentExceptionHelper.ThrowIfNull(readSerializerType);

        TestObject testObject = new() { Name = "CrossSerializerTest", Value = CrossSerializerValue, Date = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc) };

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "cross_serializer_test.db");

            // Write with first serializer
            {
                var writeSerializer = (ISerializer)Activator.CreateInstance(writeSerializerType)!;

                using SqliteBlobCache writeCache = new(dbPath, writeSerializer, ImmediateScheduler.Instance);
                writeCache.InsertObject("cross_test", testObject).WaitForCompletion();
                writeCache.Flush().WaitForCompletion();
            }

            // Read with second serializer
            {
                var readSerializer = (ISerializer)Activator.CreateInstance(readSerializerType)!;

                using SqliteBlobCache readCache = new(dbPath, readSerializer, ImmediateScheduler.Instance);

                try
                {
                    var retrievedObject = readCache.GetObject<TestObject>("cross_test").WaitForValue();

                    await Assert.That(retrievedObject).IsNotNull();
                    using (Assert.Multiple())
                    {
                        await Assert.That(retrievedObject!.Name).IsEqualTo(testObject.Name);
                        await Assert.That(retrievedObject.Value).IsEqualTo(testObject.Value);
                    }

                    // Allow for DateTime precision differences
                    var timeDiff = Math.Abs((testObject.Date - retrievedObject!.Date).TotalMinutes);
                    await Assert.That(timeDiff).IsLessThan(MaxCrossSerializerDriftMinutes);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new InvalidOperationException(
                        $"Cross-serializer test failed: could not read data written with {writeSerializerType.Name} using {readSerializerType.Name}. "
                        + $"Error: {ex.Message}");
                }
            }
        }
    }

    /// <summary>Simple test to verify SQLite cache basic operations work.</summary>
    /// <param name="serializerType">The serializer type to test.</param>
    /// <returns>A task representing the test operation.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task SimpleSqliteTest(Type serializerType)
    {
        const int SimpleValue = 123;

        ArgumentExceptionHelper.ThrowIfNull(serializerType);

        // Arrange
        var serializer = (ISerializer)Activator.CreateInstance(serializerType)!;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "simple_test.db");

            TestObject testObject = new() { Name = "SimpleTest", Value = SimpleValue, Date = new(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc) };

            // Test in single cache instance to see if issue is with multiple instances
            using SqliteBlobCache cache = new(dbPath, serializer, ImmediateScheduler.Instance);

            // Insert
            cache.InsertObject("simple_key", testObject).WaitForCompletion();

            // Verify via keys
            var allKeys = cache.GetAllKeys().ToList().WaitForValue();
            var typedKeys = cache.GetAllKeys(typeof(TestObject)).ToList().WaitForValue();

            using (Assert.Multiple())
            {
                await Assert.That(allKeys).IsNotEmpty();
                await Assert.That(typedKeys).IsNotEmpty();
            }

            // Get
            var retrieved = cache.GetObject<TestObject>("simple_key").WaitForValue();

            await Assert.That(retrieved).IsNotNull();
            using (Assert.Multiple())
            {
                await Assert.That(retrieved!.Name).IsEqualTo(testObject.Name);
                await Assert.That(retrieved.Value).IsEqualTo(testObject.Value);
            }
        }
    }

    /// <summary>Test to debug multi-instance SQLite persistence issues.</summary>
    /// <param name="serializerType">The serializer type to test.</param>
    /// <returns>A task representing the test operation.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task DebuggingMultiInstancePersistence(Type serializerType)
    {
        const int MultiInstanceValue = 789;
        const int CleanupSettleMilliseconds = 100;

        ArgumentExceptionHelper.ThrowIfNull(serializerType);

        // Arrange
        var serializer = (ISerializer)Activator.CreateInstance(serializerType)!;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "debug_multi_instance.db");

            TestObject testObject = new() { Name = "MultiInstanceDebug", Value = MultiInstanceValue, Date = new(2025, 1, 15, 15, 30, 0, DateTimeKind.Utc) };

            // Phase 1: Store data with explicit disposal and verification
            {
                SqliteBlobCache cache1 = new(dbPath, serializer, ImmediateScheduler.Instance);
                cache1.InsertObject("debug_key", testObject).WaitForCompletion();
                cache1.Flush().WaitForCompletion();

                // Verify the data exists before disposal
                var keysBeforeDisposal = cache1.GetAllKeys().ToList().WaitForValue();
                await Assert.That(keysBeforeDisposal).IsNotEmpty();

                // Explicit async disposal with proper wait
                cache1.Dispose();

                // Small delay to ensure cleanup is complete
                await Task.Delay(CleanupSettleMilliseconds);
            }

            // Phase 2: Try to read with a new instance
            {
                SqliteBlobCache cache2 = new(dbPath, serializer, ImmediateScheduler.Instance);

                // Check if file exists
                await Assert.That(File.Exists(dbPath)).IsTrue();

                // Check keys
                var allKeys = cache2.GetAllKeys().ToList().WaitForValue();
                var typedKeys = cache2.GetAllKeys(typeof(TestObject)).ToList().WaitForValue();

                // Enhanced diagnostics
                FileInfo fileInfo = new(dbPath);
                var walFile = $"{dbPath}-wal";
                var shmFile = $"{dbPath}-shm";

                _ = $"DB file size: {fileInfo.Length} bytes. "
                    + $"WAL exists: {File.Exists(walFile)}. "
                    + $"SHM exists: {File.Exists(shmFile)}. "
                    + $"All keys count: {allKeys!.Count}. "
                    + $"Typed keys count: {typedKeys!.Count}. "
                    + $"All keys: [{string.Join(", ", allKeys)}]. "
                    + $"Typed keys: [{string.Join(", ", typedKeys)}]";

                await Assert.That(allKeys).IsNotEmpty();

                // Try to retrieve
                var retrieved = cache2.GetObject<TestObject>("debug_key").WaitForValue();

                await Assert.That(retrieved).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(retrieved!.Name).IsEqualTo(testObject.Name);
                    await Assert.That(retrieved.Value).IsEqualTo(testObject.Value);
                }

                cache2.Dispose();
            }
        }
    }

    /// <summary>Test object for serialization.</summary>
    /// <remarks>
    /// Assembly-internal on purpose: the fixture exists to carry a bare <see cref="DateTime"/>, which is
    /// exactly what must not appear on a type other assemblies can bind to. The properties stay public
    /// because both serializer families ignore non-public members, and they satisfy the fixture contract.
    /// </remarks>
    internal sealed class TestObject : IDatedSerializerFixture
    {
        /// <inheritdoc/>
        public string? Name { get; set; }

        /// <inheritdoc/>
        public int Value { get; set; }

        /// <inheritdoc/>
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Simple test object without DateTime for cross-format tests where DateTime
    /// serialization formats differ (Newtonsoft \/Date()\/ vs STJ ISO 8601).
    /// </summary>
    internal sealed class SimpleTestObject : ISerializerFixture
    {
        /// <inheritdoc/>
        public string? Name { get; set; }

        /// <inheritdoc/>
        public int Value { get; set; }
    }
}
