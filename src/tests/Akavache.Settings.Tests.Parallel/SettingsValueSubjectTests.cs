// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Akavache.Settings.Core;

namespace Akavache.Settings.Tests;

/// <summary>
/// Tests for <see cref="SettingsValueSubject{T}"/> covering the OnNext-after-terminal,
/// OnCompleted forwarding, Subscribe-after-completed, Unsubscriber copy-on-write removal,
/// and double-dispose idempotency paths.
/// </summary>
[Category("Akavache")]
public class SettingsValueSubjectTests
{
    /// <summary>
    /// OnNext after Dispose is a no-op — the value does not change and no observer
    /// is notified.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNextAfterDisposeShouldBeNoOp()
    {
        var subject = new SettingsValueSubject<int>(10);
        subject.Dispose();

        subject.OnNext(42);

        await Assert.That(subject.Value).IsEqualTo(10);
    }

    /// <summary>
    /// Subscribing to a disposed subject replays the final value and completes
    /// immediately, returning <see cref="Disposable.Empty"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SubscribeAfterDisposeShouldReplayFinalValueAndComplete()
    {
        var subject = new SettingsValueSubject<string>("hello");
        subject.Dispose();

        string? received = null;
        var completed = false;
        var sub = subject.Subscribe(
            Observer.Create<string>(
                v => received = v,
                _ => { },
                () => completed = true));

        await Assert.That(received).IsEqualTo("hello");
        await Assert.That(completed).IsTrue();

        // The subscription handle should be Disposable.Empty (no-op dispose).
        sub.Dispose();
    }

    /// <summary>
    /// Dispose forwards OnCompleted to every live subscriber.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeShouldForwardOnCompletedToAllObservers()
    {
        var subject = new SettingsValueSubject<int>(0);
        var completed1 = false;
        var completed2 = false;

        subject.Subscribe(Observer.Create<int>(_ => { }, _ => { }, () => completed1 = true));
        subject.Subscribe(Observer.Create<int>(_ => { }, _ => { }, () => completed2 = true));

        subject.Dispose();

        await Assert.That(completed1).IsTrue();
        await Assert.That(completed2).IsTrue();
    }

    /// <summary>
    /// Double dispose is idempotent — the second call is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DoubleDisposeShouldBeIdempotent()
    {
        var subject = new SettingsValueSubject<int>(5);
        subject.Dispose();
        subject.Dispose(); // Should not throw

        await Assert.That(subject.Value).IsEqualTo(5);
    }

    /// <summary>
    /// Disposing the subscription handle removes the observer from the live list —
    /// subsequent OnNext calls do not reach the removed observer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UnsubscribeShouldRemoveObserverFromBroadcast()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values = new List<int>();

        var sub = subject.Subscribe(Observer.Create<int>(v => values.Add(v)));

        // The subscribe replay should have pushed the seed value.
        await Assert.That(values.Count).IsEqualTo(1);

        sub.Dispose();

