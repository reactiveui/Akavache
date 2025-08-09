// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Core;
using Akavache.EncryptedSqlite3;
using Akavache.Settings.Core;
using Akavache.Sqlite3;

namespace Akavache.Settings;

/// <summary>
/// BlobCacheBuilderExtensions.
/// </summary>
public static class BlobCacheBuilderExtensions
{
    /// <summary>
    /// Initializes static members of the <see cref="BlobCacheBuilderExtensions"/> class.
    /// </summary>
    static BlobCacheBuilderExtensions()
    {
        BlobCaches = [];
        SettingsStores = [];
    }

    internal static Dictionary<string, IBlobCache?> BlobCaches { get; }

    internal static Dictionary<string, ISettingsStorage?> SettingsStores { get; }

    /// <summary>
    /// Withes the settings cache path.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="path">The path.</param>
    /// <returns>IBlobCacheBuilder.</returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    public static IBlobCacheBuilder WithSettingsCachePath(this IBlobCacheBuilder builder, string path)
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
    public static async Task DeleteSettingsStore<T>(this IBlobCacheBuilder builder, string? overrideDatabaseName = null)
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
    /// Gets the settings store.
    /// </summary>
    /// <typeparam name="T">The store to get.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>
    /// A Settings Store.
    /// </returns>
    public static ISettingsStorage? GetSettingsStore<T>(this IBlobCacheBuilder builder, string? overrideDatabaseName = null)
    {
        var key = overrideDatabaseName ?? typeof(T).Name;
        if (SettingsStores.TryGetValue(key, out var settings))
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
    public static async Task DisposeSettingsStore<T>(this IBlobCacheBuilder builder, string? overrideDatabaseName = null)
    {
        var key = overrideDatabaseName ?? typeof(T).Name;
        var settings = builder.GetSettingsStore<T>(overrideDatabaseName);
        if (settings != null)
        {
            await settings.DisposeAsync().ConfigureAwait(false);
            SettingsStores.Remove(key);
        }

        if (BlobCaches.TryGetValue(key, out var cache))
        {
            if (cache != null)
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }

            BlobCaches.Remove(key);
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
    /// IBlobCacheBuilder for chaining.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    public static IBlobCacheBuilder WithSecureSettingsStore<T>(this IBlobCacheBuilder builder, string password, Action<T?> settings, string? overrideDatabaseName = null)
        where T : ISettingsStorage?, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var key = overrideDatabaseName ?? typeof(T).Name;
        Directory.CreateDirectory(builder.SettingsCachePath!);
        BlobCaches[key] = new EncryptedSqliteBlobCache(Path.Combine(builder.SettingsCachePath!, $"{key}.db"), password);

        var viewSettings = new T();
        SettingsStores[key] = viewSettings;
        settings?.Invoke(viewSettings);
        return builder;
    }

    /// <summary>
    /// Setups the settings store.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="settings">The new settings.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>IBlobCacheBuilder for chaining.</returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    public static IBlobCacheBuilder WithSettingsStore<T>(this IBlobCacheBuilder builder, Action<T?> settings, string? overrideDatabaseName = null)
        where T : ISettingsStorage?, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var key = overrideDatabaseName ?? typeof(T).Name;
        Directory.CreateDirectory(builder.SettingsCachePath!);
        BlobCaches[key] = new SqliteBlobCache(Path.Combine(builder.SettingsCachePath!, $"{key}.db"));

        var viewSettings = new T();
        SettingsStores[key] = viewSettings;
        settings?.Invoke(viewSettings);
        return builder;
    }
}
