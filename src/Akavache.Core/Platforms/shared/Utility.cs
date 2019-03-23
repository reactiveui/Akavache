// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using Splat;

#if WINDOWS_UWP
using System.Reactive.Windows.Foundation;
using Windows.Storage;
#endif

namespace Akavache
{
    [SuppressMessage("FxCop.Style", "CA5351: GetMd5Hash uses a broken cryptographic algorithm MD5", Justification = "Not used for encryption.")]
    internal static partial class Utility
    {
        public static string GetMd5Hash(string input)
        {
#if WINDOWS_UWP
            // NB: Technically, we could do this everywhere, but if we did this
            // upgrade, we may return different strings than we used to (i.e.
            // formatting-wise), which would break old caches.
            return MD5Core.GetHashString(input, Encoding.UTF8);
#else
            using (var md5Hasher = new MD5Managed())
            {
                // Convert the input string to a byte array and compute the hash.
                var data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sBuilder = new StringBuilder();
                foreach (var item in data)
                {
                    sBuilder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                }

                return sBuilder.ToString();
            }
#endif
        }

#if !WINDOWS_UWP
        public static IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share, IScheduler scheduler = null)
        {
            scheduler = scheduler ?? BlobCache.TaskpoolScheduler;
            var ret = new AsyncSubject<Stream>();

            Observable.Start(
                () =>
                {
                    try
                    {
                        var createModes = new[]
                        {
                            FileMode.Create,
                            FileMode.CreateNew,
                            FileMode.OpenOrCreate,
                        };

                        // NB: We do this (even though it's incorrect!) because
                        // throwing lots of 1st chance exceptions makes debugging
                        // obnoxious, as well as a bug in VS where it detects
                        // exceptions caught by Observable.Start as Unhandled.
                        if (!createModes.Contains(mode) && !File.Exists(path))
                        {
                            ret.OnError(new FileNotFoundException());
                            return;
                        }

                        Observable.Start(() => new FileStream(path, mode, access, share, 4096, false), scheduler).Cast<Stream>().Subscribe(ret);
                    }
                    catch (Exception ex)
                    {
                        ret.OnError(ex);
                    }
                }, scheduler);

            return ret;
        }

        public static void CreateRecursive(this DirectoryInfo directoryInfo)
        {
            directoryInfo.SplitFullPath().Aggregate((parent, dir) =>
            {
                var path = Path.Combine(parent, dir);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            });
        }

        public static IEnumerable<string> SplitFullPath(this DirectoryInfo directoryInfo)
        {
            var root = Path.GetPathRoot(directoryInfo.FullName);
            var components = new List<string>();
            for (var path = directoryInfo.FullName; path != root && path != null; path = Path.GetDirectoryName(path))
            {
                var filename = Path.GetFileName(path);
                if (string.IsNullOrEmpty(filename))
                {
                    continue;
                }

                components.Add(filename);
            }

            components.Add(root);
            components.Reverse();
            return components;
        }
#endif

        /// <summary>
        /// Logs the errors of the observable.
        /// </summary>
        /// <typeparam name="T">The type of observable member.</typeparam>
        /// <param name="observable">The observable.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>An observable.</returns>
        public static IObservable<T> LogErrors<T>(this IObservable<T> observable, string message = null)
        {
            return Observable.Create<T>(subj =>
            {
                return observable.Subscribe(
                    subj.OnNext,
                    ex =>
                    {
                        var msg = message ?? "0x" + observable.GetHashCode().ToString("x", CultureInfo.InvariantCulture);
                        LogHost.Default.Info(ex, "{0} failed", msg);
                        subj.OnError(ex);
                    }, subj.OnCompleted);
            });
        }

#if WINDOWS_UWP
        /// <summary>
        /// Copies a stream using async.
        /// </summary>
        /// <param name="stream">The stream to copy from.</param>
        /// <param name="destination">The stream to copy to.</param>
        /// <returns>An observable that signals when the operation has finished.</returns>
        public static IObservable<Unit> CopyToAsync(this Stream stream, Stream destination)
#else
        /// <summary>
        /// Copies a stream using async.
        /// </summary>
        /// <param name="stream">The stream to copy from.</param>
        /// <param name="destination">The stream to copy to.</param>
        /// <param name="scheduler">The scheduler to schedule on.</param>
        /// <returns>An observable that signals when the operation has finished.</returns>
        public static IObservable<Unit> CopyToAsync(this Stream stream, Stream destination, IScheduler scheduler = null)
#endif
        {
#if WINDOWS_UWP
            return stream.CopyToAsync(destination).ToObservable()
                .Do(
                x =>
                {
                    try
                    {
                        stream.Dispose();
                        destination.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogHost.Default.Warn(ex, "CopyToAsync failed");
                    }
                });
#else
            return Observable.Start(
                () =>
                {
                    try
                    {
                        stream.CopyTo(destination);
                    }
                    catch (Exception ex)
                    {
                        LogHost.Default.Warn(ex, "CopyToAsync failed");
                    }
                    finally
                    {
                        stream.Dispose();
                        destination.Dispose();
                    }
                }, scheduler ?? BlobCache.TaskpoolScheduler);
#endif
        }
    }
}
