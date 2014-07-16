using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache.DataStore;
using Splat;
using TheFactory.FileSystem;
using Xunit;

namespace Akavache.Tests
{
    public class DataStoreTests
    {
        [Fact]
        public async Task CanSetAndGetBytes()
        {
            Locator.CurrentMutable.Register(() => new DesktopFileSystem(), typeof(IFileSystem));

            string directory;
            using (Utility.WithEmptyDirectory(out directory))
            {
                var database = Path.Combine(directory, "cache.db");
                using (var fixture = new DataStoreBlobCache(database))
                {
                    await fixture.Insert("something", new byte[] {2, 3, 4});

                    var result = await fixture.Get("something");

                    Assert.Equal(2, result[0]);
                    Assert.Equal(3, result[1]);
                    Assert.Equal(4, result[2]);
                }
            }
        }
    }
}
