// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>Tests for SqliteBlobCache covering disposed-state error paths, null arg validation, and type-aware overloads.</summary>
[Category("Akavache")]
public class SqliteBlobCacheDirectTests
{
    /// <summary>Payload of the first entry a test stores; distinct so a swapped row shows in the assertion.</summary>
    private static readonly byte[] FirstEntryPayload = [1];

    /// <summary>Payload of the second entry a test stores.</summary>
    private static readonly byte[] SecondEntryPayload = [2];

    /// <summary>Payload of the third entry a test stores.</summary>
    private static readonly byte[] ThirdEntryPayload = [3];

    /// <summary>Payload of the fourth entry a test stores.</summary>
    private static readonly byte[] FourthEntryPayload = [4];

    /// <summary>A two-byte payload, for tests that assert the whole blob survives the round trip.</summary>
    private static readonly byte[] TwoBytePayload = [1, 2];

    /// <summary>A three-byte payload, for tests that assert the blob length survives the round trip.</summary>
    private static readonly byte[] ThreeBytePayload = [1, 2, 3];

    /// <summary>Payload handed to the write filter, chosen so an accidental rewrite is obvious.</summary>
    private static readonly byte[] WriteFilterProbePayload = [10, 20, 30];

    /// <summary>Payload of the first entry in a typed bulk insert.</summary>
    private static readonly byte[] FirstTypedBulkPayload = [10];

    /// <summary>Payload of the second entry in a typed bulk insert.</summary>
    private static readonly byte[] SecondTypedBulkPayload = [20];

    /// <summary>An expiry already in the past when it is stamped on an entry.</summary>
    private static readonly TimeSpan AlreadyElapsedLifetime = TimeSpan.FromSeconds(-10);

    /// <summary>A lifetime short enough to prove the entry outlives it only because the test moves the expiry.</summary>
    private static readonly TimeSpan ShortLifetime = TimeSpan.FromMinutes(5);

    /// <summary>Tests disposed-state error paths for all operations.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposedShouldThrowForAllOperations()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            cache.Dispose();

            var now = TimeProvider.System.GetLocalNow();

