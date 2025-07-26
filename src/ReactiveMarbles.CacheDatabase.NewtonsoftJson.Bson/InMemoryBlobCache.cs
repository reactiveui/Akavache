// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using ReactiveMarbles.CacheDatabase.Core;
using Splat;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// This class is an IBlobCache backed by a simple in-memory Dictionary.
/// Use it for testing / mocking purposes with BSON serialization.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
/// </remarks>
/// <param name="scheduler">The scheduler to use for Observable based operations.</param>
#if NET8_0_OR_GREATER
[RequiresUnreferencedCode("Registrations for ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson")]
[RequiresDynamicCode("Registrations for ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson")]
#endif
public sealed class InMemoryBlobCache(IScheduler scheduler) : IBlobCache, ISecureBlobCache, IEnableLogger
{
    private readonly Dictionary<string, CacheEntry> _cache = [];
    private readonly Dictionary<Type, HashSet<string>> _typeIndex = [];
    private readonly object _lock = new();
    private readonly JsonDateTimeContractResolver _jsonDateTimeContractResolver = new(); // This will make us use ticks instead of json ticks for DateTime.
    private bool _disposed;
    private DateTimeKind? _dateTimeKind;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
    /// </summary>
    public InMemoryBlobCache()
        : this(CoreRegistrations.TaskpoolScheduler)
    {
    }

    /// <inheritdoc />
    public IScheduler Scheduler { get; } = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind
    {
        get => _dateTimeKind ?? CacheDatabase.ForcedDateTimeKind;
        set
        {
            _dateTimeKind = value;
            _jsonDateTimeContractResolver?.ForceDateTimeKindOverride = value;
        }
    }

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

    /// <summary>
    /// Insert an object into the cache using BSON serialization.
    /// </summary>
    /// <typeparam name="T">The type of object to insert.</typeparam>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="value">The object to serialize.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
    public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        var data = SerializeObjectToBson(value);
        return Insert(key, data, typeof(T), absoluteExpiration);
    }

    /// <summary>
    /// Get an object from the cache and deserialize it using BSON serialization.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <param name="key">The key to look up in the cache.</param>
    /// <returns>A Future result representing the object in the cache.</returns>
    public IObservable<T?> GetObject<T>(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<T?>(nameof(InMemoryBlobCache));
        }

        return Get(key, typeof(T))
            .Select(data => data == null ? default : DeserializeObjectFromBson<T>(data));
    }

    /// <summary>
    /// Return all objects of a specific Type in the cache.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <returns>A Future result representing all objects in the cache with the specified Type.</returns>
    public IObservable<IEnumerable<T>> GetAllObjects<T>()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<IEnumerable<T>>(nameof(InMemoryBlobCache));
        }

        return GetAll(typeof(T))
            .Select(kvp => DeserializeObjectFromBson<T>(kvp.Value))
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
    /// Get an object from the cache and deserialize it using BSON serialization.
    /// If not found, fetch it using the provided function and cache the result.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <param name="key">The key to look up in the cache.</param>
    /// <param name="fetchFunc">Function to fetch the data if not in cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the object in the cache or fetched.</returns>
    public IObservable<T?> GetOrFetchObject<T>(string key, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<T?>(nameof(InMemoryBlobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (fetchFunc is null)
        {
            throw new ArgumentNullException(nameof(fetchFunc));
        }

        return GetObject<T>(key).Catch<T?, Exception>(_ =>
        {
            return Observable.FromAsync(fetchFunc)
                .SelectMany(value => InsertObject(key, value, absoluteExpiration).Select(_ => value));
        });
    }

    /// <summary>
    /// Get an object from the cache and deserialize it using BSON serialization.
    /// If not found, fetch it using the provided function and cache the result.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <param name="key">The key to look up in the cache.</param>
    /// <param name="fetchFunc">Function to fetch the data if not in cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the object in the cache or fetched.</returns>
    public IObservable<T?> GetOrFetchObject<T>(string key, Func<IObservable<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<T?>(nameof(InMemoryBlobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (fetchFunc is null)
        {
            throw new ArgumentNullException(nameof(fetchFunc));
        }

        return GetObject<T>(key).Catch<T?, Exception>(_ =>
        {
            return fetchFunc()
                .SelectMany(value => InsertObject(key, value, absoluteExpiration).Select(_ => value));
        });
    }

    private byte[] SerializeObjectToBson<T>(T value)
    {
        var serializer = GetSerializer();
        using var ms = new MemoryStream();
        using var writer = new BsonDataWriter(ms);
        serializer.Serialize(writer, new ObjectWrapper<T>(value));
        return ms.ToArray();
    }

    private T? DeserializeObjectFromBson<T>(byte[] data)
    {
        var serializer = GetSerializer();
        using var reader = new BsonDataReader(new MemoryStream(data));
        var forcedDateTimeKind = ForcedDateTimeKind;

        if (forcedDateTimeKind.HasValue)
        {
            reader.DateTimeKindHandling = forcedDateTimeKind.Value;
        }

        try
        {
            var wrapper = serializer.Deserialize<ObjectWrapper<T>>(reader);
            return wrapper is null ? default : wrapper.Value;
        }
        catch (Exception ex)
        {
            this.Log().Warn(ex, "Failed to deserialize data as boxed, we may be migrating from an old Akavache");
        }

        return serializer.Deserialize<T>(reader);
    }

    private JsonSerializer GetSerializer()
    {
        var settings = Locator.Current.GetService<JsonSerializerSettings>() ?? new JsonSerializerSettings();
        JsonSerializer serializer;

        lock (settings)
        {
            _jsonDateTimeContractResolver.ExistingContractResolver = settings.ContractResolver;
            _jsonDateTimeContractResolver.ForceDateTimeKindOverride = ForcedDateTimeKind;
            settings.ContractResolver = _jsonDateTimeContractResolver;
            serializer = JsonSerializer.Create(settings);
            settings.ContractResolver = _jsonDateTimeContractResolver.ExistingContractResolver;
        }

        return serializer;
    }

    private class CacheEntry
    {
        public byte[]? Value { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }
    }

    private class ObjectWrapper<T>
    {
        public ObjectWrapper()
        {
        }

        public ObjectWrapper(T value) => Value = value;

        public T? Value { get; set; }
    }
}
