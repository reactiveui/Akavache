// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Akavache.Core;

/// <summary>
/// Interface for building and configuring BlobCache instances.
/// </summary>
public interface IBlobCacheBuilder
{
    /// <summary>
    /// Gets the executing assembly.
    /// </summary>
    /// <value>
    /// The executing assembly.
    /// </value>
    Assembly ExecutingAssembly { get; }

    /// <summary>
    /// Gets the application root path.
    /// </summary>
    /// <value>
    /// The application root path.
    /// </value>
    string? ApplicationRootPath { get; }

    /// <summary>
    /// Gets or sets the settings cache path.
    /// </summary>
    /// <value>
    /// The settings cache path.
    /// </value>
    string? SettingsCachePath { get; internal set; }

    /// <summary>
    /// Gets the name of the executing assembly.
    /// </summary>
    /// <value>
    /// The name of the executing assembly.
    /// </value>
    string? ExecutingAssemblyName { get; }

    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <value>
    /// The version.
    /// </value>
    Version? Version { get; }

    /// <summary>
    /// Gets the UserAccount cache instance.
    /// </summary>
    IBlobCache? UserAccount { get; }

    /// <summary>
    /// Gets the LocalMachine cache instance.
    /// </summary>
    IBlobCache? LocalMachine { get; }

    /// <summary>
    /// Gets the Secure cache instance.
    /// </summary>
    ISecureBlobCache? Secure { get; }

    /// <summary>
    /// Gets the InMemory cache instance.
    /// </summary>
    IBlobCache? InMemory { get; }

    /// <summary>
    /// Sets the application name for cache directory paths.
    /// </summary>
    /// <param name="applicationName">The application name.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IBlobCacheBuilder WithApplicationName(string applicationName);

    /// <summary>
    /// Sets the UserAccount cache instance.
    /// </summary>
    /// <param name="cache">The cache instance to use for UserAccount operations.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IBlobCacheBuilder WithUserAccount(IBlobCache cache);

    /// <summary>
    /// Sets the LocalMachine cache instance.
    /// </summary>
    /// <param name="cache">The cache instance to use for LocalMachine operations.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IBlobCacheBuilder WithLocalMachine(IBlobCache cache);

    /// <summary>
    /// Sets the Secure cache instance.
    /// </summary>
    /// <param name="cache">The secure cache instance to use for Secure operations.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IBlobCacheBuilder WithSecure(ISecureBlobCache cache);

    /// <summary>
    /// Sets the InMemory cache instance.
    /// </summary>
    /// <param name="cache">The cache instance to use for InMemory operations.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IBlobCacheBuilder WithInMemory(IBlobCache cache);

    /// <summary>
    /// Withes the serializser.
    /// </summary>
    /// <param name="serializer">The serializer.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IBlobCacheBuilder WithSerializer(ISerializer serializer);

    /// <summary>
    /// Configures default in-memory caches for all cache types.
    /// Uses the appropriate InMemoryBlobCache based on the configured serializer.
    /// </summary>
    /// <returns>The builder instance for fluent configuration.</returns>
    IBlobCacheBuilder WithInMemoryDefaults();

    /// <summary>
    /// Builds and applies the configuration to BlobCache.
    /// </summary>
    /// <returns>The builder instance.</returns>
    IBlobCacheBuilder Build();
}
