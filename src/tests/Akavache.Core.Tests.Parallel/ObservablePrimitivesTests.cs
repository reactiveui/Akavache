// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core.Observables;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="InitSignal"/> and <see cref="ObservableFastOps"/>.
/// </summary>
[Category("Akavache")]
public class ObservablePrimitivesTests
{
    /// <summary>
    /// Fresh <see cref="InitSignal"/> starts in the pending state — not ready, not completed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitSignal_Pending_ShouldNotBeReadyOrCompleted()
    {
        InitSignal signal = new();
        await Assert.That(signal.IsReady).IsFalse();
        await Assert.That(signal.IsCompleted).IsFalse();
    }

    /// <summary>
    /// <see cref="InitSignal.Complete"/> transitions the signal into the ready state
    /// and the second call is idempotent.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitSignal_Complete_ShouldTransitionToReadyAndBeIdempotent()
    {
        InitSignal signal = new();

        signal.Complete();

        await Assert.That(signal.IsReady).IsTrue();
        await Assert.That(signal.IsCompleted).IsTrue();

        // Second call is a no-op.
        signal.Complete();
        await Assert.That(signal.IsReady).IsTrue();
    }

    /// <summary>
    /// <see cref="InitSignal.Fail"/> transitions to completed-but-not-ready and pins
    /// the captured error. Subsequent <see cref="InitSignal.Complete"/> is ignored.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitSignal_Fail_ShouldPinErrorAndIgnoreSubsequentComplete()
    {
        InitSignal signal = new();
        var boom = new InvalidOperationException("boom");

        signal.Fail(boom);

        await Assert.That(signal.IsReady).IsFalse();
        await Assert.That(signal.IsCompleted).IsTrue();

        signal.Complete();
        await Assert.That(signal.IsReady).IsFalse();

        // Gate<T> fast-paths the failed state to Observable.Throw with the captured error.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await signal.Gate(static () => Observable.Return(1)));
        await Assert.That(ex!.Message).IsEqualTo("boom");
    }

    /// <summary>
    /// <see cref="InitSignal.Gate{T}(Func{IObservable{T}})"/> on the ready path
    /// returns the factory's observable directly (no wrapper type).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitSignal_GateOnReadyPath_ShouldReturnFactoryObservableDirectly()
    {
        InitSignal signal = new();
        signal.Complete();

        var expected = Observable.Return(42);
        var actual = signal.Gate(() => expected);

        // Fast-path: the returned observable is the factory's observable, not a wrapper.
        await Assert.That(actual).IsSameReferenceAs(expected);
        await Assert.That(await actual).IsEqualTo(42);
    }

    /// <summary>
    /// <see cref="InitSignal.Gate{T}(Func{IObservable{T}})"/> on the pending path
    /// parks the subscription until <see cref="InitSignal.Complete"/> fires.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitSignal_GateOnPendingPath_ShouldParkUntilComplete()
    {
        InitSignal signal = new();
        var emitted = new TaskCompletionSource<int>();

        var gated = signal.Gate(() => Observable.Return(99));
        gated.Subscribe(v => emitted.TrySetResult(v));

        // Not completed yet — should not have emitted.
        await Assert.That(emitted.Task.IsCompleted).IsFalse();

        signal.Complete();
        var value = await emitted.Task;
        await Assert.That(value).IsEqualTo(99);
    }

    /// <summary>
    /// <see cref="InitSignal.Gate{T}(Func{IObservable{T}})"/> on the pending path
    /// with a subsequent <see cref="InitSignal.Fail"/> propagates the error to the
    /// parked subscription.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitSignal_GateOnPendingPath_ShouldPropagateFailToParkedSubscription()
    {
        InitSignal signal = new();
        var captured = new TaskCompletionSource<Exception>();

        var gated = signal.Gate(static () => Observable.Return(0));
        gated.Subscribe(
            _ => captured.TrySetException(new InvalidOperationException("should not emit")),
            ex => captured.TrySetResult(ex));

        signal.Fail(new InvalidOperationException("gated-error"));

        var error = await captured.Task;
        await Assert.That(error.Message).IsEqualTo("gated-error");
    }

