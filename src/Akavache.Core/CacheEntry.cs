// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using SQLite;

namespace Akavache;

/// <summary>
/// Represents an entry in a memory cache.
/// </summary>
public class CacheEntry
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
