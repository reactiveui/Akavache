// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Integration.Tests;

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
        public IScheduler Scheduler => ImmediateScheduler.Instance;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs) =>
            Insert(keyValuePairs, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data) =>
            Insert(key, data, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type) =>
            Insert(keyValuePairs, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            Type type,
            DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, Type type) =>
            Insert(key, data, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            string key,
            byte[] data,
            Type type,
            DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) =>
            Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) =>
            Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) =>
            Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) =>
            Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) =>
            Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() =>
            Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) =>
            Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
            Observable.Empty<(string Key, DateTimeOffset? Time)>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) =>
            Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
            Observable.Empty<(string Key, DateTimeOffset? Time)>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) =>
            Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<Unit> Flush() =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Flush(Type type) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key, Type type) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll() =>
            Flush();

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll(Type type) =>
            Flush(type);

        /// <inheritdoc/>
        public IObservable<Unit> Vacuum() =>
            InvalidateAll();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(
            IEnumerable<string> keys,
            Type type,
            DateTimeOffset? absoluteExpiration) =>
            Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
