// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Helpers;
using Akavache.Sqlite3;

using Splat;

namespace Akavache.V10toV11;

/// <summary>
/// Provides extension methods for configuring Akavache V11 to work with V10 database files
/// and for migrating V10 data to V11 format.
/// </summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>The cache name used for the user account cache.</summary>
    private const string UserAccount = "UserAccount";

    /// <summary>The cache name used for the local machine cache.</summary>
    private const string LocalMachine = "LocalMachine";

    /// <summary>The cache name used for the secure cache.</summary>
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
        ArgumentExceptionHelper.ThrowIfNull(builder);

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
        ArgumentExceptionHelper.ThrowIfNull(builder);

        if (builder.Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered.");
        }

        V10MigrationOptions options = new();
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
            if (GetUnderlyingBlobCache(builder.Secure) is not SqliteBlobCache secureCache)
            {
                return builder;
            }

            var v10Path = GetV10DatabasePath(builder, Secure);
            if (v10Path != null)
            {
                V10MigrationService.MigrateAsync(v10Path, secureCache, serializer, options)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        return builder;
    }

    /// <summary>
    /// Creates a <see cref="SqliteBlobCache"/> rooted at the legacy V10 directory and filename for the given cache name.
    /// </summary>
    /// <param name="cacheName">The logical V11 cache name (e.g., "UserAccount").</param>
    /// <param name="builder">The Akavache builder used to resolve directories and the serializer.</param>
    /// <returns>A <see cref="SqliteBlobCache"/> bound to the legacy V10 file path.</returns>
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

        SqliteBlobCache cache = new(filePath, serializer);

        if (builder.ForcedDateTimeKind.HasValue)
        {
            cache.ForcedDateTimeKind = builder.ForcedDateTimeKind.Value;
        }

        return cache;
    }

    /// <summary>
    /// Gets the absolute path to the V10 database file for the given cache name, or <c>null</c> if no legacy directory is available.
    /// </summary>
    /// <param name="builder">The Akavache builder used to resolve directories.</param>
    /// <param name="cacheName">The logical V11 cache name.</param>
    /// <returns>The full path to the V10 database file, or <c>null</c> if it cannot be determined.</returns>
    internal static string? GetV10DatabasePath(IAkavacheBuilder builder, string cacheName)
    {
        var directory = builder.GetLegacyCacheDirectory(cacheName);
        return string.IsNullOrWhiteSpace(directory) ? null : Path.Combine(directory, V10FileNameMap.GetV10FileName(cacheName));
    }

    /// <summary>
    /// Unwraps known secure cache wrappers to retrieve the underlying <see cref="IBlobCache"/>.
    /// </summary>
    /// <param name="secureBlobCache">The secure cache to unwrap.</param>
    /// <returns>The underlying blob cache, or <c>null</c> if none can be resolved.</returns>
    internal static IBlobCache? GetUnderlyingBlobCache(ISecureBlobCache? secureBlobCache) => secureBlobCache switch
    {
        SecureBlobCacheWrapper ourWrapper => ourWrapper.InnerCache,
        Sqlite3.AkavacheBuilderExtensions.SecureBlobCacheWrapper sqliteWrapper => sqliteWrapper.InnerCache,
        IBlobCache blobCache => blobCache,
        _ => null,
    };

    /// <summary>
    /// Validates that an application name has been configured on the builder.
    /// </summary>
    /// <param name="applicationName">The application name to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when the name is null, empty, or whitespace.</exception>
    internal static void ValidateApplicationName(string? applicationName)
    {
        if (!string.IsNullOrWhiteSpace(applicationName))
        {
            return;
        }

        throw new InvalidOperationException("Application name must be set before configuring V10 file names. Call WithApplicationName() first.");
    }

    /// <summary>
    /// A wrapper that implements ISecureBlobCache by delegating to an IBlobCache.
    /// </summary>
    internal class SecureBlobCacheWrapper : ISecureBlobCache
    {
        /// <summary>Tracks whether the wrapper has been disposed.</summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureBlobCacheWrapper"/> class.
        /// </summary>
        /// <param name="inner">The blob cache to delegate to.</param>
        internal SecureBlobCacheWrapper(IBlobCache inner)
        {
            ArgumentExceptionHelper.ThrowIfNull(inner);
            InnerCache = inner;
        }

        /// <summary>
        /// Gets the underlying blob cache that this wrapper delegates all operations to.
        /// </summary>
        public IBlobCache InnerCache { get; }

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind
        {
            get => InnerCache.ForcedDateTimeKind;
            set => InnerCache.ForcedDateTimeKind = value;
        }

        /// <inheritdoc/>
        public IScheduler Scheduler => InnerCache.Scheduler;

        /// <inheritdoc/>
        public ISerializer Serializer => InnerCache.Serializer;

        /// <inheritdoc/>
        public IHttpService HttpService
        {
            get => InnerCache.HttpService;
            set => InnerCache.HttpService = value;
        }

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) =>
            InnerCache.Insert(keyValuePairs, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            InnerCache.Insert(key, data, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) =>
            InnerCache.Insert(keyValuePairs, type, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
            InnerCache.Insert(key, data, type, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) => InnerCache.Get(key);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => InnerCache.Get(keys);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) => InnerCache.Get(key, type);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => InnerCache.Get(keys, type);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => InnerCache.GetAll(type);

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => InnerCache.GetAllKeys();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => InnerCache.GetAllKeys(type);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => InnerCache.GetCreatedAt(keys);

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => InnerCache.GetCreatedAt(key);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => InnerCache.GetCreatedAt(keys, type);

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => InnerCache.GetCreatedAt(key, type);

        /// <inheritdoc/>
        public IObservable<Unit> Flush() => InnerCache.Flush();

        /// <inheritdoc/>
        public IObservable<Unit> Flush(Type type) => InnerCache.Flush(type);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key) => InnerCache.Invalidate(key);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key, Type type) => InnerCache.Invalidate(key, type);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => InnerCache.Invalidate(keys);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll(Type type) => InnerCache.InvalidateAll(type);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => InnerCache.Invalidate(keys, type);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll() => InnerCache.InvalidateAll();

        /// <inheritdoc/>
        public IObservable<Unit> Vacuum() => InnerCache.Vacuum();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(key, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(key, type, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(keys, absoluteExpiration);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) => InnerCache.UpdateExpiration(keys, type, absoluteExpiration);

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the wrapper and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected internal virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
            {
                return;
            }

            if (InnerCache is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Asynchronously releases the resources owned by the wrapper.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
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
