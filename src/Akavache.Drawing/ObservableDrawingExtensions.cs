// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core.Observables;

namespace Akavache.Drawing;

/// <summary>Drawing-specific fluent extensions for the custom observable operators compiled into this assembly.</summary>
internal static class ObservableDrawingExtensions
{
    /// <summary>Extension members for <c>IObservable&lt;TSource&gt;</c>.</summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <param name="source">The source observable.</param>
    extension<TSource>(IObservable<TSource> source)
    {
        /// <summary>Chains two one-shot <c>SelectMany</c> projections into a single operator.</summary>
        /// <typeparam name="TMid">The intermediate element type.</typeparam>
        /// <typeparam name="TResult">The final result type.</typeparam>
        /// <param name="first">First projection.</param>
        /// <param name="second">Second projection.</param>
        /// <returns>A fused two-stage SelectMany observable.</returns>
        internal IObservable<TResult> SelectManyThen<TMid, TResult>(Func<TSource, IObservable<TMid>> first, Func<TMid, IObservable<TResult>> second) =>
            new SelectManyThenObservable<TSource, TMid, TResult>(source, first, second);
    }
}
