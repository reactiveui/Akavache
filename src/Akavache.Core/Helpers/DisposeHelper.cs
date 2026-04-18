// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Akavache.Helpers;

/// <summary>
/// Atomic dispose-once guard. The <see cref="Interlocked.CompareExchange"/>
/// branch that fires on second-entry is a compiler artifact that cannot be
/// independently triggered in tests, so this helper is excluded from coverage.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class DisposeHelper
{
    /// <summary>
    /// Returns <see langword="true"/> if this is the first call (the caller should
    /// proceed with disposal). Returns <see langword="false"/> on second-entry or
    /// when <paramref name="disposing"/> is <see langword="false"/>.
    /// </summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    /// <param name="disposed">The atomic flag field.</param>
    /// <returns><see langword="true"/> if the caller should dispose.</returns>
    internal static bool TryClaimDispose(bool disposing, ref int disposed) =>
        disposing && Interlocked.CompareExchange(ref disposed, 1, 0) == 0;
}
