// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            await cache.DisposeAsync();

            await Assert.That(async () => await cache.Insert("k", [1]).ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])]).ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.Insert("k", [1], typeof(string)).ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])], typeof(string)).ToTask()).Throws<ObjectDisposedException>();

            await Assert.That(async () => await cache.Get("k").ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.Get(["k"]).ToList().ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.Get("k", typeof(string)).ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.Get(["k"], typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();

            await Assert.That(async () => await cache.GetAllKeys().ToList().ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.GetAllKeys(typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.GetAll(typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();

            await Assert.That(async () => await cache.GetCreatedAt("k").ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.GetCreatedAt(["k"]).ToList().ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.GetCreatedAt("k", typeof(string)).ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.GetCreatedAt(["k"], typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();

            await Assert.That(async () => await cache.Flush().ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.Flush(typeof(string)).ToTask()).Throws<ObjectDisposedException>();

            await Assert.That(async () => await cache.Invalidate("k").ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.Invalidate(["k"]).ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.Invalidate("k", typeof(string)).ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.Invalidate(["k"], typeof(string)).ToTask()).Throws<ObjectDisposedException>();

            await Assert.That(async () => await cache.InvalidateAll().ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.InvalidateAll(typeof(string)).ToTask()).Throws<ObjectDisposedException>();

            await Assert.That(async () => await cache.Vacuum().ToTask()).Throws<ObjectDisposedException>();

            await Assert.That(async () => await cache.UpdateExpiration("k", DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.UpdateExpiration(["k"], DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
            await Assert.That(async () => await cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
        }
    }

    /// <summary>
    /// Tests null argument validation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NullArgsShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                await Assert.That(async () => await cache.Get((IEnumerable<string>)null!).ToList().ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Get((string)null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Get("k", (Type)null!).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Get((IEnumerable<string>)null!, typeof(string)).ToList().ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Get(["k"], (Type)null!).ToList().ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetAll(null!).ToList().ToTask()).Throws<ArgumentNullException>();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                await cache.Insert("k1", [1, 2, 3], typeof(string)).ToTask();
                var data = await cache.Get("k1", typeof(string)).ToTask();

                await Assert.That(data).IsNotNull();
                await Assert.That(data!.Length).IsEqualTo(3);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                var pairs = new[]
                {
                    new KeyValuePair<string, byte[]>("k1", [1]),
                    new KeyValuePair<string, byte[]>("k2", [2]),
                };
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                await Assert.That(async () => await cache.Get("non_existent_key").ToTask()).Throws<KeyNotFoundException>();
                await Assert.That(async () => await cache.Get("non_existent_key", typeof(string)).ToTask()).Throws<KeyNotFoundException>();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                await Assert.That(async () => await cache.Get(string.Empty).ToTask()).Throws<ArgumentNullException>();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                await cache.Insert("k1", [1, 2]).ToTask();
                await cache.Insert(
                    [
                        new KeyValuePair<string, byte[]>("k2", [3]),
                        new KeyValuePair<string, byte[]>("k3", [4])
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                // GetCreatedAt null arg variants
                await Assert.That(async () => await cache.GetCreatedAt((string)null!).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetCreatedAt((IEnumerable<string>)null!).ToList().ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetCreatedAt("k", (Type)null!).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetCreatedAt((string)null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetCreatedAt(["k"], (Type)null!).ToList().ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetCreatedAt((IEnumerable<string>)null!, typeof(string)).ToList().ToTask()).Throws<ArgumentNullException>();

                // GetAllKeys null type
                await Assert.That(async () => await cache.GetAllKeys((Type)null!).ToList().ToTask()).Throws<ArgumentNullException>();

                // Insert null args
                await Assert.That(async () => await cache.Insert((IEnumerable<KeyValuePair<string, byte[]>>)null!).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Insert((IEnumerable<KeyValuePair<string, byte[]>>)null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])
                ], (Type)null!).ToTask()).Throws<ArgumentNullException>();

                // Insert(key, data, type) arg validation
                await Assert.That(async () => await cache.Insert(string.Empty, [1], typeof(string)).ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.Insert("  ", [1], typeof(string)).ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.Insert("k", (byte[])null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Insert("k", [1], (Type)null!).ToTask()).Throws<ArgumentNullException>();

                // Invalidate arg validation
                await Assert.That(async () => await cache.Invalidate(string.Empty).ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.Invalidate("   ").ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.Invalidate(string.Empty, typeof(string)).ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.Invalidate("k", (Type)null!).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Invalidate((IEnumerable<string>)null!).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Invalidate((IEnumerable<string>)null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Invalidate(["k"], (Type)null!).ToTask()).Throws<ArgumentNullException>();

                // UpdateExpiration arg validation
                await Assert.That(async () => await cache.UpdateExpiration(string.Empty, DateTimeOffset.Now).ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.UpdateExpiration(string.Empty, typeof(string), DateTimeOffset.Now).ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.UpdateExpiration("k", (Type)null!, DateTimeOffset.Now).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.UpdateExpiration((IEnumerable<string>)null!, DateTimeOffset.Now).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.UpdateExpiration((IEnumerable<string>)null!, typeof(string), DateTimeOffset.Now).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.UpdateExpiration(["k"], (Type)null!, DateTimeOffset.Now).ToTask()).Throws<ArgumentNullException>();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            await cache.Insert("k1", [1]).ToTask();

            // Synchronous dispose exercises the Dispose(bool) wal_checkpoint/journal/close paths
            cache.Dispose();

            // Second dispose is a no-op (early return)
            cache.Dispose();

            await Assert.That(async () => await cache.Get("k1").ToTask()).Throws<ObjectDisposedException>();
        }
    }

    /// <summary>
    /// Tests GetCreatedAt for a key that does not exist returns null via DefaultIfEmpty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtForMissingKeyShouldReturnNull()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                await cache.Insert("expired", [1], typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).ToTask();

                await Assert.That(async () => await cache.Get("expired", typeof(string)).ToTask()).Throws<KeyNotFoundException>();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            await cache.DisposeAsync();

            await Assert.That(async () => await cache.BeforeWriteToDiskFilter([1, 2, 3], ImmediateScheduler.Instance).ToTask()).Throws<ObjectDisposedException>();
        }
    }

    /// <summary>
    /// Tests that BeforeWriteToDiskFilter returns data unchanged when cache is active.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BeforeWriteToDiskFilterShouldReturnDataWhenNotDisposed()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                var input = new byte[] { 10, 20, 30 };
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            await cache.Insert("k", [1]).ToTask();

            await cache.DisposeAsync();
            await cache.DisposeAsync();

            await Assert.That(async () => await cache.Get("k").ToTask()).Throws<ObjectDisposedException>();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                // Insert a valid entry to ensure the db is initialized, then look for a missing one
                await cache.Insert("existing", [1, 2]).ToTask();

                // Non-typed Get for missing key exercises full fallback path
                await Assert.That(async () => await cache.Get("nonexistent").ToTask()).Throws<KeyNotFoundException>();

                // Typed Get for missing key exercises typed fallback path
                await Assert.That(async () => await cache.Get("nonexistent", typeof(int)).ToTask()).Throws<KeyNotFoundException>();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
                await Assert.That(async () => await cache.Get("expired_plain").ToTask()).Throws<KeyNotFoundException>();
                var validData = await cache.Get("valid_plain").ToTask();
                await Assert.That(validData).IsNotNull();

                // Typed Get should not return expired
                await Assert.That(async () => await cache.Get("expired_typed", typeof(string)).ToTask()).Throws<KeyNotFoundException>();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                await cache.Insert("k1", [1]).ToTask();
                await cache.Insert("k2", [2], typeof(string)).ToTask();
                await cache.Insert("k3", [3]).ToTask();
                await cache.Insert("k4", [4], typeof(string)).ToTask();

                // Update single non-typed to past
                await cache.UpdateExpiration("k1", DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await Assert.That(async () => await cache.Get("k1").ToTask()).Throws<KeyNotFoundException>();

                // Update single typed to past
                await cache.UpdateExpiration("k2", typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await Assert.That(async () => await cache.Get("k2", typeof(string)).ToTask()).Throws<KeyNotFoundException>();

                // Update bulk non-typed to past
                await cache.UpdateExpiration(["k3"], DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await Assert.That(async () => await cache.Get("k3").ToTask()).Throws<KeyNotFoundException>();

                // Update bulk typed to past
                await cache.UpdateExpiration(["k4"], typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).ToTask();
                await Assert.That(async () => await cache.Get("k4", typeof(string)).ToTask()).Throws<KeyNotFoundException>();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                // Non-typed insert without expiration
                await cache.Insert("no_expiry", [1]).ToTask();

                // Typed insert without expiration
                await cache.Insert("no_expiry_typed", [2], typeof(string)).ToTask();

                // Bulk non-typed insert without expiration
                await cache.Insert([new KeyValuePair<string, byte[]>("bulk1", [3])]).ToTask();

                // Bulk typed insert without expiration
                await cache.Insert([new KeyValuePair<string, byte[]>("bulk2", [4])], typeof(string)).ToTask();

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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
                var customService = new HttpService();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
            var serializer = new SystemJsonSerializer();
            var cache = new SqliteBlobCache(Path.Combine(path, $"test_{Guid.NewGuid():N}.db"), serializer);
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
        await Assert.That(() => new SqliteBlobCache("test.db", (ISerializer)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new SqliteBlobCache((SQLite.SQLiteConnectionString)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that InvalidateAll removes all entries regardless of type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllShouldRemoveAllEntries()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                var future = DateTimeOffset.UtcNow.AddHours(1);
                var pairs = new[]
                {
                    new KeyValuePair<string, byte[]>("tk1", [10]),
                    new KeyValuePair<string, byte[]>("tk2", [20]),
                };
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                // Insert with a future expiration
                await cache.Insert("k1", [1], DateTimeOffset.UtcNow.AddMinutes(5)).ToTask();
                await cache.Insert("k2", [2], typeof(string), DateTimeOffset.UtcNow.AddMinutes(5)).ToTask();

                // Update to null expiration (permanent)
                await cache.UpdateExpiration("k1", (DateTimeOffset?)null).ToTask();
                await cache.UpdateExpiration("k2", typeof(string), (DateTimeOffset?)null).ToTask();
                await cache.UpdateExpiration(["k1"], (DateTimeOffset?)null).ToTask();
                await cache.UpdateExpiration(["k2"], typeof(string), (DateTimeOffset?)null).ToTask();

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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Insert("k", [1]).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])]).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Insert("k", [1], typeof(string)).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])], typeof(string)).ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.Get("k").ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Get(["k"]).ToList().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Get("k", typeof(string)).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Get(["k"], typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.GetAllKeys().ToList().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.GetAllKeys(typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.GetAll(typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.GetCreatedAt("k").ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.GetCreatedAt(["k"]).ToList().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.GetCreatedAt("k", typeof(string)).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.GetCreatedAt(["k"], typeof(string)).ToList().ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.Flush().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Flush(typeof(string)).ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.Invalidate("k").ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Invalidate(["k"]).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Invalidate("k", typeof(string)).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.Invalidate(["k"], typeof(string)).ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.InvalidateAll().ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.InvalidateAll(typeof(string)).ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.Vacuum().ToTask()).Throws<ObjectDisposedException>();

        await Assert.That(async () => await cache.UpdateExpiration("k", DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.UpdateExpiration(["k"], DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
        await Assert.That(async () => await cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now).ToTask()).Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that <see cref="SqliteBlobCache.BeforeWriteToDiskFilter"/> returns an error observable
    /// after the cache has been disposed, using an in-memory connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBeforeWriteToDiskFilterShouldThrowWhenDisposed()
    {
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.BeforeWriteToDiskFilter([1, 2, 3], ImmediateScheduler.Instance).ToTask()).Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that <see cref="SqliteBlobCache.BeforeWriteToDiskFilter"/> returns data unchanged
    /// when the cache is active, using an in-memory connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBeforeWriteToDiskFilterShouldReturnDataWhenNotDisposed()
    {
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var input = new byte[] { 10, 20, 30 };
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
        var connection = new InMemoryAkavacheConnection { SimulateNullConnection = true };
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Typed insert uses the IsValid guard; data should not be persisted.
            await cache.Insert("k", [1], typeof(string)).ToTask();
            await Assert.That(async () => await cache.Get("k", typeof(string)).ToTask()).Throws<KeyNotFoundException>();

            // Typed bulk insert also uses the IsValid guard.
            await cache.Insert([new KeyValuePair<string, byte[]>("k2", [2])], typeof(string)).ToTask();
            await Assert.That(async () => await cache.Get("k2", typeof(string)).ToTask()).Throws<KeyNotFoundException>();
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
            await Assert.That(async () => await cache.Get("k1").ToTask()).Throws<KeyNotFoundException>();

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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        await cache.DisposeAsync();
        await cache.DisposeAsync();

        await Assert.That(async () => await cache.Get("k").ToTask()).Throws<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that the constructor throws <see cref="ArgumentNullException"/> when a null
    /// <see cref="IAkavacheConnection"/> is passed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorWithNullConnectionShouldThrow()
    {
        await Assert.That(() => new SqliteBlobCache((IAkavacheConnection)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that <see cref="SqliteBlobCache.UpdateExpiration(string, DateTimeOffset?)"/>
    /// is routed through the transaction's <see cref="IAkavacheTransaction.SetExpiry"/>
    /// helper and actually mutates the stored entry's expiration when using the in-memory backend.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryUpdateExpirationShouldMutateEntry()
    {
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        var connection = new InMemoryAkavacheConnection();
        connection.LegacyV10Store["legacyKey"] = [9, 8, 7];

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection();
        connection.LegacyV10Store["legacyTyped"] = [1, 2, 3, 4];

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var data = await cache.Get("legacyTyped", typeof(string)).ToTask();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!).IsEquivalentTo(new byte[] { 1, 2, 3, 4 });
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await Assert.That(async () => await cache.Get("missing").ToTask()).Throws<KeyNotFoundException>();
            await Assert.That(async () => await cache.Get("missing", typeof(string)).ToTask()).Throws<KeyNotFoundException>();
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { FailCheckpoint = true };
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection
        {
            FailInsertOrReplaceInTransaction = true,
            FailCheckpoint = true,
        };
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { FailRunInTransaction = true };
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { FailCheckpoint = true };
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        var connection = new InMemoryAkavacheConnection
        {
            FailCheckpoint = true,
            FailCompact = true,
            FailReleaseAuxiliaryResources = true,
            FailClose = true,
        };
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        var connection = new InMemoryAkavacheConnection();
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        var connection = new InMemoryAkavacheConnection
        {
            FailCheckpoint = true,
            FailReleaseAuxiliaryResources = true,
            FailClose = true,
        };
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
        var connection = new InMemoryAkavacheConnection { FailCreateTable = true };
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            await Assert.That(async () => await cache.Get("k").ToTask()).Throws<InvalidOperationException>();
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
        var connection = new InMemoryAkavacheConnection
        {
            TransactionIsValidTrueCallsRemaining = 0,
        };
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection
        {
            // Outer guard consumes call 1 (true). Inner guard on iteration 1 consumes call 2 (false → return).
            TransactionIsValidTrueCallsRemaining = 1,
        };
        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Two entries so the loop must iterate at least once.
            await cache.Insert(
                [new KeyValuePair<string, byte[]>("a", [1]), new KeyValuePair<string, byte[]>("b", [2])],
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
        var connection = new InMemoryAkavacheConnection { BypassPredicate = true };
        connection.SeedRaw("nullId", new CacheEntry { Id = null, Value = [1] });
        connection.SeedRaw("nullValue", new CacheEntry { Id = "nullValue", Value = null });
        connection.SeedRaw("good", new CacheEntry { Id = "good", Value = [9] });

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { BypassPredicate = true };
        connection.SeedRaw("nullId", new CacheEntry { Id = null, Value = [1], TypeName = typeof(string).FullName });
        connection.SeedRaw("nullValue", new CacheEntry { Id = "nullValue", Value = null, TypeName = typeof(string).FullName });
        connection.SeedRaw("good", new CacheEntry { Id = "good", Value = [9], TypeName = typeof(string).FullName });

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { BypassPredicate = true };
        connection.SeedRaw("nullId", new CacheEntry { Id = null, Value = [1], TypeName = typeof(string).FullName });
        connection.SeedRaw("nullValue", new CacheEntry { Id = "nullValue", Value = null, TypeName = typeof(string).FullName });
        connection.SeedRaw("good", new CacheEntry { Id = "good", Value = [9], TypeName = typeof(string).FullName });

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { BypassPredicate = true };
        connection.SeedRaw("nullId", new CacheEntry { Id = null, Value = [1] });
        connection.SeedRaw("good", new CacheEntry { Id = "good", Value = [9] });

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { BypassPredicate = true };
        connection.SeedRaw("nullId", new CacheEntry { Id = null, Value = [1], TypeName = typeof(string).FullName });
        connection.SeedRaw("good", new CacheEntry { Id = "good", Value = [9], TypeName = typeof(string).FullName });

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { BypassPredicate = true };
        connection.SeedRaw("nullId", new CacheEntry { Id = null, Value = [1], CreatedAt = DateTime.UtcNow });
        connection.SeedRaw("good", new CacheEntry { Id = "good", Value = [9], CreatedAt = DateTime.UtcNow });

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { BypassPredicate = true };
        connection.SeedRaw("nullId", new CacheEntry { Id = null, Value = [1], CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName });
        connection.SeedRaw("good", new CacheEntry { Id = "good", Value = [9], CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName });

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { BypassPredicate = true };
        connection.SeedRaw("nullId", new CacheEntry { Id = null, Value = [1], CreatedAt = DateTime.UtcNow });

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var connection = new InMemoryAkavacheConnection { BypassPredicate = true };
        connection.SeedRaw("nullId", new CacheEntry { Id = null, Value = [1], CreatedAt = DateTime.UtcNow, TypeName = typeof(string).FullName });

        var cache = new SqliteBlobCache(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
        var existing = new HttpService();

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
        var offset = new DateTimeOffset(2025, 6, 15, 12, 30, 0, TimeSpan.FromHours(5));

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
        var connection = new InMemoryAkavacheConnection();

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
        var connection = new InMemoryAkavacheConnection();

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
        var connection = new InMemoryAkavacheConnection { FailCreateTable = true };

        var observable = SqliteBlobCache.InitializeDatabase(connection, ImmediateScheduler.Instance);

        await Assert.That(async () => await observable.ToTask())
            .Throws<Exception>();
    }

    /// <summary>
    /// Creates a <see cref="SqliteBlobCache"/> backed by an <see cref="InMemoryAkavacheConnection"/>
    /// rather than a real SQLite file. The <paramref name="path"/> argument is retained for
    /// call-site compatibility but is ignored — the cache's storage lives entirely in-memory,
    /// which dramatically speeds up the pure-logic tests. End-to-end validation against a real
    /// SQLite file lives in <c>SqliteBlobCacheSmokeTests</c> and
    /// <c>SqliteAkavacheConnectionTests</c>.
    /// </summary>
    /// <param name="path">Ignored; retained for call-site compatibility with the file-backed overload.</param>
    /// <returns>A new <see cref="SqliteBlobCache"/> backed by an in-memory connection.</returns>
    private static SqliteBlobCache CreateCache(string path) =>
        new(new InMemoryAkavacheConnection(), new SystemJsonSerializer(), ImmediateScheduler.Instance);
}
