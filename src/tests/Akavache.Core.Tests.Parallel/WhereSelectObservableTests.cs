// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for <see cref="WhereSelectObservable{TIn, TOut}"/> covering OnError and OnCompleted pass-through paths.</summary>
[Category("Akavache")]
public class WhereSelectObservableTests
{
    /// <summary>OnError from the source observable is passed through to the downstream observer.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnError_FromSource_IsPassedThrough()
    {
        var expected = new InvalidOperationException("source-error");
        var source = Signal.Throw<int>(expected);

        Exception? caught = null;
        var completed = false;

        var observable = new WhereSelectObservable<int, string>(
            source,
            static _ => true,
            static x => x.ToString());

        _ = observable.Subscribe(Witness.Create<string>(
            static _ => { },
            ex => caught = ex,
            () => completed = true));

        await Assert.That(caught).IsSameReferenceAs(expected);
        await Assert.That(completed).IsFalse();
    }

    /// <summary>OnCompleted from the source observable is passed through to the downstream observer.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnCompleted_FromSource_IsPassedThrough()
    {
        var source = Signal.Empty<int>();

        var completed = false;

        var observable = new WhereSelectObservable<int, string>(
            source,
            static _ => true,
            static x => x.ToString());

        _ = observable.Subscribe(Witness.Create<string>(
            static _ => { },
            static _ => { },
            () => completed = true));

        await Assert.That(completed).IsTrue();
    }

    /// <summary>Predicate exception routes to OnError on the downstream observer (lines 59-62).</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnNext_PredicateThrows_RoutesToOnError()
    {
        var source = Signal.Return(1);

        Exception? caught = null;

        var observable = new WhereSelectObservable<int, int>(
            source,
            static _ => throw new InvalidOperationException("pred-boom"),
            static x => x);

        _ = observable.Subscribe(Witness.Create<int>(
            static _ => { },
            ex => caught = ex,
            static () => { }));

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Message).IsEqualTo("pred-boom");
    }

    /// <summary>Selector exception routes to OnError on the downstream observer (lines 70-73).</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnNext_SelectorThrows_RoutesToOnError()
    {
        var source = Signal.Return(1);

        Exception? caught = null;

        var observable = new WhereSelectObservable<int, int>(
            source,
            static _ => true,
            static _ => throw new InvalidOperationException("sel-boom"));

        _ = observable.Subscribe(Witness.Create<int>(
            static _ => { },
            ex => caught = ex,
            static () => { }));

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Message).IsEqualTo("sel-boom");
    }
}
