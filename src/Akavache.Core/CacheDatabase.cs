// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>
/// CacheDatabase is the main entry point for interacting with Akavache. It provides
/// convenient static properties for accessing common cache locations.
/// This V11 implementation uses a builder pattern for configuration.
/// </summary>
public static class CacheDatabase
{
    /// <summary>The currently configured Akavache instance.</summary>
    private static IAkavacheInstance? _instance;

    /// <summary>Gets or sets the Scheduler used for task pools.</summary>
    public static ISequencer TaskpoolScheduler
    {
        get => field ?? TaskPoolSequencer.Default;
        set;
    }

    /// <summary>Gets the application name used for cache file paths.</summary>
    public static string ApplicationName => GetOrThrowIfNotInitialized().ApplicationName
        ?? throw new InvalidOperationException("CacheDatabase has not been initialized. Call CacheDatabase.Initialize() first.");

    /// <summary>Gets a value indicating whether CacheDatabase has been initialized.</summary>
    public static bool IsInitialized => Volatile.Read(ref _instance) is not null;

    /// <summary>
    /// Gets the forced DateTime kind for DateTime serialization.
    /// When set, all DateTime values will be converted to this kind during cache operations.
    /// </summary>
    public static DateTimeKind? ForcedDateTimeKind => GetOrThrowIfNotInitialized().ForcedDateTimeKind
        ?? throw new InvalidOperationException("CacheDatabase has not been initialized. Call CacheDatabase.Initialize() first.");

    /// <summary>
    /// Gets the InMemory cache instance. This cache stores data only in memory
    /// and is lost when the application shuts down. Useful for temporary data
    /// and session state.
    /// </summary>
    public static IBlobCache InMemory => GetOrThrowIfNotInitialized().InMemory
        ?? throw new InvalidOperationException("InMemory cache has not been configured on the current Akavache instance.");

    /// <summary>
    /// Gets the LocalMachine cache instance. This cache persists data but is suitable
    /// for temporary/cached data that can be safely deleted. On mobile platforms,
    /// the system may delete this data to free up disk space.
    /// </summary>
    public static IBlobCache LocalMachine => GetOrThrowIfNotInitialized().LocalMachine
        ?? throw new InvalidOperationException("LocalMachine cache has not been configured on the current Akavache instance.");

    /// <summary>Gets the Secure cache instance. This cache provides encrypted storage for sensitive data like credentials and API keys.</summary>
    public static ISecureBlobCache Secure => GetOrThrowIfNotInitialized().Secure
        ?? throw new InvalidOperationException("Secure cache has not been configured on the current Akavache instance.");

    /// <summary>
    /// Gets the UserAccount cache instance. This cache persists data and is suitable
    /// for storing user settings and preferences that should survive app restarts.
    /// On some platforms, this data may be backed up to the cloud.
    /// </summary>
    public static IBlobCache UserAccount => GetOrThrowIfNotInitialized().UserAccount
        ?? throw new InvalidOperationException("UserAccount cache has not been configured on the current Akavache instance.");

    /// <summary>
    /// Gets the current instance of the Akavache cache database.
    /// This instance provides access to blob caches and settings stores managed by Akavache.
    /// </summary>
    public static IAkavacheInstance? CurrentInstance => Volatile.Read(ref _instance);

    /// <summary>
    /// Shuts down all cache instances and flushes any pending operations.
    /// This should be called before the application terminates to ensure
    /// all data is properly saved.
    /// </summary>
    /// <returns>An observable that completes when shutdown is finished.</returns>
    public static IObservable<RxVoid> Shutdown()
    {
        // Snapshot to avoid concurrent modification issues during shutdown.
        var instance = Volatile.Read(ref _instance);
        if (instance is null)
        {
            return ImmutableReturnRxVoidSignal.Instance;
        }

        // Dispose registered blob caches and settings stores synchronously.
        // Observable.Start would schedule on the threadpool and deadlock when
        // callers block with WaitForCompletion under threadpool saturation.
        foreach (var cache in instance.BlobCaches.Values)
        {
            cache.Dispose();
        }

        foreach (var store in instance.SettingsStores.Values)
        {
            store.Dispose();
        }

        try
        {
            List<IObservable<RxVoid>> flushTasks =
            [
                FlushOrComplete(instance.UserAccount),
                FlushOrComplete(instance.LocalMachine),
                FlushOrComplete(instance.Secure),
                FlushOrComplete(instance.InMemory),
            ];

            return flushTasks.RunAll();
        }
        catch (Exception ex)
        {
            return new ImmediateThrowSignal<RxVoid>(ex);
        }
    }

