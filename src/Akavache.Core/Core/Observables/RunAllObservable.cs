// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

namespace Akavache.Core.Observables;

/// <summary>
/// Runs a list of one-shot <see cref="IObservable{Unit}"/> observables sequentially,
/// ignoring emitted values, and emits a single <see cref="Unit.Default"/> when all
/// have completed. If the list is empty, emits <see cref="Unit.Default"/> immediately.
/// Errors from any observable propagate to the downstream observer.
/// </summary>
/// <remarks>
/// Replaces the common patterns:
/// <list type="bullet">
///   <item><c>sources.Concat().LastOrDefaultAsync()</c> — 2 operator allocations</item>
///   <item><c>sources.SelectUnit().DefaultIfEmpty(Unit.Default).TakeLast(1)</c> — 3 operator allocations</item>
/// </list>
/// with a single operator that subscribes sequentially and signals completion.
/// </remarks>
/// <param name="sources">The list of one-shot observables to run in order.</param>
internal sealed class RunAllObservable(IReadOnlyList<IObservable<Unit>> sources) : IObservable<Unit>
{
    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<Unit> observer)
    {
        if (sources.Count == 0)
        {
            observer.OnNext(Unit.Default);
            observer.OnCompleted();
            return Disposable.Empty;
        }

        var sink = new Sink(observer, sources);
        sink.RunNext();
        return sink;
    }

    /// <summary>
    /// Stateful observer that walks the source list sequentially. Each source's
    /// values are ignored; on <c>OnCompleted</c> the next source is subscribed.
    /// When all sources have completed, emits <see cref="Unit.Default"/> and completes.
    /// </summary>
    private sealed class Sink(
        IObserver<Unit> downstream,
        IReadOnlyList<IObservable<Unit>> sources) : IObserver<Unit>, IDisposable
    {
        /// <summary>Index of the current source being observed.</summary>
        private int _index;

        /// <summary>Subscription to the current source.</summary>
        private IDisposable? _currentSubscription;

        /// <summary>Set once all sources have completed or we've been disposed.</summary>
        private bool _done;

        /// <inheritdoc/>
        public void OnNext(Unit value)
        {
            // Ignore emitted values — we only care about completion.
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            if (_done)
            {
                return;
            }

            _done = true;
            downstream.OnError(error);
        }

        /// <inheritdoc/>
        public void OnCompleted() => RunNext();

        /// <inheritdoc/>
        public void Dispose()
        {
            _done = true;
            Interlocked.Exchange(ref _currentSubscription, null)?.Dispose();
        }

        /// <summary>
        /// Subscribes to the next source, or emits Unit and completes if all are done.
        /// </summary>
        internal void RunNext()
        {
            if (_done)
            {
                return;
            }

            if (_index >= sources.Count)
            {
                _done = true;
                downstream.OnNext(Unit.Default);
                downstream.OnCompleted();
                return;
            }

            var source = sources[_index++];
            var sub = source.Subscribe(this);
            Interlocked.Exchange(ref _currentSubscription, sub);
        }
    }
}
