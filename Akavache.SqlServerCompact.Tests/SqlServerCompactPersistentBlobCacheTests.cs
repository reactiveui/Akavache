using System.IO;
using Akavache.Tests;
using Xunit;

namespace Akavache.SqlServerCompact.Tests
{
    public class SqlServerCompactPersistentBlobCacheTests
    {
        [Fact]
        public void FailTheThing()
        {
            string directory;
            using (Utility.WithEmptyDirectory(out directory))
            {
                var file = Path.Combine(directory, "database.sdf");
                using (var fixture = new SqlServerCompactPersistentBlobCache(file))
                {
                    Assert.True(false);
                }
            }
        }
    }
}
