// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// Slimmed-down polyfill for System.HashCode on .NET Framework.
// Based on xxHash32 from the .NET runtime (MIT license):
// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/HashCode.cs
#if NETFRAMEWORK

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace System;

/// <summary>
/// Combines the hash code for multiple values into a single hash code.
/// </summary>
[ExcludeFromCodeCoverage]
internal struct HashCode
{
    /// <summary>
    /// xxHash32 prime constant 1.
    /// </summary>
    private const uint Prime1 = 2654435761U;

    /// <summary>
    /// xxHash32 prime constant 2.
    /// </summary>
    private const uint Prime2 = 2246822519U;

    /// <summary>
    /// xxHash32 prime constant 3.
    /// </summary>
    private const uint Prime3 = 3266489917U;

    /// <summary>
    /// xxHash32 prime constant 4.
    /// </summary>
    private const uint Prime4 = 668265263U;

    /// <summary>
    /// xxHash32 prime constant 5.
    /// </summary>
    private const uint Prime5 = 374761393U;

    /// <summary>
    /// The randomly generated global seed used to initialize hash state, ensuring different hash distributions per process.
    /// </summary>
    private static readonly uint Seed = GenerateGlobalSeed();

    /// <summary>
    /// Accumulator lane 1 for the xxHash32 state.
    /// </summary>
    private uint _v1;

    /// <summary>
    /// Accumulator lane 2 for the xxHash32 state.
    /// </summary>
    private uint _v2;

    /// <summary>
    /// Accumulator lane 3 for the xxHash32 state.
    /// </summary>
    private uint _v3;

    /// <summary>
    /// Accumulator lane 4 for the xxHash32 state.
    /// </summary>
    private uint _v4;

    /// <summary>
    /// Queued input 1 awaiting a full round.
    /// </summary>
    private uint _queue1;

    /// <summary>
    /// Queued input 2 awaiting a full round.
    /// </summary>
    private uint _queue2;

    /// <summary>
    /// Queued input 3 awaiting a full round.
    /// </summary>
    private uint _queue3;

    /// <summary>
    /// The number of hash values that have been added.
    /// </summary>
    private uint _length;

    /// <summary>Combines the hash codes of up to eight values into a single hash code.</summary>
    /// <typeparam name="T1">The type of the first value.</typeparam>
    /// <param name="value1">The first value to combine.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1>(T1 value1)
    {
        var hash = default(HashCode);
        hash.Add(value1);
        return hash.ToHashCode();
    }

