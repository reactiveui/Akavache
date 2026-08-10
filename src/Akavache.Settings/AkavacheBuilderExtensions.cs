// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings;
#else
namespace Akavache.Settings;
#endif

/// <summary>Provides extension methods for configuring Akavache settings storage.</summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>Extension members for <c>IAkavacheBuilder</c>.</summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    extension(IAkavacheBuilder builder)
    {
        /// <summary>Configures the cache path for settings storage.</summary>
        /// <param name="path">The file system path where settings cache files will be stored.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public IAkavacheBuilder WithSettingsCachePath(string path)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            builder.SettingsCachePath = path;
            return builder;
        }

        /// <summary>Configures a secure settings store with password protection and initializes it using the provided configuration action.</summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="password">The password for encrypting the settings database.</param>
        /// <param name="settings">Action to configure the settings instance once created.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IAkavacheBuilder WithSecureSettingsStore<T>(string password, Action<T?> settings)
            where T : class, ISettingsStorage, new() =>
            builder.WithSecureSettingsStore(password, settings, (string?)null);

        /// <summary>Configures a secure settings store with password protection and initializes it using the provided configuration action.</summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="password">The password for encrypting the settings database.</param>
        /// <param name="settings">Action to configure the settings instance once created.</param>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public IAkavacheBuilder WithSecureSettingsStore<T>(string password, Action<T?> settings, string? overrideDatabaseName)
                where T : class, ISettingsStorage, new()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            var settingsDb = builder.GetSecureSettingsStore<T>(password, overrideDatabaseName);
            settings?.Invoke(settingsDb);
            return builder;
        }

        /// <summary>Configures a standard settings store and initializes it using the provided configuration action.</summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="settings">Action to configure the settings instance once created.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IAkavacheBuilder WithSettingsStore<T>(Action<T?> settings)
            where T : class, ISettingsStorage, new() =>
            builder.WithSettingsStore(settings, (string?)null);

        /// <summary>Configures a standard settings store and initializes it using the provided configuration action.</summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="settings">Action to configure the settings instance once created.</param>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IAkavacheBuilder WithSettingsStore<T>(Action<T?> settings, string? overrideDatabaseName)
                where T : class, ISettingsStorage, new() =>
                builder.WithSettingsStore(settings, overrideDatabaseName, scheduler: null);

        /// <summary>
        /// Configures a standard settings store backed by SQLite using the supplied
        /// <paramref name="scheduler"/>. Intended for test harnesses that want to avoid
        /// thread-pool scheduling on the cache initialization observable (pass
        /// <see cref="Sequencer.Immediate"/>).
        /// </summary>
        /// <typeparam name="T">The settings type.</typeparam>
        /// <param name="settings">Action to configure the settings instance once created.</param>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <param name="scheduler">Scheduler to use for the underlying blob cache, or <see langword="null"/> for the default task-pool scheduler.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        public IAkavacheBuilder WithSettingsStore<T>(Action<T?> settings, string? overrideDatabaseName, ISequencer? scheduler)
                where T : class, ISettingsStorage, new()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            var settingsDb = builder.GetSettingsStore<T>(overrideDatabaseName, scheduler);
            settings?.Invoke(settingsDb);
            return builder;
        }

        /// <summary>
        /// Configures a settings store using a custom <see cref="IBlobCache"/> instance and initializes it using the provided configuration action.
        /// This is useful for testing scenarios where an in-memory cache is preferred.
        /// </summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="cache">The custom blob cache instance to use for settings storage.</param>
        /// <param name="settings">Action to configure the settings instance once created.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="cache"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IAkavacheBuilder WithSettingsStore<T>(IBlobCache cache, Action<T?> settings)
            where T : class, ISettingsStorage, new() =>
            builder.WithSettingsStore(cache, settings, (string?)null);

        /// <summary>
        /// Configures a settings store using a custom <see cref="IBlobCache"/> instance and initializes it using the provided configuration action.
        /// This is useful for testing scenarios where an in-memory cache is preferred.
        /// </summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="cache">The custom blob cache instance to use for settings storage.</param>
        /// <param name="settings">Action to configure the settings instance once created.</param>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="cache"/> is null.</exception>
        public IAkavacheBuilder WithSettingsStore<T>(IBlobCache cache, Action<T?> settings, string? overrideDatabaseName)
                where T : class, ISettingsStorage, new()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(cache);

            var settingsDb = builder.GetSettingsStore<T>(cache, overrideDatabaseName);
            settings?.Invoke(settingsDb);
            return builder;
        }
    }

    /// <summary>Extension members for <c>IAkavacheInstance</c>.</summary>
    /// <param name="builder">The Akavache builder instance.</param>
    extension(IAkavacheInstance builder)
    {
        /// <summary>
        /// Deletes the settings store for the specified type, including both in-memory cache and persistent storage.
        /// Disposes any registered store/cache for the type, then deletes the <c>.db</c> file on disk.
        /// File deletion errors are swallowed — the store may not exist or may still be in use.
        /// </summary>
        /// <typeparam name="T">The settings type whose store should be deleted.</typeparam>
        /// <returns>A one-shot observable that completes when deletion is done.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public IObservable<RxVoid> DeleteSettingsStore<T>() =>
            builder.DeleteSettingsStore<T>((string?)null);

        /// <summary>
        /// Deletes the settings store for the specified type, including both in-memory cache and persistent storage.
        /// Disposes any registered store/cache for the type, then deletes the <c>.db</c> file on disk.
        /// File deletion errors are swallowed — the store may not exist or may still be in use.
        /// </summary>
        /// <typeparam name="T">The settings type whose store should be deleted.</typeparam>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <returns>A one-shot observable that completes when deletion is done.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public IObservable<RxVoid> DeleteSettingsStore<T>(
                string? overrideDatabaseName)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            return builder.DisposeSettingsStore<T>(overrideDatabaseName)
                .Do(_ =>
                {
                    try
                    {
                        if (builder.SettingsCachePath is not null && !string.IsNullOrEmpty(builder.SettingsCachePath) && Directory.Exists(builder.SettingsCachePath))
                        {
                            var validatedDatabaseName = SecurityUtilities.ValidateDatabaseName(overrideDatabaseName ?? typeof(T).Name, nameof(overrideDatabaseName));
                            var filePath = Path.Combine(builder.SettingsCachePath, $"{validatedDatabaseName}.db");
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                            }
                        }
                    }
                    catch (IOException)
                    {
                        // The file is missing or still held by another handle. Cleanup is
                        // best-effort, matching the prior Task-returning contract.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // The file is read-only or the process lacks rights to remove it; the
                        // caller asked for a delete, not a permissions escalation.
                    }
                    catch (ArgumentException)
                    {
                        // The override name failed validation, so there is no file this call
                        // could ever have removed. Deleting a store that cannot exist is not an
                        // error the caller needs to handle.
                    }
                });
        }

        /// <summary>Gets a settings store that has already been loaded into memory.</summary>
        /// <typeparam name="T">The settings type to retrieve.</typeparam>
        /// <returns>The loaded settings store instance, or <c>null</c> if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public ISettingsStorage? GetLoadedSettingsStore<T>() =>
            builder.GetLoadedSettingsStore<T>((string?)null);

        /// <summary>Gets a settings store that has already been loaded into memory.</summary>
        /// <typeparam name="T">The settings type to retrieve.</typeparam>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <returns>The loaded settings store instance, or <c>null</c> if not found.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public ISettingsStorage? GetLoadedSettingsStore<T>(string? overrideDatabaseName)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);
            return builder.SettingsStores.TryGetValue(overrideDatabaseName ?? typeof(T).Name, out var store) ? store : null;
        }

        /// <summary>
        /// Disposes the settings store for the specified type, cleaning up both in-memory and persistent resources.
        /// Disposal runs in order: settings store first, then the underlying blob cache — each bridged
        /// so the whole teardown remains a single pure Rx pipeline.
        /// </summary>
        /// <typeparam name="T">The settings type whose store should be disposed.</typeparam>
        /// <returns>A one-shot observable that completes when disposal is done.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public IObservable<RxVoid> DisposeSettingsStore<T>() =>
            builder.DisposeSettingsStore<T>((string?)null);

        /// <summary>
        /// Disposes the settings store for the specified type, cleaning up both in-memory and persistent resources.
        /// Disposal runs in order: settings store first, then the underlying blob cache — each bridged
        /// so the whole teardown remains a single pure Rx pipeline.
        /// </summary>
        /// <typeparam name="T">The settings type whose store should be disposed.</typeparam>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <returns>A one-shot observable that completes when disposal is done.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public IObservable<RxVoid> DisposeSettingsStore<T>(string? overrideDatabaseName)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            return Signal.Defer(() =>
            {
                var key = overrideDatabaseName ?? typeof(T).Name;
                var settings = builder.GetLoadedSettingsStore<T>(overrideDatabaseName);

                if (settings is not null)
                {
                    settings.Dispose();
                    _ = builder.SettingsStores.Remove(key);
                }

                if (builder.BlobCaches.TryGetValue(key, out var cache))
                {
                    cache.Dispose();
                    _ = builder.BlobCaches.Remove(key);
                }

                return ImmutableReturnRxVoidSignal.Instance;
            });
        }

        /// <summary>Gets or creates a secure encrypted settings store with password protection.</summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="password">The password for encrypting the settings database.</param>
        /// <returns>The settings store instance configured for secure storage.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when AkavacheBuilder has not been initialized or serializer is not configured.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public T GetSecureSettingsStore<T>(string password)
            where T : class, ISettingsStorage, new() =>
            builder.GetSecureSettingsStore<T>(password, (string?)null);

        /// <summary>Gets or creates a secure encrypted settings store with password protection.</summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="password">The password for encrypting the settings database.</param>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <returns>The settings store instance configured for secure storage.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when AkavacheBuilder has not been initialized or serializer is not configured.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public T GetSecureSettingsStore<T>(string password, string? overrideDatabaseName)
                where T : class, ISettingsStorage, new() =>
                builder.GetSecureSettingsStore<T>(password, overrideDatabaseName, scheduler: null);

        /// <summary>
        /// Gets or creates a secure encrypted settings store with password protection, optionally
        /// overriding the scheduler the underlying <see cref="EncryptedSqliteBlobCache"/> uses.
        /// Pass <see cref="Sequencer.Immediate"/> from tests to avoid thread-pool hops
        /// on the initialization observable.
        /// </summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="password">The password for encrypting the settings database.</param>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <param name="scheduler">Scheduler to use for the underlying blob cache, or <see langword="null"/> for the default task-pool scheduler.</param>
        /// <returns>The settings store instance configured for secure storage.</returns>
        /// <exception cref="InvalidOperationException">No serializer has been registered on the builder.</exception>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public T GetSecureSettingsStore<T>(string password, string? overrideDatabaseName, ISequencer? scheduler)
                where T : class, ISettingsStorage, new()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            if (builder.Serializer is null)
            {
                throw new InvalidOperationException("AkavacheInstance serializer is not set. Ensure the builder has a serializer configured.");
            }

            // Validate database name to prevent path traversal attacks
            var validatedKey = SecurityUtilities.ValidateDatabaseName(overrideDatabaseName ?? typeof(T).Name, nameof(overrideDatabaseName));

            _ = Directory.CreateDirectory(builder.SettingsCachePath!);
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

        /// <summary>Gets or creates a standard settings store using SQLite for persistence.</summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <returns>The settings store instance configured for standard storage.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when AkavacheBuilder has not been initialized or serializer is not configured.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public T GetSettingsStore<T>()
            where T : class, ISettingsStorage, new() =>
            builder.GetSettingsStore<T>((string?)null);

        /// <summary>Gets or creates a standard settings store using SQLite for persistence.</summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <returns>The settings store instance configured for standard storage.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when AkavacheBuilder has not been initialized or serializer is not configured.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public T GetSettingsStore<T>(string? overrideDatabaseName)
                where T : class, ISettingsStorage, new() =>
                builder.GetSettingsStore<T>(overrideDatabaseName, scheduler: null);

        /// <summary>
        /// Gets or creates a standard settings store using SQLite for persistence, optionally
        /// overriding the scheduler the underlying <see cref="SqliteBlobCache"/> uses.
        /// Pass <see cref="Sequencer.Immediate"/> from tests to avoid thread-pool hops
        /// on the initialization observable.
        /// </summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <param name="scheduler">Scheduler to use for the underlying blob cache, or <see langword="null"/> for the default task-pool scheduler.</param>
        /// <returns>The settings store instance configured for standard storage.</returns>
        /// <exception cref="InvalidOperationException">No serializer has been registered on the builder.</exception>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public T GetSettingsStore<T>(string? overrideDatabaseName, ISequencer? scheduler)
                where T : class, ISettingsStorage, new()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            if (builder.Serializer is null)
            {
                throw new InvalidOperationException("AkavacheInstance serializer is not set. Ensure the builder has a serializer configured.");
            }

            // Validate database name to prevent path traversal attacks
            var validatedKey = SecurityUtilities.ValidateDatabaseName(overrideDatabaseName ?? typeof(T).Name, nameof(overrideDatabaseName));

            _ = Directory.CreateDirectory(builder.SettingsCachePath!);
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
        /// Gets or creates a settings store using a custom <see cref="IBlobCache"/> instance.
        /// This is useful for testing scenarios where an in-memory cache is preferred.
        /// </summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="cache">The custom blob cache instance to use for settings storage.</param>
        /// <returns>The settings store instance configured with the custom cache.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="cache"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when AkavacheBuilder has not been initialized.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public T GetSettingsStore<T>(IBlobCache cache)
            where T : class, ISettingsStorage, new() =>
            builder.GetSettingsStore<T>(cache, (string?)null);

        /// <summary>
        /// Gets or creates a settings store using a custom <see cref="IBlobCache"/> instance.
        /// This is useful for testing scenarios where an in-memory cache is preferred.
        /// </summary>
        /// <typeparam name="T">The settings type that implements <see cref="ISettingsStorage"/>.</typeparam>
        /// <param name="cache">The custom blob cache instance to use for settings storage.</param>
        /// <param name="overrideDatabaseName">Optional override database name to use instead of the type name.</param>
        /// <returns>The settings store instance configured with the custom cache.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="cache"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when AkavacheBuilder has not been initialized.</exception>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public T GetSettingsStore<T>(IBlobCache cache, string? overrideDatabaseName)
                where T : class, ISettingsStorage, new()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(cache);

            // Validate database name to prevent path traversal attacks
            var validatedKey = SecurityUtilities.ValidateDatabaseName(overrideDatabaseName ?? typeof(T).Name, nameof(overrideDatabaseName));

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
}
