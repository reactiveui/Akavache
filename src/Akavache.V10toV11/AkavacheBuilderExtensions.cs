// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Sqlite3;

using Splat;

namespace Akavache.V10toV11;

/// <summary>
/// Provides extension methods for configuring Akavache V11 to work with V10 database files
/// and for migrating V10 data to V11 format.
/// </summary>
public static class AkavacheBuilderExtensions
{
    private const string UserAccount = "UserAccount";
    private const string LocalMachine = "LocalMachine";
    private const string Secure = "Secure";

    /// <summary>
    /// Configures the builder to use V10-era database filenames (blobs.db, userblobs.db, secret.db)
    /// at the legacy directory locations. This allows V11 to find and read existing V10 databases in-place.
    /// New writes will use the V11 CacheEntry table within the same database file, while old data
    /// in the V10 CacheElement table is read transparently via the built-in legacy shim.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no serializer has been registered.</exception>
    public static IAkavacheBuilder WithV10FileNames(this IAkavacheBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using V10 file names.");
        }

        ValidateApplicationName(builder.ApplicationName);

        // Ensure legacy file location is set so directories resolve to V10 paths
        if (builder.FileLocationOption != FileLocationOption.Legacy)
        {
            builder.WithLegacyFileLocation();
        }

        // Create caches using V10 filenames at legacy directory locations
        builder.WithUserAccount(CreateV10Cache(UserAccount, builder))
               .WithLocalMachine(CreateV10Cache(LocalMachine, builder))
               .WithInMemory()
               .WithSecure(new SecureBlobCacheWrapper(CreateV10Cache(Secure, builder)));

