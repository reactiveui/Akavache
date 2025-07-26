// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// This class is an IBlobCache backed by a simple in-memory Dictionary.
/// Use it for testing / mocking purposes with BSON serialization.
/// </summary>
public sealed class InMemoryBlobCache : IBlobCache
{
    private readonly Dictionary<string, CacheEntry> _cache = [];
    private readonly object _lock = new();
    private readonly CompositeDisposable _disposables = [];
    private readonly AsyncSubject<Unit> _shutdown = new();
    private ISerializer? _originalSerializer;
    private BsonAwareSerializer? _bsonSerializer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
    /// </summary>
    public InMemoryBlobCache()
        : this(CoreRegistrations.TaskpoolScheduler)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
    /// </summary>
    /// <param name="scheduler">The scheduler to use for Observable based operations.</param>
    public InMemoryBlobCache(IScheduler scheduler)
    {
        Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        SetupBsonSerialization();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
    /// </summary>
    /// <param name="initialContents">The initial contents of the cache.</param>
    public InMemoryBlobCache(IEnumerable<KeyValuePair<string, byte[]>> initialContents)
        : this(CoreRegistrations.TaskpoolScheduler, initialContents)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
    /// </summary>
    /// <param name="scheduler">The scheduler to use for Observable based operations.</param>
    /// <param name="initialContents">The initial contents of the cache.</param>
    public InMemoryBlobCache(IScheduler scheduler, IEnumerable<KeyValuePair<string, byte[]>>? initialContents)
        : this(scheduler)
    {
        if (initialContents != null)
        {
            lock (_lock)
            {
                foreach (var item in initialContents)
                {
                    _cache[item.Key] = new CacheEntry
                    {
                        Value = item.Value,
                        CreatedAt = Scheduler.Now,
                        ExpiresAt = null,
                        TypeName = null
                    };
                }
            }
        }
    }

    /// <inheritdoc />
    public IScheduler Scheduler { get; }

    /// <summary>
    /// Gets or sets the DateTimeKind handling for BSON readers to be forced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, BsonReader uses a <see cref="DateTimeKind"/> of <see cref="DateTimeKind.Local"/> and see BsonWriter
    /// uses <see cref="DateTimeKind.Utc"/>. Thus, DateTimes are serialized as UTC but deserialized as local time. To force BSON readers to
    /// use some other <c>DateTimeKind</c>, you can set this value.
    /// </para>
    /// </remarks>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <summary>
    /// Gets an observable that signals when the cache is shut down.
    /// </summary>
    public IObservable<Unit> Shutdown => _shutdown;

    /// <summary>
    /// Overrides the global registrations with specified values.
    /// </summary>
    /// <param name="scheduler">The default scheduler to use.</param>
    /// <param name="initialContents">The default inner contents to use.</param>
    /// <returns>A generated cache.</returns>
    public static InMemoryBlobCache OverrideGlobals(IScheduler? scheduler = null, params KeyValuePair<string, byte[]>[] initialContents) =>
        new InMemoryBlobCache(scheduler ?? CoreRegistrations.TaskpoolScheduler, initialContents);

    /// <summary>
    /// Overrides the global registrations with specified values.
    /// </summary>
    /// <param name="initialContents">The default inner contents to use.</param>
    /// <param name="scheduler">The default scheduler to use.</param>
    /// <returns>A generated cache.</returns>
    public static InMemoryBlobCache OverrideGlobals(IDictionary<string, byte[]> initialContents, IScheduler? scheduler = null) => new InMemoryBlobCache(scheduler ?? CoreRegistrations.TaskpoolScheduler, initialContents);

    /// <summary>
    /// Overrides the global registrations with specified values.
    /// </summary>
    /// <param name="initialContents">The default inner contents to use.</param>
    /// <param name="scheduler">The default scheduler to use.</param>
    /// <returns>A generated cache.</returns>
    public static InMemoryBlobCache OverrideGlobals(IDictionary<string, object> initialContents, IScheduler? scheduler = null)
    {
        var bsonCache = new InMemoryBlobCache(scheduler ?? CoreRegistrations.TaskpoolScheduler);
        if (initialContents is null)
        {
            throw new ArgumentNullException(nameof(initialContents));
        }

        foreach (var kvp in initialContents)
        {
            var data = bsonCache.SerializeObjectToBson(kvp.Value);
            bsonCache.Insert(kvp.Key, data).Wait();
        }

        return bsonCache;
    }

    /// <inheritdoc />
    public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        if (keyValuePairs is null)
        {
            throw new ArgumentNullException(nameof(keyValuePairs));
        }

        lock (_lock)
        {
            foreach (var kvp in keyValuePairs)
            {
                _cache[kvp.Key] = new CacheEntry
                {
                    Value = kvp.Value,
                    CreatedAt = Scheduler.Now,
                    ExpiresAt = absoluteExpiration,
                    TypeName = null
                };
            }
        }

        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc />
    public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        lock (_lock)
        {
            _cache[key] = new CacheEntry
            {
                Value = data,
                CreatedAt = Scheduler.Now,
                ExpiresAt = absoluteExpiration,
                TypeName = null
            };
        }

        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc />
    public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        if (keyValuePairs is null)
        {
            throw new ArgumentNullException(nameof(keyValuePairs));
        }

        lock (_lock)
        {
            foreach (var kvp in keyValuePairs)
            {
                _cache[kvp.Key] = new CacheEntry
                {
                    Value = kvp.Value,
                    CreatedAt = Scheduler.Now,
                    ExpiresAt = absoluteExpiration,
                    TypeName = type?.FullName
                };
            }
        }

        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc />
    public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        lock (_lock)
        {
            _cache[key] = new CacheEntry
            {
                Value = data,
                CreatedAt = Scheduler.Now,
                ExpiresAt = absoluteExpiration,
                TypeName = type?.FullName
            };
        }

        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc />
    public IObservable<byte[]?> Get(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]?>(nameof(InMemoryBlobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        CacheEntry? entry;
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out entry))
            {
                return IBlobCache.ExceptionHelpers.ObservableThrowKeyNotFoundException<byte[]?>(key);
            }
        }

