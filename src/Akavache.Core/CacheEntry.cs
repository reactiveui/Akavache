// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using SQLite;

namespace Akavache;

/// <summary>
/// Represents an entry in a memory cache.
/// </summary>
[DebuggerDisplay("Id: {Id}, Type: {TypeName}, Expires: {ExpiresAt}")]
public class CacheEntry : IEquatable<CacheEntry>
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

    /// <inheritdoc />
    public override string ToString() => $"Id: {Id}, Type: {TypeName}, Created: {CreatedAt}, Expires: {ExpiresAt}";

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as CacheEntry);

    /// <inheritdoc />
    public bool Equals(CacheEntry? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Id, other.Id, StringComparison.Ordinal) &&
               CreatedAt.Equals(other.CreatedAt) &&
               Nullable.Equals(ExpiresAt, other.ExpiresAt) &&
               string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
               ValueEquals(Value, other.Value);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Id, StringComparer.Ordinal);
        hash.Add(CreatedAt);
        hash.Add(ExpiresAt);
        hash.Add(TypeName, StringComparer.Ordinal);
        if (Value is not null)
        {
            hash.AddBytes(Value);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Checks if two byte arrays are equal.
    /// </summary>
    /// <param name="left">The first byte array.</param>
    /// <param name="right">The second byte array.</param>
    /// <returns>True if the byte arrays are equal.</returns>
    private static bool ValueEquals(byte[]? left, byte[]? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.AsSpan().SequenceEqual(right);
    }
}
