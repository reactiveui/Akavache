// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Tizen.Applications;

namespace Akavache;

/// <summary>
/// A file system provider for the Tizen platform.
/// </summary>
public class TizenFilesystemProvider : IFilesystemProvider
{
    private readonly SimpleFilesystemProvider _inner = new();

    /// <inheritdoc />
    public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler) => _inner.OpenFileForReadAsync(path, scheduler);

    /// <inheritdoc />
    public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler) => _inner.OpenFileForWriteAsync(path, scheduler);

    /// <inheritdoc />
    public IObservable<Unit> CreateRecursive(string path) => _inner.CreateRecursive(path);

    /// <inheritdoc />
    public IObservable<Unit> Delete(string path) => _inner.Delete(path);

    /// <inheritdoc />
    public string GetDefaultLocalMachineCacheDirectory() => Application.Current.DirectoryInfo.Cache;

    /// <inheritdoc />
    public string GetDefaultRoamingCacheDirectory() => Application.Current.DirectoryInfo.ExternalCache;

    /// <inheritdoc />
    public string GetDefaultSecretCacheDirectory()
    {
        var path = Application.Current.DirectoryInfo.ExternalCache;
        var di = new System.IO.DirectoryInfo(Path.Combine(path, "Secret"));
        if (!di.Exists)
        {
            di.CreateRecursive();
        }

        return di.FullName;
    }
}