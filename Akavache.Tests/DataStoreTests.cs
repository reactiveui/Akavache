using System.IO;
using Akavache.DataStore;
using Splat;
using TheFactory.FileSystem;
using Xunit;

namespace Akavache.Tests
{
    public class DataStoreTests
    {
        [Fact]
        public void CanSetAndGetBytes()
        {
            Locator.CurrentMutable.Register(() => new DesktopFileSystem(), typeof(IFileSystem));

            string directory;
            using (Utility.WithEmptyDirectory(out directory))
            {
                var database = Path.Combine(directory, "cache.db");
                using (var fixture = new DataStoreBlobCache(database))
                {
                    Assert.True(false);
                }
            }
        }
    }
}
