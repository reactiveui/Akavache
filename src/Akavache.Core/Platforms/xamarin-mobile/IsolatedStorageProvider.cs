// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO.IsolatedStorage;
using System.Reactive.Disposables;

namespace Akavache;

/// <summary>
/// A storage provided that uses isolated storage.
/// </summary>
public class IsolatedStorageProvider : IFilesystemProvider
{
    /// <inheritdoc />
    public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler) =>
        Observable.Create<Stream>(subj =>
        {
            var disp = new CompositeDisposable();
            IsolatedStorageFile fs;
            try
            {
                fs = IsolatedStorageFile.GetUserStoreForApplication();
                disp.Add(fs);
                disp.Add(Observable.Start(() => fs.OpenFile(path, FileMode.Open, FileAccess.Read, FileShare.Read), BlobCache.TaskpoolScheduler).Subscribe(subj));
            }
            catch (Exception ex)
            {
                subj.OnError(ex);
            }

            return disp;
        });

    /// <inheritdoc />
    public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler) =>
        Observable.Create<Stream>(subj =>
        {
            var disp = new CompositeDisposable();
            IsolatedStorageFile fs;
            try
            {
                fs = IsolatedStorageFile.GetUserStoreForApplication();
                disp.Add(fs);
                disp.Add(Observable.Start(() => fs.OpenFile(path, FileMode.Create, FileAccess.Write, FileShare.None), BlobCache.TaskpoolScheduler).Subscribe(subj));
            }
            catch (Exception ex)
            {
                subj.OnError(ex);
            }

            return disp;
        });

    /// <inheritdoc />
    public IObservable<Unit> CreateRecursive(string dirPath) =>
        Observable.Start(
            () =>
            {
                using var fs = IsolatedStorageFile.GetUserStoreForApplication();
                var acc = string.Empty;
                foreach (var x in dirPath.Split(Path.DirectorySeparatorChar))
                {
                    var path = Path.Combine(acc, x);

                    if (path[path.Length - 1] == Path.VolumeSeparatorChar)
                    {
                        path += Path.DirectorySeparatorChar;
                    }

                    if (!fs.DirectoryExists(path))
                    {
                        fs.CreateDirectory(path);
                    }

                    acc = path;
                }
            },
            BlobCache.TaskpoolScheduler);

    /// <inheritdoc />
    public IObservable<Unit> Delete(string path) =>
        Observable.Start(
            () =>
            {
                using var fs = IsolatedStorageFile.GetUserStoreForApplication();
                if (!fs.FileExists(path))
                {
                    return;
                }

                try
                {
                    fs.DeleteFile(path);
                }
                catch (FileNotFoundException)
                {
                }
                catch (IsolatedStorageException)
                {
                }
            },
            BlobCache.TaskpoolScheduler);

    /// <inheritdoc />
    public string GetDefaultRoamingCacheDirectory() => "BlobCache";

    /// <inheritdoc />
    public string GetDefaultSecretCacheDirectory() => "SecretCache";

    /// <inheritdoc />
    public string GetDefaultLocalMachineCacheDirectory() => "LocalBlobCache";
}