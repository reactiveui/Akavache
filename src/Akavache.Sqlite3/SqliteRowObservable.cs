// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Akavache.Helpers;

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Multi-value single-subscriber <see cref="IObservable{T}"/> used as the worker-to-caller
/// channel for <c>GetMany</c> / <c>GetAll</c> / <c>GetAllKeys</c> — anything that emits one
/// item per SQLite row. Complements <see cref="SqliteReplyObservable{T}"/> (which is
/// one-shot) for the streaming case.
/// </summary>
/// <remarks>
/// <para>
/// The worker thread pushes rows by calling <see cref="OnNext"/> directly — no boxed
/// enumerator, no intermediate <see cref="List{T}"/>, no <c>SelectMany</c> operator
/// chain. When the query finishes the worker calls <see cref="OnCompleted"/>, or
/// <see cref="OnError"/> if <c>sqlite3_step</c> fails partway through.
/// </para>
/// <para>
/// Single-subscriber contract: a second call to <see cref="Subscribe"/> throws
/// <see cref="InvalidOperationException"/>. This is fine for the queue-reply use case
/// where exactly one caller is streaming rows out.
/// </para>
/// <para>
/// <b>Cancellation via Dispose.</b> Unlike <see cref="SqliteReplyObservable{T}"/>,
/// disposing the subscription flips an internal cancel flag that the worker checks
/// between rows (via <see cref="IsCancelled"/>). Mid-<c>sqlite3_step</c> cancel isn't
/// supported — SQLite has no per-statement cancel primitive — but a disposed subscription
/// on a 10k-row scan will stop emitting within one row's worth of latency. On cancel
/// the worker transitions to <see cref="OnCompleted"/> cleanly (no <c>OnError</c>), so
/// the caller sees a truncated-but-valid sequence.
/// </para>
/// <para>
/// State machine:
/// <list type="bullet">
///   <item><description><b>Pending, no subscriber</b> — constructed, nobody has subscribed yet. Worker OnNext calls are dropped silently (see below).</description></item>
///   <item><description><b>Pending, subscriber</b> — Subscribe has been called; worker OnNext calls forward to the observer.</description></item>
///   <item><description><b>Cancelled</b> — subscriber disposed. Further OnNext calls are dropped; the next worker check of <see cref="IsCancelled"/> is expected to short-circuit the scan and call OnCompleted.</description></item>
///   <item><description><b>Completed</b> — OnCompleted was called. Further calls are no-ops.</description></item>
///   <item><description><b>Errored</b> — OnError was called. Further calls are no-ops.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Pre-subscribe buffering.</b> If the worker pushes rows before the caller
/// subscribes, those rows are buffered in a <see cref="List{T}"/> and drained under
/// the lock when <see cref="Subscribe"/> is called. This guarantees no rows are lost
/// even when the worker dequeues before the caller attaches. The drain happens before
/// <c>_observer</c> is set, so the worker blocks on the lock during replay, preserving
/// row ordering.
/// </para>
/// </remarks>
/// <typeparam name="T">The row type emitted.</typeparam>
internal sealed class SqliteRowObservable<T> : IObservable<T>
{
    /// <summary>State value: active; worker may emit, observer may subscribe.</summary>
    private const int StatePending = 0;

    /// <summary>State value: <see cref="OnCompleted"/> has fired. No further notifications.</summary>
    private const int StateCompleted = 1;

    /// <summary>State value: <see cref="OnError"/> has fired. No further notifications.</summary>
    private const int StateErrored = 2;

    /// <summary>State value: subscriber disposed mid-stream. Worker should short-circuit and call <see cref="OnCompleted"/>.</summary>
    private const int StateCancelled = 3;

    /// <summary>
    /// Synchronizes every read-modify-write on <see cref="_observer"/>, <see cref="_state"/>,
    /// and <see cref="_subscribed"/>. The lock is held only long enough to transition
    /// state — observer callbacks fire outside the lock so a slow observer cannot stall
    /// the worker producing the next row.
    /// </summary>
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif

    /// <summary>The active subscriber, or <see langword="null"/> before Subscribe / after terminal state.</summary>
    private IObserver<T>? _observer;

    /// <summary>Current position in the <see cref="StatePending"/>/<see cref="StateCompleted"/>/<see cref="StateErrored"/>/<see cref="StateCancelled"/> state machine.</summary>
    private int _state;

    /// <summary>Set to <see langword="true"/> once <see cref="Subscribe"/> has been called — enforces the single-subscriber contract.</summary>
    private bool _subscribed;

    /// <summary>Rows buffered before the subscriber attached. Drained on Subscribe, then set to null.</summary>
    private List<T>? _buffer;

    /// <summary>
    /// Gets a value indicating whether the subscriber has disposed the subscription or
    /// the stream has otherwise reached a terminal state. The worker thread should read
    /// this between rows and stop emitting (falling through to <see cref="OnCompleted"/>)
    /// when it becomes <see langword="true"/>.
    /// </summary>
    public bool IsCancelled => Volatile.Read(ref _state) != StatePending;

    /// <summary>
    /// Pushes a row to the subscriber. Worker thread calls this per <c>SQLITE_ROW</c>
    /// return from <c>sqlite3_step</c>. Silently no-ops in any non-pending state, which
    /// covers the race where the caller disposes right as the worker is iterating.
    /// </summary>
    /// <param name="value">The row to forward.</param>
    public void OnNext(T value)
    {
        IObserver<T>? observer;
        lock (_gate)
        {
            if (_state != StatePending)
            {
                return;
            }

            observer = _observer;
            if (observer is null)
            {
                _buffer ??= [];
                _buffer.Add(value);
                return;
            }
        }

        observer.OnNext(value);
    }

