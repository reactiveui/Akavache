using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akavache.Sqlite3;
using Microsoft.Reactive.Testing;
using ReactiveUI;
using ReactiveUI.Testing;
using Xunit;

namespace Akavache.Tests
{
    public abstract class BlobCacheInterfaceFixture
    {
        protected abstract IBlobCache CreateBlobCache(string path);

        [Fact]
        public async Task CacheShouldBeAbleToGetAndInsertBlobs()
        {
            string path;
            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path)) 
            {
                await fixture.Insert("Foo", new byte[] { 1, 2, 3 });
                await fixture.Insert("Bar", new byte[] { 4, 5, 6 });

                Assert.Throws<ArgumentNullException>(() =>
                    fixture.Insert(null, new byte[] { 7, 8, 9 }).First());

                byte[] output1 = await fixture.Get("Foo");
                byte[] output2 = await fixture.Get("Bar");

                Assert.Throws<ArgumentNullException>(() =>
                    fixture.Get(null).First());

                Assert.Throws<KeyNotFoundException>(() =>
                    fixture.Get("Baz").First());

                Assert.Equal(3, output1.Length);
                Assert.Equal(3, output2.Length);

                Assert.Equal(1, output1[0]);
                Assert.Equal(4, output2[0]);
            }
        }

        [Fact]
        public void CacheShouldBeRoundtrippable()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
            {
                var fixture = CreateBlobCache(path);
                using (fixture)
                {
                    // InMemoryBlobCache isn't round-trippable by design
                    if (fixture is InMemoryBlobCache) return;

                    fixture.Insert("Foo", new byte[] {1, 2, 3});
                }

                fixture.Shutdown.Wait();

                using (var fixture2 = CreateBlobCache(path))
                {
                    var output = fixture2.Get("Foo").First();
                    Assert.Equal(3, output.Length);
                    Assert.Equal(1, output[0]);
                }
            }
        }

        public void CreatedAtShouldBeSetAutomaticallyAndBeRetrievable()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
            {
                var fixture = CreateBlobCache(path);
                DateTimeOffset roughCreationTime;
                using (fixture)
                {
                    fixture.Insert("Foo", new byte[] { 1, 2, 3 }).Wait();
                    roughCreationTime = fixture.Scheduler.Now;
                }

                fixture.Shutdown.Wait();

                using (var fixture2 = CreateBlobCache(path))
                {
                    var createdAt = fixture2.GetCreatedAt("Foo").Wait();

                    Assert.InRange(
                        actual: createdAt.Value,
                        low: roughCreationTime - TimeSpan.FromSeconds(1),
                        high: roughCreationTime);
                }
            }
        }

        [Fact]
        public void InsertingAnItemTwiceShouldAlwaysGetTheNewOne()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                fixture.Insert("Foo", new byte[] { 1, 2, 3 }).Wait();

                var output = fixture.Get("Foo").First();
                Assert.Equal(3, output.Length);
                Assert.Equal(1, output[0]);

                fixture.Insert("Foo", new byte[] { 4, 5 }).Wait();

                output = fixture.Get("Foo").First();
                Assert.Equal(2, output.Length);
                Assert.Equal(4, output[0]);
            }
        }

        [Fact(Skip = "TestScheduler tests aren't gonna work with new SQLite")]
        public void CacheShouldRespectExpiration()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
            {
                (new TestScheduler()).With(sched =>
                {
                    bool wasTestCache;

                    using (var fixture = CreateBlobCache(path))
                    {
                        wasTestCache = fixture is InMemoryBlobCache;
                        fixture.Insert("foo", new byte[] { 1, 2, 3 }, TimeSpan.FromMilliseconds(100));
                        fixture.Insert("bar", new byte[] { 4, 5, 6 }, TimeSpan.FromMilliseconds(500));

                        byte[] result = null;
                        sched.AdvanceToMs(20);
                        fixture.Get("foo").Subscribe(x => result = x);

                        // Foo should still be active
                        sched.AdvanceToMs(50);
                        Assert.Equal(1, result[0]);

                        // From 100 < t < 500, foo should be inactive but bar should still work
                        bool shouldFail = true;
                        sched.AdvanceToMs(120);
                        fixture.Get("foo").Subscribe(
                            x => result = x,
                            ex => shouldFail = false);
                        fixture.Get("bar").Subscribe(x => result = x);

                        sched.AdvanceToMs(300);
                        Assert.False(shouldFail);
                        Assert.Equal(4, result[0]);
                    }

                    // NB: InMemoryBlobCache is not serializable by design
                    if (wasTestCache) return;

                    sched.AdvanceToMs(350);
                    sched.AdvanceToMs(351);
                    sched.AdvanceToMs(352);

                    // Serialize out the cache and reify it again
                    using (var fixture = CreateBlobCache(path))
                    {
                        byte[] result = null;
                        fixture.Get("bar").Subscribe(x => result = x);
                        sched.AdvanceToMs(400);

                        Assert.Equal(4, result[0]);

                        // At t=1000, everything is invalidated
                        bool shouldFail = true;
                        sched.AdvanceToMs(1000);
                        fixture.Get("bar").Subscribe(
                            x => result = x,
                            ex => shouldFail = false);

                        sched.AdvanceToMs(1010);
                        Assert.False(shouldFail);
                    }

                    sched.Start();
                });
            }
        }

        [Fact]
        public void InvalidateAllReallyDoesInvalidateEverything()
        {
            string path;
            using (Utility.WithEmptyDirectory(out path)) 
            {
                using (var fixture = CreateBlobCache(path)) 
                {
                    fixture.Insert("Foo", new byte[] { 1, 2, 3 }).First();
                    fixture.Insert("Bar", new byte[] { 4, 5, 6 }).First();
                    fixture.Insert("Bamf", new byte[] { 7, 8, 9 }).First();

                    Assert.NotEqual(0, fixture.GetAllKeys().First().Count());

                    fixture.InvalidateAll().First();

                    Assert.Equal(0, fixture.GetAllKeys().First().Count());
                }

                using (var fixture = CreateBlobCache(path)) 
                {
                    Assert.Equal(0, fixture.GetAllKeys().First().Count());
                }
            }
        }

        [Fact]
        public void GetAllKeysShouldntReturnExpiredKeys()
        {
            string path;
            using (Utility.WithEmptyDirectory(out path)) 
            {
                using (var fixture = CreateBlobCache(path)) 
                {
                    var inThePast = BlobCache.TaskpoolScheduler.Now - TimeSpan.FromDays(1.0);

                    fixture.Insert("Foo", new byte[] { 1, 2, 3 }, inThePast).First();
                    fixture.Insert("Bar", new byte[] { 4, 5, 6 }, inThePast).First();
                    fixture.Insert("Bamf", new byte[] { 7, 8, 9 }).First();

                    Assert.Equal(1, fixture.GetAllKeys().First().Count());
                }

                using (var fixture = CreateBlobCache(path)) 
                {
                    if (fixture is InMemoryBlobCache) return;
                    Assert.Equal(1, fixture.GetAllKeys().First().Count());
                }
            }
        }

        [Fact]
        public void VacuumDoesntPurgeKeysThatShouldBeThere()
        {
            string path;
            using (Utility.WithEmptyDirectory(out path)) 
            {
                using (var fixture = CreateBlobCache(path)) 
                {
                    var inThePast = BlobCache.TaskpoolScheduler.Now - TimeSpan.FromDays(1.0);

                    fixture.Insert("Foo", new byte[] { 1, 2, 3 }, inThePast).First();
                    fixture.Insert("Bar", new byte[] { 4, 5, 6 }, inThePast).First();
                    fixture.Insert("Bamf", new byte[] { 7, 8, 9 }).First();

                    try 
                    {
                        fixture.Vacuum().First();
                    } 
                    catch (NotImplementedException) 
                    {
                        // NB: The old and busted cache will never have this, 
                        // just make the test pass
                    }

                    Assert.Equal(1, fixture.GetAllKeys().First().Count());
                }

                using (var fixture = CreateBlobCache(path)) 
                {
                    if (fixture is InMemoryBlobCache) return;
                    Assert.Equal(1, fixture.GetAllKeys().First().Count());
                }
            }
        }

        [Fact]
        public void DateTimeKindCanBeForced()
        {
            var before = BlobCache.ForcedDateTimeKind;
            BlobCache.ForcedDateTimeKind = DateTimeKind.Utc;

            try
            {
                string path;

                using (Utility.WithEmptyDirectory(out path))
                using (var fixture = CreateBlobCache(path))
                {
                    var value = DateTime.UtcNow;
                    fixture.InsertObject("key", value).First();
                    var result = fixture.GetObject<DateTime>("key").First();
                    Assert.Equal(DateTimeKind.Utc, result.Kind);
                }
            }
            finally
            {
                BlobCache.ForcedDateTimeKind = before;
            }
        }
    }

    public class InMemoryBlobCacheInterfaceFixture : BlobCacheInterfaceFixture
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new InMemoryBlobCache(RxApp.TaskpoolScheduler);
        }
    }

    public class SqliteBlobCacheInterfaceFixture : BlobCacheInterfaceFixture
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new SQLitePersistentBlobCache(Path.Combine(path, "sqlite.db"));
        }
    }
}