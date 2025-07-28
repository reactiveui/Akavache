// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using Xunit;

namespace Akavache.Tests;

/// <summary>
/// Tests for IBlobCache interface core functionality and helper methods.
/// </summary>
public class IBlobCacheInterfaceTests
{
    /// <summary>
    /// Tests that IBlobCache.ExceptionHelpers work correctly.
    /// </summary>
    /// <returns>A task representing the test completion.</returns>
    [Fact]
    public async Task ExceptionHelpersShouldWorkCorrectly()
    {
        // Test KeyNotFoundException helper
        var keyNotFoundObs = IBlobCache.ExceptionHelpers.ObservableThrowKeyNotFoundException<string>("test_key");

        var keyNotFoundEx = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await keyNotFoundObs.FirstAsync();
        });

        Assert.Contains("test_key", keyNotFoundEx.Message);
        Assert.Contains("not present in the cache", keyNotFoundEx.Message);

        // Test ObjectDisposedException helper
        var objectDisposedObs = IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>("test_cache");

        var objectDisposedEx = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await objectDisposedObs.FirstAsync();
        });

        Assert.Contains("test_cache", objectDisposedEx.Message);
        Assert.Contains("disposed", objectDisposedEx.Message);
    }

    /// <summary>
    /// Tests that IBlobCache basic operations work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task BasicBlobCacheOperationsShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                // Test basic byte array operations
                var testData = new byte[] { 1, 2, 3, 4, 5 };

                // Insert
                await cache.Insert("byte_key", testData).FirstAsync();

                // Get
                var retrieved = await cache.Get("byte_key").FirstAsync();
                Assert.Equal(testData, retrieved);

                // GetCreatedAt
                var createdAt = await cache.GetCreatedAt("byte_key").FirstAsync();
                Assert.NotNull(createdAt);
                Assert.True(createdAt <= DateTimeOffset.Now);

                // GetAllKeys
                var keys = await cache.GetAllKeys().ToList().FirstAsync();
                Assert.Contains("byte_key", keys);

                // Invalidate
                await cache.Invalidate("byte_key").FirstAsync();

                // Verify invalidated
                await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                {
                    await cache.Get("byte_key").FirstAsync();
                });
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
    /// Tests that IBlobCache bulk operations work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task BulkBlobCacheOperationsShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                // Test bulk byte array operations
                var testData = new Dictionary<string, byte[]>
                {
                    ["key1"] = [1, 2, 3],
                    ["key2"] = [4, 5, 6],
                    ["key3"] = [7, 8, 9]
                };

                // Bulk insert
                await cache.Insert(testData).FirstAsync();

                // Bulk get
                var keys = testData.Keys.ToArray();
                var retrieved = await cache.Get(keys).ToList().FirstAsync();

                Assert.Equal(3, retrieved.Count);
                foreach (var item in retrieved)
                {
                    Assert.Equal(testData[item.Key], item.Value);
                }

                // Bulk invalidate
                await cache.Invalidate(keys).FirstAsync();

                // Verify all invalidated
                foreach (var key in keys)
                {
                    await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                    {
                        await cache.Get(key).FirstAsync();
                    });
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
    /// Tests that IBlobCache expiration operations work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ExpirationOperationsShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                var testData = new byte[] { 1, 2, 3, 4, 5 };
                var expiration = DateTimeOffset.Now.AddSeconds(1);

                // Insert with expiration
                await cache.Insert("expiring_key", testData, expiration).FirstAsync();

                // Should be available immediately
                var retrieved = await cache.Get("expiring_key").FirstAsync();
                Assert.Equal(testData, retrieved);

                // Wait for expiration
                await Task.Delay(1500);

                // Should now be expired
                await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                {
                    await cache.Get("expiring_key").FirstAsync();
                });

                // Test bulk insert with expiration
                var bulkData = new Dictionary<string, byte[]>
                {
                    ["bulk1"] = [1, 2],
                    ["bulk2"] = [3, 4]
                };
                var bulkExpiration = DateTimeOffset.Now.AddSeconds(1);

                await cache.Insert(bulkData, bulkExpiration).FirstAsync();

                // Should be available immediately
                var bulkRetrieved = await cache.Get(bulkData.Keys.ToArray()).ToList().FirstAsync();
                Assert.Equal(2, bulkRetrieved.Count);

                // Wait for expiration
                await Task.Delay(1500);

                // Should now be expired
                foreach (var key in bulkData.Keys)
                {
                    await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                    {
                        await cache.Get(key).FirstAsync();
                    });
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
    /// Tests that IBlobCache InvalidateAll works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task InvalidateAllShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                // Insert multiple items
                await cache.Insert("key1", new byte[] { 1, 2, 3 }).FirstAsync();
                await cache.Insert("key2", new byte[] { 4, 5, 6 }).FirstAsync();
                await cache.Insert("key3", new byte[] { 7, 8, 9 }).FirstAsync();

                // Verify items exist
                var keys = await cache.GetAllKeys().ToList().FirstAsync();
                Assert.Equal(3, keys.Count);

                // InvalidateAll
                await cache.InvalidateAll().FirstAsync();

                // Verify all items are gone
                var keysAfter = await cache.GetAllKeys().ToList().FirstAsync();
                Assert.Empty(keysAfter);

                // Verify individual gets fail
                await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                {
                    await cache.Get("key1").FirstAsync();
                });
                await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                {
                    await cache.Get("key2").FirstAsync();
                });
                await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                {
                    await cache.Get("key3").FirstAsync();
                });
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
    /// Tests that IBlobCache Flush operation works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task FlushShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                // Insert data
                await cache.Insert("flush_test", new byte[] { 1, 2, 3 }).FirstAsync();

                // Flush should complete without error
                await cache.Flush().FirstAsync();

                // Data should still be available after flush
                var retrieved = await cache.Get("flush_test").FirstAsync();
                Assert.Equal(new byte[] { 1, 2, 3 }, retrieved);
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
    /// Tests that IBlobCache Vacuum operation works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task VacuumShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                // Insert and remove data to create fragmentation
                await cache.Insert("vacuum_test1", new byte[] { 1, 2, 3 }).FirstAsync();
                await cache.Insert("vacuum_test2", new byte[] { 4, 5, 6 }).FirstAsync();
                await cache.Invalidate("vacuum_test1").FirstAsync();

                // Vacuum should complete without error
                await cache.Vacuum().FirstAsync();

                // Remaining data should still be available
                var retrieved = await cache.Get("vacuum_test2").FirstAsync();
                Assert.Equal(new byte[] { 4, 5, 6 }, retrieved);
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
    /// Tests that IBlobCache handles argument validation correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ArgumentValidationShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                // Test null/empty key validation for basic operations
                Assert.Throws<ArgumentException>(() => cache.Insert(string.Empty, new byte[] { 1, 2, 3 }));
                Assert.Throws<ArgumentException>(() => cache.Insert("   ", new byte[] { 1, 2, 3 }));
                Assert.Throws<ArgumentException>(() => cache.Get(string.Empty));
                Assert.Throws<ArgumentException>(() => cache.Get("   "));
                Assert.Throws<ArgumentException>(() => cache.Invalidate(string.Empty));
                Assert.Throws<ArgumentException>(() => cache.Invalidate("   "));
                Assert.Throws<ArgumentException>(() => cache.GetCreatedAt(string.Empty));
                Assert.Throws<ArgumentException>(() => cache.GetCreatedAt("   "));

                // Test null data validation
                Assert.Throws<ArgumentNullException>(() => cache.Insert("test", null!));

                // Test null collections validation
                Assert.Throws<ArgumentNullException>(() => cache.Insert((Dictionary<string, byte[]>)null!));
                Assert.Throws<ArgumentNullException>(() => cache.Get((string[])null!));
                Assert.Throws<ArgumentNullException>(() => cache.Invalidate((string[])null!));
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
    /// Tests that IBlobCache properties work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CachePropertiesShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                // Test Scheduler property
                Assert.NotNull(cache.Scheduler);

                // Test ForcedDateTimeKind property
                cache.ForcedDateTimeKind = DateTimeKind.Utc;
                Assert.Equal(DateTimeKind.Utc, cache.ForcedDateTimeKind);

                cache.ForcedDateTimeKind = DateTimeKind.Local;
                Assert.Equal(DateTimeKind.Local, cache.ForcedDateTimeKind);

                cache.ForcedDateTimeKind = null;
                Assert.Null(cache.ForcedDateTimeKind);
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
    /// Tests that IBlobCache handles concurrent dispose correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ConcurrentDisposeShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            // Insert some data
            await cache.Insert("dispose_test", new byte[] { 1, 2, 3 }).FirstAsync();

            // Test multiple concurrent dispose calls
            var disposeTasks = new[]
            {
                cache.DisposeAsync().AsTask(),
                cache.DisposeAsync().AsTask(),
                cache.DisposeAsync().AsTask()
            };

            // Should complete without exception
            await Task.WhenAll(disposeTasks);

            // Subsequent operations should throw ObjectDisposedException
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            {
                await cache.Get("dispose_test").FirstAsync();
            });
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that IBlobCache GetCreatedAt handles missing keys correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetCreatedAtShouldHandleMissingKeys()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                // GetCreatedAt for non-existent key should return null
                var createdAt = await cache.GetCreatedAt("non_existent_key").FirstAsync();
                Assert.Null(createdAt);
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
    /// Tests that IBlobCache operations with Type parameters work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task TypeBasedOperationsShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                var testData = new byte[] { 1, 2, 3, 4, 5 };
                var userType = typeof(string);

                // Insert with Type
                await cache.Insert("typed_key", testData, userType).FirstAsync();

                // Get with Type
                var retrieved = await cache.Get("typed_key", userType).FirstAsync();
                Assert.Equal(testData, retrieved);

                // GetCreatedAt with Type
                var createdAt = await cache.GetCreatedAt("typed_key", userType).FirstAsync();
                Assert.NotNull(createdAt);

                // GetAllKeys with Type
                var typedKeys = await cache.GetAllKeys(userType).ToList().FirstAsync();
                Assert.Contains("typed_key", typedKeys);

                // GetAll with Type
                var allTypedData = await cache.GetAll(userType).ToList().FirstAsync();
                Assert.True(allTypedData.Count > 0);
                Assert.Contains(allTypedData, kvp => kvp.Key == "typed_key");

                // Bulk Insert with Type
                var bulkData = new Dictionary<string, byte[]>
                {
                    ["bulk1"] = [1, 2],
                    ["bulk2"] = [3, 4]
                };
                await cache.Insert(bulkData, userType).FirstAsync();

                // Bulk Get with Type
                var bulkRetrieved = await cache.Get(bulkData.Keys.ToArray(), userType).ToList().FirstAsync();
                Assert.Equal(2, bulkRetrieved.Count);

                // Bulk GetCreatedAt with Type
                var bulkCreatedAt = await cache.GetCreatedAt(bulkData.Keys.ToArray(), userType).ToList().FirstAsync();
                Assert.Equal(2, bulkCreatedAt.Count);

                // Flush with Type
                await cache.Flush(userType).FirstAsync();

                // Invalidate with Type
                await cache.Invalidate("typed_key", userType).FirstAsync();

                // Verify invalidation
                await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                {
                    await cache.Get("typed_key", userType).FirstAsync();
                });

                // Bulk Invalidate with Type
                await cache.Invalidate(bulkData.Keys.ToArray(), userType).FirstAsync();

                // InvalidateAll with Type
                await cache.InvalidateAll(userType).FirstAsync();

                // Verify all are gone
                var keysAfterInvalidateAll = await cache.GetAllKeys(userType).ToList().FirstAsync();
                Assert.Empty(keysAfterInvalidateAll);
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
    /// Tests that IBlobCache bulk operations with collections work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task BulkCollectionOperationsShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                // Test GetCreatedAt with multiple keys
                var testKeys = new[] { "key1", "key2", "key3" };
                var testData = new byte[] { 1, 2, 3 };

                // Insert test data
                foreach (var key in testKeys)
                {
                    await cache.Insert(key, testData).FirstAsync();
                }

                // Get created at times for multiple keys
                var createdAtResults = await cache.GetCreatedAt(testKeys).ToList().FirstAsync();
                Assert.Equal(testKeys.Length, createdAtResults.Count);

                foreach (var result in createdAtResults)
                {
                    Assert.Contains(result.Key, testKeys);
                    Assert.NotNull(result.Time);
                    Assert.True(result.Time <= DateTimeOffset.Now);
                }

                // Test with Type parameter
                var userType = typeof(string);
                var typedCreatedAtResults = await cache.GetCreatedAt(testKeys, userType).ToList().FirstAsync();

                // Should be empty since we didn't insert with that type
                Assert.Empty(typedCreatedAtResults);

                // Insert some data with the type
                await cache.Insert("typed_key1", testData, userType).FirstAsync();
                await cache.Insert("typed_key2", testData, userType).FirstAsync();

                var typedKeys = new[] { "typed_key1", "typed_key2" };
                var typedCreatedAt = await cache.GetCreatedAt(typedKeys, userType).ToList().FirstAsync();
                Assert.Equal(2, typedCreatedAt.Count);
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
    /// Tests that IBlobCache handles empty collections correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task EmptyCollectionOperationsShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        CoreRegistrations.Serializer = new SystemJsonSerializer();

        try
        {
            var cache = new InMemoryBlobCache();

            try
            {
                // Test with empty collections
                var emptyKeys = Array.Empty<string>();
                var emptyData = new Dictionary<string, byte[]>();

                // Insert empty collection
                await cache.Insert(emptyData).FirstAsync();

                // Get empty collection
                var emptyGetResults = await cache.Get(emptyKeys).ToList().FirstAsync();
                Assert.Empty(emptyGetResults);

                var emptyCreatedAtResults = await cache.GetCreatedAt(emptyKeys).ToList().FirstAsync();
                Assert.Empty(emptyCreatedAtResults);

                // Invalidate empty collection
                await cache.Invalidate(emptyKeys).FirstAsync();

                // These operations should complete without error
                Assert.True(true);
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
