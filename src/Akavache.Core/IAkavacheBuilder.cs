// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Akavache;

/// <summary>
/// Interface for building and configuring BlobCache instances.
/// </summary>
public interface IAkavacheBuilder : IAkavacheInstance
{
    /// <summary>
    /// Gets the file location option.
    /// </summary>
    /// <value>
    /// The file location option.
    /// </value>
    FileLocationOption FileLocationOption { get; }

    /// <summary>
    /// Sets the application name for cache directory paths.
    /// </summary>
    /// <param name="applicationName">The application name.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder WithApplicationName(string? applicationName);

    /// <summary>
    /// Sets the InMemory cache instance.
    /// </summary>
    /// <param name="cache">The cache instance to use for InMemory operations.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder WithInMemory(IBlobCache cache);

    /// <summary>
    /// Configures default in-memory caches for all cache types.
    /// Uses the appropriate InMemoryBlobCache based on the configured serializer.
    /// </summary>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder WithInMemoryDefaults();

    /// <summary>
    /// Sets the LocalMachine cache instance.
    /// </summary>
    /// <param name="cache">The cache instance to use for LocalMachine operations.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder WithLocalMachine(IBlobCache cache);

    /// <summary>
    /// Sets the Secure cache instance.
    /// </summary>
    /// <param name="cache">The secure cache instance to use for Secure operations.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder WithSecure(ISecureBlobCache cache);

    /// <summary>
    /// Sets the UserAccount cache instance.
    /// </summary>
    /// <param name="cache">The cache instance to use for UserAccount operations.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder WithUserAccount(IBlobCache cache);

    /// <summary>
    /// Configures the serializer to use for cache operations.
    /// </summary>
    /// <typeparam name="T">The type of serializer to configure.</typeparam>
    /// <returns>The builder instance for fluent configuration.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    IAkavacheBuilder WithSerializer<T>()
        where T : ISerializer, new();

    /// <summary>
    /// Configures the serializer to use for cache operations with a custom factory function.
    /// </summary>
    /// <typeparam name="T">The type of serializer to configure.</typeparam>
    /// <param name="configure">A function that creates and configures the serializer instance.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    IAkavacheBuilder WithSerializer<T>(Func<T> configure)

        where T : ISerializer;

    /// <summary>
    /// Sets the forced DateTime kind for serialization operations.
    /// </summary>
    /// <param name="kind">The DateTime kind to use for all DateTime serialization.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder UseForcedDateTimeKind(DateTimeKind kind);

    /// <summary>
    /// Builds the configuration and returns the configured Akavache instance.
    /// </summary>
    /// <returns>The configured Akavache instance ready for use.</returns>
    IAkavacheInstance Build();
}
