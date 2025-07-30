// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Registration helpers for SQLite-based Akavache initialization.
/// </summary>
public static class Registrations
{
#if ENCRYPTED
    /// <summary>
    /// Initializes Akavache with SQLite defaults and configures the serializer.
    /// </summary>
    /// <param name="applicationName">The application name for cache directories.</param>
    /// <param name="password">The password.</param>
    /// <param name="initializeSqlite">Optional SQLite initialization action.</param>
    /// <exception cref="System.ArgumentException">Application name cannot be null or empty. - applicationName.</exception>
    public static void Start(string applicationName, string password, Action? initializeSqlite = null)
#else
    /// <summary>
    /// Initializes Akavache with SQLite defaults and configures the serializer.
    /// </summary>
    /// <param name="applicationName">The application name for cache directories.</param>
    /// <param name="initializeSqlite">Optional SQLite initialization action.</param>
    public static void Start(string applicationName, Action? initializeSqlite = null)
#endif
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new ArgumentException("Application name cannot be null or empty.", nameof(applicationName));
        }

        // Initialize SQLite if provided
        initializeSqlite?.Invoke();

        // Initialize BlobCache with SQLite defaults
        BlobCache.Initialize(builder =>
        {
            builder.WithApplicationName(applicationName);
#if ENCRYPTED
            builder.WithSqliteDefaults(password);
#else
            builder.WithSqliteDefaults();
#endif
        });
    }
}
