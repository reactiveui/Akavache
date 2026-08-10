// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Akavache.Benchmarks.V10;

/// <summary> The payload the object benchmarks round-trip through the cache: a small immutable POCO mixing a value type, a string and a timestamp so serialization cost is representative. </summary>
[DebuggerDisplay("Id: {Id}, Name: {Name}, Value: {Value}")]
public sealed class TestData : IEquatable<TestData>
{
    /// <summary> Gets the identifier. Init-only because it takes part in <see cref="GetHashCode"/>. </summary>
    /// <value>
    /// The identifier.
    /// </value>
    public Guid Id { get; init; }

    /// <summary> Gets the name. Init-only because it takes part in <see cref="GetHashCode"/>. </summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; init; } = string.Empty;

    /// <summary> Gets the value. Init-only because it takes part in <see cref="GetHashCode"/>. </summary>
    /// <value>
    /// The value.
    /// </value>
    public int Value { get; init; }

    /// <summary> Gets the creation timestamp. Init-only because it takes part in <see cref="GetHashCode"/>. </summary>
    /// <value>
    /// The creation timestamp.
    /// </value>
    public DateTimeOffset Created { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"Id: {Id}, Name: {Name}, Value: {Value}, Created: {Created}";

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TestData);

    /// <inheritdoc />
    public bool Equals(TestData? other) =>
        other is not null
        && (ReferenceEquals(this, other)
            || (Id.Equals(other.Id)
                && string.Equals(Name, other.Name, StringComparison.Ordinal)
                && Value == other.Value
                && Created.Equals(other.Created)));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Id);
        hash.Add(Name, StringComparer.Ordinal);
        hash.Add(Value);
        hash.Add(Created);
        return hash.ToHashCode();
    }
}
