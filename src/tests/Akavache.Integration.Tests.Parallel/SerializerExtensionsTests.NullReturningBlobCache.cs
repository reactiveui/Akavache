// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>The null-returning cache stub used to drive the serializer extensions' empty-payload paths.</summary>
public partial class SerializerExtensionsTests
{
    /// <summary>
    /// A minimal IBlobCache implementation that returns null from Get(key, type)
    /// to exercise the null byte array guard in GetObject's Select lambda.
    /// </summary>
    private sealed class NullReturningBlobCache : IBlobCache
    {
        /// <summary>Initializes a new instance of the <see cref="NullReturningBlobCache"/> class.</summary>
        /// <param name="serializer">The serializer to use.</param>
        public NullReturningBlobCache(ISerializer serializer) => Serializer = serializer;

        /// <inheritdoc/>
        public ISerializer Serializer { get; }

        /// <inheritdoc/>
        public ISequencer Scheduler => ImmediateSequencer.Instance;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs) =>
            Insert(keyValuePairs, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(string key, byte[] data) =>
            Insert(key, data, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type) =>
            Insert(keyValuePairs, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            Type type,
            DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(string key, byte[] data, Type type) =>
            Insert(key, data, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Insert(
            string key,
            byte[] data,
            Type type,
            DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]?> Get(string key) =>
            Signal.Return<byte[]?>(null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) =>
            Signal.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<byte[]?> Get(string key, Type type) =>
            Signal.Return<byte[]?>(null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) =>
            Signal.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) =>
            Signal.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<string> GetAllKeys() =>
            Signal.Empty<string>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<string> GetAllKeys(Type type) =>
            Signal.Empty<string>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
            Signal.Empty<(string Key, DateTimeOffset? Time)>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) =>
            Signal.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
            Signal.Empty<(string Key, DateTimeOffset? Time)>();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) =>
            Signal.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Flush() =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Flush(Type type) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Invalidate(string key) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Invalidate(string key, Type type) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Invalidate(IEnumerable<string> keys) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Invalidate(IEnumerable<string> keys, Type type) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> InvalidateAll() =>
            Flush();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> InvalidateAll(Type type) =>
            Flush(type);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> Vacuum() =>
            InvalidateAll();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> UpdateExpiration(
            IEnumerable<string> keys,
            Type type,
            DateTimeOffset? absoluteExpiration) =>
            Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
