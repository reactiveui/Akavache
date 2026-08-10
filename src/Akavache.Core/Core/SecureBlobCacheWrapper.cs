// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Core;
#else
namespace Akavache.Core;
#endif

/// <summary>A wrapper that implements <see cref="ISecureBlobCache"/> by delegating to an <see cref="IBlobCache"/>.</summary>
/// <remarks>
/// This file is compiled into every assembly that needs it via
/// <c>&lt;Compile Include Link&gt;</c> — it is <see langword="internal"/> and
/// each consumer gets its own copy.
/// </remarks>
internal sealed class SecureBlobCacheWrapper : ISecureBlobCache, IWrappedBlobCache
{
    /// <summary>Tracks whether the wrapper has already been disposed. Transition is monotonic 0→1; the dispose gate uses <see cref="Interlocked.CompareExchange(ref int, int, int)"/>.</summary>
    private int _disposed;

    /// <summary>Initializes a new instance of the <see cref="SecureBlobCacheWrapper"/> class that wraps the specified <paramref name="inner"/> blob cache as a secure cache.</summary>
    /// <param name="inner">The underlying blob cache to delegate all operations to.</param>
    internal SecureBlobCacheWrapper(IBlobCache inner)
    {
        ArgumentExceptionHelper.ThrowIfNull(inner);
        InnerCache = inner;
    }

    /// <summary>Gets the underlying blob cache that this wrapper delegates to.</summary>
    public IBlobCache InnerCache { get; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind
    {
        get => InnerCache.ForcedDateTimeKind;
        set => InnerCache.ForcedDateTimeKind = value;
    }

    /// <inheritdoc/>
    public ISequencer Scheduler => InnerCache.Scheduler;

    /// <inheritdoc/>
    public ISerializer Serializer => InnerCache.Serializer ?? throw new InvalidOperationException("The inner cache's Serializer is null.");

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs) =>
        Insert(keyValuePairs, (DateTimeOffset?)null);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration) =>
        InnerCache.Insert(keyValuePairs, absoluteExpiration);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Insert(string key, byte[] data) =>
        Insert(key, data, (DateTimeOffset?)null);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration) =>
        InnerCache.Insert(key, data, absoluteExpiration);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type) =>
        Insert(keyValuePairs, type, (DateTimeOffset?)null);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration) =>
        InnerCache.Insert(keyValuePairs, type, absoluteExpiration);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Insert(string key, byte[] data, Type type) =>
        Insert(key, data, type, (DateTimeOffset?)null);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration) =>
        InnerCache.Insert(key, data, type, absoluteExpiration);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]?> Get(string key) => InnerCache.Get(key);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => InnerCache.Get(keys);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<byte[]?> Get(string key, Type type) => InnerCache.Get(key, type);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => InnerCache.Get(keys, type);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => InnerCache.GetAll(type);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<string> GetAllKeys() => InnerCache.GetAllKeys();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<string> GetAllKeys(Type type) => InnerCache.GetAllKeys(type);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => InnerCache.GetCreatedAt(keys);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<DateTimeOffset?> GetCreatedAt(string key) => InnerCache.GetCreatedAt(key);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => InnerCache.GetCreatedAt(keys, type);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => InnerCache.GetCreatedAt(key, type);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Flush() => InnerCache.Flush();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Flush(Type type) => InnerCache.Flush(type);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Invalidate(string key) => InnerCache.Invalidate(key);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Invalidate(string key, Type type) => InnerCache.Invalidate(key, type);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Invalidate(IEnumerable<string> keys) => InnerCache.Invalidate(keys);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Invalidate(IEnumerable<string> keys, Type type) => InnerCache.Invalidate(keys, type);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> InvalidateAll(Type type) => InnerCache.InvalidateAll(type);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> InvalidateAll() => InnerCache.InvalidateAll();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> Vacuum() => InnerCache.Vacuum();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(key, absoluteExpiration);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(key, type, absoluteExpiration);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(keys, absoluteExpiration);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(keys, type, absoluteExpiration);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        ((IDisposable)InnerCache).Dispose();
    }
}
