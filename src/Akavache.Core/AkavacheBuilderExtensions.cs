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
    /// Initializes BlobCache with a custom builder configuration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">An action to configure the BlobCache builder.</param>
    /// <param name="applicationName">Name of the application.</param>
    /// <returns>
    /// The configured builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
    /// <exception cref="ArgumentNullException">configure.</exception>
    public static AppBuilder WithAkavache(this AppBuilder builder, Action<IAkavacheBuilder> configure, string? applicationName = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        CacheDatabase.Initialize(configure, applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes BlobCache with default in-memory caches.
    /// This is the safest default as it doesn't require any additional packages.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">The application name for cache directories. If null, uses the current ApplicationName.</param>
    /// <returns>
    /// A BlobCache builder for further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
    public static AppBuilder WithAkavache(this AppBuilder builder, string? applicationName = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        CacheDatabase.Initialize(applicationName);

        return builder;
    }

    /// <summary>
    /// Withes the serializser.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="serializer">The serializer.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">serializer.</exception>
    public static IAkavacheBuilder WithSerializer(this IAkavacheBuilder builder, ISerializer serializer)
    {
        if (serializer == null)
        {
            throw new ArgumentNullException(nameof(serializer));
        }

        // Ensure the builder is not null
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.WithSerializer(serializer);
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

        return builder.WithInMemory(new InMemoryBlobCache());
    }

    /// <summary>
    /// Uses the kind of the forced date time.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    public static IAkavacheBuilder UseForcedDateTimeKind(this IAkavacheBuilder builder, DateTimeKind kind)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        CacheDatabase.ForcedDateTimeKind = kind;
        return builder;
    }
}
