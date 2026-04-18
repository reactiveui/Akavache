// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core.Observables;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="InitSignal"/> covering Complete, Fail, Gate, TryPark, and
/// idempotent / race-condition paths.
/// </summary>
[Category("Akavache")]
public class InitSignalTests
{
    /// <summary>
    /// Complete with no parked callbacks is a clean no-op (snapshot is null path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Complete_NoPendingCallbacks_TransitionsToReady()
    {
        var signal = new InitSignal();
        signal.Complete();

        await Assert.That(signal.IsReady).IsTrue();
        await Assert.That(signal.IsCompleted).IsTrue();
    }

    /// <summary>
    /// Fail with no parked callbacks transitions to failed (snapshot is null path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Fail_NoPendingCallbacks_TransitionsToFailed()
    {
        var signal = new InitSignal();
        signal.Fail(new InvalidOperationException("no-pending"));

        await Assert.That(signal.IsReady).IsFalse();
        await Assert.That(signal.IsCompleted).IsTrue();
    }

    /// <summary>
    /// Complete fires all parked callbacks with null error.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Complete_WithPendingCallbacks_FiresAllWithNullError()
    {
        var signal = new InitSignal();
        Exception?[] received = [new InvalidOperationException("sentinel"), new InvalidOperationException("sentinel")];

        signal.TryPark(err => received[0] = err, out _);
        signal.TryPark(err => received[1] = err, out _);

        signal.Complete();

        await Assert.That(received[0]).IsNull();
        await Assert.That(received[1]).IsNull();
    }

