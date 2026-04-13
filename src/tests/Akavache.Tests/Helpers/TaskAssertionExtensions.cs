// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using TUnit.Assertions.Exceptions;

namespace Akavache.Tests;

/// <summary>
/// Task-based exception assertions used by tests instead of TUnit's
/// <c>Assert.That(() =&gt; task).Throws&lt;T&gt;()</c> pattern.
/// <para>
/// TUnit's generic <c>Assert.That&lt;T&gt;(Func&lt;Task&lt;T?&gt;&gt;)</c> overload fails
/// to unify non-nullable <see cref="Task{T}"/> results with its internally-nullable
/// target type, producing CS8619 noise. These extensions bypass that overload by
/// operating on an already-materialized <see cref="Task"/>, so no generic covariance
/// puzzle arises.
/// </para>
/// </summary>
internal static class TaskAssertionExtensions
{
    /// <summary>
    /// Awaits <paramref name="task"/> and asserts that it faults with an exception
    /// of type <typeparamref name="TException"/> (or a derived type).
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="task">The task to await.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    public static async Task ShouldThrowAsync<TException>(this Task task)
        where TException : Exception
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"Expected {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}");
        }

        throw new AssertionException(
            $"Expected {typeof(TException).Name} but no exception was thrown.");
    }
}