        return builder;
    }

    /// <summary>
    /// Performs a one-time migration of data from V10 database files into the current V11 databases.
    /// This method should be called AFTER <c>WithSqliteDefaults()</c> so that V11 databases have been created.
    /// The migration reads all entries from the V10 CacheElement table, converts them to V11 CacheEntry format,
    /// and inserts them into the V11 databases. A sentinel key prevents re-migration on subsequent runs.
    /// </summary>
    /// <param name="builder">The Akavache builder with V11 caches already configured.</param>
    /// <param name="configure">Optional configuration for migration behavior.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when V11 caches have not been configured yet.</exception>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    public static IAkavacheBuilder MigrateFromV10(this IAkavacheBuilder builder, Action<V10MigrationOptions>? configure = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered.");
        }

        var options = new V10MigrationOptions();
        configure?.Invoke(options);

        var serializer = builder.Serializer;

        // Migrate each cache type
        if (options.MigrateUserAccount && builder.UserAccount is SqliteBlobCache userAccount)
        {
            var v10Path = GetV10DatabasePath(builder, UserAccount);
            if (v10Path != null)
            {
                V10MigrationService.MigrateAsync(v10Path, userAccount, serializer, options)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        if (options.MigrateLocalMachine && builder.LocalMachine is SqliteBlobCache localMachine)
        {
            var v10Path = GetV10DatabasePath(builder, LocalMachine);
            if (v10Path != null)
            {
                V10MigrationService.MigrateAsync(v10Path, localMachine, serializer, options)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        if (options.MigrateSecure)
        {
            // Secure cache may be wrapped in SecureBlobCacheWrapper
            var secureCache = GetUnderlyingBlobCache(builder.Secure) as SqliteBlobCache;
            if (secureCache != null)
            {
                var v10Path = GetV10DatabasePath(builder, Secure);
                if (v10Path != null)
                {
                    V10MigrationService.MigrateAsync(v10Path, secureCache, serializer, options)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
        }

        return builder;
    }

    internal static SqliteBlobCache CreateV10Cache(string cacheName, IAkavacheBuilder builder)
    {
        var directory = builder.GetLegacyCacheDirectory(cacheName);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException($"Failed to determine legacy cache directory for '{cacheName}'.");
        }

        // Ensure the cache directory exists
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Use the V10 filename instead of the V11 name
        var filePath = Path.Combine(directory, V10FileNameMap.GetV10FileName(cacheName));

        var serializer = AppLocator.Current.GetService<ISerializer>(builder.SerializerTypeName)
            ?? throw new InvalidOperationException($"No serializer of type '{builder.SerializerTypeName}' is registered in the service locator.");

        var cache = new SqliteBlobCache(filePath, serializer);

        if (builder.ForcedDateTimeKind.HasValue)
        {
            cache.ForcedDateTimeKind = builder.ForcedDateTimeKind.Value;
        }

        return cache;
    }

    internal static string? GetV10DatabasePath(IAkavacheBuilder builder, string cacheName)
    {
        var directory = builder.GetLegacyCacheDirectory(cacheName);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        return Path.Combine(directory, V10FileNameMap.GetV10FileName(cacheName));
    }

    internal static IBlobCache? GetUnderlyingBlobCache(ISecureBlobCache? secureBlobCache) => secureBlobCache switch
    {
        SecureBlobCacheWrapper ourWrapper => ourWrapper.InnerCache,
        Sqlite3.AkavacheBuilderExtensions.SecureBlobCacheWrapper sqliteWrapper => sqliteWrapper.InnerCache,
        IBlobCache blobCache => blobCache,
        _ => null,
    };

    internal static void ValidateApplicationName(string? applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new InvalidOperationException("Application name must be set before configuring V10 file names. Call WithApplicationName() first.");
        }
    }

    /// <summary>
    /// A wrapper that implements ISecureBlobCache by delegating to an IBlobCache.
    /// </summary>
    internal class SecureBlobCacheWrapper : ISecureBlobCache
    {
        private bool _disposed;

        internal SecureBlobCacheWrapper(IBlobCache inner) => InnerCache = inner ?? throw new ArgumentNullException(nameof(inner));

        public IBlobCache InnerCache { get; }

        public DateTimeKind? ForcedDateTimeKind
        {
            get => InnerCache.ForcedDateTimeKind;
            set => InnerCache.ForcedDateTimeKind = value;
        }

        public IScheduler Scheduler => InnerCache.Scheduler;

        public ISerializer Serializer => InnerCache.Serializer;

        public IHttpService HttpService
        {
            get => InnerCache.HttpService;
            set => InnerCache.HttpService = value;
        }

        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) =>
            InnerCache.Insert(keyValuePairs, absoluteExpiration);

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            InnerCache.Insert(key, data, absoluteExpiration);

        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) =>
            InnerCache.Insert(keyValuePairs, type, absoluteExpiration);

        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
            InnerCache.Insert(key, data, type, absoluteExpiration);

        public IObservable<byte[]?> Get(string key) => InnerCache.Get(key);

        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => InnerCache.Get(keys);

        public IObservable<byte[]?> Get(string key, Type type) => InnerCache.Get(key, type);

        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => InnerCache.Get(keys, type);

        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => InnerCache.GetAll(type);

        public IObservable<string> GetAllKeys() => InnerCache.GetAllKeys();

        public IObservable<string> GetAllKeys(Type type) => InnerCache.GetAllKeys(type);

        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => InnerCache.GetCreatedAt(keys);

        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => InnerCache.GetCreatedAt(key);

        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => InnerCache.GetCreatedAt(keys, type);

        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => InnerCache.GetCreatedAt(key, type);

        public IObservable<Unit> Flush() => InnerCache.Flush();

        public IObservable<Unit> Flush(Type type) => InnerCache.Flush(type);

        public IObservable<Unit> Invalidate(string key) => InnerCache.Invalidate(key);

        public IObservable<Unit> Invalidate(string key, Type type) => InnerCache.Invalidate(key, type);

        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => InnerCache.Invalidate(keys);

        public IObservable<Unit> InvalidateAll(Type type) => InnerCache.InvalidateAll(type);

        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => InnerCache.Invalidate(keys, type);

        public IObservable<Unit> InvalidateAll() => InnerCache.InvalidateAll();

        public IObservable<Unit> Vacuum() => InnerCache.Vacuum();

        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(key, absoluteExpiration);

        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(key, type, absoluteExpiration);

        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(keys, absoluteExpiration);

        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(keys, type, absoluteExpiration);

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

        protected internal virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (InnerCache is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _disposed = true;
            }
        }

        protected internal virtual async ValueTask DisposeAsyncCore()
        {
            if (InnerCache is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (InnerCache is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
