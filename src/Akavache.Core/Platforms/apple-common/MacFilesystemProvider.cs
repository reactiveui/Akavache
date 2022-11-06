// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Foundation;

namespace Akavache;

/// <summary>
/// A file system provider that is related to the Mac operating system.
/// </summary>
public class MacFilesystemProvider : IFilesystemProvider
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
    public string GetDefaultLocalMachineCacheDirectory() => CreateAppDirectory(NSSearchPathDirectory.CachesDirectory);

    /// <inheritdoc />
    public string GetDefaultRoamingCacheDirectory() => CreateAppDirectory(NSSearchPathDirectory.ApplicationSupportDirectory);

    /// <inheritdoc />
    public string GetDefaultSecretCacheDirectory() => CreateAppDirectory(NSSearchPathDirectory.ApplicationSupportDirectory, "SecretCache");

    private string CreateAppDirectory(NSSearchPathDirectory targetDir, string subDir = "BlobCache")
    {
        using var fm = new NSFileManager();
        var url = fm.GetUrl(targetDir, NSSearchPathDomain.All, null, true, out _);
        if (url == null)
        {
            throw new DirectoryNotFoundException();
        }

        var rp = url.RelativePath;
        if (rp == null)
        {
            throw new DirectoryNotFoundException();
        }

        var ret = Path.Combine(rp, BlobCache.ApplicationName, subDir);
        if (!Directory.Exists(ret))
        {
            _inner.CreateRecursive(ret).Wait();
        }

        return ret;
    }
}
