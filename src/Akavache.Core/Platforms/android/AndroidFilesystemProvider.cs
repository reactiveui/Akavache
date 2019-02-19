// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using Akavache;
using Android.App;
using Android.Content;

namespace Akavache
{
    /// <summary>
    /// The file system provider that understands the android.
    /// </summary>
    public class AndroidFilesystemProvider : IFilesystemProvider
    {
        private readonly SimpleFilesystemProvider _inner = new SimpleFilesystemProvider();

        /// <summary>
        /// Opens a file for reading and provide a observable to the stream.
        /// </summary>
        /// <param name="path">The path to the file to open.</param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
        {
            return _inner.OpenFileForReadAsync(path, scheduler);
        }

        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
        {
            return _inner.OpenFileForWriteAsync(path, scheduler);
        }

        public IObservable<Unit> CreateRecursive(string path)
        {
            return _inner.CreateRecursive(path);
        }

        public IObservable<Unit> Delete(string path)
        {
            return _inner.Delete(path);
        }

        public string GetDefaultLocalMachineCacheDirectory()
        {
            return Application.Context.CacheDir.AbsolutePath;
        }

        public string GetDefaultRoamingCacheDirectory()
        {
            return Application.Context.FilesDir.AbsolutePath;
        }

        public string GetDefaultSecretCacheDirectory()
        {
            var path = Application.Context.FilesDir.AbsolutePath;
            var di = new DirectoryInfo(Path.Combine(path, "Secret"));
            if (!di.Exists) di.CreateRecursive();

            return di.FullName;
        }
    }
}

