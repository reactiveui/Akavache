using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.Reactive.Testing;
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

                    fixture.Insert("Foo", new byte[] { 1, 2, 3 });
                    fixture.Insert("Bar", new byte[] { 4, 5, 6 });

                    Assert.Throws<ArgumentNullException>(() =>
                        fixture.Insert(null, new byte[] { 7, 8, 9 }).First());

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
            new TestScheduler().With(sched =>
            {
                string path;

                using (Utility.WithEmptyDirectory(out path))
                {
                    using (var fixture = CreateBlobCache(path))
                    {
                        fixture.Insert("Foo", new byte[] {1, 2, 3});
                    }
                    sched.Start();
                    using (var fixture = CreateBlobCache(path))
                    {
                        var action = fixture.GetAsync("Foo");
                        sched.Start();
                        var output = action.First();
                        Assert.Equal(3, output.Length);
                        Assert.Equal(1, output[0]);
                    }
                }
            });
        }

        [Fact]
        public void InsertingAnItemTwiceShouldAlwaysGetTheNewOne()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
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

            using (Utility.WithEmptyDirectory(out path))
            {
                (new TestScheduler()).With(sched =>
                {
                    using (var fixture = CreateBlobCache(path))
                    {
                        fixture.Insert("foo", new byte[] { 1, 2, 3 }, TimeSpan.FromMilliseconds(100));
                        fixture.Insert("bar", new byte[] { 4, 5, 6 }, TimeSpan.FromMilliseconds(500));

                        byte[] result = null;
                        sched.AdvanceToMs(20);
                        fixture.GetAsync("foo").Subscribe(x => result = x);

                        // Foo should still be active
                        sched.AdvanceToMs(50);
                        Assert.Equal(1, result[0]);

                        // From 100 < t < 500, foo should be inactive but bar should still work
                        bool shouldFail = true;
                        sched.AdvanceToMs(120);
                        fixture.GetAsync("foo").Subscribe(
                            x => result = x,
                            ex => shouldFail = false);
                        fixture.GetAsync("bar").Subscribe(x => result = x);

                        sched.AdvanceToMs(300);
                        Assert.False(shouldFail);
                        Assert.Equal(4, result[0]);
                    }

                    // Serialize out the cache and reify it again
                    using (var fixture = CreateBlobCache(path))
                    {
                        byte[] result = null;
                        fixture.GetAsync("bar").Subscribe(x => result = x);
                        sched.AdvanceToMs(400);

                        Assert.Equal(4, result[0]);

                        // At t=1000, everything is invalidated
                        bool shouldFail = true;
                        sched.AdvanceToMs(1000);
                        fixture.GetAsync("bar").Subscribe(
                            x => result = x,
                            ex => shouldFail = false);

                        sched.AdvanceToMs(1010);
                        Assert.False(shouldFail);
                    }

                    sched.Start();
                });
            }
        }

        [Fact(Skip = "Put off this test until later, it's fairly evil")]
        public void AbuseTheCacheOnATonOfThreads()
        {
            var rng = new Random();
            var keys = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid().ToString()).ToArray();

            var actions = Enumerable.Range(0, 1000)
                .Select(_ => new { AddOrDelete = rng.Next() % 2 == 0, Key = keys[rng.Next(0, keys.Length - 1)], Val = Guid.NewGuid().ToByteArray() })
                .ToArray();

            var exList = new List<Exception>();

            string path;
            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                var threads = Enumerable.Range(0, 10).Select(__ => new Thread(() =>
                {
                    var prng = new Random();
                    int start = prng.Next(0, actions.Length);

                    try
                    {
                        for (int i = start; i < start + actions.Length; i++)
                        {
                            var item = actions[i % actions.Length];
                            if (prng.Next() % 2 == 0)
                            {
                                fixture.GetAsync(item.Key)
                                    .Catch<byte[], KeyNotFoundException>(_ => Observable.Return(new byte[0]))
                                    .Subscribe(_ => { }, ex => { lock (exList) { exList.Add(ex); } });
                                continue;
                            }

                            if (item.AddOrDelete)
                            {
                                fixture.Insert(item.Key, item.Val);
                            }
                            else
                            {
                                fixture.Invalidate(item.Key);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exList) { exList.Add(ex); }
                    }
                })).ToArray();

                foreach (var t in threads) { t.Start(); }
                foreach (var t in threads) { t.Join(); }

                Thread.Sleep(10 * 1000);
            }

            Assert.Equal(0, exList.Count);
        }

        [Fact]
        public void DisposedCacheThrowsObjectDisposedException()
        {
            var cache = CreateBlobCache("somepath");
            cache.Dispose();

            Assert.Throws<ObjectDisposedException>(() => cache.Insert("key", new byte[] { }).First());
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
