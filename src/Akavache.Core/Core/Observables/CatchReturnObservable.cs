// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core.Observables;

/// <summary>
/// Error-swallowing operator that forwards the source sequence and, on any
/// <see cref="IObserver{T}.OnError"/> from the source, emits a stored fallback value
/// followed by <see cref="IObserver{T}.OnCompleted"/>. Replaces the ubiquitous
/// <c>source.Catch&lt;T, Exception&gt;(static _ =&gt; Observable.Return(fallback))</c>
/// pattern with a single operator + observer pair, avoiding the
/// <see cref="Observable.Return{TResult}(TResult)"/> wrapper and the catch-selector
/// lambda allocation.
/// </summary>
/// <remarks>
/// Only the constant-fallback shape is covered here — callers that need to chain
/// another observable on error should still use <c>Catch</c>. This covers the
/// overwhelming majority of sites in <c>Akavache.Sqlite3.SqliteBlobCache</c>'s
/// dispose/checkpoint pipelines where the fallback is always <see cref="Unit.Default"/>
/// or another cheap constant.
/// </remarks>
/// <typeparam name="T">The element type of the source observable.</typeparam>
/// <param name="source">The source observable whose values are forwarded verbatim.</param>
/// <param name="fallback">The value emitted if the source errors. Forwarded via <see cref="IObserver{T}.OnNext"/> followed by <see cref="IObserver{T}.OnCompleted"/>.</param>
internal sealed class CatchReturnObservable<T>(IObservable<T> source, T fallback) : IObservable<T>
{
    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer) =>
        source.Subscribe(new CatchReturnObserver(observer, fallback));

    /// <summary>
    /// Forwarding observer that passes <see cref="OnNext"/>/<see cref="OnCompleted"/>
    /// through and replaces <see cref="OnError"/> with an inline emit of the stored
    /// fallback followed by terminal <see cref="IObserver{T}.OnCompleted"/>.
    /// </summary>
    /// <param name="downstream">The downstream observer receiving the forwarded signals.</param>
    /// <param name="fallback">The fallback value to emit when the source errors.</param>
    private sealed class CatchReturnObserver(IObserver<T> downstream, T fallback) : IObserver<T>
    {
        /// <inheritdoc/>
        public void OnNext(T value) => downstream.OnNext(value);

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            downstream.OnNext(fallback);
            downstream.OnCompleted();
        }

        /// <inheritdoc/>
        public void OnCompleted() => downstream.OnCompleted();
    }
}
