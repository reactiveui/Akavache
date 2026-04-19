// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

using Akavache.Helpers;

namespace Akavache.Core.Observables;

/// <summary>
/// Observable operator that walks a list of candidate keys sequentially, projects each
/// key into a one-shot <see cref="IObservable{TRaw}"/>, transforms the raw value into
/// <typeparamref name="TResult"/>, and emits the first transformed value that satisfies
/// a predicate. Errors from individual projections are swallowed (the candidate is
/// skipped and the next one is tried). If no candidate matches, completes with a single
/// emission of <paramref name="fallback"/>.
/// </summary>
/// <remarks>
/// <para>
/// <c>Subscribe</c> attempts a synchronous fast-path first: each candidate's projection
/// is subscribed and, if it completes inline (common with <c>ImmediateScheduler</c>
/// caches), the transform + predicate run on the calling thread with zero additional
/// allocations. Only when a projection completes asynchronously does the method allocate
/// an <see cref="AsyncSink"/> to track state across callbacks.
/// </para>
/// <para>
/// The projection function must return a one-shot observable (at most one <c>OnNext</c>
/// then <c>OnCompleted</c>).
/// </para>
/// </remarks>
/// <typeparam name="TKey">The type of candidate keys.</typeparam>
/// <typeparam name="TRaw">The element type emitted by the projected observable (e.g. <c>byte[]?</c>).</typeparam>
/// <typeparam name="TResult">The final result type emitted to downstream after transformation.</typeparam>
/// <param name="candidates">The ordered list of candidate keys to walk.</param>
/// <param name="project">Projects a candidate key into a one-shot observable of raw values.</param>
/// <param name="transform">Synchronous transform applied to each raw value to produce the result.</param>
/// <param name="predicate">Returns <see langword="true"/> when a transformed value is a match.</param>
/// <param name="fallback">Value emitted when no candidate matches (typically <see langword="default"/>).</param>
internal sealed class FirstMatchFromCandidatesObservable<TKey, TRaw, TResult>(
    IReadOnlyList<TKey> candidates,
    Func<TKey, IObservable<TRaw>> project,
    Func<TRaw, TResult> transform,
    Func<TResult, bool> predicate,
    TResult fallback) : IObservable<TResult>
{
    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<TResult> observer)
    {
        ArgumentExceptionHelper.ThrowIfNull(observer);

        if (candidates.Count == 0)
        {
            observer.OnNext(fallback);
            observer.OnCompleted();
            return Disposable.Empty;
        }

        // Fast-path: try each candidate synchronously. SyncProbe is a stack-only
        // struct observer — no heap allocation. If the projection completes inline
        // (ImmediateScheduler, InMemoryBlobCache), the entire loop runs without
        // creating an AsyncSink.
        return TrySyncLoop(observer);
    }

    /// <summary>
    /// Attempts to resolve every candidate synchronously. Returns
    /// <see cref="Disposable.Empty"/> when the loop completes entirely on the
    /// calling thread, or an <see cref="AsyncSink"/> subscription when a projection
    /// does not complete synchronously and async continuation is required.
    /// </summary>
    /// <param name="observer">The downstream observer.</param>
    /// <returns>The subscription disposable.</returns>
    internal IDisposable TrySyncLoop(IObserver<TResult> observer)
    {
        var probe = new SyncProbe();

        for (var i = 0; i < candidates.Count; i++)
        {
            IObservable<TRaw> projected;
            try
            {
                projected = project(candidates[i]);
            }
            catch
            {
                continue;
            }

            probe.Reset();
            var sub = projected.Subscribe(probe);

            if (!probe.Completed)
            {
                // The projection didn't complete synchronously — hand off to AsyncSink
                // which will handle this in-flight subscription and any remaining candidates.
                sub.Dispose();
                var sink = new AsyncSink(observer, candidates, project, transform, predicate, fallback, i);
                sink.TryNext();
                return sink;
            }

            sub.Dispose();

            if (probe.Errored)
            {
                continue;
            }

            if (!probe.HasValue)
            {
                continue;
            }

            TResult transformed;
            try
            {
                transformed = transform(probe.Value!);
            }
            catch
            {
                continue;
            }

            if (predicate(transformed))
            {
                observer.OnNext(transformed);
                observer.OnCompleted();
                return Disposable.Empty;
            }
        }

        observer.OnNext(fallback);
        observer.OnCompleted();
        return Disposable.Empty;
    }

    /// <summary>
    /// Lightweight observer used by the synchronous fast-path to capture the result
    /// of a one-shot projection. Cheaper than <see cref="AsyncSink"/> because it
    /// carries no downstream observer, candidate list, or delegate references.
    /// </summary>
    internal sealed class SyncProbe : IObserver<TRaw>
    {
        /// <summary>Gets a value indicating whether <c>OnNext</c> was called.</summary>
        internal bool HasValue { get; private set; }

        /// <summary>Gets a value indicating whether <c>OnError</c> was called.</summary>
        internal bool Errored { get; private set; }

        /// <summary>Gets a value indicating whether <c>OnCompleted</c> or <c>OnError</c> was called.</summary>
        internal bool Completed { get; private set; }

        /// <summary>Gets the value received via <c>OnNext</c>.</summary>
        internal TRaw? Value { get; private set; }

        /// <inheritdoc/>
        public void OnNext(TRaw value)
        {
            Value = value;
            HasValue = true;
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            Errored = true;
            Completed = true;
        }

        /// <inheritdoc/>
        public void OnCompleted() => Completed = true;

        /// <summary>Resets state for reuse across candidates.</summary>
        internal void Reset()
        {
            HasValue = false;
            Errored = false;
            Completed = false;
            Value = default;
        }
    }

    /// <summary>
    /// Heap-allocated observer used when a projection does not complete synchronously.
    /// Walks the remaining candidates via async callbacks.
    /// </summary>
    private sealed class AsyncSink(
        IObserver<TResult> downstream,
        IReadOnlyList<TKey> candidates,
        Func<TKey, IObservable<TRaw>> project,
        Func<TRaw, TResult> transform,
        Func<TResult, bool> predicate,
        TResult fallback,
        int startIndex) : IObserver<TRaw>, IDisposable
    {
        /// <summary>Index of the current candidate being evaluated.</summary>
        private int _index = startIndex;

        /// <summary>The subscription to the current candidate's projected observable.</summary>
        private IDisposable? _currentSubscription;

        /// <summary>Set once we've emitted a matching value or exhausted all candidates.</summary>
        private bool _done;

        /// <inheritdoc/>
        public void OnNext(TRaw value)
        {
            if (_done)
            {
                return;
            }

            TResult transformed;
            try
            {
                transformed = transform(value);
            }
            catch
            {
                // Transform threw — treat as non-match; OnCompleted will advance.
                return;
            }

            if (!predicate(transformed))
            {
                return;
            }

            _done = true;
            downstream.OnNext(transformed);
            downstream.OnCompleted();
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            if (_done)
            {
                return;
            }

            // Swallow the error and advance to the next candidate.
            TryNext();
        }

        /// <inheritdoc/>
        public void OnCompleted()
        {
            if (_done)
            {
                return;
            }

            // The current candidate's observable completed without a matching value.
            TryNext();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _done = true;
            Interlocked.Exchange(ref _currentSubscription, null)?.Dispose();
        }

        /// <summary>
        /// Subscribes to the next candidate's projected observable, or emits the fallback
        /// and completes if no candidates remain.
        /// </summary>
        internal void TryNext()
        {
            while (true)
            {
                if (_done)
                {
                    return;
                }

                if (_index >= candidates.Count)
                {
                    _done = true;
                    downstream.OnNext(fallback);
                    downstream.OnCompleted();
                    return;
                }

                var key = candidates[_index++];

                IObservable<TRaw> projected;
                try
                {
                    projected = project(key);
                }
                catch
                {
                    // Projection itself threw — skip this candidate.
                    continue;
                }

                var sub = projected.Subscribe(this);
                Interlocked.Exchange(ref _currentSubscription, sub);

                // If the projected observable completed synchronously, _done may
                // already be set (match found) or OnCompleted may have called
                // TryNext recursively. If neither, the subscription is async.
                return;
            }
        }
    }
}
