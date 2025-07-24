// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveMarbles.CacheDatabase.Core;

/// <summary>
/// This class is an IBlobCache backed by a simple in-memory Dictionary.
/// Use it for testing / mocking purposes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
/// </remarks>
/// <param name="scheduler">The scheduler to use for Observable based operations.</param>
public sealed class InMemoryBlobCache(IScheduler scheduler) : IBlobCache, ISecureBlobCache
{
    private readonly Dictionary<string, CacheEntry> _cache = [];
    private readonly Dictionary<Type, HashSet<string>> _typeIndex = [];
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
    /// </summary>
    public InMemoryBlobCache()
        : this(CoreRegistrations.TaskpoolScheduler)
    {
    }

    /// <inheritdoc />
    public IScheduler Scheduler { get; } = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

    /// <inheritdoc />
    public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        return Observable.Start(
            () =>
        {
            lock (_lock)
            {
                _cache[key] = new CacheEntry
                {
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]?>(nameof(InMemoryBlobCache));
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

                if (entry.ExpiresAt < Scheduler.Now)
                {
                    _cache.Remove(key);
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(nameof(InMemoryBlobCache));
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(nameof(InMemoryBlobCache));
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
                        if (entry.ExpiresAt < now)
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(nameof(InMemoryBlobCache));
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
                    if (kvp.Value.ExpiresAt < now)
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(nameof(InMemoryBlobCache));
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
                        if (entry.ExpiresAt < now)
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<(string Key, DateTimeOffset? Time)>(nameof(InMemoryBlobCache));
        }

        return keys.ToObservable()
            .Select(key =>
            {
                lock (_lock)
                {
                    return _cache.TryGetValue(key, out var entry) ? (key, (DateTimeOffset?)entry.CreatedAt) : (key, (DateTimeOffset?)null);
                }
            });
    }

    /// <inheritdoc />
    public IObservable<DateTimeOffset?> GetCreatedAt(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<DateTimeOffset?>(nameof(InMemoryBlobCache));
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
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
                    if (kvp.Value.ExpiresAt < now)
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
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _cache.Clear();
            _typeIndex.Clear();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await Task.Run(() => Dispose());

    private class CacheEntry
    {
        public byte[]? Value { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }
    }
}
