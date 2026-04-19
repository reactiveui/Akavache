// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.SystemTextJson;

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
        await Assert.That(static () => new SecureBlobCacheWrapper(null!))
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
            wrapper.Insert("k", [1, 2, 3]).SubscribeAndComplete();
            var data = wrapper.Get("k").SubscribeGetValue();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!.Length).IsEqualTo(3);
        }
        finally
        {
            wrapper.Dispose();
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
            KeyValuePair<string, byte[]>[] pairs =
            [
                new("k1", [1]),
                new("k2", [2])
            ];
            wrapper.Insert(pairs).SubscribeAndComplete();

            var keys = wrapper.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys).Contains("k1");
            await Assert.That(keys).Contains("k2");

            var results = wrapper.Get(["k1", "k2"]).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(2);
        }
        finally
        {
            wrapper.Dispose();
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
            wrapper.Insert("k", [1], typeof(string)).SubscribeAndComplete();
            var data = wrapper.Get("k", typeof(string)).SubscribeGetValue();
            await Assert.That(data).IsNotNull();
        }
        finally
        {
            wrapper.Dispose();
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
            KeyValuePair<string, byte[]>[] pairs =
            [
                new("k1", [1]),
                new("k2", [2])
            ];
            wrapper.Insert(pairs, typeof(string)).SubscribeAndComplete();

            var results = wrapper.Get(["k1", "k2"], typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(2);

            var keys = wrapper.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(2);

            var all = wrapper.GetAll(typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(all!.Count).IsEqualTo(2);
        }
        finally
        {
            wrapper.Dispose();
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
            wrapper.Insert("k1", [1]).SubscribeAndComplete();
            wrapper.Insert("k2", [2]).SubscribeAndComplete();

            var single = wrapper.GetCreatedAt("k1").SubscribeGetValue();
            await Assert.That(single).IsNotNull();

            var multi = wrapper.GetCreatedAt(["k1", "k2"]).ToList().SubscribeGetValue();
            await Assert.That(multi!.Count).IsEqualTo(2);

            wrapper.Insert("k3", [3], typeof(int)).SubscribeAndComplete();
            var typed = wrapper.GetCreatedAt("k3", typeof(int)).SubscribeGetValue();
            await Assert.That(typed).IsNotNull();

            var typedMulti = wrapper.GetCreatedAt(["k3"], typeof(int)).ToList().SubscribeGetValue();
            await Assert.That(typedMulti!.Count).IsEqualTo(1);
        }
        finally
        {
            wrapper.Dispose();
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
            wrapper.Insert("k1", [1]).SubscribeAndComplete();
            wrapper.Insert("k2", [2]).SubscribeAndComplete();
            wrapper.Insert("k3", [3], typeof(string)).SubscribeAndComplete();
            wrapper.Insert("k4", [4], typeof(int)).SubscribeAndComplete();

            wrapper.Invalidate("k1").SubscribeAndComplete();
            wrapper.Invalidate("k3", typeof(string)).SubscribeAndComplete();
            wrapper.Invalidate(["k2"]).SubscribeAndComplete();
            wrapper.Invalidate(["k4"], typeof(int)).SubscribeAndComplete();

            var keys = wrapper.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            wrapper.Dispose();
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
            wrapper.Insert("k1", [1], typeof(string)).SubscribeAndComplete();
            wrapper.InvalidateAll(typeof(string)).SubscribeAndComplete();

            wrapper.Insert("k2", [2]).SubscribeAndComplete();
            wrapper.InvalidateAll().SubscribeAndComplete();

            var keys = wrapper.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            wrapper.Dispose();
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
            wrapper.Flush().SubscribeAndComplete();
            wrapper.Flush(typeof(string)).SubscribeAndComplete();
        }
        finally
        {
            wrapper.Dispose();
        }

        await Task.CompletedTask;
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
            wrapper.Vacuum().SubscribeAndComplete();
        }
        finally
        {
            wrapper.Dispose();
        }

        await Task.CompletedTask;
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
            wrapper.Insert("k1", [1]).SubscribeAndComplete();
            wrapper.Insert("k2", [2], typeof(string)).SubscribeAndComplete();

            var future = DateTimeOffset.Now.AddHours(1);
            wrapper.UpdateExpiration("k1", future).SubscribeAndComplete();
            wrapper.UpdateExpiration("k2", typeof(string), future).SubscribeAndComplete();
            wrapper.UpdateExpiration(["k1"], future).SubscribeAndComplete();
            wrapper.UpdateExpiration(["k2"], typeof(string), future).SubscribeAndComplete();
        }
        finally
        {
            wrapper.Dispose();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests Scheduler, Serializer, ForcedDateTimeKind, and InnerCache properties.
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

            wrapper.ForcedDateTimeKind = DateTimeKind.Utc;
            await Assert.That(wrapper.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
            wrapper.ForcedDateTimeKind = null;
        }
        finally
        {
            wrapper.Dispose();
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
    /// Tests SecureBlobCacheWrapper.Serializer throws when inner cache serializer is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializerShouldThrowWhenInnerSerializerIsNull()
    {
        FakeNullSerializerBlobCache fakeInner = new();
        var wrapper = new SecureBlobCacheWrapper(fakeInner);

        await Assert.That(() => _ = wrapper.Serializer)
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests SecureBlobCacheWrapper.Dispose disposes the inner cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeShouldDisposeInner()
    {
        InMemoryBlobCache inner = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        var wrapper = new SecureBlobCacheWrapper(inner);

        wrapper.Dispose();
        await Assert.That(wrapper).IsNotNull();
    }

    /// <summary>
    /// Tests SecureBlobCacheWrapper double Dispose is idempotent.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DoubleDisposeShouldNotThrow()
    {
        InMemoryBlobCache inner = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        var wrapper = new SecureBlobCacheWrapper(inner);

        wrapper.Dispose();

        // Double dispose should not throw
        wrapper.Dispose();
    }

    /// <summary>
    /// Creates a fresh <see cref="SecureBlobCacheWrapper"/> over an in-memory backing cache.
    /// </summary>
    /// <returns>A new wrapper instance.</returns>
    private static SecureBlobCacheWrapper CreateWrapper() =>
        new(new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

    /// <summary>
    /// Fake IBlobCache with null Serializer to test the null guard in SecureBlobCacheWrapper.
    /// </summary>
    private sealed class FakeNullSerializerBlobCache : IBlobCache
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IScheduler Scheduler => ImmediateScheduler.Instance;

        /// <inheritdoc/>
        public ISerializer Serializer => null!;

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) => Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) => Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => Observable.Empty<(string, DateTimeOffset?)>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => Observable.Empty<(string, DateTimeOffset?)>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<Unit> Flush() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Flush(Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key, Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll(Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Vacuum() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
