// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Akavache
{
    /// <summary>
    /// A storage provided that uses isolated storage.
    /// </summary>
    public class IsolatedStorageProvider : IFilesystemProvider
    {
        /// <inheritdoc />
        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
        {
            return Observable.Create<Stream>(subj =>
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
        }

        /// <inheritdoc />
        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
        {
            return Observable.Create<Stream>(subj =>
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
        }

        /// <inheritdoc />
        public IObservable<Unit> CreateRecursive(string dirPath)
        {
            return Observable.Start(
                () =>
                {
                    using (var fs = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        string acc = string.Empty;
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
                    }
                },
                BlobCache.TaskpoolScheduler);
        }

        /// <inheritdoc />
        public IObservable<Unit> Delete(string path)
        {
            return Observable.Start(
                () =>
                {
                    using (var fs = IsolatedStorageFile.GetUserStoreForApplication())
                    {
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
                    }
                },
                BlobCache.TaskpoolScheduler);
        }

        /// <inheritdoc />
        public string GetDefaultRoamingCacheDirectory()
        {
            return "BlobCache";
        }

        /// <inheritdoc />
        public string GetDefaultSecretCacheDirectory()
        {
            return "SecretCache";
        }

        /// <inheritdoc />
        public string GetDefaultLocalMachineCacheDirectory()
        {
            return "LocalBlobCache";
        }
    }
}
