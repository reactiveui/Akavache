// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// Normalisation helpers for <see cref="Unit"/>-valued observable pipelines. Centralises
/// the "discard incoming emissions and signal <see cref="Unit.Default"/>" pattern that
/// previously appeared inline as <c>.Select(static _ =&gt; Unit.Default)</c> across the
/// library. Perf is identical (both forms end up with a compiler-cached static delegate),
/// the value is readability and a single point of change.
/// </summary>
internal static class ObservableUnitExtensions
{
    /// <summary>
    /// Projects every emission of <paramref name="source"/> onto <see cref="Unit.Default"/>,
    /// producing an <see cref="IObservable{T}"/> of <see cref="Unit"/>. Equivalent to
    /// <c>source.Select(static _ =&gt; Unit.Default)</c> but clearer at the call site.
    /// </summary>
    /// <typeparam name="T">The element type of the source observable (ignored).</typeparam>
    /// <param name="source">The observable whose emissions should be normalised to <see cref="Unit.Default"/>.</param>
    /// <returns>An observable that emits <see cref="Unit.Default"/> once per emission of <paramref name="source"/>.</returns>
    public static IObservable<Unit> SelectUnit<T>(this IObservable<T> source) =>
        source.Select(static _ => Unit.Default);
}
