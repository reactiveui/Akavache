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

        // Build the migration pipeline as a single observable chain, then block on it
        // exactly once at the bottom. The builder extension's outer API is synchronous
        // by contract — it returns the builder for continued fluent configuration — so
        // the blocking bridge lives here rather than inside V10MigrationService. Each
        // BuildMigration call returns Observable.Return(Unit.Default) when its cache
        // kind is disabled or unavailable, so Concat runs exactly the enabled ones.
        var pipeline = BuildMigration(builder, UserAccount, options.MigrateUserAccount, builder.UserAccount as SqliteBlobCache, serializer, options)
            .Concat(BuildMigration(builder, LocalMachine, options.MigrateLocalMachine, builder.LocalMachine as SqliteBlobCache, serializer, options))
            .Concat(BuildMigration(builder, Secure, options.MigrateSecure, GetUnderlyingBlobCache(builder.Secure) as SqliteBlobCache, serializer, options));

        pipeline.Wait();

        return builder;
    }

    /// <summary>
    /// Wraps a single cache-kind migration in an <see cref="IObservable{Unit}"/> that
    /// short-circuits when the kind is disabled, the underlying cache is not a
    /// <see cref="SqliteBlobCache"/>, or no V10 database file exists for it. The
    /// returned observable emits a single <see cref="Unit"/> on completion regardless
    /// of which branch fired — so callers can <c>Concat</c> multiple kinds into one
    /// pipeline without tracking each one individually.
    /// </summary>
    /// <remarks>
    /// Marked <c>internal</c> so tests can drive each branch in isolation without
    /// spinning up the full <see cref="MigrateFromV10"/> entry point. Every
    /// observable branch returns one item then completes, which makes it trivial to
    /// assert on the result sequence in a unit test.
    /// </remarks>
    /// <param name="builder">The Akavache builder supplying path resolution and serializer context.</param>
    /// <param name="cacheName">Logical cache-kind name (<c>UserAccount</c> / <c>LocalMachine</c> / <c>Secure</c>).</param>
    /// <param name="enabled">Whether the migration is enabled for this kind in the options.</param>
    /// <param name="sqliteCache">The V11 destination cache, or <see langword="null"/> when the kind isn't a SqliteBlobCache.</param>
    /// <param name="serializer">The current serializer (used by the row-conversion path).</param>
    /// <param name="options">Migration options.</param>
    /// <returns>A one-shot observable that completes when migration for this kind finishes (or is skipped).</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    internal static IObservable<Unit> BuildMigration(
        IAkavacheBuilder builder,
        string cacheName,
        bool enabled,
        SqliteBlobCache? sqliteCache,
        ISerializer serializer,
        V10MigrationOptions options)
    {
        if (!enabled || sqliteCache is null)
        {
            return Observable.Return(Unit.Default);
        }

        var v10Path = GetV10DatabasePath(builder, cacheName);
        return v10Path is null
            ? Observable.Return(Unit.Default)
            : V10MigrationService.Migrate(v10Path, sqliteCache, serializer, options);
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
        if (directory is null || string.IsNullOrWhiteSpace(directory))
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
        return directory is null || string.IsNullOrWhiteSpace(directory) ? null : Path.Combine(directory, V10FileNameMap.GetV10FileName(cacheName));
    }

    /// <summary>
    /// Unwraps known secure cache wrappers to retrieve the underlying <see cref="IBlobCache"/>.
    /// </summary>
    /// <param name="secureBlobCache">The secure cache to unwrap.</param>
    /// <returns>The underlying blob cache, or <c>null</c> if none can be resolved.</returns>
    internal static IBlobCache? GetUnderlyingBlobCache(ISecureBlobCache? secureBlobCache) => secureBlobCache switch
    {
        IWrappedBlobCache wrappedBlobCache => wrappedBlobCache.InnerCache,
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
}
