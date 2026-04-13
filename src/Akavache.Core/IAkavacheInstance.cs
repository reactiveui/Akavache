// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Akavache;

/// <summary>
/// Represents a configured Akavache instance with access to all cache types and configuration.
/// </summary>
public interface IAkavacheInstance
{
    /// <summary>
    /// Gets the executing assembly.
    /// </summary>
    /// <remarks>
    /// Returns the Akavache core assembly as a sentinel unless the caller has set
    /// an explicit value via <c>WithExecutingAssembly</c> on the builder. Prefer
    /// holding your own assembly reference (for example
    /// <c>typeof(MyApp).Assembly</c>) instead of reading this property — that is
    /// both trim/AOT-safe and exact.
    /// </remarks>
    [Obsolete("Hold your own Assembly reference directly, or call WithExecutingAssembly on the builder if you need Akavache to track it for you.", error: false)]
    Assembly ExecutingAssembly { get; }

    /// <summary>
    /// Gets the application name used for cache directory paths.
    /// </summary>
    string ApplicationName { get; }

    /// <summary>
    /// Gets the application root path.
    /// </summary>
    string? ApplicationRootPath { get; }

    /// <summary>
    /// Gets or sets the settings cache path.
    /// </summary>
    string? SettingsCachePath { get; internal set; }

    /// <summary>
    /// Gets the short name of the executing assembly.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> unless the caller has set an explicit assembly via
    /// <c>WithExecutingAssembly</c> on the builder.
    /// </remarks>
    [Obsolete("Hold your own Assembly reference and read Name off it, or call WithExecutingAssembly on the builder.", error: false)]
    string? ExecutingAssemblyName { get; }

    /// <summary>
    /// Gets the parsed <see cref="AssemblyFileVersionAttribute"/> of the executing
    /// assembly.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> unless the caller has set an explicit assembly via
    /// <c>WithExecutingAssembly</c> on the builder.
    /// </remarks>
    [Obsolete("Pass the value in via WithExecutingAssembly on the builder, or use your own versioning strategy.", error: false)]
    Version? Version { get; }

    /// <summary>
    /// Gets the InMemory cache instance for temporary data storage.
    /// </summary>
    IBlobCache? InMemory { get; }

    /// <summary>
    /// Gets the LocalMachine cache instance for persistent but temporary data.
    /// </summary>
    IBlobCache? LocalMachine { get; }

    /// <summary>
    /// Gets the Secure cache instance for encrypted data storage.
    /// </summary>
    ISecureBlobCache? Secure { get; }

    /// <summary>
    /// Gets the UserAccount cache instance for user-specific persistent data.
    /// </summary>
    IBlobCache? UserAccount { get; }

    /// <summary>
    /// Gets or sets the HTTP service for web-based cache operations.
    /// </summary>
    IHttpService? HttpService { get; set; }

    /// <summary>
    /// Gets the serializer used for object serialization and deserialization.
    /// </summary>
    ISerializer? Serializer { get; }

    /// <summary>
    /// Gets or sets the forced DateTime kind for DateTime serialization.
    /// When set, all DateTime values will be converted to this kind during cache operations.
    /// </summary>
    DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <summary>
    /// Gets the name of the serializer type.
    /// </summary>
    string? SerializerTypeName { get; }
}
