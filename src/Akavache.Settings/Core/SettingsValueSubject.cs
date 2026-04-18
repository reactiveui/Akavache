// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Akavache.Helpers;

namespace Akavache.Settings.Core;

/// <summary>
/// Lightweight BehaviorSubject-like primitive used by <see cref="SettingsStream{T}"/> to
/// hold a property's latest value and broadcast updates to any number of live subscribers.
/// Exists as an alternative to <c>System.Reactive.Subjects.BehaviorSubject&lt;T&gt;</c>
/// for two reasons:
/// <list type="bullet">
///   <item>
///     <description>
///       Avoids the <c>ImmutableList</c>-backed observer-list allocation that Subject
///       types in System.Reactive use internally. We copy-on-write a plain array instead —
///       still O(n) on subscribe/unsubscribe, but with no secondary node allocations.
///     </description>
///   </item>
///   <item>
///     <description>
///       Lets us use <c>System.Threading.Lock</c> on net9+ for the synchronization gate,
///       which is cheaper than monitor-on-object. The standard Subject types are locked
///       to the classic monitor path for binary compatibility.
///     </description>
///   </item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// Semantics match <c>BehaviorSubject&lt;T&gt;</c> for the subset we care about:
/// <list type="bullet">
///   <item><description>Always has a current value (supplied to the constructor as the seed).</description></item>
///   <item><description><see cref="OnNext"/> replaces the current value and broadcasts to every live subscriber.</description></item>
///   <item><description><see cref="Subscribe"/> replays the current value to the new observer synchronously, then streams subsequent updates.</description></item>
///   <item><description>Disposing the subject completes every outstanding subscription.</description></item>
/// </list>
/// Unlike <c>BehaviorSubject&lt;T&gt;</c> this type does not surface <c>OnError</c> at
/// all — Settings cold loads swallow their errors upstream (see <see cref="SettingsStream{T}"/>),
/// so we do not need error bookkeeping in the subject itself.
/// </para>
/// <para>
/// Thread safety: producer (<see cref="OnNext"/>) and consumers
/// (<see cref="Subscribe"/>/unsubscribe) may be called concurrently from any thread.
/// Broadcasting happens outside the lock so a slow observer cannot block writers.
/// </para>
/// </remarks>
/// <typeparam name="T">The value type the subject carries.</typeparam>
internal sealed class SettingsValueSubject<T> : IObservable<T>, IDisposable
{
    /// <summary>
    /// Synchronizes every read-modify-write on <see cref="_current"/>, <see cref="_observers"/>,
    /// and <see cref="_completed"/>. On net9+ this is a first-class
    /// <c>System.Threading.Lock</c> which the JIT can inline more aggressively than
    /// monitor-on-<see cref="object"/>; on older TFMs it degrades to the conventional
    /// reference-object lock.
    /// </summary>
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif

    /// <summary>
    /// The latest value. Guarded by <see cref="_gate"/> for writes; reads via
    /// <see cref="Value"/> do not take the lock because a torn read on a value type is
    /// harmless here (a fresh subscriber would see the previous or next value, both of
    /// which are valid published states).
    /// </summary>
    private T _current;

    /// <summary>
    /// Live subscriber list. Copy-on-write — every subscribe/unsubscribe allocates a new
    /// array of length <c>Length ± 1</c>, and <see cref="OnNext"/> iterates the array
    /// snapshot it captured under the lock. This lets broadcasting run without holding
    /// the gate, so a slow observer can't block writers.
    /// </summary>
    private IObserver<T>[] _observers = [];

    /// <summary>
    /// Set to <see langword="true"/> once <see cref="Dispose"/> has run. Further
    /// <see cref="OnNext"/> calls become no-ops and fresh subscribers receive
    /// <c>OnNext(current) + OnCompleted()</c> inline.
    /// </summary>
    private bool _completed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsValueSubject{T}"/> class
    /// seeded with <paramref name="initialValue"/>. The first <see cref="Subscribe"/>
    /// call sees this value.
    /// </summary>
    /// <param name="initialValue">The value to publish to subscribers before any <see cref="OnNext"/> call.</param>
    public SettingsValueSubject(T initialValue) => _current = initialValue;

