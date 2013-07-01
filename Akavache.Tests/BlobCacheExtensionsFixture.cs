using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using Akavache.Sqlite3;
using Microsoft.Reactive.Testing;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Xaml;
using ReactiveUI.Testing;
using Xunit;
using System.Threading;

namespace Akavache.Tests
{
    public class UserObject
    {
        public string Bio { get; set; }
        public string Name { get; set; }
        public string Blog { get; set; }
    }

    public class UserModel
    {
        public UserModel(UserObject user)
        {
        }

        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class ServiceProvider : IServiceProvider
    {
        public object GetService(Type t)
        {
            if (t == typeof(UserModel))
            {
                return new UserModel(new UserObject());
            }
            return null;
        }
    }

    [DataContract]
    public class DummyRoutedViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment { get { return "foo"; } }
        [DataMember] public IScreen HostScreen { get; private set; }

        Guid _ARandomGuid;
        [DataMember] public Guid ARandomGuid 
        {
            get { return _ARandomGuid; }
            set { this.RaiseAndSetIfChanged(ref _ARandomGuid, value); }
        }

        public DummyRoutedViewModel(IScreen screen)
        {
            HostScreen = screen;
        }
    }

    [DataContract]
    public class DummyAppBootstrapper : IScreen
    {
        [DataMember]
        public IRoutingState Router { get; protected set; }

        public DummyAppBootstrapper()
        {
            Router = new RoutingState();
            Router.NavigateAndReset.Execute(new DummyRoutedViewModel(this) { ARandomGuid =  Guid.NewGuid() });
        }
    }

    public abstract class BlobCacheExtensionsFixture
    {
        protected abstract IBlobCache CreateBlobCache(string path);

