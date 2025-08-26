// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Splat;

namespace Akavache;

/// <summary>
/// Base class for in-memory blob cache implementations that provides common functionality
/// for all serialization-specific InMemoryBlobCache implementations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryBlobCacheBase"/> class.
/// </remarks>
/// <param name="scheduler">The scheduler to use for Observable based operations.</param>
/// <param name="serializer">The serializer to use for object serialization/deserialization.</param>
public abstract class InMemoryBlobCacheBase(IScheduler scheduler, ISerializer? serializer) : ISecureBlobCache
{
    private readonly Dictionary<string, CacheEntry> _cache = [];
    private readonly Dictionary<Type, HashSet<string>> _typeIndex = [];
    private readonly object _lock = new();
    private bool _disposed;
    private IHttpService? _httpService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBlobCacheBase"/> class with default scheduler.
    /// </summary>
    /// <param name="serializer">The serializer to use for object serialization/deserialization.</param>
    protected InMemoryBlobCacheBase(ISerializer? serializer)
        : this(CacheDatabase.TaskpoolScheduler, serializer)
    {
    }

    /// <inheritdoc />
    public IScheduler Scheduler { get; } = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

    /// <inheritdoc/>
    public ISerializer Serializer { get; } = serializer ?? throw new ArgumentNullException(nameof(serializer));

    /// <inheritdoc/>
    public IHttpService HttpService { get => _httpService ??= new HttpService(); set => _httpService = value; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind
    {
        get => Serializer.ForcedDateTimeKind;
        set
        {
            Serializer.ForcedDateTimeKind = value;

            // Also update the global serializer to ensure extension methods use the same setting
            // This ensures GetOrFetchObject and other extension methods respect the cache's DateTime handling
            var serialzer = AppLocator.Current.GetService<ISerializer>();
            serializer?.ForcedDateTimeKind = value;
        }
    }

    /// <inheritdoc />
    public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(GetType().Name);
        }

        if (keyValuePairs is null)
        {
            throw new ArgumentNullException(nameof(keyValuePairs));
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                foreach (var pair in keyValuePairs)
                {
                    _cache[pair.Key] = new CacheEntry
                    {
                        Id = pair.Key,
                        Value = pair.Value,
                        CreatedAt = Scheduler.Now,
                        ExpiresAt = absoluteExpiration
                    };
                }
            }

