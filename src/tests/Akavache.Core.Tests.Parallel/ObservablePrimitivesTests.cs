// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for <see cref="InitSignal"/> and <see cref="ReactiveExtensions"/>.</summary>
[Category("Akavache")]
public class ObservablePrimitivesTests
{
    /// <summary>Value the gate factory emits on the ready fast path.</summary>
    private const int FactoryValue = 42;

    /// <summary>Value the gate factory emits once a parked subscription is released.</summary>
    private const int ParkedValue = 99;

    /// <summary>Divisor of the <c>WhereSelect</c> predicate, which keeps only even elements.</summary>
    private const int EvenPredicateDivisor = 2;

    /// <summary>Factor the <c>WhereSelect</c> selector applies to each element it keeps.</summary>
    private const int ProjectionMultiplier = 10;

    /// <summary>Fallback <c>CatchReturn</c> emits when the source faults.</summary>
    private const int ErrorFallbackValue = 42;

    /// <summary>Fallback handed to <c>CatchReturn</c> over a successful source, which must never be emitted.</summary>
    private const int UnusedFallbackValue = 99;

    /// <summary>Fallback <c>CatchReturn</c> emits for the string-typed source.</summary>
    private const string StringFallbackValue = "fallback";

    /// <summary>Fresh <see cref="InitSignal"/> starts in the pending state — not ready, not completed.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitSignal_Pending_ShouldNotBeReadyOrCompleted()
    {
        InitSignal signal = new();
        await Assert.That(signal.IsReady).IsFalse();
        await Assert.That(signal.IsCompleted).IsFalse();
    }

