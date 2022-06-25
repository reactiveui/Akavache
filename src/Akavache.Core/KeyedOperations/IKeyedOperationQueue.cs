// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// A queue that uses keys to determine the queue operations.
/// </summary>
public interface IKeyedOperationQueue
{
    /// <summary>
    ///   Queue an operation to run in the background that returns a stream of values. All operations with the
    ///   same key will run in sequence, waiting for the previous operation to complete.
    ///   If you want to queue an operation that already returns IObservable, this is your guy.
    /// </summary>
    /// <param name="key">The key to use.</param>
    /// <param name="asyncCalculationFunc">A method to run in the background that returns a stream of values.</param>
    /// <typeparam name="T">The type of item to queue.</typeparam>
    /// <returns>A future stream of values.</returns>
    IObservable<T> EnqueueObservableOperation<T>(string key, Func<IObservable<T>> asyncCalculationFunc);

    /// <summary>
    ///   Queue an operation to run in the background that returns a value. All operations with the same key will
    ///   run in sequence, waiting for the previous operation to complete.
    /// </summary>
    /// <param name="key">The key to use.</param>
    /// <param name="calculationFunc">A method to run in the background that returns a single value.</param>
    /// <typeparam name="T">The type of item to queue.</typeparam>
    /// <returns>A future value.</returns>
    IObservable<T> EnqueueOperation<T>(string key, Func<T> calculationFunc);

    /// <summary>
    ///   Queue an operation to run in the background. All operations with the same key will run in sequence,
    ///   waiting for the previous operation to complete.
    /// </summary>
    /// <param name="key">The key to use.</param>
    /// <param name="action">A method to run in the background.</param>
    /// <returns>A future representing when the operation completes.</returns>
    IObservable<Unit> EnqueueOperation(string key, Action action);

    /// <summary>
    ///   Flushes the remaining operations and returns a signal when they are all complete.
    /// </summary>
    /// <returns>An observable that signals when the queue has shutdown.</returns>
    IObservable<Unit> ShutdownQueue();
}