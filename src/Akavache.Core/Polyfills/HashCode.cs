// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NETFRAMEWORK

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System;

/// <summary>Combines the hash codes of multiple values into a single hash code.</summary>
/// <remarks>
/// Polyfill for System.HashCode on .NET Framework, which does not ship it. The combination is a
/// plain multiply-accumulate over the component hash codes rather than a reimplementation of the
/// runtime's xxHash32 accumulator: hash codes are only required to agree with equality within a
/// single process, so matching the runtime's exact bit pattern buys nothing and the seeded
/// accumulator is far more code to carry.
/// </remarks>
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[SuppressMessage(
    "Design",
    "SST2324:Do not declare a member more accessible than its containing type",
    Justification = "Mirrors the shape of the corresponding BCL type (System.HashCode); the polyfill compiles only where the BCL lacks it.")]
internal struct HashCode : IEquatable<HashCode>
{
    /// <summary>Odd prime multiplier; the conventional choice for a multiply-accumulate hash.</summary>
    private const int Multiplier = 31;

    /// <summary>The running hash.</summary>
    private int _accumulator;

    /// <summary>Combines a single value into a hash code.</summary>
    /// <typeparam name="T1">The type of the value.</typeparam>
    /// <param name="value1">The value to hash.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1>(T1 value1) => value1?.GetHashCode() ?? 0;

    /// <summary>Combines two values into a hash code.</summary>
    /// <typeparam name="T1">The type of the first value.</typeparam>
    /// <typeparam name="T2">The type of the second value.</typeparam>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2>(T1 value1, T2 value2) => (value1, value2).GetHashCode();

    /// <summary>Combines three values into a hash code.</summary>
    /// <typeparam name="T1">The type of the first value.</typeparam>
    /// <typeparam name="T2">The type of the second value.</typeparam>
    /// <typeparam name="T3">The type of the third value.</typeparam>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <param name="value3">The third value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3) =>
        (value1, value2, value3).GetHashCode();

    /// <summary>Combines four values into a hash code.</summary>
    /// <typeparam name="T1">The type of the first value.</typeparam>
    /// <typeparam name="T2">The type of the second value.</typeparam>
    /// <typeparam name="T3">The type of the third value.</typeparam>
    /// <typeparam name="T4">The type of the fourth value.</typeparam>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <param name="value3">The third value.</param>
    /// <param name="value4">The fourth value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4) =>
        (value1, value2, value3, value4).GetHashCode();

    /// <summary>Adds a value to the running hash.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to add.</param>
    public void Add<T>(T value) => AddHash(value?.GetHashCode() ?? 0);

    /// <summary>Adds a value to the running hash using a specific comparer.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to add.</param>
    /// <param name="comparer">The comparer used to obtain the value's hash code.</param>
    public void Add<T>(T value, IEqualityComparer<T>? comparer) =>
        AddHash(value is null ? 0 : comparer?.GetHashCode(value) ?? value.GetHashCode());

    /// <summary>Adds each byte of the supplied span to the running hash.</summary>
    /// <param name="value">The bytes to add.</param>
    public void AddBytes(ReadOnlySpan<byte> value)
    {
        foreach (var b in value)
        {
            AddHash(b);
        }
    }

    /// <summary>Returns the accumulated hash code.</summary>
    /// <returns>The combined hash code of every added value.</returns>
    public readonly int ToHashCode() => _accumulator;

    /// <summary>Not supported; matches the BCL, which forbids hashing the accumulator itself.</summary>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override readonly int GetHashCode() => throw new NotSupportedException("Call ToHashCode to retrieve the computed hash code.");

    /// <summary>Not supported; matches the BCL, which forbids comparing accumulators.</summary>
    /// <param name="obj">Ignored.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override readonly bool Equals(object? obj) => throw new NotSupportedException("HashCode is a mutable accumulator and is not comparable.");

    /// <summary>Not supported; matches the BCL, which forbids comparing accumulators.</summary>
    /// <param name="other">Ignored.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public readonly bool Equals(HashCode other) => throw new NotSupportedException("HashCode is a mutable accumulator and is not comparable.");

    /// <summary>Folds one component hash into the accumulator.</summary>
    /// <param name="hash">The component hash code.</param>
    private void AddHash(int hash) => _accumulator = unchecked((_accumulator * Multiplier) + hash);
}

#else
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(HashCode))]
#endif
