// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;

namespace Akavache
{
    /// <summary>
    /// A mock set of facades. On .net Standard they aren't supported and you should be using
    /// a application framework (such as .NET Core/.Net Framework) to execute in the end.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Gets a MD5 hash the input.
        /// </summary>
        /// <param name="input">The value we are generating.</param>
        /// <returns>The MD5 hash.</returns>
        /// <exception cref="NotImplementedException">We use bait and switch on final platforms.</exception>
        public static string GetMd5Hash(string input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Logs a series of errors into a blob cache.
        /// </summary>
        /// <typeparam name="T">The type of item stored inside the blob cache.</typeparam>
        /// <param name="blobCache">The blob cache where to store the errors.</param>
        /// <param name="message">The message to store.</param>
        /// <returns>An observable that signals when the operation is completed.</returns>
        public static IObservable<T> LogErrors<T>(this IObservable<T> blobCache, string message = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copies a blob to the stream.
        /// </summary>
        /// <param name="blobCache">The blob to copy.</param>
        /// <param name="destination">The destination stream.</param>
        /// <param name="scheduler">The scheduler to use to perform the copy.</param>
        /// <returns>An observable that signals when the operation is completed.</returns>
        public static IObservable<Unit> CopyToAsync(this Stream blobCache, Stream destination, IScheduler scheduler = null)
        {
            throw new NotImplementedException();
        }
    }
}
