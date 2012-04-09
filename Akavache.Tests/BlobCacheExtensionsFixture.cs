using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Reactive.Testing;
using ReactiveUI.Testing;
using Xunit;

namespace Akavache.Tests
{
    public class UserObject
    {
        public string Bio { get; set; }
        public string Name { get; set; }
        public string Blog { get; set; }
    }

    public class BlobCacheExtensionsFixture
    {
        [Fact]
        public void DownloadUrlTest()
        {
            string path;

            using(Utility.WithEmptyDirectory(out path))
            using(var fixture = new TPersistentBlobCache(path))
            {
                var bytes = fixture.DownloadUrl(@"https://www.google.com/intl/en_com/images/srpr/logo3w.png").First();
                Assert.True(bytes.Length > 0);
            }
        }

        [Fact]
        public void ObjectsShouldBeRoundtrippable()
        {
            new TestScheduler().With(sched =>
            {
                string path;
                var input = new UserObject() {Bio = "A totally cool cat!", Name = "octocat", Blog = "http://www.github.com"};
                UserObject result;

                using (Utility.WithEmptyDirectory(out path))
                {
                    using (var fixture = new TPersistentBlobCache(path))
                    {
                        fixture.InsertObject("key", input);
                    }
                    sched.Start();
                    using (var fixture = new TPersistentBlobCache(path))
                    {
                        var action = fixture.GetObjectAsync<UserObject>("key");
                        sched.Start();
                        result = action.First();
                    }
                }

                Assert.Equal(input.Blog, result.Blog);
                Assert.Equal(input.Bio, result.Bio);
                Assert.Equal(input.Name, result.Name);
            });
        }

        [Fact]
        public void FetchFunctionShouldBeCalledOnceForGetOrFetchObject()
        {
            int fetchCount = 0;
            var fetcher = new Func<IObservable<Tuple<string, string>>>(() =>
            {
                fetchCount++;
                return Observable.Return(new Tuple<string, string>("Foo", "Bar"));
            });

            string path;
            using(Utility.WithEmptyDirectory(out path))
            {
                using(var fixture = new TPersistentBlobCache(path))
                {
                    var result = fixture.GetOrFetchObject("Test", fetcher).First();
                    Assert.Equal("Foo", result.Item1);
                    Assert.Equal("Bar", result.Item2);
                    Assert.Equal(1, fetchCount);

                    // 2nd time around, we should be grabbing from cache
                    result = fixture.GetOrFetchObject("Test", fetcher).First();
                    Assert.Equal("Foo", result.Item1);
                    Assert.Equal("Bar", result.Item2);
                    Assert.Equal(1, fetchCount);
                }

                using(var fixture = new TPersistentBlobCache(path))
                {
                    var result = fixture.GetOrFetchObject("Test", fetcher).First();
                    Assert.Equal("Foo", result.Item1);
                    Assert.Equal("Bar", result.Item2);
                    Assert.Equal(1, fetchCount);
                }
            }
        }
    }
}