        if (entry.ExpiresAt.HasValue && Scheduler.Now > entry.ExpiresAt.Value)
        {
            lock (_lock)
            {
                _cache.Remove(key);
            }

            return IBlobCache.ExceptionHelpers.ObservableThrowKeyNotFoundException<byte[]?>(key);
        }

        return Observable.Return(entry.Value, Scheduler);
    }

    /// <inheritdoc />
    public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(nameof(InMemoryBlobCache));
        }

        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        return keys.ToObservable()
            .SelectMany(key => Get(key)
                .Where(data => data != null)
                .Select(data => new KeyValuePair<string, byte[]>(key, data!))
                .Catch<KeyValuePair<string, byte[]>, Exception>(_ => Observable.Empty<KeyValuePair<string, byte[]>>()));
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

        lock (_lock)
        {
            var validEntries = _cache
                .Where(kvp => (!kvp.Value.ExpiresAt.HasValue || kvp.Value.ExpiresAt.Value >= Scheduler.Now) &&
                              kvp.Value.Value != null &&
                              (type == null || kvp.Value.TypeName == type.FullName))
                .Select(kvp => new KeyValuePair<string, byte[]>(kvp.Key, kvp.Value.Value!))
                .ToList();

            return validEntries.ToObservable();
        }
    }

    /// <inheritdoc />
    public IObservable<string> GetAllKeys()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(nameof(InMemoryBlobCache));
        }

        lock (_lock)
        {
            return _cache
                .Where(kvp => !kvp.Value.ExpiresAt.HasValue || kvp.Value.ExpiresAt.Value >= Scheduler.Now)
                .Select(kvp => kvp.Key)
                .ToList()
                .ToObservable();
        }
    }

    /// <inheritdoc />
    public IObservable<string> GetAllKeys(Type type)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(nameof(InMemoryBlobCache));
        }

        lock (_lock)
        {
            return _cache
                .Where(kvp => (!kvp.Value.ExpiresAt.HasValue || kvp.Value.ExpiresAt.Value >= Scheduler.Now) &&
                              (type == null || kvp.Value.TypeName == type.FullName))
                .Select(kvp => kvp.Key)
                .ToList()
                .ToObservable();
        }
    }

    /// <inheritdoc />
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<(string, DateTimeOffset?)>(nameof(InMemoryBlobCache));
        }

        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        return keys.ToObservable()
            .SelectMany(key => GetCreatedAt(key)
                .Select(time => (key, time))
                .Catch<(string, DateTimeOffset?), Exception>(_ => Observable.Return((key, (DateTimeOffset?)null))));
    }

    /// <inheritdoc />
    public IObservable<DateTimeOffset?> GetCreatedAt(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<DateTimeOffset?>(nameof(InMemoryBlobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        CacheEntry? entry;
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out entry))
            {
                return Observable.Return<DateTimeOffset?>(null);
            }
        }

        if (entry.ExpiresAt.HasValue && Scheduler.Now > entry.ExpiresAt.Value)
        {
            lock (_lock)
            {
                _cache.Remove(key);
            }

            return Observable.Return<DateTimeOffset?>(null);
        }

        return Observable.Return<DateTimeOffset?>(entry.CreatedAt, Scheduler);
    }

    /// <inheritdoc />
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => GetCreatedAt(keys);

    /// <inheritdoc />
    public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => GetCreatedAt(key);

    /// <inheritdoc />
    public IObservable<Unit> Flush()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc />
    public IObservable<Unit> Flush(Type type) => Flush();

    /// <inheritdoc />
    public IObservable<Unit> Invalidate(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        lock (_lock)
        {
            _cache.Remove(key);
        }

        return Observable.Return(Unit.Default);
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

        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        lock (_lock)
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
        }

        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc />
    public IObservable<Unit> InvalidateAll(Type type)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        lock (_lock)
        {
            var keysToRemove = _cache
                .Where(kvp => type == null || kvp.Value.TypeName == type.FullName)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }

        return Observable.Return(Unit.Default);
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

        lock (_lock)
        {
            _cache.Clear();
        }

        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc />
    public IObservable<Unit> Vacuum()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        lock (_lock)
        {
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.ExpiresAt.HasValue && Scheduler.Now > kvp.Value.ExpiresAt.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }
        }

        return Observable.Return(Unit.Default);
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
        }

        // Restore original serializer
        if (_originalSerializer != null)
        {
            CoreRegistrations.Serializer = _originalSerializer;
        }

        _disposables.Dispose();
        _shutdown.OnNext(Unit.Default);
        _shutdown.OnCompleted();
        _shutdown.Dispose();
        _disposed = true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await Task.Run(() =>
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }).ConfigureAwait(false);

        // Restore original serializer
        if (_originalSerializer != null)
        {
            CoreRegistrations.Serializer = _originalSerializer;
        }

        _disposables.Dispose();
        _shutdown.OnNext(Unit.Default);
        _shutdown.OnCompleted();
        _shutdown.Dispose();
        _disposed = true;
    }

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

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
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

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
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
    public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<DateTimeOffset?>(nameof(InMemoryBlobCache));
        }

        return GetCreatedAt(key, typeof(T));
    }

    /// <summary>
    /// Invalidates a single object from the cache.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    public IObservable<Unit> InvalidateObject<T>(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        return Invalidate(key, typeof(T));
    }

    /// <summary>
    /// Invalidates all objects of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object associated with the blob.</typeparam>
    /// <returns>A Future result representing the completion of the invalidation.</returns>
    public IObservable<Unit> InvalidateAllObjects<T>()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(nameof(InMemoryBlobCache));
        }

        return InvalidateAll(typeof(T));
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

    private void SetupBsonSerialization(DateTimeKind? forcedDateTimeKind = null)
    {
        // Store the original serializer if this is the first time
        if (_originalSerializer == null)
        {
            _originalSerializer = CoreRegistrations.Serializer;
        }

        // Update the ForcedDateTimeKind property
        ForcedDateTimeKind = forcedDateTimeKind;

        // Set up BSON-aware serializer globally so extension methods use it
        if (_originalSerializer != null)
        {
            _bsonSerializer = new BsonAwareSerializer(_originalSerializer, forcedDateTimeKind);
            CoreRegistrations.Serializer = _bsonSerializer;
        }
    }

    private JsonSerializerSettings GetBsonSettings() => new JsonSerializerSettings
    {
        ContractResolver = new DateTimeContractResolver
        {
            ForceDateTimeKindOverride = ForcedDateTimeKind
        },
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
    };

    private byte[] SerializeObjectToBson<T>(T value)
    {
        var settings = GetBsonSettings();
        using var ms = new MemoryStream();
        using var writer = new BsonDataWriter(ms);
        var serializer = JsonSerializer.Create(settings);
        serializer.Serialize(writer, new ObjectWrapper<T>(value));
        return ms.ToArray();
    }

    private T? DeserializeObjectFromBson<T>(byte[] data)
    {
        var settings = GetBsonSettings();
        using var ms = new MemoryStream(data);
        using var reader = new BsonDataReader(ms);
        var serializer = JsonSerializer.Create(settings);

        try
        {
            var wrapper = serializer.Deserialize<ObjectWrapper<T>>(reader);
            return wrapper != null ? wrapper.Value : default;
        }
        catch
        {
            // Fallback to direct deserialization for backward compatibility
            ms.Position = 0;
            return serializer.Deserialize<T>(reader);
        }
    }

    private class CacheEntry
    {
        public byte[]? Value { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public string? TypeName { get; set; }
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
