// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core.Observables;

/// <summary>
/// Fluent extension methods that wire the custom observable primitives in this
/// namespace in place of the equivalent Rx LINQ operator chains. Each extension is a
/// one-line delegation to the custom type's constructor so call sites can opt into
/// the faster path without spelling out the class name.
/// </summary>
internal static class ObservableFastOpsExtensions
{
    /// <summary>Extension members for <c>IObservable&lt;T&gt;</c>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source observable whose values are forwarded verbatim.</param>
    extension<T>(IObservable<T> source)
    {
        /// <summary>
        /// Swallows any source error by emitting <paramref name="fallback"/> followed by
        /// <see cref="IObserver{T}.OnCompleted"/>. Equivalent to
        /// <c>source.Catch&lt;T, Exception&gt;(static _ =&gt; Observable.Return(fallback))</c>
        /// but avoids the catch selector lambda and the <see cref="Observable.Return{TResult}(TResult)"/>
        /// wrapper allocation on the error path. See <see cref="CatchReturnObservable{T}"/>.
        /// </summary>
        /// <param name="fallback">The value emitted if the source errors.</param>
        /// <returns>An observable that never produces an error terminal.</returns>
        internal IObservable<T> CatchReturn(T fallback) =>
                new CatchReturnObservable<T>(source, fallback);
    }

    /// <summary>Extension members for <c>IObservable&lt;TIn&gt;</c>.</summary>
    /// <typeparam name="TIn">The source element type.</typeparam>
    /// <param name="source">The source observable.</param>
    extension<TIn>(IObservable<TIn> source)
    {
        /// <summary>
        /// Fused <c>Where(predicate).Select(selector)</c>. See
        /// <see cref="WhereSelectObservable{TIn, TOut}"/> for the semantics; this shim
        /// just saves the call site a <c>new WhereSelectObservable&lt;,&gt;(source, ...)</c>
        /// expression.
        /// </summary>
        /// <typeparam name="TOut">The projected element type.</typeparam>
        /// <param name="predicate">Filter applied to each source element.</param>
        /// <param name="selector">Projection applied to elements that pass <paramref name="predicate"/>.</param>
        /// <returns>A fused filter-and-project observable.</returns>
        internal IObservable<TOut> WhereSelect<TOut>(
            Func<TIn, bool> predicate,
            Func<TIn, TOut> selector) =>
                new WhereSelectObservable<TIn, TOut>(source, predicate, selector);

        /// <summary>
        /// Applies <paramref name="selector"/> and emits only non-null results.
        /// Replaces <c>.Select(f).Where(x =&gt; x is not null).Select(x =&gt; x!)</c>
        /// with a single operator allocation.
        /// </summary>
        /// <typeparam name="TOut">The projected element type.</typeparam>
        /// <param name="selector">Projection that may return <see langword="null"/>.</param>
        /// <returns>An observable that emits only non-null projected values.</returns>
        internal IObservable<TOut> TrySelect<TOut>(
                Func<TIn, TOut?> selector) =>
                new TrySelectObservable<TIn, TOut>(source, selector);
    }

    /// <summary>Extension members for <c>IObservable&lt;TSource&gt;</c>.</summary>
    /// <typeparam name="TSource">The source element type (ignored).</typeparam>
    /// <param name="source">The source observable whose values are ignored.</param>
    extension<TSource>(IObservable<TSource> source)
    {
        /// <summary>
        /// Projects every source element to a stored constant, avoiding the closure
        /// allocation of <c>.Select(_ =&gt; value)</c>. Common in insert-then-return-value
        /// chains: <c>blobCache.Insert(key, x).SelectConstant(x)</c>.
        /// See <see cref="SelectConstantObservable{TSource, TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The result element type.</typeparam>
        /// <param name="constant">The constant value emitted for each source element.</param>
        /// <returns>An observable that emits <paramref name="constant"/> for each source element.</returns>
        internal IObservable<TResult> SelectConstant<TResult>(
                TResult constant) =>
                new SelectConstantObservable<TSource, TResult>(source, constant);

        /// <summary>
        /// Chains two one-shot <c>SelectMany</c> projections into
        /// a single operator. Replaces <c>.SelectMany(a).SelectMany(b)</c> (2 operator
        /// allocations) with 1.
        /// </summary>
        /// <typeparam name="TMid">The intermediate element type.</typeparam>
        /// <typeparam name="TResult">The final result type.</typeparam>
        /// <param name="first">First projection: source → intermediate observable.</param>
        /// <param name="second">Second projection: intermediate → result observable.</param>
        /// <returns>A fused two-stage SelectMany observable.</returns>
        internal IObservable<TResult> SelectManyThen<TMid, TResult>(
            Func<TSource, IObservable<TMid>> first,
            Func<TMid, IObservable<TResult>> second) =>
                new SelectManyThenObservable<TSource, TMid, TResult>(source, first, second);
    }

    /// <summary>Extension members for <c>IObservable&lt;Unit&gt;</c>.</summary>
    /// <param name="source">The source observable.</param>
    extension(IObservable<Unit> source)
    {
        /// <summary>
        /// Convenience overload for the common <see cref="Unit"/>-returning dispose /
        /// checkpoint pipelines: <c>source.CatchReturnUnit()</c> is shorthand for
        /// <c>source.CatchReturn(Unit.Default)</c>.
        /// </summary>
        /// <returns>An observable that never produces an error terminal — errors are replaced with a single <see cref="Unit.Default"/>.</returns>
        internal IObservable<Unit> CatchReturnUnit() =>
                new CatchReturnObservable<Unit>(source, Unit.Default);
    }

    /// <summary>Extension members for <c>IReadOnlyList&lt;IObservable&lt;Unit&gt;&gt;</c>.</summary>
    /// <param name="sources">The observables to run in order.</param>
    extension(IReadOnlyList<IObservable<Unit>> sources)
    {
        /// <summary>
        /// Runs a list of one-shot <see cref="IObservable{Unit}"/> sequentially and emits
        /// a single <see cref="Unit.Default"/> when all have completed. Replaces
        /// <c>.Concat().LastOrDefaultAsync()</c> with a single operator.
        /// </summary>
        /// <returns>A one-shot observable that completes after all sources.</returns>
        internal IObservable<Unit> RunAll() =>
                new RunAllObservable(sources);
    }
}
