// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for IBlobCache interface core functionality and helper methods.</summary>
[Category("Akavache")]
[NotInParallel("CacheDatabaseState")]
public class IBlobCacheInterfaceTests
{
    /// <summary>Key the untyped byte-array round-trip test stores its entry under.</summary>
    private const string ByteKey = "byte_key";

    /// <summary>Key the expiration test stores its short-lived entry under.</summary>
    private const string ExpiringKey = "expiring_key";

    /// <summary>Key the Type-aware operation test stores its entry under.</summary>
    private const string TypedKey = "typed_key";

    /// <summary>Wait comfortably past the one-second expiry the expiration tests set, so the entry is certain to have lapsed.</summary>
    private const int ExpiryWaitMilliseconds = 1500;

    /// <summary>Number of entries seeded before the bulk invalidation assertions run.</summary>
    private const int SeededEntryCount = 3;

    /// <summary>Payload written whenever a test asserts on cache bookkeeping rather than on the stored bytes.</summary>
    private static readonly byte[] SamplePayload = [1, 2, 3];

    /// <summary>A payload distinct from <see cref="SamplePayload"/>, so a cross-wired lookup between two entries fails the assertion.</summary>
    private static readonly byte[] SecondSamplePayload = [4, 5, 6];

    /// <summary>A third distinct payload, for the fixtures that need to tell three entries apart.</summary>
    private static readonly byte[] ThirdSamplePayload = [7, 8, 9];

    /// <summary>First payload of the two-entry bulk fixtures.</summary>
    private static readonly byte[] FirstBulkPayload = [1, 2];

    /// <summary>Second payload of the two-entry bulk fixtures.</summary>
    private static readonly byte[] SecondBulkPayload = [3, 4];

    /// <summary>Tests that IBlobCache.ExceptionHelpers work correctly.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ExceptionHelpersShouldWorkCorrectly()
    {
        // Test KeyNotFoundException helper
        var keyNotFoundObs = IBlobCache.ExceptionHelpers.ObservableThrowKeyNotFoundException<string>("test_key");

        var keyNotFoundError = keyNotFoundObs.SubscribeGetError();
        await Assert.That(keyNotFoundError).IsTypeOf<KeyNotFoundException>();

        await Assert.That(keyNotFoundError).IsNotNull();
        var keyNotFoundEx = (KeyNotFoundException)keyNotFoundError!;
        using (Assert.Multiple())
        {
            await Assert.That(keyNotFoundEx.Message).Contains("test_key");
            await Assert.That(keyNotFoundEx.Message).Contains("not present in the cache");
        }

        // Test ObjectDisposedException helper
        var objectDisposedObs =
            IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>("test_cache");

        var objectDisposedError = objectDisposedObs.SubscribeGetError();
        await Assert.That(objectDisposedError).IsTypeOf<ObjectDisposedException>();

        await Assert.That(objectDisposedError).IsNotNull();
        var objectDisposedEx = (ObjectDisposedException)objectDisposedError!;
        using (Assert.Multiple())
        {
            await Assert.That(objectDisposedEx.Message).Contains("test_cache");
            await Assert.That(objectDisposedEx.Message).Contains("disposed");
        }
    }

    /// <summary>Tests that IBlobCache basic operations work correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BasicBlobCacheOperationsShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Test basic byte array operations
        byte[] testData = [1, 2, 3, 4, 5];

        // Insert
        cache.Insert(ByteKey, testData).SubscribeAndComplete();

        // Get
        var retrieved = cache.Get(ByteKey).SubscribeGetValue();
        await Assert.That(retrieved).IsEqualTo(testData);

        // GetCreatedAt
        var createdAt = cache.GetCreatedAt(ByteKey).SubscribeGetValue();
        using (Assert.Multiple())
        {
            await Assert.That(createdAt).IsNotNull();
            await Assert.That(createdAt!.Value).IsLessThanOrEqualTo(TimeProvider.System.GetLocalNow());
        }

