// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.IsolatedStorage;
using System.Reflection;
using Splat;
using Splat.Builder;

namespace Akavache;

/// <summary>
/// AkavacheBuilderExtensions.
/// </summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">An action to configure the CacheDatabase builder.</param>
    /// <param name="applicationName">Name of the application.</param>
    /// <returns>
    /// The configured builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
    /// <exception cref="ArgumentNullException">configure.</exception>
#if NET6_0_OR_GREATER

    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static AppBuilder WithAkavacheCacheDatabase<T>(this AppBuilder builder, Action<IAkavacheBuilder> configure, string? applicationName = null)
#else
    public static AppBuilder WithAkavacheCacheDatabase<T>(this AppBuilder builder, Action<IAkavacheBuilder> configure, string? applicationName = null)
#endif
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        CacheDatabase.Initialize<T>(configure, applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="configureSerializer">The serializer configuration.</param>
    /// <param name="configure">An action to configure the CacheDatabase builder.</param>
    /// <param name="applicationName">Name of the application.</param>
    /// <returns>
    /// The configured builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER

    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static AppBuilder WithAkavacheCacheDatabase<T>(this AppBuilder builder, Func<T> configureSerializer, Action<IAkavacheBuilder> configure, string? applicationName = null)
#else
    public static AppBuilder WithAkavacheCacheDatabase<T>(this AppBuilder builder, Func<T> configureSerializer, Action<IAkavacheBuilder> configure, string? applicationName = null)
#endif
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        CacheDatabase.Initialize(configureSerializer, configure, applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with default in-memory caches.
    /// This is the safest default as it doesn't require any additional packages.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">The application name for cache directories. If null, uses the current ApplicationName.</param>
    /// <returns>
    /// A BlobCache builder for further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER

    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static AppBuilder WithAkavacheCacheDatabase<T>(this AppBuilder builder, string? applicationName = null)
#else
    public static AppBuilder WithAkavacheCacheDatabase<T>(this AppBuilder builder, string? applicationName = null)
#endif
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        CacheDatabase.Initialize<T>(applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with default in-memory caches.
    /// This is the safest default as it doesn't require any additional packages.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="configureSerializer">The serializer configuration.</param>
    /// <param name="applicationName">The application name for cache directories. If null, uses the current ApplicationName.</param>
    /// <returns>
    /// A BlobCache builder for further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER

    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static AppBuilder WithAkavacheCacheDatabase<T>(this AppBuilder builder, Func<T> configureSerializer, string? applicationName = null)
#else
    public static AppBuilder WithAkavacheCacheDatabase<T>(this AppBuilder builder, Func<T> configureSerializer, string? applicationName = null)
#endif
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        CacheDatabase.Initialize(configureSerializer, applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="configure">An action to configure the CacheDatabase builder.</param>
    /// <param name="instance">The instance.</param>
    /// <returns>
    /// The configured builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder
    /// or
    /// configure
    /// or
    /// instance.</exception>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER

    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static AppBuilder WithAkavache<T>(this AppBuilder builder, string? applicationName, Action<IAkavacheBuilder> configure, Action<IAkavacheInstance> instance)
#else
    public static AppBuilder WithAkavache<T>(this AppBuilder builder, string? applicationName, Action<IAkavacheBuilder> configure, Action<IAkavacheInstance> instance)
#endif
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var akavacheBuilder = CacheDatabase.CreateBuilder()
            .WithApplicationName(applicationName)
            .WithSerializer<T>();
        configure(akavacheBuilder);
        instance(akavacheBuilder.Build());
        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="configure">An action to configure the CacheDatabase builder.</param>
    /// <param name="instance">The instance.</param>
    /// <returns>
    /// The configured builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder
    /// or
    /// configure
    /// or
    /// instance.</exception>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER

    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static AppBuilder WithAkavache<T>(this AppBuilder builder, string? applicationName, Action<IAkavacheBuilder> configure, Action<IMutableDependencyResolver, IAkavacheInstance> instance)
#else
    public static AppBuilder WithAkavache<T>(this AppBuilder builder, string? applicationName, Action<IAkavacheBuilder> configure, Action<IMutableDependencyResolver, IAkavacheInstance> instance)
#endif
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var akavacheBuilder = CacheDatabase.CreateBuilder()
            .WithApplicationName(applicationName)
            .WithSerializer<T>();
        configure(akavacheBuilder);

        return builder.WithCustomRegistration(splat => instance(splat, akavacheBuilder.Build()));
    }

    /// <summary>
    /// Initializes CacheDatabase with a set of default in-memory caches.
    /// This is the safest default as it doesn't require any additional packages.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">The application name for cache directories. If null, uses the current ApplicationName.</param>
    /// <param name="instance">The instance created.</param>
    /// <returns>
    /// A BlobCache builder for further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER

    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static AppBuilder WithAkavache<T>(this AppBuilder builder, string? applicationName, Action<IAkavacheInstance> instance)
#else
    public static AppBuilder WithAkavache<T>(this AppBuilder builder, string? applicationName, Action<IAkavacheInstance> instance)
#endif
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        instance(CacheDatabase.CreateBuilder()
            .WithApplicationName(applicationName)
            .WithSerializer<T>()
            .WithInMemoryDefaults().Build());

        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with a set of default in-memory caches.
    /// This is the safest default as it doesn't require any additional packages.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">The application name for cache directories. If null, uses the current ApplicationName.</param>
    /// <param name="instance">The instance created.</param>
    /// <returns>
    /// A BlobCache builder for further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER

    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static AppBuilder WithAkavache<T>(this AppBuilder builder, string? applicationName, Action<IMutableDependencyResolver, IAkavacheInstance> instance)
#else
    public static AppBuilder WithAkavache<T>(this AppBuilder builder, string? applicationName, Action<IMutableDependencyResolver, IAkavacheInstance> instance)
#endif
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        return builder.WithCustomRegistration(splat => instance(splat, CacheDatabase.CreateBuilder()
            .WithApplicationName(applicationName)
            .WithSerializer<T>()
            .WithInMemoryDefaults().Build()));
    }

    /// <summary>
    /// Withes the in memory.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
    public static IAkavacheBuilder WithInMemory(this IAkavacheBuilder builder)
    {
        // Ensure the builder is not null
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.SerializerTypeName == null)
        {
            throw new InvalidOperationException("A serializer must be configured before using in-memory cache.");
        }

        return builder.WithInMemory(new InMemoryBlobCache(builder.SerializerTypeName));
    }

    /// <summary>
    /// Gets the isolated cache directory.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="cacheName">Name of the cache.</param>
    /// <returns>The Isolated cache path.</returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    /// <exception cref="System.ArgumentException">
    /// Cache name cannot be null or empty. - cacheName
    /// or
    /// Application name cannot be null or empty. - ApplicationName.
    /// </exception>
    public static string? GetIsolatedCacheDirectory(this IAkavacheInstance builder, string cacheName)
    {
        // Ensure the builder is not null
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrWhiteSpace(cacheName))
        {
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(cacheName));
        }

        if (string.IsNullOrWhiteSpace(builder.ApplicationName))
        {
            throw new ArgumentException("Application name cannot be null or empty.", nameof(builder.ApplicationName));
        }

        string? cachePath = null;

        // Compute CachePath under a writable location (fix iOS bundle write attempt)
        using (var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly, null, null))
        {
            // Try to get a path within isolated storage for the settings cache using the application name
            try
            {
                if (isoStore != null)
                {
                    var isoPath = Path.Combine(builder.ApplicationName, cacheName);

                    // Ensure the directory exists
                    if (!isoStore.DirectoryExists(isoPath))
                    {
                        isoStore.CreateDirectory(isoPath);
                    }

                    if (isoStore.DirectoryExists(isoPath))
                    {
                        var dirNames = isoStore.GetDirectoryNames(isoPath);
                        cachePath = Path.Combine(isoStore.GetType().GetProperty("RootDirectory", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(isoStore)?.ToString() ?? string.Empty, isoPath);
                    }
                }
            }
            catch
            {
                // Ignore isolated storage exceptions and fall back to local app data path
            }
        }

        return cachePath;
    }
}
