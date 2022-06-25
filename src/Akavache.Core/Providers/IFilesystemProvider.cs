// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// An abstraction for the simple file operations that an IBlobCache can
/// perform. Create a new instance of this when adapting IBlobCache to
/// different platforms or backing stores, or for testing purposes.
/// </summary>
public interface IFilesystemProvider
{
    /// <summary>
    /// Open a file on a background thread, with the File object in 'async
    /// mode'. It is critical that this operation is deferred and returns
    /// immediately (i.e. wrapped in an Observable.Start).
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="scheduler">The scheduler to schedule the open under.</param>
    /// <returns>A Future result representing the Open file.</returns>
    IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler);

    /// <summary>
    /// Open a file on a background thread, with the File object in 'async
    /// mode'. It is critical that this operation is deferred and returns
    /// immediately (i.e. wrapped in an Observable.Start).
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="scheduler">The scheduler to schedule the open under.</param>
    /// <returns>A Future result representing the Open file.</returns>
    IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler);

    /// <summary>
    /// Create a directory and its parents. If the directory already
    /// exists, this method does nothing (i.e. it does not throw if a
    /// directory exists).
    /// </summary>
    /// <param name="path">The path to create.</param>
    /// <returns>A observable which signals when the create is finished.</returns>
    IObservable<Unit> CreateRecursive(string path);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>A observable which signals when the delete is finished.</returns>
    IObservable<Unit> Delete(string path);

    /// <summary>
    /// Gets the default local machine cache directory (i.e. the one for temporary data).
    /// </summary>
    /// <returns>The default local machine cache directory.</returns>
    string? GetDefaultLocalMachineCacheDirectory();

    /// <summary>
    /// Gets the default roaming cache directory (i.e. the one for user settings).
    /// </summary>
    /// <returns>The default roaming cache directory.</returns>
    string? GetDefaultRoamingCacheDirectory();

    /// <summary>
    /// Gets the default roaming cache directory (i.e. the one for user settings).
    /// </summary>
    /// <returns>The default roaming cache directory.</returns>
    string? GetDefaultSecretCacheDirectory();
}