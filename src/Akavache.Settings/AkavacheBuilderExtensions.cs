// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.EncryptedSqlite3;
using Akavache.Sqlite3;

namespace Akavache.Settings;

/// <summary>
/// Provides extension methods for configuring Akavache settings storage.
/// </summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>
    /// Configures the cache path for settings storage.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <param name="path">The file system path where settings cache files will be stored.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static IAkavacheBuilder WithSettingsCachePath(this IAkavacheBuilder builder, string path)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.SettingsCachePath = path;
        return builder;
    }

    /// <summary>
    /// Deletes the settings store for the specified type, including both in-memory cache and persistent storage.
    /// </summary>
    /// <typeparam name="T">The settings type whose store should be deleted.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>A task representing the asynchronous deletion operation.</returns>
    public static async Task DeleteSettingsStore<T>(this IAkavacheInstance builder, string? overrideDatabaseName = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        await builder.DisposeSettingsStore<T>(overrideDatabaseName).ConfigureAwait(false);

        try
        {
            // Ensure the directory exists before attempting to delete the file
            if (!string.IsNullOrEmpty(builder.SettingsCachePath) && Directory.Exists(builder.SettingsCachePath))
            {
                // Validate database name to prevent path traversal attacks
                var databaseName = overrideDatabaseName ?? typeof(T).Name;
                var validatedDatabaseName = SecurityUtilities.ValidateDatabaseName(databaseName, nameof(overrideDatabaseName));
                var filePath = Path.Combine(builder.SettingsCachePath, $"{validatedDatabaseName}.db");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
        catch (Exception ex)
        {
            // Silently ignore file deletion errors - the store might not exist
            // or might be in use, which is acceptable for cleanup operations
            System.Diagnostics.Debug.WriteLine($"Error deleting settings store: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a settings store that has already been loaded into memory.
    /// </summary>
    /// <typeparam name="T">The settings type to retrieve.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>The loaded settings store instance, or <c>null</c> if not found.</returns>
    public static ISettingsStorage? GetLoadedSettingsStore<T>(this IAkavacheInstance builder, string? overrideDatabaseName = null)
    {
        if (AkavacheBuilder.SettingsStores == null)
        {
            return null;
        }

        var key = overrideDatabaseName ?? typeof(T).Name;
        if (AkavacheBuilder.SettingsStores.TryGetValue(key, out var settings))
        {
            return settings;
        }

        return null;
    }

    /// <summary>
    /// Disposes the settings store for the specified type, cleaning up both in-memory and persistent resources.
    /// </summary>
    /// <typeparam name="T">The settings type whose store should be disposed.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>A task representing the asynchronous disposal operation.</returns>
    public static async Task DisposeSettingsStore<T>(this IAkavacheInstance builder, string? overrideDatabaseName = null)
    {
        if (AkavacheBuilder.SettingsStores == null || AkavacheBuilder.BlobCaches == null)
        {
            return;
        }

        var key = overrideDatabaseName ?? typeof(T).Name;
        var settings = builder.GetLoadedSettingsStore<T>(overrideDatabaseName);
        if (settings != null)
        {
            await settings.DisposeAsync().ConfigureAwait(false);
            AkavacheBuilder.SettingsStores.Remove(key);
        }

        if (AkavacheBuilder.BlobCaches.TryGetValue(key, out var cache))
        {
            if (cache != null)
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }

            AkavacheBuilder.BlobCaches.Remove(key);
        }
    }

    /// <summary>
    /// Configures a secure settings store with password protection and initializes it using the provided configuration action.
    /// </summary>
    /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <param name="password">The password for encrypting the settings database.</param>
    /// <param name="settings">Action to configure the settings instance once created.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static IAkavacheBuilder WithSecureSettingsStore<T>(this IAkavacheBuilder builder, string password, Action<T?> settings, string? overrideDatabaseName = null)
        where T : ISettingsStorage?, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var settingsDb = builder.GetSecureSettingsStore<T>(password, overrideDatabaseName);
        settings?.Invoke(settingsDb);
        return builder;
    }

    /// <summary>
    /// Gets or creates a secure encrypted settings store with password protection.
    /// </summary>
    /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="password">The password for encrypting the settings database.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>The settings store instance configured for secure storage.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when AkavacheBuilder has not been initialized or serializer is not configured.</exception>
    public static T? GetSecureSettingsStore<T>(this IAkavacheInstance builder, string password, string? overrideDatabaseName = null)
        where T : ISettingsStorage?, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.Serializer == null)
        {
            throw new InvalidOperationException("AkavacheInstance serializer is not set. Ensure the builder has a serializer configured.");
        }

        if (AkavacheBuilder.SettingsStores == null || AkavacheBuilder.BlobCaches == null)
        {
            throw new InvalidOperationException("AkavacheBuilder has not been initialized. Call CacheDatabase.Initialize() first.");
        }

        var key = overrideDatabaseName ?? typeof(T).Name;

        // Validate database name to prevent path traversal attacks
        var validatedKey = SecurityUtilities.ValidateDatabaseName(key, nameof(overrideDatabaseName));

        Directory.CreateDirectory(builder.SettingsCachePath!);
        AkavacheBuilder.BlobCaches[validatedKey] = new EncryptedSqliteBlobCache(Path.Combine(builder.SettingsCachePath!, $"{validatedKey}.db"), password, builder.Serializer);

        var viewSettings = new T();
        AkavacheBuilder.SettingsStores[validatedKey] = viewSettings;
        return viewSettings;
    }

    /// <summary>
    /// Configures a standard settings store and initializes it using the provided configuration action.
    /// </summary>
    /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <param name="settings">Action to configure the settings instance once created.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static IAkavacheBuilder WithSettingsStore<T>(this IAkavacheBuilder builder, Action<T?> settings, string? overrideDatabaseName = null)
        where T : ISettingsStorage?, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var settingsDb = builder.GetSettingsStore<T>(overrideDatabaseName);
        settings?.Invoke(settingsDb);
        return builder;
    }

    /// <summary>
    /// Gets or creates a standard settings store using SQLite for persistence.
    /// </summary>
    /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>The settings store instance configured for standard storage.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when AkavacheBuilder has not been initialized or serializer is not configured.</exception>
    public static T? GetSettingsStore<T>(this IAkavacheInstance builder, string? overrideDatabaseName = null)
        where T : ISettingsStorage?, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.Serializer == null)
        {
            throw new InvalidOperationException("AkavacheInstance serializer is not set. Ensure the builder has a serializer configured.");
        }

        if (AkavacheBuilder.SettingsStores == null || AkavacheBuilder.BlobCaches == null)
        {
            throw new InvalidOperationException("AkavacheBuilder has not been initialized. Call CacheDatabase.Initialize() first.");
        }

        var key = overrideDatabaseName ?? typeof(T).Name;

        // Validate database name to prevent path traversal attacks
        var validatedKey = SecurityUtilities.ValidateDatabaseName(key, nameof(overrideDatabaseName));

        Directory.CreateDirectory(builder.SettingsCachePath!);
        AkavacheBuilder.BlobCaches[validatedKey] = new SqliteBlobCache(Path.Combine(builder.SettingsCachePath!, $"{validatedKey}.db"), builder.Serializer);

        var viewSettings = new T();
        AkavacheBuilder.SettingsStores[validatedKey] = viewSettings;
        return viewSettings;
    }
}