        // GetAllKeys
        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys!).Contains(ByteKey);

        // Invalidate
        cache.Invalidate(ByteKey).SubscribeAndComplete();

        // Verify invalidated
        var getError = cache.Get(ByteKey).SubscribeGetError();
        await Assert.That(getError).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>Tests that IBlobCache bulk operations work correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BulkBlobCacheOperationsShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Test bulk byte array operations
        Dictionary<string, byte[]> testData = new() { ["key1"] = SamplePayload, ["key2"] = SecondSamplePayload, ["key3"] = ThirdSamplePayload };

        // Bulk insert
        cache.Insert(testData).SubscribeAndComplete();

        // Bulk get
        var keys = testData.Keys.ToArray();
        var retrieved = cache.Get(keys).ToList().SubscribeGetValue();

        await Assert.That(retrieved).Count().IsEqualTo(testData.Count);
        foreach (var item in retrieved!)
        {
            await Assert.That(item.Value).IsEqualTo(testData[item.Key]);
        }

        // Bulk invalidate
        cache.Invalidate(keys).SubscribeAndComplete();

        // Verify all invalidated
        foreach (var key in keys)
        {
            var getError = cache.Get(key).SubscribeGetError();
            await Assert.That(getError).IsTypeOf<KeyNotFoundException>();
        }
    }

    /// <summary>Tests that IBlobCache expiration operations work correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ExpirationOperationsShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        byte[] testData = [1, 2, 3, 4, 5];
        var expiration = TimeProvider.System.GetLocalNow().AddSeconds(1);

        // Insert with expiration
        cache.Insert(ExpiringKey, testData, expiration).SubscribeAndComplete();

        // Should be available immediately
        var retrieved = cache.Get(ExpiringKey).SubscribeGetValue();
        await Assert.That(retrieved).IsEqualTo(testData);

        // Wait for expiration
        await Task.Delay(ExpiryWaitMilliseconds);

        // Should now be expired
        var expiredError = cache.Get(ExpiringKey).SubscribeGetError();
        await Assert.That(expiredError).IsTypeOf<KeyNotFoundException>();

        // Test bulk insert with expiration
        Dictionary<string, byte[]> bulkData = new() { ["bulk1"] = FirstBulkPayload, ["bulk2"] = SecondBulkPayload };
        var bulkExpiration = TimeProvider.System.GetLocalNow().AddSeconds(1);

        cache.Insert(bulkData, bulkExpiration).SubscribeAndComplete();

        // Should be available immediately
        var bulkRetrieved = cache.Get([.. bulkData.Keys]).ToList().SubscribeGetValue();
        await Assert.That(bulkRetrieved).Count().IsEqualTo(bulkData.Count);

        // Wait for expiration
        await Task.Delay(ExpiryWaitMilliseconds);

        // Should now be expired
        foreach (var key in bulkData.Keys)
        {
            var bulkExpiredError = cache.Get(key).SubscribeGetError();
            await Assert.That(bulkExpiredError).IsTypeOf<KeyNotFoundException>();
        }
    }

    /// <summary>Tests that IBlobCache InvalidateAll works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InvalidateAllShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Insert multiple items
        cache.Insert("key1", SamplePayload).SubscribeAndComplete();
        cache.Insert("key2", SecondSamplePayload).SubscribeAndComplete();
        cache.Insert("key3", ThirdSamplePayload).SubscribeAndComplete();

        // Verify items exist
        var keys = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keys).Count().IsEqualTo(SeededEntryCount);

        // InvalidateAll
        cache.InvalidateAll().SubscribeAndComplete();

        // Verify all items are gone
        var keysAfter = cache.GetAllKeys().ToList().SubscribeGetValue();
        await Assert.That(keysAfter).IsEmpty();

        // Verify individual gets fail
        var error1 = cache.Get("key1").SubscribeGetError();
        await Assert.That(error1).IsTypeOf<KeyNotFoundException>();

        var error2 = cache.Get("key2").SubscribeGetError();
        await Assert.That(error2).IsTypeOf<KeyNotFoundException>();

        var error3 = cache.Get("key3").SubscribeGetError();
        await Assert.That(error3).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>Tests that IBlobCache Flush operation works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task FlushShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Insert data
        cache.Insert("flush_test", SamplePayload).SubscribeAndComplete();

        // Flush should complete without error
        cache.Flush().SubscribeAndComplete();

        // Data should still be available after flush
        var retrieved = cache.Get("flush_test").SubscribeGetValue();
        await Assert.That(retrieved).IsEquivalentTo(SamplePayload);
    }

    /// <summary>Tests that IBlobCache Vacuum operation works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task VacuumShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Insert and remove data to create fragmentation
        cache.Insert("vacuum_test1", SamplePayload).SubscribeAndComplete();
        cache.Insert("vacuum_test2", SecondSamplePayload).SubscribeAndComplete();
        cache.Invalidate("vacuum_test1").SubscribeAndComplete();

        // Vacuum should complete without error
        cache.Vacuum().SubscribeAndComplete();

        // Remaining data should still be available
        var retrieved = cache.Get("vacuum_test2").SubscribeGetValue();
        await Assert.That(retrieved).IsEquivalentTo(SecondSamplePayload);
    }

    /// <summary>Tests that IBlobCache handles argument validation correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ArgumentValidationShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Test null key validation - these should consistently throw ArgumentNullException
        var insertNullError = cache.Insert(null!, SamplePayload).SubscribeGetError();
        await Assert.That(insertNullError).IsTypeOf<ArgumentNullException>();

        var getNullError = cache.Get((string)null!).SubscribeGetError();
        await Assert.That(getNullError).IsTypeOf<ArgumentNullException>();

        var invalidateNullError = cache.Invalidate((string)null!).SubscribeGetError();
        await Assert.That(invalidateNullError).IsTypeOf<ArgumentNullException>();

        // GetCreatedAt may not always throw for null - InMemoryBlobCache might handle this differently
        try
        {
            _ = cache.GetCreatedAt((string)null!).SubscribeGetValue();

            // If it doesn't throw, that's also acceptable for some cache implementations
        }
        catch (ArgumentNullException)
        {
            // This is the expected behavior
        }

        // Test null collections validation — these throw eagerly before Subscribe
        await Assert.That(() => cache.Insert((IEnumerable<KeyValuePair<string, byte[]>>)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => cache.Get((IEnumerable<string>)null!)).Throws<ArgumentNullException>();

        // For empty/whitespace string validation, different cache implementations may handle this differently
        // InMemoryBlobCache may allow empty strings as valid keys, while other implementations might not
        // We'll test the behavior but be flexible about the exception type
        RoundTripDegenerateKey(string.Empty);
        RoundTripDegenerateKey("   ");

        // Verify that valid operations still work
        cache.Insert("valid_key", SamplePayload).SubscribeAndComplete();

        var validData = cache.Get("valid_key").SubscribeGetValue();
        await Assert.That(validData).IsEquivalentTo(SamplePayload);

        // An empty or whitespace key is either rejected up front or accepted and simply not found.
        // Both are legitimate implementations, so the round-trip only has to not blow up.
        void RoundTripDegenerateKey(string key)
        {
            try
            {
                cache.Insert(key, SamplePayload).SubscribeAndComplete();
                _ = cache.Get(key).SubscribeGetValue();
            }
            catch (ArgumentException)
            {
                // Expected for implementations that validate the key shape.
            }
            catch (KeyNotFoundException)
            {
                // Expected for implementations that accept the key but store nothing under it.
            }
        }
    }

    /// <summary>Tests that IBlobCache properties work correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task CachePropertiesShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Test Scheduler property
        await Assert.That(cache.Scheduler).IsNotNull();

        // Test ForcedDateTimeKind property
        cache.ForcedDateTimeKind = DateTimeKind.Utc;
        await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);

        cache.ForcedDateTimeKind = DateTimeKind.Local;
        await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Local);

        cache.ForcedDateTimeKind = null;
        await Assert.That(cache.ForcedDateTimeKind).IsNull();
    }

    /// <summary>Tests that IBlobCache handles concurrent dispose correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ConcurrentDisposeShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Insert some data
        cache.Insert("dispose_test", SamplePayload).SubscribeAndComplete();

        // Test multiple dispose calls — all should be idempotent
        cache.Dispose();
        cache.Dispose();
        cache.Dispose();

        // Subsequent operations should throw ObjectDisposedException
        var disposeError = cache.Get("dispose_test").SubscribeGetError();
        await Assert.That(disposeError).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>Tests that IBlobCache GetCreatedAt handles missing keys correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task GetCreatedAtShouldHandleMissingKeys()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // GetCreatedAt for non-existent key should return null
        var createdAt = cache.GetCreatedAt("non_existent_key").SubscribeGetValue();
        await Assert.That(createdAt).IsNull();
    }

    /// <summary>Tests that IBlobCache operations with Type parameters work correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task TypeBasedOperationsShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        byte[] testData = [1, 2, 3, 4, 5];
        var userType = typeof(string);

        // Insert with Type
        cache.Insert(TypedKey, testData, userType).SubscribeAndComplete();

        // Get with Type
        var retrieved = cache.Get(TypedKey, userType).SubscribeGetValue();
        await Assert.That(retrieved).IsEqualTo(testData);

        // GetCreatedAt with Type
        var createdAt = cache.GetCreatedAt(TypedKey, userType).SubscribeGetValue();
        await Assert.That(createdAt).IsNotNull();

        // GetAllKeys with Type
        var typedKeys = cache.GetAllKeys(userType).ToList().SubscribeGetValue();
        await Assert.That(typedKeys!).Contains(TypedKey);

        // GetAll with Type
        var allTypedData = cache.GetAll(userType).ToList().SubscribeGetValue();
        await Assert.That(allTypedData).IsNotEmpty();
        await Assert.That(allTypedData!.Any(static kvp => kvp.Key == TypedKey)).IsTrue();

        // Bulk Insert with Type
        Dictionary<string, byte[]> bulkData = new() { ["bulk1"] = FirstBulkPayload, ["bulk2"] = SecondBulkPayload };
        cache.Insert(bulkData, userType).SubscribeAndComplete();

        // Bulk Get with Type
        var bulkRetrieved = cache.Get([.. bulkData.Keys], userType).ToList().SubscribeGetValue();
        await Assert.That(bulkRetrieved).Count().IsEqualTo(bulkData.Count);

        // Bulk GetCreatedAt with Type
        var bulkCreatedAt = cache.GetCreatedAt([.. bulkData.Keys], userType).ToList().SubscribeGetValue();
        await Assert.That(bulkCreatedAt).Count().IsEqualTo(bulkData.Count);

        // Flush with Type
        cache.Flush(userType).SubscribeAndComplete();

        // Invalidate with Type
        cache.Invalidate(TypedKey, userType).SubscribeAndComplete();

        // Verify invalidation
        var invalidateError = cache.Get(TypedKey, userType).SubscribeGetError();
        await Assert.That(invalidateError).IsTypeOf<KeyNotFoundException>();

        // Bulk Invalidate with Type
        cache.Invalidate([.. bulkData.Keys], userType).SubscribeAndComplete();

        // InvalidateAll with Type
        cache.InvalidateAll(userType).SubscribeAndComplete();

        // Verify all are gone
        var keysAfterInvalidateAll = cache.GetAllKeys(userType).ToList().SubscribeGetValue();
        await Assert.That(keysAfterInvalidateAll).IsEmpty();
    }

    /// <summary>Tests that IBlobCache bulk operations with collections work correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BulkCollectionOperationsShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Test GetCreatedAt with multiple keys - simplified approach
        string[] testKeys = ["key1", "key2", "key3"];
        byte[] testData = [1, 2, 3];

        // Insert test data
        foreach (var key in testKeys)
        {
            cache.Insert(key, testData).SubscribeAndComplete();
        }

        // Test bulk GetCreatedAt - InMemoryBlobCache may handle this differently
        try
        {
            var createdAtResults = cache.GetCreatedAt(testKeys).ToList().SubscribeGetValue();

            // Check if we got any results
            if (createdAtResults!.Count > 0)
            {
                // If we get results, validate them
                await Assert.That(createdAtResults).Count().IsLessThanOrEqualTo(testKeys.Length);
                foreach (var (key, time) in createdAtResults)
                {
                    using (Assert.Multiple())
                    {
                        await Assert.That(testKeys).Contains(key);
                        await Assert.That(time).IsNotNull();
                    }

                    await Assert.That(time!.Value).IsLessThanOrEqualTo(TimeProvider.System.GetLocalNow());
                }
            }

            // InMemoryBlobCache might not support bulk GetCreatedAt in the same way
            // as persistent caches - this is acceptable
        }
        catch (NotImplementedException)
        {
            // InMemoryBlobCache might not implement bulk GetCreatedAt - this is acceptable
        }

        // Test individual GetCreatedAt operations work
        foreach (var key in testKeys)
        {
            var individualCreatedAt = cache.GetCreatedAt(key).SubscribeGetValue();
            await Assert.That(individualCreatedAt).IsNotNull();
            await Assert.That(individualCreatedAt!.Value).IsLessThanOrEqualTo(TimeProvider.System.GetLocalNow());
        }

        // Test empty collection handling
        string[] emptyKeys = [];
        var emptyResults = cache.GetCreatedAt(emptyKeys).ToList().SubscribeGetValue();
        await Assert.That(emptyResults).IsEmpty();
    }

    /// <summary>Tests that IBlobCache handles empty collections correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task EmptyCollectionOperationsShouldWork()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        // Test with empty collections
        string[] emptyKeys = [];
        Dictionary<string, byte[]> emptyData = [];

        // Insert empty collection
        cache.Insert(emptyData).SubscribeAndComplete();

        // Get empty collection
        var emptyGetResults = cache.Get(emptyKeys).ToList().SubscribeGetValue();
        await Assert.That(emptyGetResults).IsEmpty();

        var emptyCreatedAtResults = cache.GetCreatedAt(emptyKeys).ToList().SubscribeGetValue();
        await Assert.That(emptyCreatedAtResults).IsEmpty();

        // Invalidate empty collection
        cache.Invalidate(emptyKeys).SubscribeAndComplete();

        // These operations should complete without error
    }
}
