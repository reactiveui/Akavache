// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Akavache.Core;
using Akavache.Settings.Core;

#if ENCRYPTED
using Akavache.EncryptedSqlite3;
#else
using Akavache.Sqlite3;
#endif

#if ENCRYPTED
namespace Akavache.EncryptedSettings;
#else
namespace Akavache.Settings;
#endif

/// <summary>
/// App Info.
/// </summary>
public static class AppInfo
{
    static AppInfo()
    {
        SettingsStores = [];
        BlobCaches = [];
        ExecutingAssemblyName = ExecutingAssembly.FullName!.Split(',')[0];
        ApplicationRootPath = Path.Combine(Path.GetDirectoryName(ExecutingAssembly.Location)!, "..");
        SettingsCachePath = Path.Combine(ApplicationRootPath, "SettingsCache");
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(ExecutingAssembly.Location);
        Version = new(fileVersionInfo.ProductMajorPart, fileVersionInfo.ProductMinorPart, fileVersionInfo.ProductBuildPart, fileVersionInfo.ProductPrivatePart);
    }

    /// <summary>
    /// Gets the application root path.
    /// </summary>
    /// <value>
    /// The application root path.
    /// </value>
    public static string? ApplicationRootPath { get; }

    /// <summary>
    /// Gets the settings cache path.
    /// </summary>
    /// <value>
    /// The settings cache path.
    /// </value>
    public static string? SettingsCachePath { get; private set; }

    /// <summary>
    /// Gets the executing assembly.
    /// </summary>
    /// <value>
    /// The executing assembly.
    /// </value>
    public static Assembly ExecutingAssembly => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

    /// <summary>
    /// Gets the name of the executing assembly.
    /// </summary>
    /// <value>
    /// The name of the executing assembly.
    /// </value>
    public static string? ExecutingAssemblyName { get; }

    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <value>
    /// The version.
    /// </value>
    public static Version? Version { get; }

    /// <summary>
    /// Gets or sets the serializer.
    /// </summary>
    /// <value>
    /// The serializer.
    /// </value>
    public static ISerializer? Serializer
    {
        get => CoreRegistrations.Serializer;
        set
        {
            // Check if the new type is the same as the existing one, if so, do nothing.
            if (CoreRegistrations.Serializer?.GetType() == value?.GetType())
            {
                return;
            }

            CoreRegistrations.Serializer = value;
        }
    }

    internal static Dictionary<string, IBlobCache?> BlobCaches { get; }

    internal static Dictionary<string, ISettingsStorage?> SettingsStores { get; }

    /// <summary>
    /// Overrides the settings cache path.
    /// </summary>
    /// <param name="path">The path.</param>
    public static void OverrideSettingsCachePath(string path) => SettingsCachePath = path;

    /// <summary>
    /// Deletes the settings store.
    /// </summary>
    /// <typeparam name="T">The type of store to delete.</typeparam>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>
    /// A Task.
    /// </returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("DeleteSettingsStore requires types to be preserved for settings store management.")]
    [RequiresDynamicCode("DeleteSettingsStore requires types to be preserved for settings store management.")]
#endif
    public static async Task DeleteSettingsStore<T>(string? overrideDatabaseName = null)
    {
        await DisposeSettingsStore<T>().ConfigureAwait(false);
        File.Delete(Path.Combine(SettingsCachePath!, $"{overrideDatabaseName ?? typeof(T).Name}.db"));
    }

    /// <summary>
    /// Gets the settings store.
    /// </summary>
    /// <typeparam name="T">The store to get.</typeparam>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>
    /// A Settings Store.
    /// </returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("GetSettingsStore requires types to be preserved for settings store retrieval.")]
    [RequiresDynamicCode("GetSettingsStore requires types to be preserved for settings store retrieval.")]
#endif
    public static ISettingsStorage? GetSettingsStore<T>(string? overrideDatabaseName = null)
    {
        if (SettingsStores.TryGetValue(overrideDatabaseName ?? typeof(T).Name, out var settings))
        {
            return settings;
        }

        return null;
    }

    /// <summary>
    /// Disposes the settings store.
    /// </summary>
    /// <typeparam name="T">The type of store.</typeparam>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>
    /// A Task.
    /// </returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("DisposeSettingsStore requires types to be preserved for settings store disposal.")]
    [RequiresDynamicCode("DisposeSettingsStore requires types to be preserved for settings store disposal.")]
#endif
    public static async Task DisposeSettingsStore<T>(string? overrideDatabaseName = null)
    {
        var settings = GetSettingsStore<T>(overrideDatabaseName);
        if (settings != null)
        {
            await settings.DisposeAsync().ConfigureAwait(false);
        }

        if (BlobCaches.TryGetValue(overrideDatabaseName ?? typeof(T).Name, out var cache))
        {
            if (cache == null)
            {
                return;
            }

            await cache.DisposeAsync().ConfigureAwait(false);
        }
    }

#if ENCRYPTED

    /// <summary>
    /// Setup the secure settings store.
    /// </summary>
    /// <typeparam name="T">The Type of settings store.</typeparam>
    /// <param name="password">Secure password.</param>
    /// <param name="initialise">Initialise the Settings values.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>
    /// The Settings store.
    /// </returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("SetupSettingsStore requires types to be preserved for settings store creation and initialization.")]
    [RequiresDynamicCode("SetupSettingsStore requires types to be preserved for settings store creation and initialization.")]
#endif
    public static async Task<T?> SetupSettingsStore<T>(string password, bool initialise = true, string? overrideDatabaseName = null)
        where T : ISettingsStorage?, new()
    {
        Directory.CreateDirectory(SettingsCachePath!);
        BlobCaches[typeof(T).Name] = new EncryptedSqliteBlobCache(Path.Combine(SettingsCachePath!, $"{overrideDatabaseName ?? typeof(T).Name}.db"), password);

        var viewSettings = new T();
        SettingsStores[typeof(T).Name] = viewSettings;
        if (initialise)
        {
            await viewSettings.InitializeAsync().ConfigureAwait(false);
        }

        return viewSettings;
    }

#else

    /// <summary>
    /// Setup the settings store.
    /// </summary>
    /// <typeparam name="T">The Type of settings store.</typeparam>
    /// <param name="initialise">Initialise the Settings values.</param>
    /// <param name="overrideDatabaseName">Name of the override database.</param>
    /// <returns>
    /// The Settings store.
    /// </returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("SetupSettingsStore requires types to be preserved for settings store creation and initialization.")]
    [RequiresDynamicCode("SetupSettingsStore requires types to be preserved for settings store creation and initialization.")]
#endif
    public static async Task<T?> SetupSettingsStore<T>(bool initialise = true, string? overrideDatabaseName = null)
        where T : ISettingsStorage?, new()
    {
        Directory.CreateDirectory(SettingsCachePath!);
        BlobCaches[typeof(T).Name] = new SqliteBlobCache(Path.Combine(SettingsCachePath!, $"{overrideDatabaseName ?? typeof(T).Name}.db"));

        var viewSettings = new T();
        SettingsStores[typeof(T).Name] = viewSettings;
        if (initialise)
        {
            await viewSettings.InitializeAsync().ConfigureAwait(false);
        }

        return viewSettings;
    }
#endif
}
