// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using Android.App;

namespace Akavache
{
    /// <summary>
    /// The file system provider that understands the android.
    /// </summary>
    public class AndroidFilesystemProvider : IFilesystemProvider
    {
        private readonly SimpleFilesystemProvider _inner = new SimpleFilesystemProvider();

        /// <inheritdoc />
        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
        {
            return _inner.OpenFileForReadAsync(path, scheduler);
        }

        /// <inheritdoc />
        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
        {
            return _inner.OpenFileForWriteAsync(path, scheduler);
        }

        /// <inheritdoc />
        public IObservable<Unit> CreateRecursive(string path)
        {
            return _inner.CreateRecursive(path);
        }

        /// <inheritdoc />
        public IObservable<Unit> Delete(string path)
        {
            return _inner.Delete(path);
        }

        /// <inheritdoc />
        public string? GetDefaultLocalMachineCacheDirectory()
        {
            return Application.Context.CacheDir?.AbsolutePath;
        }

        /// <inheritdoc />
        public string? GetDefaultRoamingCacheDirectory()
        {
            return Application.Context.FilesDir?.AbsolutePath;
        }

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
}
