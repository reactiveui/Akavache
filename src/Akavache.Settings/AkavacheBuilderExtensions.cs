// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.EncryptedSqlite3;
using Akavache.Helpers;
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
        ArgumentExceptionHelper.ThrowIfNull(builder);

        builder.SettingsCachePath = path;
        return builder;
    }

    /// <summary>
    /// Deletes the settings store for the specified type, including both in-memory cache and persistent storage.
    /// Disposes any registered store/cache for the type, then deletes the <c>.db</c> file on disk.
    /// File deletion errors are swallowed — the store may not exist or may still be in use.
    /// </summary>
    /// <typeparam name="T">The settings type whose store should be deleted.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>A one-shot observable that completes when deletion is done.</returns>
    public static IObservable<Unit> DeleteSettingsStore<T>(
        this IAkavacheInstance builder,
        string? overrideDatabaseName = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

        return builder.DisposeSettingsStore<T>(overrideDatabaseName)
            .Do(_ =>
            {
                try
                {
                    if (builder.SettingsCachePath is not null && !string.IsNullOrEmpty(builder.SettingsCachePath) && Directory.Exists(builder.SettingsCachePath))
                    {
                        var databaseName = overrideDatabaseName ?? typeof(T).Name;
                        var validatedDatabaseName = SecurityUtilities.ValidateDatabaseName(databaseName, nameof(overrideDatabaseName));
                        var filePath = Path.Combine(builder.SettingsCachePath, $"{validatedDatabaseName}.db");
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                }
                catch
                {
                    // Best-effort cleanup: the store may not exist or may still be held by
                    // another handle. Swallowing matches the prior Task-returning contract.
                }
            });
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
        ArgumentExceptionHelper.ThrowIfNull(builder);

        var key = overrideDatabaseName ?? typeof(T).Name;
        return builder.SettingsStores.TryGetValue(key, out var store) ? store : null;
    }

    /// <summary>
    /// Disposes the settings store for the specified type, cleaning up both in-memory and persistent resources.
    /// Disposal runs in order: settings store first, then the underlying blob cache — each bridged
    /// so the whole teardown remains a single pure Rx pipeline.
    /// </summary>
    /// <typeparam name="T">The settings type whose store should be disposed.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>A one-shot observable that completes when disposal is done.</returns>
    public static IObservable<Unit> DisposeSettingsStore<T>(this IAkavacheInstance builder, string? overrideDatabaseName = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

        return Observable.Defer(() =>
        {
            var key = overrideDatabaseName ?? typeof(T).Name;
            var settings = builder.GetLoadedSettingsStore<T>(overrideDatabaseName);

            if (settings is not null)
            {
                settings.Dispose();
                builder.SettingsStores.Remove(key);
            }

            if (builder.BlobCaches.TryGetValue(key, out var cache))
            {
                cache.Dispose();
                builder.BlobCaches.Remove(key);
            }

            return Observable.Return(Unit.Default);
        });
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
        where T : class, ISettingsStorage, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

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
    public static T GetSecureSettingsStore<T>(this IAkavacheInstance builder, string password, string? overrideDatabaseName = null)
        where T : class, ISettingsStorage, new() =>
        builder.GetSecureSettingsStore<T>(password, overrideDatabaseName, scheduler: null);

    /// <summary>
    /// Gets or creates a secure encrypted settings store with password protection, optionally
    /// overriding the scheduler the underlying <see cref="EncryptedSqliteBlobCache"/> uses.
    /// Pass <see cref="ImmediateScheduler.Instance"/> from tests to avoid thread-pool hops
    /// on the initialization observable.
    /// </summary>
    /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="password">The password for encrypting the settings database.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <param name="scheduler">Scheduler to use for the underlying blob cache, or <see langword="null"/> for the default task-pool scheduler.</param>
    /// <returns>The settings store instance configured for secure storage.</returns>
    public static T GetSecureSettingsStore<T>(this IAkavacheInstance builder, string password, string? overrideDatabaseName, IScheduler? scheduler)
        where T : class, ISettingsStorage, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

        if (builder.Serializer == null)
        {
            throw new InvalidOperationException("AkavacheInstance serializer is not set. Ensure the builder has a serializer configured.");
        }

        var key = overrideDatabaseName ?? typeof(T).Name;

        // Validate database name to prevent path traversal attacks
        var validatedKey = SecurityUtilities.ValidateDatabaseName(key, nameof(overrideDatabaseName));

        Directory.CreateDirectory(builder.SettingsCachePath!);
        var dbPath = Path.Combine(builder.SettingsCachePath!, $"{validatedKey}.db");
        var cache = scheduler is not null
            ? new EncryptedSqliteBlobCache(dbPath, password, builder.Serializer, scheduler)
            : new EncryptedSqliteBlobCache(dbPath, password, builder.Serializer);
        builder.BlobCaches[validatedKey] = cache;

        T viewSettings;
        using (SettingsBase.PushAmbientCache(cache))
        {
            viewSettings = new();
        }

        builder.SettingsStores[validatedKey] = viewSettings;
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
        where T : class, ISettingsStorage, new() =>
        builder.WithSettingsStore(settings, overrideDatabaseName, scheduler: null);

    /// <summary>
    /// Configures a standard settings store backed by SQLite using the supplied
    /// <paramref name="scheduler"/>. Intended for test harnesses that want to avoid
    /// thread-pool scheduling on the cache initialization observable (pass
    /// <see cref="ImmediateScheduler.Instance"/>).
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="builder">The Akavache builder.</param>
    /// <param name="settings">Action to configure the settings instance once created.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <param name="scheduler">Scheduler to use for the underlying blob cache, or <see langword="null"/> for the default task-pool scheduler.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    public static IAkavacheBuilder WithSettingsStore<T>(this IAkavacheBuilder builder, Action<T?> settings, string? overrideDatabaseName, IScheduler? scheduler)
        where T : class, ISettingsStorage, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

        var settingsDb = builder.GetSettingsStore<T>(overrideDatabaseName, scheduler);
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
    public static T GetSettingsStore<T>(this IAkavacheInstance builder, string? overrideDatabaseName = null)
        where T : class, ISettingsStorage, new() =>
        builder.GetSettingsStore<T>(overrideDatabaseName, scheduler: null);

    /// <summary>
    /// Gets or creates a standard settings store using SQLite for persistence, optionally
    /// overriding the scheduler the underlying <see cref="SqliteBlobCache"/> uses.
    /// Pass <see cref="ImmediateScheduler.Instance"/> from tests to avoid thread-pool hops
    /// on the initialization observable.
    /// </summary>
    /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <param name="scheduler">Scheduler to use for the underlying blob cache, or <see langword="null"/> for the default task-pool scheduler.</param>
    /// <returns>The settings store instance configured for standard storage.</returns>
    public static T GetSettingsStore<T>(this IAkavacheInstance builder, string? overrideDatabaseName, IScheduler? scheduler)
        where T : class, ISettingsStorage, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

        if (builder.Serializer == null)
        {
            throw new InvalidOperationException("AkavacheInstance serializer is not set. Ensure the builder has a serializer configured.");
        }

        var key = overrideDatabaseName ?? typeof(T).Name;

        // Validate database name to prevent path traversal attacks
        var validatedKey = SecurityUtilities.ValidateDatabaseName(key, nameof(overrideDatabaseName));

        Directory.CreateDirectory(builder.SettingsCachePath!);
        var dbPath = Path.Combine(builder.SettingsCachePath!, $"{validatedKey}.db");
        var cache = scheduler is not null
            ? new SqliteBlobCache(dbPath, builder.Serializer, scheduler)
            : new SqliteBlobCache(dbPath, builder.Serializer);
        builder.BlobCaches[validatedKey] = cache;

        // Publish the just-created cache as the ambient cache while we construct the
        // settings type. SettingsBase's parameterless ctor reads the ambient slot first
        // so it doesn't have to hunt through CacheDatabase.CurrentInstance, which still
        // points at the previous build while this builder is being configured.
        T viewSettings;
        using (SettingsBase.PushAmbientCache(cache))
        {
            viewSettings = new();
        }

        builder.SettingsStores[validatedKey] = viewSettings;
        return viewSettings;
    }

    /// <summary>
    /// Configures a settings store using a custom <see cref="IBlobCache"/> instance and initializes it using the provided configuration action.
    /// This is useful for testing scenarios where an in-memory cache is preferred.
    /// </summary>
    /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <param name="cache">The custom blob cache instance to use for settings storage.</param>
    /// <param name="settings">Action to configure the settings instance once created.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="cache"/> is null.</exception>
    public static IAkavacheBuilder WithSettingsStore<T>(this IAkavacheBuilder builder, IBlobCache cache, Action<T?> settings, string? overrideDatabaseName = null)
        where T : class, ISettingsStorage, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

        ArgumentExceptionHelper.ThrowIfNull(cache);

        var settingsDb = builder.GetSettingsStore<T>(cache, overrideDatabaseName);
        settings?.Invoke(settingsDb);
        return builder;
    }

    /// <summary>
    /// Gets or creates a settings store using a custom <see cref="IBlobCache"/> instance.
    /// This is useful for testing scenarios where an in-memory cache is preferred.
    /// </summary>
    /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="cache">The custom blob cache instance to use for settings storage.</param>
    /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
    /// <returns>The settings store instance configured with the custom cache.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="cache"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when AkavacheBuilder has not been initialized.</exception>
    public static T GetSettingsStore<T>(this IAkavacheInstance builder, IBlobCache cache, string? overrideDatabaseName = null)
        where T : class, ISettingsStorage, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

        ArgumentExceptionHelper.ThrowIfNull(cache);

        var key = overrideDatabaseName ?? typeof(T).Name;

        // Validate database name to prevent path traversal attacks
        var validatedKey = SecurityUtilities.ValidateDatabaseName(key, nameof(overrideDatabaseName));

        builder.BlobCaches[validatedKey] = cache;

        T viewSettings;
        using (SettingsBase.PushAmbientCache(cache))
        {
            viewSettings = new();
        }

        builder.SettingsStores[validatedKey] = viewSettings;
        return viewSettings;
    }
}
