// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if MONOANDROID13_0
using Android.App;
#endif

using System.Diagnostics.CodeAnalysis;

namespace Akavache;

/// <summary>
/// The file system provider that understands the android.
/// </summary>
#if NET8_0_OR_GREATER
[RequiresUnreferencedCode("Registrations for Akavache.Core")]
[RequiresDynamicCode("Registrations for Akavache.Core")]
#endif
public class AndroidFilesystemProvider : IFilesystemProvider
{
    private readonly SimpleFilesystemProvider _inner = new();

    /// <inheritdoc />
    [SuppressMessage("Roslynator", "RCS1047:Non-asynchronous method name should not end with 'Async'.", Justification = "By Design")]
    public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler) => _inner.OpenFileForReadAsync(path, scheduler);

    /// <inheritdoc />
    [SuppressMessage("Roslynator", "RCS1047:Non-asynchronous method name should not end with 'Async'.", Justification = "By Design")]
    public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler) => _inner.OpenFileForWriteAsync(path, scheduler);

    /// <inheritdoc />
    public IObservable<Unit> CreateRecursive(string path) => _inner.CreateRecursive(path);

    /// <inheritdoc />
    public IObservable<Unit> Delete(string path) => _inner.Delete(path);

    /// <inheritdoc />
    public string? GetDefaultLocalMachineCacheDirectory() => Application.Context.CacheDir?.AbsolutePath;

    /// <inheritdoc />
    public string? GetDefaultRoamingCacheDirectory() => Application.Context.FilesDir?.AbsolutePath;

    /// <inheritdoc />
    public string? GetDefaultSecretCacheDirectory()
    {
        var path = Application.Context.FilesDir?.AbsolutePath;

        if (path is null)
        {
            return null;
        }

        var di = new DirectoryInfo(Path.Combine(path, "Secret"));
        if (!di.Exists)
        {
            di.CreateRecursive();
        }

        return di.FullName;
    }
}