    /// <summary>
    /// Signals end-of-stream. Worker thread calls this once after the last row (or
    /// immediately when the result set is empty). Subsequent <see cref="OnNext"/> calls
    /// are no-ops.
    /// </summary>
    public void OnCompleted()
    {
        IObserver<T>? observer;
        lock (_gate)
        {
            // Cancelled is treated the same as Completed from the worker's perspective —
            // we're exiting cleanly, either because the caller disposed or because
            // sqlite3_step returned SQLITE_DONE. Either way, fire OnCompleted at most
            // once by transitioning through the state machine.
            if (_state is StateCompleted or StateErrored)
            {
                return;
            }

            _state = StateCompleted;
            observer = _observer;
            _observer = null;
        }

        observer?.OnCompleted();
    }

    /// <summary>
    /// Signals failure. Worker thread calls this when <c>sqlite3_step</c> returns an
    /// error or a body exception propagates out. Suppressed after terminal state.
    /// </summary>
    /// <param name="error">The exception to forward.</param>
    public void OnError(Exception error)
    {
        IObserver<T>? observer;
        lock (_gate)
        {
            if (_state is StateCompleted or StateErrored)
            {
                return;
            }

            _state = StateErrored;
            observer = _observer;
            _observer = null;
        }

        observer?.OnError(error);
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentExceptionHelper.ThrowIfNull(observer);

        lock (_gate)
        {
            if (_subscribed)
            {
                throw new InvalidOperationException(
                    $"{nameof(SqliteRowObservable<T>)} only supports a single subscriber. Multi-subscriber semantics are intentionally not supported — bridge through an Rx <c>Publish</c> if you need them.");
            }

            _subscribed = true;
        }

        // Phase 1: snapshot any rows buffered before we subscribed.
        // _observer stays null so the worker keeps buffering during the drain.
        var snapshot = TakeBufferSnapshot();
        DrainBuffer(observer, snapshot);

        // Phase 2: set the observer so the worker forwards directly from now on.
        // Grab any rows that arrived during Phase 1 drain.
        bool terminal;
        T[]? gap;
        lock (_gate)
        {
            (terminal, gap) = CaptureGapAndSetObserver(
                ref _state,
                ref _buffer,
                ref _observer,
                observer);
        }

        DrainBuffer(observer, gap);

        if (terminal)
        {
            observer.OnCompleted();
            return Disposable.Empty;
        }

        return new CancellationDisposable(this);
    }

    /// <summary>
    /// Captures any gap rows buffered during Phase 1 drain and sets the observer
    /// if the stream is still pending. Extracted from the lock body in Subscribe
    /// so the pattern-match branches are directly testable.
    /// </summary>
    /// <param name="state">The state field.</param>
    /// <param name="buffer">The buffer field (cleared on exit).</param>
    /// <param name="observerSlot">The observer field (set if not terminal).</param>
    /// <param name="observer">The subscribing observer.</param>
    /// <returns>Whether the stream is terminal, and the gap snapshot.</returns>
    internal static (bool Terminal, T[]? Gap) CaptureGapAndSetObserver(
        ref int state,
        ref List<T>? buffer,
        ref IObserver<T>? observerSlot,
        IObserver<T> observer)
    {
        var terminal = state is StateCompleted or StateErrored;
        var gap = buffer is { Count: > 0 } ? buffer.ToArray() : null;
        buffer = null;

        if (!terminal)
        {
            observerSlot = observer;
        }

        return (terminal, gap);
    }

    /// <summary>
    /// Flips the state to <see cref="StateCancelled"/> so the worker's next
    /// <see cref="IsCancelled"/> check short-circuits the scan. No observer notification
    /// is sent — the subscriber asked to stop listening, so firing <c>OnCompleted</c>
    /// into the disposed subscription would be wasted work.
    /// </summary>
    internal void CancelFromDispose()
    {
        lock (_gate)
        {
            if (_state != StatePending)
            {
                return;
            }

            _state = StateCancelled;
            _observer = null;
        }
    }

    /// <summary>
    /// Replays buffered rows to the observer. Runs outside the lock on an independent snapshot.
    /// </summary>
    /// <param name="observer">The observer to forward buffered rows to.</param>
    /// <param name="buffered">The buffered rows, or <see langword="null"/> if none were captured.</param>
    private static void DrainBuffer(IObserver<T> observer, T[]? buffered)
    {
        if (buffered is null)
        {
            return;
        }

        foreach (var item in buffered)
        {
            observer.OnNext(item);
        }
    }

    /// <summary>
    /// Takes a snapshot of the current buffer under the lock and clears it.
    /// The worker keeps buffering into a fresh list while the caller drains the snapshot.
    /// </summary>
    /// <returns>An array snapshot of buffered rows, or <see langword="null"/> if the buffer was empty.</returns>
    private T[]? TakeBufferSnapshot()
    {
        lock (_gate)
        {
            if (_buffer is not { Count: > 0 })
            {
                return null;
            }

            var snapshot = _buffer.ToArray();
            _buffer.Clear();
            return snapshot;
        }
    }

    /// <summary>
    /// Subscription disposable returned from <see cref="Subscribe"/>. Disposing it
    /// transitions the stream into <see cref="StateCancelled"/> so the worker stops
    /// iterating at the next row boundary. Kept as a dedicated sealed class rather
    /// than a lambda via <c>Disposable.Create</c> to avoid the closure allocation on
    /// the Subscribe path.
    /// </summary>
    /// <param name="parent">The stream to flag as cancelled on dispose.</param>
    private sealed class CancellationDisposable(SqliteRowObservable<T> parent) : IDisposable
    {
        /// <summary>Non-zero once <see cref="Dispose"/> has run — idempotent marker.</summary>
        private int _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            parent.CancelFromDispose();
        }
    }
}
