using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache.Tests;
using Xunit;

namespace Akavache.SqlServerCompact.Tests
{
    public class SqlServerCompactPersistentBlobCacheTests
    {
        [Fact]
        public async Task CanInsertAndGetSomeBytes()
        {
            string directory;
            using (Utility.WithTempDirectory(out directory))
            {
                var file = Path.Combine(directory, "database.sdf");
                using (var fixture = new SqlServerCompactPersistentBlobCache(file))
                {
                    await fixture.Insert("something", new byte[] {3, 4, 5});

                    var result = await fixture.Get("something");

                    Assert.Equal(3, result[0]);
                    Assert.Equal(4, result[1]);
                    Assert.Equal(5, result[2]);
                }
            }
        }
    }
}
