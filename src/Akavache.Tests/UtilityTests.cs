// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Xunit;

namespace Akavache.Tests
{
    /// <summary>
    /// Tests associated with our utilities.
    /// </summary>
    public class UtilityTests
    {
        /// <summary>
        /// Tests to make sure that create directories work.
        /// </summary>
        [Fact]
        public void DirectoryCreateCreatesDirectories()
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var dir = new DirectoryInfo(Path.Combine(path, @"foo\bar\baz"));
                dir.CreateRecursive();
                Assert.True(dir.Exists);
            }
        }

        /// <summary>
        /// Gets to make sure we get exceptions on invalid network paths.
        /// </summary>
        [Fact]
        public void DirectoryCreateThrowsIOExceptionForNonexistentNetworkPaths()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var exception = Assert.Throws<IOException>(() => new DirectoryInfo(@"\\does\not\exist").CreateRecursive());
            Assert.StartsWith("The network path was not found", exception.Message);
        }

        /// <summary>
        /// Test to make sure we can split absolute paths.
        /// </summary>
        [Fact]
        public void UtilitySplitsAbsolutePaths()
        {
            string path;
            string expectedRoot;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = @"c:\foo\bar";
                expectedRoot = @"c:\";
            }
            else
            {
                path = "/foo/bar";
                expectedRoot = "/";
            }

            Assert.Equal(new[] { expectedRoot, "foo", "bar" }, new DirectoryInfo(path).SplitFullPath());
        }

        /// <summary>
        /// Tests to make sure we can resolve and split relative paths.
        /// </summary>
        [Fact]
        public void UtilityResolvesAndSplitsRelativePaths()
        {
            string path;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = @"foo\bar";
            }
            else
            {
                path = "foo/bar";
            }

            var components = new DirectoryInfo(path).SplitFullPath().ToList();
            Assert.True(components.Count > 2);
            Assert.Equal(new[] { "foo", "bar" }, components.Skip(components.Count - 2));
        }

        /// <summary>
        /// Tests to make sure we can split on UNC paths.
        /// </summary>
        [Fact]
        public void UtilitySplitsUncPaths()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            Assert.Equal(new[] { @"\\foo\bar", "baz" }, new DirectoryInfo(@"\\foo\bar\baz").SplitFullPath());
        }

        /// <summary>
        /// Test to make sure the operation queue shuts down.
        /// </summary>
        [Fact]
        public void KeyedOperationQueueCorrectlyShutsDown()
        {
            var fixture = new KeyedOperationQueue();
            var op1 = new Subject<int>();
            var op2 = new Subject<int>();
            var op3 = new Subject<int>();
            bool isCompleted = false;

            int op1Result = 0, op2Result = 0, op3Result = 0;

            fixture.EnqueueObservableOperation("foo", () => op1).Subscribe(x => op1Result = x);
            fixture.EnqueueObservableOperation("bar", () => op2).Subscribe(x => op2Result = x);

            // Shut down the queue, shouldn't be completed until op1 and op2 complete
            fixture.ShutdownQueue().Subscribe(_ => isCompleted = true);
            Assert.False(isCompleted);

            op1.OnNext(1);
            op1.OnCompleted();
            Assert.False(isCompleted);
            Assert.Equal(1, op1Result);

            op2.OnNext(2);
            op2.OnCompleted();
            Assert.True(isCompleted);
            Assert.Equal(2, op2Result);

            // We've already shut down, new ops should be ignored
            fixture.EnqueueObservableOperation("foo", () => op3).Subscribe(x => op3Result = x);
            op3.OnNext(3);
            op3.OnCompleted();
            Assert.Equal(0, op3Result);
        }
    }
}
