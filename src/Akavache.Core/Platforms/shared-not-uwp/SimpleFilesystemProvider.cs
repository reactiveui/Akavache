// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Splat;

namespace Akavache;

/// <summary>
/// A wrapper around the file system.
/// </summary>
public class SimpleFilesystemProvider : IFilesystemProvider
{
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1047:Non-asynchronous method name should not end with 'Async'.", Justification = "By Design")]
    public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler) => Utility.SafeOpenFileAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read, scheduler);

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1047:Non-asynchronous method name should not end with 'Async'.", Justification = "By Design")]
    public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler) => Utility.SafeOpenFileAsync(path, FileMode.Create, FileAccess.Write, FileShare.None, scheduler);

    /// <inheritdoc />
    public IObservable<Unit> CreateRecursive(string path)
    {
        Utility.CreateRecursive(new(path));
        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc />
    public IObservable<Unit> Delete(string path) => Observable.Start(() => File.Delete(path), BlobCache.TaskpoolScheduler);

    /// <inheritdoc />
    public string GetDefaultRoamingCacheDirectory() =>
        ModeDetector.InUnitTestRunner() ?
            Path.Combine(GetAssemblyDirectoryName(), "BlobCache") :
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BlobCache.ApplicationName, "BlobCache");

    /// <inheritdoc />
    public string GetDefaultSecretCacheDirectory() =>
        ModeDetector.InUnitTestRunner() ?
            Path.Combine(GetAssemblyDirectoryName(), "SecretCache") :
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BlobCache.ApplicationName, "SecretCache");

    /// <inheritdoc />
    public string GetDefaultLocalMachineCacheDirectory() =>
        ModeDetector.InUnitTestRunner() ?
            Path.Combine(GetAssemblyDirectoryName(), "LocalBlobCache") :
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), BlobCache.ApplicationName, "BlobCache");

    /// <summary>
    /// Gets the assembly directory name.
    /// </summary>
    /// <returns>The assembly directory name.</returns>
    protected static string GetAssemblyDirectoryName()
    {
        var assemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        return assemblyDirectoryName ?? throw new InvalidOperationException("The directory name of the assembly location is null");
    }
}
