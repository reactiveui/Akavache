// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Akavache.Settings.Tests;

/// <summary>
/// A helper for the different tests.
/// </summary>
internal static class TestHelper
{
    /// <summary>
    /// Polls a condition until it returns <see langword="true"/> or the timeout expires.
    /// Handles transient disposal exceptions as retryable.
    /// </summary>
    /// <param name="condition">An asynchronous function that returns <see langword="true"/> when the condition is satisfied.</param>
    /// <param name="timeoutMs">The maximum time, in milliseconds, to wait before failing the assertion. Default is 10000ms.</param>
    /// <param name="initialDelayMs">The initial delay between polls, in milliseconds. Default is 50ms.</param>
    /// <param name="backoff">The multiplicative backoff applied to the delay between retries. Default is 1.5.</param>
    /// <param name="maxDelayMs">The maximum delay between polls, in milliseconds. Default is 500ms.</param>
    /// <returns>A task that completes when the condition is satisfied or fails the test on timeout.</returns>
    public static async Task EventuallyAsync(
        Func<Task<bool>> condition,
        int timeoutMs = 10000,
        int initialDelayMs = 50,
        double backoff = 1.5,
        int maxDelayMs = 500)
    {
        var sw = Stopwatch.StartNew();
        var delay = initialDelayMs;

        // Initial small delay to allow async operations to start
        await Task.Delay(delay).ConfigureAwait(false);

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            bool ok;
            try
            {
                ok = await condition().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                ok = false;
            }
            catch (InvalidOperationException ex) when (IsDisposedMessage(ex))
            {
                ok = false;
            }

            if (ok)
            {
                return;
            }

            await Task.Delay(delay).ConfigureAwait(false);
            delay = Math.Min((int)(delay * backoff), maxDelayMs);
        }

        Assert.Fail($"Condition not met within {timeoutMs}ms.");
    }

    /// <summary>
    /// Polls a condition until it returns <see langword="true"/> or the timeout expires.
    /// Handles transient disposal exceptions as retryable.
    /// </summary>
    /// <param name="condition">A synchronous function that returns <see langword="true"/> when the condition is satisfied.</param>
    /// <param name="timeoutMs">The maximum time, in milliseconds, to wait before failing the assertion. Default is 10000ms.</param>
    /// <param name="initialDelayMs">The initial delay between polls, in milliseconds. Default is 50ms.</param>
    /// <param name="backoff">The multiplicative backoff applied to the delay between retries. Default is 1.5.</param>
    /// <param name="maxDelayMs">The maximum delay between polls, in milliseconds. Default is 500ms.</param>
    /// <returns>A task that completes when the condition is satisfied or fails the test on timeout.</returns>
    public static Task EventuallyAsync(
        Func<bool> condition,
        int timeoutMs = 10000,
        int initialDelayMs = 50,
        double backoff = 1.5,
        int maxDelayMs = 500) =>
        EventuallyAsync(() => Task.FromResult(condition()), timeoutMs, initialDelayMs, backoff, maxDelayMs);

    /// <summary>
    /// Opens a fresh secure settings store, runs an async action, disposes the store,
    /// and treats transient disposal as retryable (returns false).
    /// </summary>
    /// <param name="instance">The Akavache instance.</param>
    /// <param name="getViewSettings">Gets the view settings.</param>
    /// <param name="action">The action to execute against the fresh store.</param>
    /// <returns>
    /// True if the action completes successfully; false if a transient disposal occurred and the caller should retry.
    /// </returns>
    public static async Task<bool> WithFreshStoreAsync(
        IAkavacheInstance instance,
        Func<ViewSettings?> getViewSettings,
        Func<ViewSettings, Task<bool>> action)
    {
        ViewSettings? s = null;
        try
        {
            s = getViewSettings();

            if (s != null)
            {
                return await action(s).ConfigureAwait(false);
            }

            Assert.Fail("Must have a valid setting");
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (InvalidOperationException ex) when (IsDisposedMessage(ex))
        {
            return false;
        }
        finally
        {
            if (s is not null)
            {
                try
                {
                    await s.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Best-effort dispose.
                }
            }
        }
    }

    /// <summary>
    /// Attempts to evaluate a getter/condition that may touch a cache; treats disposal as transient.
    /// Also treats SQLite "file is not a database" (wrong key / encrypted) as transient.
    /// </summary>
    /// <param name="probe">A function that evaluates to <see langword="true"/> when the condition is satisfied.</param>
    /// <returns>True if the probe succeeded and returned true; false on transient disposal or false condition.</returns>
    public static bool TryRead(Func<bool> probe)
    {
        try
        {
            return probe();
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (InvalidOperationException ex) when (IsDisposedMessage(ex))
        {
            return false;
        }
        catch (Exception ex) when (IsSqliteNotADatabaseMessage(ex))
        {
            // Opening an encrypted SQLite DB with the wrong key throws messages like:
            // "file is not a database" or "file is encrypted or is not a database".
            // Treat as transient / non-fatal in polling.
            return false;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if the supplied exception message looks like a "disposed" transient from Rx.
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns>True if the message indicates a disposed resource; otherwise, false.</returns>
    public static bool IsDisposedMessage(this InvalidOperationException ex) =>
        ex.Message.Contains("disposed", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True if the exception message looks like SQLCipher/wrong-key open:
    /// "file is not a database" or "file is encrypted or is not a database".
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns>True if the message indicates a disposed resource; otherwise, false.</returns>
    private static bool IsSqliteNotADatabaseMessage(Exception ex) =>
        ex.Message.Contains("not a database", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("file is encrypted", StringComparison.OrdinalIgnoreCase);
}