            return Unit.Default;
        },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                _cache[key] = new CacheEntry
                {
                    Id = key,
                    Value = data,
                    CreatedAt = Scheduler.Now,
                    ExpiresAt = absoluteExpiration
                };
            }

            return Unit.Default;
        },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                if (!_typeIndex.TryGetValue(type, out var value))
                {
                    value = [];
                    _typeIndex[type] = value;
                }

                foreach (var pair in keyValuePairs)
                {
                    _cache[pair.Key] = new CacheEntry
                    {
                        Id = pair.Key,
                        TypeName = type.FullName,
                        Value = pair.Value,
                        CreatedAt = Scheduler.Now,
                        ExpiresAt = absoluteExpiration
                    };
                    value.Add(pair.Key);
                }
            }

            return Unit.Default;
        },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                if (!_typeIndex.TryGetValue(type, out var value))
                {
                    value = [];
                    _typeIndex[type] = value;
                }

                _cache[key] = new CacheEntry
                {
                    Id = key,
                    TypeName = type.FullName,
                    Value = data,
                    CreatedAt = Scheduler.Now,
                    ExpiresAt = absoluteExpiration
                };
                value.Add(key);
            }

            return Unit.Default;
        },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<byte[]?> Get(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]?>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                if (!_cache.TryGetValue(key, out var entry))
                {
                    throw new KeyNotFoundException($"The given key '{key}' was not present in the cache.");
                }

                // Check if the entry has expired
                if (entry.ExpiresAt <= Scheduler.Now)
                {
                    _cache.Remove(key);

                    // Also remove from type indexes
                    foreach (var typeKeys in _typeIndex.Values)
                    {
                        typeKeys.Remove(key);
                    }

                    throw new KeyNotFoundException($"The given key '{key}' was not present in the cache.");
                }

                return entry.Value;
            }
        },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(GetType().Name);
        }

        return keys.ToObservable()
            .SelectMany(key => Get(key).Select(value => new KeyValuePair<string, byte[]>(key, value!))
                .Catch<KeyValuePair<string, byte[]>, KeyNotFoundException>(_ => Observable.Empty<KeyValuePair<string, byte[]>>()));
    }

    /// <inheritdoc />
    public IObservable<byte[]?> Get(string key, Type type) => Get(key);

    /// <inheritdoc />
    public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => Get(keys);

    /// <inheritdoc />
    public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                if (!_typeIndex.TryGetValue(type, out var keys))
                {
                    return Enumerable.Empty<KeyValuePair<string, byte[]>>();
                }

                var now = Scheduler.Now;
                var result = new List<KeyValuePair<string, byte[]>>();
                var expiredKeys = new List<string>();

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
                            result.Add(new KeyValuePair<string, byte[]>(key, entry.Value!));
                        }
                    }
                }

                // Clean up expired keys
                foreach (var expiredKey in expiredKeys)
                {
                    _cache.Remove(expiredKey);
                    keys.Remove(expiredKey);
                }

                return result;
            }
        },
            Scheduler).SelectMany(x => x);
    }

    /// <inheritdoc />
    public IObservable<string> GetAllKeys()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                var now = Scheduler.Now;
                var expiredKeys = new List<string>();
                var validKeys = new List<string>();

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
                    _cache.Remove(expiredKey);
                }

                return validKeys;
            }
        },
            Scheduler).SelectMany(x => x);
    }

    /// <inheritdoc />
    public IObservable<string> GetAllKeys(Type type)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                if (!_typeIndex.TryGetValue(type, out var keys))
                {
                    return Enumerable.Empty<string>();
                }

                var now = Scheduler.Now;
                var expiredKeys = new List<string>();
                var validKeys = new List<string>();

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
                    _cache.Remove(expiredKey);
                    keys.Remove(expiredKey);
                }

                return validKeys;
            }
        },
            Scheduler).SelectMany(x => x);
    }

    /// <inheritdoc />
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<(string Key, DateTimeOffset? Time)>(GetType().Name);
        }

        return keys.ToObservable()
            .Select(key =>
            {
                lock (_lock)
                {
                    return _cache.TryGetValue(key, out var entry) ? (key, (DateTimeOffset?)entry.CreatedAt) : (key, null);
                }
            });
    }

    /// <inheritdoc />
    public IObservable<DateTimeOffset?> GetCreatedAt(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<DateTimeOffset?>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                return _cache.TryGetValue(key, out var entry) ? (DateTimeOffset?)entry.CreatedAt : null;
            }
        },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
        GetCreatedAt(keys);

    /// <inheritdoc />
    public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => GetCreatedAt(key);

    /// <inheritdoc />
    public IObservable<Unit> Flush() => Observable.Return(Unit.Default);

    /// <inheritdoc />
    public IObservable<Unit> Flush(Type type) => Observable.Return(Unit.Default);

    /// <inheritdoc />
    public IObservable<Unit> Invalidate(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                _cache.Remove(key);

                // Remove from type indexes
                foreach (var typeKeys in _typeIndex.Values)
                {
                    typeKeys.Remove(key);
                }
            }

            return Unit.Default;
        },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<Unit> Invalidate(string key, Type type) => Invalidate(key);

    /// <inheritdoc />
    public IObservable<Unit> Invalidate(IEnumerable<string> keys)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                foreach (var key in keys)
                {
                    _cache.Remove(key);

                    // Remove from type indexes
                    foreach (var typeKeys in _typeIndex.Values)
                    {
                        typeKeys.Remove(key);
                    }
                }
            }

            return Unit.Default;
        },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<Unit> InvalidateAll(Type type)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                if (_typeIndex.TryGetValue(type, out var keys))
                {
                    foreach (var key in keys)
                    {
                        _cache.Remove(key);
                    }

                    keys.Clear();
                }
            }

            return Unit.Default;
        },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => Invalidate(keys);

    /// <inheritdoc />
    public IObservable<Unit> InvalidateAll()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                _cache.Clear();
                _typeIndex.Clear();
            }

            return Unit.Default;
        },
            Scheduler);
    }

    /// <inheritdoc />
    public IObservable<Unit> Vacuum()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(GetType().Name);
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                var now = Scheduler.Now;
                var expiredKeys = new List<string>();

                foreach (var kvp in _cache)
                {
                    if (kvp.Value.ExpiresAt <= now)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var expiredKey in expiredKeys)
                {
                    _cache.Remove(expiredKey);

                    // Remove from type indexes
                    foreach (var typeKeys in _typeIndex.Values)
                    {
                        typeKeys.Remove(expiredKey);
                    }
                }
            }

            return Unit.Default;
        },
            Scheduler);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Dispose already calls SuppressFinalize")]
    public async ValueTask DisposeAsync() => await Task.Run(() => Dispose());

    /// <summary>
    /// Insert an object into the cache using the configured serializer.
    /// </summary>
    /// <typeparam name="T">The type of object to insert.</typeparam>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="value">The object to serialize.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using InsertObject requires types to be preserved for serialization")]
    [RequiresDynamicCode("Using InsertObject requires types to be preserved for serialization")]
