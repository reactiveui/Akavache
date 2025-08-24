// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Akavache;

/// <summary>
/// Interface for building and configuring BlobCache instances.
/// </summary>
public interface IAkavacheBuilder : IAkavacheInstance
{
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
    /// Withes the serializser.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <returns>
    /// The builder instance for fluent configuration.
    /// </returns>
#if NET6_0_OR_GREATER

    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    IAkavacheBuilder WithSerializer<T>()
#else
    IAkavacheBuilder WithSerializer<T>()
#endif
        where T : ISerializer, new();

    /// <summary>
    /// Withes the serializer.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="configure">The configure.</param>
    /// <returns>
    /// The builder instance for fluent configuration.
    /// </returns>
#if NET6_0_OR_GREATER

    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    IAkavacheBuilder WithSerializer<T>(Func<T> configure)
#else
    IAkavacheBuilder WithSerializer<T>(Func<T> configure)
#endif
        where T : ISerializer;

    /// <summary>
    /// Uses the kind of the forced date time.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder UseForcedDateTimeKind(DateTimeKind kind);

    /// <summary>
    /// Builds and applies the configuration to BlobCache.
    /// </summary>
    /// <returns>The builder instance.</returns>
    IAkavacheInstance Build();
}
