// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Akavache;

/// <summary>
/// A builder interface that represents a built Akavache instance.
/// </summary>
public interface IAkavacheInstance
{
    /// <summary>
    /// Gets the executing assembly.
    /// </summary>
    /// <value>
    /// The executing assembly.
    /// </value>
    Assembly ExecutingAssembly { get; }

    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    /// <value>
    /// The name of the application.
    /// </value>
    string ApplicationName { get; }

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
    /// Gets the InMemory cache instance.
    /// </summary>
    IBlobCache? InMemory { get; }

    /// <summary>
    /// Gets the LocalMachine cache instance.
    /// </summary>
    IBlobCache? LocalMachine { get; }

    /// <summary>
    /// Gets the Secure cache instance.
    /// </summary>
    ISecureBlobCache? Secure { get; }

    /// <summary>
    /// Gets the UserAccount cache instance.
    /// </summary>
    IBlobCache? UserAccount { get; }

    /// <summary>
    /// Gets or sets the http service.
    /// </summary>
    IHttpService? HttpService { get; set; }

    /// <summary>
    /// Gets the serializer.
    /// </summary>
    /// <value>
    /// The serializer.
    /// </value>
    ISerializer? Serializer { get; }

    /// <summary>
    /// Gets or sets the forced DateTime kind for DateTime serialization.
    /// When set, all DateTime values will be converted to this kind during cache operations.
    /// </summary>
    DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <summary>
    /// Gets the name of the serializer type.
    /// </summary>
    /// <value>
    /// The name of the serializer type.
    /// </value>
    string? SerializerTypeName { get; }
}
