// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
    /// Gets the name of the executing assembly.
    /// </summary>
    string? ExecutingAssemblyName { get; }

    /// <summary>
    /// Gets the version of the executing assembly.
    /// </summary>
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