    /// <summary>Initializes CacheDatabase with default in-memory caches and a required application name.</summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Failed to create an AkavacheBuilder instance.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static void Initialize<T>(string applicationName)
        where T : class, ISerializer, new() =>
        Initialize<T>(applicationName, FileLocationOption.Default);

    /// <summary>Initializes CacheDatabase with default in-memory caches and a required application name.</summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Failed to create an AkavacheBuilder instance.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static void Initialize<T>(string applicationName, FileLocationOption fileLocationOption)
       where T : class, ISerializer, new() => SetBuilder(CreateBuilder(applicationName, fileLocationOption)
            .WithSerializer<T>()
            .WithInMemoryDefaults()
            .Build());

    /// <summary>Initializes CacheDatabase with default in-memory caches, a required application name, and a custom serializer factory.</summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configureSerializer">The Serializer configuration.</param>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static void Initialize<T>(Func<T> configureSerializer, string applicationName)
        where T : class, ISerializer, new() =>
        Initialize(configureSerializer, applicationName, FileLocationOption.Default);

    /// <summary>Initializes CacheDatabase with default in-memory caches, a required application name, and a custom serializer factory.</summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configureSerializer">The Serializer configuration.</param>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static void Initialize<T>(Func<T> configureSerializer, string applicationName, FileLocationOption fileLocationOption)
       where T : class, ISerializer, new() => SetBuilder(CreateBuilder(applicationName, fileLocationOption)
            .WithSerializer(configureSerializer)
            .WithInMemoryDefaults()
            .Build());

    /// <summary>Initializes CacheDatabase with a custom builder configuration and a required application name.</summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configure">An action to configure the Akavache builder.</param>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static void Initialize<T>(Action<IAkavacheBuilder> configure, string applicationName)
        where T : class, ISerializer, new() =>
        Initialize<T>(configure, applicationName, FileLocationOption.Default);

    /// <summary>Initializes CacheDatabase with a custom builder configuration and a required application name.</summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configure">An action to configure the Akavache builder.</param>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Type parameter appears in no parameter",
        Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static void Initialize<T>(Action<IAkavacheBuilder> configure, string applicationName, FileLocationOption fileLocationOption)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(configure);

        var builder = CreateBuilder(applicationName, fileLocationOption)
            .WithSerializer<T>();

        configure(builder);

