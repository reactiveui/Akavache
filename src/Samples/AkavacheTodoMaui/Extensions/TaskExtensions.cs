// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;

namespace AkavacheTodoMaui.Extensions;

/// <summary>
/// Extension methods for Task to Observable conversion.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Converts a Task{T} to IObservable{T}.
    /// </summary>
    /// <typeparam name="T">The type of the task result.</typeparam>
    /// <param name="task">The task to convert.</param>
    /// <returns>An observable that produces the task result.</returns>
    public static IObservable<T> ToObservable<T>(this Task<T> task) => Observable.FromAsync(() => task);

    /// <summary>
    /// Converts a Task to IObservable{Unit}.
    /// </summary>
    /// <param name="task">The task to convert.</param>
    /// <returns>An observable that completes when the task completes.</returns>
    public static IObservable<Unit> ToObservable(this Task task) => Observable.FromAsync(() => task);
}
