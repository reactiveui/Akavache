// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Core;
using Akavache.Helpers;

namespace Akavache;

/// <summary>
/// CacheDatabase is the main entry point for interacting with Akavache. It provides
/// convenient static properties for accessing common cache locations.
/// This V11 implementation uses a builder pattern for configuration.
/// </summary>
public static class CacheDatabase
{
    /// <summary>The currently configured Akavache instance.</summary>
    private static IAkavacheInstance? _builder;

    /// <summary>Tracks whether <see cref="Initialize{T}(string, FileLocationOption)"/> has been called.</summary>
    private static bool _isInitialized;

    /// <summary>Optional override for the task pool scheduler.</summary>
    private static IScheduler? _taskPoolOverride;

    /// <summary>
    /// Gets or sets the Scheduler used for task pools.
    /// </summary>
    public static IScheduler TaskpoolScheduler
    {
        get => _taskPoolOverride ?? TaskPoolScheduler.Default;
        set => _taskPoolOverride = value;
    }

    /// <summary>
    /// Gets the application name used for cache file paths.
    /// </summary>
    public static string? ApplicationName => GetOrThrowIfNotInitialized().ApplicationName ??
        throw new InvalidOperationException("CacheDatabase has not been initialized. Call CacheDatabase.Initialize() first.");

    /// <summary>
    /// Gets a value indicating whether CacheDatabase has been initialized.
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the forced DateTime kind for DateTime serialization.
    /// When set, all DateTime values will be converted to this kind during cache operations.
    /// </summary>
    public static DateTimeKind? ForcedDateTimeKind => GetOrThrowIfNotInitialized().ForcedDateTimeKind ??
        throw new InvalidOperationException("CacheDatabase has not been initialized. Call CacheDatabase.Initialize() first.");

    /// <summary>
    /// Gets the InMemory cache instance. This cache stores data only in memory
    /// and is lost when the application shuts down. Useful for temporary data
    /// and session state.
    /// </summary>
    public static IBlobCache InMemory => GetOrThrowIfNotInitialized().InMemory ??
        throw new InvalidOperationException("InMemory cache has not been configured on the current Akavache instance.");

    /// <summary>
    /// Gets the LocalMachine cache instance. This cache persists data but is suitable
    /// for temporary/cached data that can be safely deleted. On mobile platforms,
    /// the system may delete this data to free up disk space.
    /// </summary>
    public static IBlobCache LocalMachine => GetOrThrowIfNotInitialized().LocalMachine ??
        throw new InvalidOperationException("LocalMachine cache has not been configured on the current Akavache instance.");

    /// <summary>
    /// Gets the Secure cache instance. This cache provides encrypted storage
    /// for sensitive data like credentials and API keys.
    /// </summary>
    public static ISecureBlobCache Secure => GetOrThrowIfNotInitialized().Secure ??
        throw new InvalidOperationException("Secure cache has not been configured on the current Akavache instance.");

    /// <summary>
    /// Gets the UserAccount cache instance. This cache persists data and is suitable
    /// for storing user settings and preferences that should survive app restarts.
    /// On some platforms, this data may be backed up to the cloud.
    /// </summary>
    public static IBlobCache UserAccount => GetOrThrowIfNotInitialized().UserAccount ??
        throw new InvalidOperationException("UserAccount cache has not been configured on the current Akavache instance.");

    /// <summary>
    /// Shuts down all cache instances and flushes any pending operations.
    /// This should be called before the application terminates to ensure
    /// all data is properly saved.
    /// </summary>
    /// <returns>An observable that completes when shutdown is finished.</returns>
    public static IObservable<Unit> Shutdown()
    {
        if (!_isInitialized || _builder == null)
        {
            return Observable.Return(Unit.Default);
        }

        List<IObservable<Unit>> shutdownTasks = [];

        // dispose the settings store
        if (AkavacheBuilder.BlobCaches != null)
        {
            var shutdownSettingsBlobs = Observable.Start(static async () =>
            {
                List<Task> tasks = AkavacheBuilder.BlobCaches
                .Where(static cachePair => cachePair.Value != null)
                .Select(static async cache => await cache.Value!.DisposeAsync())
                .ToList();
                await Task.WhenAll(tasks);
            }).Select(static _ => Unit.Default);
            shutdownTasks.Add(shutdownSettingsBlobs);
        }

        if (AkavacheBuilder.SettingsStores != null)
        {
            var shutdownSettingsStores = Observable.Start(static async () =>
            {
                List<Task> tasks = AkavacheBuilder.SettingsStores
                .Where(static cachePair => cachePair.Value != null)
                .Select(static async cache => await cache.Value!.DisposeAsync())
                .ToList();
                await Task.WhenAll(tasks);
            }).Select(static _ => Unit.Default);
            shutdownTasks.Add(shutdownSettingsStores);
        }

        try
        {
            shutdownTasks.Add(_builder.UserAccount?.Flush() ?? Observable.Return(Unit.Default));
            shutdownTasks.Add(_builder.LocalMachine?.Flush() ?? Observable.Return(Unit.Default));
            shutdownTasks.Add(_builder.Secure?.Flush() ?? Observable.Return(Unit.Default));
            shutdownTasks.Add(_builder.InMemory?.Flush() ?? Observable.Return(Unit.Default));
        }
        catch (Exception ex)
        {
            return Observable.Throw<Unit>(ex);
        }

        // The try block above unconditionally appends UserAccount/LocalMachine/Secure/InMemory
        // flush observables (each defaulting to Observable.Return on null), so the task list
        // is guaranteed to have at least four entries by this point.
        return shutdownTasks.Merge().TakeLast(1).Select(static _ => Unit.Default);
    }

