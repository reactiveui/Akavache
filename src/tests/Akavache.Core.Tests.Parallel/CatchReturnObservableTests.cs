// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for <see cref="CatchReturnObservable{T}"/> covering success forwarding, error swallowing with fallback, empty sources, and multi-value streams.</summary>
[Category("Akavache")]
public class CatchReturnObservableTests
{
    /// <summary>The value a successful source emits, which must reach the subscriber unchanged.</summary>
    private const int SuccessValue = 42;

    /// <summary>The value a subject emits before it faults, which must still reach the subscriber.</summary>
    private const int ValueBeforeError = 10;

    /// <summary>Success source — value is forwarded and sequence completes normally.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Success_ForwardsValue()
    {
        var sut = new CatchReturnObservable<int>(Signal.Return(SuccessValue), -1);

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo(SuccessValue);
    }

    /// <summary>Error source — fallback is emitted instead.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Error_EmitsFallback()
    {
        var sut = new CatchReturnObservable<string>(
            Signal.Throw<string>(new InvalidOperationException("boom")),
            "recovered");

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo("recovered");
    }

    /// <summary>Empty source (OnCompleted with no OnNext) — completes without emitting.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Empty_CompletesWithNoValue()
    {
        var sut = new CatchReturnObservable<int>(Signal.Empty<int>(), -1);

        var result = await sut.ToList();
        await Assert.That(result).IsEmpty();
    }

    /// <summary>Multi-value source — all values forwarded then completes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MultiValue_ForwardsAllValues()
    {
        int[] expectedValues = [1, 2, 3];
        var source = expectedValues.ToObservable();
        var sut = new CatchReturnObservable<int>(source, -1);

        var result = await sut.ToList();
        await Assert.That(result).IsEquivalentTo(expectedValues);
    }

    /// <summary>Async error (via Subject) — values before the error are forwarded, then fallback is emitted on error.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AsyncError_ForwardsValuesThenFallback()
    {
        using var subject = new Signal<int>();

        var results = new List<int>();
        var completed = false;
        var sut = new CatchReturnObservable<int>(subject, -1);
        _ = sut.Subscribe(
            results.Add,
            static _ => { },
            () => completed = true);

        subject.OnNext(ValueBeforeError);
        subject.OnError(new InvalidOperationException("async error"));

        int[] expectedValueThenFallback = [ValueBeforeError, -1];
        await Assert.That(results).IsEquivalentTo(expectedValueThenFallback);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>Async success (via Subject) — values forwarded then completes normally.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AsyncSuccess_ForwardsValues()
    {
        using var subject = new Signal<string>();

        var results = new List<string>();
        var completed = false;
        var sut = new CatchReturnObservable<string>(subject, "fallback");
        _ = sut.Subscribe(
            results.Add,
            static _ => { },
            () => completed = true);

        subject.OnNext("a");
        subject.OnNext("b");
        subject.OnCompleted();

        await Assert.That(results).IsEquivalentTo(["a", "b"]);
        await Assert.That(completed).IsTrue();
    }
}
