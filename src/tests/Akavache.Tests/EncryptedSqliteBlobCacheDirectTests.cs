// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
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
    /// Tests Vacuum removes expired entries.
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

                // Vacuum should have run; valid entries remain
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
    /// Tests that the constructor throws ArgumentNullException for a null file name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorWithNullFileNameShouldThrow()
    {
        await Assert.That(() => new EncryptedSqliteBlobCache(null!, TestPassword, new SystemJsonSerializer())).Throws<ArgumentNullException>();
    }

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
            var cache = CreateCache(path);
            try
            {
                await Assert.That(async () => await cache.Insert((IEnumerable<KeyValuePair<string, byte[]>>)null!).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Insert((IEnumerable<KeyValuePair<string, byte[]>>)null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Insert([new KeyValuePair<string, byte[]>("k", [1])], (Type)null!).ToTask()).Throws<ArgumentNullException>();

                await Assert.That(async () => await cache.Insert("k", null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Insert("k", [1], (Type)null!).ToTask()).Throws<ArgumentNullException>();

                await Assert.That(async () => await cache.Invalidate((IEnumerable<string>)null!).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Invalidate((IEnumerable<string>)null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.Invalidate(["k"], (Type)null!).ToTask()).Throws<ArgumentNullException>();

                await Assert.That(async () => await cache.GetCreatedAt((string)null!).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetCreatedAt((IEnumerable<string>)null!).ToList().ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetCreatedAt((string)null!, typeof(string)).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetCreatedAt("k", (Type)null!).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetCreatedAt((IEnumerable<string>)null!, typeof(string)).ToList().ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.GetCreatedAt(["k"], (Type)null!).ToList().ToTask()).Throws<ArgumentNullException>();

                await Assert.That(async () => await cache.GetAllKeys((Type)null!).ToList().ToTask()).Throws<ArgumentNullException>();

                await Assert.That(async () => await cache.UpdateExpiration((IEnumerable<string>)null!, DateTimeOffset.Now).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.UpdateExpiration((IEnumerable<string>)null!, typeof(string), DateTimeOffset.Now).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.UpdateExpiration(["k"], (Type)null!, DateTimeOffset.Now).ToTask()).Throws<ArgumentNullException>();
                await Assert.That(async () => await cache.UpdateExpiration("k", (Type)null!, DateTimeOffset.Now).ToTask()).Throws<ArgumentNullException>();
            }
            finally
            {
                await cache.DisposeAsync();
            }
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
            var cache = CreateCache(path);
            try
            {
                await Assert.That(async () => await cache.Insert(" ", [1], typeof(string)).ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.Invalidate(" ").ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.Invalidate(" ", typeof(string)).ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.UpdateExpiration(" ", DateTimeOffset.Now).ToTask()).Throws<ArgumentException>();
                await Assert.That(async () => await cache.UpdateExpiration(" ", typeof(string), DateTimeOffset.Now).ToTask()).Throws<ArgumentException>();
            }
            finally
            {
                await cache.DisposeAsync();
            }
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
            var cache = CreateCache(path);
            try
            {
                await cache.Insert("k1", [1]).ToTask();
                await cache.Insert("k2", [2]).ToTask();

                var single = await cache.GetCreatedAt("k1").ToTask();
                await Assert.That(single).IsNotNull();

                var multi = await cache.GetCreatedAt(["k1", "k2"]).ToList().ToTask();
                await Assert.That(multi.Count).IsEqualTo(2);
            }
            finally
            {
                await cache.DisposeAsync();
            }
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
            var cache = CreateCache(path);
            try
            {
                await cache.Insert("k1", [1]).ToTask();
                await cache.Insert("k2", [2]).ToTask();

                var newExpiration = DateTimeOffset.Now.AddHours(1);
                await cache.UpdateExpiration("k1", newExpiration).ToTask();
                await cache.UpdateExpiration(["k1", "k2"], newExpiration).ToTask();

                var data = await cache.Get("k1").ToTask();
                await Assert.That(data).IsNotNull();
            }
            finally
            {
                await cache.DisposeAsync();
            }
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
            var cache = CreateCache(path);
            try
            {
                await cache.Insert("k1", [1]).ToTask();
                await cache.Insert("k2", [2]).ToTask();

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
    /// Tests Invalidate with a single key (non-typed) happy path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateSingleKeyShouldRemoveEntry()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = CreateCache(path);
            try
            {
                await cache.Insert("k1", [1]).ToTask();
                await cache.Insert("k2", [2]).ToTask();

                await cache.Invalidate("k1").ToTask();

                var keys = await cache.GetAllKeys().ToList().ToTask();
                await Assert.That(keys.Count).IsEqualTo(1);
            }
            finally
            {
                await cache.DisposeAsync();
            }
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
            var cache = CreateCache(path);
            try
            {
                await cache.Insert("k1", [1]).ToTask();

                await cache.Flush().ToTask();
                await cache.Flush(typeof(string)).ToTask();
            }
            finally
            {
                await cache.DisposeAsync();
            }
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
            var cache = CreateCache(path);
            try
            {
                var results = await cache.Get(["missing1", "missing2"]).ToList().ToTask();
                await Assert.That(results).IsEmpty();

                var typedResults = await cache.Get(["missing1", "missing2"], typeof(string)).ToList().ToTask();
                await Assert.That(typedResults).IsEmpty();
            }
            finally
            {
                await cache.DisposeAsync();
            }
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
            var cache = CreateCache(path);
            try
            {
                var single = await cache.GetCreatedAt("missing").ToTask();
                await Assert.That(single).IsNull();

                var singleTyped = await cache.GetCreatedAt("missing", typeof(string)).ToTask();
                await Assert.That(singleTyped).IsNull();

                var multi = await cache.GetCreatedAt(["missing"]).ToList().ToTask();
                await Assert.That(multi).IsEmpty();
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests <see cref="EncryptedSqliteBlobCache.GetOrCreateHttpService"/> constructs
    /// a fresh default <see cref="HttpService"/> when the cached value is
    /// <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateHttpServiceShouldConstructDefaultWhenNull()
    {
        var result = EncryptedSqliteBlobCache.GetOrCreateHttpService(null);

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsTypeOf<HttpService>();
    }

    /// <summary>
    /// Tests <see cref="EncryptedSqliteBlobCache.GetOrCreateHttpService"/> returns
    /// the already-cached instance when it is non-null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateHttpServiceShouldReturnExistingWhenNonNull()
    {
        var existing = new HttpService();

        var result = EncryptedSqliteBlobCache.GetOrCreateHttpService(existing);

        await Assert.That(result).IsSameReferenceAs(existing);
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
        var offset = new DateTimeOffset(2025, 6, 15, 12, 30, 0, TimeSpan.FromHours(5));

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
    /// Tests <see cref="EncryptedSqliteBlobCache.TryGetLegacyValueAsync"/> returns
    /// <see langword="null"/> when the legacy row is missing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetLegacyValueAsyncShouldReturnNullWhenLegacyRowMissing()
    {
        var connection = new InMemoryAkavacheConnection();

        var result = await EncryptedSqliteBlobCache.TryGetLegacyValueAsync(connection, "no-such-key", DateTimeOffset.UtcNow, null);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="EncryptedSqliteBlobCache.InitializeDatabase"/> creates the
    /// schema on the supplied connection and completes the observable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeDatabaseShouldCompleteAndCreateTable()
    {
        var connection = new InMemoryAkavacheConnection();

        var observable = EncryptedSqliteBlobCache.InitializeDatabase(connection, ImmediateScheduler.Instance);
        await observable.ToTask();

        var tableExists = await connection.TableExistsAsync("CacheEntry");
        await Assert.That(tableExists).IsTrue();
    }

    /// <summary>
    /// Tests <see cref="EncryptedSqliteBlobCache.InitializeDatabase"/> propagates
    /// errors when the underlying connection cannot create the table.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeDatabaseShouldErrorWhenCreateTableFails()
    {
        var connection = new InMemoryAkavacheConnection { FailCreateTable = true };

        var observable = EncryptedSqliteBlobCache.InitializeDatabase(connection, ImmediateScheduler.Instance);

        await Assert.That(async () => await observable.ToTask())
            .Throws<Exception>();
    }

    /// <summary>
    /// Creates a fresh <see cref="EncryptedSqliteBlobCache"/> at a unique path inside the supplied directory.
    /// </summary>
    /// <param name="path">The directory used to host the temporary cache database.</param>
    /// <returns>A new encrypted blob cache instance.</returns>
    private static EncryptedSqliteBlobCache CreateCache(string path) =>
        new(Path.Combine(path, $"test_{Guid.NewGuid():N}.db"), TestPassword, new SystemJsonSerializer());
}
