// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Helpers;

using SQLitePCL;

namespace Akavache.EncryptedSqlite3;

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

            var applicationName = builder.ApplicationName;
            if (string.IsNullOrWhiteSpace(applicationName))
            {
                throw new InvalidOperationException("Application name must be set before configuring SQLite defaults. Call WithApplicationName() first.");
            }

            _ = builder.WithUserAccount(CreateEncryptedSqliteCache(UserAccount, builder, password))
                   .WithLocalMachine(CreateEncryptedSqliteCache(LocalMachine, builder, password))
                   .WithInMemory()
                   .WithSecure(CreateEncryptedSqliteCache(Secure, builder, password));

            return builder;
        }
    }

    /// <summary>Resets the SQLite provider state for testing purposes.</summary>
    internal static void ResetSqliteProviderForTests() => _sqliteProvider = null;

    /// <summary>Creates an <see cref="EncryptedSqliteBlobCache"/> for the specified cache name using the builder's serializer and directory configuration.</summary>
    /// <param name="name">The logical cache name (e.g. <c>UserAccount</c>, <c>LocalMachine</c>, <c>Secure</c>).</param>
    /// <param name="builder">The Akavache builder supplying serializer, application name, and file location options.</param>
    /// <param name="password">The password used to encrypt the SQLite database.</param>
    /// <returns>A configured <see cref="EncryptedSqliteBlobCache"/>.</returns>
    internal static EncryptedSqliteBlobCache CreateEncryptedSqliteCache(string name, IAkavacheBuilder builder, string password)
    {
        var serializer = builder.Serializer
            ?? throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using SQLite caches.");

        ArgumentValidation.ThrowIfNullOrWhiteSpace(name);
        ArgumentValidation.ThrowIfNullOrWhiteSpace(builder.ApplicationName);

        var validatedName = SecurityUtilities.ValidateCacheName(name, nameof(name));

        var directory = builder.FileLocationOption switch
        {
            FileLocationOption.Legacy => builder.GetLegacyCacheDirectory(validatedName),
            _ => builder.GetIsolatedCacheDirectory(validatedName),
        };

        if (!Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory!);
        }

        var filePath = Path.Combine(directory!, $"{validatedName}.db");
        var cache = new EncryptedSqliteBlobCache(filePath, password, serializer);
        if (builder.ForcedDateTimeKind.HasValue)
        {
            cache.ForcedDateTimeKind = builder.ForcedDateTimeKind.Value;
        }

        return cache;
    }
}
