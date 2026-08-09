// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using SQLitePCL;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Sqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>Provides extension methods for configuring Akavache to use SQLite-based blob caches.</summary>
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

    /// <summary>Extension members for <c>IAkavacheBuilder</c>.</summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    extension(IAkavacheBuilder builder)
    {
        /// <summary>Configures the builder to use the SQLite provider for persistent data storage.</summary>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public IAkavacheBuilder WithSqliteProvider()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            // Ensure SQLitePCL is initialized only once
            if (_sqliteProvider is not null)
            {
                return builder;
            }

            Batteries_V2.Init();
            _sqliteProvider = true;
            return builder;
        }

        /// <summary>Configures default SQLite-based caches for all cache types.</summary>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IAkavacheBuilder WithSqliteDefaults()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            // For backward compatibility, automatically initialize the SQLite provider if not already done
            if (_sqliteProvider is null)
            {
                _ = builder.WithSqliteProvider();
            }

            if (builder.Serializer is null)
            {
                throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using SQLite defaults.");
            }

            if (string.IsNullOrWhiteSpace(builder.ApplicationName))
            {
                throw new InvalidOperationException("Application name must be set before configuring SQLite defaults. Call WithApplicationName() first.");
            }

            // Create SQLite caches for persistent storage
            _ = builder.WithUserAccount(SqliteCacheFactory.CreateSqliteCache(UserAccount, builder))
                   .WithLocalMachine(SqliteCacheFactory.CreateSqliteCache(LocalMachine, builder))
                   .WithInMemory()
                   .WithSecure(new SecureBlobCacheWrapper(SqliteCacheFactory.CreateSqliteCache(Secure, builder)));

            return builder;
        }
    }

    /// <summary>Resets the SQLite provider state for testing purposes.</summary>
    internal static void ResetSqliteProviderForTests() => _sqliteProvider = null;
}
