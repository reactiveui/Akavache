// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using SQLite;

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Represents an entry in a sqlite cache.
/// </summary>
internal class SqliteCacheEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for the cache entry.
    /// </summary>
    [PrimaryKey]
    [Unique]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the cache entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the cache entry will expire.
    /// </summary>
    [Indexed]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the type name associated with the cache entry.
    /// </summary>
    [Indexed]
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets or sets the serialized value stored in the cache entry.
    /// </summary>
    public byte[]? Value { get; set; }
}
