// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// Polyfill implementation adapted from Simon Cropp's Polyfill library
// https://github.com/SimonCropp/Polyfill
#if !NET

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System;

/// <summary>
/// Represents a type that can be used to index a collection either from the start or the end.
/// The C# <c>^</c> from-end index operator lowers to this type, so it must exist on target
/// frameworks that predate it even when the emitted code never allocates one.
/// </summary>
/// <remarks>
/// Link: https://learn.microsoft.com/en-us/dotnet/api/system.index.
/// </remarks>
[SuppressMessage(
    "Design",
    "SST2324:Do not declare a member more accessible than its containing type",
    Justification = "Mirrors the shape of the corresponding BCL type (System.Index); the polyfill compiles only where the BCL lacks it.")]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal readonly struct Index : IEquatable<Index>
{
    /// <summary>Non-negative is measured from the start; the bitwise complement is measured from the end.</summary>
    private readonly int _value;

    /// <summary>Initializes a new instance of the <see cref="Index"/> struct.</summary>
    /// <param name="value">The index value; must be zero or positive.</param>
    /// <param name="fromEnd">Whether the index is counted from the end of the collection.</param>
    public Index(int value, bool fromEnd = false)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Non-negative number required.");
        }

        _value = fromEnd ? ~value : value;
    }

    /// <summary>Gets the index value without the from-end flag.</summary>
    public int Value => _value < 0 ? ~_value : _value;

    /// <summary>Gets a value indicating whether the index counts from the end.</summary>
    public bool IsFromEnd => _value < 0;

    /// <summary>Converts an integer into a from-start <see cref="Index"/>.</summary>
    /// <param name="value">The index value.</param>
    public static implicit operator Index(int value) => new(value);

    /// <summary>Calculates the offset this index represents within a collection of the given length.</summary>
    /// <param name="length">The length of the collection being indexed.</param>
    /// <returns>The zero-based offset from the start of the collection.</returns>
    public int GetOffset(int length) => _value < 0 ? length + _value + 1 : _value;

    /// <inheritdoc/>
    public bool Equals(Index other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Index other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _value;
}

#else
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Index))]
#endif
