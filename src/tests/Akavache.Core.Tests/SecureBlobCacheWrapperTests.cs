// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for the private SecureBlobCacheWrapper class via the ISecureBlobCache it returns.
/// Achieved via the public AkavacheBuilder.WithInMemoryDefaults() which creates the wrapper.
/// </summary>
[Category("Akavache")]
public class SecureBlobCacheWrapperTests
{
    /// <summary>
    /// Tests basic Insert and Get round-trip.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertAndGet()
    {
        var cache = CreateSecureCache();
        try
        {
            await cache.Insert("k", [1, 2, 3]);
            var data = await cache.Get("k");
            await Assert.That(data).IsNotNull();
            await Assert.That(data!.Length).IsEqualTo(3);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests Insert and Get for multiple keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertAndGetMultiple()
    {
        var cache = CreateSecureCache();
        try
        {
            KeyValuePair<string, byte[]>[] pairs =
            [
                new("k1", [1]),
                new("k2", [2])
            ];
            await cache.Insert(pairs);

            var keys = await cache.GetAllKeys().ToList();
            await Assert.That(keys).Contains("k1");
            await Assert.That(keys).Contains("k2");

            var results = await cache.Get(["k1", "k2"]).ToList();
            await Assert.That(results.Count).IsEqualTo(2);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests Insert with type and Get with type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertAndGetWithType()
    {
        var cache = CreateSecureCache();
        try
        {
            await cache.Insert("k", [1], typeof(string));
            var data = await cache.Get("k", typeof(string));
            await Assert.That(data).IsNotNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests Insert multiple with type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertMultipleWithType()
    {
        var cache = CreateSecureCache();
        try
        {
            KeyValuePair<string, byte[]>[] pairs =
            [
                new("k1", [1]),
                new("k2", [2])
            ];
            await cache.Insert(pairs, typeof(string));

            var results = await cache.Get(["k1", "k2"], typeof(string)).ToList();
            await Assert.That(results.Count).IsEqualTo(2);

            var keys = await cache.GetAllKeys(typeof(string)).ToList();
            await Assert.That(keys.Count).IsEqualTo(2);

            var all = await cache.GetAll(typeof(string)).ToList();
            await Assert.That(all.Count).IsEqualTo(2);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests GetCreatedAt for single and multiple keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldGetCreatedAt()
    {
        var cache = CreateSecureCache();
        try
        {
            await cache.Insert("k1", [1]);
            await cache.Insert("k2", [2]);

            var single = await cache.GetCreatedAt("k1");
            await Assert.That(single).IsNotNull();

            var multi = await cache.GetCreatedAt(["k1", "k2"]).ToList();
            await Assert.That(multi.Count).IsEqualTo(2);

            await cache.Insert("k3", [3], typeof(int));
            var typed = await cache.GetCreatedAt("k3", typeof(int));
            await Assert.That(typed).IsNotNull();

            var typedMulti = await cache.GetCreatedAt(["k3"], typeof(int)).ToList();
            await Assert.That(typedMulti.Count).IsEqualTo(1);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests Invalidate operations.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInvalidate()
    {
        var cache = CreateSecureCache();
        try
        {
            await cache.Insert("k1", [1]);
            await cache.Insert("k2", [2]);
            await cache.Insert("k3", [3], typeof(string));
            await cache.Insert("k4", [4], typeof(int));

            await cache.Invalidate("k1");
            await cache.Invalidate("k3", typeof(string));
            await cache.Invalidate(["k2"]);
            await cache.Invalidate(["k4"], typeof(int));

            var keys = await cache.GetAllKeys().ToList();
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InvalidateAll and InvalidateAll(type).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInvalidateAll()
    {
        var cache = CreateSecureCache();
        try
        {
            await cache.Insert("k1", [1], typeof(string));
            await cache.InvalidateAll(typeof(string));

            await cache.Insert("k2", [2]);
            await cache.InvalidateAll();

            var keys = await cache.GetAllKeys().ToList();
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests Flush and Flush(type).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldFlush()
    {
        var cache = CreateSecureCache();
        try
        {
            await cache.Flush();
            await cache.Flush(typeof(string));
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration overloads.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldUpdateExpiration()
    {
        var cache = CreateSecureCache();
        try
        {
            await cache.Insert("k1", [1]);
            await cache.Insert("k2", [2], typeof(string));

            var future = DateTimeOffset.Now.AddHours(1);
            await cache.UpdateExpiration("k1", future);
            await cache.UpdateExpiration("k2", typeof(string), future);
            await cache.UpdateExpiration(["k1"], future);
            await cache.UpdateExpiration(["k2"], typeof(string), future);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests Scheduler, Serializer, and ForcedDateTimeKind properties.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldExposeProperties()
    {
        var cache = CreateSecureCache();
        try
        {
            await Assert.That(cache.Scheduler).IsNotNull();
            await Assert.That(cache.Serializer).IsNotNull();

            // ForcedDateTimeKind getter and setter
            cache.ForcedDateTimeKind = DateTimeKind.Utc;
            await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
            cache.ForcedDateTimeKind = null;
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests Dispose (sync) does not throw.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldDispose()
    {
        var cache = CreateSecureCache();
        await Assert.That(() => cache.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// Creates a secure cache using the builder for testing.
    /// </summary>
    /// <returns>The secure cache.</returns>
    private static ISecureBlobCache CreateSecureCache() =>
        CacheDatabase.CreateBuilder()
            .WithApplicationName("SecureWrapperTest_" + Guid.NewGuid().ToString("N"))
            .WithSerializer<SystemJsonSerializer>()
            .WithInMemoryDefaults()
            .Build()
            .Secure!;
}
