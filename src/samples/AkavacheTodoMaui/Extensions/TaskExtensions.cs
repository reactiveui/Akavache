// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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
        public IObservable<Unit> ToObservable() => Observable.FromAsync(() => task);
    }

    /// <summary>Observable conversions for a task that produces a result.</summary>
    /// <typeparam name="T">The type of the task result.</typeparam>
    /// <param name="task">The task to convert.</param>
    extension<T>(Task<T> task)
    {
        /// <summary>Converts a Task{T} to IObservable{T}.</summary>
        /// <returns>An observable that produces the task result.</returns>
        public IObservable<T> ToObservable() => Observable.FromAsync(() => task);
    }
}
