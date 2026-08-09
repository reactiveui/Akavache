// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for Akavache.Sqlite3.AkavacheBuilderExtensions.SecureBlobCacheWrapper.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class Sqlite3SecureBlobCacheWrapperTests
{
    /// <summary>How many keys the batch tests write, and therefore how many the batch reads must return.</summary>
    private const int BatchedKeyCount = 2;

    /// <summary>Tests constructor throws on null inner cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowOnNullInner() =>
        await Assert.That(static () => new SecureBlobCacheWrapper(null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests Insert and Get round-trip.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertAndGet()
    {
        var wrapper = CreateWrapper();
        try
        {
            byte[] payload = [1, 2, 3];
            wrapper.Insert("k", payload).WaitForCompletion();
            var data = wrapper.Get("k").SubscribeGetValue();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!.Length).IsEqualTo(payload.Length);
        }
        finally
        {
            wrapper.Dispose();
        }
    }

    /// <summary>Tests Insert and Get for multiple keys.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertAndGetMultiple()
    {
        var wrapper = CreateWrapper();
        try
        {
            byte[] firstPayload = [1];
            byte[] secondPayload = [2];
            KeyValuePair<string, byte[]>[] pairs =
            [
                new("k1", firstPayload),
                new("k2", secondPayload)
            ];
            wrapper.Insert(pairs).WaitForCompletion();

            var keys = wrapper.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys).Contains("k1");
            await Assert.That(keys).Contains("k2");

            var results = wrapper.Get(["k1", "k2"]).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(BatchedKeyCount);
        }
        finally
        {
            wrapper.Dispose();
        }
    }

    /// <summary>Tests Insert with type and Get with type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertAndGetWithType()
    {
        var wrapper = CreateWrapper();
        try
        {
            wrapper.Insert("k", [1], typeof(string)).WaitForCompletion();
            var data = wrapper.Get("k", typeof(string)).SubscribeGetValue();
            await Assert.That(data).IsNotNull();
        }
        finally
        {
            wrapper.Dispose();
        }
    }

    /// <summary>Tests Insert multiple with type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInsertMultipleWithType()
    {
        var wrapper = CreateWrapper();
        try
        {
            byte[] firstPayload = [1];
            byte[] secondPayload = [2];
            KeyValuePair<string, byte[]>[] pairs =
            [
                new("k1", firstPayload),
                new("k2", secondPayload)
            ];
            wrapper.Insert(pairs, typeof(string)).WaitForCompletion();

            var results = wrapper.Get(["k1", "k2"], typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(results!.Count).IsEqualTo(BatchedKeyCount);

            var keys = wrapper.GetAllKeys(typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(keys!.Count).IsEqualTo(BatchedKeyCount);

            var all = wrapper.GetAll(typeof(string)).ToList().SubscribeGetValue();
            await Assert.That(all!.Count).IsEqualTo(BatchedKeyCount);
        }
        finally
        {
            wrapper.Dispose();
        }
    }

    /// <summary>Tests GetCreatedAt operations.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldGetCreatedAt()
    {
        var wrapper = CreateWrapper();
        try
        {
            byte[] firstPayload = [1];
            byte[] secondPayload = [2];
            byte[] thirdPayload = [3];
            wrapper.Insert("k1", firstPayload).WaitForCompletion();
            wrapper.Insert("k2", secondPayload).WaitForCompletion();

            var single = wrapper.GetCreatedAt("k1").SubscribeGetValue();
            await Assert.That(single).IsNotNull();

            var multi = wrapper.GetCreatedAt(["k1", "k2"]).ToList().SubscribeGetValue();
            await Assert.That(multi!.Count).IsEqualTo(BatchedKeyCount);

            wrapper.Insert("k3", thirdPayload, typeof(int)).WaitForCompletion();
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

    /// <summary>Tests Invalidate operations.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInvalidate()
    {
        var wrapper = CreateWrapper();
        try
        {
            byte[] firstPayload = [1];
            byte[] secondPayload = [2];
            byte[] thirdPayload = [3];
            byte[] fourthPayload = [4];
            wrapper.Insert("k1", firstPayload).WaitForCompletion();
            wrapper.Insert("k2", secondPayload).WaitForCompletion();
            wrapper.Insert("k3", thirdPayload, typeof(string)).WaitForCompletion();
            wrapper.Insert("k4", fourthPayload, typeof(int)).WaitForCompletion();

            wrapper.Invalidate("k1").WaitForCompletion();
            wrapper.Invalidate("k3", typeof(string)).WaitForCompletion();
            wrapper.Invalidate(["k2"]).WaitForCompletion();
            wrapper.Invalidate(["k4"], typeof(int)).WaitForCompletion();

            var keys = wrapper.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            wrapper.Dispose();
        }
    }

    /// <summary>Tests InvalidateAll operations.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldInvalidateAll()
    {
        var wrapper = CreateWrapper();
        try
        {
            byte[] firstPayload = [1];
            byte[] secondPayload = [2];
            wrapper.Insert("k1", firstPayload, typeof(string)).WaitForCompletion();
            wrapper.InvalidateAll(typeof(string)).WaitForCompletion();

            wrapper.Insert("k2", secondPayload).WaitForCompletion();
            wrapper.InvalidateAll().WaitForCompletion();

            var keys = wrapper.GetAllKeys().ToList().SubscribeGetValue();
            await Assert.That(keys).IsEmpty();
        }
        finally
        {
            wrapper.Dispose();
        }
    }

    /// <summary>Tests Flush operations.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldFlush()
    {
        var wrapper = CreateWrapper();
        try
        {
            wrapper.Flush().WaitForCompletion();
            wrapper.Flush(typeof(string)).WaitForCompletion();
        }
        finally
        {
            wrapper.Dispose();
        }

        await Task.CompletedTask;
    }

    /// <summary>Tests Vacuum operation.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldVacuum()
    {
        var wrapper = CreateWrapper();
        try
        {
            wrapper.Vacuum().WaitForCompletion();
        }
        finally
        {
            wrapper.Dispose();
        }

        await Task.CompletedTask;
    }

    /// <summary>Tests UpdateExpiration operations.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldUpdateExpiration()
    {
        var wrapper = CreateWrapper();
        try
        {
            byte[] firstPayload = [1];
            byte[] secondPayload = [2];
            wrapper.Insert("k1", firstPayload).WaitForCompletion();
            wrapper.Insert("k2", secondPayload, typeof(string)).WaitForCompletion();

            var future = TimeProvider.System.GetLocalNow().AddHours(1);
            wrapper.UpdateExpiration("k1", future).WaitForCompletion();
            wrapper.UpdateExpiration("k2", typeof(string), future).WaitForCompletion();
            wrapper.UpdateExpiration(["k1"], future).WaitForCompletion();
            wrapper.UpdateExpiration(["k2"], typeof(string), future).WaitForCompletion();
        }
        finally
        {
            wrapper.Dispose();
        }

        await Task.CompletedTask;
    }

    /// <summary>Tests Scheduler, Serializer, ForcedDateTimeKind, and InnerCache properties.</summary>
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

    /// <summary>Tests sync Dispose does not throw.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShouldDispose()
    {
        var wrapper = CreateWrapper();
        await Assert.That(() => wrapper.Dispose()).ThrowsNothing();
    }

    /// <summary>Tests SecureBlobCacheWrapper.Serializer throws when inner cache serializer is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializerShouldThrowWhenInnerSerializerIsNull()
    {
        FakeNullSerializerBlobCache fakeInner = new();
        var wrapper = new SecureBlobCacheWrapper(fakeInner);

        await Assert.That(() => _ = wrapper.Serializer)
            .Throws<InvalidOperationException>();
    }

    /// <summary>Tests SecureBlobCacheWrapper.Dispose disposes the inner cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeShouldDisposeInner()
    {
        InMemoryBlobCache inner = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        var wrapper = new SecureBlobCacheWrapper(inner);

        wrapper.Dispose();
        await Assert.That(wrapper).IsNotNull();
    }

    /// <summary>Tests SecureBlobCacheWrapper double Dispose is idempotent.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DoubleDisposeShouldNotThrow()
    {
        InMemoryBlobCache inner = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        var wrapper = new SecureBlobCacheWrapper(inner);

        wrapper.Dispose();

        // Double dispose should not throw
        wrapper.Dispose();
    }

    /// <summary>Creates a fresh <see cref="SecureBlobCacheWrapper"/> over an in-memory backing cache.</summary>
    /// <returns>A new wrapper instance.</returns>
    private static SecureBlobCacheWrapper CreateWrapper() =>
        new(new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer()));

    /// <summary>Fake IBlobCache with null Serializer to test the null guard in SecureBlobCacheWrapper.</summary>
    private sealed class FakeNullSerializerBlobCache : IBlobCache
    {
        /// <summary>Shared already-completed result: every mutating member of this fake does nothing.</summary>
        private static readonly IObservable<RxVoid> NoOpResult = Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public ISequencer Scheduler => ImmediateSequencer.Instance;

        /// <inheritdoc/>
        public ISerializer Serializer => null!;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs) =>
            Insert(keyValuePairs, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(string key, byte[] data) =>
            Insert(key, data, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type) =>
            Insert(keyValuePairs, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(string key, byte[] data, Type type) =>
            Insert(key, data, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]?> Get(string key) => Signal.Return<byte[]?>(null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => Signal.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]?> Get(string key, Type type) => Signal.Return<byte[]?>(null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => Signal.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => Signal.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<string> GetAllKeys() => Signal.Empty<string>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<string> GetAllKeys(Type type) => Signal.Empty<string>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => Signal.Empty<(string Key, DateTimeOffset? Time)>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => Signal.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => Signal.Empty<(string Key, DateTimeOffset? Time)>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => Signal.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Flush() => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Flush(Type type) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Invalidate(string key) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Invalidate(string key, Type type) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Invalidate(IEnumerable<string> keys) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Invalidate(IEnumerable<string> keys, Type type) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> InvalidateAll() => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> InvalidateAll(Type type) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Vacuum() => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) => NoOpResult;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) => NoOpResult;

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