    /// <summary>Verifies that <see cref="InitSignal.Complete"/> transitions the signal into the ready state and the second call is idempotent.</summary>
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
    /// Verifies that <see cref="InitSignal.Fail"/> transitions to completed-but-not-ready and pins
    /// the captured error, and that a subsequent <see cref="InitSignal.Complete"/> is ignored.
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
            async () => await signal.Gate(static () => Signal.Return(1)));
        await Assert.That(ex!.Message).IsEqualTo("boom");
    }

    /// <summary>Verifies that <see cref="InitSignal.Gate{T}(Func{IObservable{T}})"/> on the ready path returns the factory's observable directly (no wrapper type).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitSignal_GateOnReadyPath_ShouldReturnFactoryObservableDirectly()
    {
        InitSignal signal = new();
        signal.Complete();

        var expected = Signal.Return(FactoryValue);
        var actual = signal.Gate(() => expected);

        // Fast-path: the returned observable is the factory's observable, not a wrapper.
        await Assert.That(actual).IsSameReferenceAs(expected);
        await Assert.That(await actual).IsEqualTo(FactoryValue);
    }

    /// <summary>Verifies that <see cref="InitSignal.Gate{T}(Func{IObservable{T}})"/> on the pending path parks the subscription until <see cref="InitSignal.Complete"/> fires.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitSignal_GateOnPendingPath_ShouldParkUntilComplete()
    {
        InitSignal signal = new();
        var emitted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        var gated = signal.Gate(static () => Signal.Return(ParkedValue));
        _ = gated.Subscribe(v => emitted.TrySetResult(v));

        // Not completed yet — should not have emitted.
        await Assert.That(emitted.Task.IsCompleted).IsFalse();

        signal.Complete();
        var value = await emitted.Task;
        await Assert.That(value).IsEqualTo(ParkedValue);
    }

    /// <summary>Verifies that <see cref="InitSignal.Gate{T}(Func{IObservable{T}})"/> on the pending path propagates a subsequent <see cref="InitSignal.Fail"/> to the parked subscription.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitSignal_GateOnPendingPath_ShouldPropagateFailToParkedSubscription()
    {
        InitSignal signal = new();
        var captured = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

        var gated = signal.Gate(static () => Signal.Return(0));
        _ = gated.Subscribe(
            _ => captured.TrySetException(new InvalidOperationException("should not emit")),
            ex => captured.TrySetResult(ex));

        signal.Fail(new InvalidOperationException("gated-error"));

        var error = await captured.Task;
        await Assert.That(error.Message).IsEqualTo("gated-error");
    }

    /// <summary>
    /// Verifies that <see cref="ReactiveExtensions.WhereSelect{T, TOut}(IObservable{T}, Func{T, bool}, Func{T, TOut})"/>
    /// forwards only elements that pass the predicate, projected through the selector.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WhereSelect_ShouldFilterAndProjectInOnePass()
    {
        int[] input = [1, 2, 3, 4, 5];
        int[] expected = [20, 40];
        var source = input.ToObservable();

        var result = await source
            .WhereSelect(static x => x % EvenPredicateDivisor == 0, static x => x * ProjectionMultiplier)
            .ToList();

        await Assert.That(result).IsEquivalentTo(expected);
    }

    /// <summary>Verifies that <see cref="ReactiveExtensions.CatchReturnUnit(IObservable{RxVoid})"/> forwards terminal errors as a single <see cref="RxVoid.Default"/> + OnCompleted.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CatchReturnUnit_ShouldSwallowErrorAndEmitUnit()
    {
        var source = Signal.Throw<RxVoid>(new InvalidOperationException("boom"));

        var result = await source.CatchReturnUnit().ToList();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo(RxVoid.Default);
    }

    /// <summary>
    /// Verifies that <see cref="ReactiveExtensions.CatchReturn{T}(IObservable{T}, T)"/> forwards the stored
    /// fallback when the source errors, and forwards source values verbatim otherwise.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CatchReturn_ShouldForwardValuesAndFallbackOnError()
    {
        string[] values = ["a", "b"];
        string[] expectedSuccess = ["a", "b"];
        string[] expectedFailure = [StringFallbackValue];
        var successful = values.ToObservable();
        var failed = Signal.Throw<string>(new InvalidOperationException("boom"));

        var successResult = await successful.CatchReturn(StringFallbackValue).ToList();
        var failureResult = await failed.CatchReturn(StringFallbackValue).ToList();

        await Assert.That(successResult).IsEquivalentTo(expectedSuccess);
        await Assert.That(failureResult).IsEquivalentTo(expectedFailure);
    }

    /// <summary>
    /// Verifies that <see cref="ReactiveExtensions.WhereSelect{T, TOut}(IObservable{T}, Func{T, bool}, Func{T, TOut})"/>
    /// routes a throwing predicate to <see cref="IObserver{T}.OnError"/> on the downstream observer.
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
    /// Verifies that <see cref="ReactiveExtensions.WhereSelect{T, TOut}(IObservable{T}, Func{T, bool}, Func{T, TOut})"/>
    /// routes a throwing selector to <see cref="IObserver{T}.OnError"/> on the downstream observer.
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

    /// <summary>Verifies that <see cref="ReactiveExtensions.CatchReturn{T}(IObservable{T}, T)"/> forwards the fallback on error for a non-RxVoid type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CatchReturn_WithIntFallback_ShouldEmitFallbackOnError()
    {
        var source = Signal.Throw<int>(new InvalidOperationException("err"));

        var result = await source.CatchReturn(ErrorFallbackValue).ToList();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo(ErrorFallbackValue);
    }

    /// <summary>
    /// Verifies that <see cref="ReactiveExtensions.SelectManyThen{T, TMid, TResult}(IObservable{T}, Func{T, IObservable{TMid}}, Func{TMid, IObservable{TResult}})"/>
    /// pipes the source element through the first projection and then feeds that intermediate value
    /// into the second projection, emitting only the final result.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SelectManyThen_ShouldFeedFirstProjectionResultIntoSecond()
    {
        var source = Signal.Return("seed");

        var result = await source
            .SelectManyThen(
                static x => Signal.Return($"{x}-first"),
                static mid => Signal.Return($"{mid}-second"))
            .ToList();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo("seed-first-second");
    }

    /// <summary>Verifies that <see cref="ReactiveExtensions.CatchReturn{T}(IObservable{T}, T)"/> with a successful source forwards values verbatim without emitting the fallback.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CatchReturn_WithSuccessfulSource_ForwardsValuesOnly()
    {
        int[] expected = [1, 2, 3];
        var source = expected.ToObservable();

        var result = await source.CatchReturn(UnusedFallbackValue).ToList();

        await Assert.That(result).IsEquivalentTo(expected);
    }
}
