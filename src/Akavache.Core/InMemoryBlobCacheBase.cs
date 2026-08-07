// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using Splat;

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>
/// Base class for in-memory blob cache implementations that provides common functionality
/// for all serialization-specific InMemoryBlobCache implementations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryBlobCacheBase"/> class.
/// </remarks>
/// <param name="scheduler">The scheduler to use for Observable based operations.</param>
/// <param name="serializer">The serializer to use for object serialization/deserialization.</param>
public class InMemoryBlobCacheBase(ISequencer scheduler, ISerializer? serializer) : ISecureBlobCache
{
    /// <summary>The in-memory key to cache entry mapping.</summary>
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    /// <summary>Per-type index of keys for fast type-scoped lookups.</summary>
    private readonly Dictionary<Type, HashSet<string>> _typeIndex = [];

    /// <summary>Reverse map from cache key to the <see cref="Type"/> bucket it currently lives in.</summary>
    private readonly Dictionary<string, Type> _keyToType = new(StringComparer.Ordinal);

    /// <summary>Synchronization primitive guarding mutations.</summary>
    private readonly Lock _lock = new();

    /// <summary>Tracks whether the instance has been disposed.</summary>
    private int _disposed;

    /// <inheritdoc />
    public ISequencer Scheduler { get; } = ArgumentValidation.EnsureNotNull(scheduler);

    /// <inheritdoc/>
    public ISerializer Serializer { get; } = ArgumentValidation.EnsureNotNull(serializer);

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind
    {
        get => Serializer.ForcedDateTimeKind;
        set
        {
            Serializer.ForcedDateTimeKind = value;

            // Also update the global serializer to ensure extension methods use the same setting
            // This ensures GetOrFetchObject and other extension methods respect the cache's DateTime handling
            var serializer = AppLocator.Current.GetService<ISerializer>();

            serializer?.ForcedDateTimeKind = value;
        }
    }

    /// <inheritdoc />
    public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs) =>
        Insert(keyValuePairs, (DateTimeOffset?)null);

    /// <inheritdoc />
    public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration)
    {
        ArgumentExceptionHelper.ThrowIfNull(keyValuePairs);
        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name);
        }

        // Empty-input guard.
        return keyValuePairs is ICollection<KeyValuePair<string, byte[]>> { Count: 0 } ? ImmutableReturnRxVoidSignal.Instance : Signal.Start(
            () =>
            {
                lock (_lock)
                {
                    var now = Scheduler.Now;
                    foreach (var pair in keyValuePairs)
                    {
                        _cache[pair.Key] = new(pair.Key, TypeName: null, pair.Value, now, absoluteExpiration);
                    }
                }

                return RxVoid.Default;
            },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<RxVoid> Insert(string key, byte[] data) =>
        Insert(key, data, (DateTimeOffset?)null);

    /// <inheritdoc />
    public IObservable<RxVoid> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        _cache[key] = new(key, TypeName: null, data, Scheduler.Now, absoluteExpiration);
                    }

                    return RxVoid.Default;
                },
                Scheduler);

    /// <inheritdoc />
    public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type) =>
        Insert(keyValuePairs, type, (DateTimeOffset?)null);

    /// <inheritdoc />
    public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name);
        }

        // Empty-input guard.
        return keyValuePairs is ICollection<KeyValuePair<string, byte[]>> { Count: 0 } ? ImmutableReturnRxVoidSignal.Instance : Signal.Start(
            () =>
            {
                lock (_lock)
                {
#if NET6_0_OR_GREATER
                    ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndex, type, out _);
                    value ??= new(StringComparer.Ordinal);
#else
                    if (!_typeIndex.TryGetValue(type, out var value))
                    {
                        value = new(StringComparer.Ordinal);
                        _typeIndex[type] = value;
                    }
#endif

                    var typeFullName = type.FullName;
                    var now = Scheduler.Now;
                    foreach (var pair in keyValuePairs)
                    {
                        // Evict from a previous type bucket.
                        if (_keyToType.TryGetValue(pair.Key, out var previousType) && previousType != type
                            && _typeIndex.TryGetValue(previousType, out var previousSet))
                        {
                            _ = previousSet.Remove(pair.Key);
                        }

                        _cache[pair.Key] = new(pair.Key, typeFullName, pair.Value, now, absoluteExpiration);
                        _ = value.Add(pair.Key);
                        _keyToType[pair.Key] = type;
                    }
                }

                return RxVoid.Default;
            },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<RxVoid> Insert(string key, byte[] data, Type type) =>
        Insert(key, data, type, (DateTimeOffset?)null);

    /// <inheritdoc />
    public IObservable<RxVoid> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
