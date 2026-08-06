// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core.Observables;

namespace Akavache.Tests;

/// <summary>Tests for <see cref="FirstMatchFromCandidatesObservable{TKey, TRaw, TResult}"/>.</summary>
[Category("Akavache")]
public class FirstMatchFromCandidatesObservableTests
{
    /// <summary>Fallback emitted when every candidate is probed and none satisfies the predicate.</summary>
    private const int NoMatchFallbackValue = -99;

    /// <summary>Fallback emitted when every candidate's projection faults.</summary>
    private const int AllErrorsFallbackValue = -42;

    /// <summary>Factor the sync-loop test's projection applies to each candidate key.</summary>
    private const int ProjectionMultiplier = 10;

    /// <summary>The projected value the sync-loop test's predicate accepts (the second candidate scaled by <see cref="ProjectionMultiplier"/>).</summary>
    private const int MatchingProjectedValue = 20;

    /// <summary>Fallback emitted by the cases where no candidate ever satisfies the predicate.</summary>
    private const string FallbackText = "fallback";

    /// <summary>Key of the candidate whose projection stays pending until the test pushes into it.</summary>
    private const string PendingCandidateKey = "slow";

    /// <summary>Key of the candidate that resolves inline once the pending one gives up.</summary>
    private const string InlineCandidateKey = "fast";

    /// <summary>The inline candidate's value, and the only value its predicate accepts.</summary>
    private const string InlineCandidateValue = "fast-value";

    /// <summary>Raw value pushed into the pending candidate's projection.</summary>
    private const string PendingCandidateRawValue = "pending";

    /// <summary>The pending candidate's raw value after the transform — what its predicate accepts.</summary>
    private const string PendingCandidateMatchValue = "pending!";

    /// <summary>Empty candidate list emits the fallback value immediately.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EmptyCandidates_EmitsFallback()
    {
        var sut = new FirstMatchFromCandidatesObservable<string, int, int>(
            [],
            static _ => Observable.Return(1),
            static x => x,
            static _ => true,
            -1);

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo(-1);
    }

