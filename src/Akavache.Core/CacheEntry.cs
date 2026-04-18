// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// Represents an entry in a memory cache. The synthesized record equality is
/// overridden so <see cref="Value"/> byte arrays are compared by content — two freshly
/// allocated buffers carrying the same bytes compare equal.
/// </summary>
/// <param name="Id">The cache entry's unique key.</param>
/// <param name="TypeName">Optional type discriminator, stored alongside the row.</param>
/// <param name="Value">The serialized payload bytes.</param>
/// <param name="CreatedAt">The instant at which the entry was created.</param>
/// <param name="ExpiresAt">Optional absolute expiration time. <see langword="null"/> means "never expires".</param>
public sealed record CacheEntry(
    string? Id,
    string? TypeName,
    byte[]? Value,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt)
{
    /// <inheritdoc />
    public bool Equals(CacheEntry? other) =>
        other is not null &&
        (ReferenceEquals(this, other) ||
         (string.Equals(Id, other.Id, StringComparison.Ordinal) &&
          CreatedAt.Equals(other.CreatedAt) &&
          Nullable.Equals(ExpiresAt, other.ExpiresAt) &&
          string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
          (ReferenceEquals(Value, other.Value) ||
           (Value is not null && other.Value is not null && Value.AsSpan().SequenceEqual(other.Value)))));

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
}
