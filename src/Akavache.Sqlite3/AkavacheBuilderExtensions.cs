// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Extension methods for IAkavacheBuilder to add SQLite support.
/// </summary>
public static class AkavacheBuilderExtensions
{
#if ENCRYPTED
    /// <summary>
    /// Configures default SQLite-based caches for all cache types.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="password">The password.</param>
    /// <returns>
    /// The builder instance for fluent configuration.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    /// <exception cref="System.InvalidOperationException">
    /// No serializer has been registered. Call CacheDatabase.Serializer = new [SerializerType]() before using SQLite defaults.
    /// or
    /// Application name must be set before configuring SQLite defaults. Call WithApplicationName() first.
    /// </exception>
    public static IAkavacheBuilder WithSqliteDefaults(this IAkavacheBuilder builder, string password)
#else
    /// <summary>
    /// Configures default SQLite-based caches for all cache types.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    public static IAkavacheBuilder WithSqliteDefaults(this IAkavacheBuilder builder)
#endif
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (CacheDatabase.Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Serializer = new [SerializerType]() before using SQLite defaults.");
        }

        var applicationName = CacheDatabase.ApplicationName;
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new InvalidOperationException("Application name must be set before configuring SQLite defaults. Call WithApplicationName() first.");
        }

        SQLitePCL.Batteries_V2.Init();

#if ENCRYPTED
        // Create SQLite caches for persistent storage
        builder.WithUserAccount(CreateEncryptedSqliteCache("UserAccount", applicationName, password))
               .WithLocalMachine(CreateEncryptedSqliteCache("LocalMachine", applicationName, password))
               .WithInMemory()
               .WithSecure(CreateEncryptedSqliteCache("Secure", applicationName, password));
#else
        // Create SQLite caches for persistent storage
        builder.WithUserAccount(CreateSqliteCache("UserAccount", applicationName))
               .WithLocalMachine(CreateSqliteCache("LocalMachine", applicationName))
               .WithInMemory()
               .WithSecure(new SecureBlobCacheWrapper(CreateSqliteCache("Secure", applicationName)));
#endif

        return builder;
    }

#if ENCRYPTED
    private static EncryptedSqliteBlobCache CreateEncryptedSqliteCache(string name, string applicationName, string password)
#else
    private static SqliteBlobCache CreateSqliteCache(string name, string applicationName)
#endif
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new ArgumentException("Application name cannot be null or empty.", nameof(applicationName));
        }

        string filePath;
        if (name == ":memory:")
        {
            filePath = ":memory:";
        }
        else
        {
            var directory = GetCacheDirectory(name, applicationName);
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create cache directory '{directory}': {ex.Message}", ex);
            }

            filePath = Path.Combine(directory, $"{name}.db");
        }

#if ENCRYPTED
        var cache = new EncryptedSqliteBlobCache(filePath, password);
#else
        var cache = new SqliteBlobCache(filePath);
#endif
        if (CacheDatabase.ForcedDateTimeKind.HasValue)
        {
            cache.ForcedDateTimeKind = CacheDatabase.ForcedDateTimeKind.Value;
        }

        return cache;
    }

    private static string GetCacheDirectory(string cacheName, string applicationName)
    {
        if (string.IsNullOrWhiteSpace(cacheName))
        {
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(cacheName));
        }

        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new ArgumentException("Application name cannot be null or empty.", nameof(applicationName));
        }

        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new InvalidOperationException("Unable to determine local application data directory.");
        }

        return Path.Combine(baseDirectory, applicationName, cacheName);
    }

#if !ENCRYPTED
    /// <summary>
    /// A wrapper that implements ISecureBlobCache by delegating to an IBlobCache.
    /// </summary>
    private class SecureBlobCacheWrapper(IBlobCache inner) : ISecureBlobCache
    {
        private readonly IBlobCache _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        private bool _disposed;

        public DateTimeKind? ForcedDateTimeKind
        {
            get => _inner.ForcedDateTimeKind;
            set => _inner.ForcedDateTimeKind = value;
        }

        public IScheduler Scheduler => _inner.Scheduler;

        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) =>
            _inner.Insert(keyValuePairs, absoluteExpiration);

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            _inner.Insert(key, data, absoluteExpiration);

        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) =>
            _inner.Insert(keyValuePairs, type, absoluteExpiration);

        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
            _inner.Insert(key, data, type, absoluteExpiration);

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

        public IObservable<Unit> Flush() => _inner.Flush();

        public IObservable<Unit> Flush(Type type) => _inner.Flush(type);

        public IObservable<Unit> Invalidate(string key) => _inner.Invalidate(key);

        public IObservable<Unit> Invalidate(string key, Type type) => _inner.Invalidate(key, type);

        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => _inner.Invalidate(keys);

        public IObservable<Unit> InvalidateAll(Type type) => _inner.InvalidateAll(type);

        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => _inner.Invalidate(keys, type);

        public IObservable<Unit> InvalidateAll() => _inner.InvalidateAll();

        public IObservable<Unit> Vacuum() => _inner.Vacuum();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_inner is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _disposed = true;
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_inner is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (_inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
#endif
}