    /// <summary>
    /// Initializes CacheDatabase with default in-memory caches and a required application name.
    /// </summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Failed to create AkavacheBuilder instance.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static void Initialize<T>(string applicationName, FileLocationOption fileLocationOption = FileLocationOption.Default)
       where T : class, ISerializer, new() => SetBuilder(CreateBuilder(applicationName, fileLocationOption)
            .WithSerializer<T>()
            .WithInMemoryDefaults()
            .Build());

    /// <summary>
    /// Initializes CacheDatabase with default in-memory caches, a required application name,
    /// and a custom serializer factory.
    /// </summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configureSerializer">The Serializer configuration.</param>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static void Initialize<T>(Func<T> configureSerializer, string applicationName, FileLocationOption fileLocationOption = FileLocationOption.Default)
       where T : class, ISerializer, new() => SetBuilder(CreateBuilder(applicationName, fileLocationOption)
            .WithSerializer(configureSerializer)
            .WithInMemoryDefaults()
            .Build());

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration and a required application name.
    /// </summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configure">An action to configure the Akavache builder.</param>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static void Initialize<T>(Action<IAkavacheBuilder> configure, string applicationName, FileLocationOption fileLocationOption = FileLocationOption.Default)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(configure);

        var builder = CreateBuilder(applicationName, fileLocationOption)
            .WithSerializer<T>();

        configure(builder);

        SetBuilder(builder.Build());
    }

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration and a required application name.
    /// </summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configureSerializer">The Serializer configuration.</param>
    /// <param name="configure">An action to configure the Akavache builder.</param>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static void Initialize<T>(Func<T> configureSerializer, Action<IAkavacheBuilder> configure, string applicationName, FileLocationOption fileLocationOption = FileLocationOption.Default)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(configure);

        var builder = CreateBuilder(applicationName, fileLocationOption)
            .WithSerializer(configureSerializer);

        configure(builder);

        SetBuilder(builder.Build());
    }

    /// <summary>
    /// Creates a new Akavache builder for configuration with a required application name.
    /// </summary>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <returns>
    /// A new Akavache builder instance with the application name already applied.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    public static IAkavacheBuilder CreateBuilder(string applicationName, FileLocationOption fileLocationOption = FileLocationOption.Default) =>
        string.IsNullOrWhiteSpace(applicationName)
            ? throw new ArgumentException("Application name must not be null or whitespace.", nameof(applicationName))
            : new AkavacheBuilder(fileLocationOption).WithApplicationName(applicationName);

    /// <summary>
    /// Creates a new Akavache builder for configuration.
    /// </summary>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <returns>
    /// A new Akavache builder instance.
    /// </returns>
    [Obsolete("Use CreateBuilder(string applicationName, ...) which requires an explicit application name.", false)]
    public static IAkavacheBuilder CreateBuilder(FileLocationOption fileLocationOption = FileLocationOption.Default) => new AkavacheBuilder(fileLocationOption);

    /// <summary>
    /// Resets all CacheDatabase state for testing purposes.
    /// Calls Shutdown first, then clears the builder and initialization flag.
    /// </summary>
    /// <returns>A task that completes once shutdown and reset are done.</returns>
    internal static async Task ResetForTestsAsync()
    {
        if (_isInitialized)
        {
            try
            {
                await Shutdown().LastOrDefaultAsync();
            }
            catch (Exception ex)
            {
                // Best-effort: shutdown may fail if caches are already disposed
                System.Diagnostics.Debug.WriteLine($"CacheDatabase.ResetForTests shutdown failed: {ex.Message}");
            }
        }

        _builder = null;
        _isInitialized = false;
        _taskPoolOverride = null;
    }

    /// <summary>
    /// Internal method to set the builder instance. Used by the builder pattern.
    /// </summary>
    /// <param name="builder">The configured builder instance.</param>
    internal static void SetBuilder(IAkavacheInstance builder)
    {
        _builder = builder;
        _isInitialized = true;
    }

    /// <summary>
    /// Returns the configured <see cref="IAkavacheInstance"/>, throwing
    /// <see cref="InvalidOperationException"/> if CacheDatabase has not been initialized.
    /// The return value is guaranteed non-null on success because <see cref="SetBuilder"/>
    /// always assigns a non-null instance and is the only writer for <c>_builder</c>.
    /// </summary>
    /// <returns>The configured Akavache instance.</returns>
    internal static IAkavacheInstance GetOrThrowIfNotInitialized() =>
        !_isInitialized || _builder is null
            ? throw new InvalidOperationException(
                "CacheDatabase has not been initialized. " +
                "Call CacheDatabase.Initialize<TSerializer>(\"MyApp\") or " +
                "CacheDatabase.Initialize<TSerializer>(builder => { ... }, \"MyApp\") first. " +
                "For advanced scenarios, use CacheDatabase.CreateBuilder(\"MyApp\") to configure custom cache implementations.")
            : _builder;
}
