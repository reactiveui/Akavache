// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using SQLite;

#if ENCRYPTED
namespace ReactiveMarbles.CacheDatabase.EncryptedSqlite3;
#else
namespace ReactiveMarbles.CacheDatabase.Sqlite3;
#endif

/// <summary>
/// A entry in a memory cache.
/// </summary>
internal class CacheEntry
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    [PrimaryKey]
    [Unique]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entry will expire.
    /// </summary>
    [Indexed]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the type name of the entry.
    /// </summary>
    [Indexed]
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets or sets the value of the entry.
    /// </summary>
    public byte[]? Value { get; set; } = [];
}
