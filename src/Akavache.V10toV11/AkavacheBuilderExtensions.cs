// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.V10toV11;
#else
namespace Akavache.V10toV11;
#endif

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

    /// <summary>Extension members for <c>IAkavacheBuilder</c>.</summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    extension(IAkavacheBuilder builder)
    {
        /// <summary>
        /// Configures the builder to use V10-era database filenames (blobs.db, userblobs.db, secret.db)
        /// at the legacy directory locations. This allows V11 to find and read existing V10 databases in-place.
        /// New writes will use the V11 CacheEntry table within the same database file, while old data
        /// in the V10 CacheElement table is read transparently via the built-in legacy shim.
        /// </summary>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no serializer has been registered.</exception>
        public IAkavacheBuilder WithV10FileNames()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            if (builder.Serializer is null)
            {
                throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using V10 file names.");
            }

            V10MigrationHelpers.ValidateApplicationName(builder.ApplicationName);

            // Ensure legacy file location is set so directories resolve to V10 paths
            if (builder.FileLocationOption != FileLocationOption.Legacy)
            {
                _ = builder.WithLegacyFileLocation();
            }

            // Create caches using V10 filenames at legacy directory locations
            _ = builder.WithUserAccount(V10MigrationHelpers.CreateV10Cache(UserAccount, builder))
                   .WithLocalMachine(V10MigrationHelpers.CreateV10Cache(LocalMachine, builder))
                   .WithInMemory()
                   .WithSecure(new SecureBlobCacheWrapper(V10MigrationHelpers.CreateV10Cache(Secure, builder)));

            return builder;
        }

        /// <summary>
        /// Performs a one-time migration of data from V10 database files into the current V11 databases.
        /// This method should be called AFTER <c>WithSqliteDefaults()</c> so that V11 databases have been created.
        /// The migration reads all entries from the V10 CacheElement table, converts them to V11 CacheEntry format,
        /// and inserts them into the V11 databases. A sentinel key prevents re-migration on subsequent runs.
        /// </summary>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when V11 caches have not been configured yet.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
        public IAkavacheBuilder MigrateFromV10() =>
            builder.MigrateFromV10((Action<V10MigrationOptions>?)null);

        /// <summary>
        /// Performs a one-time migration of data from V10 database files into the current V11 databases.
        /// This method should be called AFTER <c>WithSqliteDefaults()</c> so that V11 databases have been created.
        /// The migration reads all entries from the V10 CacheElement table, converts them to V11 CacheEntry format,
        /// and inserts them into the V11 databases. A sentinel key prevents re-migration on subsequent runs.
        /// </summary>
        /// <param name="configure">Optional configuration for migration behavior.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when V11 caches have not been configured yet.</exception>
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
        public IAkavacheBuilder MigrateFromV10(Action<V10MigrationOptions>? configure)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            if (builder.Serializer is null)
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
            var pipeline = V10MigrationHelpers.BuildMigration(builder, UserAccount, options.MigrateUserAccount, builder.UserAccount as SqliteBlobCache, serializer, options)
                .Concat(V10MigrationHelpers.BuildMigration(builder, LocalMachine, options.MigrateLocalMachine, builder.LocalMachine as SqliteBlobCache, serializer, options))
                .Concat(V10MigrationHelpers.BuildMigration(builder, Secure, options.MigrateSecure, V10MigrationHelpers.GetUnderlyingBlobCache(builder.Secure) as SqliteBlobCache, serializer, options));

            pipeline.WaitForCompletion();

            return builder;
        }
    }
}
