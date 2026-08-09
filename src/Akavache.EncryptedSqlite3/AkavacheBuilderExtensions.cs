// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using SQLitePCL;

#if REACTIVE_SHIM
namespace Akavache.Reactive.EncryptedSqlite3;
#else
namespace Akavache.EncryptedSqlite3;
#endif

/// <summary>Provides extension methods for configuring Akavache to use encrypted SQLite-based blob caches.</summary>
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
        /// <summary>Configures the builder to use the encrypted SQLite provider for secure data storage.</summary>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public IAkavacheBuilder WithEncryptedSqliteProvider()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            if (_sqliteProvider is not null)
            {
                return builder;
            }

            Batteries_V2.Init();
            _sqliteProvider = true;
            return builder;
        }

        /// <summary>Configures default SQLite-based caches for all cache types.</summary>
        /// <param name="password">The password.</param>
        /// <returns>
        /// The builder instance for fluent configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">builder.</exception>
        /// <exception cref="InvalidOperationException">
        /// No serializer has been registered. Call CacheDatabase.Serializer = new [SerializerType]() before using SQLite defaults.
        /// or
        /// Application name must be set before configuring SQLite defaults. Call WithApplicationName() first.
        /// </exception>
        public IAkavacheBuilder WithSqliteDefaults(string password)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            if (_sqliteProvider is null)
            {
                _ = builder.WithEncryptedSqliteProvider();
            }

            if (builder.Serializer is null)
            {
                throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using SQLite defaults.");
            }

            if (string.IsNullOrWhiteSpace(builder.ApplicationName))
            {
                throw new InvalidOperationException("Application name must be set before configuring SQLite defaults. Call WithApplicationName() first.");
            }

            _ = builder.WithUserAccount(EncryptedSqliteCacheFactory.CreateEncryptedSqliteCache(UserAccount, builder, password))
                   .WithLocalMachine(EncryptedSqliteCacheFactory.CreateEncryptedSqliteCache(LocalMachine, builder, password))
                   .WithInMemory()
                   .WithSecure(EncryptedSqliteCacheFactory.CreateEncryptedSqliteCache(Secure, builder, password));

            return builder;
        }
    }

    /// <summary>Resets the SQLite provider state for testing purposes.</summary>
    internal static void ResetSqliteProviderForTests() => _sqliteProvider = null;
}
