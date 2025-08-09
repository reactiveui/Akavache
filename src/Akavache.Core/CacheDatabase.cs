// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// BlobCache is the main entry point for interacting with Akavache. It provides
/// convenient static properties for accessing common cache locations.
/// This V11 implementation uses a builder pattern for configuration.
/// </summary>
public static class CacheDatabase
{
    private static IBlobCacheBuilder? _builder;
    private static bool _isInitialized;

    /// <summary>
    /// Gets or sets the application name used for cache file paths.
    /// </summary>
    public static string ApplicationName { get; set; } = "Akavache";

    /// <summary>
    /// Gets or sets the forced DateTime kind for DateTime serialization.
    /// When set, all DateTime values will be converted to this kind during cache operations.
    /// </summary>
    public static DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <summary>
    /// Gets the InMemory cache instance. This cache stores data only in memory
    /// and is lost when the application shuts down. Useful for temporary data
    /// and session state.
    /// </summary>
    public static IBlobCache InMemory => GetOrThrowIfNotInitialized()?.InMemory ??
        throw new InvalidOperationException("BlobCache has not been initialized. Call BlobCache.Initialize() first.");

    /// <summary>
    /// Gets a value indicating whether BlobCache has been initialized.
    /// </summary>
    public static bool IsInitialized => _isInitialized;
    /// <summary>
    /// Gets the LocalMachine cache instance. This cache persists data but is suitable
    /// for temporary/cached data that can be safely deleted. On mobile platforms,
    /// the system may delete this data to free up disk space.
    /// </summary>
    public static IBlobCache LocalMachine => GetOrThrowIfNotInitialized()?.LocalMachine ??
        throw new InvalidOperationException("BlobCache has not been initialized. Call BlobCache.Initialize() first.");

    /// <summary>
    /// Gets the Secure cache instance. This cache provides encrypted storage
    /// for sensitive data like credentials and API keys.
    /// </summary>
    public static ISecureBlobCache Secure => GetOrThrowIfNotInitialized()?.Secure ??
        throw new InvalidOperationException("BlobCache has not been initialized. Call BlobCache.Initialize() first.");

    /// <summary>
    /// Gets the UserAccount cache instance. This cache persists data and is suitable
    /// for storing user settings and preferences that should survive app restarts.
    /// On some platforms, this data may be backed up to the cloud.
    /// </summary>
    public static IBlobCache UserAccount => GetOrThrowIfNotInitialized()?.UserAccount ??
        throw new InvalidOperationException("BlobCache has not been initialized. Call BlobCache.Initialize() first.");
    /// <summary>
    /// Creates a new BlobCache builder for configuration.
    /// </summary>
    /// <returns>A new BlobCache builder instance.</returns>
    public static IBlobCacheBuilder CreateBuilder() => new BlobCacheBuilder();

    /// <summary>
    /// Initializes BlobCache with default in-memory caches.
    /// This is the safest default as it doesn't require any additional packages.
    /// </summary>
    /// <param name="applicationName">The application name for cache directories. If null, uses the current ApplicationName.</param>
    /// <returns>A BlobCache builder for further configuration.</returns>
    public static IBlobCacheBuilder Initialize(string? applicationName = null)
    {
        if (applicationName != null)
        {
            ApplicationName = applicationName;
        }

        var builder = CreateBuilder()
            .WithApplicationName(ApplicationName)
            .WithInMemoryDefaults();

        return builder.Build();
    }

    /// <summary>
    /// Initializes BlobCache with a custom builder configuration.
    /// </summary>
    /// <param name="configure">An action to configure the BlobCache builder.</param>
    /// <returns>The configured builder.</returns>
    public static IBlobCacheBuilder Initialize(Action<IBlobCacheBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = CreateBuilder().WithApplicationName(ApplicationName);
        configure(builder);
        return builder.Build();
    }

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
            ? Observable.Merge(shutdownTasks).TakeLast(1).Select(_ => Unit.Default)
            : Observable.Return(Unit.Default);
    }

    /// <summary>
    /// Internal method to set the builder instance. Used by the builder pattern.
    /// </summary>
    /// <param name="builder">The configured builder instance.</param>
    internal static void SetBuilder(IBlobCacheBuilder builder)
    {
        _builder = builder;
        _isInitialized = true;
    }

    private static IBlobCacheBuilder? GetOrThrowIfNotInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "BlobCache has not been initialized. " +
                "Call BlobCache.Initialize() or BlobCache.Initialize(builder => { ... }) first. " +
                "For more advanced scenarios, use BlobCache.CreateBuilder() to configure custom cache implementations.");
        }

        return _builder;
    }
}
