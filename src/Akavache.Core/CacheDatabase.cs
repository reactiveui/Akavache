// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Core;

namespace Akavache;

/// <summary>
/// CacheDatabase is the main entry point for interacting with Akavache. It provides
/// convenient static properties for accessing common cache locations.
/// This V11 implementation uses a builder pattern for configuration.
/// </summary>
public static class CacheDatabase
{
    private static IAkavacheInstance? _builder;
    private static bool _isInitialized;
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
    public static string? ApplicationName => GetOrThrowIfNotInitialized()?.ApplicationName ??
        throw new InvalidOperationException("CacheDatabase has not been initialized. Call CacheDatabase.Initialize() first.");

    /// <summary>
    /// Gets a value indicating whether CacheDatabase has been initialized.
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the forced DateTime kind for DateTime serialization.
    /// When set, all DateTime values will be converted to this kind during cache operations.
    /// </summary>
    public static DateTimeKind? ForcedDateTimeKind => GetOrThrowIfNotInitialized()?.ForcedDateTimeKind ??
        throw new InvalidOperationException("CacheDatabase has not been initialized. Call CacheDatabase.Initialize() first.");

    /// <summary>
    /// Gets the InMemory cache instance. This cache stores data only in memory
    /// and is lost when the application shuts down. Useful for temporary data
    /// and session state.
    /// </summary>
    public static IBlobCache InMemory => GetOrThrowIfNotInitialized()?.InMemory ??
        throw new InvalidOperationException("CacheDatabase has not been initialized. Call CacheDatabase.Initialize() first.");

    /// <summary>
    /// Gets the LocalMachine cache instance. This cache persists data but is suitable
    /// for temporary/cached data that can be safely deleted. On mobile platforms,
    /// the system may delete this data to free up disk space.
    /// </summary>
    public static IBlobCache LocalMachine => GetOrThrowIfNotInitialized()?.LocalMachine ??
        throw new InvalidOperationException("CacheDatabase has not been initialized. Call CacheDatabase.Initialize() first.");

    /// <summary>
    /// Gets the Secure cache instance. This cache provides encrypted storage
    /// for sensitive data like credentials and API keys.
    /// </summary>
    public static ISecureBlobCache Secure => GetOrThrowIfNotInitialized()?.Secure ??
        throw new InvalidOperationException("CacheDatabase has not been initialized. Call CacheDatabase.Initialize() first.");

    /// <summary>
    /// Gets the UserAccount cache instance. This cache persists data and is suitable
    /// for storing user settings and preferences that should survive app restarts.
    /// On some platforms, this data may be backed up to the cloud.
    /// </summary>
    public static IBlobCache UserAccount => GetOrThrowIfNotInitialized()?.UserAccount ??
        throw new InvalidOperationException("CacheDatabase has not been initialized. Call CacheDatabase.Initialize() first.");

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

        var shutdownTasks = new List<IObservable<Unit>>();

        // dispose the settings store
        if (AkavacheBuilder.BlobCaches != null)
        {
            var shutdownSettingsBlobs = Observable.Start(static async () =>
            {
                var tasks = AkavacheBuilder.BlobCaches
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
                var tasks = AkavacheBuilder.SettingsStores
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

        return shutdownTasks.Count > 0
            ? shutdownTasks.Merge().TakeLast(1).Select(static _ => Unit.Default)
            : Observable.Return(Unit.Default);
    }

    /// <summary>
    /// Initializes CacheDatabase with default in-memory caches.
    /// This is the safest default as it doesn't require any additional packages.
    /// </summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="applicationName">The application name for cache directories. If null, uses the current ApplicationName.</param>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <exception cref="InvalidOperationException">Failed to create AkavacheBuilder instance.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static void Initialize<T>(string? applicationName = null, FileLocationOption fileLocationOption = FileLocationOption.Default)

       where T : ISerializer, new() => SetBuilder(CreateBuilder(fileLocationOption)
            .WithApplicationName(applicationName)
            .WithSerializer<T>()
            .WithInMemoryDefaults()
            .Build());

    /// <summary>
    /// Initializes CacheDatabase with default in-memory caches.
    /// This is the safest default as it doesn't require any additional packages.
    /// </summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configureSerializer">The Serializer configuration.</param>
    /// <param name="applicationName">The application name for cache directories. If null, uses the current ApplicationName.</param>
    /// <param name="fileLocationOption">The file location option.</param>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static void Initialize<T>(Func<T> configureSerializer, string? applicationName = null, FileLocationOption fileLocationOption = FileLocationOption.Default)
       where T : ISerializer, new() => SetBuilder(CreateBuilder(fileLocationOption)
            .WithApplicationName(applicationName)
            .WithSerializer(configureSerializer)
            .WithInMemoryDefaults()
            .Build());

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration.
    /// </summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configure">An action to configure the Akavache builder.</param>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="fileLocationOption">The file location option.</param>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static void Initialize<T>(Action<IAkavacheBuilder> configure, string? applicationName = null, FileLocationOption fileLocationOption = FileLocationOption.Default)
        where T : ISerializer, new()
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = CreateBuilder(fileLocationOption)
            .WithApplicationName(applicationName)
            .WithSerializer<T>();

        configure(builder);

        SetBuilder(builder.Build());
    }

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration.
    /// </summary>
    /// <typeparam name="T">The serializer.</typeparam>
    /// <param name="configureSerializer">The Serializer configuration.</param>
    /// <param name="configure">An action to configure the Akavache builder.</param>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="fileLocationOption">The file location option.</param>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static void Initialize<T>(Func<T> configureSerializer, Action<IAkavacheBuilder> configure, string? applicationName = null, FileLocationOption fileLocationOption = FileLocationOption.Default)
        where T : ISerializer, new()
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = CreateBuilder(fileLocationOption)
            .WithApplicationName(applicationName)
            .WithSerializer(configureSerializer);

        configure(builder);

        SetBuilder(builder.Build());
    }

    /// <summary>
    /// Creates a new Akavache builder for configuration.
    /// </summary>
    /// <param name="fileLocationOption">The file location option.</param>
    /// <returns>
    /// A new Akavache builder instance.
    /// </returns>
    public static IAkavacheBuilder CreateBuilder(FileLocationOption fileLocationOption = FileLocationOption.Default) => new AkavacheBuilder(fileLocationOption);

    /// <summary>
    /// Internal method to set the builder instance. Used by the builder pattern.
    /// </summary>
    /// <param name="builder">The configured builder instance.</param>
    private static void SetBuilder(IAkavacheInstance builder)
    {
        _builder = builder;
        _isInitialized = true;
    }

    private static IAkavacheInstance? GetOrThrowIfNotInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "CacheDatabase has not been initialized. " +
                "Call CacheDatabase.Initialize() or CacheDatabase.Initialize(builder => { ... }) first. " +
                "For more advanced scenarios, use CacheDatabase.CreateBuilder() to configure custom cache implementations.");
        }

        return _builder;
    }
}
