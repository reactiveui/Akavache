// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for the UpdateExpiration functionality across all IBlobCache implementations.
/// </summary>
public class UpdateExpirationTests : IDisposable
{
    /// <summary>Tracks whether <see cref="Dispose(bool)"/> has already run.</summary>
    private bool _disposed;

    /// <summary>
    /// Tests to make sure UpdateExpiration updates the expiration date without reading or writing data.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationSingleKeyTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            const string key = "test-key";
            const string originalData = "test-value";
            var originalExpiration = DateTimeOffset.Now.AddMinutes(30);
            var newExpiration = DateTimeOffset.Now.AddHours(2);

            // Insert the data with initial expiration
            await fixture.InsertObject(key, originalData, originalExpiration);

            // Act - Update the expiration
            await fixture.UpdateExpiration(key, newExpiration);

            // Assert - Verify the data is still there and retrievable
            var retrievedData = await fixture.GetObject<string>(key);
            await Assert.That(retrievedData).IsEqualTo(originalData);
        }
    }

    /// <summary>
    /// Tests to make sure UpdateExpiration with Type parameter works correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationWithTypeTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            const string key = "test-key";
            const string originalData = "test-value";
            var originalExpiration = DateTimeOffset.Now.AddMinutes(30);
            var newExpiration = DateTimeOffset.Now.AddHours(2);

            // Insert the data with initial expiration
            await fixture.InsertObject(key, originalData, originalExpiration);

            // Act - Update the expiration with type filtering
            await fixture.UpdateExpiration(key, typeof(string), newExpiration);

            // Assert - Verify the data is still there and retrievable
            var retrievedData = await fixture.GetObject<string>(key);
            await Assert.That(retrievedData).IsEqualTo(originalData);
        }
    }

    /// <summary>
    /// Tests to make sure UpdateExpiration with multiple keys works correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationMultipleKeysTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            string[] keys = ["test-key-1", "test-key-2", "test-key-3"];
            string[] originalData = ["value-1", "value-2", "value-3"];
            var originalExpiration = DateTimeOffset.Now.AddMinutes(30);
            var newExpiration = DateTimeOffset.Now.AddHours(2);

            // Insert the data with initial expiration
            for (var i = 0; i < keys.Length; i++)
            {
                await fixture.InsertObject(keys[i], originalData[i], originalExpiration);
            }

            // Act - Update expiration for all keys
            await fixture.UpdateExpiration(keys, newExpiration);

            // Assert - Verify all data is still there and retrievable
            for (var i = 0; i < keys.Length; i++)
            {
                var retrievedData = await fixture.GetObject<string>(keys[i]);
                await Assert.That(retrievedData).IsEqualTo(originalData[i]);
            }
        }
    }

    /// <summary>
    /// Tests to make sure UpdateExpiration with TimeSpan extension method works correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationRelativeTimeTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            const string key = "test-key";
            const string originalData = "test-value";
            var originalExpiration = DateTimeOffset.Now.AddMinutes(30);
            var extensionTime = TimeSpan.FromHours(1);

            // Insert the data with initial expiration
            await fixture.InsertObject(key, originalData, originalExpiration);

            // Act - Update the expiration using relative time extension
            await fixture.UpdateExpiration(key, extensionTime);

            // Assert - Verify the data is still there and retrievable
            var retrievedData = await fixture.GetObject<string>(key);
            await Assert.That(retrievedData).IsEqualTo(originalData);
        }
    }

    /// <summary>
    /// Tests to make sure UpdateExpiration doesn't affect non-existent keys.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationNonExistentKeyTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            const string nonExistentKey = "non-existent-key";
            var newExpiration = DateTimeOffset.Now.AddHours(1);

            // Act - Should not throw when updating expiration for non-existent key
            await fixture.UpdateExpiration(nonExistentKey, newExpiration);

            // Assert - Verify the key still doesn't exist
            try
            {
                await fixture.GetObject<string>(nonExistentKey);
                Assert.Fail("Should have thrown KeyNotFoundException for non-existent key");
            }
            catch (KeyNotFoundException)
            {
                // Expected behavior
            }
        }
    }

    /// <summary>
    /// Tests to make sure UpdateExpiration can remove expiration (set to null).
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationRemoveExpirationTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            const string key = "test-key";
            const string originalData = "test-value";
            var originalExpiration = DateTimeOffset.Now.AddMinutes(30);

            // Insert the data with initial expiration
            await fixture.InsertObject(key, originalData, originalExpiration);

            // Act - Remove expiration by setting to null
            await fixture.UpdateExpiration(key, null);

            // Assert - Verify the data is still there and retrievable
            var retrievedData = await fixture.GetObject<string>(key);
            await Assert.That(retrievedData).IsEqualTo(originalData);
        }
    }

    /// <summary>
    /// Tests that the type-scoped single-key
    /// <see cref="Sqlite3.SqliteBlobCache.UpdateExpiration(string, Type, DateTimeOffset?)"/>
    /// accepts a null absolute expiration and leaves the stored value intact.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task UpdateExpirationKeyWithTypeNullExpirationShouldSucceed()
    {
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            const string key = "typed-null-exp-key";
            await fixture.InsertObject(key, "value", DateTimeOffset.Now.AddMinutes(30));

            await fixture.UpdateExpiration(key, typeof(string), null).FirstAsync();

            var value = await fixture.GetObject<string>(key);
            await Assert.That(value).IsEqualTo("value");
        }
    }

    /// <summary>
    /// Tests that the multi-key
    /// <see cref="Sqlite3.SqliteBlobCache.UpdateExpiration(IEnumerable{string}, DateTimeOffset?)"/>
    /// accepts a null absolute expiration.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithNullExpirationShouldSucceed()
    {
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            string[] keys = ["multi-null-1", "multi-null-2"];
            await fixture.InsertObject(keys[0], "v1", DateTimeOffset.Now.AddMinutes(30));
            await fixture.InsertObject(keys[1], "v2", DateTimeOffset.Now.AddMinutes(30));

            await fixture.UpdateExpiration(keys, null).FirstAsync();

            var first = await fixture.GetObject<string>(keys[0]);
            var second = await fixture.GetObject<string>(keys[1]);
            await Assert.That(first).IsEqualTo("v1");
            await Assert.That(second).IsEqualTo("v2");
        }
    }

    /// <summary>
    /// Tests that the type-scoped multi-key
    /// <see cref="Sqlite3.SqliteBlobCache.UpdateExpiration(IEnumerable{string}, Type, DateTimeOffset?)"/>
    /// accepts a null absolute expiration.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task UpdateExpirationKeysWithTypeNullExpirationShouldSucceed()
    {
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            string[] keys = ["typed-multi-null-1", "typed-multi-null-2"];
            await fixture.InsertObject(keys[0], "v1", DateTimeOffset.Now.AddMinutes(30));
            await fixture.InsertObject(keys[1], "v2", DateTimeOffset.Now.AddMinutes(30));

            await fixture.UpdateExpiration(keys, typeof(string), null).FirstAsync();

            var first = await fixture.GetObject<string>(keys[0]);
            var second = await fixture.GetObject<string>(keys[1]);
            await Assert.That(first).IsEqualTo("v1");
            await Assert.That(second).IsEqualTo("v2");
        }
    }

    /// <summary>
    /// Dispose method for cleanup.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected virtual dispose method.
    /// </summary>
    /// <param name="disposing">Whether we're disposing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
        {
            return;
        }

        _disposed = true;
    }

    /// <summary>
    /// Creates a SqliteBlobCache for testing with given serializer.
    /// </summary>
    /// <param name="path">The path for the cache.</param>
    /// <param name="serializer">The serializer to use.</param>
    /// <returns>A new SqliteBlobCache instance.</returns>
    private static Sqlite3.SqliteBlobCache CreateBlobCache(string path, ISerializer serializer)
    {
        // Create separate database files for each serializer to ensure compatibility
        var serializerName = serializer.GetType().Name ?? "Unknown";

        // Further separate JSON and BSON formats to prevent cross-contamination
        var formatType = serializerName.Contains("Bson") ? "bson" : "json";
        var fileName = $"updateexpiration-{serializerName}-{formatType}.db";

        return new(Path.Combine(path, fileName), serializer);
    }

    /// <summary>
    /// Sets up the test with the specified serializer type.
    /// </summary>
    /// <param name="serializerType">The type of serializer to use for this test.</param>
    /// <returns>The configured serializer instance.</returns>
    private static ISerializer SetupTestSerializer(Type? serializerType)
    {
        // Clear any existing in-flight requests to ensure clean test state
        RequestCache.Clear();

        if (serializerType == typeof(NewtonsoftBsonSerializer))
        {
            // Register the Newtonsoft BSON serializer specifically
            return new NewtonsoftBsonSerializer();
        }

        if (serializerType == typeof(SystemJsonBsonSerializer))
        {
            // Register the System.Text.Json BSON serializer specifically
            return new SystemJsonBsonSerializer();
        }

        if (serializerType == typeof(NewtonsoftSerializer))
        {
            // Register the Newtonsoft JSON serializer
            return new NewtonsoftSerializer();
        }

        if (serializerType == typeof(SystemJsonSerializer))
        {
            // Register the System.Text.Json serializer
            return new SystemJsonSerializer();
        }

        return null!;
    }
}
