using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Xunit;

namespace Tests
{
    public class CacheDirectoryFailingTests
    {
        public CacheDirectoryFailingTests()
        {
            BlobCache.ApplicationName = "MyApp";
        }

        public class MyPersistentBlobCache : PersistentBlobCache
        {
            public MyPersistentBlobCache(string cacheDirectory = null, IScheduler scheduler = null) : base(cacheDirectory, null, scheduler) { }
        }

        static MyPersistentBlobCache SetupCache(string cacheDirectory)
        {
            BlobCache.EnsureInitialized();

            var fixture = new MyPersistentBlobCache(cacheDirectory);
            // ensuring we are working with a clear cache
            fixture.InvalidateAll();
            return fixture;
        }

        static string RoamingFolder()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "BlobCache");
        }

        static string LocalFolder()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "BlobCache");
        }

        class SomethingUseful { public int Id; }

        [Fact]
        public async Task GetOrFetchObject_UsingRoamingStore_ReturnsCreatedObject()
        {
            // failing
            var fixture = SetupCache(RoamingFolder());

            var existingValueNoExpiration = await fixture.GetOrFetchObject(
                 "KeyOne",
                 () => Observable.Return(new SomethingUseful { Id = 1 }));

            Assert.NotNull(existingValueNoExpiration);
            Assert.Equal(1, existingValueNoExpiration.Id);
        }

        [Fact]
        public async Task GetOrFetchObject_UsingLocalAccount_ReturnsCreatedObject()
        {
            // also doesn't work
            var fixture = SetupCache(LocalFolder());

            var existingValueNoExpiration = await fixture.GetOrFetchObject(
                 "KeyOne",
                 () => Observable.Return(new SomethingUseful { Id = 1 }));

            Assert.NotNull(existingValueNoExpiration);
            Assert.Equal(1, existingValueNoExpiration.Id);
        }
    }
}