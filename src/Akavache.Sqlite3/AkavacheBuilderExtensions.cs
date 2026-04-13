// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;

using SQLitePCL;

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Provides extension methods for configuring Akavache to use SQLite-based blob caches.
/// </summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>Cache name used for the per-user account persistent cache.</summary>
    private const string UserAccount = "UserAccount";

    /// <summary>Cache name used for the local machine persistent cache.</summary>
    private const string LocalMachine = "LocalMachine";

    /// <summary>Cache name used for the secure persistent cache.</summary>
    private const string Secure = "Secure";

    /// <summary>Tracks whether the SQLite provider batteries have already been initialized.</summary>
    private static bool? _sqliteProvider;

#if ENCRYPTED
    /// <summary>
    /// Configures the builder to use the encrypted SQLite provider for secure data storage.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static IAkavacheBuilder WithEncryptedSqliteProvider(this IAkavacheBuilder builder)
#else
    /// <summary>
    /// Configures the builder to use the SQLite provider for persistent data storage.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static IAkavacheBuilder WithSqliteProvider(this IAkavacheBuilder builder)
#endif
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        // Ensure SQLitePCL is initialized only once
        if (_sqliteProvider != null)
        {
            return builder;
        }

        Batteries_V2.Init();
        _sqliteProvider = true;
        return builder;
    }

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

        // For backward compatibility, automatically initialize the SQLite provider if not already done
        if (_sqliteProvider == null)
        {
#if ENCRYPTED
            builder.WithEncryptedSqliteProvider();
#else
            builder.WithSqliteProvider();
#endif
        }

        if (builder.Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using SQLite defaults.");
        }

        var applicationName = builder.ApplicationName;
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new InvalidOperationException("Application name must be set before configuring SQLite defaults. Call WithApplicationName() first.");
        }

#if ENCRYPTED
        // Create SQLite caches for persistent storage
        builder.WithUserAccount(CreateEncryptedSqliteCache(UserAccount, builder, password))
               .WithLocalMachine(CreateEncryptedSqliteCache(LocalMachine, builder, password))
               .WithInMemory()
               .WithSecure(CreateEncryptedSqliteCache(Secure, builder, password));
#else
        // Create SQLite caches for persistent storage
        builder.WithUserAccount(CreateSqliteCache(UserAccount, builder))
               .WithLocalMachine(CreateSqliteCache(LocalMachine, builder))
               .WithInMemory()
               .WithSecure(new SecureBlobCacheWrapper(CreateSqliteCache(Secure, builder)));
#endif

        return builder;
    }

    /// <summary>
    /// Resets the SQLite provider state for testing purposes.
    /// </summary>
    internal static void ResetSqliteProviderForTests() => _sqliteProvider = null;

#if ENCRYPTED
    /// <summary>
    /// Creates an <see cref="EncryptedSqliteBlobCache"/> for the specified cache name using the builder's serializer and directory configuration.
    /// </summary>
    /// <param name="name">The logical cache name (e.g. <c>UserAccount</c>, <c>LocalMachine</c>, <c>Secure</c>).</param>
    /// <param name="builder">The Akavache builder supplying serializer, application name, and file location options.</param>
    /// <param name="password">The password used to encrypt the SQLite database.</param>
    /// <returns>A configured <see cref="EncryptedSqliteBlobCache"/>.</returns>
    internal static EncryptedSqliteBlobCache CreateEncryptedSqliteCache(string name, IAkavacheBuilder builder, string password)
#else
    /// <summary>
    /// Creates a <see cref="SqliteBlobCache"/> for the specified cache name using the builder's serializer and directory configuration.
    /// </summary>
    /// <param name="name">The logical cache name (e.g. <c>UserAccount</c>, <c>LocalMachine</c>, <c>Secure</c>).</param>
    /// <param name="builder">The Akavache builder supplying serializer, application name, and file location options.</param>
    /// <returns>A configured <see cref="SqliteBlobCache"/>.</returns>
    internal static SqliteBlobCache CreateSqliteCache(string name, IAkavacheBuilder builder)
#endif
    {
        var serializer = builder.Serializer;
        if (serializer is null)
        {
            throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using SQLite caches.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(builder.ApplicationName))
        {
            throw new ArgumentException("Application name cannot be null or empty.", nameof(builder.ApplicationName));
        }

        // Validate cache name to prevent path traversal attacks
        var validatedName = SecurityUtilities.ValidateCacheName(name, nameof(name));

        // Determine the cache directory.
        var directory = builder.FileLocationOption switch
        {
            FileLocationOption.Legacy => builder.GetLegacyCacheDirectory(validatedName),
            _ => builder.GetIsolatedCacheDirectory(validatedName),
        };

        // Ensure the cache directory exists (legacy paths may not be pre-created).
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        var filePath = Path.Combine(directory!, $"{validatedName}.db");

#if ENCRYPTED
        var cache = new EncryptedSqliteBlobCache(filePath, password, serializer);
#else
        var cache = new SqliteBlobCache(filePath, serializer);
#endif
        if (builder.ForcedDateTimeKind.HasValue)
        {
            cache.ForcedDateTimeKind = builder.ForcedDateTimeKind.Value;
        }

        return cache;
    }

#if !ENCRYPTED
    /// <summary>
    /// A wrapper that implements ISecureBlobCache by delegating to an IBlobCache.
    /// </summary>
    internal class SecureBlobCacheWrapper : ISecureBlobCache
    {
        /// <summary>Tracks whether the wrapper has already been disposed.</summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureBlobCacheWrapper"/> class
        /// that wraps the specified <paramref name="inner"/> blob cache as a secure cache.
        /// </summary>
        /// <param name="inner">The underlying blob cache to delegate all operations to.</param>
        internal SecureBlobCacheWrapper(IBlobCache inner) => InnerCache = inner ?? throw new ArgumentNullException(nameof(inner));

        /// <summary>Gets the underlying blob cache that this wrapper delegates to.</summary>
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
        public ISerializer Serializer => InnerCache.Serializer ?? throw new InvalidOperationException("The inner cache's Serializer is null.");

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
        /// Releases managed resources held by the wrapper, disposing the <see cref="InnerCache"/> when applicable.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>; <see langword="false"/> from a finalizer or async disposal path.</param>
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
        /// Asynchronously releases resources held by the wrapper, disposing the <see cref="InnerCache"/> when it supports async disposal.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
        protected internal virtual async ValueTask DisposeAsyncCore()
        {
            if (InnerCache is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
#endif
}
