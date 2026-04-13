// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests for SqliteBlobCache covering disposed-state error paths,
/// null arg validation, and type-aware overloads.
/// </summary>
[Category("Akavache")]
public class SqliteBlobCacheDirectTests
{
    /// <summary>
    /// Tests disposed-state error paths for all operations.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposedShouldThrowForAllOperations()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            await cache.DisposeAsync();

            await cache.Insert("k", [1]).ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.Insert([new("k", [1])]).ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.Insert("k", [1], typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.Insert([new("k", [1])], typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

            await cache.Get("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.Get(["k"]).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.Get("k", typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.Get(["k"], typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();

            await cache.GetAllKeys().ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.GetAllKeys(typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.GetAll(typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();

            await cache.GetCreatedAt("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.GetCreatedAt(["k"]).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.GetCreatedAt("k", typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.GetCreatedAt(["k"], typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();

            await cache.Flush().ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.Flush(typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

            await cache.Invalidate("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.Invalidate(["k"]).ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.Invalidate("k", typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.Invalidate(["k"], typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

            await cache.InvalidateAll().ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.InvalidateAll(typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

            await cache.Vacuum().ToTask().ShouldThrowAsync<ObjectDisposedException>();

            await cache.UpdateExpiration("k", DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.UpdateExpiration(["k"], DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
            await cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        }
    }

    /// <summary>
    /// Tests null argument validation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NullArgsShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Get((IEnumerable<string>)null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.Get((string)null!, typeof(string)).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.Get("k", null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.Get((IEnumerable<string>)null!, typeof(string)).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.Get(["k"], null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.GetAll(null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests type-aware Insert and Get round-trip.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareInsertAndGetShouldRoundTrip()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("k1", [1, 2, 3], typeof(string)).ToTask();
                var data = await cache.Get("k1", typeof(string)).ToTask();

                await Assert.That(data).IsNotNull();
                await Assert.That(data.Length).IsEqualTo(3);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests type-aware bulk Insert and Get.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareBulkInsertAndGetShouldRoundTrip()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                KeyValuePair<string, byte[]>[] pairs =
                [
                    new("k1", [1]),
                    new("k2", [2])
                ];
                await cache.Insert(pairs, typeof(string)).ToTask();

                var results = await cache.Get(["k1", "k2"], typeof(string)).ToList().ToTask();
                await Assert.That(results.Count).IsEqualTo(2);

                var typedKeys = await cache.GetAllKeys(typeof(string)).ToList().ToTask();
                await Assert.That(typedKeys.Count).IsEqualTo(2);

                var allOfType = await cache.GetAll(typeof(string)).ToList().ToTask();
                await Assert.That(allOfType.Count).IsEqualTo(2);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests type-aware Invalidate.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareInvalidateShouldRemoveEntries()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("k1", [1], typeof(string)).ToTask();
                await cache.Insert("k2", [2], typeof(int)).ToTask();

                await cache.Invalidate("k1", typeof(string)).ToTask();
                await cache.InvalidateAll(typeof(int)).ToTask();

                var keys = await cache.GetAllKeys().ToList().ToTask();
                await Assert.That(keys).IsEmpty();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests type-aware Invalidate by keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareInvalidateByKeysShouldRemoveEntries()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("k1", [1], typeof(string)).ToTask();
                await cache.Insert("k2", [2], typeof(string)).ToTask();

                await cache.Invalidate(["k1", "k2"], typeof(string)).ToTask();

                var keys = await cache.GetAllKeys().ToList().ToTask();
                await Assert.That(keys).IsEmpty();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests type-aware GetCreatedAt.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareGetCreatedAtShouldReturnTimestamps()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("k1", [1], typeof(string)).ToTask();
                await cache.Insert("k2", [2], typeof(string)).ToTask();

                var single = await cache.GetCreatedAt("k1", typeof(string)).ToTask();
                await Assert.That(single).IsNotNull();

                var multi = await cache.GetCreatedAt(["k1", "k2"], typeof(string)).ToList().ToTask();
                await Assert.That(multi.Count).IsEqualTo(2);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests type-aware UpdateExpiration.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareUpdateExpirationShouldUpdateEntries()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("k1", [1], typeof(string)).ToTask();
                var newExpiration = DateTimeOffset.Now.AddHours(1);

                await cache.UpdateExpiration("k1", typeof(string), newExpiration).ToTask();
                await cache.UpdateExpiration(["k1"], typeof(string), newExpiration).ToTask();

                var data = await cache.Get("k1", typeof(string)).ToTask();
                await Assert.That(data).IsNotNull();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests Get with non-existent key throws KeyNotFoundException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetNonExistentKeyShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Get("non_existent_key").ToTask().ShouldThrowAsync<KeyNotFoundException>();
                await cache.Get("non_existent_key", typeof(string)).ToTask().ShouldThrowAsync<KeyNotFoundException>();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests Get with whitespace key throws ArgumentNullException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetWithWhitespaceKeyShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Get(string.Empty).ToTask().ShouldThrowAsync<ArgumentNullException>();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests Vacuum operation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldWork()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("k1", [1], DateTimeOffset.Now.AddSeconds(-10)).ToTask();
                await cache.Insert("k2", [2], DateTimeOffset.Now.AddHours(1)).ToTask();

                await cache.Vacuum().ToTask();

                var data = await cache.Get("k2").ToTask();
                await Assert.That(data).IsNotNull();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests non-typed Insert, Get, GetAllKeys, GetAll, Invalidate, InvalidateAll happy paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NonTypedHappyPathsShouldRoundTrip()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("k1", [1, 2]).ToTask();
                await cache.Insert(
                    [
                        new("k2", [3]),
                        new("k3", [4])
                    ],
                    DateTimeOffset.Now.AddHours(1)).ToTask();

                var single = await cache.Get("k1").ToTask();
                await Assert.That(single).IsNotNull();

                var multi = await cache.Get(["k2", "k3"]).ToList().ToTask();
                await Assert.That(multi.Count).IsEqualTo(2);

                var keys = await cache.GetAllKeys().ToList().ToTask();
                await Assert.That(keys.Count).IsEqualTo(3);

                var created = await cache.GetCreatedAt("k1").ToTask();
                await Assert.That(created).IsNotNull();

                var createdMany = await cache.GetCreatedAt(["k1", "k2"]).ToList().ToTask();
                await Assert.That(createdMany.Count).IsEqualTo(2);

                await cache.UpdateExpiration("k1", DateTimeOffset.Now.AddDays(1)).ToTask();
                await cache.UpdateExpiration(["k2", "k3"], DateTimeOffset.Now.AddDays(1)).ToTask();

                await cache.Flush().ToTask();
                await cache.Flush(typeof(string)).ToTask();

                await cache.Invalidate("k1").ToTask();
                await cache.Invalidate(["k2"]).ToTask();

                await cache.InvalidateAll().ToTask();

                var remaining = await cache.GetAllKeys().ToList().ToTask();
                await Assert.That(remaining).IsEmpty();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests additional null and whitespace argument validation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AdditionalNullAndWhitespaceArgsShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                // GetCreatedAt null arg variants
                await cache.GetCreatedAt((string)null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.GetCreatedAt((IEnumerable<string>)null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.GetCreatedAt("k", null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.GetCreatedAt((string)null!, typeof(string)).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.GetCreatedAt(["k"], null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.GetCreatedAt((IEnumerable<string>)null!, typeof(string)).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();

                // GetAllKeys null type
                await cache.GetAllKeys(null!).ToList().ToTask().ShouldThrowAsync<ArgumentNullException>();

                // Insert null args
                await cache.Insert(null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.Insert(null!, typeof(string)).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.Insert([new("k", [1])], (Type)null!).ToTask().ShouldThrowAsync<ArgumentNullException>();

                // Insert(key, data, type) arg validation
                await cache.Insert(string.Empty, [1], typeof(string)).ToTask().ShouldThrowAsync<ArgumentException>();
                await cache.Insert("  ", [1], typeof(string)).ToTask().ShouldThrowAsync<ArgumentException>();
                await cache.Insert("k", null!, typeof(string)).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.Insert("k", [1], (Type)null!).ToTask().ShouldThrowAsync<ArgumentNullException>();

                // Invalidate arg validation
                await cache.Invalidate(string.Empty).ToTask().ShouldThrowAsync<ArgumentException>();
                await cache.Invalidate("   ").ToTask().ShouldThrowAsync<ArgumentException>();
                await cache.Invalidate(string.Empty, typeof(string)).ToTask().ShouldThrowAsync<ArgumentException>();
                await cache.Invalidate("k", null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.Invalidate((IEnumerable<string>)null!).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.Invalidate((IEnumerable<string>)null!, typeof(string)).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.Invalidate(["k"], null!).ToTask().ShouldThrowAsync<ArgumentNullException>();

                // UpdateExpiration arg validation
                await cache.UpdateExpiration(string.Empty, DateTimeOffset.Now).ToTask().ShouldThrowAsync<ArgumentException>();
                await cache.UpdateExpiration(string.Empty, typeof(string), DateTimeOffset.Now).ToTask().ShouldThrowAsync<ArgumentException>();
                await cache.UpdateExpiration("k", null!, DateTimeOffset.Now).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.UpdateExpiration((IEnumerable<string>)null!, DateTimeOffset.Now).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.UpdateExpiration((IEnumerable<string>)null!, typeof(string), DateTimeOffset.Now).ToTask().ShouldThrowAsync<ArgumentNullException>();
                await cache.UpdateExpiration(["k"], null!, DateTimeOffset.Now).ToTask().ShouldThrowAsync<ArgumentNullException>();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests synchronous Dispose path covering Dispose(bool isDisposing=true) branches.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Test deliberately exercises the synchronous Dispose path.")]
    public async Task SynchronousDisposeShouldCompleteCleanup()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            await cache.Insert("k1", [1]).ToTask();

            // Synchronous dispose exercises the Dispose(bool) wal_checkpoint/journal/close paths
            cache.Dispose();

            // Second dispose is a no-op (early return)
            cache.Dispose();

            await cache.Get("k1").ToTask().ShouldThrowAsync<ObjectDisposedException>();
        }
    }

    /// <summary>
    /// Tests GetCreatedAt for a key that does not exist returns null via DefaultIfEmpty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtForMissingKeyShouldReturnNull()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                var result = await cache.GetCreatedAt("missing").ToTask();
                await Assert.That(result).IsNull();

                var typed = await cache.GetCreatedAt("missing", typeof(string)).ToTask();
                await Assert.That(typed).IsNull();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests Insert with expired entries and retrieval after expiration.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertWithPastExpirationShouldNotBeRetrievable()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("expired", [1], typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).ToTask();

                await cache.Get("expired", typeof(string)).ToTask().ShouldThrowAsync<KeyNotFoundException>();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that BeforeWriteToDiskFilter throws ObjectDisposedException after disposal.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BeforeWriteToDiskFilterShouldThrowWhenDisposed()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            await cache.DisposeAsync();

            await cache.BeforeWriteToDiskFilter([1, 2, 3], ImmediateScheduler.Instance).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        }
    }

    /// <summary>
    /// Tests that BeforeWriteToDiskFilter returns data unchanged when cache is active.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BeforeWriteToDiskFilterShouldReturnDataWhenNotDisposed()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                byte[] input = [10, 20, 30];
                var result = await cache.BeforeWriteToDiskFilter(input, ImmediateScheduler.Instance).ToTask();
                await Assert.That(result).IsEquivalentTo(input);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that calling DisposeAsync twice does not throw — the second call is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DoubleDisposeAsyncShouldNotThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            await cache.Insert("k", [1]).ToTask();

            await cache.DisposeAsync();
            await cache.DisposeAsync();

            await cache.Get("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
        }
    }

    /// <summary>
    /// Tests that Get falls back to legacy and ultimately throws KeyNotFoundException
    /// when neither V11 nor V10 tables contain the key. This exercises the full
    /// TryGetLegacyValueAsync fallback path in Get(string).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetShouldFallbackToLegacyThenThrowWhenNotFound()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                // Insert a valid entry to ensure the db is initialized, then look for a missing one
                await cache.Insert("existing", [1, 2]).ToTask();

                // Non-typed Get for missing key exercises full fallback path
                await cache.Get("nonexistent").ToTask().ShouldThrowAsync<KeyNotFoundException>();

                // Typed Get for missing key exercises typed fallback path
                await cache.Get("nonexistent", typeof(int)).ToTask().ShouldThrowAsync<KeyNotFoundException>();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that bulk Get returns only matching entries, exercising the Where filter
    /// for non-null values.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BulkGetShouldReturnOnlyMatchingKeys()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("a", [1]).ToTask();
                await cache.Insert("b", [2]).ToTask();

                // Request keys where only some exist
                var results = await cache.Get(["a", "c", "d"]).ToList().ToTask();
                await Assert.That(results.Count).IsEqualTo(1);
                await Assert.That(results[0].Key).IsEqualTo("a");

                // Typed bulk get with no matches
                var typedResults = await cache.Get(["x", "y"], typeof(string)).ToList().ToTask();
                await Assert.That(typedResults).IsEmpty();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that GetAll with a type that has no entries returns empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllWithUnusedTypeShouldReturnEmpty()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("k", [1], typeof(string)).ToTask();

                var results = await cache.GetAll(typeof(int)).ToList().ToTask();
                await Assert.That(results).IsEmpty();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that GetAllKeys with a type filter only returns keys of that type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysWithTypeShouldFilterByType()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("str1", [1], typeof(string)).ToTask();
                await cache.Insert("int1", [2], typeof(int)).ToTask();

                var stringKeys = await cache.GetAllKeys(typeof(string)).ToList().ToTask();
                await Assert.That(stringKeys.Count).IsEqualTo(1);
                await Assert.That(stringKeys[0]).IsEqualTo("str1");
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that expired entries are not returned by Get, GetAllKeys, GetAll, and GetCreatedAt.
    /// This exercises the expiration predicate in all query paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExpiredEntriesShouldNotBeReturnedByAnyQueryMethod()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                var pastExpiration = DateTimeOffset.UtcNow.AddDays(-1);
                var futureExpiration = DateTimeOffset.UtcNow.AddDays(1);

                // Non-typed inserts with expiration
                await cache.Insert("expired_plain", [1], pastExpiration).ToTask();
                await cache.Insert("valid_plain", [2], futureExpiration).ToTask();

                // Typed inserts with expiration
                await cache.Insert("expired_typed", [3], typeof(string), pastExpiration).ToTask();
                await cache.Insert("valid_typed", [4], typeof(string), futureExpiration).ToTask();

                // Non-typed Get should not return expired
                await cache.Get("expired_plain").ToTask().ShouldThrowAsync<KeyNotFoundException>();
                var validData = await cache.Get("valid_plain").ToTask();
                await Assert.That(validData).IsNotNull();

                // Typed Get should not return expired
                await cache.Get("expired_typed", typeof(string)).ToTask().ShouldThrowAsync<KeyNotFoundException>();
                var validTyped = await cache.Get("valid_typed", typeof(string)).ToTask();
                await Assert.That(validTyped).IsNotNull();

                // Bulk Get should only return non-expired
                var bulkResults = await cache.Get(["expired_plain", "valid_plain"]).ToList().ToTask();
                await Assert.That(bulkResults.Count).IsEqualTo(1);

                var bulkTypedResults = await cache.Get(["expired_typed", "valid_typed"], typeof(string)).ToList().ToTask();
                await Assert.That(bulkTypedResults.Count).IsEqualTo(1);

                // GetAllKeys should only return non-expired
                var allKeys = await cache.GetAllKeys().ToList().ToTask();
                await Assert.That(allKeys).Contains("valid_plain");
                await Assert.That(allKeys).Contains("valid_typed");
                await Assert.That(allKeys).DoesNotContain("expired_plain");
                await Assert.That(allKeys).DoesNotContain("expired_typed");

                // GetAllKeys(type) should only return non-expired
                var typedKeys = await cache.GetAllKeys(typeof(string)).ToList().ToTask();
                await Assert.That(typedKeys.Count).IsEqualTo(1);
                await Assert.That(typedKeys[0]).IsEqualTo("valid_typed");

                // GetAll(type) should only return non-expired
                var allOfType = await cache.GetAll(typeof(string)).ToList().ToTask();
                await Assert.That(allOfType.Count).IsEqualTo(1);

                // GetCreatedAt should return null for expired
                var createdExpired = await cache.GetCreatedAt("expired_plain").ToTask();
                await Assert.That(createdExpired).IsNull();

                var createdExpiredTyped = await cache.GetCreatedAt("expired_typed", typeof(string)).ToTask();
                await Assert.That(createdExpiredTyped).IsNull();

                // Bulk GetCreatedAt should only return non-expired
                var createdBulk = await cache.GetCreatedAt(["expired_plain", "valid_plain"]).ToList().ToTask();
                await Assert.That(createdBulk.Count).IsEqualTo(1);

                var createdBulkTyped = await cache.GetCreatedAt(["expired_typed", "valid_typed"], typeof(string)).ToList().ToTask();
                await Assert.That(createdBulkTyped.Count).IsEqualTo(1);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests UpdateExpiration with typed entries, verifying that updating expiration
    /// to the past makes entries unretrievable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationToThePastShouldMakeEntryUnretrievable()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("k1", [1]).ToTask();
                await cache.Insert("k2", [2], typeof(string)).ToTask();
                await cache.Insert("k3", [3]).ToTask();
                await cache.Insert("k4", [4], typeof(string)).ToTask();

                // Update single non-typed to past
                await cache.UpdateExpiration("k1", DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await cache.Get("k1").ToTask().ShouldThrowAsync<KeyNotFoundException>();

                // Update single typed to past
                await cache.UpdateExpiration("k2", typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await cache.Get("k2", typeof(string)).ToTask().ShouldThrowAsync<KeyNotFoundException>();

                // Update bulk non-typed to past
                await cache.UpdateExpiration(["k3"], DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await cache.Get("k3").ToTask().ShouldThrowAsync<KeyNotFoundException>();

                // Update bulk typed to past
                await cache.UpdateExpiration(["k4"], typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await cache.Get("k4", typeof(string)).ToTask().ShouldThrowAsync<KeyNotFoundException>();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that Flush operations complete successfully without errors.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FlushShouldCompleteSuccessfully()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("k", [1]).ToTask();

                // Non-typed flush triggers WAL checkpoint
                await cache.Flush().ToTask();

                // Typed flush is a no-op on SQLite but should complete
                await cache.Flush(typeof(string)).ToTask();

                // Verify data is still accessible after flush
                var data = await cache.Get("k").ToTask();
                await Assert.That(data).IsNotNull();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests Vacuum removes expired entries and compacts the database.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldRemoveExpiredAndCompact()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("expired1", [1], DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await cache.Insert("expired2", [2], typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await cache.Insert("valid", [3], DateTimeOffset.UtcNow.AddDays(1)).ToTask();

                await cache.Vacuum().ToTask();

                var keys = await cache.GetAllKeys().ToList().ToTask();
                await Assert.That(keys.Count).IsEqualTo(1);
                await Assert.That(keys[0]).IsEqualTo("valid");
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that InvalidateAll with a specific type only removes entries of that type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllWithTypeShouldOnlyRemoveMatchingType()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("str1", [1], typeof(string)).ToTask();
                await cache.Insert("str2", [2], typeof(string)).ToTask();
                await cache.Insert("int1", [3], typeof(int)).ToTask();

                await cache.InvalidateAll(typeof(string)).ToTask();

                var remaining = await cache.GetAllKeys(typeof(int)).ToList().ToTask();
                await Assert.That(remaining.Count).IsEqualTo(1);
                await Assert.That(remaining[0]).IsEqualTo("int1");

                var stringKeys = await cache.GetAllKeys(typeof(string)).ToList().ToTask();
                await Assert.That(stringKeys).IsEmpty();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that Insert with null expiration stores entries that never expire.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertWithNullExpirationShouldNeverExpire()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                // Non-typed insert without expiration
                await cache.Insert("no_expiry", [1]).ToTask();

                // Typed insert without expiration
                await cache.Insert("no_expiry_typed", [2], typeof(string)).ToTask();

                // Bulk non-typed insert without expiration
                await cache.Insert([new("bulk1", [3])]).ToTask();

                // Bulk typed insert without expiration
                await cache.Insert([new("bulk2", [4])], typeof(string)).ToTask();

                // All should be retrievable
                var data1 = await cache.Get("no_expiry").ToTask();
                await Assert.That(data1).IsNotNull();

                var data2 = await cache.Get("no_expiry_typed", typeof(string)).ToTask();
                await Assert.That(data2).IsNotNull();

                var data3 = await cache.Get("bulk1").ToTask();
                await Assert.That(data3).IsNotNull();

                var data4 = await cache.Get("bulk2", typeof(string)).ToTask();
                await Assert.That(data4).IsNotNull();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that the HttpService property can be set and retrieved.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HttpServicePropertyShouldBeSettableAndGettable()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                // First access exercises the lazy-initialisation branch of the ??=
                // operator (the backing field is null and a new HttpService is created).
                var defaultService = cache.HttpService;
                await Assert.That(defaultService).IsNotNull();

                // Second access exercises the already-initialised branch of the ??=
                // operator: the backing field is non-null, so the existing instance
                // is returned verbatim.
                var secondAccess = cache.HttpService;
                await Assert.That(secondAccess).IsSameReferenceAs(defaultService);

                // Setting a custom service should work.
                HttpService customService = new();
                cache.HttpService = customService;
                await Assert.That(cache.HttpService).IsEqualTo(customService);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that ForcedDateTimeKind property can be set and retrieved.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindPropertyShouldBeSettableAndGettable()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await Assert.That(cache.ForcedDateTimeKind).IsNull();

                cache.ForcedDateTimeKind = DateTimeKind.Utc;
                await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that Scheduler property returns a valid scheduler.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SchedulerPropertyShouldReturnValidScheduler()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await Assert.That(cache.Scheduler).IsNotNull();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that Serializer property returns the serializer passed to the constructor.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializerPropertyShouldReturnConstructorSerializer()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            SystemJsonSerializer serializer = new();
            SqliteBlobCache cache = new(Path.Combine(path, $"test_{Guid.NewGuid():N}.db"), serializer);
            try
            {
                await Assert.That(cache.Serializer).IsEqualTo(serializer);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests constructor argument validation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowOnNullArgs()
    {
        await Assert.That(() => new SqliteBlobCache((string)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();
        await Assert.That(() => new SqliteBlobCache("test.db", null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new SqliteBlobCache((SQLite.SQLiteConnectionString)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that InvalidateAll removes all entries regardless of type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllShouldRemoveAllEntries()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("a", [1]).ToTask();
                await cache.Insert("b", [2], typeof(string)).ToTask();
                await cache.Insert("c", [3], typeof(int)).ToTask();

                await cache.InvalidateAll().ToTask();

                var keys = await cache.GetAllKeys().ToList().ToTask();
                await Assert.That(keys).IsEmpty();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that bulk Invalidate by keys removes only the specified keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BulkInvalidateShouldRemoveOnlySpecifiedKeys()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await cache.Insert("a", [1]).ToTask();
                await cache.Insert("b", [2]).ToTask();
                await cache.Insert("c", [3]).ToTask();

                await cache.Invalidate(["a", "b"]).ToTask();

                var keys = await cache.GetAllKeys().ToList().ToTask();
                await Assert.That(keys.Count).IsEqualTo(1);
                await Assert.That(keys[0]).IsEqualTo("c");
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that Insert with typed bulk entries with explicit expiration works correctly.
    /// This exercises the typed Insert(IEnumerable, Type, DateTimeOffset?) path with a future expiration.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypedBulkInsertWithExpirationShouldRoundTrip()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                var future = DateTimeOffset.UtcNow.AddHours(1);
                KeyValuePair<string, byte[]>[] pairs =
                [
                    new("tk1", [10]),
                    new("tk2", [20])
                ];
                await cache.Insert(pairs, typeof(string), future).ToTask();

                var results = await cache.Get(["tk1", "tk2"], typeof(string)).ToList().ToTask();
                await Assert.That(results.Count).IsEqualTo(2);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that UpdateExpiration with null expiration (no expiry) makes entry permanently available.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationToNullShouldMakeEntryPermanent()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                // Insert with a future expiration
                await cache.Insert("k1", [1], DateTimeOffset.UtcNow.AddMinutes(5)).ToTask();
                await cache.Insert("k2", [2], typeof(string), DateTimeOffset.UtcNow.AddMinutes(5)).ToTask();

                // Update to null expiration (permanent)
                await cache.UpdateExpiration("k1", null).ToTask();
                await cache.UpdateExpiration("k2", typeof(string), null).ToTask();
                await cache.UpdateExpiration(["k1"], null).ToTask();
                await cache.UpdateExpiration(["k2"], typeof(string), null).ToTask();

                var d1 = await cache.Get("k1").ToTask();
                await Assert.That(d1).IsNotNull();

                var d2 = await cache.Get("k2", typeof(string)).ToTask();
                await Assert.That(d2).IsNotNull();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that all public methods throw <see cref="ObjectDisposedException"/> after disposal
    /// when using an in-memory connection (no real SQLite database required).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposedShouldThrowForAllOperations()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.DisposeAsync();

        await cache.Insert("k", [1]).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Insert([new("k", [1])]).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Insert("k", [1], typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Insert([new("k", [1])], typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.Get("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Get(["k"]).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Get("k", typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Get(["k"], typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.GetAllKeys().ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.GetAllKeys(typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.GetAll(typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.GetCreatedAt("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.GetCreatedAt(["k"]).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.GetCreatedAt("k", typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.GetCreatedAt(["k"], typeof(string)).ToList().ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.Flush().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Flush(typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.Invalidate("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Invalidate(["k"]).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Invalidate("k", typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.Invalidate(["k"], typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.InvalidateAll().ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.InvalidateAll(typeof(string)).ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.Vacuum().ToTask().ShouldThrowAsync<ObjectDisposedException>();

        await cache.UpdateExpiration("k", DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.UpdateExpiration(["k"], DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
        await cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now).ToTask().ShouldThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that <see cref="SqliteBlobCache.BeforeWriteToDiskFilter"/> returns an error observable
    /// after the cache has been disposed, using an in-memory connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBeforeWriteToDiskFilterShouldThrowWhenDisposed()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.DisposeAsync();

        await cache.BeforeWriteToDiskFilter([1, 2, 3], ImmediateScheduler.Instance).ToTask().ShouldThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that <see cref="SqliteBlobCache.BeforeWriteToDiskFilter"/> returns data unchanged
    /// when the cache is active, using an in-memory connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBeforeWriteToDiskFilterShouldReturnDataWhenNotDisposed()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            byte[] input = [10, 20, 30];
            var result = await cache.BeforeWriteToDiskFilter(input, ImmediateScheduler.Instance).ToTask();
            await Assert.That(result).IsEquivalentTo(input);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that typed Insert silently succeeds when the transaction reports
    /// <see cref="IAkavacheTransaction.IsValid"/> as <c>false</c>, simulating
    /// a null or invalid underlying connection during transaction execution.
    /// The <c>IsValid</c> guard in the typed Insert path returns early without persisting data.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedInsertWithInvalidTransactionShouldNotPersist()
    {
        InMemoryAkavacheConnection connection = new() { SimulateNullConnection = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Typed insert uses the IsValid guard; data should not be persisted.
            await cache.Insert("k", [1], typeof(string)).ToTask();
            await cache.Get("k", typeof(string)).ToTask().ShouldThrowAsync<KeyNotFoundException>();

            // Typed bulk insert also uses the IsValid guard.
            await cache.Insert([new("k2", [2])], typeof(string)).ToTask();
            await cache.Get("k2", typeof(string)).ToTask().ShouldThrowAsync<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests basic CRUD operations using the in-memory connection, verifying that the
    /// <see cref="IAkavacheConnection"/> abstraction works correctly for Insert, Get,
    /// GetAllKeys, Invalidate, and InvalidateAll.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryCrudOperationsShouldWork()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Insert and Get
            await cache.Insert("k1", [1, 2]).ToTask();
            var data = await cache.Get("k1").ToTask();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!.Length).IsEqualTo(2);

            // Typed Insert and Get
            await cache.Insert("k2", [3], typeof(string)).ToTask();
            var typedData = await cache.Get("k2", typeof(string)).ToTask();
            await Assert.That(typedData).IsNotNull();

            // GetAllKeys
            var keys = await cache.GetAllKeys().ToList().ToTask();
            await Assert.That(keys.Count).IsEqualTo(2);

            // Invalidate single
            await cache.Invalidate("k1").ToTask();
            await cache.Get("k1").ToTask().ShouldThrowAsync<KeyNotFoundException>();

            // InvalidateAll
            await cache.InvalidateAll().ToTask();
            var remainingKeys = await cache.GetAllKeys().ToList().ToTask();
            await Assert.That(remainingKeys).IsEmpty();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that double async disposal does not throw when using an in-memory connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDoubleDisposeAsyncShouldNotThrow()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        await cache.DisposeAsync();
        await cache.DisposeAsync();

        await cache.Get("k").ToTask().ShouldThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that the constructor throws <see cref="ArgumentNullException"/> when a null
    /// <see cref="IAkavacheConnection"/> is passed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorWithNullConnectionShouldThrow() => await Assert.That(() => new SqliteBlobCache((IAkavacheConnection)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.UpdateExpiration(string, DateTimeOffset?)"/>
    /// is routed through the transaction's <see cref="IAkavacheTransaction.SetExpiry"/>
    /// helper and actually mutates the stored entry's expiration when using the in-memory backend.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryUpdateExpirationShouldMutateEntry()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("k1", [1], DateTimeOffset.UtcNow.AddMinutes(1)).ToTask();
            var newExpiry = DateTimeOffset.UtcNow.AddHours(2);

            await cache.UpdateExpiration("k1", newExpiry).ToTask();

            var stored = connection.Store["k1"];
            await Assert.That(stored.ExpiresAt).IsNotNull();
            await Assert.That(stored.ExpiresAt!.Value).IsEqualTo(newExpiry.UtcDateTime);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.UpdateExpiration(string, Type, DateTimeOffset?)"/>
    /// only affects entries whose <c>TypeName</c> column matches, leaving other entries untouched.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryUpdateExpirationWithTypeShouldRespectTypeFilter()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var initialExpiry = DateTimeOffset.UtcNow.AddMinutes(1);
            await cache.Insert("k1", [1], typeof(string), initialExpiry).ToTask();
            await cache.Insert("k1", [1], typeof(int)).ToTask(); // overwrites would happen only if same key+type; dictionary keyed by Id so last write wins

            // Insert a different key so we can prove type filter isolates the right row.
            await cache.Insert("k2", [2], typeof(string), initialExpiry).ToTask();

            var updatedExpiry = DateTimeOffset.UtcNow.AddDays(1);

            // Update k2 only, scoped to typeof(string).
            await cache.UpdateExpiration("k2", typeof(string), updatedExpiry).ToTask();

            var k2 = connection.Store["k2"];
            await Assert.That(k2.ExpiresAt!.Value).IsEqualTo(updatedExpiry.UtcDateTime);

            // Mismatching type filter should be a no-op.
            await cache.UpdateExpiration("k2", typeof(object), DateTimeOffset.UtcNow.AddYears(10)).ToTask();
            await Assert.That(connection.Store["k2"].ExpiresAt!.Value).IsEqualTo(updatedExpiry.UtcDateTime);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the bulk <see cref="SqliteBlobCache.UpdateExpiration(IEnumerable{string}, DateTimeOffset?)"/>
    /// overload updates all supplied keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBulkUpdateExpirationShouldMutateAllEntries()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("k1", [1]).ToTask();
            await cache.Insert("k2", [2]).ToTask();
            await cache.Insert("k3", [3]).ToTask();

            var updated = DateTimeOffset.UtcNow.AddHours(5);
            await cache.UpdateExpiration(["k1", "k2", "k3"], updated).ToTask();

            foreach (var id in new[] { "k1", "k2", "k3" })
            {
                await Assert.That(connection.Store[id].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
            }
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the typed bulk <see cref="SqliteBlobCache.UpdateExpiration(IEnumerable{string}, Type, DateTimeOffset?)"/>
    /// overload only touches entries with a matching <c>TypeName</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBulkUpdateExpirationWithTypeShouldRespectTypeFilter()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("a", [1], typeof(string), DateTimeOffset.UtcNow.AddMinutes(1)).ToTask();
            await cache.Insert("b", [2], typeof(string), DateTimeOffset.UtcNow.AddMinutes(1)).ToTask();

            var updated = DateTimeOffset.UtcNow.AddDays(3);
            await cache.UpdateExpiration(["a", "b"], typeof(string), updated).ToTask();

            await Assert.That(connection.Store["a"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
            await Assert.That(connection.Store["b"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);

            // Wrong type should leave both untouched.
            var wrongTypeExpiry = DateTimeOffset.UtcNow.AddYears(10);
            await cache.UpdateExpiration(["a", "b"], typeof(int), wrongTypeExpiry).ToTask();
            await Assert.That(connection.Store["a"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
            await Assert.That(connection.Store["b"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.Flush()"/> calls
    /// <see cref="IAkavacheConnection.CheckpointAsync"/> with <see cref="CheckpointMode.Passive"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryFlushShouldRequestPassiveCheckpoint()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Flush().ToTask();
            await Assert.That(connection.CheckpointCount).IsEqualTo(1);
            await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Passive);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that typed <see cref="SqliteBlobCache.Insert(string, byte[], Type, DateTimeOffset?)"/>
    /// triggers a passive checkpoint on the backend for multi-instance durability.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedInsertShouldCheckpointAfterWrite()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var before = connection.CheckpointCount;
            await cache.Insert("k", [1], typeof(string)).ToTask();
            await Assert.That(connection.CheckpointCount).IsGreaterThan(before);
            await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Passive);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.Vacuum"/> is routed through
    /// <see cref="IAkavacheConnection.CompactAsync"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryVacuumShouldCallCompact()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Vacuum().ToTask();
            await Assert.That(connection.CompactCount).IsEqualTo(1);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.DisposeAsync"/> issues a full checkpoint and
    /// then releases auxiliary resources before closing the connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposeAsyncShouldCheckpointAndReleaseAuxiliary()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        await cache.DisposeAsync();

        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Full);
        await Assert.That(connection.ReleaseAuxiliaryResourcesCount).IsGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.Get(string)"/> falls back to the V10 legacy
    /// backing store when the key is not present in the primary V11 table.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetShouldFallBackToLegacyV10Store()
    {
        InMemoryAkavacheConnection connection = new();
        connection.LegacyV10Store["legacyKey"] = [9, 8, 7];

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var data = await cache.Get("legacyKey").ToTask();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!).IsEquivalentTo(new byte[] { 9, 8, 7 });
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that typed <see cref="SqliteBlobCache.Get(string, Type)"/> falls back to the
    /// V10 legacy backing store when the key is not present in the primary V11 table.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedGetShouldFallBackToLegacyV10Store()
    {
        InMemoryAkavacheConnection connection = new();
        connection.LegacyV10Store["legacyTyped"] = [1, 2, 3, 4];

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var data = await cache.Get("legacyTyped", typeof(string)).ToTask();
            await Assert.That(data).IsNotNull();
            await Assert.That(data).IsEquivalentTo(new byte[] { 1, 2, 3, 4 });
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.Get(string)"/> throws
    /// <see cref="KeyNotFoundException"/> when the key is missing from both the
    /// primary and legacy V10 stores.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetMissingKeyShouldThrowKeyNotFound()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Get("missing").ToTask().ShouldThrowAsync<KeyNotFoundException>();
            await cache.Get("missing", typeof(string)).ToTask().ShouldThrowAsync<KeyNotFoundException>();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.Invalidate(IEnumerable{string}, Type)"/> only
    /// removes entries whose <c>TypeName</c> matches, leaving other entries intact.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryInvalidateWithTypeShouldOnlyRemoveTypedEntries()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("a", [1], typeof(string)).ToTask();
            await cache.Insert("b", [2]).ToTask(); // untyped

            await cache.Invalidate(["a", "b"], typeof(string)).ToTask();

            // "a" removed (typed match); "b" still present (no TypeName).
            await Assert.That(connection.Store.ContainsKey("a")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("b")).IsTrue();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies <see cref="SqliteBlobCache.InvalidateAll(Type)"/> removes only entries
    /// with a matching <c>TypeName</c> and leaves the rest intact.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryInvalidateAllWithTypeShouldOnlyRemoveTypedEntries()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("a", [1], typeof(string)).ToTask();
            await cache.Insert("b", [2], typeof(string)).ToTask();
            await cache.Insert("c", [3]).ToTask(); // untyped

            await cache.InvalidateAll(typeof(string)).ToTask();

            await Assert.That(connection.Store.ContainsKey("a")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("b")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("c")).IsTrue();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.GetCreatedAt(string)"/> returns the stored
    /// creation timestamp using the in-memory backend.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetCreatedAtShouldReturnStoredTime()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("k", [1]).ToTask();
            var createdAt = await cache.GetCreatedAt("k").ToTask();
            await Assert.That(createdAt).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.Flush()"/> completes successfully even when
    /// the backend checkpoint throws, exercising the catch branch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryFlushSwallowsCheckpointFailure()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Should not throw, even though CheckpointAsync raises.
            await cache.Flush().ToTask();
            await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            // Disable the failure so DisposeAsync can complete cleanly.
            connection.FailCheckpoint = false;
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies typed Insert swallows an inner <see cref="IAkavacheTransaction.InsertOrReplace"/>
    /// failure (exercising the inner try/catch in the entry loop) and also tolerates a post-write
    /// checkpoint failure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedInsertSwallowsInnerInsertFailure()
    {
        InMemoryAkavacheConnection connection = new()
        {
            FailInsertOrReplaceInTransaction = true,
            FailCheckpoint = true,
        };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Should complete without throwing even though both the inner insert and the
            // post-transaction checkpoint fail.
            await cache.Insert("k", [1], typeof(string)).ToTask();
            await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        }
        finally
        {
            connection.FailCheckpoint = false;
            connection.FailInsertOrReplaceInTransaction = false;
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies typed Insert swallows a <see cref="IAkavacheConnection.RunInTransactionAsync"/>
    /// failure at the outer try/catch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedInsertSwallowsOuterTransactionFailure()
    {
        InMemoryAkavacheConnection connection = new() { FailRunInTransaction = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Should complete without throwing even though the transaction fails outright.
            await cache.Insert("k", [1], typeof(string)).ToTask();
            await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        }
        finally
        {
            connection.FailRunInTransaction = false;
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.DisposeAsync"/> falls back from a failing
    /// checkpoint to <see cref="IAkavacheConnection.CompactAsync"/>, then continues on to
    /// release auxiliary resources and close.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposeAsyncFallsBackToCompactWhenCheckpointFails()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        await cache.DisposeAsync();

        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.CompactCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.ReleaseAuxiliaryResourcesCount).IsGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.DisposeAsync"/> tolerates all three teardown
    /// calls throwing (checkpoint, compact, and release auxiliary).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposeAsyncTolerantOfAllTeardownFailures()
    {
        InMemoryAkavacheConnection connection = new()
        {
            FailCheckpoint = true,
            FailCompact = true,
            FailReleaseAuxiliaryResources = true,
            FailClose = true,
        };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Should not throw even though every teardown operation raises.
        await cache.DisposeAsync();
    }

    /// <summary>
    /// Verifies synchronous <see cref="SqliteBlobCache.Dispose()"/> runs the best-effort
    /// cleanup path (truncate checkpoint, release auxiliary, close).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Test deliberately exercises the synchronous Dispose path.")]
    public async Task InMemorySyncDisposeRunsCleanupPath()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        cache.Dispose();

        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Truncate);
        await Assert.That(connection.ReleaseAuxiliaryResourcesCount).IsGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Verifies that synchronous <see cref="SqliteBlobCache.Dispose()"/> tolerates every
    /// teardown call throwing.
    /// </summary>
    [Test]
    public void InMemorySyncDisposeTolerantOfAllFailures()
    {
        InMemoryAkavacheConnection connection = new()
        {
            FailCheckpoint = true,
            FailReleaseAuxiliaryResources = true,
            FailClose = true,
        };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Should not throw.
        cache.Dispose();
    }

    /// <summary>
    /// Verifies that an error raised from <see cref="IAkavacheConnection.CreateTableAsync{T}"/>
    /// during initialization is surfaced on the first operation that awaits initialization.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryInitializationFailureShouldPropagate()
    {
        InMemoryAkavacheConnection connection = new() { FailCreateTable = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Get("k").ToTask().ShouldThrowAsync<InvalidOperationException>();
        }
        finally
        {
            connection.FailCreateTable = false;
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the OUTER <c>!tx.IsValid</c> guard inside typed <c>Insert</c> returns
    /// early without iterating any entries when the transaction is immediately invalid.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedInsertOuterInvalidGuardShouldReturnEarly()
    {
        InMemoryAkavacheConnection connection = new()
        {
            TransactionIsValidTrueCallsRemaining = 0,
        };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await cache.Insert("k", [1], typeof(string)).ToTask();
            await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        }
        finally
        {
            connection.TransactionIsValidTrueCallsRemaining = int.MaxValue;
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the inner mid-loop <c>!tx.IsValid</c> guard inside typed <c>Insert</c>
    /// returns early once the transaction reports invalid between iterations. The outer
    /// guard accepts the first <c>IsValid</c> call (so the transaction body runs) and the
    /// inner per-entry guard rejects subsequent calls.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedInsertInnerInvalidGuardShouldReturnEarly()
    {
        InMemoryAkavacheConnection connection = new()
        {
            // Outer guard consumes call 1 (true). Inner guard on iteration 1 consumes call 2 (false → return).
            TransactionIsValidTrueCallsRemaining = 1,
        };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Two entries so the loop must iterate at least once.
            await cache.Insert(
                [new("a", [1]), new("b", [2])],
                typeof(string)).ToTask();

            await Assert.That(connection.Store.ContainsKey("a")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("b")).IsFalse();
        }
        finally
        {
            connection.TransactionIsValidTrueCallsRemaining = int.MaxValue;
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the post-query defensive <c>x?.Id is not null</c> filter in the
    /// <c>Get(IEnumerable&lt;string&gt;)</c> overload skips entries with a null <c>Id</c>
    /// surfaced by the storage layer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBulkGetShouldSkipEntriesWithNullIdOrValue()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new() { Id = null, Value = [1] });
        connection.SeedRaw("nullValue", new() { Id = "nullValue", Value = null });
        connection.SeedRaw("good", new() { Id = "good", Value = [9] });

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var results = await cache.Get(["nullId", "nullValue", "good"]).ToList().ToTask();
            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].Key).IsEqualTo("good");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the typed bulk <c>Get(IEnumerable&lt;string&gt;, Type)</c> overload's
    /// post-query defensive filter skips entries with a null <c>Id</c> or <c>Value</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedBulkGetShouldSkipEntriesWithNullIdOrValue()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new() { Id = null, Value = [1], TypeName = typeof(string).FullName });
        connection.SeedRaw("nullValue", new() { Id = "nullValue", Value = null, TypeName = typeof(string).FullName });
        connection.SeedRaw("good", new() { Id = "good", Value = [9], TypeName = typeof(string).FullName });

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var results = await cache.Get(["nullId", "nullValue", "good"], typeof(string)).ToList().ToTask();
            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].Key).IsEqualTo("good");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.GetAll(Type)"/>'s post-query defensive filter
    /// skips entries with a null <c>Id</c> or <c>Value</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetAllShouldSkipEntriesWithNullIdOrValue()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new() { Id = null, Value = [1], TypeName = typeof(string).FullName });
        connection.SeedRaw("nullValue", new() { Id = "nullValue", Value = null, TypeName = typeof(string).FullName });
        connection.SeedRaw("good", new() { Id = "good", Value = [9], TypeName = typeof(string).FullName });

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var results = await cache.GetAll(typeof(string)).ToList().ToTask();
            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].Key).IsEqualTo("good");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.GetAllKeys()"/>'s post-query defensive filter
    /// skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetAllKeysShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new() { Id = null, Value = [1] });
        connection.SeedRaw("good", new() { Id = "good", Value = [9] });

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var keys = await cache.GetAllKeys().ToList().ToTask();
            await Assert.That(keys.Count).IsEqualTo(1);
            await Assert.That(keys[0]).IsEqualTo("good");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.GetAllKeys(Type)"/>'s post-query defensive
    /// filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetAllKeysWithTypeShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new() { Id = null, Value = [1], TypeName = typeof(string).FullName });
        connection.SeedRaw("good", new() { Id = "good", Value = [9], TypeName = typeof(string).FullName });

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var keys = await cache.GetAllKeys(typeof(string)).ToList().ToTask();
            await Assert.That(keys.Count).IsEqualTo(1);
            await Assert.That(keys[0]).IsEqualTo("good");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the bulk <see cref="SqliteBlobCache.GetCreatedAt(IEnumerable{string})"/>
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBulkGetCreatedAtShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new() { Id = null, Value = [1], CreatedAt = DateTime.UtcNow });
        connection.SeedRaw("good", new() { Id = "good", Value = [9], CreatedAt = DateTime.UtcNow });

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var results = await cache.GetCreatedAt(["nullId", "good"]).ToList().ToTask();
            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].Key).IsEqualTo("good");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the typed bulk <see cref="SqliteBlobCache.GetCreatedAt(IEnumerable{string}, Type)"/>
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedBulkGetCreatedAtShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new() { Id = null, Value = [1], CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName });
        connection.SeedRaw("good", new() { Id = "good", Value = [9], CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName });

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var results = await cache.GetCreatedAt(["nullId", "good"], typeof(string)).ToList().ToTask();
            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].Key).IsEqualTo("good");
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the single-key <see cref="SqliteBlobCache.GetCreatedAt(string)"/>
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetCreatedAtSingleShouldSkipNullIdEntry()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new() { Id = null, Value = [1], CreatedAt = DateTime.UtcNow });

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Defensive Where filters out the null-Id entry; the DefaultIfEmpty fallback yields null.
            var result = await cache.GetCreatedAt("nullId").ToTask();
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that the typed single-key <see cref="SqliteBlobCache.GetCreatedAt(string, Type)"/>
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetCreatedAtSingleTypedShouldSkipNullIdEntry()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new() { Id = null, Value = [1], CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName });

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var result = await cache.GetCreatedAt("nullId", typeof(string)).ToTask();
            await Assert.That(result).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests <see cref="SqliteBlobCache.GetOrCreateHttpService"/> constructs a
    /// fresh default <see cref="HttpService"/> when the cached value is
    /// <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateHttpServiceShouldConstructDefaultWhenNull()
    {
        var result = SqliteBlobCache.GetOrCreateHttpService(null);

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsTypeOf<HttpService>();
    }

    /// <summary>
    /// Tests <see cref="SqliteBlobCache.GetOrCreateHttpService"/> returns the
    /// already-cached instance when it is non-null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateHttpServiceShouldReturnExistingWhenNonNull()
    {
        HttpService existing = new();

        var result = SqliteBlobCache.GetOrCreateHttpService(existing);

        await Assert.That(result).IsSameReferenceAs(existing);
    }

    /// <summary>
    /// Tests <see cref="SqliteBlobCache.ToExpiryValue"/> returns the UTC
    /// <see cref="DateTime"/> component when given a non-null
    /// <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ToExpiryValueShouldReturnUtcDateTimeForNonNullOffset()
    {
        DateTimeOffset offset = new(2025, 6, 15, 12, 30, 0, TimeSpan.FromHours(5));

        var result = SqliteBlobCache.ToExpiryValue(offset);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(offset.UtcDateTime);
    }

    /// <summary>
    /// Tests <see cref="SqliteBlobCache.ToExpiryValue"/> returns
    /// <see langword="null"/> for a null offset.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ToExpiryValueShouldReturnNullForNullOffset()
    {
        var result = SqliteBlobCache.ToExpiryValue(null);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="SqliteBlobCache.TryGetLegacyValueAsync"/> delegates to
    /// <see cref="IAkavacheConnection.TryReadLegacyV10ValueAsync"/> on the supplied
    /// connection and returns whatever that returns.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetLegacyValueAsyncShouldReturnNullWhenLegacyRowMissing()
    {
        InMemoryAkavacheConnection connection = new();

        var result = await SqliteBlobCache.TryGetLegacyValueAsync(connection, "no-such-key", DateTimeOffset.UtcNow, null);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="SqliteBlobCache.InitializeDatabase"/> creates the schema
    /// on the supplied connection and completes the observable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeDatabaseShouldCompleteAndCreateTable()
    {
        InMemoryAkavacheConnection connection = new();

        var observable = SqliteBlobCache.InitializeDatabase(connection, ImmediateScheduler.Instance);
        await observable.ToTask();

        var tableExists = await connection.TableExistsAsync("CacheEntry");
        await Assert.That(tableExists).IsTrue();
    }

    /// <summary>
    /// Tests <see cref="SqliteBlobCache.InitializeDatabase"/> propagates errors
    /// when the underlying connection cannot create the table.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeDatabaseShouldErrorWhenCreateTableFails()
    {
        InMemoryAkavacheConnection connection = new() { FailCreateTable = true };

        var observable = SqliteBlobCache.InitializeDatabase(connection, ImmediateScheduler.Instance);

        await observable.ToTask().ShouldThrowAsync<Exception>();
    }

    /// <summary>
    /// Creates a new instance of <see cref="SqliteBlobCache"/> that utilizes an <see cref="InMemoryAkavacheConnection"/>
    /// for storage, enabling fast, in-memory operations for unit tests and logic validations.
    /// This method bypasses file-based persistence by storing data entirely in memory.
    /// </summary>
    /// <returns>A <see cref="SqliteBlobCache"/> instance backed by an in-memory connection.</returns>
    private static SqliteBlobCache CreateCache() =>
        new(new InMemoryAkavacheConnection(), new SystemJsonSerializer(), ImmediateScheduler.Instance);
}
