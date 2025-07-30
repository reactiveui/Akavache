// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// Default implementation of IBlobCacheBuilder.
/// </summary>
internal class BlobCacheBuilder : IBlobCacheBuilder
{
    private string _applicationName = "Akavache";

    /// <inheritdoc />
    public IBlobCache? InMemory { get; private set; }

    /// <inheritdoc />
    public IBlobCache? LocalMachine { get; private set; }

    /// <inheritdoc />
    public ISecureBlobCache? Secure { get; private set; }

    /// <inheritdoc />
    public IBlobCache? UserAccount { get; private set; }
    /// <inheritdoc />
    public IBlobCacheBuilder Build()
    {
        BlobCache.SetBuilder(this);
        return this;
    }

    /// <inheritdoc />
    public IBlobCacheBuilder WithApplicationName(string applicationName)
    {
        _applicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
        return this;
    }

    /// <inheritdoc />
    public IBlobCacheBuilder WithInMemory(IBlobCache cache)
    {
        InMemory = cache ?? throw new ArgumentNullException(nameof(cache));
        return this;
    }

    /// <inheritdoc />
    public IBlobCacheBuilder WithInMemoryDefaults()
    {
        if (CoreRegistrations.Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered. Call CoreRegistrations.Serializer = new [SerializerType]() before using InMemory defaults.");
        }

        UserAccount ??= BlobCacheBuilder.CreateInMemoryCache();
        LocalMachine ??= BlobCacheBuilder.CreateInMemoryCache();
        Secure ??= new SecureBlobCacheWrapper(BlobCacheBuilder.CreateInMemoryCache());
        InMemory ??= BlobCacheBuilder.CreateInMemoryCache();

        return this;
    }

    /// <inheritdoc />
    public IBlobCacheBuilder WithLocalMachine(IBlobCache cache)
    {
        LocalMachine = cache ?? throw new ArgumentNullException(nameof(cache));
        return this;
    }

    /// <inheritdoc />
    public IBlobCacheBuilder WithSecure(ISecureBlobCache cache)
    {
        Secure = cache ?? throw new ArgumentNullException(nameof(cache));
        return this;
    }

    /// <inheritdoc />
    public IBlobCacheBuilder WithUserAccount(IBlobCache cache)
    {
        UserAccount = cache ?? throw new ArgumentNullException(nameof(cache));
        return this;
    }

    private static void ApplyForcedDateTimeKind(IBlobCache cache)
    {
        if (BlobCache.ForcedDateTimeKind.HasValue)
        {
            cache.ForcedDateTimeKind = BlobCache.ForcedDateTimeKind.Value;
        }
    }

    private static IBlobCache CreateInMemoryCache()
    {
        if (CoreRegistrations.Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered. Call CoreRegistrations.Serializer = new [SerializerType]() before using BlobCache.");
        }

        var serializerType = CoreRegistrations.Serializer.GetType();

        // Try to create the appropriate InMemoryBlobCache based on serializer
        if (serializerType.Namespace?.Contains("SystemTextJson") == true)
        {
            var type = Type.GetType("Akavache.SystemTextJson.InMemoryBlobCache, Akavache.SystemTextJson");
            if (type != null)
            {
                var cache = (IBlobCache)Activator.CreateInstance(type)!;
                ApplyForcedDateTimeKind(cache);
                return cache;
            }
        }
        else if (serializerType.Namespace?.Contains("NewtonsoftJson") == true)
        {
            var type = Type.GetType("Akavache.NewtonsoftJson.InMemoryBlobCache, Akavache.NewtonsoftJson");
            if (type != null)
            {
                var cache = (IBlobCache)Activator.CreateInstance(type)!;
                ApplyForcedDateTimeKind(cache);
                return cache;
            }
        }

        throw new InvalidOperationException(
            "No suitable InMemoryBlobCache implementation found. " +
            "Install one of: Akavache.SystemTextJson or Akavache.NewtonsoftJson packages and ensure a serializer is registered.");
    }

    /// <summary>
    /// A wrapper that implements ISecureBlobCache by delegating to an IBlobCache.
    /// </summary>
    private class SecureBlobCacheWrapper : ISecureBlobCache
    {
        private readonly IBlobCache _inner;

        public SecureBlobCacheWrapper(IBlobCache inner)
        {
            _inner = inner;
        }

        public DateTimeKind? ForcedDateTimeKind
        {
            get => _inner.ForcedDateTimeKind;
            set => _inner.ForcedDateTimeKind = value;
        }

        public IScheduler Scheduler => _inner.Scheduler;

        public void Dispose()
        {
            if (_inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_inner is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (_inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public IObservable<Unit> Flush() => _inner.Flush();

        public IObservable<Unit> Flush(Type type) => _inner.Flush(type);

        public IObservable<byte[]?> Get(string key) => _inner.Get(key);

        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => _inner.Get(keys);

        public IObservable<byte[]?> Get(string key, Type type) => _inner.Get(key, type);

        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => _inner.Get(keys, type);

        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => _inner.GetAll(type);

        public IObservable<string> GetAllKeys() => _inner.GetAllKeys();

        public IObservable<string> GetAllKeys(Type type) => _inner.GetAllKeys(type);

        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => _inner.GetCreatedAt(keys);

        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => _inner.GetCreatedAt(key);

        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => _inner.GetCreatedAt(keys, type);

        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => _inner.GetCreatedAt(key, type);

        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) =>
                                                                                                                                    _inner.Insert(keyValuePairs, absoluteExpiration);

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            _inner.Insert(key, data, absoluteExpiration);

        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) =>
            _inner.Insert(keyValuePairs, type, absoluteExpiration);

        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
            _inner.Insert(key, data, type, absoluteExpiration);

        public IObservable<Unit> Invalidate(string key) => _inner.Invalidate(key);

        public IObservable<Unit> Invalidate(string key, Type type) => _inner.Invalidate(key, type);

        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => _inner.Invalidate(keys);

        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => _inner.Invalidate(keys, type);

        public IObservable<Unit> InvalidateAll(Type type) => _inner.InvalidateAll(type);

        public IObservable<Unit> InvalidateAll() => _inner.InvalidateAll();

        public IObservable<Unit> Vacuum() => _inner.Vacuum();
    }
}
