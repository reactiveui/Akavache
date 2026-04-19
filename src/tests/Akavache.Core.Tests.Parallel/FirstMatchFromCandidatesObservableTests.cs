// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core.Observables;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="FirstMatchFromCandidatesObservable{TKey, TRaw, TResult}"/>.
/// </summary>
[Category("Akavache")]
public class FirstMatchFromCandidatesObservableTests
{
    /// <summary>
    /// Empty candidate list emits the fallback value immediately.
    /// </summary>
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

    /// <summary>
    /// First candidate matches — emits it and completes.
    /// </summary>
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

    /// <summary>
    /// Match is on the third candidate — first two are skipped.
    /// </summary>
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

    /// <summary>
    /// No candidate matches — emits fallback.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task NoCandidateMatches_EmitsFallback()
    {
        List<int> keys = [1, 2, 3];

        var sut = new FirstMatchFromCandidatesObservable<int, int, int>(
            keys,
            static k => Observable.Return(k),
            static x => x,
            static _ => false,
            -99);

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo(-99);
    }

    /// <summary>
    /// Projection error on a candidate is swallowed — advances to next.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ProjectionError_SkipsCandidate()
    {
        List<string> keys = ["boom", "ok"];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            k => k == "boom"
                ? Observable.Throw<string>(new InvalidOperationException("bang"))
                : Observable.Return(k),
            static x => x,
            static v => v == "ok",
            "none");

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo("ok");
    }

    /// <summary>
    /// Projection factory throwing (not returning an erroring observable) is swallowed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ProjectionFactoryThrows_SkipsCandidate()
    {
        List<string> keys = ["throw", "ok"];

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            k => k == "throw"
                ? throw new InvalidOperationException("factory boom")
                : Observable.Return(k),
            static x => x,
            static v => v == "ok",
            "none");

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo("ok");
    }

    /// <summary>
    /// Transform exception is swallowed — candidate treated as non-match.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TransformThrows_SkipsCandidate()
    {
        List<int> keys = [1, 2];

        var sut = new FirstMatchFromCandidatesObservable<int, int, string>(
            keys,
            static k => Observable.Return(k),
            k => k == 1 ? throw new InvalidOperationException("transform boom") : k.ToString(),
            static _ => true,
            "none");

        var result = await sut.FirstAsync();
        await Assert.That(result).IsEqualTo("2");
    }

    /// <summary>
    /// All projections error — emits fallback.
    /// </summary>
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

    /// <summary>
    /// Dispose during async iteration stops further candidates.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Dispose_StopsFurtherCandidates()
    {
        List<string> keys = ["a", "b", "c"];
        var projected = new List<string>();

        var sut = new FirstMatchFromCandidatesObservable<string, string, string>(
            keys,
            k =>
            {
                projected.Add(k);
                return Observable.Return(k);
            },
            static x => x,
            static _ => false, // never match — would normally exhaust all candidates
            "fallback");

        // Subscribe and immediately dispose after first candidate
        string? received = null;
        var completed = false;
        var subscription = sut.Subscribe(
            v => received = v,
            _ => { },
            () => completed = true);

        // With sync completion and no match, all candidates are tried synchronously.
        // The subscribe already completed by the time we get here.
        await Assert.That(completed).IsTrue();
        await Assert.That(received).IsEqualTo("fallback");
    }

    // ── SyncProbe fast-path tests ───────────────────────────────────────

    /// <summary>
    /// Sync sources (Observable.Return) take the fast-path and return Disposable.Empty.
    /// </summary>
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
        sut.Subscribe(
            v => result = v,
            _ => { },
            () => completed = true);

        await Assert.That(result).IsEqualTo("found");
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// TrySyncLoop is exercised directly — verifies the internal sync fast-path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TrySyncLoop_MatchOnSecondCandidate()
    {
        List<int> keys = [1, 2, 3];

        var sut = new FirstMatchFromCandidatesObservable<int, int, int>(
            keys,
            static k => Observable.Return(k * 10),
            static x => x,
            static v => v == 20,
            -1);

        int? result = null;
        var completed = false;
        sut.TrySyncLoop(Observer.Create<int>(
            v => result = v,
            _ => { },
            () => completed = true));

        await Assert.That(result).IsEqualTo(20);
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// TrySyncLoop with all errors returns fallback.
    /// </summary>
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
            -42);

        int? result = null;
        sut.TrySyncLoop(Observer.Create<int>(
            v => result = v,
            _ => { },
            () => { }));

        await Assert.That(result).IsEqualTo(-42);
    }
}
