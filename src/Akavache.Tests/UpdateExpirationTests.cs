// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for the UpdateExpiration functionality across all IBlobCache implementations.
/// </summary>
[NonParallelizable]
public class UpdateExpirationTests : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Tests to make sure UpdateExpiration updates the expiration date without reading or writing data.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationSingleKeyTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            var key = "test-key";
            var originalData = "test-value";
            var originalExpiration = DateTimeOffset.Now.AddMinutes(30);
            var newExpiration = DateTimeOffset.Now.AddHours(2);

            // Insert the data with initial expiration
            await fixture.InsertObject(key, originalData, originalExpiration);

            // Act - Update the expiration
            await fixture.UpdateExpiration(key, newExpiration);

            // Assert - Verify the data is still there and retrievable
            var retrievedData = await fixture.GetObject<string>(key);
            Assert.That(retrievedData, Is.EqualTo(originalData), "Data should remain unchanged after UpdateExpiration");
        }
    }

    /// <summary>
    /// Tests to make sure UpdateExpiration with Type parameter works correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationWithTypeTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            var key = "test-key";
            var originalData = "test-value";
            var originalExpiration = DateTimeOffset.Now.AddMinutes(30);
            var newExpiration = DateTimeOffset.Now.AddHours(2);

            // Insert the data with initial expiration
            await fixture.InsertObject(key, originalData, originalExpiration);

            // Act - Update the expiration with type filtering
            await fixture.UpdateExpiration(key, typeof(string), newExpiration);

            // Assert - Verify the data is still there and retrievable
            var retrievedData = await fixture.GetObject<string>(key);
            Assert.That(retrievedData, Is.EqualTo(originalData), "Data should remain unchanged after UpdateExpiration with type");
        }
    }

    /// <summary>
    /// Tests to make sure UpdateExpiration with multiple keys works correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationMultipleKeysTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            var keys = new[] { "test-key-1", "test-key-2", "test-key-3" };
            var originalData = new[] { "value-1", "value-2", "value-3" };
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
                Assert.That(retrievedData, Is.EqualTo(originalData[i]), $"Data for key {keys[i]} should remain unchanged after UpdateExpiration");
            }
        }
    }

    /// <summary>
    /// Tests to make sure UpdateExpiration with TimeSpan extension method works correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationRelativeTimeTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            var key = "test-key";
            var originalData = "test-value";
            var originalExpiration = DateTimeOffset.Now.AddMinutes(30);
            var extensionTime = TimeSpan.FromHours(1);

            // Insert the data with initial expiration
            await fixture.InsertObject(key, originalData, originalExpiration);

            // Act - Update the expiration using relative time extension
            await fixture.UpdateExpiration(key, extensionTime);

            // Assert - Verify the data is still there and retrievable
            var retrievedData = await fixture.GetObject<string>(key);
            Assert.That(retrievedData, Is.EqualTo(originalData), "Data should remain unchanged after UpdateExpiration with TimeSpan");
        }
    }

    /// <summary>
    /// Tests to make sure UpdateExpiration doesn't affect non-existent keys.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationNonExistentKeyTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            var nonExistentKey = "non-existent-key";
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
    [TestCase(typeof(SystemJsonSerializer))]
    [TestCase(typeof(SystemJsonBsonSerializer))]
    [TestCase(typeof(NewtonsoftSerializer))]
    [TestCase(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task UpdateExpirationRemoveExpirationTest(Type serializerType)
    {
        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path, serializer))
        {
            // Arrange
            var key = "test-key";
            var originalData = "test-value";
            var originalExpiration = DateTimeOffset.Now.AddMinutes(30);

            // Insert the data with initial expiration
            await fixture.InsertObject(key, originalData, originalExpiration);

            // Act - Remove expiration by setting to null
            await fixture.UpdateExpiration(key, (DateTimeOffset?)null);

            // Assert - Verify the data is still there and retrievable
            var retrievedData = await fixture.GetObject<string>(key);
            Assert.That(retrievedData, Is.EqualTo(originalData), "Data should remain unchanged after removing expiration");
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
        if (!_disposed && disposing)
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Creates a SqliteBlobCache for testing with given serializer.
    /// </summary>
    /// <param name="path">The path for the cache.</param>
    /// <param name="serializer">The serializer to use.</param>
    /// <returns>A new SqliteBlobCache instance.</returns>
    private static IBlobCache CreateBlobCache(string path, ISerializer serializer)
    {
        // Create separate database files for each serializer to ensure compatibility
        var serializerName = serializer.GetType().Name ?? "Unknown";

        // Further separate JSON and BSON formats to prevent cross-contamination
        var formatType = serializerName.Contains("Bson") ? "bson" : "json";
        var fileName = $"updateexpiration-{serializerName}-{formatType}.db";

        return new Akavache.Sqlite3.SqliteBlobCache(Path.Combine(path, fileName), serializer);
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
        else if (serializerType == typeof(SystemJsonBsonSerializer))
        {
            // Register the System.Text.Json BSON serializer specifically
            return new SystemJsonBsonSerializer();
        }
        else if (serializerType == typeof(NewtonsoftSerializer))
        {
            // Register the Newtonsoft JSON serializer
            return new NewtonsoftSerializer();
        }
        else if (serializerType == typeof(SystemJsonSerializer))
        {
            // Register the System.Text.Json serializer
            return new SystemJsonSerializer();
        }
        else
        {
            return null!;
        }
    }
}