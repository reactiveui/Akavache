using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akavache.Sqlite3.Internal;
using Xunit;

namespace Akavache.Tests
{
    public class AsyncLockTests
    {
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
