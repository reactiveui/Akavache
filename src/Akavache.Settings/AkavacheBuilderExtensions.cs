// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.EncryptedSqlite3;
using Akavache.Sqlite3;
using SQLitePCL;

namespace Akavache.Settings;

/// <summary>
/// AkavacheBuilderExtensions.
/// </summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>
    /// Withes the settings cache path.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="path">The path.</param>
    /// <returns>IAkavacheBuilder.</returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
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
    /// Deletes the settings store.
    /// </summary>
    /// <typeparam name="T">The type of store to delete.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>
    /// A Task.
    /// </returns>
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
                var filePath = Path.Combine(builder.SettingsCachePath, $"{overrideDatabaseName ?? typeof(T).Name}.db");
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
    /// Gets a settings store that has already been loaded.
    /// </summary>
    /// <typeparam name="T">The store to get.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>
    /// A Settings Store.
    /// </returns>
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
    /// Disposes the settings store.
    /// </summary>
    /// <typeparam name="T">The type of store.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>
    /// A Task.
    /// </returns>
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
    /// Setups the settings store.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="password">The password.</param>
    /// <param name="settings">The new settings.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>
    /// IAkavacheBuilder for chaining.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    public static IAkavacheBuilder WithSecureSettingsStore<T>(this IAkavacheBuilder builder, string password, Action<T?> settings, string? overrideDatabaseName = null)
        where T : ISettingsStorage?, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        settings?.Invoke(builder.GetSecureSettingsStore<T>(password, overrideDatabaseName));
        return builder;
    }

    /// <summary>
    /// Gets the secure settings store.
    /// </summary>
    /// /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="password">The password.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>The new settings.</returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    /// <exception cref="System.InvalidOperationException">AkavacheBuilder has not been initialized. Call CacheDatabase.Initialize() first.</exception>
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
        Directory.CreateDirectory(builder.SettingsCachePath!);
        AkavacheBuilder.BlobCaches[key] = new EncryptedSqliteBlobCache(Path.Combine(builder.SettingsCachePath!, $"{key}.db"), password, builder.Serializer);

        var viewSettings = new T();
        AkavacheBuilder.SettingsStores[key] = viewSettings;
        return viewSettings;
    }

    /// <summary>
    /// Setups the settings store.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="settings">The new settings.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>IAkavacheBuilder for chaining.</returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    public static IAkavacheBuilder WithSettingsStore<T>(this IAkavacheBuilder builder, Action<T?> settings, string? overrideDatabaseName = null)
        where T : ISettingsStorage?, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        settings?.Invoke(builder.GetSettingsStore<T>(overrideDatabaseName));
        return builder;
    }

    /// <summary>
    /// Gets the settings store.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>The new settings.</returns>
    /// <exception cref="ArgumentNullException">nameof(builder).</exception>
    /// <exception cref="InvalidOperationException">AkavacheBuilder has not been initialized. Call CacheDatabase.Initialize() first.</exception>
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
        Directory.CreateDirectory(builder.SettingsCachePath!);
        AkavacheBuilder.BlobCaches[key] = new SqliteBlobCache(Path.Combine(builder.SettingsCachePath!, $"{key}.db"), builder.Serializer);

        var viewSettings = new T();
        AkavacheBuilder.SettingsStores[key] = viewSettings;
        return viewSettings;
    }
}
