// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Core;
#else
namespace Akavache.Core;
#endif

/// <summary>
/// Normalisation helpers for <see cref="RxVoid"/>-valued observable pipelines. Centralises
/// the "discard incoming emissions and signal <see cref="RxVoid.Default"/>" pattern that
/// previously appeared inline as <c>.Select(static _ =&gt; Unit.Default)</c> across the
/// library. Perf is identical (both forms end up with a compiler-cached static delegate),
/// the value is readability and a single point of change.
/// </summary>
internal static class ObservableUnitExtensions
{
    /// <summary>Unit-normalisation helpers for an observable of any element type.</summary>
    /// <typeparam name="T">The element type of the source observable (ignored).</typeparam>
    /// <param name="source">The observable whose emissions should be normalised to <see cref="RxVoid.Default"/>.</param>
    extension<T>(IObservable<T> source)
    {
        /// <summary>
        /// Projects every emission of <paramref name="source"/> onto <see cref="RxVoid.Default"/>,
        /// producing an <see cref="IObservable{T}"/> of <see cref="RxVoid"/>. Equivalent to
        /// <c>source.Select(static _ =&gt; Unit.Default)</c> but clearer at the call site.
        /// </summary>
        /// <returns>An observable that emits <see cref="RxVoid.Default"/> once per emission of <paramref name="source"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IObservable<RxVoid> SelectUnit() =>
            new SelectConstantObservable<T, RxVoid>(source, RxVoid.Default);
    }
}
