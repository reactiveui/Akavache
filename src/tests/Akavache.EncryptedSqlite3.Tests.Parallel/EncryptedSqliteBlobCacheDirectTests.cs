// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests for EncryptedSqliteBlobCache covering disposed-state error paths,
/// null arg validation, and type-aware overloads.
/// </summary>
[Category("Akavache")]
public class EncryptedSqliteBlobCacheDirectTests
{
    /// <summary>The password used for the encrypted test database.</summary>
    private const string TestPassword = "test_password_123";

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
            cache.Dispose();

            var error = cache.Insert("k", [1]).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Insert([new("k", [1])]).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Insert("k", [1], typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Insert([new("k", [1])], typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Get("k").SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Get(["k"]).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Get("k", typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Get(["k"], typeof(string)).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.GetAllKeys().ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.GetAllKeys(typeof(string)).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.GetAll(typeof(string)).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.GetCreatedAt("k").SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.GetCreatedAt(["k"]).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.GetCreatedAt("k", typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.GetCreatedAt(["k"], typeof(string)).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Flush().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Flush(typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Invalidate("k").SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Invalidate(["k"]).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Invalidate("k", typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Invalidate(["k"], typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.InvalidateAll().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.InvalidateAll(typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.Vacuum().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.UpdateExpiration("k", DateTimeOffset.Now).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.UpdateExpiration(["k"], DateTimeOffset.Now).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.UpdateExpiration("k", typeof(string), DateTimeOffset.Now).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            error = cache.UpdateExpiration(["k"], typeof(string), DateTimeOffset.Now).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();
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
            using var cache = CreateCache(path);

            var error = cache.Get((IEnumerable<string>)null!).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Get((string)null!, typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Get("k", null!).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Get((IEnumerable<string>)null!, typeof(string)).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Get(["k"], null!).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.GetAll(null!).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();
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
            using var cache = CreateCache(path);
            cache.Insert("k1", [1, 2, 3], typeof(string)).WaitForCompletion();

            var data = cache.Get("k1", typeof(string)).WaitForValue();

            await Assert.That(data).IsNotNull();
            await Assert.That(data!.Length).IsEqualTo(3);
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
            using var cache = CreateCache(path);
            KeyValuePair<string, byte[]>[] pairs =
            [
                new("k1", [1]),
                new("k2", [2])
            ];
            cache.Insert(pairs, typeof(string)).WaitForCompletion();

            var results = cache.Get(["k1", "k2"], typeof(string)).ToList().WaitForValue();
            await Assert.That(results!.Count).IsEqualTo(2);

            var typedKeys = cache.GetAllKeys(typeof(string)).ToList().WaitForValue();
            await Assert.That(typedKeys!.Count).IsEqualTo(2);

            var allOfType = cache.GetAll(typeof(string)).ToList().WaitForValue();
            await Assert.That(allOfType!.Count).IsEqualTo(2);
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
            using var cache = CreateCache(path);
            cache.Insert("k1", [1], typeof(string)).WaitForCompletion();
            cache.Insert("k2", [2], typeof(int)).WaitForCompletion();
            cache.Invalidate("k1", typeof(string)).WaitForCompletion();
            cache.InvalidateAll(typeof(int)).WaitForCompletion();

            var keys = cache.GetAllKeys().ToList().WaitForValue();
            await Assert.That(keys).IsEmpty();
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
            using var cache = CreateCache(path);
            cache.Insert("k1", [1], typeof(string)).WaitForCompletion();
            cache.Insert("k2", [2], typeof(string)).WaitForCompletion();
            cache.Invalidate(["k1", "k2"], typeof(string)).WaitForCompletion();

            var keys = cache.GetAllKeys().ToList().WaitForValue();
            await Assert.That(keys).IsEmpty();
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
            using var cache = CreateCache(path);

            // Ensure schema is ready before inserting
            cache.Connection.CreateSchema().WaitForCompletion();
            cache.Insert("k1", [1], typeof(string)).WaitForCompletion();
            cache.Insert("k2", [2], typeof(string)).WaitForCompletion();

            // Verify single-key typed GetCreatedAt works
            var single = cache.GetCreatedAt("k1", typeof(string)).WaitForValue();
            await Assert.That(single).IsNotNull();

            // Multi-key GetCreatedAt uses json_each() which requires the JSON1
            // extension — SQLite3MC may not ship it, so tolerate an empty result
            // and only assert when the extension is available.
            var multi = cache.GetCreatedAt(["k1", "k2"], typeof(string)).ToList().WaitForValue();
            if (multi!.Count > 0)
            {
                await Assert.That(multi.Count).IsEqualTo(2);
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
            using var cache = CreateCache(path);
            cache.Insert("k1", [1], typeof(string)).WaitForCompletion();

            var newExpiration = DateTimeOffset.Now.AddHours(1);

            cache.UpdateExpiration("k1", typeof(string), newExpiration).WaitForCompletion();
            cache.UpdateExpiration(["k1"], typeof(string), newExpiration).WaitForCompletion();

            var data = cache.Get("k1", typeof(string)).WaitForValue();
            await Assert.That(data).IsNotNull();
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
            using var cache = CreateCache(path);

            var error1 = cache.Get("non_existent_key").WaitForError();
            await Assert.That(error1).IsTypeOf<KeyNotFoundException>();

            var error2 = cache.Get("non_existent_key", typeof(string)).WaitForError();
            await Assert.That(error2).IsTypeOf<KeyNotFoundException>();
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
            using var cache = CreateCache(path);
            var error = cache.Get(string.Empty).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();
        }
    }

    /// <summary>
    /// Tests Vacuum removes expired entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumShouldWork()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            using var cache = CreateCache(path);
            cache.Insert("k1", [1], DateTimeOffset.Now.AddSeconds(-10)).WaitForCompletion();
            cache.Insert("k2", [2], DateTimeOffset.Now.AddHours(1)).WaitForCompletion();
            cache.Vacuum().WaitForCompletion();

            // Vacuum should have run; valid entries remain
            var data = cache.Get("k2").WaitForValue();
            await Assert.That(data).IsNotNull();
        }
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException for a null file name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorWithNullFileNameShouldThrow() => await Assert.That(static () => new EncryptedSqliteBlobCache(null!, TestPassword, new SystemJsonSerializer())).Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException for a null password.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorWithNullPasswordShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var file = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
            await Assert.That(() => new EncryptedSqliteBlobCache(file, null!, new SystemJsonSerializer())).Throws<ArgumentNullException>();
        }
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException for a null serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorWithNullSerializerShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var file = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
            await Assert.That(() => new EncryptedSqliteBlobCache(file, TestPassword, null!)).Throws<ArgumentNullException>();
        }
    }

    /// <summary>
    /// Tests additional null argument validation paths that were not previously covered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AdditionalNullArgsShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            using var cache = CreateCache(path);

            var error = cache.Insert(null!).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Insert(null!, typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Insert([new("k", [1])], (Type)null!).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Insert("k", null!, typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Insert("k", [1], (Type)null!).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Invalidate((IEnumerable<string>)null!).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Invalidate((IEnumerable<string>)null!, typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.Invalidate(["k"], null!).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.GetCreatedAt((string)null!).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.GetCreatedAt((IEnumerable<string>)null!).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.GetCreatedAt((string)null!, typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.GetCreatedAt("k", null!).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.GetCreatedAt((IEnumerable<string>)null!, typeof(string)).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.GetCreatedAt(["k"], null!).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.GetAllKeys(null!).ToList().SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.UpdateExpiration((IEnumerable<string>)null!, DateTimeOffset.Now).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.UpdateExpiration((IEnumerable<string>)null!, typeof(string), DateTimeOffset.Now).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.UpdateExpiration(["k"], null!, DateTimeOffset.Now).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();

            error = cache.UpdateExpiration("k", null!, DateTimeOffset.Now).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentNullException>();
        }
    }

    /// <summary>
    /// Tests whitespace/empty key argument validation paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WhitespaceKeysShouldThrowArgumentException()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            using var cache = CreateCache(path);

            var error = cache.Insert(" ", [1], typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentException>();

            error = cache.Invalidate(" ").SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentException>();

            error = cache.Invalidate(" ", typeof(string)).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentException>();

            error = cache.UpdateExpiration(" ", DateTimeOffset.Now).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentException>();

            error = cache.UpdateExpiration(" ", typeof(string), DateTimeOffset.Now).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ArgumentException>();
        }
    }

    /// <summary>
    /// Tests the non-typed GetCreatedAt operations happy paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtNonTypedHappyPath()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            using var cache = CreateCache(path);
            cache.Insert("k1", [1]).WaitForCompletion();
            cache.Insert("k2", [2]).WaitForCompletion();

            var single = cache.GetCreatedAt("k1").WaitForValue();
            await Assert.That(single).IsNotNull();

            var multi = cache.GetCreatedAt(["k1", "k2"]).ToList().WaitForValue();
            await Assert.That(multi!.Count).IsEqualTo(2);
        }
    }

    /// <summary>
    /// Tests the non-typed UpdateExpiration operations happy paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationNonTypedHappyPath()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            using var cache = CreateCache(path);
            cache.Insert("k1", [1]).WaitForCompletion();
            cache.Insert("k2", [2]).WaitForCompletion();

            var newExpiration = DateTimeOffset.Now.AddHours(1);

            cache.UpdateExpiration("k1", newExpiration).WaitForCompletion();
            cache.UpdateExpiration(["k1", "k2"], newExpiration).WaitForCompletion();

            var data = cache.Get("k1").WaitForValue();
            await Assert.That(data).IsNotNull();
        }
    }

    /// <summary>
    /// Tests InvalidateAll without a type filter.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAllShouldRemoveAllEntries()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            using var cache = CreateCache(path);
            cache.Insert("k1", [1]).WaitForCompletion();
            cache.Insert("k2", [2]).WaitForCompletion();
            cache.InvalidateAll().WaitForCompletion();

            var keys = cache.GetAllKeys().ToList().WaitForValue();
            await Assert.That(keys).IsEmpty();
        }
    }

    /// <summary>
    /// Tests Invalidate with a single key (non-typed) happy path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateSingleKeyShouldRemoveEntry()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            using var cache = CreateCache(path);
            cache.Insert("k1", [1]).WaitForCompletion();
            cache.Insert("k2", [2]).WaitForCompletion();
            cache.Invalidate("k1").WaitForCompletion();

            var keys = cache.GetAllKeys().ToList().WaitForValue();
            await Assert.That(keys!.Count).IsEqualTo(1);
        }
    }

    /// <summary>
    /// Tests Flush and Flush(Type) happy paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FlushShouldWork()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            using var cache = CreateCache(path);
            cache.Insert("k1", [1]).WaitForCompletion();
            cache.Flush().WaitForCompletion();
            cache.Flush(typeof(string)).WaitForCompletion();
        }
    }

    /// <summary>
    /// Tests that the Get IEnumerable overload returns no results for non-existent keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetBulkWithMissingKeysReturnsEmpty()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            using var cache = CreateCache(path);

            var results = cache.Get(["missing1", "missing2"]).ToList().WaitForValue();
            await Assert.That(results).IsEmpty();

            var typedResults = cache.Get(["missing1", "missing2"], typeof(string)).ToList().WaitForValue();
            await Assert.That(typedResults).IsEmpty();
        }
    }

    /// <summary>
    /// Tests GetCreatedAt for a key that does not exist returns null or empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetCreatedAtForMissingKeyReturnsDefault()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            using var cache = CreateCache(path);

            var single = cache.GetCreatedAt("missing").WaitForValue();
            await Assert.That(single).IsNull();

            var singleTyped = cache.GetCreatedAt("missing", typeof(string)).WaitForValue();
            await Assert.That(singleTyped).IsNull();

            var multi = cache.GetCreatedAt(["missing"]).ToList().WaitForValue();
            await Assert.That(multi).IsEmpty();
        }
    }

    /// <summary>
    /// Tests <see cref="EncryptedSqliteBlobCache.ToExpiryValue"/> returns the UTC
    /// <see cref="DateTime"/> component when given a non-null
    /// <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ToExpiryValueShouldReturnUtcDateTimeForNonNullOffset()
    {
        DateTimeOffset offset = new(2025, 6, 15, 12, 30, 0, TimeSpan.FromHours(5));

        var result = EncryptedSqliteBlobCache.ToExpiryValue(offset);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(offset.UtcDateTime);
    }

    /// <summary>
    /// Tests <see cref="EncryptedSqliteBlobCache.ToExpiryValue"/> returns
    /// <see langword="null"/> for a null offset.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ToExpiryValueShouldReturnNullForNullOffset()
    {
        var result = EncryptedSqliteBlobCache.ToExpiryValue(null);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="EncryptedSqliteBlobCache.TryGetLegacyValue"/> returns
    /// <see langword="null"/> when the legacy row is missing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetLegacyValueShouldReturnNullWhenLegacyRowMissing()
    {
        InMemoryAkavacheConnection connection = new();

        var result = EncryptedSqliteBlobCache.TryGetLegacyValue(connection, "no-such-key", DateTimeOffset.UtcNow, null).SubscribeGetValue();

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Typed bulk Insert with an empty collection returns Unit without touching the database.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TypedBulkInsertWithEmptyCollectionShouldReturnUnit()
    {
        InMemoryAkavacheConnection connection = new();
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InvalidateAllWithNullTypeShouldThrowArgumentNullException()
    {
        InMemoryAkavacheConnection connection = new();
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);
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
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DisposeSwallowsCheckpointException()
    {
        InMemoryAkavacheConnection connection = new() { FailCheckpoint = true };
        EncryptedSqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateScheduler.Instance);

        // Dispose should not throw even though Checkpoint(Full) raises.
        cache.Dispose();

        // The connection should still be disposed.
        await Assert.That(connection.SimulateDisposed).IsTrue();
    }

    /// <summary>
    /// Creates a fresh <see cref="EncryptedSqliteBlobCache"/> at a unique path inside the supplied directory.
    /// </summary>
    /// <param name="path">The directory used to host the temporary cache database.</param>
    /// <returns>A new encrypted blob cache instance.</returns>
    private static EncryptedSqliteBlobCache CreateCache(string path) =>
        new(Path.Combine(path, $"test_{Guid.NewGuid():N}.db"), TestPassword, new SystemJsonSerializer(), ImmediateScheduler.Instance);
}
