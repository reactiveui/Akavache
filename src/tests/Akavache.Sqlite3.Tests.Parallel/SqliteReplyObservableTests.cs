// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for <see cref="SqliteReplyObservable{T}"/> covering idempotent set, late subscribe, replay, and single-subscriber enforcement.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class SqliteReplyObservableTests
{
    /// <summary>Calling SetResult a second time after an initial SetResult is a no-op; the subscriber receives only the first value.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetResult_ThenSetResult_SecondCallIsNoop()
    {
        const int FirstResult = 1;
        const int SupersededResult = 2;
        var sut = new SqliteReplyObservable<int>();

        sut.SetResult(FirstResult);
        sut.SetResult(SupersededResult);

        int? received = null;
        var completed = false;
        _ = sut.Subscribe(
            Witness.Create<int>(
                v => received = v,
                static _ => { },
                () => completed = true));

        await Assert.That(received).IsEqualTo(FirstResult);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>Calling SetError then SetResult leaves the observable in the error state; the subscriber receives the error.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetError_ThenSetResult_SecondCallIsNoop()
    {
        const int SupersededResult = 42;
        var sut = new SqliteReplyObservable<int>();
        var expected = new InvalidOperationException("first-error");

        sut.SetError(expected);
        sut.SetResult(SupersededResult);

        Exception? caught = null;
        _ = sut.Subscribe(
            Witness.Create<int>(
                static _ => { },
                ex => caught = ex,
                static () => { }));

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    /// <summary>When SetResult is called before Subscribe, the late subscriber receives OnNext followed by OnCompleted (replay path).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetResult_BeforeSubscribe_ReplaysValue()
    {
        var sut = new SqliteReplyObservable<string>();
        sut.SetResult("hello");

        string? received = null;
        var completed = false;
        _ = sut.Subscribe(
            Witness.Create<string>(
                v => received = v,
                static _ => { },
                () => completed = true));

        await Assert.That(received).IsEqualTo("hello");
        await Assert.That(completed).IsTrue();
    }

    /// <summary>When SetError is called before Subscribe, the late subscriber receives OnError (replay path).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetError_BeforeSubscribe_ReplaysError()
    {
        var sut = new SqliteReplyObservable<int>();
        var expected = new InvalidOperationException("boom");
        sut.SetError(expected);

        Exception? caught = null;
        _ = sut.Subscribe(
            Witness.Create<int>(
                static _ => { },
                ex => caught = ex,
                static () => { }));

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    /// <summary>A second call to Subscribe throws <see cref="InvalidOperationException"/> because the single-subscriber contract is enforced.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Subscribe_Twice_Throws()
    {
        var sut = new SqliteReplyObservable<int>();

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

    /// <summary>The Fail method on <see cref="SqliteOperation{T}"/> routes the error through the reply observable's SetError path.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Fail_CalledFromOperationFail_PropagatesError()
    {
        var reply = new SqliteReplyObservable<int>();
        var op = new SqliteOperation<int>(static _ => 1, reply, coalescable: false);

        Exception? caught = null;
        _ = reply.Subscribe(Witness.Create<int>(
            static _ => { },
            ex => caught = ex,
            static () => { }));

        var expected = new InvalidOperationException("op-fail");
        op.Fail(expected);

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Calling SetResult with no subscriber stashes the value; a later subscriber
    /// receives it via replay. This covers the observer-is-null path in SetResult
    /// (lines 98-101).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetResult_NoSubscriber_StashesForReplay()
    {
        const int StashedResult = 42;
        var sut = new SqliteReplyObservable<int>();

        // Set result before anyone subscribes — observer is null.
        sut.SetResult(StashedResult);

        int? received = null;
        var completed = false;
        _ = sut.Subscribe(Witness.Create<int>(
            v => received = v,
            static _ => { },
            () => completed = true));

        await Assert.That(received).IsEqualTo(StashedResult);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// Calling SetError with no subscriber stashes the error; a later subscriber
    /// receives it via replay. This covers the observer-is-null path in SetError
    /// (line 129: observer?.OnError — null-conditional path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetError_NoSubscriber_StashesForReplay()
    {
        var sut = new SqliteReplyObservable<int>();
        var expected = new InvalidOperationException("no-sub-error");

        // Set error before anyone subscribes — observer is null.
        sut.SetError(expected);

        Exception? caught = null;
        _ = sut.Subscribe(Witness.Create<int>(
            static _ => { },
            ex => caught = ex,
            static () => { }));

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Calling SetError then SetError again is a no-op — the second error is dropped.
    /// Covers lines 117-120 (state != StatePending early return in second SetError).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetError_ThenSetError_SecondCallIsNoop()
    {
        var sut = new SqliteReplyObservable<int>();
        var first = new InvalidOperationException("first");
        var second = new InvalidOperationException("second");

        sut.SetError(first);
        sut.SetError(second);

        Exception? caught = null;
        _ = sut.Subscribe(Witness.Create<int>(
            static _ => { },
            ex => caught = ex,
            static () => { }));

        await Assert.That(caught).IsSameReferenceAs(first);
    }

    /// <summary>
    /// Calling SetResult then SetError leaves the observable in success state;
    /// the subscriber receives the value.
    /// Covers lines 87-89 (state != StatePending early return in SetError after SetResult).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetResult_ThenSetError_SecondCallIsNoop()
    {
        const int SucceededResult = 99;
        var sut = new SqliteReplyObservable<int>();

        sut.SetResult(SucceededResult);
        sut.SetError(new InvalidOperationException("late-error"));

        int? received = null;
        var completed = false;
        _ = sut.Subscribe(Witness.Create<int>(
            v => received = v,
            static _ => { },
            () => completed = true));

        await Assert.That(received).IsEqualTo(SucceededResult);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>Subscribe with a null observer throws ArgumentNullException.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Subscribe_NullObserver_Throws()
    {
        var sut = new SqliteReplyObservable<int>();
        await Assert.That(() => sut.Subscribe(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>ReplayTo with StateSuccess replays value and completes.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReplayTo_StateSuccess_ReplaysValueAndCompletes()
    {
        const int ReplayedValue = 42;
        int? received = null;
        var completed = false;
        var observer = Witness.Create<int>(
            v => received = v,
            static _ => { },
            () => completed = true);

        SqliteReplyObservable<int>.ReplayTo(observer, SqliteReplyObservable<int>.StateSuccess, ReplayedValue, null);

        await Assert.That(received).IsEqualTo(ReplayedValue);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>ReplayTo with StateError replays the error.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReplayTo_StateError_ReplaysError()
    {
        Exception? caught = null;
        var observer = Witness.Create<int>(
            static _ => { },
            ex => caught = ex,
            static () => { });

        var expected = new InvalidOperationException("replay-err");
        SqliteReplyObservable<int>.ReplayTo(observer, SqliteReplyObservable<int>.StateError, default, expected);

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    // ── Extracted static helpers ─────────────────────────────────────────
    /// <summary>TryTransitionToSuccess returns null when state is already terminal.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryTransitionToSuccess_AlreadyTerminal_ReturnsNull()
    {
        const int RejectedValue = 42;
        var state = SqliteReplyObservable<int>.StateSuccess;
        int value = default;
        var observer = Witness.Create<int>(static _ => { });

        var result = SqliteReplyObservable<int>.TryTransitionToSuccess(
            ref state,
            ref value,
            ref observer,
            RejectedValue);

        await Assert.That(result).IsNull();
        await Assert.That(value).IsEqualTo(0);
    }

    /// <summary>TryTransitionToSuccess transitions from pending and returns the observer.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryTransitionToSuccess_FromPending_ReturnsObserver()
    {
        const int TransitionedValue = 42;
        var state = SqliteReplyObservable<int>.StatePending;
        int value = default;
        var observer = Witness.Create<int>(static _ => { });

        var result = SqliteReplyObservable<int>.TryTransitionToSuccess(
            ref state,
            ref value,
            ref observer,
            TransitionedValue);

        await Assert.That(result).IsNotNull();
        await Assert.That(state).IsEqualTo(SqliteReplyObservable<int>.StateSuccess);
        await Assert.That(value).IsEqualTo(TransitionedValue);
        await Assert.That(observer).IsNull();
    }

    /// <summary>TryTransitionToError returns null when state is already terminal.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryTransitionToError_AlreadyTerminal_ReturnsNull()
    {
        var state = SqliteReplyObservable<int>.StateError;
        Exception? error = null;
        var observer = Witness.Create<int>(static _ => { });

        var result = SqliteReplyObservable<int>.TryTransitionToError(
            ref state,
            ref error,
            ref observer,
            new InvalidOperationException("late"));

        await Assert.That(result).IsNull();
        await Assert.That(error).IsNull();
    }

    /// <summary>TryTransitionToError transitions from pending and returns the observer.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryTransitionToError_FromPending_ReturnsObserver()
    {
        var state = SqliteReplyObservable<int>.StatePending;
        Exception? error = null;
        var observer = Witness.Create<int>(static _ => { });
        var expected = new InvalidOperationException("boom");

        var result = SqliteReplyObservable<int>.TryTransitionToError(
            ref state,
            ref error,
            ref observer,
            expected);

        await Assert.That(result).IsNotNull();
        await Assert.That(state).IsEqualTo(SqliteReplyObservable<int>.StateError);
        await Assert.That(error).IsSameReferenceAs(expected);
        await Assert.That(observer).IsNull();
    }

    /// <summary>CaptureAndSubscribe throws when already subscribed.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CaptureAndSubscribe_AlreadySubscribed_Throws()
    {
        var subscribed = true;
        var state = SqliteReplyObservable<int>.StatePending;
        int value = default;
        Exception? error = null;
        IObserver<int>? observerSlot = null;
        var observer = Witness.Create<int>(static _ => { });

        await Assert.That(() => SqliteReplyObservable<int>.CaptureAndSubscribe(
            ref subscribed,
            ref state,
            ref value,
            ref error,
            ref observerSlot,
            observer))
            .Throws<InvalidOperationException>();
    }

    /// <summary>CaptureAndSubscribe captures pending state and sets the observer.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CaptureAndSubscribe_Pending_SetsObserver()
    {
        var subscribed = false;
        var state = SqliteReplyObservable<int>.StatePending;
        int value = default;
        Exception? error = null;
        IObserver<int>? observerSlot = null;
        var observer = Witness.Create<int>(static _ => { });

        var (s, v, e) = SqliteReplyObservable<int>.CaptureAndSubscribe(
            ref subscribed,
            ref state,
            ref value,
            ref error,
            ref observerSlot,
            observer);

        await Assert.That(s).IsEqualTo(SqliteReplyObservable<int>.StatePending);
        await Assert.That(subscribed).IsTrue();
        await Assert.That(observerSlot).IsSameReferenceAs(observer);
    }

    /// <summary>CaptureAndSubscribe captures terminal state without setting observer.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CaptureAndSubscribe_Terminal_DoesNotSetObserver()
    {
        const int TerminalValue = 99;
        var subscribed = false;
        var state = SqliteReplyObservable<int>.StateSuccess;
        var value = TerminalValue;
        Exception? error = null;
        IObserver<int>? observerSlot = null;
        var observer = Witness.Create<int>(static _ => { });

        var (s, v, e) = SqliteReplyObservable<int>.CaptureAndSubscribe(
            ref subscribed,
            ref state,
            ref value,
            ref error,
            ref observerSlot,
            observer);

        await Assert.That(s).IsEqualTo(SqliteReplyObservable<int>.StateSuccess);
        await Assert.That(v).IsEqualTo(TerminalValue);
        await Assert.That(observerSlot).IsNull();
    }

    /// <summary>DeliverResult with null observer is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DeliverResult_NullObserver_IsNoop()
    {
        const int DroppedValue = 42;
        SqliteReplyObservable<int>.DeliverResult(null, DroppedValue);
        await Task.CompletedTask;
    }

    /// <summary>DeliverResult with non-null observer delivers OnNext and OnCompleted.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DeliverResult_WithObserver_DeliversValue()
    {
        const int DeliveredValue = 77;
        int? received = null;
        var completed = false;
        var observer = Witness.Create<int>(
            v => received = v,
            static _ => { },
            () => completed = true);

        SqliteReplyObservable<int>.DeliverResult(observer, DeliveredValue);

        await Assert.That(received).IsEqualTo(DeliveredValue);
        await Assert.That(completed).IsTrue();
    }
}
