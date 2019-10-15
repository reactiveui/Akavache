// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using ReactiveUI.Testing;
using Xunit;

namespace Akavache.Tests
{
    /// <summary>
    /// A fixture for testing the blob cache interfaces.
    /// </summary>
    public abstract class BlobCacheInterfaceTestBase
    {
        /// <summary>
        /// Tests that the cache can get or insert blobs.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task CacheShouldBeAbleToGetAndInsertBlobs()
        {
            string path;
            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                await fixture.Insert("Foo", new byte[] { 1, 2, 3 });
                await fixture.Insert("Bar", new byte[] { 4, 5, 6 });

                await Assert.ThrowsAsync<ArgumentNullException>(async () => await fixture.Insert(null, new byte[] { 7, 8, 9 }).FirstAsync()).ConfigureAwait(false);

                byte[] output1 = await fixture.Get("Foo");
                byte[] output2 = await fixture.Get("Bar");

                await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                    await fixture.Get(null).FirstAsync()).ConfigureAwait(false);

                await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                    await fixture.Get("Baz").FirstAsync()).ConfigureAwait(false);

                Assert.Equal(3, output1.Length);
                Assert.Equal(3, output2.Length);

                Assert.Equal(1, output1[0]);
                Assert.Equal(4, output2[0]);
            }
        }

        /// <summary>
        /// Tests to make sure that cache's can be written then read.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task CacheShouldBeRoundtrippable()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
            {
                var fixture = CreateBlobCache(path);
                using (fixture)
                {
                    // InMemoryBlobCache isn't round-trippable by design
                    if (fixture is InMemoryBlobCache)
                    {
                        return;
                    }

                    await fixture.Insert("Foo", new byte[] { 1, 2, 3 });
                }

                fixture.Shutdown.Wait();

                using (var fixture2 = CreateBlobCache(path))
                {
                    var output = await fixture2.Get("Foo").FirstAsync();
                    Assert.Equal(3, output.Length);
                    Assert.Equal(1, output[0]);
                }
            }
        }

        /// <summary>
        /// Checks to make sure that the property CreatedAt is populated and can be retrieved.
        /// </summary>
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

        /// <summary>
        /// Tests to make sure that inserting an item twice only allows getting of the first item.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task InsertingAnItemTwiceShouldAlwaysGetTheNewOne()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
            using (var fixture = CreateBlobCache(path))
            {
                fixture.Insert("Foo", new byte[] { 1, 2, 3 }).Wait();

                var output = await fixture.Get("Foo").FirstAsync();
                Assert.Equal(3, output.Length);
                Assert.Equal(1, output[0]);

                fixture.Insert("Foo", new byte[] { 4, 5 }).Wait();

                output = await fixture.Get("Foo").FirstAsync();
                Assert.Equal(2, output.Length);
                Assert.Equal(4, output[0]);
            }
        }

        /// <summary>
        /// Checks to make sure that the cache respects expiration dates.
        /// </summary>
        [Fact(Skip = "TestScheduler tests aren't gonna work with new SQLite")]
        public void CacheShouldRespectExpiration()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
            {
                new TestScheduler().With(sched =>
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
                    if (wasTestCache)
                    {
                        return;
                    }

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

        /// <summary>
        /// Tests to make sure that InvalidateAll invalidates everything.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task InvalidateAllReallyDoesInvalidateEverything()
        {
            string path;
            using (Utility.WithEmptyDirectory(out path))
            {
                using (var fixture = CreateBlobCache(path))
                {
                    await fixture.Insert("Foo", new byte[] { 1, 2, 3 }).FirstAsync();
                    await fixture.Insert("Bar", new byte[] { 4, 5, 6 }).FirstAsync();
                    await fixture.Insert("Bamf", new byte[] { 7, 8, 9 }).FirstAsync();

                    Assert.NotEqual(0, (await fixture.GetAllKeys().FirstAsync()).Count());

                    await fixture.InvalidateAll().FirstAsync();

                    Assert.Equal(0, (await fixture.GetAllKeys().FirstAsync()).Count());
                }

                using (var fixture = CreateBlobCache(path))
                {
                    Assert.Equal(0, (await fixture.GetAllKeys().FirstAsync()).Count());
                }
            }
        }

        /// <summary>
        /// Tests to make sure that GetsAllKeys does not return expired keys.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task GetAllKeysShouldntReturnExpiredKeys()
        {
            string path;
            using (Utility.WithEmptyDirectory(out path))
            {
                using (var fixture = CreateBlobCache(path))
                {
                    var inThePast = BlobCache.TaskpoolScheduler.Now - TimeSpan.FromDays(1.0);

                    await fixture.Insert("Foo", new byte[] { 1, 2, 3 }, inThePast).FirstAsync();
                    await fixture.Insert("Bar", new byte[] { 4, 5, 6 }, inThePast).FirstAsync();
                    await fixture.Insert("Bamf", new byte[] { 7, 8, 9 }).FirstAsync();

                    Assert.Equal(1, (await fixture.GetAllKeys().FirstAsync()).Count());
                }

                using (var fixture = CreateBlobCache(path))
                {
                    if (fixture is InMemoryBlobCache)
                    {
                        return;
                    }

                    Assert.Equal(1, (await fixture.GetAllKeys().FirstAsync()).Count());
                }
            }
        }

        /// <summary>
        /// Make sure that the Vacuum method does not purge keys that should be there.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task VacuumDoesntPurgeKeysThatShouldBeThere()
        {
            string path;
            using (Utility.WithEmptyDirectory(out path))
            {
                using (var fixture = CreateBlobCache(path))
                {
                    var inThePast = BlobCache.TaskpoolScheduler.Now - TimeSpan.FromDays(1.0);
                    var inTheFuture = BlobCache.TaskpoolScheduler.Now + TimeSpan.FromDays(1.0);

                    await fixture.Insert("Foo", new byte[] { 1, 2, 3 }, inThePast).FirstAsync();
                    await fixture.Insert("Bar", new byte[] { 4, 5, 6 }, inThePast).FirstAsync();
                    await fixture.Insert("Bamf", new byte[] { 7, 8, 9 }).FirstAsync();
                    await fixture.Insert("Baz", new byte[] { 7, 8, 9 }, inTheFuture).FirstAsync();

                    try
                    {
                        await fixture.Vacuum().FirstAsync();
                    }
                    catch (NotImplementedException)
                    {
                        // NB: The old and busted cache will never have this,
                        // just make the test pass
                    }

                    Assert.Equal(2, (await fixture.GetAllKeys().FirstAsync()).Count());
                }

                using (var fixture = CreateBlobCache(path))
                {
                    if (fixture is InMemoryBlobCache)
                    {
                        return;
                    }

                    Assert.Equal(2, (await fixture.GetAllKeys().FirstAsync()).Count());
                }
            }
        }

        /// <summary>
        /// Make sure that the Vacuum method purges entries that are expired.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task VacuumPurgeEntriesThatAreExpired()
        {
            string path;
            using (Utility.WithEmptyDirectory(out path))
            {
                using (var fixture = CreateBlobCache(path))
                {
                    var inThePast = BlobCache.TaskpoolScheduler.Now - TimeSpan.FromDays(1.0);
                    var inTheFuture = BlobCache.TaskpoolScheduler.Now + TimeSpan.FromDays(1.0);

                    await fixture.Insert("Foo", new byte[] { 1, 2, 3 }, inThePast).FirstAsync();
                    await fixture.Insert("Bar", new byte[] { 4, 5, 6 }, inThePast).FirstAsync();
                    await fixture.Insert("Bamf", new byte[] { 7, 8, 9 }).FirstAsync();
                    await fixture.Insert("Baz", new byte[] { 7, 8, 9 }, inTheFuture).FirstAsync();

                    try
                    {
                        await fixture.Vacuum().FirstAsync();
                    }
                    catch (NotImplementedException)
                    {
                        // NB: The old and busted cache will never have this,
                        // just make the test pass
                    }

                    await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.Get("Foo").FirstAsync().ToTask()).ConfigureAwait(false);
                    await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.Get("Bar").FirstAsync().ToTask()).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IBlobCache"/> we want to do the tests against.
        /// </summary>
        /// <param name="path">The path to the blob cache.</param>
        /// <returns>The blob cache for testing.</returns>
        protected abstract IBlobCache CreateBlobCache(string path);
    }
}
