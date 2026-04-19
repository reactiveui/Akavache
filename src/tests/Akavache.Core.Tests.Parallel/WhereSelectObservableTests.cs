// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core.Observables;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="WhereSelectObservable{TIn, TOut}"/> covering OnError and
/// OnCompleted pass-through paths.
/// </summary>
[Category("Akavache")]
public class WhereSelectObservableTests
{
    /// <summary>
    /// OnError from the source observable is passed through to the downstream observer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnError_FromSource_IsPassedThrough()
    {
        var expected = new InvalidOperationException("source-error");
        var source = Observable.Throw<int>(expected);

        Exception? caught = null;
        var completed = false;

        var observable = new WhereSelectObservable<int, string>(
            source,
            static _ => true,
            static x => x.ToString());

        observable.Subscribe(Observer.Create<string>(
            _ => { },
            ex => caught = ex,
            () => completed = true));

        await Assert.That(caught).IsSameReferenceAs(expected);
        await Assert.That(completed).IsFalse();
    }

    /// <summary>
    /// OnCompleted from the source observable is passed through to the downstream observer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnCompleted_FromSource_IsPassedThrough()
    {
        var source = Observable.Empty<int>();

        var completed = false;

        var observable = new WhereSelectObservable<int, string>(
            source,
            static _ => true,
            static x => x.ToString());

        observable.Subscribe(Observer.Create<string>(
            _ => { },
            _ => { },
            () => completed = true));

        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// Predicate exception routes to OnError on the downstream observer (lines 59-62).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnNext_PredicateThrows_RoutesToOnError()
    {
        var source = Observable.Return(1);

        Exception? caught = null;

        var observable = new WhereSelectObservable<int, int>(
            source,
            static _ => throw new InvalidOperationException("pred-boom"),
            static x => x);

        observable.Subscribe(Observer.Create<int>(
            _ => { },
            ex => caught = ex,
            () => { }));

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Message).IsEqualTo("pred-boom");
    }

    /// <summary>
    /// Selector exception routes to OnError on the downstream observer (lines 70-73).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task OnNext_SelectorThrows_RoutesToOnError()
    {
        var source = Observable.Return(1);

        Exception? caught = null;

        var observable = new WhereSelectObservable<int, int>(
            source,
            static _ => true,
            static _ => throw new InvalidOperationException("sel-boom"));

        observable.Subscribe(Observer.Create<int>(
            _ => { },
            ex => caught = ex,
            () => { }));

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Message).IsEqualTo("sel-boom");
    }
}
