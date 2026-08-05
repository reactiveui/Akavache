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
    /// <summary>The value the subject under test is seeded with.</summary>
    private const int SeedValue = 10;

    /// <summary>The value pushed through <c>OnNext</c>.</summary>
    private const int PublishedValue = 42;

    /// <summary>A second, distinct value pushed after <see cref="PublishedValue"/>.</summary>
    private const int LaterPublishedValue = 99;

    /// <summary>The first value of the sequential-publish run.</summary>
    private const int FirstSequencedValue = 1;

    /// <summary>The second value of the sequential-publish run.</summary>
    private const int SecondSequencedValue = 2;

    /// <summary>The final value of the sequential-publish run.</summary>
    private const int ThirdSequencedValue = 3;

    /// <summary>Notifications a late subscriber sees: the replayed value plus one future value.</summary>
    private const int ReplayThenFutureCount = 2;

    /// <summary>OnNext after Dispose is a no-op — the value does not change and no observer is notified.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNextAfterDisposeShouldBeNoOp()
    {
        var subject = new SettingsValueSubject<int>(SeedValue);
        subject.Dispose();

        subject.OnNext(PublishedValue);

        await Assert.That(subject.Value).IsEqualTo(SeedValue);
    }

    /// <summary>Subscribing to a disposed subject replays the final value and completes immediately, returning <see cref="Disposable.Empty"/>.</summary>
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
                static _ => { },
                () => completed = true));

        await Assert.That(received).IsEqualTo("hello");
        await Assert.That(completed).IsTrue();

        // The subscription handle should be Disposable.Empty (no-op dispose).
        sub.Dispose();
    }

    /// <summary>Dispose forwards OnCompleted to every live subscriber.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeShouldForwardOnCompletedToAllObservers()
    {
        var subject = new SettingsValueSubject<int>(0);
        var completed1 = false;
        var completed2 = false;

        _ = subject.Subscribe(Observer.Create<int>(static _ => { }, static _ => { }, () => completed1 = true));
        _ = subject.Subscribe(Observer.Create<int>(static _ => { }, static _ => { }, () => completed2 = true));

        subject.Dispose();

        await Assert.That(completed1).IsTrue();
        await Assert.That(completed2).IsTrue();
    }

    /// <summary>Double dispose is idempotent — the second call is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DoubleDisposeShouldBeIdempotent()
    {
        var subject = new SettingsValueSubject<int>(SeedValue);
        subject.Dispose();
        subject.Dispose(); // Should not throw

        await Assert.That(subject.Value).IsEqualTo(SeedValue);
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

        var sub = subject.Subscribe(Observer.Create<int>(values.Add));

        // The subscribe replay should have pushed the seed value.
        await Assert.That(values.Count).IsEqualTo(1);

        sub.Dispose();

        // After unsubscribe, the observer should not see new values.
        subject.OnNext(PublishedValue);
        await Assert.That(values.Count).IsEqualTo(1);
    }

    /// <summary>Unsubscribing when only one observer is present reduces the array to empty (the <c>old.Length == 1</c> fast path).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UnsubscribeSingleObserverShouldReduceToEmpty()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values = new List<int>();

        var sub = subject.Subscribe(Observer.Create<int>(values.Add));
        sub.Dispose();

        subject.OnNext(PublishedValue);

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

        _ = subject.Subscribe(Observer.Create<int>(values1.Add));
        var sub2 = subject.Subscribe(Observer.Create<int>(values2.Add));
        _ = subject.Subscribe(Observer.Create<int>(values3.Add));

        // Remove the middle observer.
        sub2.Dispose();

        subject.OnNext(PublishedValue);

        // Observer 1 and 3 should see the new value; observer 2 should not.
        await Assert.That(values1).Contains(PublishedValue);
        await Assert.That(values3).Contains(PublishedValue);
        await Assert.That(values2).DoesNotContain(PublishedValue);
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

        var sub = subject.Subscribe(Observer.Create<int>(values.Add));
        sub.Dispose();
        sub.Dispose(); // Should not throw or double-remove.

        subject.OnNext(PublishedValue);

        // Only the initial replay value should be present.
        await Assert.That(values.Count).IsEqualTo(1);
    }

    /// <summary>Value getter returns the seed before any OnNext call.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValueShouldReturnSeedBeforeAnyOnNext()
    {
        var subject = new SettingsValueSubject<string>("seed");

        await Assert.That(subject.Value).IsEqualTo("seed");
    }

    /// <summary>OnNext updates the Value and broadcasts to subscribers.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNextShouldUpdateValueAndBroadcast()
    {
        var subject = new SettingsValueSubject<int>(0);
        var received = new List<int>();

        _ = subject.Subscribe(Observer.Create<int>(received.Add));

        subject.OnNext(PublishedValue);

        await Assert.That(subject.Value).IsEqualTo(PublishedValue);
        await Assert.That(received).Contains(PublishedValue);
    }

    /// <summary>OnNext broadcasts the same value to multiple observers simultaneously.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNextShouldBroadcastToAllObservers()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values1 = new List<int>();
        var values2 = new List<int>();

        _ = subject.Subscribe(Observer.Create<int>(values1.Add));
        _ = subject.Subscribe(Observer.Create<int>(values2.Add));

        subject.OnNext(PublishedValue);

        await Assert.That(values1).Contains(PublishedValue);
        await Assert.That(values2).Contains(PublishedValue);
    }

    /// <summary>Multiple sequential OnNext calls each update Value and reach every subscriber.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MultipleOnNextCallsShouldUpdateValueSequentially()
    {
        var subject = new SettingsValueSubject<int>(0);
        var received = new List<int>();

        _ = subject.Subscribe(Observer.Create<int>(received.Add));

        subject.OnNext(FirstSequencedValue);
        subject.OnNext(SecondSequencedValue);
        subject.OnNext(ThirdSequencedValue);

        await Assert.That(subject.Value).IsEqualTo(ThirdSequencedValue);
        await Assert.That(received).Contains(FirstSequencedValue);
        await Assert.That(received).Contains(SecondSequencedValue);
        await Assert.That(received).Contains(ThirdSequencedValue);
    }

    /// <summary>Subscribe replays the current value (updated via OnNext, not just the seed) to a late subscriber.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SubscribeShouldReplayLatestValueNotJustSeed()
    {
        var subject = new SettingsValueSubject<int>(0);

        subject.OnNext(PublishedValue);

        var received = new List<int>();
        _ = subject.Subscribe(Observer.Create<int>(received.Add));

        await Assert.That(received.Count).IsEqualTo(1);
        await Assert.That(received[0]).IsEqualTo(PublishedValue);
    }

    /// <summary>Unsubscribing the first observer of multiple uses the array copy path that copies the trailing segment.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UnsubscribeFirstObserverShouldCopyOnWriteShrink()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values1 = new List<int>();
        var values2 = new List<int>();

        var sub1 = subject.Subscribe(Observer.Create<int>(values1.Add));
        _ = subject.Subscribe(Observer.Create<int>(values2.Add));

        sub1.Dispose();

        subject.OnNext(PublishedValue);

        await Assert.That(values1).DoesNotContain(PublishedValue);
        await Assert.That(values2).Contains(PublishedValue);
    }

    /// <summary>Unsubscribing the last observer of multiple uses the array copy path that copies the leading segment.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UnsubscribeLastObserverShouldCopyOnWriteShrink()
    {
        var subject = new SettingsValueSubject<int>(0);
        var values1 = new List<int>();
        var values2 = new List<int>();

        _ = subject.Subscribe(Observer.Create<int>(values1.Add));
        var sub2 = subject.Subscribe(Observer.Create<int>(values2.Add));

        sub2.Dispose();

        subject.OnNext(PublishedValue);

        await Assert.That(values1).Contains(PublishedValue);
        await Assert.That(values2).DoesNotContain(PublishedValue);
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

        subject.OnNext(PublishedValue);

        var received = new List<int>();
        _ = subject.Subscribe(Observer.Create<int>(received.Add));

        subject.OnNext(LaterPublishedValue);

        await Assert.That(received.Count).IsEqualTo(ReplayThenFutureCount);
        await Assert.That(received[0]).IsEqualTo(PublishedValue);
        await Assert.That(received[1]).IsEqualTo(LaterPublishedValue);
    }

    /// <summary>Dispose with no subscribers completes without error (empty observer array iteration path).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeWithNoSubscribersShouldComplete()
    {
        var subject = new SettingsValueSubject<int>(SeedValue);

        subject.Dispose();

        await Assert.That(subject.Value).IsEqualTo(SeedValue);
    }

    /// <summary>Subscribe with a null observer throws <see cref="ArgumentNullException"/>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SubscribeWithNullObserverShouldThrow() =>
        await Assert.That(static () => new SettingsValueSubject<int>(0).Subscribe(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Disposing the subject (which clears all observers), then disposing a
    /// subscription handle, drives the <c>index &lt; 0</c> early-return path in
    /// <c>Unsubscribe</c> because the observer was already removed by the subject's
    /// own dispose.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UnsubscribeAfterSubjectDisposeShouldHitIndexLessThanZeroPath()
    {
        var subject = new SettingsValueSubject<int>(0);
        var sub = subject.Subscribe(Observer.Create<int>(static _ => { }));

        // Dispose the subject first — clears the observer array.
        subject.Dispose();

        // Now dispose the subscription handle — Unsubscribe finds index < 0.
        sub.Dispose();

        await Assert.That(subject.Value).IsEqualTo(0);
    }

    /// <summary>Unsubscribing all observers then calling OnNext does not throw and has no side effects.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnNextAfterAllUnsubscribedShouldNotThrow()
    {
        var subject = new SettingsValueSubject<int>(0);
        var sub = subject.Subscribe(Observer.Create<int>(static _ => { }));
        sub.Dispose();

        subject.OnNext(PublishedValue);

        await Assert.That(subject.Value).IsEqualTo(PublishedValue);
    }
}
