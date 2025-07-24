// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.Settings.Core;

#if ENCRYPTED

using ReactiveMarbles.CacheDatabase.EncryptedSqlite3;

#else
using ReactiveMarbles.CacheDatabase.Sqlite3;
#endif

using System.Diagnostics;
using System.Reflection;

#if ENCRYPTED

namespace ReactiveMarbles.CacheDatabase.EncryptedSettings
#else
namespace ReactiveMarbles.CacheDatabase.Settings
#endif
{
    /// <summary>
    /// App Info.
    /// </summary>
    public static class AppInfo
    {
        static AppInfo()
        {
            SettingsStores = new();
            BlobCaches = new();
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
        public static ISettingsStorage? GetSettingsStore<T>(string? overrideDatabaseName = null) =>
            SettingsStores[overrideDatabaseName ?? typeof(T).Name];

        /// <summary>
        /// Disposes the settings store.
        /// </summary>
        /// <typeparam name="T">The type of store.</typeparam>
        /// <param name="overrideDatabaseName">Name of the override database.</param>
        /// <returns>
        /// A Task.
        /// </returns>
        public static async Task DisposeSettingsStore<T>(string? overrideDatabaseName = null)
        {
            await GetSettingsStore<T>(overrideDatabaseName)!.DisposeAsync().ConfigureAwait(false);
            await BlobCaches[overrideDatabaseName ?? typeof(T).Name]!.DisposeAsync().ConfigureAwait(false);
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
}
