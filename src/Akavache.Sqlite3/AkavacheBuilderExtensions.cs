// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Helpers;

using SQLitePCL;

namespace Akavache.Sqlite3;

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

    /// <summary>
    /// Configures the builder to use the SQLite provider for persistent data storage.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static IAkavacheBuilder WithSqliteProvider(this IAkavacheBuilder builder)
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

        // Ensure SQLitePCL is initialized only once
        if (_sqliteProvider != null)
        {
            return builder;
        }

        Batteries_V2.Init();
        _sqliteProvider = true;
        return builder;
    }

    /// <summary>
    /// Configures default SQLite-based caches for all cache types.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    public static IAkavacheBuilder WithSqliteDefaults(this IAkavacheBuilder builder)
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

        // For backward compatibility, automatically initialize the SQLite provider if not already done
        if (_sqliteProvider == null)
        {
            builder.WithSqliteProvider();
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

        // Create SQLite caches for persistent storage
        builder.WithUserAccount(CreateSqliteCache(UserAccount, builder))
               .WithLocalMachine(CreateSqliteCache(LocalMachine, builder))
               .WithInMemory()
               .WithSecure(new SecureBlobCacheWrapper(CreateSqliteCache(Secure, builder)));

        return builder;
    }

    /// <summary>
    /// Resets the SQLite provider state for testing purposes.
    /// </summary>
    internal static void ResetSqliteProviderForTests() => _sqliteProvider = null;

    /// <summary>
    /// Creates a <see cref="SqliteBlobCache"/> for the specified cache name using the builder's serializer and directory configuration.
    /// </summary>
    /// <param name="name">The logical cache name (e.g. <c>UserAccount</c>, <c>LocalMachine</c>, <c>Secure</c>).</param>
    /// <param name="builder">The Akavache builder supplying serializer, application name, and file location options.</param>
    /// <returns>A configured <see cref="SqliteBlobCache"/>.</returns>
    internal static SqliteBlobCache CreateSqliteCache(string name, IAkavacheBuilder builder)
    {
        var serializer = builder.Serializer
            ?? throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using SQLite caches.");

        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(name);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(builder.ApplicationName);

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
        var cache = new SqliteBlobCache(filePath, serializer);

        if (builder.ForcedDateTimeKind.HasValue)
        {
            cache.ForcedDateTimeKind = builder.ForcedDateTimeKind.Value;
        }

        return cache;
    }
}
