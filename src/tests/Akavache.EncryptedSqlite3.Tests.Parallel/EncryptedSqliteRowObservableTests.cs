// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="SqliteRowObservable{T}"/> (encrypted variant) covering state
/// transitions, cancellation, idempotent terminal calls, and single-subscriber enforcement.
/// </summary>
[Category("Akavache")]
public class EncryptedSqliteRowObservableTests
{
    /// <summary>
    /// Calling OnNext after OnCompleted is a silent no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnNext_AfterCompleted_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        var values = new List<int>();
        var completed = false;

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            v => values.Add(v),
            _ => { },
            () => completed = true));

        sut.OnNext(1);
        sut.OnCompleted();
        sut.OnNext(2);

        await Assert.That(values.Count).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo(1);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// Calling OnCompleted twice delivers OnCompleted to the observer only once.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnCompleted_Twice_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        var completedCount = 0;

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            _ => { },
            _ => { },
            () => completedCount++));

        sut.OnCompleted();
        sut.OnCompleted();

        await Assert.That(completedCount).IsEqualTo(1);
    }

    /// <summary>
    /// OnError forwards the exception to the subscribed observer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnError_ForwardsToObserver()
    {
        var sut = new SqliteRowObservable<int>();
        Exception? caught = null;

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            _ => { },
            ex => caught = ex,
            () => { }));

        var expected = new InvalidOperationException("test-error");
        sut.OnError(expected);

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Calling OnError after OnCompleted is a silent no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnError_AfterCompleted_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        var completed = false;
        Exception? caught = null;

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            _ => { },
            ex => caught = ex,
            () => completed = true));

        sut.OnCompleted();
        sut.OnError(new InvalidOperationException("late-error"));

        await Assert.That(completed).IsTrue();
        await Assert.That(caught).IsNull();
    }

    /// <summary>
    /// Subscribing to an observable that has already completed fires OnCompleted
    /// immediately without any OnNext emissions.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Subscribe_WhenAlreadyCompleted_FiresOnCompletedImmediately()
    {
        var sut = new SqliteRowObservable<int>();
        sut.OnCompleted();

        var completed = false;
        var values = new List<int>();

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            v => values.Add(v),
            _ => { },
            () => completed = true));

        await Assert.That(completed).IsTrue();
        await Assert.That(values.Count).IsEqualTo(0);
    }

    /// <summary>
    /// Subscribing to an observable that has already errored fires OnCompleted
    /// (not OnError) because the error payload is not retained.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Subscribe_WhenAlreadyErrored_FiresOnCompletedImmediately()
    {
        var sut = new SqliteRowObservable<int>();
        sut.OnError(new InvalidOperationException("early-error"));

        var completed = false;
        Exception? caught = null;

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            _ => { },
            ex => caught = ex,
            () => completed = true));

        await Assert.That(completed).IsTrue();
        await Assert.That(caught).IsNull();
    }

    /// <summary>
    /// A second call to Subscribe throws <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Subscribe_Twice_Throws()
    {
        var sut = new SqliteRowObservable<int>();

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            _ => { },
            _ => { },
            () => { }));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
            {
                sut.Subscribe(System.Reactive.Observer.Create<int>(
                    _ => { },
                    _ => { },
                    () => { }));
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Calling CancelFromDispose on an already-completed observable is a silent no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CancelFromDispose_WhenNotPending_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        sut.OnCompleted();

        sut.CancelFromDispose();

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    /// <summary>
    /// Disposing the subscription sets IsCancelled to true.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_Subscription_SetsCancelled()
    {
        var sut = new SqliteRowObservable<int>();

        var subscription = sut.Subscribe(System.Reactive.Observer.Create<int>(
            _ => { },
            _ => { },
            () => { }));

        await Assert.That(sut.IsCancelled).IsFalse();

        subscription.Dispose();

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    /// <summary>
    /// Disposing the subscription twice is idempotent and does not throw.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_Subscription_Twice_IsIdempotent()
    {
        var sut = new SqliteRowObservable<int>();

        var subscription = sut.Subscribe(System.Reactive.Observer.Create<int>(
            _ => { },
            _ => { },
            () => { }));

        subscription.Dispose();
        subscription.Dispose();

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    /// <summary>
    /// Calling OnNext after the subscription has been disposed (cancelled state) is a
    /// silent no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnNext_AfterCancelled_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        var values = new List<int>();

        var subscription = sut.Subscribe(System.Reactive.Observer.Create<int>(
            v => values.Add(v),
            _ => { },
            () => { }));

        sut.OnNext(1);
        subscription.Dispose();
        sut.OnNext(2);

        await Assert.That(values.Count).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo(1);
    }

    /// <summary>
    /// OnNext before any subscriber buffers the value and drains it on Subscribe.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnNext_BeforeSubscribe_BuffersAndDrains()
    {
        var sut = new SqliteRowObservable<int>();

        sut.OnNext(10);
        sut.OnNext(20);
        sut.OnNext(30);

        var values = new List<int>();
        var completed = false;

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            v => values.Add(v),
            _ => { },
            () => completed = true));

        sut.OnCompleted();

        await Assert.That(values.Count).IsEqualTo(3);
        await Assert.That(values[0]).IsEqualTo(10);
        await Assert.That(values[1]).IsEqualTo(20);
        await Assert.That(values[2]).IsEqualTo(30);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// TakeBufferSnapshot returns null when no rows were buffered before subscribe.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Subscribe_WithNoBufferedRows_DoesNotDrain()
    {
        var sut = new SqliteRowObservable<int>();

        var values = new List<int>();
        sut.Subscribe(System.Reactive.Observer.Create<int>(
            v => values.Add(v),
            _ => { },
            () => { }));

        sut.OnNext(42);
        sut.OnCompleted();

        await Assert.That(values.Count).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo(42);
    }

    /// <summary>
    /// OnError after errored state is a no-op (double OnError suppressed).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnError_AfterErrored_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        var errorCount = 0;

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            _ => { },
            _ => errorCount++,
            () => { }));

        sut.OnError(new InvalidOperationException("first"));
        sut.OnError(new InvalidOperationException("second"));

        await Assert.That(errorCount).IsEqualTo(1);
    }

    /// <summary>
    /// CancelFromDispose while in pending state with no subscriber sets
    /// IsCancelled to true and subsequent OnNext is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CancelFromDispose_WhilePending_SetsCancelledAndDropsOnNext()
    {
        var sut = new SqliteRowObservable<int>();
        var values = new List<int>();

        var subscription = sut.Subscribe(System.Reactive.Observer.Create<int>(
            v => values.Add(v),
            _ => { },
            () => { }));

        sut.OnNext(1);
        subscription.Dispose();

        await Assert.That(sut.IsCancelled).IsTrue();

        sut.OnNext(2);

        await Assert.That(values.Count).IsEqualTo(1);
    }

    /// <summary>
    /// OnNext buffered before subscribe, then OnCompleted before subscribe,
    /// drains buffered items and fires OnCompleted on subscribe.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnNext_ThenOnCompleted_BeforeSubscribe_DrainsThenCompletes()
    {
        var sut = new SqliteRowObservable<int>();

        sut.OnNext(1);
        sut.OnNext(2);
        sut.OnCompleted();

        var values = new List<int>();
        var completed = false;

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            v => values.Add(v),
            _ => { },
            () => completed = true));

        await Assert.That(values.Count).IsEqualTo(2);
        await Assert.That(values[0]).IsEqualTo(1);
        await Assert.That(values[1]).IsEqualTo(2);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// OnCompleted without any subscriber and without any buffered rows does not throw.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnCompleted_WithoutSubscriber_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        sut.OnCompleted();

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    /// <summary>
    /// OnError without any subscriber does not throw and transitions to errored state.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnError_WithoutSubscriber_TransitionsToErrored()
    {
        var sut = new SqliteRowObservable<int>();
        sut.OnError(new InvalidOperationException("no-subscriber"));

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    /// <summary>
    /// Subscribe works correctly when there is nothing to drain (no pre-subscribe OnNext calls).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Subscribe_NeverBuffered_DrainBufferReceivesNull()
    {
        var sut = new SqliteRowObservable<int>();
        var values = new List<int>();
        var completed = false;

        sut.Subscribe(System.Reactive.Observer.Create<int>(
            v => values.Add(v),
            _ => { },
            () => completed = true));

        sut.OnNext(99);
        sut.OnCompleted();

        await Assert.That(values).Count().IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo(99);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// OnCompleted from cancelled state is allowed (worker calls OnCompleted after
    /// noticing IsCancelled). The transition from Cancelled to Completed is valid.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnCompleted_AfterCancelled_TransitionsCleanly()
    {
        var sut = new SqliteRowObservable<int>();
        var values = new List<int>();

        var subscription = sut.Subscribe(System.Reactive.Observer.Create<int>(
            v => values.Add(v),
            _ => { },
            () => { }));

        subscription.Dispose();
        sut.OnCompleted();

        await Assert.That(sut.IsCancelled).IsTrue();
    }
}