    /// <summary>Combines the hash codes of up to eight values into a single hash code.</summary>
    /// <typeparam name="T1">The type of the first value.</typeparam>
    /// <typeparam name="T2">The type of the second value.</typeparam>
    /// <param name="value1">The first value to combine.</param>
    /// <param name="value2">The second value to combine.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        var hash = default(HashCode);
        hash.Add(value1);
        hash.Add(value2);
        return hash.ToHashCode();
    }

    /// <summary>Combines the hash codes of up to eight values into a single hash code.</summary>
    /// <typeparam name="T1">The type of the first value.</typeparam>
    /// <typeparam name="T2">The type of the second value.</typeparam>
    /// <typeparam name="T3">The type of the third value.</typeparam>
    /// <param name="value1">The first value to combine.</param>
    /// <param name="value2">The second value to combine.</param>
    /// <param name="value3">The third value to combine.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        var hash = default(HashCode);
        hash.Add(value1);
        hash.Add(value2);
        hash.Add(value3);
        return hash.ToHashCode();
    }

    /// <summary>Combines the hash codes of up to eight values into a single hash code.</summary>
    /// <typeparam name="T1">The type of the first value.</typeparam>
    /// <typeparam name="T2">The type of the second value.</typeparam>
    /// <typeparam name="T3">The type of the third value.</typeparam>
    /// <typeparam name="T4">The type of the fourth value.</typeparam>
    /// <param name="value1">The first value to combine.</param>
    /// <param name="value2">The second value to combine.</param>
    /// <param name="value3">The third value to combine.</param>
    /// <param name="value4">The fourth value to combine.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        var hash = default(HashCode);
        hash.Add(value1);
        hash.Add(value2);
        hash.Add(value3);
        hash.Add(value4);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Adds a value to the hash code.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to add.</param>
    public void Add<T>(T value) =>
        AddHash(value?.GetHashCode() ?? 0);

    /// <summary>
    /// Adds a value to the hash code using a custom comparer.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to add.</param>
    /// <param name="comparer">The comparer to use for hashing.</param>
    public void Add<T>(T value, IEqualityComparer<T>? comparer) =>
        AddHash(value is null ? 0 : comparer?.GetHashCode(value) ?? value.GetHashCode());

    /// <summary>
    /// Adds the bytes from the supplied span into the running hash.
    /// </summary>
    /// <param name="value">The byte span to feed into the hash.</param>
    public void AddBytes(ReadOnlySpan<byte> value)
    {
        foreach (var b in value)
        {
            AddHash(b);
        }
    }

    /// <summary>
    /// Calculates the final hash code after consecutive <see cref="Add{T}(T)"/> invocations.
    /// </summary>
    /// <returns>The calculated hash code.</returns>
    public readonly int ToHashCode()
    {
        var length = _length;
        var position = length % 4;
        var hash = length < 4 ? MixEmptyState() : MixState(_v1, _v2, _v3, _v4);

        hash += length * 4;

        if (position > 0)
        {
            hash = QueueRound(hash, _queue1);
            if (position > 1)
            {
                hash = QueueRound(hash, _queue2);
                if (position > 2)
                {
                    hash = QueueRound(hash, _queue3);
                }
            }
        }

        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// Rotates <paramref name="value"/> left by <paramref name="offset"/> bits.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The number of bits to rotate by.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RotateLeft(uint value, int offset) =>
        (value << offset) | (value >> (32 - offset));

    /// <summary>
    /// Initializes the four accumulator lanes with the global seed and prime constants.
    /// </summary>
    /// <param name="v1">Accumulator lane 1.</param>
    /// <param name="v2">Accumulator lane 2.</param>
    /// <param name="v3">Accumulator lane 3.</param>
    /// <param name="v4">Accumulator lane 4.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
    {
        v1 = Seed + Prime1 + Prime2;
        v2 = Seed + Prime2;
        v3 = Seed;
        v4 = Seed - Prime1;
    }

    /// <summary>
    /// Performs a single xxHash32 round on the accumulator with the given input.
    /// </summary>
    /// <param name="hash">The current accumulator value.</param>
    /// <param name="input">The input to mix into the accumulator.</param>
    /// <returns>The updated accumulator value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Round(uint hash, uint input) =>
        RotateLeft(hash + (input * Prime2), 13) * Prime1;

    /// <summary>
    /// Mixes a queued value into the hash when fewer than four values have been accumulated.
    /// </summary>
    /// <param name="hash">The current hash value.</param>
    /// <param name="queuedValue">The queued value to mix in.</param>
    /// <returns>The updated hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint QueueRound(uint hash, uint queuedValue) =>
        RotateLeft(hash + (queuedValue * Prime3), 17) * Prime4;

    /// <summary>
    /// Combines the four accumulator lanes into a single hash value.
    /// </summary>
    /// <param name="v1">Accumulator lane 1.</param>
    /// <param name="v2">Accumulator lane 2.</param>
    /// <param name="v3">Accumulator lane 3.</param>
    /// <param name="v4">Accumulator lane 4.</param>
    /// <returns>The combined hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint MixState(uint v1, uint v2, uint v3, uint v4) =>
        RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);

    /// <summary>
    /// Applies the xxHash32 finalization avalanche to ensure all bits of the input affect the output.
    /// </summary>
    /// <param name="hash">The hash value to finalize.</param>
    /// <returns>The finalized hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint MixFinal(uint hash)
    {
        hash ^= hash >> 15;
        hash *= Prime2;
        hash ^= hash >> 13;
        hash *= Prime3;
        hash ^= hash >> 16;
        return hash;
    }

    /// <summary>
    /// Generates a random seed value used to initialize the hash state per process.
    /// </summary>
    /// <returns>A random 32-bit unsigned integer seed.</returns>
    internal static uint GenerateGlobalSeed()
    {
        var bytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    /// Returns the initial hash state when fewer than four values have been added.
    /// </summary>
    /// <returns>The seed mixed with <see cref="Prime5"/>.</returns>
    internal static uint MixEmptyState() => Seed + Prime5;

    /// <summary>
    /// Adds a single hash code integer to the internal state, advancing the accumulator.
    /// </summary>
    /// <param name="value">The hash code value to add.</param>
    internal void AddHash(int value)
    {
        var val = (uint)value;
        var previousLength = _length++;
        var position = previousLength % 4;

        if (position == 0)
        {
            _queue1 = val;
        }
        else if (position == 1)
        {
            _queue2 = val;
        }
        else if (position == 2)
        {
            _queue3 = val;
        }
        else
        {
            if (previousLength == 3)
            {
                Initialize(out _v1, out _v2, out _v3, out _v4);
            }

            _v1 = Round(_v1, _queue1);
            _v2 = Round(_v2, _queue2);
            _v3 = Round(_v3, _queue3);
            _v4 = Round(_v4, val);
        }
    }
}

#endif
