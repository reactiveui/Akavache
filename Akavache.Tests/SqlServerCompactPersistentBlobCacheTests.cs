using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache.SqlServerCompact;
using Akavache.Tests;
using Xunit;

namespace Akavache.Tests
{
    public class SqlServerCompactPersistentBlobCacheTests : IDisposable
    {
        IDisposable directoryToDispose;
        string databaseFile;

        public SqlServerCompactPersistentBlobCacheTests()
        {
            string directory;
            directoryToDispose = Utility.WithTempDirectory(out directory);

            databaseFile = Path.Combine(directory, "database.sdf");
        }

        [Fact]
        public async Task CanInsertAndGetSomeBytes()
        {
            using (var fixture = new SqlServerCompactPersistentBlobCache(databaseFile))
            {
                await fixture.Insert("something", new byte[] { 3, 4, 5 });

                var result = await fixture.Get("something");

                Assert.Equal(3, result[0]);
                Assert.Equal(4, result[1]);
                Assert.Equal(5, result[2]);
            }
        }

        [Fact]
        public async Task CanInsertManyItems()
        {
            using (var fixture = new SqlServerCompactPersistentBlobCache(databaseFile))
            {
                var items = new Dictionary<string, byte[]>
                {
                    {"first", new byte[] {1, 2, 3}},
                    {"second", new byte[] {4, 5, 6}},
                    {"third", new byte[] {7, 8, 9}}
                };

                await fixture.Insert(items);

                var result = await fixture.Get(new[] { "first", "second", "third" });

                Assert.Equal(1, result["first"][0]);
                Assert.Equal(4, result["second"][0]);
                Assert.Equal(7, result["third"][0]);
            }
        }

        [Fact]
        public async Task CanInsertAPlainObject()
        {
            using (var fixture = new SqlServerCompactPersistentBlobCache(databaseFile))
            {
                var item = new SomeObject { HotGuid = Guid.NewGuid() };
                await fixture.InsertObject("some-key", item);

                var result = await fixture.GetObject<SomeObject>("some-key");

                Assert.Equal(item.HotGuid, result.HotGuid);
            }
        }

        public class SomeObject
        {
            public Guid HotGuid { get; set; }
        }

        public void Dispose()
        {
            directoryToDispose.Dispose();
        }
    }
}
