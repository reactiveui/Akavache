// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core.Observables;

/// <summary>
/// Fused <c>.SelectMany(first).SelectMany(second)</c> operator that chains two one-shot
/// async projections in a single operator allocation. The source emits a value, it's
/// projected through <paramref name="first"/> producing an intermediate observable, whose
/// single emission is then projected through <paramref name="second"/> producing the
/// final result. Errors at any stage propagate to the downstream observer.
/// </summary>
/// <typeparam name="TSource">The source element type.</typeparam>
/// <typeparam name="TMid">The intermediate element type produced by the first projection.</typeparam>
/// <typeparam name="TResult">The final element type produced by the second projection.</typeparam>
/// <param name="source">The source observable.</param>
/// <param name="first">First projection: source element → intermediate observable.</param>
/// <param name="second">Second projection: intermediate element → result observable.</param>
internal sealed class SelectManyThenObservable<TSource, TMid, TResult>(
    IObservable<TSource> source,
    Func<TSource, IObservable<TMid>> first,
    Func<TMid, IObservable<TResult>> second) : IObservable<TResult>
{
    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<TResult> observer) =>
        source.Subscribe(new SourceObserver(observer, first, second));

    /// <summary>Receives the source value and subscribes to the first projection.</summary>
    private sealed class SourceObserver(
        IObserver<TResult> downstream,
        Func<TSource, IObservable<TMid>> first,
        Func<TMid, IObservable<TResult>> second) : IObserver<TSource>
    {
        /// <inheritdoc/>
        public void OnNext(TSource value)
        {
            try
            {
                first(value).Subscribe(new MidObserver(downstream, second));
            }
            catch (Exception ex)
            {
                downstream.OnError(ex);
            }
        }

        /// <inheritdoc/>
        public void OnError(Exception error) => downstream.OnError(error);

        /// <inheritdoc/>
        public void OnCompleted() => downstream.OnCompleted();
    }

    /// <summary>Receives the intermediate value and subscribes to the second projection.</summary>
    private sealed class MidObserver(
        IObserver<TResult> downstream,
        Func<TMid, IObservable<TResult>> second) : IObserver<TMid>
    {
        /// <inheritdoc/>
        public void OnNext(TMid value)
        {
            try
            {
                second(value).Subscribe(new FinalObserver(downstream));
            }
            catch (Exception ex)
            {
                downstream.OnError(ex);
            }
        }

        /// <inheritdoc/>
        public void OnError(Exception error) => downstream.OnError(error);

        /// <inheritdoc/>
        public void OnCompleted() => downstream.OnCompleted();
    }

    /// <summary>Forwards the final result to downstream.</summary>
    private sealed class FinalObserver(IObserver<TResult> downstream) : IObserver<TResult>
    {
        /// <inheritdoc/>
        public void OnNext(TResult value) => downstream.OnNext(value);

        /// <inheritdoc/>
        public void OnError(Exception error) => downstream.OnError(error);

        /// <inheritdoc/>
        public void OnCompleted() => downstream.OnCompleted();
    }
}