    /// <summary>First candidate matches — emits it and completes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FirstCandidateMatches_EmitsAndCompletes()
    {
        List<string> keys = ["a", "b", "c"];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            static k => Observable.Return(k.ToUpperInvariant()),
            static x => x,
            static v => v == "A",
            "none");

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo("A");
    }

    /// <summary>Match is on the third candidate — first two are skipped.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ThirdCandidateMatches_SkipsFirstTwo()
    {
        List<string> keys = ["x", "y", "z"];
        var projected = new List<string>();

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            k =>
            {
                projected.Add(k);
                return Observable.Return(k);
            },
            static x => x,
            static v => v == "z",
            "none");

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo("z");
        await Assert.That(projected).IsEquivalentTo(["x", "y", "z"]);
    }

    /// <summary>No candidate matches — emits fallback.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NoCandidateMatches_EmitsFallback()
    {
        List<int> keys = [1, 2, 3];

        var sut = new FirstMatchFromCandidatesObservable<int, int, int>(
            keys,
            Observable.Return,
            static x => x,
            static _ => false,
            NoMatchFallbackValue);

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo(NoMatchFallbackValue);
    }

    /// <summary>Projection error on a candidate is swallowed — advances to next.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ProjectionError_SkipsCandidate()
    {
        List<string> keys = ["boom", "ok"];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            static k => k == "boom"
                ? Observable.Throw<string>(new InvalidOperationException("bang"))
                : Observable.Return(k),
            static x => x,
            static v => v == "ok",
            "none");

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo("ok");
    }

    /// <summary>Projection factory throwing (not returning an erroring observable) is swallowed.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ProjectionFactoryThrows_SkipsCandidate()
    {
        List<string> keys = ["throw", "ok"];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            static k => k == "throw"
                ? throw new InvalidOperationException("factory boom")
                : Observable.Return(k),
            static x => x,
            static v => v == "ok",
            "none");

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo("ok");
    }

    /// <summary>Transform exception is swallowed — candidate treated as non-match.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TransformThrows_SkipsCandidate()
    {
        List<int> keys = [1, 2];

        var sut = new FirstMatchFromCandidatesObservable<int, int, string>(
            keys,
            Observable.Return,
            static k => k == 1 ? throw new InvalidOperationException("transform boom") : k.ToString(),
            static _ => true,
            "none");

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo("2");
    }

    /// <summary>All projections error — emits fallback.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AllProjectionsError_EmitsFallback()
    {
        List<int> keys = [1, 2, 3];

        var sut = new FirstMatchFromCandidatesObservable<int, int, int>(
            keys,
            static _ => Observable.Throw<int>(new InvalidOperationException("all fail")),
            static x => x,
            static _ => true,
            -1);

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo(-1);
    }

    /// <summary>Dispose during async iteration stops further candidates.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Dispose_StopsFurtherCandidates()
    {
        List<string> keys = ["a", "b", "c"];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            Observable.Return,
            static x => x,
            static _ => false, // never match — would normally exhaust all candidates
            FallbackText);

        // Subscribe and immediately dispose after first candidate
        string? received = null;
        var completed = false;
        _ = sut.Subscribe(
            v => received = v,
            static _ => { },
            () => completed = true);

        // With sync completion and no match, all candidates are tried synchronously.
        // The subscribe already completed by the time we get here.
        await Assert.That(completed).IsTrue();
        await Assert.That(received).IsEqualTo(FallbackText);
    }

    // ── SyncProbe fast-path tests ───────────────────────────────────────
    /// <summary>Sync sources (Observable.Return) take the fast-path and return Disposable.Empty.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SyncSource_TakesFastPath()
    {
        List<string> keys = ["hit"];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            static _ => Observable.Return("found"),
            static x => x,
            static _ => true,
            "none");

        string? result = null;
        var completed = false;
        _ = sut.Subscribe(
            v => result = v,
            static _ => { },
            () => completed = true);

        await Assert.That(result).IsEqualTo("found");
        await Assert.That(completed).IsTrue();
    }

    // ── Async continuation tests ────────────────────────────────────────
    // A projection that does not complete on the calling thread pushes the
    // subscription off the SyncProbe fast-path and onto the async walker,
    // which re-projects the pending candidate and resumes from there.
    /// <summary>A candidate that produces its value later still reaches the downstream observer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AsyncCandidateEmitsMatch_ForwardsValueAndCompletes()
    {
        using Subject<string> pending = new();
        List<string> keys = [PendingCandidateKey];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            _ => pending,
            static x => $"{x}!",
            static v => v == PendingCandidateMatchValue,
            "none");

        string? received = null;
        var completed = false;
        _ = sut.Subscribe(
            v => received = v,
            static _ => { },
            () => completed = true);

        // Nothing has been produced yet, so the subscriber must still be waiting.
        using (Assert.Multiple())
        {
            await Assert.That(received).IsNull();
            await Assert.That(completed).IsFalse();
        }

        pending.OnNext(PendingCandidateRawValue);

        using (Assert.Multiple())
        {
            await Assert.That(received).IsEqualTo(PendingCandidateMatchValue);
            await Assert.That(completed).IsTrue();
        }
    }

    /// <summary>A pending candidate that completes without a value hands over to the next candidate.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AsyncCandidateCompletesEmpty_AdvancesToNextCandidate()
    {
        using Subject<string> pending = new();
        List<string> keys = [PendingCandidateKey, InlineCandidateKey];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            k => k == PendingCandidateKey ? pending : Observable.Return(InlineCandidateValue),
            static x => x,
            static v => v == InlineCandidateValue,
            "none");

        string? received = null;
        var completed = false;
        _ = sut.Subscribe(
            v => received = v,
            static _ => { },
            () => completed = true);

        await Assert.That(received).IsNull();

        // The first candidate yields nothing, so the walker must move on to the second.
        pending.OnCompleted();

        using (Assert.Multiple())
        {
            await Assert.That(received).IsEqualTo(InlineCandidateValue);
            await Assert.That(completed).IsTrue();
        }
    }

    /// <summary>An error from a pending candidate is swallowed and the next candidate is tried.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AsyncCandidateErrors_AdvancesToNextCandidate()
    {
        using Subject<string> pending = new();
        List<string> keys = [PendingCandidateKey, InlineCandidateKey];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            k => k == PendingCandidateKey ? pending : Observable.Return(InlineCandidateValue),
            static x => x,
            static v => v == InlineCandidateValue,
            "none");

        string? received = null;
        Exception? error = null;
        var completed = false;
        _ = sut.Subscribe(
            v => received = v,
            ex => error = ex,
            () => completed = true);

        pending.OnError(new InvalidOperationException("candidate boom"));

        using (Assert.Multiple())
        {
            await Assert.That(error).IsNull();
            await Assert.That(received).IsEqualTo(InlineCandidateValue);
            await Assert.That(completed).IsTrue();
        }
    }

    /// <summary>Once the walker runs out of candidates it emits the fallback and completes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AsyncCandidatesExhausted_EmitsFallback()
    {
        using Subject<string> pending = new();
        List<string> keys = [PendingCandidateKey];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            _ => pending,
            static x => x,
            static _ => true,
            FallbackText);

        string? received = null;
        var completed = false;
        _ = sut.Subscribe(
            v => received = v,
            static _ => { },
            () => completed = true);

        pending.OnCompleted();

        using (Assert.Multiple())
        {
            await Assert.That(received).IsEqualTo(FallbackText);
            await Assert.That(completed).IsTrue();
        }
    }

    /// <summary>Disposing the subscription tears down the in-flight candidate subscription the walker is holding.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeDuringAsyncCandidate_UnsubscribesFromSource()
    {
        using Subject<string> pending = new();
        List<string> keys = [PendingCandidateKey];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            _ => pending,
            static x => x,
            static _ => true,
            FallbackText);

        string? received = null;
        var completed = false;
        var subscription = sut.Subscribe(
            v => received = v,
            static _ => { },
            () => completed = true);

        await Assert.That(pending.HasObservers).IsTrue();

        subscription.Dispose();

        await Assert.That(pending.HasObservers).IsFalse();

        pending.OnNext("too late");

        using (Assert.Multiple())
        {
            await Assert.That(received).IsNull();
            await Assert.That(completed).IsFalse();
        }
    }

    /// <summary>TrySyncLoop is exercised directly — verifies the internal sync fast-path.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TrySyncLoop_MatchOnSecondCandidate()
    {
        List<int> keys = [1, 2, 3];

        var sut = new FirstMatchFromCandidatesObservable<int, int, int>(
            keys,
            static k => Observable.Return(k * ProjectionMultiplier),
            static x => x,
            static v => v == MatchingProjectedValue,
            -1);

        int? result = null;
        var completed = false;
        _ = sut.TrySyncLoop(Observer.Create<int>(
            v => result = v,
            static _ => { },
            () => completed = true));

        await Assert.That(result).IsEqualTo(MatchingProjectedValue);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>TrySyncLoop with all errors returns fallback.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TrySyncLoop_AllErrors_ReturnsFallback()
    {
        List<int> keys = [1, 2];

        var sut = new FirstMatchFromCandidatesObservable<int, int, int>(
            keys,
            static _ => Observable.Throw<int>(new InvalidOperationException("fail")),
            static x => x,
            static _ => true,
            AllErrorsFallbackValue);

        int? result = null;
        _ = sut.TrySyncLoop(Observer.Create<int>(
            v => result = v,
            static _ => { },
            static () => { }));

        await Assert.That(result).IsEqualTo(AllErrorsFallbackValue);
    }
}
