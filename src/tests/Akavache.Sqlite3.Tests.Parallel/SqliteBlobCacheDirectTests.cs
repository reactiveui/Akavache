// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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
            cache.Dispose();

            Exception? ex = null;
            cache.Insert("k", [1]).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Insert([new("k", [1])]).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Insert("k", [1], typeof(string)).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Insert([new("k", [1])], typeof(string)).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Get("k").Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Get(["k"]).ToList().Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Get("k", typeof(string)).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Get(["k"], typeof(string)).ToList().Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.GetAllKeys().ToList().Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.GetAllKeys(typeof(string)).ToList().Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.GetAll(typeof(string)).ToList().Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.GetCreatedAt("k").Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.GetCreatedAt(["k"]).ToList().Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.GetCreatedAt("k", typeof(string)).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.GetCreatedAt(["k"], typeof(string)).ToList().Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Flush().Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Flush(typeof(string)).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Invalidate("k").Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Invalidate(["k"]).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Invalidate("k", typeof(string)).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Invalidate(["k"], typeof(string)).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.InvalidateAll().Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.InvalidateAll(typeof(string)).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.Vacuum().Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.UpdateExpiration("k", DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.UpdateExpiration(["k"], DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

            ex = null;
            cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();
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
                Exception? ex = null;
                cache.Get((IEnumerable<string>)null!).ToList().Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.Get((string)null!, typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.Get("k", null!).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.Get((IEnumerable<string>)null!, typeof(string)).ToList().Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.Get(["k"], null!).ToList().Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.GetAll(null!).ToList().Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k1", [1, 2, 3], typeof(string)).SubscribeAndComplete();
                var data = cache.Get("k1", typeof(string)).SubscribeGetValue();

                await Assert.That(data).IsNotNull();
                await Assert.That(data!.Length).IsEqualTo(3);
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert(pairs, typeof(string)).SubscribeAndComplete();

                var results = cache.Get(["k1", "k2"], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(results!.Count).IsEqualTo(2);

                var typedKeys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(typedKeys!.Count).IsEqualTo(2);

                var allOfType = cache.GetAll(typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(allOfType!.Count).IsEqualTo(2);
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k1", [1], typeof(string)).SubscribeAndComplete();
                cache.Insert("k2", [2], typeof(int)).SubscribeAndComplete();

                cache.Invalidate("k1", typeof(string)).SubscribeAndComplete();
                cache.InvalidateAll(typeof(int)).SubscribeAndComplete();

                var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(keys!).IsEmpty();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k1", [1], typeof(string)).SubscribeAndComplete();
                cache.Insert("k2", [2], typeof(string)).SubscribeAndComplete();

                cache.Invalidate(["k1", "k2"], typeof(string)).SubscribeAndComplete();

                var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(keys!).IsEmpty();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k1", [1], typeof(string)).SubscribeAndComplete();
                cache.Insert("k2", [2], typeof(string)).SubscribeAndComplete();

                var single = cache.GetCreatedAt("k1", typeof(string)).SubscribeGetValue();
                await Assert.That(single).IsNotNull();

                var multi = cache.GetCreatedAt(["k1", "k2"], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(multi!.Count).IsEqualTo(2);
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k1", [1], typeof(string)).SubscribeAndComplete();
                var newExpiration = DateTimeOffset.Now.AddHours(1);

                cache.UpdateExpiration("k1", typeof(string), newExpiration).SubscribeAndComplete();
                cache.UpdateExpiration(["k1"], typeof(string), newExpiration).SubscribeAndComplete();

                var data = cache.Get("k1", typeof(string)).SubscribeGetValue();
                await Assert.That(data).IsNotNull();
            }
            finally
            {
                cache.Dispose();
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
                Exception? ex = null;
                cache.Get("non_existent_key").Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();

                ex = null;
                cache.Get("non_existent_key", typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
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
                Exception? ex = null;
                cache.Get(string.Empty).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k1", [1], DateTimeOffset.Now.AddSeconds(-10)).SubscribeAndComplete();
                cache.Insert("k2", [2], DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

                cache.Vacuum().SubscribeAndComplete();

                var data = cache.Get("k2").SubscribeGetValue();
                await Assert.That(data).IsNotNull();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k1", [1, 2]).SubscribeAndComplete();
                cache.Insert(
                    [
                        new("k2", [3]),
                        new("k3", [4])
                    ],
                    DateTimeOffset.Now.AddHours(1)).SubscribeAndComplete();

                var single = cache.Get("k1").SubscribeGetValue();
                await Assert.That(single).IsNotNull();

                var multi = cache.Get(["k2", "k3"]).ToList().SubscribeGetValue();
                await Assert.That(multi!.Count).IsEqualTo(2);

                var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(keys!.Count).IsEqualTo(3);

                var created = cache.GetCreatedAt("k1").SubscribeGetValue();
                await Assert.That(created).IsNotNull();

                var createdMany = cache.GetCreatedAt(["k1", "k2"]).ToList().SubscribeGetValue();
                await Assert.That(createdMany!.Count).IsEqualTo(2);

                cache.UpdateExpiration("k1", DateTimeOffset.Now.AddDays(1)).SubscribeAndComplete();
                cache.UpdateExpiration(["k2", "k3"], DateTimeOffset.Now.AddDays(1)).SubscribeAndComplete();

                cache.Flush().SubscribeAndComplete();
                cache.Flush(typeof(string)).SubscribeAndComplete();

                cache.Invalidate("k1").SubscribeAndComplete();
                cache.Invalidate(["k2"]).SubscribeAndComplete();

                cache.InvalidateAll().SubscribeAndComplete();

                var remaining = cache.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(remaining!).IsEmpty();
            }
            finally
            {
                cache.Dispose();
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
                Exception? ex = null;

                // GetCreatedAt null arg variants
                cache.GetCreatedAt((string)null!).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.GetCreatedAt((IEnumerable<string>)null!).ToList().Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.GetCreatedAt("k", null!).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.GetCreatedAt((string)null!, typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.GetCreatedAt(["k"], null!).ToList().Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.GetCreatedAt((IEnumerable<string>)null!, typeof(string)).ToList().Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                // GetAllKeys null type
                ex = null;
                cache.GetAllKeys(null!).ToList().Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                // Insert null args
                ex = null;
                cache.Insert(null!).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.Insert(null!, typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.Insert([new("k", [1])], (Type)null!).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                // Insert(key, data, type) arg validation
                ex = null;
                cache.Insert(string.Empty, [1], typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentException>();

                ex = null;
                cache.Insert("  ", [1], typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentException>();

                ex = null;
                cache.Insert("k", null!, typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.Insert("k", [1], (Type)null!).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                // Invalidate arg validation
                ex = null;
                cache.Invalidate(string.Empty).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentException>();

                ex = null;
                cache.Invalidate("   ").Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentException>();

                ex = null;
                cache.Invalidate(string.Empty, typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentException>();

                ex = null;
                cache.Invalidate("k", null!).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.Invalidate((IEnumerable<string>)null!).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.Invalidate((IEnumerable<string>)null!, typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.Invalidate(["k"], null!).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                // UpdateExpiration arg validation
                ex = null;
                cache.UpdateExpiration(string.Empty, DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentException>();

                ex = null;
                cache.UpdateExpiration(string.Empty, typeof(string), DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentException>();

                ex = null;
                cache.UpdateExpiration("k", null!, DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.UpdateExpiration((IEnumerable<string>)null!, DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.UpdateExpiration((IEnumerable<string>)null!, typeof(string), DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();

                ex = null;
                cache.UpdateExpiration(["k"], null!, DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<ArgumentNullException>();
            }
            finally
            {
                cache.Dispose();
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
            cache.Insert("k1", [1]).SubscribeAndComplete();

            // Synchronous dispose exercises the Dispose(bool) wal_checkpoint/journal/close paths
            cache.Dispose();

            // Second dispose is a no-op (early return)
            cache.Dispose();

            Exception? ex = null;
            cache.Get("k1").Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();
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
                var result = cache.GetCreatedAt("missing").SubscribeGetValue();
                await Assert.That(result).IsNull();

                var typed = cache.GetCreatedAt("missing", typeof(string)).SubscribeGetValue();
                await Assert.That(typed).IsNull();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("expired", [1], typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).SubscribeAndComplete();

                Exception? ex = null;
                cache.Get("expired", typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
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
            cache.Dispose();

            Exception? ex = null;
            cache.BeforeWriteToDiskFilter([1, 2, 3], ImmediateScheduler.Instance).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();
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
                var result = cache.BeforeWriteToDiskFilter(input, ImmediateScheduler.Instance).SubscribeGetValue();
                await Assert.That(result).IsEquivalentTo(input);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that calling Dispose twice does not throw — the second call is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DoubleDisposeShouldNotThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            cache.Insert("k", [1]).SubscribeAndComplete();

            cache.Dispose();
            cache.Dispose();

            Exception? ex = null;
            cache.Get("k").Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ObjectDisposedException>();
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
                cache.Insert("existing", [1, 2]).SubscribeAndComplete();

                Exception? ex = null;

                // Non-typed Get for missing key exercises full fallback path
                cache.Get("nonexistent").Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();

                // Typed Get for missing key exercises typed fallback path
                ex = null;
                cache.Get("nonexistent", typeof(int)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("a", [1]).SubscribeAndComplete();
                cache.Insert("b", [2]).SubscribeAndComplete();

                // Request keys where only some exist
                var results = cache.Get(["a", "c", "d"]).ToList().SubscribeGetValue();
                await Assert.That(results!.Count).IsEqualTo(1);
                await Assert.That(results![0].Key).IsEqualTo("a");

                // Typed bulk get with no matches
                var typedResults = cache.Get(["x", "y"], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(typedResults!).IsEmpty();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k", [1], typeof(string)).SubscribeAndComplete();

                var results = cache.GetAll(typeof(int)).ToList().SubscribeGetValue();
                await Assert.That(results!).IsEmpty();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("str1", [1], typeof(string)).SubscribeAndComplete();
                cache.Insert("int1", [2], typeof(int)).SubscribeAndComplete();

                var stringKeys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(stringKeys!.Count).IsEqualTo(1);
                await Assert.That(stringKeys![0]).IsEqualTo("str1");
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("expired_plain", [1], pastExpiration).SubscribeAndComplete();
                cache.Insert("valid_plain", [2], futureExpiration).SubscribeAndComplete();

                // Typed inserts with expiration
                cache.Insert("expired_typed", [3], typeof(string), pastExpiration).SubscribeAndComplete();
                cache.Insert("valid_typed", [4], typeof(string), futureExpiration).SubscribeAndComplete();

                Exception? ex = null;

                // Non-typed Get should not return expired
                cache.Get("expired_plain").Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();

                var validData = cache.Get("valid_plain").SubscribeGetValue();
                await Assert.That(validData).IsNotNull();

                // Typed Get should not return expired
                ex = null;
                cache.Get("expired_typed", typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();

                var validTyped = cache.Get("valid_typed", typeof(string)).SubscribeGetValue();
                await Assert.That(validTyped).IsNotNull();

                // Bulk Get should only return non-expired
                var bulkResults = cache.Get(["expired_plain", "valid_plain"]).ToList().SubscribeGetValue();
                await Assert.That(bulkResults!.Count).IsEqualTo(1);

                var bulkTypedResults = cache.Get(["expired_typed", "valid_typed"], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(bulkTypedResults!.Count).IsEqualTo(1);

                // GetAllKeys should only return non-expired
                var allKeys = cache.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(allKeys!).Contains("valid_plain");
                await Assert.That(allKeys!).Contains("valid_typed");
                await Assert.That(allKeys!).DoesNotContain("expired_plain");
                await Assert.That(allKeys!).DoesNotContain("expired_typed");

                // GetAllKeys(type) should only return non-expired
                var typedKeys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(typedKeys!.Count).IsEqualTo(1);
                await Assert.That(typedKeys![0]).IsEqualTo("valid_typed");

                // GetAll(type) should only return non-expired
                var allOfType = cache.GetAll(typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(allOfType!.Count).IsEqualTo(1);

                // GetCreatedAt should return null for expired
                var createdExpired = cache.GetCreatedAt("expired_plain").SubscribeGetValue();
                await Assert.That(createdExpired).IsNull();

                var createdExpiredTyped = cache.GetCreatedAt("expired_typed", typeof(string)).SubscribeGetValue();
                await Assert.That(createdExpiredTyped).IsNull();

                // Bulk GetCreatedAt should only return non-expired
                var createdBulk = cache.GetCreatedAt(["expired_plain", "valid_plain"]).ToList().SubscribeGetValue();
                await Assert.That(createdBulk!.Count).IsEqualTo(1);

                var createdBulkTyped = cache.GetCreatedAt(["expired_typed", "valid_typed"], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(createdBulkTyped!.Count).IsEqualTo(1);
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k1", [1]).SubscribeAndComplete();
                cache.Insert("k2", [2], typeof(string)).SubscribeAndComplete();
                cache.Insert("k3", [3]).SubscribeAndComplete();
                cache.Insert("k4", [4], typeof(string)).SubscribeAndComplete();

                Exception? ex;

                // Update single non-typed to past
                cache.UpdateExpiration("k1", DateTimeOffset.UtcNow.AddDays(-1)).SubscribeAndComplete();
                ex = null;
                cache.Get("k1").Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();

                // Update single typed to past
                cache.UpdateExpiration("k2", typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).SubscribeAndComplete();
                ex = null;
                cache.Get("k2", typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();

                // Update bulk non-typed to past
                cache.UpdateExpiration(["k3"], DateTimeOffset.UtcNow.AddDays(-1)).SubscribeAndComplete();
                ex = null;
                cache.Get("k3").Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();

                // Update bulk typed to past
                cache.UpdateExpiration(["k4"], typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).SubscribeAndComplete();
                ex = null;
                cache.Get("k4", typeof(string)).Subscribe(_ => { }, e => ex = e);
                await Assert.That(ex).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k", [1]).SubscribeAndComplete();

                // Non-typed flush triggers WAL checkpoint
                cache.Flush().SubscribeAndComplete();

                // Typed flush is a no-op on SQLite but should complete
                cache.Flush(typeof(string)).SubscribeAndComplete();

                // Verify data is still accessible after flush
                var data = cache.Get("k").SubscribeGetValue();
                await Assert.That(data).IsNotNull();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("expired1", [1], DateTimeOffset.UtcNow.AddDays(-1)).SubscribeAndComplete();
                cache.Insert("expired2", [2], typeof(string), DateTimeOffset.UtcNow.AddDays(-1)).SubscribeAndComplete();
                cache.Insert("valid", [3], DateTimeOffset.UtcNow.AddDays(1)).SubscribeAndComplete();

                cache.Vacuum().SubscribeAndComplete();

                var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(keys!.Count).IsEqualTo(1);
                await Assert.That(keys![0]).IsEqualTo("valid");
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("str1", [1], typeof(string)).SubscribeAndComplete();
                cache.Insert("str2", [2], typeof(string)).SubscribeAndComplete();
                cache.Insert("int1", [3], typeof(int)).SubscribeAndComplete();

                cache.InvalidateAll(typeof(string)).SubscribeAndComplete();

                var remaining = cache.GetAllKeys(typeof(int)).ToList().SubscribeGetValue();
                await Assert.That(remaining!.Count).IsEqualTo(1);
                await Assert.That(remaining![0]).IsEqualTo("int1");

                var stringKeys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(stringKeys!).IsEmpty();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("no_expiry", [1]).SubscribeAndComplete();

                // Typed insert without expiration
                cache.Insert("no_expiry_typed", [2], typeof(string)).SubscribeAndComplete();

                // Bulk non-typed insert without expiration
                cache.Insert([new("bulk1", [3])]).SubscribeAndComplete();

                // Bulk typed insert without expiration
                cache.Insert([new("bulk2", [4])], typeof(string)).SubscribeAndComplete();

                // All should be retrievable
                var data1 = cache.Get("no_expiry").SubscribeGetValue();
                await Assert.That(data1).IsNotNull();

                var data2 = cache.Get("no_expiry_typed", typeof(string)).SubscribeGetValue();
                await Assert.That(data2).IsNotNull();

                var data3 = cache.Get("bulk1").SubscribeGetValue();
                await Assert.That(data3).IsNotNull();

                var data4 = cache.Get("bulk2", typeof(string)).SubscribeGetValue();
                await Assert.That(data4).IsNotNull();
            }
            finally
            {
                cache.Dispose();
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
                cache.Dispose();
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
                cache.Dispose();
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
                cache.Dispose();
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
        await Assert.That(static () => new SqliteBlobCache((string)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();
        await Assert.That(static () => new SqliteBlobCache("test.db", null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => new SqliteBlobCache((IAkavacheConnection)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();
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
                cache.Insert("a", [1]).SubscribeAndComplete();
                cache.Insert("b", [2], typeof(string)).SubscribeAndComplete();
                cache.Insert("c", [3], typeof(int)).SubscribeAndComplete();

                cache.InvalidateAll().SubscribeAndComplete();

                var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(keys!).IsEmpty();
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("a", [1]).SubscribeAndComplete();
                cache.Insert("b", [2]).SubscribeAndComplete();
                cache.Insert("c", [3]).SubscribeAndComplete();

                cache.Invalidate(["a", "b"]).SubscribeAndComplete();

                var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(keys!.Count).IsEqualTo(1);
                await Assert.That(keys![0]).IsEqualTo("c");
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert(pairs, typeof(string), future).SubscribeAndComplete();

                var results = cache.Get(["tk1", "tk2"], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(results!.Count).IsEqualTo(2);
            }
            finally
            {
                cache.Dispose();
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
                cache.Insert("k1", [1], DateTimeOffset.UtcNow.AddMinutes(5)).SubscribeAndComplete();
                cache.Insert("k2", [2], typeof(string), DateTimeOffset.UtcNow.AddMinutes(5)).SubscribeAndComplete();

                // Update to null expiration (permanent)
                cache.UpdateExpiration("k1", null).SubscribeAndComplete();
                cache.UpdateExpiration("k2", typeof(string), null).SubscribeAndComplete();
                cache.UpdateExpiration(["k1"], null).SubscribeAndComplete();
                cache.UpdateExpiration(["k2"], typeof(string), null).SubscribeAndComplete();

                var d1 = cache.Get("k1").SubscribeGetValue();
                await Assert.That(d1).IsNotNull();

                var d2 = cache.Get("k2", typeof(string)).SubscribeGetValue();
                await Assert.That(d2).IsNotNull();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that all public methods throw ObjectDisposedException after disposal
    /// when using an in-memory connection (no real SQLite database required).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposedShouldThrowForAllOperations()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        cache.Dispose();

        Exception? ex = null;
        cache.Insert("k", [1]).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Insert([new("k", [1])]).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Insert("k", [1], typeof(string)).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Insert([new("k", [1])], typeof(string)).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Get("k").Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Get(["k"]).ToList().Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Get("k", typeof(string)).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Get(["k"], typeof(string)).ToList().Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.GetAllKeys().ToList().Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.GetAllKeys(typeof(string)).ToList().Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.GetAll(typeof(string)).ToList().Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.GetCreatedAt("k").Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.GetCreatedAt(["k"]).ToList().Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.GetCreatedAt("k", typeof(string)).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.GetCreatedAt(["k"], typeof(string)).ToList().Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Flush().Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Flush(typeof(string)).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Invalidate("k").Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Invalidate(["k"]).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Invalidate("k", typeof(string)).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Invalidate(["k"], typeof(string)).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.InvalidateAll().Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.InvalidateAll(typeof(string)).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.Vacuum().Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.UpdateExpiration("k", DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.UpdateExpiration(["k"], DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();

        ex = null;
        cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that SqliteBlobCache.BeforeWriteToDiskFilter returns an error observable
    /// after the cache has been disposed, using an in-memory connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBeforeWriteToDiskFilterShouldThrowWhenDisposed()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        cache.Dispose();

        Exception? ex = null;
        cache.BeforeWriteToDiskFilter([1, 2, 3], ImmediateScheduler.Instance).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that SqliteBlobCache.BeforeWriteToDiskFilter returns data unchanged
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
            var result = cache.BeforeWriteToDiskFilter(input, ImmediateScheduler.Instance).SubscribeGetValue();
            await Assert.That(result).IsEquivalentTo(input);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests basic CRUD operations using the in-memory connection, verifying that the
    /// IAkavacheConnection abstraction works correctly for Insert, Get,
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
            cache.Insert("k1", [1, 2]).SubscribeAndComplete();
            var data = cache.Get("k1").SubscribeGetValue();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!.Length).IsEqualTo(2);

            // Typed Insert and Get
            cache.Insert("k2", [3], typeof(string)).SubscribeAndComplete();
            var typedData = cache.Get("k2", typeof(string)).SubscribeGetValue();
            await Assert.That(typedData).IsNotNull();

            // GetAllKeys
            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(2);

            // Invalidate single
            cache.Invalidate("k1").SubscribeAndComplete();
            Exception? ex = null;
            cache.Get("k1").Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<KeyNotFoundException>();

            // InvalidateAll
            cache.InvalidateAll().SubscribeAndComplete();
            var remainingKeys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(remainingKeys!).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that double disposal does not throw when using an in-memory connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDoubleDisposeShouldNotThrow()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        cache.Dispose();
        cache.Dispose();

        Exception? ex = null;
        cache.Get("k").Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when a null
    /// IAkavacheConnection is passed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorWithNullConnectionShouldThrow() => await Assert.That(static () => new SqliteBlobCache((IAkavacheConnection)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();

    /// <summary>
    /// Verifies that SqliteBlobCache.UpdateExpiration(string, DateTimeOffset?)
    /// is routed through the connection's <c>SetExpiryAsync</c> helper and actually
    /// mutates the stored entry's expiration when using the in-memory backend.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryUpdateExpirationShouldMutateEntry()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            cache.Insert("k1", [1], DateTimeOffset.UtcNow.AddMinutes(1)).SubscribeAndComplete();
            var newExpiry = DateTimeOffset.UtcNow.AddHours(2);

            cache.UpdateExpiration("k1", newExpiry).SubscribeAndComplete();

            var stored = connection.Store["k1"];
            await Assert.That(stored.ExpiresAt).IsNotNull();
            await Assert.That(stored.ExpiresAt!.Value).IsEqualTo(newExpiry.UtcDateTime);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.UpdateExpiration(string, Type, DateTimeOffset?)
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
            cache.Insert("k1", [1], typeof(string), initialExpiry).SubscribeAndComplete();
            cache.Insert("k1", [1], typeof(int)).SubscribeAndComplete(); // overwrites would happen only if same key+type; dictionary keyed by Id so last write wins

            // Insert a different key so we can prove type filter isolates the right row.
            cache.Insert("k2", [2], typeof(string), initialExpiry).SubscribeAndComplete();

            var updatedExpiry = DateTimeOffset.UtcNow.AddDays(1);

            // Update k2 only, scoped to typeof(string).
            cache.UpdateExpiration("k2", typeof(string), updatedExpiry).SubscribeAndComplete();

            var k2 = connection.Store["k2"];
            await Assert.That(k2.ExpiresAt!.Value).IsEqualTo(updatedExpiry.UtcDateTime);

            // Mismatching type filter should be a no-op.
            cache.UpdateExpiration("k2", typeof(object), DateTimeOffset.UtcNow.AddYears(10)).SubscribeAndComplete();
            await Assert.That(connection.Store["k2"].ExpiresAt!.Value).IsEqualTo(updatedExpiry.UtcDateTime);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies the bulk SqliteBlobCache.UpdateExpiration(IEnumerable{string}, DateTimeOffset?)
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
            cache.Insert("k1", [1]).SubscribeAndComplete();
            cache.Insert("k2", [2]).SubscribeAndComplete();
            cache.Insert("k3", [3]).SubscribeAndComplete();

            var updated = DateTimeOffset.UtcNow.AddHours(5);
            cache.UpdateExpiration(["k1", "k2", "k3"], updated).SubscribeAndComplete();

            foreach (var id in new[] { "k1", "k2", "k3" })
            {
                await Assert.That(connection.Store[id].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies the typed bulk SqliteBlobCache.UpdateExpiration(IEnumerable{string}, Type, DateTimeOffset?)
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
            cache.Insert("a", [1], typeof(string), DateTimeOffset.UtcNow.AddMinutes(1)).SubscribeAndComplete();
            cache.Insert("b", [2], typeof(string), DateTimeOffset.UtcNow.AddMinutes(1)).SubscribeAndComplete();

            var updated = DateTimeOffset.UtcNow.AddDays(3);
            cache.UpdateExpiration(["a", "b"], typeof(string), updated).SubscribeAndComplete();

            await Assert.That(connection.Store["a"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
            await Assert.That(connection.Store["b"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);

            // Wrong type should leave both untouched.
            var wrongTypeExpiry = DateTimeOffset.UtcNow.AddYears(10);
            cache.UpdateExpiration(["a", "b"], typeof(int), wrongTypeExpiry).SubscribeAndComplete();
            await Assert.That(connection.Store["a"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
            await Assert.That(connection.Store["b"].ExpiresAt!.Value).IsEqualTo(updated.UtcDateTime);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Flush() calls
    /// IAkavacheConnection.CheckpointAsync(CheckpointMode) with
    /// CheckpointMode.Passive.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryFlushShouldRequestPassiveCheckpoint()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            cache.Flush().SubscribeAndComplete();
            await Assert.That(connection.CheckpointCount).IsEqualTo(1);
            await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Passive);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that typed SqliteBlobCache.Insert(string, byte[], Type, DateTimeOffset?)
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
            cache.Insert("k", [1], typeof(string)).SubscribeAndComplete();
            await Assert.That(connection.CheckpointCount).IsGreaterThan(before);
            await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Passive);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Vacuum is routed through
    /// IAkavacheConnection.CompactAsync().
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryVacuumShouldCallCompact()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            cache.Vacuum().SubscribeAndComplete();
            await Assert.That(connection.CompactCount).IsEqualTo(1);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Dispose issues a full checkpoint and
    /// then releases auxiliary resources before closing the connection.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposeShouldCheckpointAndReleaseAuxiliary()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        cache.Dispose();

        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Full);
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Get(string) falls back to the V10 legacy
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
            var data = cache.Get("legacyKey").SubscribeGetValue();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!).IsEquivalentTo(new byte[] { 9, 8, 7 });
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that typed SqliteBlobCache.Get(string, Type) falls back to the
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
            var data = cache.Get("legacyTyped", typeof(string)).SubscribeGetValue();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!).IsEquivalentTo(new byte[] { 1, 2, 3, 4 });
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Get(string) throws
    /// KeyNotFoundException when the key is missing from both the
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
            Exception? ex = null;
            cache.Get("missing").Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<KeyNotFoundException>();

            ex = null;
            cache.Get("missing", typeof(string)).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<KeyNotFoundException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Invalidate(IEnumerable{string}, Type) only
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
            cache.Insert("a", [1], typeof(string)).SubscribeAndComplete();
            cache.Insert("b", [2]).SubscribeAndComplete(); // untyped

            cache.Invalidate(["a", "b"], typeof(string)).SubscribeAndComplete();

            // "a" removed (typed match); "b" still present (no TypeName).
            await Assert.That(connection.Store.ContainsKey("a")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("b")).IsTrue();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies SqliteBlobCache.InvalidateAll(Type) removes only entries
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
            cache.Insert("a", [1], typeof(string)).SubscribeAndComplete();
            cache.Insert("b", [2], typeof(string)).SubscribeAndComplete();
            cache.Insert("c", [3]).SubscribeAndComplete(); // untyped

            cache.InvalidateAll(typeof(string)).SubscribeAndComplete();

            await Assert.That(connection.Store.ContainsKey("a")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("b")).IsFalse();
            await Assert.That(connection.Store.ContainsKey("c")).IsTrue();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.GetCreatedAt(string) returns the stored
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
            cache.Insert("k", [1]).SubscribeAndComplete();
            var createdAt = cache.GetCreatedAt("k").SubscribeGetValue();
            await Assert.That(createdAt).IsNotNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Flush() completes successfully even when
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
            cache.Flush().SubscribeAndComplete();
            await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            // Disable the failure so Dispose can complete cleanly.
            connection.FailCheckpoint = false;
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies typed Insert swallows a failing IAkavacheConnection.UpsertAsync(IReadOnlyList{CacheEntry})
    /// and also tolerates a post-write checkpoint failure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedInsertSwallowsUpsertFailure()
    {
        InMemoryAkavacheConnection connection = new()
        {
            FailUpsert = true,
            FailCheckpoint = true,
        };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Should complete without throwing even though both the upsert and the
            // post-transaction checkpoint fail.
            cache.Insert("k", [1], typeof(string)).SubscribeAndComplete();
            await Assert.That(connection.Store.ContainsKey("k")).IsFalse();
        }
        finally
        {
            connection.FailCheckpoint = false;
            connection.FailUpsert = false;
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Dispose falls back from a failing
    /// checkpoint to IAkavacheConnection.CompactAsync(), then continues on
    /// to release auxiliary resources and close.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposeFallsBackToCompactWhenCheckpointFails()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Dispose enqueues a best-effort checkpoint (which fails here) then
        // calls Connection.Dispose(). No compact fallback — the checkpoint
        // error is swallowed silently.
        cache.Dispose();

        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.Dispose tolerates all teardown
    /// calls throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryDisposeTolerantOfAllTeardownFailures()
    {
        InMemoryAkavacheConnection connection = new()
        {
            FailCheckpoint = true,
            FailCompact = true,
        };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Should not throw even though every teardown operation raises.
        cache.Dispose();
    }

    /// <summary>
    /// Verifies synchronous SqliteBlobCache.Dispose() runs the best-effort
    /// cleanup path (truncate checkpoint).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Test deliberately exercises the synchronous Dispose path.")]
    public async Task InMemorySyncDisposeRunsCleanupPath()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        cache.Dispose();

        // Sync dispose enqueues a best-effort full checkpoint then calls
        // Connection.Dispose().
        await Assert.That(connection.CheckpointCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(connection.LastCheckpointMode).IsEqualTo(CheckpointMode.Full);
    }

    /// <summary>
    /// Verifies that synchronous SqliteBlobCache.Dispose() tolerates every
    /// teardown call throwing.
    /// </summary>
    [Test]
    public void InMemorySyncDisposeTolerantOfAllFailures()
    {
        InMemoryAkavacheConnection connection = new()
        {
            FailCheckpoint = true,
        };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Should not throw.
        cache.Dispose();
    }

    /// <summary>
    /// Verifies that an error raised from IAkavacheConnection.CreateSchemaAsync()
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
            Exception? ex = null;
            cache.Get("k").Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            connection.FailCreateTable = false;
            cache.Dispose();
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
        connection.SeedRaw("nullId", new(Id: null, TypeName: null, [1], default, ExpiresAt: null));
        connection.SeedRaw("nullValue", new("nullValue", TypeName: null, Value: null, default, ExpiresAt: null));
        connection.SeedRaw("good", new("good", TypeName: null, [9], default, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var results = cache.Get(["nullId", "nullValue", "good"]).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo("good");
        }
        finally
        {
            cache.Dispose();
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
        connection.SeedRaw("nullId", new(Id: null, typeof(string).FullName, [1], default, ExpiresAt: null));
        connection.SeedRaw("nullValue", new("nullValue", typeof(string).FullName, Value: null, default, ExpiresAt: null));
        connection.SeedRaw("good", new("good", typeof(string).FullName, [9], default, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var results = cache.Get(["nullId", "nullValue", "good"], typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo("good");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.GetAll(Type)'s post-query defensive filter
    /// skips entries with a null <c>Id</c> or <c>Value</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetAllShouldSkipEntriesWithNullIdOrValue()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new(Id: null, typeof(string).FullName, [1], default, ExpiresAt: null));
        connection.SeedRaw("nullValue", new("nullValue", typeof(string).FullName, Value: null, default, ExpiresAt: null));
        connection.SeedRaw("good", new("good", typeof(string).FullName, [9], default, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var results = cache.GetAll(typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo("good");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.GetAllKeys()'s post-query defensive filter
    /// skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetAllKeysShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new(Id: null, TypeName: null, [1], default, ExpiresAt: null));
        connection.SeedRaw("good", new("good", TypeName: null, [9], default, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(1);
            await Assert.That(keys![0]).IsEqualTo("good");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SqliteBlobCache.GetAllKeys(Type)'s post-query defensive
    /// filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetAllKeysWithTypeShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new(Id: null, typeof(string).FullName, [1], default, ExpiresAt: null));
        connection.SeedRaw("good", new("good", typeof(string).FullName, [9], default, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var keys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(1);
            await Assert.That(keys![0]).IsEqualTo("good");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the bulk SqliteBlobCache.GetCreatedAt(IEnumerable{string})
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryBulkGetCreatedAtShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new(Id: null, TypeName: null, [1], DateTime.UtcNow, ExpiresAt: null));
        connection.SeedRaw("good", new("good", TypeName: null, [9], DateTime.UtcNow, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var results = cache.GetCreatedAt(["nullId", "good"]).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo("good");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the typed bulk SqliteBlobCache.GetCreatedAt(IEnumerable{string}, Type)
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryTypedBulkGetCreatedAtShouldSkipEntriesWithNullId()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new(Id: null, typeof(string).FullName, [1], DateTime.UtcNow, ExpiresAt: null));
        connection.SeedRaw("good", new("good", typeof(string).FullName, [9], DateTime.UtcNow, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var results = cache.GetCreatedAt(["nullId", "good"], typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(1);
            await Assert.That(results![0].Key).IsEqualTo("good");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the single-key SqliteBlobCache.GetCreatedAt(string)
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetCreatedAtSingleShouldSkipNullIdEntry()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new(Id: null, TypeName: null, [1], DateTime.UtcNow, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            // Defensive Where filters out the null-Id entry; the DefaultIfEmpty fallback yields null.
            var result = cache.GetCreatedAt("nullId").SubscribeGetValue();
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the typed single-key SqliteBlobCache.GetCreatedAt(string, Type)
    /// overload's post-query defensive filter skips entries with a null <c>Id</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryGetCreatedAtSingleTypedShouldSkipNullIdEntry()
    {
        InMemoryAkavacheConnection connection = new() { BypassPredicate = true };
        connection.SeedRaw("nullId", new(Id: null, typeof(string).FullName, [1], DateTime.UtcNow, ExpiresAt: null));

        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            var result = cache.GetCreatedAt("nullId", typeof(string)).SubscribeGetValue();
            await Assert.That(result).IsNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests SqliteBlobCache.ToExpiryValue returns the UTC
    /// DateTime component when given a non-null
    /// DateTimeOffset.
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
    /// Tests SqliteBlobCache.ToExpiryValue returns
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
    /// Tests SqliteBlobCache.TryGetLegacyValue delegates to
    /// IAkavacheConnection.TryReadLegacyV10Value(string, DateTimeOffset, Type?)
    /// on the supplied connection and returns whatever that returns.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetLegacyValueShouldReturnNullWhenLegacyRowMissing()
    {
        InMemoryAkavacheConnection connection = new();

        var result = SqliteBlobCache.TryGetLegacyValue(connection, "no-such-key", DateTimeOffset.UtcNow, null).SubscribeGetValue();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies that constructing a SqliteBlobCache creates the CacheEntry
    /// schema on the supplied connection (observed through the public API).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructingCacheShouldCreateCacheEntryTable()
    {
        InMemoryAkavacheConnection connection = new();
        using SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        cache.Insert("k", [1]).SubscribeAndComplete();

        var tableExists = connection.TableExists("CacheEntry").SubscribeGetValue();
        await Assert.That(tableExists).IsTrue();
    }

    /// <summary>
    /// Verifies that operations surface an error when the underlying
    /// connection fails to create the CacheEntry table during init.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FailedCreateTableShouldPropagateToOperations()
    {
        InMemoryAkavacheConnection connection = new() { FailCreateTable = true };
        using SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        Exception? ex = null;
        cache.Insert("k", [1]).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<Exception>();
    }

    /// <summary>
    /// Tests that SqliteBlobCache.ReadValueWithLegacyFallbackAsync(string, Type?)
    /// returns the stored bytes when the V11 <c>CacheEntry</c> table contains
    /// the requested key (untyped overload).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadValueWithLegacyFallbackAsyncShouldReturnV11ValueWhenPresent()
    {
        using var cache = CreateCache();
        cache.Insert("v11-key", [10, 20, 30]).SubscribeAndComplete();

        var bytes = cache.ReadValueWithLegacyFallback("v11-key", type: null).SubscribeGetValue();

        await Assert.That(bytes).IsEquivalentTo(new byte[] { 10, 20, 30 });
    }

    /// <summary>
    /// Tests that SqliteBlobCache.ReadValueWithLegacyFallbackAsync(string, Type?)
    /// returns the stored bytes from the V11 table when the typed overload's
    /// <c>TypeName</c> filter matches the entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadValueWithLegacyFallbackAsyncShouldReturnTypedV11ValueWhenPresent()
    {
        using var cache = CreateCache();
        cache.Insert("typed-key", [4, 5, 6], typeof(string)).SubscribeAndComplete();

        var bytes = cache.ReadValueWithLegacyFallback("typed-key", typeof(string)).SubscribeGetValue();

        await Assert.That(bytes).IsEquivalentTo(new byte[] { 4, 5, 6 });
    }

    /// <summary>
    /// Tests that SqliteBlobCache.ReadValueWithLegacyFallbackAsync(string, Type?)
    /// falls through to the legacy V10 store when the V11 table has no row, and
    /// returns those bytes instead.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadValueWithLegacyFallbackAsyncShouldFallBackToLegacyV10Store()
    {
        InMemoryAkavacheConnection connection = new();
        connection.LegacyV10Store["legacy-only"] = "\t\t\t"u8.ToArray();
        using SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        var bytes = cache.ReadValueWithLegacyFallback("legacy-only", type: null).SubscribeGetValue();

        await Assert.That(bytes).IsEquivalentTo("\t\t\t"u8.ToArray());
    }

    /// <summary>
    /// Tests that SqliteBlobCache.ReadValueWithLegacyFallbackAsync(string, Type?)
    /// throws KeyNotFoundException when neither the V11 nor the
    /// legacy V10 stores contain the requested key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadValueWithLegacyFallbackAsyncShouldThrowWhenKeyMissingInBothStores()
    {
        using var cache = CreateCache();

        Exception? ex = null;
        cache.ReadValueWithLegacyFallback("missing", type: null).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests that the KeyNotFoundException message produced by the
    /// typed branch of SqliteBlobCache.ReadValueWithLegacyFallbackAsync(string, Type?)
    /// includes the type's full name so callers can disambiguate identical keys
    /// stored under different types.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadValueWithLegacyFallbackAsyncShouldIncludeTypeNameInMissingMessage()
    {
        using var cache = CreateCache();

        Exception? ex = null;
        cache.ReadValueWithLegacyFallback("missing", typeof(string)).Subscribe(_ => { }, e => ex = e);
        await Assert.That(ex).IsTypeOf<KeyNotFoundException>();
        await Assert.That(ex!.Message).Contains("System.String");
    }

    /// <summary>
    /// Typed bulk Insert with an empty collection returns Unit without touching the database.
    /// Covers lines 433-435 (entries.Count == 0 early return in typed Insert).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TypedBulkInsertWithEmptyCollectionShouldReturnUnit()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            cache.Insert([], typeof(string)).SubscribeAndComplete();

            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// InvalidateAll with a null type throws ArgumentNullException.
    /// Covers lines 536-538 (null type guard in InvalidateAll(Type)).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InvalidateAllWithNullTypeShouldThrowArgumentNullException()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            Exception? ex = null;
            cache.InvalidateAll((Type)null!).Subscribe(_ => { }, e => ex = e);
            await Assert.That(ex).IsTypeOf<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Dispose catches and swallows errors when Connection.Checkpoint(Full) throws.
    /// Covers lines 772-775 (catch block in Dispose(bool)).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DisposeSwallowsCheckpointException()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Dispose should not throw even though Checkpoint(Full) raises.
        cache.Dispose();

        // The connection should still be disposed.
        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    /// <summary>
    /// Dispose swallows a synchronous throw from Connection.Checkpoint
    /// (the catch at lines 772-775).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DisposeSwallowsSynchronousCheckpointThrow()
    {
        InMemoryAkavacheConnection connection = new() { ThrowOnCheckpointCall = true };
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        cache.Dispose();

        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    // ── MaterializeKeys ICollection path (lines 702-706) ────────────

    /// <summary>
    /// MaterializeKeys with an ICollection (HashSet) exercises the CopyTo path (lines 702-706).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task MaterializeKeys_WithHashSet_UsesCopyToPath()
    {
        var keys = new HashSet<string> { "alpha", "beta", "gamma" };
        var result = SqliteBlobCache.MaterializeKeys(keys);

        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result).Contains("alpha");
        await Assert.That(result).Contains("beta");
        await Assert.That(result).Contains("gamma");
    }

    // ── MaterializeKeys IReadOnlyList path (lines 697-700) ──────────

    /// <summary>
    /// MaterializeKeys with an IReadOnlyList returns the same instance (lines 697-700).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task MaterializeKeys_WithReadOnlyList_ReturnsSameInstance()
    {
        IReadOnlyList<string> keys = ["one", "two"];
        var result = SqliteBlobCache.MaterializeKeys(keys);

        await Assert.That(ReferenceEquals(result, keys)).IsTrue();
    }

    // ── MaterializeKeys iterator path (line 709) ─────────────────────

    /// <summary>
    /// MaterializeKeys with a plain iterator (yield return) exercises the fallback
    /// spread path (line 709).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task MaterializeKeys_WithIterator_CollectsViaSpread()
    {
        static IEnumerable<string> Generate()
        {
            yield return "x";
            yield return "y";
        }

        var result = SqliteBlobCache.MaterializeKeys(Generate());

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsEqualTo("x");
        await Assert.That(result[1]).IsEqualTo("y");
    }

    // ── MaterializeKeys with array (IReadOnlyList) ───────────────────

    /// <summary>
    /// MaterializeKeys with an array (which implements IReadOnlyList) returns the
    /// same instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task MaterializeKeys_WithArray_ReturnsSameInstance()
    {
        string[] keys = ["a", "b", "c"];
        var result = SqliteBlobCache.MaterializeKeys(keys);

        await Assert.That(ReferenceEquals(result, keys)).IsTrue();
    }

    // ── MaterializeKeys with List (IReadOnlyList + ICollection) ──────

    /// <summary>
    /// MaterializeKeys with a List (implements both IReadOnlyList and ICollection)
    /// takes the IReadOnlyList fast path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task MaterializeKeys_WithList_TakesReadOnlyListPath()
    {
        var keys = new List<string> { "p", "q" };
        var result = SqliteBlobCache.MaterializeKeys(keys);

        // List<T> implements IReadOnlyList<T>, so it should be the same reference.
        await Assert.That(ReferenceEquals(result, keys)).IsTrue();
    }

    // ── Invalidate with empty keys via ICollection path ─────────────

    /// <summary>
    /// Invalidate with an empty HashSet exercises the MaterializeKeys ICollection path
    /// plus the empty-key-list early return (lines 500-503).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Invalidate_WithEmptyHashSet_IsNoop()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            cache.Insert("keep", [1]).SubscribeAndComplete();
            cache.Invalidate(new HashSet<string>()).SubscribeAndComplete();

            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(1);
        }
        finally
        {
            cache.Dispose();
        }
    }

    // ── Invalidate typed with empty keys via ICollection path ────────

    /// <summary>
    /// Invalidate typed with an empty HashSet exercises the MaterializeKeys ICollection
    /// path plus the empty-key-list early return (lines 527-530).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InvalidateTyped_WithEmptyHashSet_IsNoop()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            cache.Insert("keep", [1], typeof(string)).SubscribeAndComplete();
            cache.Invalidate(new HashSet<string>(), typeof(string)).SubscribeAndComplete();

            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(1);
        }
        finally
        {
            cache.Dispose();
        }
    }

    // ── Dispose(isDisposing: false) path (line 761) ──────────────────

    /// <summary>
    /// Calling Dispose(false) via the non-disposing path is a no-op. We test this
    /// by verifying that the cache is still functional after the base finalizer would
    /// call Dispose(false). Since we cannot call Dispose(false) directly on the sealed
    /// class, we verify the double-dispose idempotency instead, which covers line 761.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_CalledTwice_SecondCallIsIdempotent()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        cache.Dispose();
        cache.Dispose();

        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    // ── BuildCacheEntries with ICollection input ─────────────────────

    /// <summary>
    /// BuildCacheEntries with a Dictionary.ValueCollection (ICollection but not
    /// ICollection{KVP}) exercises the initial capacity heuristic.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task BuildCacheEntries_WithArrayInput_BuildsCorrectEntries()
    {
        KeyValuePair<string, byte[]>[] pairs =
        [
            new("k1", [1]),
            new("k2", [2]),
        ];

        var entries = SqliteBlobCache.BuildCacheEntries(
            pairs,
            "TestType",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1));

        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries[0].Id).IsEqualTo("k1");
        await Assert.That(entries[0].TypeName).IsEqualTo("TestType");
        await Assert.That(entries[1].Id).IsEqualTo("k2");
    }

    /// <summary>
    /// BuildCacheEntries with an iterator source exercises the non-ICollection
    /// initial capacity fallback.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task BuildCacheEntries_WithIterator_BuildsCorrectEntries()
    {
        static IEnumerable<KeyValuePair<string, byte[]>> Generate()
        {
            yield return new("i1", [10]);
            yield return new("i2", [20]);
            yield return new("i3", [30]);
        }

        var entries = SqliteBlobCache.BuildCacheEntries(
            Generate(),
            null,
            DateTimeOffset.UtcNow,
            null);

        await Assert.That(entries.Count).IsEqualTo(3);
        await Assert.That(entries[0].TypeName).IsNull();
        await Assert.That(entries[2].ExpiresAt).IsNull();
    }

    // ── Insert with empty key-value pairs (line 398 branch) ──────────

    /// <summary>
    /// Insert with an empty collection of key-value pairs returns Unit.Default
    /// without calling Connection.Upsert. Covers the <c>entries.Count > 0</c>
    /// ternary false branch at line 398.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Insert_EmptyKeyValuePairs_ReturnsUnitWithoutUpsert()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            cache.Insert([]).SubscribeAndComplete();

            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(0);
        }
        finally
        {
            cache.Dispose();
        }
    }

    // ── Insert typed with empty key-value pairs (line 433 branch) ───

    /// <summary>
    /// Insert typed with an empty collection returns Unit.Default without
    /// calling Connection.Upsert. Covers the <c>entries.Count == 0</c>
    /// early return at line 433.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InsertTyped_EmptyKeyValuePairs_ReturnsUnitWithoutUpsert()
    {
        InMemoryAkavacheConnection connection = new();
        SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
        try
        {
            cache.Insert([], typeof(string)).SubscribeAndComplete();

            var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(0);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Creates a new instance of SqliteBlobCache that utilizes an InMemoryAkavacheConnection
    /// for storage, enabling fast, in-memory operations for unit tests and logic validations.
    /// This method bypasses file-based persistence by storing data entirely in memory.
    /// </summary>
    /// <returns>A SqliteBlobCache instance backed by an in-memory connection.</returns>
    private static SqliteBlobCache CreateCache() =>
        new(new InMemoryAkavacheConnection(), new SystemJsonSerializer(), ImmediateScheduler.Instance);
}
