using System.IO;
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
        }
    }
}