    /// <summary>
    /// Fail fires all parked callbacks with the captured error.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Fail_WithPendingCallbacks_FiresAllWithError()
    {
        var signal = new InitSignal();
        var expected = new InvalidOperationException("fail-all");
        Exception?[] received = new Exception?[2];

        signal.TryPark(err => received[0] = err, out _);
        signal.TryPark(err => received[1] = err, out _);

        signal.Fail(expected);

        await Assert.That(received[0]).IsSameReferenceAs(expected);
        await Assert.That(received[1]).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Double-Complete is idempotent — second call is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Complete_CalledTwice_IsIdempotent()
    {
        var signal = new InitSignal();
        signal.Complete();
        signal.Complete();

        await Assert.That(signal.IsReady).IsTrue();
    }

    /// <summary>
    /// Double-Fail is idempotent — second call is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Fail_CalledTwice_IsIdempotent()
    {
        var signal = new InitSignal();
        var first = new InvalidOperationException("first");
        signal.Fail(first);
        signal.Fail(new InvalidOperationException("second"));

        await Assert.That(signal.IsReady).IsFalse();
        await Assert.That(signal.IsCompleted).IsTrue();

        // The first error is pinned — Gate surfaces it.
        Exception? caught = null;
        signal.Gate(() => Observable.Return(0)).Subscribe(
            _ => { },
            ex => caught = ex,
            () => { });

        await Assert.That(caught).IsSameReferenceAs(first);
    }

    /// <summary>
    /// Fail after Complete is a no-op — signal stays ready.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Fail_AfterComplete_IsNoop()
    {
        var signal = new InitSignal();
        signal.Complete();
        signal.Fail(new InvalidOperationException("late"));

        await Assert.That(signal.IsReady).IsTrue();
    }

    /// <summary>
    /// Complete after Fail is a no-op — signal stays failed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Complete_AfterFail_IsNoop()
    {
        var signal = new InitSignal();
        signal.Fail(new InvalidOperationException("first"));
        signal.Complete();

        await Assert.That(signal.IsReady).IsFalse();
        await Assert.That(signal.IsCompleted).IsTrue();
    }

    /// <summary>
    /// TryPark when pending returns true and adds the callback.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryPark_WhenPending_ReturnsTrueAndParksCallback()
    {
        var signal = new InitSignal();
        var called = false;

        var parked = signal.TryPark(_ => called = true, out var error);

        await Assert.That(parked).IsTrue();
        await Assert.That(error).IsNull();
        await Assert.That(called).IsFalse();

        signal.Complete();
        await Assert.That(called).IsTrue();
    }

    /// <summary>
    /// TryPark when already ready returns false with null error.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryPark_WhenReady_ReturnsFalseWithNullError()
    {
        var signal = new InitSignal();
        signal.Complete();

        var parked = signal.TryPark(_ => { }, out var error);

        await Assert.That(parked).IsFalse();
        await Assert.That(error).IsNull();
    }

    /// <summary>
    /// TryPark when already failed returns false with the captured error.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryPark_WhenFailed_ReturnsFalseWithCapturedError()
    {
        var signal = new InitSignal();
        var expected = new InvalidOperationException("parked-fail");
        signal.Fail(expected);

        var parked = signal.TryPark(_ => { }, out var error);

        await Assert.That(parked).IsFalse();
        await Assert.That(error).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Gate on the ready path returns the factory observable directly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Gate_WhenReady_ReturnsFactoryObservable()
    {
        var signal = new InitSignal();
        signal.Complete();

        var expected = Observable.Return(42);
        var actual = signal.Gate(() => expected);

        await Assert.That(actual).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Gate on the failed path returns a throwing observable with the captured error.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Gate_WhenFailed_ReturnsThrowingObservable()
    {
        var signal = new InitSignal();
        var expected = new InvalidOperationException("gate-fail");
        signal.Fail(expected);

        Exception? caught = null;
        signal.Gate(() => Observable.Return(0)).Subscribe(
            _ => { },
            ex => caught = ex,
            () => { });

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Gate on the pending path returns a <see cref="GatedByInitObservable{T}"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Gate_WhenPending_ReturnsGatedObservable()
    {
        var signal = new InitSignal();

        var gated = signal.Gate(() => Observable.Return(1));

        await Assert.That(gated).IsTypeOf<GatedByInitObservable<int>>();
    }

    /// <summary>
    /// Gate on the pending path parks, then delivers value on Complete.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Gate_WhenPending_DeliversValueAfterComplete()
    {
        var signal = new InitSignal();
        int? received = null;

        signal.Gate(() => Observable.Return(77)).Subscribe(
            v => received = v,
            _ => { },
            () => { });

        await Assert.That(received).IsNull();

        signal.Complete();

        await Assert.That(received).IsEqualTo(77);
    }

    /// <summary>
    /// Gate on the pending path parks, then delivers error on Fail.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Gate_WhenPending_DeliversErrorAfterFail()
    {
        var signal = new InitSignal();
        Exception? caught = null;
        var expected = new InvalidOperationException("pending-fail");

        signal.Gate(() => Observable.Return(0)).Subscribe(
            _ => { },
            ex => caught = ex,
            () => { });

        signal.Fail(expected);

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Gate on the failed path returns Observable.Throw carrying the pinned error, then
    /// Gate on the ready path after that verifies the error is still the first one.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Gate_WhenFailed_ReturnsThrowWithPinnedError()
    {
        var signal = new InitSignal();
        var expected = new InvalidOperationException("gate-fail-pinned");
        signal.Fail(expected);

        Exception? caught = null;
        signal.Gate(() => Observable.Return(0)).Subscribe(
            _ => { },
            ex => caught = ex,
            () => { });

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Fail then Gate exercises the failed fast-path (lines 159-161 in InitSignal.Gate).
    /// Then Complete after Fail is a no-op, so Gate still surfaces the error.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Fail_ThenGate_ThenComplete_StillSurfacesOriginalError()
    {
        var signal = new InitSignal();
        var expected = new InvalidOperationException("first-error");
        signal.Fail(expected);
        signal.Complete();

        Exception? caught = null;
        signal.Gate(() => Observable.Return(0)).Subscribe(
            _ => { },
            ex => caught = ex,
            () => { });

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Fail with parked callbacks fires each callback with the error, exercising
    /// lines 114-135 (the for loop over snapshot in Fail).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Fail_WithMultiplePendingCallbacks_FiresEachWithError()
    {
        var signal = new InitSignal();
        var expected = new InvalidOperationException("multi-fail");
        Exception?[] received = new Exception?[3];

        for (var i = 0; i < 3; i++)
        {
            var idx = i;
            signal.TryPark(err => received[idx] = err, out _);
        }

        signal.Fail(expected);

        await Assert.That(received[0]).IsSameReferenceAs(expected);
        await Assert.That(received[1]).IsSameReferenceAs(expected);
        await Assert.That(received[2]).IsSameReferenceAs(expected);
    }

    /// <summary>
    /// Complete with parked callbacks fires each callback with null error, exercising
    /// lines 83-84, 92-100 (the for loop over snapshot in Complete).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Complete_WithMultiplePendingCallbacks_FiresEachWithNull()
    {
        var signal = new InitSignal();
        var callCount = 0;
        Exception?[] received = [new InvalidOperationException("s"), new InvalidOperationException("s"), new InvalidOperationException("s")];

        for (var i = 0; i < 3; i++)
        {
            var idx = i;
            signal.TryPark(
                err =>
                {
                    received[idx] = err;
                    Interlocked.Increment(ref callCount);
                },
                out _);
        }

        signal.Complete();

        await Assert.That(callCount).IsEqualTo(3);
        await Assert.That(received[0]).IsNull();
        await Assert.That(received[1]).IsNull();
        await Assert.That(received[2]).IsNull();
    }

    /// <summary>
    /// Complete when already completed (not pending) is a no-op (line 83-84 early return).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Complete_WhenAlreadyReady_IsNoopReturnEarly()
    {
        var signal = new InitSignal();
        signal.Complete();

        // Park a callback AFTER Complete — TryPark returns false.
        var parked = signal.TryPark(_ => { }, out _);
        await Assert.That(parked).IsFalse();

        // Second Complete is a no-op — the early return at line 83-84.
        signal.Complete();
        await Assert.That(signal.IsReady).IsTrue();
    }

    /// <summary>
    /// Fail when already completed (line 115-117 early return in Fail) is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Fail_WhenAlreadyReady_IsNoopReturnEarly()
    {
        var signal = new InitSignal();
        signal.Complete();

        signal.Fail(new InvalidOperationException("late-fail"));

        await Assert.That(signal.IsReady).IsTrue();
    }
}
