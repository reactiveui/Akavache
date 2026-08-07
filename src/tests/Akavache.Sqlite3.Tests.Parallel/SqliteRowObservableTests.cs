// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for <see cref="SqliteRowObservable{T}"/> covering state transitions, cancellation, idempotent terminal calls, and single-subscriber enforcement.</summary>
[Category("Akavache")]
public class SqliteRowObservableTests
{
    /// <summary>Calling OnNext after OnCompleted is a silent no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNext_AfterCompleted_IsNoop()
    {
        const int DeliveredRow = 1;
        const int DroppedRow = 2;
        var sut = new SqliteRowObservable<int>();
        var values = new List<int>();
        var completed = false;

        _ = sut.Subscribe(Witness.Create<int>(
            values.Add,
            static _ => { },
            () => completed = true));

        sut.OnNext(DeliveredRow);
        sut.OnCompleted();
        sut.OnNext(DroppedRow);

        await Assert.That(values.Count).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo(DeliveredRow);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>Calling OnCompleted twice delivers OnCompleted to the observer only once.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnCompleted_Twice_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        var completedCount = 0;

        _ = sut.Subscribe(Witness.Create<int>(
            static _ => { },
            static _ => { },
            () => completedCount++));

        sut.OnCompleted();
        sut.OnCompleted();

        await Assert.That(completedCount).IsEqualTo(1);
    }

    /// <summary>OnError forwards the exception to the subscribed observer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnError_ForwardsToObserver()
    {
        var sut = new SqliteRowObservable<int>();
        Exception? caught = null;

        _ = sut.Subscribe(Witness.Create<int>(
            static _ => { },
            ex => caught = ex,
            static () => { }));

        var expected = new InvalidOperationException("test-error");
        sut.OnError(expected);

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    /// <summary>Calling OnError after OnCompleted is a silent no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnError_AfterCompleted_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        var completed = false;
        Exception? caught = null;

        _ = sut.Subscribe(Witness.Create<int>(
            static _ => { },
            ex => caught = ex,
            () => completed = true));

        sut.OnCompleted();
        sut.OnError(new InvalidOperationException("late-error"));

        await Assert.That(completed).IsTrue();
        await Assert.That(caught).IsNull();
    }

