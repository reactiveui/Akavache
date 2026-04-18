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
/// Lightweight one-shot <see cref="IObservable{T}"/> used as the worker-to-caller reply
/// channel inside <see cref="SqliteOperationQueue"/>. Semantically equivalent to
/// <c>AsyncSubject&lt;T&gt;</c> — emits at most one value and then completes, or emits an
/// error — but avoids the per-instance <c>ImmutableList&lt;IObserver&gt;</c> allocation,
/// the broadcast iteration, and the dispose-tracking that <c>Subject</c>/<c>AsyncSubject</c>
/// carry for the multi-subscriber contract we don't need.
/// </summary>
/// <remarks>
/// <para>
/// Single-subscriber contract: only one call to <see cref="Subscribe"/> is supported per
/// instance. Subsequent subscribers will throw <see cref="InvalidOperationException"/>.
/// This is fine for the reply-channel use case where exactly one caller is awaiting the
/// result.
/// </para>
/// <para>
/// The producer calls <see cref="SetResult"/> or <see cref="SetError"/> from the worker
/// thread. The consumer calls <see cref="Subscribe"/> on its own thread. The two orderings
/// — subscribe-first vs set-first — are both supported and the observer always receives
/// exactly one notification pair (OnNext+OnCompleted, or OnError).
/// </para>
/// </remarks>
/// <typeparam name="T">The type of value produced by the reply.</typeparam>
internal sealed class SqliteReplyObservable<T> : IObservable<T>
{
    /// <summary>State machine value: no result posted, no subscriber attached.</summary>
    internal const int StatePending = 0;

    /// <summary>State machine value: <see cref="_value"/> populated (either already dispatched to the subscriber or stashed for a late-subscribe replay).</summary>
    internal const int StateSuccess = 1;

    /// <summary>State machine value: <see cref="_error"/> populated (either already dispatched or stashed for replay).</summary>
    internal const int StateError = 2;

    /// <summary>
    /// Synchronizes access to the mutable fields below. Producer (worker thread) and
    /// consumer (caller thread) may race on <see cref="Subscribe"/> / <see cref="SetResult"/>
    /// / <see cref="SetError"/>, so every read-modify-write happens under this lock.
    /// On net9+ this is a first-class <c>System.Threading.Lock</c>.
    /// </summary>
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif

    /// <summary>The active subscriber, or <see langword="null"/> if nobody is waiting.</summary>
    private IObserver<T>? _observer;

    /// <summary>The captured result when <see cref="_state"/> is <see cref="StateSuccess"/>.</summary>
    private T? _value;

    /// <summary>The captured exception when <see cref="_state"/> is <see cref="StateError"/>.</summary>
    private Exception? _error;

    /// <summary>Current position in the pending → success/error state machine.</summary>
    private int _state;

    /// <summary>Set to <see langword="true"/> once <see cref="Subscribe"/> has been called — enforces the single-subscriber contract.</summary>
    private bool _subscribed;

    /// <summary>
    /// Signals success. Invoked by the producer (the sqlite worker thread) with the
    /// result of the work item. If a subscriber is already waiting it receives
    /// <c>OnNext</c>+<c>OnCompleted</c> inline; otherwise the value is stashed for the
    /// next <see cref="Subscribe"/> call to replay.
    /// </summary>
    /// <param name="value">The result value to publish.</param>
    public void SetResult(T value)
    {
        IObserver<T>? observer;
        lock (_gate)
        {
            observer = TryTransitionToSuccess(ref _state, ref _value, ref _observer, value);
        }

        DeliverResult(observer, value);
    }

    /// <summary>
    /// Signals failure. Invoked by the producer when the work item throws. If a
    /// subscriber is already waiting it receives <c>OnError</c> inline; otherwise the
    /// exception is stashed for the next <see cref="Subscribe"/> call to replay.
    /// </summary>
    /// <param name="error">The exception to publish.</param>
    public void SetError(Exception error)
    {
        IObserver<T>? observer;
        lock (_gate)
        {
            observer = TryTransitionToError(ref _state, ref _error, ref _observer, error);
        }

        observer?.OnError(error);
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentExceptionHelper.ThrowIfNull(observer);

        int capturedState;
        T? capturedValue;
        Exception? capturedError;

        lock (_gate)
        {
            (capturedState, capturedValue, capturedError) = CaptureAndSubscribe(
                ref _subscribed, ref _state, ref _value, ref _error, ref _observer, observer);
        }

        if (capturedState == StatePending)
        {
            return Disposable.Empty;
        }

        ReplayTo(observer, capturedState, capturedValue, capturedError);
        return Disposable.Empty;
    }