    /// <summary>
    /// <see cref="ObservableFastOps.WhereSelect{TIn, TOut}"/> forwards only elements
    /// that pass the predicate, projected through the selector.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WhereSelect_ShouldFilterAndProjectInOnePass()
    {
        int[] input = [1, 2, 3, 4, 5];
        int[] expected = [20, 40];
        var source = input.ToObservable();

        var result = await source
            .WhereSelect(static x => x % 2 == 0, static x => x * 10)
            .ToList();

        await Assert.That(result).IsEquivalentTo(expected);
    }

    /// <summary>
    /// <see cref="ObservableFastOps.CatchReturnUnit"/> forwards terminal errors as
    /// a single <see cref="Unit.Default"/> + OnCompleted.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CatchReturnUnit_ShouldSwallowErrorAndEmitUnit()
    {
        var source = Observable.Throw<Unit>(new InvalidOperationException("boom"));

        var result = await source.CatchReturnUnit().ToList();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo(Unit.Default);
    }

    /// <summary>
    /// <see cref="ObservableFastOps.CatchReturn{T}"/> forwards the stored fallback
    /// when the source errors, and forwards source values verbatim otherwise.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CatchReturn_ShouldForwardValuesAndFallbackOnError()
    {
        string[] values = ["a", "b"];
        string[] expectedSuccess = ["a", "b"];
        string[] expectedFailure = ["fallback"];
        var successful = values.ToObservable();
        var failed = Observable.Throw<string>(new InvalidOperationException("boom"));

        var successResult = await successful.CatchReturn("fallback").ToList();
        var failureResult = await failed.CatchReturn("fallback").ToList();

        await Assert.That(successResult).IsEquivalentTo(expectedSuccess);
        await Assert.That(failureResult).IsEquivalentTo(expectedFailure);
    }

    /// <summary>
    /// <see cref="WhereSelectObservable{TIn, TOut}"/> routes a throwing predicate
    /// to <see cref="IObserver{T}.OnError"/> on the downstream observer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WhereSelect_ThrowingPredicate_ShouldRouteErrorDownstream()
    {
        int[] input = [1];
        var source = input.ToObservable();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await source
                .WhereSelect(
                    static _ => throw new InvalidOperationException("predicate-boom"),
                    static x => x)
                .ToList());

        await Assert.That(ex!.Message).IsEqualTo("predicate-boom");
    }

    /// <summary>
    /// <see cref="WhereSelectObservable{TIn, TOut}"/> routes a throwing selector
    /// to <see cref="IObserver{T}.OnError"/> on the downstream observer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WhereSelect_ThrowingSelector_ShouldRouteErrorDownstream()
    {
        int[] input = [1];
        var source = input.ToObservable();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await source
                .WhereSelect<int, int>(
                    static _ => true,
                    static _ => throw new InvalidOperationException("selector-boom"))
                .ToList());

        await Assert.That(ex!.Message).IsEqualTo("selector-boom");
    }

    /// <summary>
    /// <see cref="ObservableFastOps.CatchReturn{T}"/> with a non-Unit type creates a
    /// <see cref="CatchReturnObservable{T}"/> that forwards the fallback on error.
    /// Exercises line 45 of ObservableFastOps.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CatchReturn_WithIntFallback_ShouldEmitFallbackOnError()
    {
        var source = Observable.Throw<int>(new InvalidOperationException("err"));

        var result = await source.CatchReturn(42).ToList();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo(42);
    }

    /// <summary>
    /// <see cref="ObservableFastOps.CatchReturn{T}"/> with a successful source forwards
    /// values verbatim without emitting the fallback.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CatchReturn_WithSuccessfulSource_ForwardsValuesOnly()
    {
        var source = Observable.Return(1).Concat(Observable.Return(2)).Concat(Observable.Return(3));

        var result = await source.CatchReturn(99).ToList();

        int[] expected = [1, 2, 3];
        await Assert.That(result).IsEquivalentTo(expected);
    }
}