#if NET6_0_OR_GREATER
                        ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndex, type, out _);
                        value ??= new(StringComparer.Ordinal);
#else
                        if (!_typeIndex.TryGetValue(type, out var value))
                        {
                            value = new(StringComparer.Ordinal);
                            _typeIndex[type] = value;
                        }
#endif

                        // Evict from a previous type bucket so the one-type-per-key invariant holds.
                        if (_keyToType.TryGetValue(key, out var previousType) && previousType != type
                            && _typeIndex.TryGetValue(previousType, out var previousSet))
                        {
                            _ = previousSet.Remove(key);
                        }

                        _cache[key] = new(key, type.FullName, data, Scheduler.Now, absoluteExpiration);
                        _ = value.Add(key);
                        _keyToType[key] = type;
                    }

                    return RxVoid.Default;
                },
                Scheduler);

    /// <inheritdoc />
    public IObservable<byte[]?> Get(string key) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]?>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        if (!_cache.TryGetValue(key, out var entry))
                        {
                            throw new KeyNotFoundException($"The given key '{key}' was not present in the cache.");
                        }

                        // Check expiration.
                        if (entry.ExpiresAt <= Scheduler.Now)
                        {
                            _ = _cache.Remove(key);

                            // Remove from type indexes.
                            foreach (var kvp in _typeIndex)
                            {
                                _ = kvp.Value.Remove(key);
                            }

                            throw new KeyNotFoundException($"The given key '{key}' was not present in the cache.");
                        }

                        return entry.Value;
                    }
                },
                Scheduler);

    /// <inheritdoc />
    public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(GetType().Name)
            : keys.ToObservable()
                .SelectMany(key => Get(key)
                    .Select(value => new KeyValuePair<string, byte[]>(key, value!))
                    .Catch<KeyValuePair<string, byte[]>, KeyNotFoundException>(static _ => ImmutableEmptySignal<KeyValuePair<string, byte[]>>.Instance));

    /// <inheritdoc />
    public IObservable<byte[]?> Get(string key, Type type) => Get(key);

    /// <inheritdoc />
    public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => Get(keys);

    /// <inheritdoc />
    public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        if (!_typeIndex.TryGetValue(type, out var keys))
                        {
                            return [];
                        }

                        var now = Scheduler.Now;
                        List<KeyValuePair<string, byte[]>> result = new(keys.Count);
                        List<string> expiredKeys = new(keys.Count);

                        foreach (var key in keys)
                        {
                            if (_cache.TryGetValue(key, out var entry))
                            {
                                if (entry.ExpiresAt <= now)
                                {
                                    expiredKeys.Add(key);
                                }
                                else
                                {
                                    result.Add(new(key, entry.Value!));
                                }
                            }
                        }

                        // Clean up expired keys
                        foreach (var expiredKey in expiredKeys)
                        {
                            _ = _cache.Remove(expiredKey);
                            _ = keys.Remove(expiredKey);
                        }

                        return result;
                    }
                },
                Scheduler).SelectMany(static x => x);

    /// <inheritdoc />
    public IObservable<string> GetAllKeys() =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        var now = Scheduler.Now;
                        List<string> expiredKeys = new(_cache.Count);
                        List<string> validKeys = new(_cache.Count);

                        foreach (var kvp in _cache)
                        {
                            if (kvp.Value.ExpiresAt <= now)
                            {
                                expiredKeys.Add(kvp.Key);
                            }
                            else
                            {
                                validKeys.Add(kvp.Key);
                            }
                        }

                        // Clean up expired keys
                        foreach (var expiredKey in expiredKeys)
                        {
                            _ = _cache.Remove(expiredKey);
                        }

                        return validKeys;
                    }
                },
                Scheduler).SelectMany(static x => x);

    /// <inheritdoc />
    public IObservable<string> GetAllKeys(Type type) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        if (!_typeIndex.TryGetValue(type, out var keys))
                        {
                            return [];
                        }

                        var now = Scheduler.Now;
                        List<string> expiredKeys = new(keys.Count);
                        List<string> validKeys = new(keys.Count);

                        foreach (var key in keys)
                        {
                            if (_cache.TryGetValue(key, out var entry))
                            {
                                if (entry.ExpiresAt <= now)
                                {
                                    expiredKeys.Add(key);
                                }
                                else
                                {
                                    validKeys.Add(key);
                                }
                            }
                        }

                        // Clean up expired keys
                        foreach (var expiredKey in expiredKeys)
                        {
                            _ = _cache.Remove(expiredKey);
                            _ = keys.Remove(expiredKey);
                        }

                        return validKeys;
                    }
                },
                Scheduler).SelectMany(static x => x);

    /// <inheritdoc />
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<(string Key, DateTimeOffset? Time)>(GetType().Name)
            : keys.ToObservable()
                .Select(key =>
                {
                    lock (_lock)
                    {
                        return _cache.TryGetValue(key, out var entry)
                            ? (key, (DateTimeOffset?)entry.CreatedAt)
                            : (key, null);
                    }
                });

    /// <inheritdoc />
    public IObservable<DateTimeOffset?> GetCreatedAt(string key) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<DateTimeOffset?>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        return _cache.TryGetValue(key, out var entry) ? (DateTimeOffset?)entry.CreatedAt : null;
                    }
                },
                Scheduler);

    /// <inheritdoc />
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
        GetCreatedAt(keys);

    /// <inheritdoc />
    public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => GetCreatedAt(key);

    /// <inheritdoc />
    public IObservable<RxVoid> Flush() => ImmutableReturnRxVoidSignal.Instance;

    /// <inheritdoc />
    public IObservable<RxVoid> Flush(Type type) => ImmutableReturnRxVoidSignal.Instance;

    /// <inheritdoc />
    public IObservable<RxVoid> Invalidate(string key) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        _ = _cache.Remove(key);
                        RemoveKeyFromTypeIndexFast(_typeIndex, _keyToType, key);
                    }

                    // Clear pending requests for this key.
                    RequestCache.RemoveRequestsForKey(key);

                    return RxVoid.Default;
                },
                Scheduler);

    /// <inheritdoc />
    public IObservable<RxVoid> Invalidate(string key, Type type) => Invalidate(key);

    /// <inheritdoc />
    public IObservable<RxVoid> Invalidate(IEnumerable<string> keys)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name);
        }

        // Empty-input guard — skip the Observable.Start scheduling and lock acquisition entirely.
        return keys is ICollection<string> { Count: 0 } ? ImmutableReturnRxVoidSignal.Instance : Signal.Start(
            () =>
            {
                // Materialize the enumerable. The spread pre-sizes from an ICollection source.
                List<string> keysToInvalidate = [.. keys];

                lock (_lock)
                {
                    foreach (var key in keysToInvalidate)
                    {
                        _ = _cache.Remove(key);
                        RemoveKeyFromTypeIndexFast(_typeIndex, _keyToType, key);
                    }
                }

                // Clear pending requests for these keys.
                foreach (var key in keysToInvalidate)
                {
                    RequestCache.RemoveRequestsForKey(key);
                }

                return RxVoid.Default;
            },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<RxVoid> Invalidate(IEnumerable<string> keys, Type type) => Invalidate(keys);

    /// <inheritdoc />
    public IObservable<RxVoid> InvalidateAll(Type type) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    List<string> keysToInvalidate = [];

                    lock (_lock)
                    {
                        if (_typeIndex.TryGetValue(type, out var keys))
                        {
                            // Capture keys before clearing. Spread pre-sizes from the ICollection source.
                            keysToInvalidate = [.. keys];

                            foreach (var key in keys)
                            {
                                _ = _cache.Remove(key);
                                _ = _keyToType.Remove(key);
                            }

                            keys.Clear();
                        }
                    }

                    // Clear pending requests.
                    foreach (var key in keysToInvalidate)
                    {
                        RequestCache.RemoveRequest(key, type);
                    }

                    return RxVoid.Default;
                },
                Scheduler);

    /// <inheritdoc />
    public IObservable<RxVoid> InvalidateAll() =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        _cache.Clear();
                        _typeIndex.Clear();
                        _keyToType.Clear();
                    }

                    // Clear all pending requests.
                    RequestCache.Clear();

                    return RxVoid.Default;
                },
                Scheduler);

    /// <inheritdoc />
    public IObservable<RxVoid> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
        (string.IsNullOrWhiteSpace(key), Volatile.Read(ref _disposed) != 0) switch
        {
            (true, _) => new ImmediateThrowSignal<RxVoid>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key))),
            (_, true) => IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name),
            _ => Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        if (_cache.TryGetValue(key, out var entry))
                        {
                            _cache[key] = entry with { ExpiresAt = absoluteExpiration };
                        }
                    }

                    return RxVoid.Default;
                },
                Scheduler),
        };

    /// <inheritdoc />
    public IObservable<RxVoid> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
        (string.IsNullOrWhiteSpace(key), type is null, Volatile.Read(ref _disposed) != 0) switch
        {
            (true, _, _) => new ImmediateThrowSignal<RxVoid>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key))),
            (_, true, _) => new ImmediateThrowSignal<RxVoid>(new ArgumentNullException(nameof(type))),
            (_, _, true) => IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name),
            _ => Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        if (_cache.TryGetValue(key, out var entry) && entry.TypeName == type!.FullName)
                        {
                            _cache[key] = entry with { ExpiresAt = absoluteExpiration };
                        }
                    }

                    return RxVoid.Default;
                },
                Scheduler),
        };

    /// <inheritdoc />
    public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
        (keys is null, Volatile.Read(ref _disposed) != 0) switch
        {
            (true, _) => new ImmediateThrowSignal<RxVoid>(new ArgumentNullException(nameof(keys))),
            (_, true) => IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name),
            _ => Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        foreach (var key in keys!)
                        {
                            if (_cache.TryGetValue(key, out var entry))
                            {
                                _cache[key] = entry with { ExpiresAt = absoluteExpiration };
                            }
                        }
                    }

                    return RxVoid.Default;
                },
                Scheduler),
        };

    /// <inheritdoc />
    public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) =>
        (keys is null, type is null, Volatile.Read(ref _disposed) != 0) switch
        {
            (true, _, _) => new ImmediateThrowSignal<RxVoid>(new ArgumentNullException(nameof(keys))),
            (_, true, _) => new ImmediateThrowSignal<RxVoid>(new ArgumentNullException(nameof(type))),
            (_, _, true) => IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name),
            _ => Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        foreach (var key in keys!)
                        {
                            if (_cache.TryGetValue(key, out var entry) && entry.TypeName == type!.FullName)
                            {
                                _cache[key] = entry with { ExpiresAt = absoluteExpiration };
                            }
                        }
                    }

                    return RxVoid.Default;
                },
                Scheduler),
        };

    /// <inheritdoc />
    public IObservable<RxVoid> Vacuum() =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name)
            : Signal.Start(
                () =>
                {
                    lock (_lock)
                    {
                        VacuumExpiredEntriesFast(_cache, _typeIndex, _keyToType, Scheduler.Now);
                    }

                    return RxVoid.Default;
                },
                Scheduler);

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Insert an object into the cache using the configured serializer.</summary>
    /// <typeparam name="T">The type of object to insert.</typeparam>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
    [RequiresUnreferencedCode("Using InsertObject requires types to be preserved for serialization")]
    [RequiresDynamicCode("Using InsertObject requires types to be preserved for serialization")]
    public IObservable<RxVoid> InsertObject<T>(string key, T value) =>
        InsertObject(key, value, (DateTimeOffset?)null);

    /// <summary>Insert an object into the cache using the configured serializer.</summary>
    /// <typeparam name="T">The type of object to insert.</typeparam>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="value">The object to serialize.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
    [RequiresUnreferencedCode("Using InsertObject requires types to be preserved for serialization")]
    [RequiresDynamicCode("Using InsertObject requires types to be preserved for serialization")]
    public IObservable<RxVoid> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<RxVoid>(GetType().Name)
            : Insert(key, Serializer.Serialize(value), typeof(T), absoluteExpiration);

    /// <summary>Get an object from the cache and deserialize it using the configured serializer.</summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <param name="key">The key to look up in the cache.</param>
    /// <returns>A Future result representing the object in the cache.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    [RequiresUnreferencedCode("Using GetObject requires types to be preserved for deserialization")]
    [RequiresDynamicCode("Using GetObject requires types to be preserved for deserialization")]
    public IObservable<T?> GetObject<T>(string key) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<T?>(GetType().Name)
            : Get(key, typeof(T))
                .Select(data => data is null ? default : Serializer.Deserialize<T>(data));

    /// <summary>Return all objects of a specific Type in the cache.</summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <returns>A Future result representing all objects in the cache with the specified Type.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    [RequiresUnreferencedCode("Using GetAllObjects requires types to be preserved for deserialization")]
    [RequiresDynamicCode("Using GetAllObjects requires types to be preserved for deserialization")]
    public IObservable<IEnumerable<T>> GetAllObjects<T>() =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<IEnumerable<T>>(GetType().Name)
            : GetAll(typeof(T))
                .TrySelect(kvp => Serializer.Deserialize<T>(kvp.Value))
                .ToList()
                .Select(static list => (IEnumerable<T>)list);

    /// <summary>Returns the time that the object with the key was added to the cache, or returns null if the key isn't in the cache.</summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="key">The key to return the date for.</param>
    /// <returns>The date the key was created on.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key) => GetCreatedAt(key, typeof(T));

    /// <summary>Invalidates a single object from the cache.</summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    public IObservable<RxVoid> InvalidateObject<T>(string key) => Invalidate(key, typeof(T));

    /// <summary>Invalidates all objects of the specified type.</summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    public IObservable<RxVoid> InvalidateAllObjects<T>() => InvalidateAll(typeof(T));

    /// <summary>Removes any entries from <paramref name="cache"/> whose <c>ExpiresAt</c> is at or before <paramref name="now"/>.</summary>
    /// <param name="cache">The key-to-entry dictionary to vacuum.</param>
    /// <param name="typeIndex">The per-type key index to prune.</param>
    /// <param name="now">The current time used to determine expiration.</param>
    internal static void VacuumExpiredEntries(
        Dictionary<string, CacheEntry> cache,
        Dictionary<Type, HashSet<string>> typeIndex,
        DateTimeOffset now)
    {
        foreach (var expiredKey in CollectExpiredKeys(cache, now))
        {
            _ = cache.Remove(expiredKey);
            RemoveKeyFromAllTypeIndexes(typeIndex, expiredKey);
        }
    }

    /// <summary>O(1) vacuum that uses <paramref name="keyToType"/> for type-index removal.</summary>
    /// <param name="cache">The key-to-entry dictionary to vacuum.</param>
    /// <param name="typeIndex">The per-type key index to prune.</param>
    /// <param name="keyToType">Reverse key-to-type map.</param>
    /// <param name="now">The cutoff time.</param>
    internal static void VacuumExpiredEntriesFast(
        Dictionary<string, CacheEntry> cache,
        Dictionary<Type, HashSet<string>> typeIndex,
        Dictionary<string, Type> keyToType,
        DateTimeOffset now)
    {
        foreach (var expiredKey in CollectExpiredKeys(cache, now))
        {
            _ = cache.Remove(expiredKey);
            RemoveKeyFromTypeIndexFast(typeIndex, keyToType, expiredKey);
        }
    }

    /// <summary>Removes <paramref name="key"/> from whichever type's set it currently lives in.</summary>
    /// <param name="typeIndex">The per-type key index being pruned.</param>
    /// <param name="keyToType">Reverse key-to-type map.</param>
    /// <param name="key">The cache key being removed.</param>
    internal static void RemoveKeyFromTypeIndexFast(
        Dictionary<Type, HashSet<string>> typeIndex,
        Dictionary<string, Type> keyToType,
        string key)
    {
        if (!keyToType.TryGetValue(key, out var type))
        {
            return;
        }

        if (typeIndex.TryGetValue(type, out var set))
        {
            _ = set.Remove(key);
        }

        _ = keyToType.Remove(key);
    }

    /// <summary>Returns the list of keys in <paramref name="cache"/> whose <c>ExpiresAt</c> is at or before <paramref name="now"/>.</summary>
    /// <param name="cache">The cache dictionary to scan.</param>
    /// <param name="now">The cutoff time.</param>
    /// <returns>A list of expired keys.</returns>
    internal static List<string> CollectExpiredKeys(
        Dictionary<string, CacheEntry> cache,
        DateTimeOffset now)
    {
        List<string> expiredKeys = new(cache.Count);
        foreach (var kvp in cache)
        {
            if (kvp.Value.ExpiresAt <= now)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        return expiredKeys;
    }

    /// <summary>Removes <paramref name="key"/> from every set in <paramref name="typeIndex"/>.</summary>
    /// <param name="typeIndex">The per-type key index to prune.</param>
    /// <param name="key">The key to remove from each type's set.</param>
    internal static void RemoveKeyFromAllTypeIndexes(
        Dictionary<Type, HashSet<string>> typeIndex,
        string key)
    {
        foreach (var kvp in typeIndex)
        {
            _ = kvp.Value.Remove(key);
        }
    }

    /// <summary>Releases the resources used by the <see cref="InMemoryBlobCacheBase"/>.</summary>
    /// <param name="disposing">true to release both managed and unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!DisposeHelper.TryClaimDispose(disposing, ref _disposed))
        {
            return;
        }

        lock (_lock)
        {
            _cache.Clear();
            _typeIndex.Clear();
            _keyToType.Clear();
        }
    }
}
