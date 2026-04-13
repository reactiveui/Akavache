// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using SQLite;

namespace Akavache.V10toV11;

/// <summary>
/// Internal model matching the Akavache V10 CacheElement database schema.
/// Used to read rows from V10 databases during migration.
/// </summary>
[Table("CacheElement")]
internal class V10CacheElement
{
    /// <summary>
    /// Gets or sets the cache key.
    /// </summary>
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully qualified type name of the cached object.
    /// </summary>
    [Indexed]
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets or sets the serialized value.
    /// </summary>
    public byte[]? Value { get; set; }

    /// <summary>
    /// Gets or sets the expiration time as DateTime ticks.
    /// A value of 0 or less indicates no expiration.
    /// </summary>
    public long Expiration { get; set; }

    /// <summary>
    /// Gets or sets the creation time as DateTime ticks.
    /// </summary>
    public long CreatedAt { get; set; }
}
