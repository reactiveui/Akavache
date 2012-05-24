using System.IO;
using System.Linq;
using Xunit;

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
    }
}
