using System;
using System.IO;
using System.Linq;
using Xunit;
using Akavache;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace Akavache.Tests
{
    public class UtilityTests
    {
        public class TheCreateRecursiveMethod
        {
            [Fact]
            public void CreatesDirectories()
            {
                string path;
                using (Utility.WithEmptyDirectory(out path))
                {
                    var dir = new DirectoryInfo(Path.Combine(path, @"foo\bar\baz"));
                    dir.CreateRecursive();
                    Assert.True(dir.Exists);
                }
            }

            [Fact]
            public void ThrowsIOExceptionForNonexistentNetworkPaths()
            {
                var exception = Assert.Throws<IOException>(() => new DirectoryInfo(@"\\does\not\exist").CreateRecursive());
                Assert.Equal("The network path was not found.\r\n", exception.Message);
            }
        }

        public class TheSplitFullPathMethod
        {
            [Fact]
            public void SplitsAbsolutePaths()
            {
                Assert.Equal(new[] {@"c:\", "foo", "bar"}, new DirectoryInfo(@"c:\foo\bar").SplitFullPath());
            }

            [Fact]
            public void ResolvesAndSplitsRelativePaths()
            {
                var components = new DirectoryInfo(@"foo\bar").SplitFullPath().ToList();
                Assert.True(components.Count > 2);
                Assert.Equal(new[] {"foo", "bar"}, components.Skip(components.Count - 2));
            }

            [Fact]
            public void SplitsUncPaths()
            {
                Assert.Equal(new[] {@"\\foo\bar", "baz"}, new DirectoryInfo(@"\\foo\bar\baz").SplitFullPath());
            }
        }

        public class TheKeyedOperationQueue
        {
            [Fact]
            public void CorrectlyShutsDown()
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

                op1.OnNext(1); op1.OnCompleted();
                Assert.False(isCompleted);
                Assert.Equal(1, op1Result);

                op2.OnNext(2); op2.OnCompleted();
                Assert.True(isCompleted);
                Assert.Equal(2, op2Result);

                // We've already shut down, new ops should be ignored
                fixture.EnqueueObservableOperation("foo", () => op3).Subscribe(x => op3Result = x);
                op3.OnNext(3);  op3.OnCompleted();
                Assert.Equal(0, op3Result);
            }
        }
    }
}