            await AssertDisposed(cache.Insert("k", FirstEntryPayload));
            await AssertDisposed(cache.Insert([new("k", FirstEntryPayload)]));
            await AssertDisposed(cache.Insert("k", FirstEntryPayload, typeof(string)));
            await AssertDisposed(cache.Insert([new("k", FirstEntryPayload)], typeof(string)));
            await AssertDisposed(cache.Get("k"));
            await AssertDisposed(cache.Get(["k"]).ToList());
            await AssertDisposed(cache.Get("k", typeof(string)));
            await AssertDisposed(cache.Get(["k"], typeof(string)).ToList());
            await AssertDisposed(cache.GetAllKeys().ToList());
            await AssertDisposed(cache.GetAllKeys(typeof(string)).ToList());
            await AssertDisposed(cache.GetAll(typeof(string)).ToList());
            await AssertDisposed(cache.GetCreatedAt("k"));
            await AssertDisposed(cache.GetCreatedAt(["k"]).ToList());
            await AssertDisposed(cache.GetCreatedAt("k", typeof(string)));
            await AssertDisposed(cache.GetCreatedAt(["k"], typeof(string)).ToList());
            await AssertDisposed(cache.Flush());
            await AssertDisposed(cache.Flush(typeof(string)));
            await AssertDisposed(cache.Invalidate("k"));
            await AssertDisposed(cache.Invalidate(["k"]));
            await AssertDisposed(cache.Invalidate("k", typeof(string)));
            await AssertDisposed(cache.Invalidate(["k"], typeof(string)));
            await AssertDisposed(cache.InvalidateAll());
            await AssertDisposed(cache.InvalidateAll(typeof(string)));
            await AssertDisposed(cache.Vacuum());
            await AssertDisposed(cache.UpdateExpiration("k", now));
            await AssertDisposed(cache.UpdateExpiration(["k"], now));
            await AssertDisposed(cache.UpdateExpiration("k", typeof(string), now));
            await AssertDisposed(cache.UpdateExpiration(["k"], typeof(string), now));
        }
    }

    /// <summary>Tests null argument validation.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NullArgsShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await AssertArgumentNull(cache.Get((IEnumerable<string>)null!).ToList());
                await AssertArgumentNull(cache.Get((string)null!, typeof(string)));
                await AssertArgumentNull(cache.Get("k", null!));
                await AssertArgumentNull(cache.Get((IEnumerable<string>)null!, typeof(string)).ToList());
                await AssertArgumentNull(cache.Get(["k"], null!).ToList());
                await AssertArgumentNull(cache.GetAll(null!).ToList());
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests type-aware Insert and Get round-trip.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareInsertAndGetShouldRoundTrip()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("k1", ThreeBytePayload, typeof(string)).SubscribeAndComplete();
                var data = cache.Get("k1", typeof(string)).SubscribeGetValue();

                await Assert.That(data).IsNotNull();
                await Assert.That(data!.Length).IsEqualTo(ThreeBytePayload.Length);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests type-aware bulk Insert and Get.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareBulkInsertAndGetShouldRoundTrip()
    {
        const int InsertedPairCount = 2;

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                KeyValuePair<string, byte[]>[] pairs =
                [
                    new("k1", FirstEntryPayload),
                    new("k2", SecondEntryPayload)
                ];
                cache.Insert(pairs, typeof(string)).SubscribeAndComplete();

                var results = cache.Get(["k1", "k2"], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(results!.Count).IsEqualTo(InsertedPairCount);

                var typedKeys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(typedKeys!.Count).IsEqualTo(InsertedPairCount);

                var allOfType = cache.GetAll(typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(allOfType!.Count).IsEqualTo(InsertedPairCount);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests type-aware Invalidate.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareInvalidateShouldRemoveEntries()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("k1", FirstEntryPayload, typeof(string)).SubscribeAndComplete();
                cache.Insert("k2", SecondEntryPayload, typeof(int)).SubscribeAndComplete();

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

    /// <summary>Tests type-aware Invalidate by keys.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareInvalidateByKeysShouldRemoveEntries()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("k1", FirstEntryPayload, typeof(string)).SubscribeAndComplete();
                cache.Insert("k2", SecondEntryPayload, typeof(string)).SubscribeAndComplete();

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

    /// <summary>Tests type-aware GetCreatedAt.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareGetCreatedAtShouldReturnTimestamps()
    {
        const int StoredKeyCount = 2;

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("k1", FirstEntryPayload, typeof(string)).SubscribeAndComplete();
                cache.Insert("k2", SecondEntryPayload, typeof(string)).SubscribeAndComplete();

                var single = cache.GetCreatedAt("k1", typeof(string)).SubscribeGetValue();
                await Assert.That(single).IsNotNull();

                var multi = cache.GetCreatedAt(["k1", "k2"], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(multi!.Count).IsEqualTo(StoredKeyCount);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests type-aware UpdateExpiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TypeAwareUpdateExpirationShouldUpdateEntries()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("k1", FirstEntryPayload, typeof(string)).SubscribeAndComplete();
                var newExpiration = TimeProvider.System.GetLocalNow().AddHours(1);

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

    /// <summary>Tests Get with non-existent key throws KeyNotFoundException.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetNonExistentKeyShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await Assert.That(CaptureError(cache.Get("non_existent_key"))).IsTypeOf<KeyNotFoundException>();
                await Assert.That(CaptureError(cache.Get("non_existent_key", typeof(string)))).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests Get with whitespace key throws ArgumentNullException.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetWithWhitespaceKeyShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                await AssertArgumentNull(cache.Get(string.Empty));
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests Vacuum operation.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldWork()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("k1", FirstEntryPayload, TimeProvider.System.GetLocalNow().Add(AlreadyElapsedLifetime)).SubscribeAndComplete();
                cache.Insert("k2", SecondEntryPayload, TimeProvider.System.GetLocalNow().AddHours(1)).SubscribeAndComplete();

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

    /// <summary>Tests non-typed Insert, Get, GetAllKeys, GetAll, Invalidate, InvalidateAll happy paths.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NonTypedHappyPathsShouldRoundTrip()
    {
        const int BulkKeyCount = 2;
        const int TotalKeyCount = 3;

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("k1", TwoBytePayload).SubscribeAndComplete();
                cache.Insert(
                    [
                        new("k2", ThirdEntryPayload),
                        new("k3", FourthEntryPayload)
                    ],
                    TimeProvider.System.GetLocalNow().AddHours(1)).SubscribeAndComplete();

                var single = cache.Get("k1").SubscribeGetValue();
                await Assert.That(single).IsNotNull();

                var multi = cache.Get(["k2", "k3"]).ToList().SubscribeGetValue();
                await Assert.That(multi!.Count).IsEqualTo(BulkKeyCount);

                var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(keys!.Count).IsEqualTo(TotalKeyCount);

                var created = cache.GetCreatedAt("k1").SubscribeGetValue();
                await Assert.That(created).IsNotNull();

                var createdMany = cache.GetCreatedAt(["k1", "k2"]).ToList().SubscribeGetValue();
                await Assert.That(createdMany!.Count).IsEqualTo(BulkKeyCount);

                cache.UpdateExpiration("k1", TimeProvider.System.GetLocalNow().AddDays(1)).SubscribeAndComplete();
                cache.UpdateExpiration(["k2", "k3"], TimeProvider.System.GetLocalNow().AddDays(1)).SubscribeAndComplete();

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

    /// <summary>Tests additional null and whitespace argument validation.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AdditionalNullAndWhitespaceArgsShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            var now = TimeProvider.System.GetLocalNow();
            try
            {
                // GetCreatedAt null arg variants
                await AssertArgumentNull(cache.GetCreatedAt((string)null!));
                await AssertArgumentNull(cache.GetCreatedAt((IEnumerable<string>)null!).ToList());
                await AssertArgumentNull(cache.GetCreatedAt("k", null!));
                await AssertArgumentNull(cache.GetCreatedAt((string)null!, typeof(string)));
                await AssertArgumentNull(cache.GetCreatedAt(["k"], null!).ToList());
                await AssertArgumentNull(cache.GetCreatedAt((IEnumerable<string>)null!, typeof(string)).ToList());

                // GetAllKeys null type
                await AssertArgumentNull(cache.GetAllKeys(null!).ToList());

                // Insert null args
                await AssertArgumentNull(cache.Insert(null!));
                await AssertArgumentNull(cache.Insert(null!, typeof(string)));
                await AssertArgumentNull(cache.Insert([new("k", FirstEntryPayload)], (Type)null!));

                // Insert(key, data, type) arg validation
                await AssertArgument(cache.Insert(string.Empty, FirstEntryPayload, typeof(string)));
                await AssertArgument(cache.Insert("  ", FirstEntryPayload, typeof(string)));
                await AssertArgumentNull(cache.Insert("k", null!, typeof(string)));
                await AssertArgumentNull(cache.Insert("k", FirstEntryPayload, (Type)null!));

                // Invalidate arg validation
                await AssertArgument(cache.Invalidate(string.Empty));
                await AssertArgument(cache.Invalidate("   "));
                await AssertArgument(cache.Invalidate(string.Empty, typeof(string)));
                await AssertArgumentNull(cache.Invalidate("k", null!));
                await AssertArgumentNull(cache.Invalidate((IEnumerable<string>)null!));
                await AssertArgumentNull(cache.Invalidate((IEnumerable<string>)null!, typeof(string)));
                await AssertArgumentNull(cache.Invalidate(["k"], null!));

                // UpdateExpiration arg validation
                await AssertArgument(cache.UpdateExpiration(string.Empty, now));
                await AssertArgument(cache.UpdateExpiration(string.Empty, typeof(string), now));
                await AssertArgumentNull(cache.UpdateExpiration("k", null!, now));
                await AssertArgumentNull(cache.UpdateExpiration((IEnumerable<string>)null!, now));
                await AssertArgumentNull(cache.UpdateExpiration((IEnumerable<string>)null!, typeof(string), now));
                await AssertArgumentNull(cache.UpdateExpiration(["k"], null!, now));
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests synchronous Dispose path covering Dispose(bool isDisposing=true) branches.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SynchronousDisposeShouldCompleteCleanup()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            cache.Insert("k1", FirstEntryPayload).SubscribeAndComplete();

            // Synchronous dispose exercises the Dispose(bool) wal_checkpoint/journal/close paths
            cache.Dispose();

            // Second dispose is a no-op (early return)
            cache.Dispose();

            await AssertDisposed(cache.Get("k1"));
        }
    }

    /// <summary>Tests GetCreatedAt for a key that does not exist returns null via DefaultIfEmpty.</summary>
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

    /// <summary>Tests Insert with expired entries and retrieval after expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertWithPastExpirationShouldNotBeRetrievable()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("expired", FirstEntryPayload, typeof(string), TimeProvider.System.GetUtcNow().AddDays(-1)).SubscribeAndComplete();

                await Assert.That(CaptureError(cache.Get("expired", typeof(string)))).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that BeforeWriteToDiskFilter throws ObjectDisposedException after disposal.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BeforeWriteToDiskFilterShouldThrowWhenDisposed()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            cache.Dispose();

            await AssertDisposed(cache.BeforeWriteToDiskFilter(ThreeBytePayload, ImmediateScheduler.Instance));
        }
    }

    /// <summary>Tests that BeforeWriteToDiskFilter returns data unchanged when cache is active.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BeforeWriteToDiskFilterShouldReturnDataWhenNotDisposed()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                var result = cache.BeforeWriteToDiskFilter(WriteFilterProbePayload, ImmediateScheduler.Instance).SubscribeGetValue();
                await Assert.That(result).IsEquivalentTo(WriteFilterProbePayload);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that calling Dispose twice does not throw — the second call is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DoubleDisposeShouldNotThrow()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            cache.Insert("k", FirstEntryPayload).SubscribeAndComplete();

            cache.Dispose();
            cache.Dispose();

            await AssertDisposed(cache.Get("k"));
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
                cache.Insert("existing", TwoBytePayload).SubscribeAndComplete();

                // Non-typed Get for missing key exercises full fallback path
                await Assert.That(CaptureError(cache.Get("nonexistent"))).IsTypeOf<KeyNotFoundException>();

                // Typed Get for missing key exercises typed fallback path
                await Assert.That(CaptureError(cache.Get("nonexistent", typeof(int)))).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that bulk Get returns only matching entries, exercising the Where filter for non-null values.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BulkGetShouldReturnOnlyMatchingKeys()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("a", FirstEntryPayload).SubscribeAndComplete();
                cache.Insert("b", SecondEntryPayload).SubscribeAndComplete();

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

    /// <summary>Tests that GetAll with a type that has no entries returns empty.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllWithUnusedTypeShouldReturnEmpty()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("k", FirstEntryPayload, typeof(string)).SubscribeAndComplete();

                var results = cache.GetAll(typeof(int)).ToList().SubscribeGetValue();
                await Assert.That(results!).IsEmpty();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that GetAllKeys with a type filter only returns keys of that type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeysWithTypeShouldFilterByType()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("str1", FirstEntryPayload, typeof(string)).SubscribeAndComplete();
                cache.Insert("int1", SecondEntryPayload, typeof(int)).SubscribeAndComplete();

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
        const string ExpiredUntypedKey = "expired_plain";
        const string ValidUntypedKey = "valid_plain";
        const string ExpiredTypedKey = "expired_typed";
        const string ValidTypedKey = "valid_typed";

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                var pastExpiration = TimeProvider.System.GetUtcNow().AddDays(-1);
                var futureExpiration = TimeProvider.System.GetUtcNow().AddDays(1);

                // Non-typed inserts with expiration
                cache.Insert(ExpiredUntypedKey, FirstEntryPayload, pastExpiration).SubscribeAndComplete();
                cache.Insert(ValidUntypedKey, SecondEntryPayload, futureExpiration).SubscribeAndComplete();

                // Typed inserts with expiration
                cache.Insert(ExpiredTypedKey, ThirdEntryPayload, typeof(string), pastExpiration).SubscribeAndComplete();
                cache.Insert(ValidTypedKey, FourthEntryPayload, typeof(string), futureExpiration).SubscribeAndComplete();

                // Non-typed Get should not return expired
                await Assert.That(CaptureError(cache.Get(ExpiredUntypedKey))).IsTypeOf<KeyNotFoundException>();

                var validData = cache.Get(ValidUntypedKey).SubscribeGetValue();
                await Assert.That(validData).IsNotNull();

                // Typed Get should not return expired
                await Assert.That(CaptureError(cache.Get(ExpiredTypedKey, typeof(string)))).IsTypeOf<KeyNotFoundException>();

                var validTyped = cache.Get(ValidTypedKey, typeof(string)).SubscribeGetValue();
                await Assert.That(validTyped).IsNotNull();

                // Bulk Get should only return non-expired
                var bulkResults = cache.Get([ExpiredUntypedKey, ValidUntypedKey]).ToList().SubscribeGetValue();
                await Assert.That(bulkResults!.Count).IsEqualTo(1);

                var bulkTypedResults = cache.Get([ExpiredTypedKey, ValidTypedKey], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(bulkTypedResults!.Count).IsEqualTo(1);

                // GetAllKeys should only return non-expired
                var allKeys = cache.GetAllKeys().ToList().SubscribeGetValue();
                await Assert.That(allKeys!).Contains(ValidUntypedKey);
                await Assert.That(allKeys!).Contains(ValidTypedKey);
                await Assert.That(allKeys!).DoesNotContain(ExpiredUntypedKey);
                await Assert.That(allKeys!).DoesNotContain(ExpiredTypedKey);

                // GetAllKeys(type) should only return non-expired
                var typedKeys = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(typedKeys!.Count).IsEqualTo(1);
                await Assert.That(typedKeys![0]).IsEqualTo(ValidTypedKey);

                // GetAll(type) should only return non-expired
                var allOfType = cache.GetAll(typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(allOfType!.Count).IsEqualTo(1);

                // GetCreatedAt should return null for expired
                var createdExpired = cache.GetCreatedAt(ExpiredUntypedKey).SubscribeGetValue();
                await Assert.That(createdExpired).IsNull();

                var createdExpiredTyped = cache.GetCreatedAt(ExpiredTypedKey, typeof(string)).SubscribeGetValue();
                await Assert.That(createdExpiredTyped).IsNull();

                // Bulk GetCreatedAt should only return non-expired
                var createdBulk = cache.GetCreatedAt([ExpiredUntypedKey, ValidUntypedKey]).ToList().SubscribeGetValue();
                await Assert.That(createdBulk!.Count).IsEqualTo(1);

                var createdBulkTyped = cache.GetCreatedAt([ExpiredTypedKey, ValidTypedKey], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(createdBulkTyped!.Count).IsEqualTo(1);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests UpdateExpiration with typed entries, verifying that updating expiration to the past makes entries unretrievable.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationToThePastShouldMakeEntryUnretrievable()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("k1", FirstEntryPayload).SubscribeAndComplete();
                cache.Insert("k2", SecondEntryPayload, typeof(string)).SubscribeAndComplete();
                cache.Insert("k3", ThirdEntryPayload).SubscribeAndComplete();
                cache.Insert("k4", FourthEntryPayload, typeof(string)).SubscribeAndComplete();

                var past = TimeProvider.System.GetUtcNow().AddDays(-1);

                // Update single non-typed to past
                cache.UpdateExpiration("k1", past).SubscribeAndComplete();
                await Assert.That(CaptureError(cache.Get("k1"))).IsTypeOf<KeyNotFoundException>();

                // Update single typed to past
                cache.UpdateExpiration("k2", typeof(string), past).SubscribeAndComplete();
                await Assert.That(CaptureError(cache.Get("k2", typeof(string)))).IsTypeOf<KeyNotFoundException>();

                // Update bulk non-typed to past
                cache.UpdateExpiration(["k3"], past).SubscribeAndComplete();
                await Assert.That(CaptureError(cache.Get("k3"))).IsTypeOf<KeyNotFoundException>();

                // Update bulk typed to past
                cache.UpdateExpiration(["k4"], typeof(string), past).SubscribeAndComplete();
                await Assert.That(CaptureError(cache.Get("k4", typeof(string)))).IsTypeOf<KeyNotFoundException>();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that Flush operations complete successfully without errors.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FlushShouldCompleteSuccessfully()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("k", FirstEntryPayload).SubscribeAndComplete();

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

    /// <summary>Tests Vacuum removes expired entries and compacts the database.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldRemoveExpiredAndCompact()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("expired1", FirstEntryPayload, TimeProvider.System.GetUtcNow().AddDays(-1)).SubscribeAndComplete();
                cache.Insert("expired2", SecondEntryPayload, typeof(string), TimeProvider.System.GetUtcNow().AddDays(-1)).SubscribeAndComplete();
                cache.Insert("valid", ThirdEntryPayload, TimeProvider.System.GetUtcNow().AddDays(1)).SubscribeAndComplete();

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

    /// <summary>Tests that InvalidateAll with a specific type only removes entries of that type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllWithTypeShouldOnlyRemoveMatchingType()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("str1", FirstEntryPayload, typeof(string)).SubscribeAndComplete();
                cache.Insert("str2", SecondEntryPayload, typeof(string)).SubscribeAndComplete();
                cache.Insert("int1", ThirdEntryPayload, typeof(int)).SubscribeAndComplete();

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

    /// <summary>Tests that Insert with null expiration stores entries that never expire.</summary>
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
                cache.Insert("no_expiry", FirstEntryPayload).SubscribeAndComplete();

                // Typed insert without expiration
                cache.Insert("no_expiry_typed", SecondEntryPayload, typeof(string)).SubscribeAndComplete();

                // Bulk non-typed insert without expiration
                cache.Insert([new("bulk1", ThirdEntryPayload)]).SubscribeAndComplete();

                // Bulk typed insert without expiration
                cache.Insert([new("bulk2", FourthEntryPayload)], typeof(string)).SubscribeAndComplete();

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

    /// <summary>Tests that ForcedDateTimeKind property can be set and retrieved.</summary>
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

    /// <summary>Tests that Scheduler property returns a valid scheduler.</summary>
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

    /// <summary>Tests that Serializer property returns the serializer passed to the constructor.</summary>
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

    /// <summary>Tests constructor argument validation.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowOnNullArgs()
    {
        await Assert.That(static () => new SqliteBlobCache((string)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();
        await Assert.That(static () => new SqliteBlobCache("test.db", null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => new SqliteBlobCache((IAkavacheConnection)null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();
    }

    /// <summary>Tests that InvalidateAll removes all entries regardless of type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllShouldRemoveAllEntries()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("a", FirstEntryPayload).SubscribeAndComplete();
                cache.Insert("b", SecondEntryPayload, typeof(string)).SubscribeAndComplete();
                cache.Insert("c", ThirdEntryPayload, typeof(int)).SubscribeAndComplete();

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

    /// <summary>Tests that bulk Invalidate by keys removes only the specified keys.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BulkInvalidateShouldRemoveOnlySpecifiedKeys()
    {
        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                cache.Insert("a", FirstEntryPayload).SubscribeAndComplete();
                cache.Insert("b", SecondEntryPayload).SubscribeAndComplete();
                cache.Insert("c", ThirdEntryPayload).SubscribeAndComplete();

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
        const int InsertedPairCount = 2;

        using (Utility.WithEmptyDirectory(out _))
        {
            var cache = CreateCache();
            try
            {
                var future = TimeProvider.System.GetUtcNow().AddHours(1);
                KeyValuePair<string, byte[]>[] pairs =
                [
                    new("tk1", FirstTypedBulkPayload),
                    new("tk2", SecondTypedBulkPayload)
                ];
                cache.Insert(pairs, typeof(string), future).SubscribeAndComplete();

                var results = cache.Get(["tk1", "tk2"], typeof(string)).ToList().SubscribeGetValue();
                await Assert.That(results!.Count).IsEqualTo(InsertedPairCount);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that UpdateExpiration with null expiration (no expiry) makes entry permanently available.</summary>
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
                cache.Insert("k1", FirstEntryPayload, TimeProvider.System.GetUtcNow().Add(ShortLifetime)).SubscribeAndComplete();
                cache.Insert("k2", SecondEntryPayload, typeof(string), TimeProvider.System.GetUtcNow().Add(ShortLifetime)).SubscribeAndComplete();

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

    /// <summary>Subscribes to <paramref name="source"/> and hands back whatever error it produced.</summary>
    /// <typeparam name="T">The observable's element type.</typeparam>
    /// <param name="source">The observable to drain.</param>
    /// <returns>The error the sequence raised, or <see langword="null"/> when it did not raise one.</returns>
    internal static Exception? CaptureError<T>(IObservable<T> source)
    {
        Exception? error = null;
        _ = source.Subscribe(static _ => { }, e => error = e);
        return error;
    }

    /// <summary>Asserts that <paramref name="source"/> reported the cache as already disposed.</summary>
    /// <typeparam name="T">The observable's element type.</typeparam>
    /// <param name="source">The observable to drain.</param>
    /// <returns>A task.</returns>
    internal static async Task AssertDisposed<T>(IObservable<T> source) =>
        await Assert.That(CaptureError(source)).IsTypeOf<ObjectDisposedException>();

    /// <summary>Asserts that <paramref name="source"/> rejected a null argument.</summary>
    /// <typeparam name="T">The observable's element type.</typeparam>
    /// <param name="source">The observable to drain.</param>
    /// <returns>A task.</returns>
    private static async Task AssertArgumentNull<T>(IObservable<T> source) =>
        await Assert.That(CaptureError(source)).IsTypeOf<ArgumentNullException>();

    /// <summary>Asserts that <paramref name="source"/> rejected an empty or whitespace argument.</summary>
    /// <typeparam name="T">The observable's element type.</typeparam>
    /// <param name="source">The observable to drain.</param>
    /// <returns>A task.</returns>
    private static async Task AssertArgument<T>(IObservable<T> source) =>
        await Assert.That(CaptureError(source)).IsTypeOf<ArgumentException>();

    /// <summary>
    /// Creates a new instance of SqliteBlobCache that utilizes an InMemoryAkavacheConnection
    /// for storage, enabling fast, in-memory operations for unit tests and logic validations.
    /// This method bypasses file-based persistence by storing data entirely in memory.
    /// </summary>
    /// <returns>A SqliteBlobCache instance backed by an in-memory connection.</returns>
    private static SqliteBlobCache CreateCache() =>
        new(new InMemoryAkavacheConnection(), new SystemJsonSerializer(), ImmediateScheduler.Instance);
}
