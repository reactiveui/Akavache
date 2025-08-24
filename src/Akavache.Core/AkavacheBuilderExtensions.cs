// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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
    public static AppBuilder WithAkavacheCacheDatabase<T>(this AppBuilder builder, Action<IAkavacheBuilder> configure, string? applicationName = null)
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
    public static AppBuilder WithAkavacheCacheDatabase<T>(this AppBuilder builder, string? applicationName = null)
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
    /// <exception cref="System.ArgumentNullException">builder
    /// or
    /// configure
    /// or
    /// instance.</exception>
    /// <exception cref="ArgumentNullException">builder.</exception>
    public static AppBuilder WithAkavache<T>(this AppBuilder builder, string? applicationName, Action<IAkavacheBuilder> configure, Action<IAkavacheInstance> instance)
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
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    public static AppBuilder WithAkavache<T>(this AppBuilder builder, string? applicationName, Action<IAkavacheInstance> instance)
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
}
