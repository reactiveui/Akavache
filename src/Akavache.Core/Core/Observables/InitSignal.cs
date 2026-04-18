// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core.Observables;

/// <summary>
/// One-shot "initialization complete" latch used as the gate in front of every hot-path
/// cache operation in <c>Akavache.Sqlite3.SqliteBlobCache</c>. Semantically similar to
/// <see cref="System.Reactive.Subjects.AsyncSubject{T}"/> but exposes a synchronous
/// <see cref="IsReady"/> probe so call sites can fast-path past the gate without
/// allocating any Rx operator state once initialization has fired — the common case
/// after the first couple of operations on a newly-constructed cache.
/// </summary>
/// <remarks>
/// <para>
/// Producers (e.g. the schema-creation observable) call <see cref="Complete"/> on
/// success or <see cref="Fail"/> on error. Consumers call <see cref="Gate{T}(Func{IObservable{T}})"/>
/// which either returns the factory's observable directly (ready path, zero wrapper
/// allocation) or hands back a <see cref="GatedByInitObservable{T}"/> that parks the
/// subscription until the signal fires.
/// </para>
/// <para>
/// Contract: <see cref="Complete"/> / <see cref="Fail"/> are idempotent and may be
/// called at most once each — the first call wins; subsequent calls are no-ops. The
/// error path poisons the signal permanently so every subsequent <see cref="Gate{T}"/>
/// call short-circuits to <see cref="Observable.Throw{TResult}(System.Exception)"/>
/// with the captured exception.
/// </para>
/// </remarks>
internal sealed class InitSignal
{
    /// <summary>Pending: no terminal signal has been observed yet.</summary>
    private const int StatePending = 0;

    /// <summary>Completed successfully: <see cref="Gate{T}"/> fast-paths to the factory.</summary>
    private const int StateReady = 1;

    /// <summary>Completed with error: <see cref="Gate{T}"/> fast-paths to <see cref="Observable.Throw{TResult}(System.Exception)"/>.</summary>
    private const int StateFailed = 2;

#if NET9_0_OR_GREATER
    /// <summary>
    /// Synchronization object used to coordinate access to shared resources within the state machine.
    /// Ensures thread safety for operations that alter or query the internal state.
    /// </summary>
    private readonly System.Threading.Lock _gate = new();
#else
    /// <summary>
    /// Synchronization object used to coordinate access to shared resources within the state machine.
    /// Ensures thread safety for operations that alter or query the internal state.
    /// </summary>
    private readonly object _gate = new();
#endif

    /// <summary>Parked subscription callbacks waiting for the signal. Allocated lazily on the first cold subscription.</summary>
    private List<Action<Exception?>>? _pending;

    /// <summary>The captured exception when <see cref="_state"/> is <see cref="StateFailed"/>.</summary>
    private Exception? _error;

    /// <summary>Current position in the pending → ready/failed state machine. Volatile so readers see the latest write without taking <see cref="_gate"/>.</summary>
    private volatile int _state;

    /// <summary>Gets a value indicating whether the signal has already completed successfully.</summary>
    /// <remarks>Lock-free — backed by a volatile read of <see cref="_state"/>. Safe to call from the hot path.</remarks>
    public bool IsReady => _state == StateReady;

    /// <summary>Gets a value indicating whether the signal has reached a terminal state (success or error).</summary>
    public bool IsCompleted => _state != StatePending;

    /// <summary>
    /// Signals successful completion. Any pending gated subscriptions are released to
    /// their factory observables. Idempotent — a second call (or a call after
    /// <see cref="Fail"/>) is a no-op.
    /// </summary>
    public void Complete()
    {
        List<Action<Exception?>>? snapshot;
        lock (_gate)
        {
            if (_state != StatePending)
            {
                return;
            }

            _state = StateReady;
            snapshot = _pending;
            _pending = null;
        }

        if (snapshot is null)
        {
            return;
        }

        for (var i = 0; i < snapshot.Count; i++)
        {
            snapshot[i](null);
        }
    }

    /// <summary>
    /// Signals failure. Any pending gated subscriptions receive <see cref="IObserver{T}.OnError"/>
    /// with <paramref name="error"/>, and every subsequent <see cref="Gate{T}"/> call
    /// short-circuits to <see cref="Observable.Throw{TResult}(System.Exception)"/>.
    /// Idempotent — a second call (or a call after <see cref="Complete"/>) is a no-op.
    /// </summary>
    /// <param name="error">The terminal error to publish.</param>
    public void Fail(Exception error)
    {
        List<Action<Exception?>>? snapshot;
        lock (_gate)
        {
            if (_state != StatePending)
            {
                return;
            }

            _error = error;
            _state = StateFailed;
            snapshot = _pending;
            _pending = null;
        }

        if (snapshot is null)
        {
            return;
        }

        for (var i = 0; i < snapshot.Count; i++)
        {
            snapshot[i](error);
        }
    }

    /// <summary>
    /// Gates an inner observable behind this initialization signal. On the ready path
    /// (the common case after the first few calls on a newly-constructed cache) the
    /// method invokes <paramref name="factory"/> and returns its observable directly —
    /// no wrapper allocation. On the pending path it returns a
    /// <see cref="GatedByInitObservable{T}"/> that parks the subscription until
    /// <see cref="Complete"/> or <see cref="Fail"/> fires. On the failed path it
    /// returns a cached throwing observable carrying the captured error.
    /// </summary>
    /// <typeparam name="T">The element type of the gated observable.</typeparam>
    /// <param name="factory">Factory that produces the inner observable. Invoked lazily (never before the signal is ready) so call sites can capture state from the enclosing method without doing the work eagerly.</param>
    /// <returns>A gated observable.</returns>
    public IObservable<T> Gate<T>(Func<IObservable<T>> factory)
    {
        // Fast path: lock-free volatile read. The overwhelming majority of calls against
        // a configured cache take this branch and skip every Rx wrapper entirely.
        var state = _state;
        if (state == StateReady)
        {
            return factory();
        }

        if (state == StateFailed)
        {
            return Observable.Throw<T>(_error!);
        }

        return new GatedByInitObservable<T>(this, factory);
    }

    /// <summary>
    /// Parks a pending callback for release when the signal fires. Called by
    /// <see cref="GatedByInitObservable{T}"/> on the cold-path subscription. Returns
    /// <see langword="false"/> (with the signal state + captured error) when the
    /// signal has already transitioned out of pending — the caller should dispatch
    /// inline instead of parking.
    /// </summary>
    /// <param name="callback">Callback invoked with <see langword="null"/> on success or the captured exception on failure.</param>
    /// <param name="error">On <see langword="false"/>, receives the captured error for the failed state (otherwise <see langword="null"/>).</param>
    /// <returns><see langword="true"/> if the callback was parked; <see langword="false"/> if the signal is already terminal.</returns>
    internal bool TryPark(Action<Exception?> callback, out Exception? error)
    {
        lock (_gate)
        {
            if (_state == StatePending)
            {
                (_pending ??= []).Add(callback);
                error = null;
                return true;
            }

            error = _error;
            return false;
        }
    }
}
