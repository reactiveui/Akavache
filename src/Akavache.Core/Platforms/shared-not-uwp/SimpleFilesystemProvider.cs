// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using Splat;

namespace Akavache
{
    /// <summary>
    /// A wrapper around the file system.
    /// </summary>
    public class SimpleFilesystemProvider : IFilesystemProvider
    {
        /// <inheritdoc />
        public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
        {
            return Utility.SafeOpenFileAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read, scheduler);
        }

        /// <inheritdoc />
        public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
        {
            return Utility.SafeOpenFileAsync(path, FileMode.Create, FileAccess.Write, FileShare.None, scheduler);
        }

        /// <inheritdoc />
        public IObservable<Unit> CreateRecursive(string path)
        {
            Utility.CreateRecursive(new DirectoryInfo(path));
            return Observable.Return(Unit.Default);
        }

        /// <inheritdoc />
        public IObservable<Unit> Delete(string path)
        {
            return Observable.Start(() => File.Delete(path), BlobCache.TaskpoolScheduler);
        }

        /// <inheritdoc />
        public string GetDefaultRoamingCacheDirectory()
        {
            return ModeDetector.InUnitTestRunner() ?
                Path.Combine(GetAssemblyDirectoryName(), "BlobCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BlobCache.ApplicationName, "BlobCache");
        }

        /// <inheritdoc />
        public string GetDefaultSecretCacheDirectory()
        {
            return ModeDetector.InUnitTestRunner() ?
                Path.Combine(GetAssemblyDirectoryName(), "SecretCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BlobCache.ApplicationName, "SecretCache");
        }

        /// <inheritdoc />
        public string GetDefaultLocalMachineCacheDirectory()
        {
            return ModeDetector.InUnitTestRunner() ?
                Path.Combine(GetAssemblyDirectoryName(), "LocalBlobCache") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), BlobCache.ApplicationName, "BlobCache");
        }

        /// <summary>
        /// Gets the assembly directory name.
        /// </summary>
        /// <returns>The assembly directory name.</returns>
        protected static string GetAssemblyDirectoryName()
        {
            var assemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.Assert(assemblyDirectoryName != null, "The directory name of the assembly location is null");
            return assemblyDirectoryName;
        }
    }
}
