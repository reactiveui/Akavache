using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using Akavache;
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
        public void CacheShouldBeAbleToGetAndInsertBlobs()
        {
            string path;
            using (Utility.WithEmptyDirectory(out path))
            {
                (Scheduler.Immediate).With(sched =>
                {
                    var fixture = CreateBlobCache(path);

                    fixture.Insert("Foo", new byte[] {1, 2, 3});
                    fixture.Insert("Bar", new byte[] {4, 5, 6});

                    Assert.Throws<ArgumentNullException>(() =>
                        fixture.Insert(null, new byte[] {7, 8, 9}));

                    byte[] output1 = fixture.GetAsync("Foo").First();
                    byte[] output2 = fixture.GetAsync("Bar").First();

                    Assert.Throws<ArgumentNullException>(() =>
                        fixture.GetAsync(null).First());

                    Assert.Throws<KeyNotFoundException>(() =>
                        fixture.GetAsync("Baz").First());

                    Assert.Equal(3, output1.Length);
                    Assert.Equal(3, output2.Length);

                    Assert.Equal(1, output1[0]);
                    Assert.Equal(4, output2[0]);
                });
            }
        }

        [Fact]
        public void CacheShouldBeRoundtrippable()
        {
            string path;

            using(Utility.WithEmptyDirectory(out path))
            {
                using (var fixture = CreateBlobCache(path))
                {
                    fixture.Insert("Foo", new byte[] { 1, 2, 3 });
                }   

                using(var fixture = CreateBlobCache(path))
                {
                    var output = fixture.GetAsync("Foo").First();
                    Assert.Equal(3, output.Length);
                    Assert.Equal(1, output[0]);
                }
            }
        }

        [Fact]
        public void InsertingAnItemTwiceShouldAlwaysGetTheNewOne()
        {
            string path;

            using(Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                fixture.Insert("Foo", new byte[] { 1, 2, 3 });

                var output = fixture.GetAsync("Foo").First();
                Assert.Equal(3, output.Length);
                Assert.Equal(1, output[0]);

                fixture.Insert("Foo", new byte[] { 4, 5 });

                output = fixture.GetAsync("Foo").First();
                Assert.Equal(2, output.Length);
                Assert.Equal(4, output[0]);
            }   
        }

        [Fact]
        public void CacheShouldRespectExpiration()
        {
            string path;

            using(Utility.WithEmptyDirectory(out path))
            {
                (new TestScheduler()).With(sched =>
                {
                    using (var fixture = CreateBlobCache(path))
                    {
                        fixture.Insert("foo", new byte[] {1, 2, 3}, TimeSpan.FromTicks(100));
                        fixture.Insert("bar", new byte[] {4, 5, 6}, TimeSpan.FromTicks(500));

                        byte[] result = null;
                        fixture.GetAsync("foo").Subscribe(x => result = x);

                        // Foo should still be active
                        sched.AdvanceTo(50);
                        Assert.Equal(1, result[0]);

                        // From 100 < t < 500, foo should be inactive but bar should still work
                        bool shouldFail = true;
                        sched.AdvanceTo(120);
                        fixture.GetAsync("foo").Subscribe(
                            x => result = x,
                            ex => shouldFail = false);
                        fixture.GetAsync("bar").Subscribe(x => result = x);

                        sched.AdvanceTo(300);
                        Assert.False(shouldFail);
                        Assert.Equal(4, result[0]);
                    }

                    // Serialize out the cache and reify it again
                    using (var fixture = CreateBlobCache(path))
                    {
                        byte[] result = null;
                        fixture.GetAsync("bar").Subscribe(x => result = x);
                        sched.AdvanceTo(400);

                        Assert.Equal(4, result[0]);

                        // At t=1000, everything is invalidated
                        bool shouldFail = true;
                        sched.AdvanceTo(1000);
                        fixture.GetAsync("bar").Subscribe(
                            x => result = x,
                            ex => shouldFail = false);

                        sched.AdvanceTo(1010);
                        Assert.False(shouldFail);
                    }

                    sched.Start();
                });
            }
        }
    }

    public class TPersistentBlobCache : PersistentBlobCache
    {
        public TPersistentBlobCache(string cacheDirectory = null, IScheduler scheduler = null) : base(cacheDirectory, null, scheduler) { }
    }

    public class PersistentBlobCacheInterfaceFixture : BlobCacheInterfaceFixture
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new TPersistentBlobCache(path);
        }
    }

    public class EncryptedBlobCacheInterfaceFixture : BlobCacheInterfaceFixture
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new TEncryptedBlobCache(path);
        }

        public class TEncryptedBlobCache : EncryptedBlobCache
        {
            public TEncryptedBlobCache(string cacheDirectory = null, IScheduler scheduler = null) : base(cacheDirectory, null, scheduler) { }
        }
    }
}
