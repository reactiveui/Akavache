// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Akavache.Benchmarks.V10;

/// <summary>
/// TestData.
/// </summary>
[DebuggerDisplay("Id: {Id}, Name: {Name}, Value: {Value}")]
public class TestData : IEquatable<TestData>
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    /// <value>
    /// The identifier.
    /// </value>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>
    /// The value.
    /// </value>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the created.
    /// </summary>
    /// <value>
    /// The created.
    /// </value>
    public DateTimeOffset Created { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"Id: {Id}, Name: {Name}, Value: {Value}, Created: {Created}";

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TestData);

    /// <inheritdoc />
    public bool Equals(TestData? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id.Equals(other.Id) &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               Value == other.Value &&
               Created.Equals(other.Created);
    }

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
