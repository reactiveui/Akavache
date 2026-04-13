// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Core;
#if NET6_0_OR_GREATER
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
    /// Configures the executing assembly explicitly, bypassing the reflection
    /// fallback used by the default constructor.
    /// </summary>
    /// <remarks>
    /// Calling this also refreshes
    /// <see cref="IAkavacheInstance.ExecutingAssemblyName"/> and
    /// <see cref="IAkavacheInstance.Version"/> from the supplied assembly's
    /// metadata. This is the AOT-safe path: callers publishing trimmed or
    /// NativeAOT binaries should pass <c>typeof(MyApp).Assembly</c> here so
    /// Akavache does not need to probe
    /// <see cref="System.Reflection.Assembly.GetEntryAssembly"/> at runtime.
    /// </remarks>
    /// <param name="assembly">The assembly to use as the executing assembly.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder WithExecutingAssembly(Assembly assembly);

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
    IAkavacheBuilder WithSerializer<T>()
        where T : ISerializer, new();

    /// <summary>
    /// Configures the serializer to use for cache operations with a custom factory function.
    /// </summary>
    /// <typeparam name="T">The type of serializer to configure.</typeparam>
    /// <param name="configure">A function that creates and configures the serializer instance.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder WithSerializer<T>(Func<T> configure)
        where T : ISerializer;

    /// <summary>
    /// Configures the builder to use legacy file locations for cache directories.
    /// This is required when migrating from V10 to V11 so that V11 can find and read
    /// the existing V10 database files.
    /// </summary>
    /// <returns>The builder instance for fluent configuration.</returns>
    IAkavacheBuilder WithLegacyFileLocation();

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
