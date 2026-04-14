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
    /// Initializes a new instance of the <see cref="CacheEntry"/> class.
    /// Required by sqlite-net's ORM which materialises rows via reflection.
    /// </summary>
    public CacheEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEntry"/> class with all fields populated.
    /// Preferred over the parameterless form at construction sites in the library: on older runtimes
    /// (notably net462) the JIT produces tighter codegen for a single ctor + field writes than for
    /// a <c>new() { … }</c> initializer that expands to a parameterless ctor followed by property
    /// setters.
    /// </summary>
    /// <param name="id">The cache entry's unique key.</param>
    /// <param name="typeName">Optional type discriminator, stored alongside the row.</param>
    /// <param name="value">The serialized payload bytes.</param>
    /// <param name="createdAt">The instant at which the entry was created.</param>
    /// <param name="expiresAt">Optional absolute expiration time. Null means "never expires".</param>
    public CacheEntry(string? id, string? typeName, byte[]? value, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
    {
        Id = id;
        TypeName = typeName;
        Value = value;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

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
    public bool Equals(CacheEntry? other) =>
        other is not null &&
        (ReferenceEquals(this, other) ||
         (string.Equals(Id, other.Id, StringComparison.Ordinal) &&
          CreatedAt.Equals(other.CreatedAt) &&
          Nullable.Equals(ExpiresAt, other.ExpiresAt) &&
          string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
          ValueEquals(Value, other.Value)));

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
    /// Checks whether two byte arrays contain equal data, treating two <see langword="null"/>
    /// buffers as equal.
    /// </summary>
    /// <param name="left">The first byte array.</param>
    /// <param name="right">The second byte array.</param>
    /// <returns>True if the byte arrays are equal.</returns>
    internal static bool ValueEquals(byte[]? left, byte[]? right) =>
        ReferenceEquals(left, right) ||
        (left is not null && right is not null && left.AsSpan().SequenceEqual(right));
}