        // After unsubscribe, the observer should not see new values.
        subject.OnNext(99);
        await Assert.That(values.Count).IsEqualTo(1);
    }

    /// <summary>
    /// Unsubscribing when only one observer is present reduces the array to empty
    /// (the <c>old.Length == 1</c> fast path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UnsubscribeSingleObserverShouldReduceToEmpty()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values = new List<int>();

        var sub = subject.Subscribe(Observer.Create<int>(v => values.Add(v)));
        sub.Dispose();

        subject.OnNext(42);

        // Only the initial replay value should be present.
        await Assert.That(values.Count).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo(0);
    }

    /// <summary>
    /// Unsubscribing one of multiple observers uses the copy-on-write shrink path
    /// that copies array segments around the removed index.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UnsubscribeMiddleObserverShouldCopyOnWriteShrink()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values1 = new List<int>();
        var values2 = new List<int>();
        var values3 = new List<int>();

        var sub1 = subject.Subscribe(Observer.Create<int>(v => values1.Add(v)));
        var sub2 = subject.Subscribe(Observer.Create<int>(v => values2.Add(v)));
        var sub3 = subject.Subscribe(Observer.Create<int>(v => values3.Add(v)));

        // Remove the middle observer.
        sub2.Dispose();

        subject.OnNext(7);

        // Observer 1 and 3 should see the new value; observer 2 should not.
        await Assert.That(values1).Contains(7);
        await Assert.That(values3).Contains(7);
        await Assert.That(values2).DoesNotContain(7);
    }

    /// <summary>
    /// Double-dispose of the <c>Unsubscriber</c> is idempotent — the second call is a
    /// no-op because of the <c>Interlocked.Exchange</c> guard.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DoubleDisposeOfSubscriptionShouldBeIdempotent()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values = new List<int>();

        var sub = subject.Subscribe(Observer.Create<int>(v => values.Add(v)));
        sub.Dispose();
        sub.Dispose(); // Should not throw or double-remove.

        subject.OnNext(5);

        // Only the initial replay value should be present.
        await Assert.That(values.Count).IsEqualTo(1);
    }

    /// <summary>
    /// Value getter returns the seed before any OnNext call.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValueShouldReturnSeedBeforeAnyOnNext()
    {
        var subject = new SettingsValueSubject<string>("seed");

        await Assert.That(subject.Value).IsEqualTo("seed");
    }

    /// <summary>
    /// OnNext updates the Value and broadcasts to subscribers.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNextShouldUpdateValueAndBroadcast()
    {
        var subject = new SettingsValueSubject<int>(0);
        var received = new List<int>();

        subject.Subscribe(Observer.Create<int>(v => received.Add(v)));

        subject.OnNext(42);

        await Assert.That(subject.Value).IsEqualTo(42);
        await Assert.That(received).Contains(42);
    }

    /// <summary>
    /// OnNext broadcasts the same value to multiple observers simultaneously.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNextShouldBroadcastToAllObservers()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values1 = new List<int>();
        var values2 = new List<int>();

        subject.Subscribe(Observer.Create<int>(v => values1.Add(v)));
        subject.Subscribe(Observer.Create<int>(v => values2.Add(v)));

        subject.OnNext(99);

        await Assert.That(values1).Contains(99);
        await Assert.That(values2).Contains(99);
    }

    /// <summary>
    /// Multiple sequential OnNext calls each update Value and reach every subscriber.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MultipleOnNextCallsShouldUpdateValueSequentially()
    {
        var subject = new SettingsValueSubject<int>(0);
        var received = new List<int>();

        subject.Subscribe(Observer.Create<int>(v => received.Add(v)));

        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);

        await Assert.That(subject.Value).IsEqualTo(3);
        await Assert.That(received).Contains(1);
        await Assert.That(received).Contains(2);
        await Assert.That(received).Contains(3);
    }

    /// <summary>
    /// Subscribe replays the current value (updated via OnNext, not just the seed)
    /// to a late subscriber.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SubscribeShouldReplayLatestValueNotJustSeed()
    {
        var subject = new SettingsValueSubject<int>(0);

        subject.OnNext(55);

        var received = new List<int>();
        subject.Subscribe(Observer.Create<int>(v => received.Add(v)));

        await Assert.That(received.Count).IsEqualTo(1);
        await Assert.That(received[0]).IsEqualTo(55);
    }

    /// <summary>
    /// Unsubscribing the first observer of multiple uses the array copy path that
    /// copies the trailing segment.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UnsubscribeFirstObserverShouldCopyOnWriteShrink()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values1 = new List<int>();
        var values2 = new List<int>();

        var sub1 = subject.Subscribe(Observer.Create<int>(v => values1.Add(v)));
        subject.Subscribe(Observer.Create<int>(v => values2.Add(v)));

        sub1.Dispose();

        subject.OnNext(7);

        await Assert.That(values1).DoesNotContain(7);
        await Assert.That(values2).Contains(7);
    }

    /// <summary>
    /// Unsubscribing the last observer of multiple uses the array copy path that
    /// copies the leading segment.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UnsubscribeLastObserverShouldCopyOnWriteShrink()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values1 = new List<int>();
        var values2 = new List<int>();

        subject.Subscribe(Observer.Create<int>(v => values1.Add(v)));
        var sub2 = subject.Subscribe(Observer.Create<int>(v => values2.Add(v)));

        sub2.Dispose();

        subject.OnNext(7);

        await Assert.That(values1).Contains(7);
        await Assert.That(values2).DoesNotContain(7);
    }

    /// <summary>
    /// Subscribing after OnNext was called replays the updated (not seed) value,
    /// then the observer receives future values normally.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SubscribeAfterOnNextShouldReplayThenReceiveFuture()
    {
        var subject = new SettingsValueSubject<int>(0);

        subject.OnNext(10);

        var received = new List<int>();
        subject.Subscribe(Observer.Create<int>(v => received.Add(v)));

        subject.OnNext(20);

        await Assert.That(received.Count).IsEqualTo(2);
        await Assert.That(received[0]).IsEqualTo(10);
        await Assert.That(received[1]).IsEqualTo(20);
    }

    /// <summary>
    /// Dispose with no subscribers completes without error (empty observer array
    /// iteration path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeWithNoSubscribersShouldComplete()
    {
        var subject = new SettingsValueSubject<int>(42);

        subject.Dispose();

        await Assert.That(subject.Value).IsEqualTo(42);
    }

    /// <summary>
    /// Subscribe with a null observer throws <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SubscribeWithNullObserverShouldThrow() =>
        await Assert.That(() => new SettingsValueSubject<int>(0).Subscribe(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Disposing the subject (which clears all observers), then disposing a
    /// subscription handle, drives the <c>index &lt; 0</c> early-return path in
    /// <c>Unsubscribe</c> (lines 205-207) because the observer was already removed
    /// by the subject's own dispose.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UnsubscribeAfterSubjectDisposeShouldHitIndexLessThanZeroPath()
    {
        var subject = new SettingsValueSubject<int>(0);
        var sub = subject.Subscribe(Observer.Create<int>(_ => { }));

        // Dispose the subject first — clears the observer array.
        subject.Dispose();

        // Now dispose the subscription handle — Unsubscribe finds index < 0.
        sub.Dispose();

        await Assert.That(subject.Value).IsEqualTo(0);
    }

    /// <summary>
    /// Unsubscribing all observers then calling OnNext does not throw and has no
    /// side effects.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNextAfterAllUnsubscribedShouldNotThrow()
    {
        var subject = new SettingsValueSubject<int>(0);
        var sub = subject.Subscribe(Observer.Create<int>(_ => { }));
        sub.Dispose();

        subject.OnNext(99);

        await Assert.That(subject.Value).IsEqualTo(99);
    }
}