    /// <summary>Subscribing to an observable that has already completed fires OnCompleted immediately without any OnNext emissions.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Subscribe_WhenAlreadyCompleted_FiresOnCompletedImmediately()
    {
        var sut = new SqliteRowObservable<int>();
        sut.OnCompleted();

        var completed = false;
        var values = new List<int>();

        _ = sut.Subscribe(Witness.Create<int>(
            values.Add,
            static _ => { },
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
    public async Task Subscribe_WhenAlreadyErrored_FiresOnCompletedImmediately()
    {
        var sut = new SqliteRowObservable<int>();
        sut.OnError(new InvalidOperationException("early-error"));

        var completed = false;
        Exception? caught = null;

        _ = sut.Subscribe(Witness.Create<int>(
            static _ => { },
            ex => caught = ex,
            () => completed = true));

        await Assert.That(completed).IsTrue();
        await Assert.That(caught).IsNull();
    }

    /// <summary>A second call to Subscribe throws <see cref="InvalidOperationException"/>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Subscribe_Twice_Throws()
    {
        var sut = new SqliteRowObservable<int>();

        _ = sut.Subscribe(Witness.Create<int>(
            static _ => { },
            static _ => { },
            static () => { }));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
            {
                _ = sut.Subscribe(Witness.Create<int>(
                    static _ => { },
                    static _ => { },
                    static () => { }));
                return Task.CompletedTask;
            });
    }

    /// <summary>Calling CancelFromDispose on an already-completed observable is a silent no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CancelFromDispose_WhenNotPending_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        sut.OnCompleted();

        // Should not throw.
        sut.CancelFromDispose();

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    /// <summary>Disposing the subscription sets IsCancelled to true.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Dispose_Subscription_SetsCancelled()
    {
        var sut = new SqliteRowObservable<int>();

        var subscription = sut.Subscribe(Witness.Create<int>(
            static _ => { },
            static _ => { },
            static () => { }));

        await Assert.That(sut.IsCancelled).IsFalse();

        subscription.Dispose();

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    /// <summary>Disposing the subscription twice is idempotent and does not throw.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Dispose_Subscription_Twice_IsIdempotent()
    {
        var sut = new SqliteRowObservable<int>();

        var subscription = sut.Subscribe(Witness.Create<int>(
            static _ => { },
            static _ => { },
            static () => { }));

        subscription.Dispose();
        subscription.Dispose();

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    /// <summary>Calling OnNext after the subscription has been disposed (cancelled state) is a silent no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNext_AfterCancelled_IsNoop()
    {
        const int DeliveredRow = 1;
        const int DroppedRow = 2;
        var sut = new SqliteRowObservable<int>();
        var values = new List<int>();

        var subscription = sut.Subscribe(Witness.Create<int>(
            values.Add,
            static _ => { },
            static () => { }));

        sut.OnNext(DeliveredRow);
        subscription.Dispose();
        sut.OnNext(DroppedRow);

        await Assert.That(values.Count).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo(DeliveredRow);
    }

    /// <summary>
    /// OnNext before any subscriber buffers the value and drains it on Subscribe.
    /// Covers lines 125-128 (buffer creation and add).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNext_BeforeSubscribe_BuffersAndDrains()
    {
        const int FirstBufferedRow = 10;
        const int SecondBufferedRow = 20;
        const int ThirdBufferedRow = 30;
        const int BufferedRowCount = 3;
        var sut = new SqliteRowObservable<int>();

        sut.OnNext(FirstBufferedRow);
        sut.OnNext(SecondBufferedRow);
        sut.OnNext(ThirdBufferedRow);

        var values = new List<int>();
        var completed = false;

        _ = sut.Subscribe(Witness.Create<int>(
            values.Add,
            static _ => { },
            () => completed = true));

        sut.OnCompleted();

        await Assert.That(values.Count).IsEqualTo(BufferedRowCount);
        await Assert.That(values[0]).IsEqualTo(FirstBufferedRow);
        await Assert.That(values[1]).IsEqualTo(SecondBufferedRow);
        await Assert.That(values[2]).IsEqualTo(ThirdBufferedRow);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// TakeBufferSnapshot returns null when no rows were buffered before subscribe.
    /// Covers lines 280 (buffer is not Count > 0) returning null, and
    /// DrainBuffer with null buffered (lines 260-262).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Subscribe_WithNoBufferedRows_DoesNotDrain()
    {
        const int DeliveredRow = 42;
        var sut = new SqliteRowObservable<int>();

        var values = new List<int>();
        _ = sut.Subscribe(Witness.Create<int>(
            values.Add,
            static _ => { },
            static () => { }));

        sut.OnNext(DeliveredRow);
        sut.OnCompleted();

        await Assert.That(values.Count).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo(DeliveredRow);
    }

    /// <summary>OnError after errored state is a no-op (double OnError suppressed).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnError_AfterErrored_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        var errorCount = 0;

        _ = sut.Subscribe(Witness.Create<int>(
            static _ => { },
            _ => errorCount++,
            static () => { }));

        sut.OnError(new InvalidOperationException("first"));
        sut.OnError(new InvalidOperationException("second"));

        await Assert.That(errorCount).IsEqualTo(1);
    }

    /// <summary>CancelFromDispose while in pending state with no subscriber sets IsCancelled to true and subsequent OnNext is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CancelFromDispose_WhilePending_SetsCancelledAndDropsOnNext()
    {
        const int DeliveredRow = 1;
        const int DroppedRow = 2;
        var sut = new SqliteRowObservable<int>();
        var values = new List<int>();

        var subscription = sut.Subscribe(Witness.Create<int>(
            values.Add,
            static _ => { },
            static () => { }));

        sut.OnNext(DeliveredRow);
        subscription.Dispose();

        await Assert.That(sut.IsCancelled).IsTrue();

        sut.OnNext(DroppedRow);

        await Assert.That(values.Count).IsEqualTo(1);
    }

    /// <summary>
    /// OnNext buffered before subscribe, then OnCompleted before subscribe,
    /// drains buffered items and fires OnCompleted on subscribe.
    /// Covers lines 265-268 (DrainBuffer foreach loop).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNext_ThenOnCompleted_BeforeSubscribe_DrainsThenCompletes()
    {
        const int FirstBufferedRow = 1;
        const int SecondBufferedRow = 2;
        const int BufferedRowCount = 2;
        var sut = new SqliteRowObservable<int>();

        sut.OnNext(FirstBufferedRow);
        sut.OnNext(SecondBufferedRow);
        sut.OnCompleted();

        var values = new List<int>();
        var completed = false;

        _ = sut.Subscribe(Witness.Create<int>(
            values.Add,
            static _ => { },
            () => completed = true));

        await Assert.That(values.Count).IsEqualTo(BufferedRowCount);
        await Assert.That(values[0]).IsEqualTo(FirstBufferedRow);
        await Assert.That(values[1]).IsEqualTo(SecondBufferedRow);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// OnCompleted without any subscriber and without any buffered rows does not throw.
    /// Covers the OnCompleted path where _observer is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnCompleted_WithoutSubscriber_IsNoop()
    {
        var sut = new SqliteRowObservable<int>();
        sut.OnCompleted();

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    /// <summary>OnError without any subscriber does not throw and transitions to errored state.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnError_WithoutSubscriber_TransitionsToErrored()
    {
        var sut = new SqliteRowObservable<int>();
        sut.OnError(new InvalidOperationException("no-subscriber"));

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    /// <summary>
    /// DrainBuffer with null buffer (lines 285-287) is exercised when TakeBufferSnapshot
    /// returns null because buffer was never allocated (no pre-subscribe OnNext calls).
    /// This test ensures Subscribe works correctly when there is nothing to drain.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Subscribe_NeverBuffered_DrainBufferReceivesNull()
    {
        const int DeliveredRow = 99;
        var sut = new SqliteRowObservable<int>();
        var values = new List<int>();
        var completed = false;

        _ = sut.Subscribe(Witness.Create<int>(
            values.Add,
            static _ => { },
            () => completed = true));

        sut.OnNext(DeliveredRow);
        sut.OnCompleted();

        await Assert.That(values).Count().IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo(DeliveredRow);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// OnCompleted from cancelled state is allowed (worker calls OnCompleted after
    /// noticing IsCancelled). The transition from Cancelled to Completed is valid.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnCompleted_AfterCancelled_TransitionsCleanly()
    {
        var sut = new SqliteRowObservable<int>();
        var values = new List<int>();

        var subscription = sut.Subscribe(Witness.Create<int>(
            values.Add,
            static _ => { },
            static () => { }));

        subscription.Dispose();
        sut.OnCompleted();

        await Assert.That(sut.IsCancelled).IsTrue();
    }

    // ── CaptureGapAndSetObserver static helper ──────────────────────────
    /// <summary>CaptureGapAndSetObserver with pending state, no buffer — sets observer, returns no gap.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CaptureGapAndSetObserver_Pending_NoBuffer_SetsObserver()
    {
        var state = 0; // StatePending
        List<int>? buffer = null;
        IObserver<int>? observerSlot = null;
        var observer = Witness.Create<int>(static _ => { });

        var (terminal, gap) = SqliteRowObservable<int>.CaptureGapAndSetObserver(
            ref state,
            ref buffer,
            ref observerSlot,
            observer);

        await Assert.That(terminal).IsFalse();
        await Assert.That(gap).IsNull();
        await Assert.That(observerSlot).IsSameReferenceAs(observer);
    }

    /// <summary>CaptureGapAndSetObserver with pending state and buffered rows — sets observer, returns gap.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CaptureGapAndSetObserver_Pending_WithBuffer_ReturnsGap()
    {
        const int BufferedRowCount = 3;
        var state = 0; // StatePending
        List<int>? buffer = [10, 20, 30];
        IObserver<int>? observerSlot = null;
        var observer = Witness.Create<int>(static _ => { });

        var (terminal, gap) = SqliteRowObservable<int>.CaptureGapAndSetObserver(
            ref state,
            ref buffer,
            ref observerSlot,
            observer);

        await Assert.That(terminal).IsFalse();
        await Assert.That(gap).IsNotNull();
        await Assert.That(gap!.Length).IsEqualTo(BufferedRowCount);
        await Assert.That(buffer).IsNull();
        await Assert.That(observerSlot).IsSameReferenceAs(observer);
    }

    /// <summary>CaptureGapAndSetObserver with pending state and empty buffer — sets observer, returns null gap.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CaptureGapAndSetObserver_Pending_EmptyBuffer_ReturnsNullGap()
    {
        var state = 0; // StatePending
        List<int>? buffer = [];
        IObserver<int>? observerSlot = null;
        var observer = Witness.Create<int>(static _ => { });

        var (terminal, gap) = SqliteRowObservable<int>.CaptureGapAndSetObserver(
            ref state,
            ref buffer,
            ref observerSlot,
            observer);

        await Assert.That(terminal).IsFalse();
        await Assert.That(gap).IsNull();
        await Assert.That(buffer).IsNull();
    }

    /// <summary>CaptureGapAndSetObserver with terminal state — does not set observer.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CaptureGapAndSetObserver_Terminal_DoesNotSetObserver()
    {
        var state = 1; // StateCompleted
        List<int>? buffer = [99];
        IObserver<int>? observerSlot = null;
        var observer = Witness.Create<int>(static _ => { });

        var (terminal, gap) = SqliteRowObservable<int>.CaptureGapAndSetObserver(
            ref state,
            ref buffer,
            ref observerSlot,
            observer);

        await Assert.That(terminal).IsTrue();
        await Assert.That(gap).IsNotNull();
        await Assert.That(observerSlot).IsNull();
    }
}
