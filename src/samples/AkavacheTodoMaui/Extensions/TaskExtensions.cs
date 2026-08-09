// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace AkavacheTodoMaui.Extensions;

/// <summary>Extension methods for Task to Observable conversion.</summary>
public static class TaskExtensions
{
    /// <summary>Observable conversions for a task that produces no result.</summary>
    /// <param name="task">The task to convert.</param>
    extension(Task task)
    {
        /// <summary>Converts a Task to IObservable{Unit}.</summary>
        /// <returns>An observable that completes when the task completes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> ToObservable() => Signal.FromAsync(async () =>
        {
            await task.ConfigureAwait(false);
            return RxVoid.Default;
        });
    }

    /// <summary>Observable conversions for a task that produces a result.</summary>
    /// <typeparam name="T">The type of the task result.</typeparam>
    /// <param name="task">The task to convert.</param>
    extension<T>(Task<T> task)
    {
        /// <summary>Converts a Task{T} to IObservable{T}.</summary>
        /// <returns>An observable that produces the task result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<T> ToObservable() => Signal.FromAsync(() => task);
    }
}