        [Fact]
        public void DownloadUrlTest()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
            {
                var fixture = CreateBlobCache(path);
                using(fixture)
                {
                    var bytes = fixture.DownloadUrl(@"https://www.google.com/intl/en_com/images/srpr/logo3w.png").First();
                    Assert.True(bytes.Length > 0);
                }

                fixture.Shutdown.Wait();
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
                    using (var fixture = CreateBlobCache(path))
                    {
                        fixture.InsertObject("key", input);
                    }
                    sched.Start();
                    using (var fixture = CreateBlobCache(path))
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
        public void ArraysShouldBeRoundtrippable()
        {
            new TestScheduler().With(sched =>
            {
                string path;
                var input = new[] {new UserObject {Bio = "A totally cool cat!", Name = "octocat", Blog = "http://www.github.com"}, new UserObject {Bio = "zzz", Name = "sleepy", Blog = "http://example.com"}};
                UserObject[] result;

                using (Utility.WithEmptyDirectory(out path))
                {
                    using (var fixture = CreateBlobCache(path))
                    {
                        fixture.InsertObject("key", input);
                    }
                    sched.Start();
                    using (var fixture = CreateBlobCache(path))
                    {
                        var action = fixture.GetObjectAsync<UserObject[]>("key");
                        sched.Start();
                        result = action.First();
                    }
                }

                Assert.Equal(input.First().Blog, result.First().Blog);
                Assert.Equal(input.First().Bio, result.First().Bio);
                Assert.Equal(input.First().Name, result.First().Name);
                Assert.Equal(input.Last().Blog, result.Last().Blog);
                Assert.Equal(input.Last().Bio, result.Last().Bio);
                Assert.Equal(input.Last().Name, result.Last().Name);
            });
        }

        [Fact]
        public void ObjectsCanBeCreatedUsingObjectFactory()
        {
            new TestScheduler().With(sched =>
            {
                string path;
                var input = new UserModel(new UserObject()) {Age = 123, Name = "Old"};
                UserModel result;

                using (Utility.WithEmptyDirectory(out path))
                {
                    using (var fixture = CreateBlobCache(path))
                    {
                        fixture.InsertObject("key", input);
                    }
                    sched.Start();
                    using (var fixture = CreateBlobCache(path))
                    {
                        var action = fixture.GetObjectAsync<UserModel>("key");
                        sched.Start();
                        result = action.First();
                    }
                }

                Assert.Equal(input.Age, result.Age);
                Assert.Equal(input.Name, result.Name);
            });
        }

        [Fact]
        public void ArraysShouldBeRoundtrippableUsingObjectFactory()
        {
            new TestScheduler().With(sched =>
            {
                string path;
                var input = new[] {new UserModel(new UserObject()) {Age = 123, Name = "Old"}, new UserModel(new UserObject()) {Age = 123, Name = "Old"}};
                UserModel[] result;

                using (Utility.WithEmptyDirectory(out path))
                {
                    using (var fixture = CreateBlobCache(path))
                    {
                        fixture.InsertObject("key", input);
                    }
                    sched.Start();
                    using (var fixture = CreateBlobCache(path))
                    {
                        var action = fixture.GetObjectAsync<UserModel[]>("key");
                        sched.Start();
                        result = action.First();
                    }
                }

                Assert.Equal(input.First().Age, result.First().Age);
                Assert.Equal(input.First().Name, result.First().Name);
                Assert.Equal(input.Last().Age, result.Last().Age);
                Assert.Equal(input.Last().Name, result.Last().Name);
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
                using(var fixture = CreateBlobCache(path))
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

                using(var fixture = CreateBlobCache(path))
                {
                    var result = fixture.GetOrFetchObject("Test", fetcher).First();
                    Assert.Equal("Foo", result.Item1);
                    Assert.Equal("Bar", result.Item2);
                    Assert.Equal(1, fetchCount);
                }
            }
        }

        [Fact]
        public void FetchFunctionShouldPropagateThrownExceptionAsObservableException()
        {
            var fetcher = new Func<IObservable<Tuple<string, string>>>(() =>
            {
                throw new InvalidOperationException();
            });

            string path;
            using(Utility.WithEmptyDirectory(out path))
            {
                using(var fixture = CreateBlobCache(path))
                {
                    var result = fixture.GetOrFetchObject("Test", fetcher)
                        .Catch(Observable.Return(new Tuple<string, string>("one", "two"))).First();
                    Assert.Equal("one", result.Item1);
                    Assert.Equal("two", result.Item2);
                }
            }
        }

        [Fact]
        public void FetchFunctionShouldPropagateObservedExceptionAsObservableException()
        {
            var fetcher = new Func<IObservable<Tuple<string, string>>>(() =>
                Observable.Throw<Tuple<string, string>>(new InvalidOperationException()));

            string path;
            using (Utility.WithEmptyDirectory(out path))
            {
                var fixture = CreateBlobCache(path);
                using (fixture)
                {
                    var result = fixture.GetOrFetchObject("Test", fetcher)
                        .Catch(Observable.Return(new Tuple<string, string>("one", "two"))).First();
                    Assert.Equal("one", result.Item1);
                    Assert.Equal("two", result.Item2);
                }

                fixture.Shutdown.Wait();
            }
        }


        [Fact]
        public void ApplicationStateShouldBeRoundtrippable()
        {
            var resolver = new ModernDependencyResolver();
            resolver.InitializeResolver();
            resolver.InitializeAkavache();

            resolver.Register(() => new JsonSerializerSettings() {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore, 
                TypeNameHandling = TypeNameHandling.All, 
                ObjectCreationHandling = ObjectCreationHandling.Replace,
            }, typeof(JsonSerializerSettings));

            using (resolver.WithResolver()) 
            {
                string path;
                var input = new DummyAppBootstrapper();
                var expected = ((DummyRoutedViewModel) input.Router.NavigationStack[0]).ARandomGuid;
                input.Router.Navigate.Execute(new DummyRoutedViewModel(input) {ARandomGuid = Guid.NewGuid()});

                Console.WriteLine("After Nav Count: {0}", input.Router.NavigationStack.Count);

                using(Utility.WithEmptyDirectory(out path))
                using (var fixture = CreateBlobCache(path)) 
                {
                    fixture.InsertObject("state", input).First();

                    var result = fixture.GetObjectAsync<DummyAppBootstrapper>("state").First();
                    var output = (DummyRoutedViewModel) result.Router.NavigationStack[0];
                    Assert.Equal(expected, output.ARandomGuid);
                }
            }
        }

        [Fact]
        public void GetAllKeysSmokeTest()
        {
            string path;

            using (Utility.WithEmptyDirectory(out path))
            {
                var fixture = default(IBlobCache);
                using (fixture = CreateBlobCache(path))
                {
                    Observable.Merge(
                        fixture.InsertObject("Foo", "bar"),
                        fixture.InsertObject("Bar", 10),
                        fixture.InsertObject("Baz", new UserObject() { Bio = "Bio", Blog = "Blog", Name = "Name" })
                    ).Last();
                }

                fixture.Shutdown.Wait();

                using (fixture = CreateBlobCache(path))
                {
                    var keys = fixture.GetAllKeys();
                    Assert.Equal(3, keys.Count());
                    Assert.True(keys.Any(x => x.Contains("Foo")));
                    Assert.True(keys.Any(x => x.Contains("Bar")));
                }
            }
        }
    }

    public class PersistentBlobCacheExtensionsFixture : BlobCacheExtensionsFixture
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new TEncryptedBlobCache(path);
        }
    }

    public class SqliteBlobCacheExtensionsFixture : BlobCacheExtensionsFixture
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new SqlitePersistentBlobCache(Path.Combine(path, "sqlite.db"));
        }
    }

    public class EncryptedSqliteBlobCacheExtensionsFixture : BlobCacheExtensionsFixture
    {
        protected override IBlobCache CreateBlobCache(string path)
        {
            return new Sqlite3.EncryptedBlobCache(Path.Combine(path, "sqlite.db"));
        }
    }
}
