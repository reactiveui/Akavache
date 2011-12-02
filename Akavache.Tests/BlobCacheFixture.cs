using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using Akavache;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;

namespace Akavache.Tests
{
    class TPersistentBlobCache : PersistentBlobCache
    {
        public TPersistentBlobCache(string cacheDirectory = null, IScheduler scheduler = null) : base(cacheDirectory, scheduler) { }
    }

    public class BlobCacheFixture
    {
        [Test]
        public void CacheShouldBeAbleToGetAndInsertBlobs()
        {
            (Scheduler.CurrentThread).With(sched =>
            {
                var fixture = new TPersistentBlobCache();

                fixture.Insert("Foo", new byte[] { 1, 2, 3 });
                fixture.Insert("Bar", new byte[] { 4, 5, 6 });

                Assert.Throws<ArgumentNullException>(() =>
                    fixture.Insert(null, new byte[] { 7, 8, 9 }));

                byte[] output1 = fixture.GetAsync("Foo").First();
                byte[] output2 = fixture.GetAsync("Bar").First();

                Assert.Throws<ArgumentNullException>(() =>
                    fixture.GetAsync(null).First());

                Assert.Throws<KeyNotFoundException>(() =>
                    fixture.GetAsync("Baz").First());

                Assert.AreEqual(3, output1.Length);
                Assert.AreEqual(3, output2.Length);

                Assert.AreEqual(1, output1[0]);
                Assert.AreEqual(4, output2[0]);
            });
        }

        [Test]
        public void CacheShouldBeRoundtrippable()
        {
            string path;

            using(Utility.WithEmptyDirectory(out path))
            {
                using (var fixture = new TPersistentBlobCache(path))
                {
                    fixture.Insert("Foo", new byte[] { 1, 2, 3 });
                }   

                using(var fixture = new TPersistentBlobCache(path))
                {
                    var output = fixture.GetAsync("Foo").First();
                    Assert.AreEqual(3, output.Length);
                    Assert.AreEqual(1, output[0]);
                }
            }
        }
    }
}