    /// <summary>
    /// Gets the latest value. Synchronous, non-blocking, non-allocating. Updated
    /// immediately inside <see cref="OnNext"/> before the broadcast loop.
    /// </summary>
    public T Value => _current;

    /// <summary>
    /// Replaces the current value and broadcasts it to every live subscriber. Called
    /// from the producer thread (typically the owning <see cref="SettingsStream{T}.Set"/>
    /// or the cold-load completion path). No-op once the subject has been disposed.
    /// </summary>
    /// <param name="value">The new value to publish.</param>
    public void OnNext(T value)
    {
        IObserver<T>[] snapshot;
        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            _current = value;
            snapshot = _observers;
        }

        // Broadcast outside the lock so a slow observer can't stall other writers.
        // `snapshot` is a local reference to the pre-mutation array; a concurrent
        // subscribe/unsubscribe will allocate a brand-new array and never touch this
        // one, so iterating without the lock is safe.
        foreach (var observer in snapshot)
        {
            observer.OnNext(value);
        }
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentExceptionHelper.ThrowIfNull(observer);

        T snapshot;
        lock (_gate)
        {
            if (_completed)
            {
                // Replay the latest value and terminate, matching BehaviorSubject's
                // "late subscriber gets the final state" contract. No need to register.
                observer.OnNext(_current);
                observer.OnCompleted();
                return Disposable.Empty;
            }

            // Copy-on-write add. Allocating a fresh array keeps concurrent broadcasts
            // iterating over the pre-insert snapshot — no interference with in-flight
            // OnNext calls on other threads.
            var next = new IObserver<T>[_observers.Length + 1];
            Array.Copy(_observers, next, _observers.Length);
            next[_observers.Length] = observer;
            _observers = next;
            snapshot = _current;
        }

        // Replay the current value to the fresh subscriber outside the lock, matching
        // BehaviorSubject's replay-on-subscribe semantics.
        observer.OnNext(snapshot);
        return new Unsubscriber(this, observer);
    }

    /// <summary>
    /// Completes every outstanding subscription and disables future <see cref="OnNext"/>
    /// calls. Called by <see cref="SettingsStream{T}.Dispose"/> when the owning settings
    /// store is torn down.
    /// </summary>
    public void Dispose()
    {
        IObserver<T>[] snapshot;
        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            snapshot = _observers;
            _observers = [];
        }

        foreach (var observer in snapshot)
        {
            observer.OnCompleted();
        }
    }

    /// <summary>
    /// Removes <paramref name="observer"/> from the subscriber list via the same
    /// copy-on-write strategy used for <see cref="Subscribe"/>. Idempotent — duplicate
    /// calls after the first do nothing.
    /// </summary>
    /// <param name="observer">The observer to remove.</param>
    private void Unsubscribe(IObserver<T> observer)
    {
        lock (_gate)
        {
            var old = _observers;
            var index = Array.IndexOf(old, observer);
            if (index < 0)
            {
                return;
            }

            if (old.Length == 1)
            {
                _observers = [];
                return;
            }

            var next = new IObserver<T>[old.Length - 1];
            Array.Copy(old, 0, next, 0, index);
            Array.Copy(old, index + 1, next, index, old.Length - index - 1);
            _observers = next;
        }
    }

    /// <summary>
    /// Per-subscription disposable returned from <see cref="Subscribe"/>. Delegates back
    /// to <see cref="Unsubscribe"/> on the owning subject when disposed. Kept as a
    /// separate sealed class rather than <c>Disposable.Create</c> to avoid the closure
    /// allocation on the hot subscribe path.
    /// </summary>
    /// <param name="parent">The owning subject.</param>
    /// <param name="observer">The observer to remove on dispose.</param>
    private sealed class Unsubscriber(SettingsValueSubject<T> parent, IObserver<T> observer) : IDisposable
    {
        /// <summary>Non-zero once <see cref="Dispose"/> has run — gates duplicate unsubscribe attempts.</summary>
        private int _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            parent.Unsubscribe(observer);
        }
    }
}