    /// <summary>
    /// Attempts to transition from <see cref="StatePending"/> to <see cref="StateSuccess"/>.
    /// Returns the observer to notify outside the lock, or <see langword="null"/> if the
    /// state was already terminal (second-call no-op).
    /// </summary>
    /// <param name="state">The state field.</param>
    /// <param name="valueSlot">The value stash field.</param>
    /// <param name="observerSlot">The observer field (cleared on transition).</param>
    /// <param name="value">The result value.</param>
    /// <returns>The observer to deliver to, or null.</returns>
    internal static IObserver<T>? TryTransitionToSuccess(
        ref int state,
        ref T? valueSlot,
        ref IObserver<T>? observerSlot,
        T value)
    {
        if (state != StatePending)
        {
            return null;
        }

        valueSlot = value;
        state = StateSuccess;
        var observer = observerSlot;
        observerSlot = null;
        return observer;
    }

    /// <summary>
    /// Attempts to transition from <see cref="StatePending"/> to <see cref="StateError"/>.
    /// Returns the observer to notify outside the lock, or <see langword="null"/> if the
    /// state was already terminal (second-call no-op).
    /// </summary>
    /// <param name="state">The state field.</param>
    /// <param name="errorSlot">The error stash field.</param>
    /// <param name="observerSlot">The observer field (cleared on transition).</param>
    /// <param name="error">The exception to stash.</param>
    /// <returns>The observer to deliver to, or null.</returns>
    internal static IObserver<T>? TryTransitionToError(
        ref int state,
        ref Exception? errorSlot,
        ref IObserver<T>? observerSlot,
        Exception error)
    {
        if (state != StatePending)
        {
            return null;
        }

        errorSlot = error;
        state = StateError;
        var observer = observerSlot;
        observerSlot = null;
        return observer;
    }

    /// <summary>
    /// Captures the current state under the lock, enforces the single-subscriber contract,
    /// and sets the observer if the state is still pending.
    /// </summary>
    /// <param name="subscribed">The subscribed flag.</param>
    /// <param name="state">The state field.</param>
    /// <param name="value">The value stash field.</param>
    /// <param name="error">The error stash field.</param>
    /// <param name="observerSlot">The observer field.</param>
    /// <param name="observer">The subscribing observer.</param>
    /// <returns>The captured state, value, and error at the time of subscription.</returns>
    internal static (int State, T? Value, Exception? Error) CaptureAndSubscribe(
        ref bool subscribed,
        ref int state,
        ref T? value,
        ref Exception? error,
        ref IObserver<T>? observerSlot,
        IObserver<T> observer)
    {
        if (subscribed)
        {
            throw new InvalidOperationException(
                $"{nameof(SqliteReplyObservable<T>)} only supports a single subscriber.");
        }

        subscribed = true;
        var s = state;
        var v = value;
        var e = error;

        if (s == StatePending)
        {
            observerSlot = observer;
        }

        return (s, v, e);
    }

    /// <summary>
    /// Delivers a success result to the observer if it is not null.
    /// </summary>
    /// <param name="observer">The observer, or null if no subscriber was attached.</param>
    /// <param name="value">The result value.</param>
    internal static void DeliverResult(IObserver<T>? observer, T value)
    {
        if (observer is null)
        {
            return;
        }

        observer.OnNext(value);
        observer.OnCompleted();
    }

    /// <summary>
    /// Replays the already-captured terminal state to <paramref name="observer"/>.
    /// </summary>
    /// <param name="observer">The subscriber to notify.</param>
    /// <param name="state">The terminal state (<see cref="StateSuccess"/> or <see cref="StateError"/>).</param>
    /// <param name="value">The captured value, valid when <paramref name="state"/> is <see cref="StateSuccess"/>.</param>
    /// <param name="error">The captured exception, valid when <paramref name="state"/> is <see cref="StateError"/>.</param>
    internal static void ReplayTo(IObserver<T> observer, int state, T? value, Exception? error)
    {
        if (state == StateSuccess)
        {
            observer.OnNext(value!);
            observer.OnCompleted();
            return;
        }

        observer.OnError(error!);
    }
}
