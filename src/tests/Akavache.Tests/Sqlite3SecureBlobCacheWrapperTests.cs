// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using Akavache.SystemTextJson;
using Sqlite3Extensions = Akavache.Sqlite3.AkavacheBuilderExtensions;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.Sqlite3.AkavacheBuilderExtensions.SecureBlobCacheWrapper.
/// </summary>
[Category("Akavache")]
public class Sqlite3SecureBlobCacheWrapperTests
{
    /// <summary>
    /// Tests constructor throws on null inner cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowOnNullInner() =>
        await Assert.That(static () => new Sqlite3Extensions.SecureBlobCacheWrapper(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests Insert and Get round-trip.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertAndGet()
    {
        var wrapper = CreateWrapper();
        try
        {
            await wrapper.Insert("k", [1, 2, 3]).ToTask();
            var data = await wrapper.Get("k").ToTask();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!.Length).IsEqualTo(3);
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Insert and Get for multiple keys.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertAndGetMultiple()
    {
        var wrapper = CreateWrapper();
        try
        {
            var pairs = new[]
            {
                new KeyValuePair<string, byte[]>("k1", [1]),
                new KeyValuePair<string, byte[]>("k2", [2]),
            };
            await wrapper.Insert(pairs).ToTask();

            var keys = await wrapper.GetAllKeys().ToList().ToTask();
            await Assert.That(keys).Contains("k1");
            await Assert.That(keys).Contains("k2");

            var results = await wrapper.Get(["k1", "k2"]).ToList().ToTask();
            await Assert.That(results.Count).IsEqualTo(2);
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Insert with type and Get with type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertAndGetWithType()
    {
        var wrapper = CreateWrapper();
        try
        {
            await wrapper.Insert("k", [1], typeof(string)).ToTask();
            var data = await wrapper.Get("k", typeof(string)).ToTask();
            await Assert.That(data).IsNotNull();
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Insert multiple with type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertMultipleWithType()
    {
        var wrapper = CreateWrapper();
        try
        {
            var pairs = new[]
            {
                new KeyValuePair<string, byte[]>("k1", [1]),
                new KeyValuePair<string, byte[]>("k2", [2]),
            };
            await wrapper.Insert(pairs, typeof(string)).ToTask();

            var results = await wrapper.Get(["k1", "k2"], typeof(string)).ToList().ToTask();
            await Assert.That(results.Count).IsEqualTo(2);

            var keys = await wrapper.GetAllKeys(typeof(string)).ToList().ToTask();
            await Assert.That(keys.Count).IsEqualTo(2);

            var all = await wrapper.GetAll(typeof(string)).ToList().ToTask();
            await Assert.That(all.Count).IsEqualTo(2);
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests GetCreatedAt operations.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldGetCreatedAt()
    {
        var wrapper = CreateWrapper();
        try
        {
            await wrapper.Insert("k1", [1]).ToTask();
            await wrapper.Insert("k2", [2]).ToTask();

            var single = await wrapper.GetCreatedAt("k1").ToTask();
            await Assert.That(single).IsNotNull();

            var multi = await wrapper.GetCreatedAt(["k1", "k2"]).ToList().ToTask();
            await Assert.That(multi.Count).IsEqualTo(2);

            await wrapper.Insert("k3", [3], typeof(int)).ToTask();
            var typed = await wrapper.GetCreatedAt("k3", typeof(int)).ToTask();
            await Assert.That(typed).IsNotNull();

            var typedMulti = await wrapper.GetCreatedAt(["k3"], typeof(int)).ToList().ToTask();
            await Assert.That(typedMulti.Count).IsEqualTo(1);
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Invalidate operations.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInvalidate()
    {
        var wrapper = CreateWrapper();
        try
        {
            await wrapper.Insert("k1", [1]).ToTask();
            await wrapper.Insert("k2", [2]).ToTask();
            await wrapper.Insert("k3", [3], typeof(string)).ToTask();
            await wrapper.Insert("k4", [4], typeof(int)).ToTask();

            await wrapper.Invalidate("k1").ToTask();
            await wrapper.Invalidate("k3", typeof(string)).ToTask();
            await wrapper.Invalidate(["k2"]).ToTask();
            await wrapper.Invalidate(["k4"], typeof(int)).ToTask();

            var keys = await wrapper.GetAllKeys().ToList().ToTask();
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests InvalidateAll operations.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInvalidateAll()
    {
        var wrapper = CreateWrapper();
        try
        {
            await wrapper.Insert("k1", [1], typeof(string)).ToTask();
            await wrapper.InvalidateAll(typeof(string)).ToTask();

            await wrapper.Insert("k2", [2]).ToTask();
            await wrapper.InvalidateAll().ToTask();

            var keys = await wrapper.GetAllKeys().ToList().ToTask();
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Flush operations.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldFlush()
    {
        var wrapper = CreateWrapper();
        try
        {
            await wrapper.Flush().ToTask();
            await wrapper.Flush(typeof(string)).ToTask();
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Vacuum operation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldVacuum()
    {
        var wrapper = CreateWrapper();
        try
        {
            await wrapper.Vacuum().ToTask();
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration operations.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldUpdateExpiration()
    {
        var wrapper = CreateWrapper();
        try
        {
            await wrapper.Insert("k1", [1]).ToTask();
            await wrapper.Insert("k2", [2], typeof(string)).ToTask();

            var future = DateTimeOffset.Now.AddHours(1);
            await wrapper.UpdateExpiration("k1", future).ToTask();
            await wrapper.UpdateExpiration("k2", typeof(string), future).ToTask();
            await wrapper.UpdateExpiration(["k1"], future).ToTask();
            await wrapper.UpdateExpiration(["k2"], typeof(string), future).ToTask();
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests Scheduler, Serializer, HttpService, ForcedDateTimeKind, and InnerCache properties.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldExposeProperties()
    {
        var wrapper = CreateWrapper();
        try
        {
            await Assert.That(wrapper.InnerCache).IsNotNull();
            await Assert.That(wrapper.Scheduler).IsNotNull();
            await Assert.That(wrapper.Serializer).IsNotNull();

            var http = wrapper.HttpService;
            await Assert.That(http).IsNotNull();
            wrapper.HttpService = http;

            wrapper.ForcedDateTimeKind = DateTimeKind.Utc;
            await Assert.That(wrapper.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
            wrapper.ForcedDateTimeKind = null;
        }
        finally
        {
            await wrapper.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests sync Dispose does not throw.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldDispose()
    {
        var wrapper = CreateWrapper();
        await Assert.That(() => wrapper.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// Creates a fresh <see cref="Sqlite3Extensions.SecureBlobCacheWrapper"/> over an in-memory backing cache.
    /// </summary>
    /// <returns>A new wrapper instance.</returns>
    private static Sqlite3Extensions.SecureBlobCacheWrapper CreateWrapper() =>
        new(new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));
}
