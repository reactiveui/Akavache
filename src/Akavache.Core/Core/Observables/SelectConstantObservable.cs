// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core.Observables;

/// <summary>
/// Projection operator that ignores every source element and emits a stored constant
/// instead, followed by <see cref="IObserver{T}.OnCompleted"/> when the source
/// completes. Replaces the common <c>.Select(_ =&gt; value)</c> pattern found in
/// insert-then-return-value chains, avoiding the per-subscription closure allocation
/// that the lambda <c>_ =&gt; value</c> would capture.
/// </summary>
/// <typeparam name="TSource">The source element type (ignored).</typeparam>
/// <typeparam name="TResult">The result element type emitted to the downstream observer.</typeparam>
/// <param name="source">The source observable whose values are ignored.</param>
/// <param name="constant">The constant value emitted for each source element.</param>
internal sealed class SelectConstantObservable<TSource, TResult>(
    IObservable<TSource> source,
    TResult constant) : IObservable<TResult>
{
    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<TResult> observer) =>
        source.Subscribe(new SelectConstantObserver(observer, constant));

    /// <summary>
    /// Forwarding observer that replaces every <see cref="OnNext"/> value with
    /// the stored constant. Error and completion signals pass through unchanged.
    /// </summary>
    private sealed class SelectConstantObserver(IObserver<TResult> downstream, TResult constant) : IObserver<TSource>
    {
        /// <inheritdoc/>
        public void OnNext(TSource value) => downstream.OnNext(constant);

        /// <inheritdoc/>
        public void OnError(Exception error) => downstream.OnError(error);

        /// <inheritdoc/>
        public void OnCompleted() => downstream.OnCompleted();
    }
}