        SetBuilder(builder.Build());
    }

    /// <summary>Initializes CacheDatabase with a custom builder configuration and a required application name.</summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configureSerializer">The Serializer configuration.</param>
    /// <param name="configure">An action to configure the Akavache builder.</param>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static void Initialize<T>(Func<T> configureSerializer, Action<IAkavacheBuilder> configure, string applicationName)
        where T : class, ISerializer, new() =>
        Initialize(configureSerializer, configure, applicationName, FileLocationOption.Default);

    /// <summary>Initializes CacheDatabase with a custom builder configuration and a required application name.</summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configureSerializer">The Serializer configuration.</param>
    /// <param name="configure">An action to configure the Akavache builder.</param>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static void Initialize<T>(Func<T> configureSerializer, Action<IAkavacheBuilder> configure, string applicationName, FileLocationOption fileLocationOption)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(configure);

        var builder = CreateBuilder(applicationName, fileLocationOption)
            .WithSerializer(configureSerializer);

        configure(builder);

        SetBuilder(builder.Build());
    }

    /// <summary>Creates a new Akavache builder for configuration with a required application name.</summary>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <returns>
    /// A new Akavache builder instance with the application name already applied.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAkavacheBuilder CreateBuilder(string applicationName) =>
        CreateBuilder(applicationName, FileLocationOption.Default);

    /// <summary>Creates a new Akavache builder for configuration with a required application name.</summary>
    /// <param name="applicationName">The application name for cache directories. Must not be null or whitespace.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <returns>
    /// A new Akavache builder instance with the application name already applied.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    public static IAkavacheBuilder CreateBuilder(string applicationName, FileLocationOption fileLocationOption) =>
        string.IsNullOrWhiteSpace(applicationName)
            ? throw new ArgumentException("Application name must not be null or whitespace.", nameof(applicationName))
            : new AkavacheBuilder(fileLocationOption).WithApplicationName(applicationName);

    /// <summary>Creates a new Akavache builder for configuration.</summary>
    /// <returns>
    /// A new Akavache builder instance.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use CreateBuilder(string applicationName, ...) which requires an explicit application name.", false)]
    [SuppressMessage(
        "Design",
        "SST2310:Deprecated member should be removed once its last caller is gone",
        Justification = "Retained deliberately for one release so consumers can migrate to the overload that requires an application name.")]
    public static IAkavacheBuilder CreateBuilder() =>
        CreateBuilder(FileLocationOption.Default);

    /// <summary>Creates a new Akavache builder for configuration.</summary>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <returns>
    /// A new Akavache builder instance.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use CreateBuilder(string applicationName, ...) which requires an explicit application name.", false)]
    [SuppressMessage(
        "Design",
        "SST2310:Deprecated member should be removed once its last caller is gone",
        Justification = "Retained deliberately for one release so consumers can migrate to the overload that requires an application name.")]
    public static IAkavacheBuilder CreateBuilder(FileLocationOption fileLocationOption) => new AkavacheBuilder(fileLocationOption);

    /// <summary>Resets all CacheDatabase state for testing purposes.</summary>
    /// <returns>An observable that completes once shutdown and reset are done.</returns>
    internal static IObservable<RxVoid> ResetForTests()
    {
        var shutdown = Volatile.Read(ref _instance) is not null
            ? Shutdown().Catch<RxVoid, Exception>(static _ => ImmutableReturnRxVoidSignal.Instance)
            : ImmutableReturnRxVoidSignal.Instance;

        return shutdown.Do(static _ =>
        {
            Volatile.Write(ref _instance, null);
            TaskpoolScheduler = null!;
        });
    }

    /// <summary>
    /// Flushes <paramref name="cache"/>, or hands back an already-completed observable when the
    /// instance has no cache configured in that slot. Keeps <see cref="Shutdown"/> free of the
    /// per-slot null handling.
    /// </summary>
    /// <param name="cache">The cache occupying one of the instance's four slots, if any.</param>
    /// <returns>An observable that completes once the flush finishes.</returns>
    internal static IObservable<RxVoid> FlushOrComplete(IBlobCache? cache) =>
        cache?.Flush() ?? ImmutableReturnRxVoidSignal.Instance;

    /// <summary>Internal method to set the instance.</summary>
    /// <param name="builder">The configured instance.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetBuilder(IAkavacheInstance builder) =>
        Volatile.Write(ref _instance, builder);

    /// <summary>Returns the configured <see cref="IAkavacheInstance"/>, throwing if not initialized.</summary>
    /// <returns>The configured Akavache instance.</returns>
    /// <exception cref="InvalidOperationException">The cache database has not been initialized.</exception>
    internal static IAkavacheInstance GetOrThrowIfNotInitialized() =>
        Volatile.Read(ref _instance)
            ?? throw new InvalidOperationException(
                "CacheDatabase has not been initialized. "
                + "Call CacheDatabase.Initialize<TSerializer>(\"MyApp\") or "
                + "CacheDatabase.Initialize<TSerializer>(builder => { ... }, \"MyApp\") first. "
                + "For advanced scenarios, use CacheDatabase.CreateBuilder(\"MyApp\") to configure custom cache implementations.");
}
