// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using DynamicData;

using FluentAssertions;

using Microsoft.Reactive.Testing;

using ReactiveUI.Testing;
using Splat;

namespace Akavache.Tests;

/// <summary>
/// Fixture for testing the blob cache extension methods.
/// </summary>
public abstract class BlobCacheExtensionsTestBase
{
    /// <summary>
    /// Tests to make sure the download url extension methods download correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task DownloadUrlTest()
    {
        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path))
        {
            var bytes = await fixture.DownloadUrl("http://httpbin.org/html").FirstAsync();
            bytes.Length.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// Tests to make sure the download Uri extension method overload performs correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task DownloadUriTest()
    {
        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path))
        {
            var bytes = await fixture.DownloadUrl(new Uri("http://httpbin.org/html")).FirstAsync();
            bytes.Length.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// Tests to make sure the download with key extension method overload performs correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task DownloadUrlWithKeyTest()
    {
        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path))
        {
            var key = Guid.NewGuid().ToString();
            await fixture.DownloadUrl(key, "http://httpbin.org/html").FirstAsync();
            var bytes = await fixture.Get(key);
            bytes.Length.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// Tests to make sure the download Uri with key extension method overload performs correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task DownloadUriWithKeyTest()
    {
        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path))
        {
            var key = Guid.NewGuid().ToString();
            await fixture.DownloadUrl(key, new Uri("https://httpbin.org/html")).FirstAsync();
            var bytes = await fixture.Get(key);
            bytes.Length.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// Tests to make sure that getting non-existent keys throws an exception.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GettingNonExistentKeyShouldThrow()
    {
        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path))
        {
            Exception thrown = null;
            try
            {
                var result = await fixture.GetObject<UserObject>("WEIFJWPIEFJ")
                    .Timeout(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            Assert.True(thrown.GetType() == typeof(KeyNotFoundException));
        }
    }

    /// <summary>
    /// Makes sure that objects can be written and read.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task ObjectsShouldBeRoundtrippable()
    {
        var input = new UserObject { Bio = "A totally cool cat!", Name = "octocat", Blog = "http://www.github.com" };
        UserObject result;

        using (Utility.WithEmptyDirectory(out var path))
        {
            using (var fixture = CreateBlobCache(path))
            {
                if (fixture is InMemoryBlobCache)
                {
                    return;
                }

                await fixture.InsertObject("key", input).FirstAsync();
            }

            using (var fixture = CreateBlobCache(path))
            {
                result = await fixture.GetObject<UserObject>("key").FirstAsync();
            }
        }

        Assert.Equal(input.Blog, result.Blog);
        Assert.Equal(input.Bio, result.Bio);
        Assert.Equal(input.Name, result.Name);
    }

    /// <summary>
    /// Makes sure that arrays can be written and read.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task ArraysShouldBeRoundtrippable()
    {
        var input = new[] { new UserObject { Bio = "A totally cool cat!", Name = "octocat", Blog = "http://www.github.com" }, new UserObject { Bio = "zzz", Name = "sleepy", Blog = "http://example.com" } };
        UserObject[] result;

        using (Utility.WithEmptyDirectory(out var path))
        {
            using (var fixture = CreateBlobCache(path))
            {
                if (fixture is InMemoryBlobCache)
                {
                    return;
                }

                await fixture.InsertObject("key", input).FirstAsync();
            }

            using (var fixture = CreateBlobCache(path))
            {
                result = await fixture.GetObject<UserObject[]>("key").FirstAsync();
            }
        }

        Assert.Equal(input[0].Blog, result[0].Blog);
        Assert.Equal(input[0].Bio, result[0].Bio);
        Assert.Equal(input[0].Name, result[0].Name);
        Assert.Equal(input.Last().Blog, result.Last().Blog);
        Assert.Equal(input.Last().Bio, result.Last().Bio);
        Assert.Equal(input.Last().Name, result.Last().Name);
    }

    /// <summary>
    /// Makes sure that the objects can be created using the object factory.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task ObjectsCanBeCreatedUsingObjectFactory()
    {
        var input = new UserModel(new()) { Age = 123, Name = "Old" };
        UserModel result;

        using (Utility.WithEmptyDirectory(out var path))
        {
            using (var fixture = CreateBlobCache(path))
            {
                if (fixture is InMemoryBlobCache)
                {
                    return;
                }

                await fixture.InsertObject("key", input).FirstAsync();
            }

            using (var fixture = CreateBlobCache(path))
            {
                result = await fixture.GetObject<UserModel>("key").FirstAsync();
            }
        }

        Assert.Equal(input.Age, result.Age);
        Assert.Equal(input.Name, result.Name);
    }

    /// <summary>
    /// Makes sure that arrays can be written and read and using the object factory.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task ArraysShouldBeRoundtrippableUsingObjectFactory()
    {
        var input = new[] { new UserModel(new()) { Age = 123, Name = "Old" }, new UserModel(new()) { Age = 123, Name = "Old" } };
        UserModel[] result;
        using (Utility.WithEmptyDirectory(out var path))
        {
            using (var fixture = CreateBlobCache(path))
            {
                if (fixture is InMemoryBlobCache)
                {
                    return;
                }

                await fixture.InsertObject("key", input).FirstAsync();
            }

            using (var fixture = CreateBlobCache(path))
            {
                result = await fixture.GetObject<UserModel[]>("key").FirstAsync();
            }
        }

        Assert.Equal(input[0].Age, result[0].Age);
        Assert.Equal(input[0].Name, result[0].Name);
        Assert.Equal(input.Last().Age, result.Last().Age);
        Assert.Equal(input.Last().Name, result.Last().Name);
    }

    /// <summary>
    /// Make sure that the fetch functions are called only once for the get or fetch object methods.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [SkippableFact]
    public async Task FetchFunctionShouldBeCalledOnceForGetOrFetchObject()
    {
        // TODO: This test is failing on .NET 6.0. Investigate.
        Skip.If(GetType().Assembly.GetTargetFrameworkName().StartsWith("net"));

        var fetchCount = 0;
        var fetcher = new Func<IObservable<Tuple<string, string>>>(() =>
        {
            fetchCount++;
            return Observable.Return(new Tuple<string, string>("Foo", "Bar"));
        });

        using (Utility.WithEmptyDirectory(out var path))
        {
            using (var fixture = CreateBlobCache(path))
            {
                var result = await fixture.GetOrFetchObject("Test", fetcher).ObserveOn(ImmediateScheduler.Instance).FirstAsync();
                Assert.Equal("Foo", result.Item1);
                Assert.Equal("Bar", result.Item2);
                Assert.Equal(1, fetchCount);

                // 2nd time around, we should be grabbing from cache
                result = await fixture.GetOrFetchObject("Test", fetcher).ObserveOn(ImmediateScheduler.Instance).FirstAsync();
                Assert.Equal("Foo", result.Item1);
                Assert.Equal("Bar", result.Item2);
                Assert.Equal(1, fetchCount);

                // Testing persistence makes zero sense for InMemoryBlobCache
                if (fixture is InMemoryBlobCache)
                {
                    return;
                }
            }

            using (var fixture = CreateBlobCache(path))
            {
                var result = await fixture.GetOrFetchObject("Test", fetcher).ObserveOn(ImmediateScheduler.Instance).FirstAsync();
                Assert.Equal("Foo", result.Item1);
                Assert.Equal("Bar", result.Item2);
                Assert.Equal(1, fetchCount);
            }
        }
    }

    /// <summary>
    /// Makes sure the fetch function debounces current requests.
    /// </summary>
    [SkippableFact]
    public void FetchFunctionShouldDebounceConcurrentRequests() =>
        new TestScheduler().With(sched =>
        {
            // TODO: TestScheduler tests aren't gonna work with new SQLite.
            Skip.If(GetType().Assembly.GetTargetFrameworkName().StartsWith("net"));
            using (Utility.WithEmptyDirectory(out var path))
            {
                var callCount = 0;
                var fetcher = new Func<IObservable<int>>(() =>
                {
                    callCount++;
                    return Observable.Return(42).Delay(TimeSpan.FromMilliseconds(1000), sched);
                });

                var fixture = CreateBlobCache(path);
                try
                {
                    fixture.GetOrFetchObject("foo", fetcher).ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var result1).Subscribe();

                    Assert.Equal(0, result1.Count);

                    sched.AdvanceToMs(250);

                    // Nobody's returned yet, cache is empty, we should have called the fetcher
                    // once to get a result
                    fixture.GetOrFetchObject("foo", fetcher).ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var result2).Subscribe();
                    Assert.Equal(0, result1.Count);
                    Assert.Equal(0, result2.Count);
                    Assert.Equal(1, callCount);

                    sched.AdvanceToMs(750);

                    // Same as above, result1-3 are all listening to the same fetch
                    fixture.GetOrFetchObject("foo", fetcher).ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var result3).Subscribe();
                    Assert.Equal(0, result1.Count);
                    Assert.Equal(0, result2.Count);
                    Assert.Equal(0, result3.Count);
                    Assert.Equal(1, callCount);

                    // Fetch returned, all three collections should have an item
                    sched.AdvanceToMs(1250);
                    Assert.Equal(1, result1.Count);
                    Assert.Equal(1, result2.Count);
                    Assert.Equal(1, result3.Count);
                    Assert.Equal(1, callCount);

                    // Making a new call, but the cache has an item, this shouldn't result
                    // in a fetcher call either
                    fixture.GetOrFetchObject("foo", fetcher).ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var result4).Subscribe();
                    sched.AdvanceToMs(2500);
                    Assert.Equal(1, result1.Count);
                    Assert.Equal(1, result2.Count);
                    Assert.Equal(1, result3.Count);
                    Assert.Equal(1, result4.Count);
                    Assert.Equal(1, callCount);

                    // Making a new call, but with a new key - this *does* result in a fetcher
                    // call. Result1-4 shouldn't get any new items, and at t=3000, we haven't
                    // returned from the call made at t=2500 yet
                    fixture.GetOrFetchObject("bar", fetcher).ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var result5).Subscribe();
                    sched.AdvanceToMs(3000);
                    Assert.Equal(1, result1.Count);
                    Assert.Equal(1, result2.Count);
                    Assert.Equal(1, result3.Count);
                    Assert.Equal(1, result4.Count);
                    Assert.Equal(0, result5.Count);
                    Assert.Equal(2, callCount);

                    // Everything is done, we should have one item in result5 now
                    sched.AdvanceToMs(4000);
                    Assert.Equal(1, result1.Count);
                    Assert.Equal(1, result2.Count);
                    Assert.Equal(1, result3.Count);
                    Assert.Equal(1, result4.Count);
                    Assert.Equal(1, result5.Count);
                    Assert.Equal(2, callCount);
                }
                finally
                {
                    // Since we're in TestScheduler, we can't use the normal
                    // using statement, we need to kick off the async dispose,
                    // then start the scheduler to let it run
                    fixture.Dispose();
                    sched.Start();
                }
            }
        });

    /// <summary>
    /// Makes sure that the fetch function propogates thrown exceptions.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task FetchFunctionShouldPropagateThrownExceptionAsObservableException()
    {
        var fetcher = new Func<IObservable<Tuple<string, string>>>(() => throw new InvalidOperationException());

        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path))
        {
            var result = await fixture.GetOrFetchObject("Test", fetcher)
                .Catch(Observable.Return(new Tuple<string, string>("one", "two"))).FirstAsync();
            Assert.Equal("one", result.Item1);
            Assert.Equal("two", result.Item2);
        }
    }

    /// <summary>
    /// Makes sure that the fetch function propogates thrown exceptions.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task FetchFunctionShouldPropagateObservedExceptionAsObservableException()
    {
        var fetcher = new Func<IObservable<Tuple<string, string>>>(() =>
            Observable.Throw<Tuple<string, string>>(new InvalidOperationException()));

        using (Utility.WithEmptyDirectory(out var path))
        {
            var fixture = CreateBlobCache(path);
            using (fixture)
            {
                var result = await fixture.GetOrFetchObject("Test", fetcher)
                    .Catch(Observable.Return(new Tuple<string, string>("one", "two"))).FirstAsync();
                Assert.Equal("one", result.Item1);
                Assert.Equal("two", result.Item2);
            }
        }
    }

    /// <summary>
    /// Make sure that the GetOrFetch function respects expirations.
    /// </summary>
    [SkippableFact]
    public void GetOrFetchShouldRespectExpiration() =>
        new TestScheduler().With(sched =>
        {
            // TODO: TestScheduler tests aren't gonna work with new SQLite.
            Skip.If(GetType().Assembly.GetTargetFrameworkName().StartsWith("net"));
            using (Utility.WithEmptyDirectory(out var path))
            {
                var fixture = CreateBlobCache(path);
                using (fixture)
                {
                    var result = default(string);
                    fixture.GetOrFetchObject(
                            "foo",
                            () => Observable.Return("bar"),
                            sched.Now + TimeSpan.FromMilliseconds(1000))
                        .Subscribe(x => result = x);

                    sched.AdvanceByMs(250);
                    Assert.Equal("bar", result);

                    fixture.GetOrFetchObject(
                            "foo",
                            () => Observable.Return("baz"),
                            sched.Now + TimeSpan.FromMilliseconds(1000))
                        .Subscribe(x => result = x);

                    sched.AdvanceByMs(250);
                    Assert.Equal("bar", result);

                    sched.AdvanceByMs(1000);
                    fixture.GetOrFetchObject(
                            "foo",
                            () => Observable.Return("baz"),
                            sched.Now + TimeSpan.FromMilliseconds(1000))
                        .Subscribe(x => result = x);

                    sched.AdvanceByMs(250);
                    Assert.Equal("baz", result);
                }
            }
        });

    /// <summary>
    /// Makes sure that the GetAndFetchLatest invalidates objects on errors.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GetAndFetchLatestShouldInvalidateObjectOnError()
    {
        var fetcher = new Func<IObservable<string>>(() => Observable.Throw<string>(new InvalidOperationException()));

        using (Utility.WithEmptyDirectory(out var path))
        {
            var fixture = CreateBlobCache(path);

            using (fixture)
            {
                if (fixture is InMemoryBlobCache)
                {
                    return;
                }

                await fixture.InsertObject("foo", "bar").FirstAsync();

                await fixture.GetAndFetchLatest("foo", fetcher, shouldInvalidateOnError: true)
                    .Catch(Observable.Return("get and fetch latest error"))
                    .ToList()
                    .FirstAsync();

                var result = await fixture.GetObject<string>("foo")
                    .Catch(Observable.Return("get error"))
                    .FirstAsync();

                Assert.Equal("get error", result);
            }
        }
    }

    /// <summary>
    /// Makes sure that the GetAndFetchLatest calls the Fetch predicate.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GetAndFetchLatestCallsFetchPredicate()
    {
        var fetchPredicateCalled = false;

        bool FetchPredicate(DateTimeOffset _)
        {
            fetchPredicateCalled = true;

            return true;
        }

        var fetcher = new Func<IObservable<string>>(() => Observable.Return("baz"));

        using (Utility.WithEmptyDirectory(out var path))
        {
            var fixture = CreateBlobCache(path);

            using (fixture)
            {
                if (fixture is InMemoryBlobCache)
                {
                    return;
                }

                await fixture.InsertObject("foo", "bar").FirstAsync();

                await fixture.GetAndFetchLatest("foo", fetcher, FetchPredicate).LastAsync();

                Assert.True(fetchPredicateCalled);
            }
        }
    }

    /// <summary>
    /// Make sure that the GetAndFetchLatest method validates items already in the cache.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GetAndFetchLatestValidatesItemsToBeCached()
    {
        const string key = "tv1";
        var items = new List<int> { 4, 7, 10, 11, 3, 4 };
        var fetcher = new Func<IObservable<List<int>>>(() => Observable.Return((List<int>)null));

        using (Utility.WithEmptyDirectory(out var path))
        {
            var fixture = CreateBlobCache(path);

            using (fixture)
            {
                if (fixture is InMemoryBlobCache)
                {
                    return;
                }

                // GetAndFetchLatest will overwrite cache with null result
                await fixture.InsertObject(key, items);

                await fixture.GetAndFetchLatest(key, fetcher).LastAsync();

                var failedResult = await fixture.GetObject<List<int>>(key).FirstAsync();

                Assert.Null(failedResult);

                // GetAndFetchLatest skips cache invalidation/storage due to cache validation predicate.
                await fixture.InsertObject(key, items);

                await fixture.GetAndFetchLatest(key, fetcher, cacheValidationPredicate: i => i?.Count > 0).LastAsync();

                var result = await fixture.GetObject<List<int>>(key).FirstAsync();

                Assert.NotNull(result);
                Assert.True(result.Count > 0, "The returned list is empty.");
                Assert.Equal(items, result);
            }
        }
    }

    /// <summary>
    /// Tests to make sure that different key types work correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task KeysByTypeTest()
    {
        var input = new[]
        {
            "Foo",
            "Bar",
            "Baz"
        };

        var inputItems = input.Select(x => new UserObject { Name = x, Bio = "A thing", }).ToArray();
        var fixture = default(IBlobCache);

        using (Utility.WithEmptyDirectory(out var path))
        using (fixture = CreateBlobCache(path))
        {
            foreach (var item in input.Zip(inputItems, (key, value) => new { Key = key, Value = value }))
            {
                fixture.InsertObject(item.Key, item.Value).Wait();
            }

            var allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count()).FirstAsync();
            Assert.Equal(input.Length, (await fixture.GetAllKeys().FirstAsync()).Count());
            Assert.Equal(input.Length, allObjectsCount);

            fixture.InsertObject("Quux", new UserModel(null)).Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count()).FirstAsync();
            Assert.Equal(input.Length + 1, (await fixture.GetAllKeys().FirstAsync()).Count());
            Assert.Equal(input.Length, allObjectsCount);

            fixture.InvalidateObject<UserObject>("Foo").Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count()).FirstAsync();
            Assert.Equal(input.Length + 1 - 1, (await fixture.GetAllKeys().FirstAsync()).Count());
            Assert.Equal(input.Length - 1, allObjectsCount);

            fixture.InvalidateAllObjects<UserObject>().Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count()).FirstAsync();
            Assert.Equal(1, (await fixture.GetAllKeys().FirstAsync()).Count());
            Assert.Equal(0, allObjectsCount);
        }
    }

    /// <summary>
    /// Tests to make sure that different key types work correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task KeysByTypeBulkTest()
    {
        var input = new[]
        {
            "Foo",
            "Bar",
            "Baz"
        };

        var inputItems = input.Select(x => new UserObject { Name = x, Bio = "A thing", }).ToArray();
        var fixture = default(IBlobCache);

        using (Utility.WithEmptyDirectory(out var path))
        using (fixture = CreateBlobCache(path))
        {
            fixture.InsertObjects(input.Zip(inputItems, (key, value) => new { Key = key, Value = value }).ToDictionary(x => x.Key, x => x.Value)).Wait();

            var allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count()).FirstAsync();
            Assert.Equal(input.Length, (await fixture.GetAllKeys().FirstAsync()).Count());
            Assert.Equal(input.Length, allObjectsCount);

            fixture.InsertObject("Quux", new UserModel(null)).Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count()).FirstAsync();
            Assert.Equal(input.Length + 1, (await fixture.GetAllKeys().FirstAsync()).Count());
            Assert.Equal(input.Length, allObjectsCount);

            fixture.InvalidateObject<UserObject>("Foo").Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count()).FirstAsync();
            Assert.Equal(input.Length + 1 - 1, (await fixture.GetAllKeys().FirstAsync()).Count());
            Assert.Equal(input.Length - 1, allObjectsCount);

            fixture.InvalidateAllObjects<UserObject>().Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count()).FirstAsync();
            Assert.Equal(1, (await fixture.GetAllKeys().FirstAsync()).Count());
            Assert.Equal(0, allObjectsCount);
        }
    }

    /// <summary>
    /// Tests to make sure that different key types work correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task CreatedAtTimeAccurate()
    {
        var input = new[]
        {
            "Foo",
            "Bar",
            "Baz"
        };

        var now = DateTimeOffset.Now.AddSeconds(-30);

        var inputItems = input.Select(x => new UserObject { Name = x, Bio = "A thing", }).ToArray();
        var fixture = default(IBlobCache);

        using (Utility.WithEmptyDirectory(out var path))
        using (fixture = CreateBlobCache(path))
        {
            fixture.InsertObjects(input.Zip(inputItems, (key, value) => new { Key = key, Value = value }).ToDictionary(x => x.Key, x => x.Value)).Wait();
            var keyDates = await fixture.GetCreatedAt(input);

            Assert.Equal(keyDates.Keys.OrderBy(x => x), input.OrderBy(x => x));
            keyDates.Values.All(x => x > now).Should().BeTrue();
        }
    }

    /// <summary>
    /// Tests to make sure getting all keys works correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GetAllKeysSmokeTest()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            IBlobCache fixture;
            using (fixture = CreateBlobCache(path))
            {
                await Observable.Merge(
                        fixture.InsertObject("Foo", "bar"),
                        fixture.InsertObject("Bar", 10),
                        fixture.InsertObject("Baz", new UserObject { Bio = "Bio", Blog = "Blog", Name = "Name" }))
                    .LastAsync();

                var keys = await fixture.GetAllKeys().FirstAsync();
                Assert.Equal(3, keys.Count());
                Assert.True(keys.Any(x => x.Contains("Foo")));
                Assert.True(keys.Any(x => x.Contains("Bar")));
            }

            if (fixture is InMemoryBlobCache)
            {
                return;
            }

            using (fixture = CreateBlobCache(path))
            {
                var keys = await fixture.GetAllKeys().FirstAsync();
                Assert.Equal(3, keys.Count());
                Assert.True(keys.Any(x => x.Contains("Foo")));
                Assert.True(keys.Any(x => x.Contains("Bar")));
            }
        }
    }

    /// <summary>
    /// Tests to make sure getting all keys works correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GetAllKeysBulkSmokeTest()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            IBlobCache fixture;
            using (fixture = CreateBlobCache(path))
            {
                await fixture.InsertObjects(new Dictionary<string, object>
                {
                    ["Foo"] = "bar",
                    ["Bar"] = 10,
                    ["Baz"] = new UserObject { Bio = "Bio", Blog = "Blog", Name = "Name" }
                });

                var keys = await fixture.GetAllKeys().FirstAsync();
                Assert.Equal(3, keys.Count());
                Assert.True(keys.Any(x => x.Contains("Foo")));
                Assert.True(keys.Any(x => x.Contains("Bar")));
            }

            if (fixture is InMemoryBlobCache)
            {
                return;
            }

            using (fixture = CreateBlobCache(path))
            {
                var keys = await fixture.GetAllKeys().FirstAsync();
                Assert.Equal(3, keys.Count());
                Assert.True(keys.Any(x => x.Contains("Foo")));
                Assert.True(keys.Any(x => x.Contains("Bar")));
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
