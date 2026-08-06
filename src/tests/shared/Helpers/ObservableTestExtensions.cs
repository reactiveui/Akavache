// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

/// <summary>
/// Test-only extension methods for subscribing to observables without
/// <c>ToTask()</c> or <c>FirstAsync()</c> bridges. Two families:
/// <list type="bullet">
///   <item><c>SubscribeGet*</c> — for caches using <c>ImmediateScheduler</c> where
///   Subscribe completes synchronously on the calling thread.</item>
///   <item><c>WaitFor*</c> — for real SQLite caches where the worker thread delivers
///   results asynchronously; blocks via <see cref="ManualResetEventSlim"/>.</item>
/// </list>
/// </summary>
internal static class ObservableTestExtensions
{
    /// <summary>Default timeout for <c>WaitFor*</c> methods.</summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Extension members for any observable sequence.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The observable the members operate on.</param>
    extension<T>(IObservable<T> source)
    {
        // ── Synchronous (ImmediateScheduler) ─────────────────────────────
        /// <summary>
        /// Subscribes and returns the single value emitted by the source.
        /// Only safe when the observable completes synchronously (e.g. <c>ImmediateScheduler</c>).
        /// </summary>
        /// <returns>The emitted value.</returns>
        internal T? SubscribeGetValue()
        {
            T? result = default;
            _ = source.Subscribe(v => result = v);
            return result;
        }

        /// <summary>Subscribes and captures any error emitted by the source. Only safe when the observable completes synchronously.</summary>
        /// <returns>The captured exception, or <see langword="null"/> if none.</returns>
        internal Exception? SubscribeGetError()
        {
            Exception? error = null;
            _ = source.Subscribe(static _ => { }, ex => error = ex);
            return error;
        }

        // ── Blocking (worker-thread / real SQLite) ───────────────────────
        /// <summary>Subscribes and blocks until the source emits a value or completes. Safe for observables that deliver on a background thread.</summary>
        /// <param name="timeout">Optional timeout override.</param>
        /// <returns>The emitted value.</returns>
        internal T? WaitForValue(TimeSpan? timeout = null)
        {
            T? result = default;
            using ManualResetEventSlim done = new();
            _ = source.Subscribe(
                v => result = v,
                _ => done.Set(),
                done.Set);
            if (!done.Wait(timeout ?? DefaultTimeout))
            {
                throw new TimeoutException($"WaitForValue timed out after {(timeout ?? DefaultTimeout).TotalSeconds}s.");
            }

            return result;
        }

        /// <summary>Subscribes and blocks until the source completes, capturing any error. Safe for observables that deliver on a background thread.</summary>
        /// <param name="timeout">Optional timeout override.</param>
        /// <returns>The captured exception, or <see langword="null"/> if none.</returns>
        internal Exception? WaitForError(TimeSpan? timeout = null)
        {
            Exception? error = null;
            using ManualResetEventSlim done = new();
            _ = source.Subscribe(
                static _ => { },
                ex =>
                {
                    error = ex;
                    done.Set();
                },
                done.Set);
            if (!done.Wait(timeout ?? DefaultTimeout))
            {
                throw new TimeoutException($"WaitForError timed out after {(timeout ?? DefaultTimeout).TotalSeconds}s.");
            }

            return error;
        }
    }

    /// <summary>Extension members for observable sequences that only signal completion.</summary>
    /// <param name="source">The observable the members operate on.</param>
    extension(IObservable<Unit> source)
    {
        /// <summary>Subscribes to a <see cref="Unit"/>-producing observable, discarding the value. Only safe when the observable completes synchronously.</summary>
        internal void SubscribeAndComplete() =>
            source.Subscribe();

        /// <summary>Subscribes and blocks until the source completes. Safe for observables that deliver on a background thread.</summary>
        /// <param name="timeout">Optional timeout override.</param>
        internal void WaitForCompletion(TimeSpan? timeout = null)
        {
            Exception? error = null;
            using ManualResetEventSlim done = new();
            _ = source.Subscribe(
                static _ => { },
                ex =>
                {
                    error = ex;
                    done.Set();
                },
                done.Set);
            if (!done.Wait(timeout ?? DefaultTimeout))
            {
                throw new TimeoutException($"WaitForCompletion timed out after {(timeout ?? DefaultTimeout).TotalSeconds}s.");
            }

            if (error is null)
            {
                return;
            }

            throw error;
        }
    }
}
