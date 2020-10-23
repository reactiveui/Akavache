// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Akavache.Sqlite3.Internal;
using Xunit;

namespace Akavache.Tests
{
    /// <summary>
    /// Tests the <see cref="AsyncLock"/> class.
    /// </summary>
    public class AsyncLockTests
    {
        /// <summary>
        /// Makes sure that the AsyncLock class handles cancellation correctly.
        /// </summary>
        [Fact]
        public void HandlesCancellation()
        {
            var asyncLock = new AsyncLock();
            var lockOne = asyncLock.LockAsync();

            var cts = new CancellationTokenSource();
            var lockTwo = asyncLock.LockAsync(cts.Token);

            Assert.True(lockOne.IsCompleted);
            Assert.Equal(TaskStatus.RanToCompletion, lockOne.Status);
            Assert.NotNull(lockOne.Result);

            Assert.False(lockTwo.IsCompleted);

            cts.Cancel();

            Assert.True(lockTwo.IsCompleted);
            Assert.True(lockTwo.IsCanceled);

            lockOne.Result.Dispose();
        }
    }
}
