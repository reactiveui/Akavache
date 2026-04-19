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
internal static class ObservableFastOps
{
    /// <summary>
    /// Fused <c>Where(predicate).Select(selector)</c>. See
    /// <see cref="WhereSelectObservable{TIn, TOut}"/> for the semantics; this shim
    /// just saves the call site a <c>new WhereSelectObservable&lt;,&gt;(source, ...)</c>
    /// expression.
    /// </summary>
    /// <typeparam name="TIn">The source element type.</typeparam>
    /// <typeparam name="TOut">The projected element type.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="predicate">Filter applied to each source element.</param>
    /// <param name="selector">Projection applied to elements that pass <paramref name="predicate"/>.</param>
    /// <returns>A fused filter-and-project observable.</returns>
    public static IObservable<TOut> WhereSelect<TIn, TOut>(
        this IObservable<TIn> source,
        Func<TIn, bool> predicate,
        Func<TIn, TOut> selector) =>
        new WhereSelectObservable<TIn, TOut>(source, predicate, selector);

    /// <summary>
    /// Swallows any source error by emitting <paramref name="fallback"/> followed by
    /// <see cref="IObserver{T}.OnCompleted"/>. Equivalent to
    /// <c>source.Catch&lt;T, Exception&gt;(static _ =&gt; Observable.Return(fallback))</c>
    /// but avoids the catch selector lambda and the <see cref="Observable.Return{TResult}(TResult)"/>
    /// wrapper allocation on the error path. See <see cref="CatchReturnObservable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source observable whose values are forwarded verbatim.</param>
    /// <param name="fallback">The value emitted if the source errors.</param>
    /// <returns>An observable that never produces an error terminal.</returns>
    public static IObservable<T> CatchReturn<T>(this IObservable<T> source, T fallback) =>
        new CatchReturnObservable<T>(source, fallback);

    /// <summary>
    /// Projects every source element to a stored constant, avoiding the closure
    /// allocation of <c>.Select(_ =&gt; value)</c>. Common in insert-then-return-value
    /// chains: <c>blobCache.Insert(key, x).SelectConstant(x)</c>.
    /// See <see cref="SelectConstantObservable{TSource, TResult}"/>.
    /// </summary>
    /// <typeparam name="TSource">The source element type (ignored).</typeparam>
    /// <typeparam name="TResult">The result element type.</typeparam>
    /// <param name="source">The source observable whose values are ignored.</param>
    /// <param name="constant">The constant value emitted for each source element.</param>
    /// <returns>An observable that emits <paramref name="constant"/> for each source element.</returns>
    public static IObservable<TResult> SelectConstant<TSource, TResult>(
        this IObservable<TSource> source,
        TResult constant) =>
        new SelectConstantObservable<TSource, TResult>(source, constant);

    /// <summary>
    /// Convenience overload for the common <see cref="Unit"/>-returning dispose /
    /// checkpoint pipelines: <c>source.CatchReturnUnit()</c> is shorthand for
    /// <c>source.CatchReturn(Unit.Default)</c>.
    /// </summary>
    /// <param name="source">The source observable.</param>
    /// <returns>An observable that never produces an error terminal — errors are replaced with a single <see cref="Unit.Default"/>.</returns>
    public static IObservable<Unit> CatchReturnUnit(this IObservable<Unit> source) =>
        new CatchReturnObservable<Unit>(source, Unit.Default);
}
