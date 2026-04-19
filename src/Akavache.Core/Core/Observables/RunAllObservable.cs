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
/// Uses an iterative loop with a sync-completion flag to avoid stack overflow
/// when sources complete synchronously during <c>Subscribe</c>.
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
    /// <remarks>
    /// Synchronous completion is detected via a <c>_syncCompleted</c> flag so that
    /// <see cref="RunNext"/> can loop instead of recursing through
    /// <see cref="OnCompleted"/> → <see cref="RunNext"/> → <c>Subscribe</c> →
    /// <see cref="OnCompleted"/> ad infinitum. This prevents stack overflow when
    /// many sources complete inline (e.g. <c>Observable.Return</c>,
    /// <c>ImmediateScheduler</c> caches).
    /// </remarks>
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

        /// <summary>
        /// Set by <see cref="OnCompleted"/> to signal <see cref="RunNext"/> that the
        /// current source completed synchronously during <c>Subscribe</c>, so the
        /// loop should continue instead of returning.
        /// </summary>
        private bool _syncCompleted;

        /// <summary>
        /// Guards against re-entrant <see cref="RunNext"/> calls. When <c>true</c>,
        /// <see cref="OnCompleted"/> sets <see cref="_syncCompleted"/> instead of
        /// calling <see cref="RunNext"/> directly.
        /// </summary>
        private bool _looping;

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
        public void OnCompleted()
        {
            if (_done)
            {
                return;
            }

            if (_looping)
            {
                // RunNext is already on the stack — signal it to continue the loop
                // instead of recursing.
                _syncCompleted = true;
                return;
            }

            RunNext();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _done = true;
            Interlocked.Exchange(ref _currentSubscription, null)?.Dispose();
        }

        /// <summary>
        /// Subscribes to the next source, or emits Unit and completes if all are done.
        /// Uses an iterative loop: after each <c>Subscribe</c>, checks whether
        /// <see cref="OnCompleted"/> fired synchronously (via <see cref="_syncCompleted"/>)
        /// and continues the loop if so, avoiding recursive stack growth.
        /// </summary>
        internal void RunNext()
        {
            _looping = true;
            try
            {
                while (!_done && _index < sources.Count)
                {
                    _syncCompleted = false;
                    var source = sources[_index++];
                    var sub = source.Subscribe(this);
                    Interlocked.Exchange(ref _currentSubscription, sub);

                    if (!_syncCompleted)
                    {
                        // Source didn't complete synchronously — it will call
                        // OnCompleted asynchronously, which will call RunNext.
                        return;
                    }

                    // Source completed synchronously — loop to the next one.
                }
            }
            finally
            {
                _looping = false;
            }

            if (_done)
            {
                return;
            }

            _done = true;
            downstream.OnNext(Unit.Default);
            downstream.OnCompleted();
        }
    }
}