#endif
    public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(GetType().Name);
        }

        var data = Serializer.Serialize(value);
        return Insert(key, data, typeof(T), absoluteExpiration);
    }

    /// <summary>
    /// Get an object from the cache and deserialize it using the configured serializer.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <param name="key">The key to look up in the cache.</param>
    /// <returns>A Future result representing the object in the cache.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetObject requires types to be preserved for deserialization")]
    [RequiresDynamicCode("Using GetObject requires types to be preserved for deserialization")]
#endif
    public IObservable<T?> GetObject<T>(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<T?>(GetType().Name);
        }

        return Get(key, typeof(T))
            .Select(data => data == null ? default : Serializer.Deserialize<T>(data));
    }

    /// <summary>
    /// Return all objects of a specific Type in the cache.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <returns>A Future result representing all objects in the cache with the specified Type.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using GetAllObjects requires types to be preserved for deserialization")]
    [RequiresDynamicCode("Using GetAllObjects requires types to be preserved for deserialization")]
#endif
    public IObservable<IEnumerable<T>> GetAllObjects<T>()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<IEnumerable<T>>(GetType().Name);
        }

        return GetAll(typeof(T))
            .Select(kvp => Serializer.Deserialize<T>(kvp.Value))
            .Where(obj => obj is not null)
            .Select(obj => obj!)
            .ToList()
            .Select(list => (IEnumerable<T>)list);
    }

    /// <summary>
    /// Returns the time that the object with the key was added to the cache, or returns
    /// null if the key isn't in the cache.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="key">The key to return the date for.</param>
    /// <returns>The date the key was created on.</returns>
    public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key) => GetCreatedAt(key, typeof(T));

    /// <summary>
    /// Invalidates a single object from the cache.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    public IObservable<Unit> InvalidateObject<T>(string key) => Invalidate(key, typeof(T));

    /// <summary>
    /// Invalidates all objects of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    public IObservable<Unit> InvalidateAllObjects<T>() => InvalidateAll(typeof(T));

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="InMemoryBlobCacheBase"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    _cache.Clear();
                    _typeIndex.Clear();
                }
            }

            _disposed = true;
        }
    }
}
