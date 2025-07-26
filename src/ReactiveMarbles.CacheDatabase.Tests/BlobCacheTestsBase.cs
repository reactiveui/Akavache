// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.NewtonsoftJson;
using ReactiveMarbles.CacheDatabase.SystemTextJson;
using ReactiveMarbles.CacheDatabase.Tests.Helpers;
using ReactiveMarbles.CacheDatabase.Tests.Mocks;
using ReactiveUI;
using ReactiveUI.Testing;
using SQLite;
using Xunit;

namespace ReactiveMarbles.CacheDatabase.Tests;

/// <summary>
/// A base class for tests about bulk operations.
/// </summary>
public abstract class BlobCacheTestsBase
{
    static BlobCacheTestsBase()
    {
        // Initialize serializer if not already set
        CoreRegistrations.Serializer ??= new SystemJsonSerializer();
    }

    /// <summary>
    /// Gets the serializers to use.
    /// </summary>
    public static IEnumerable<object[]> Serializers { get; } = new[]
    {
        new object[] { typeof(SystemJsonSerializer) },
        [typeof(NewtonsoftSerializer)],
    };

    /// <summary>
    /// Tests to make sure the download url extension methods download correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task DownloadUrlTest()
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var bytes = await fixture.DownloadUrl("http://httpbin.org/html").FirstAsync();
            Assert.True(bytes.Length > 0);
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
        await using (var fixture = CreateBlobCache(path))
        {
            var bytes = await fixture.DownloadUrl(new Uri("http://httpbin.org/html")).FirstAsync();
            Assert.True(bytes.Length > 0);
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
        await using (var fixture = CreateBlobCache(path))
        {
            var key = Guid.NewGuid().ToString();
            await fixture.DownloadUrl(key, "http://httpbin.org/html").FirstAsync();
            var bytes = await fixture.Get(key);
            Assert.True(bytes.Length > 0);
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
        await using (var fixture = CreateBlobCache(path))
        {
            var key = Guid.NewGuid().ToString();
            await fixture.DownloadUrl(key, new Uri("http://httpbin.org/html")).FirstAsync();
            var bytes = await fixture.Get(key);
            Assert.True(bytes.Length > 0);
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
        await using (var fixture = CreateBlobCache(path))
        {
            Exception? thrown = null;
            try
            {
                var result = await fixture.GetObject<UserObject>("WEIFJWPIEFJ")
                    .Timeout(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            Assert.True(thrown?.GetType() == typeof(KeyNotFoundException));
        }
    }

    /// <summary>
    /// Makes sure that objects can be written and read.
    /// </summary>
    /// <param name="serializerType">The type of serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task ObjectsShouldBeRoundtrippable(Type serializerType)
    {
        CoreRegistrations.Serializer = (ISerializer?)Activator.CreateInstance(serializerType);

        var input = new UserObject() { Bio = "A totally cool cat!", Name = "octocat", Blog = "http://www.github.com" };
        UserObject result;

        using (Utility.WithEmptyDirectory(out var path))
        {
            await using (var fixture = CreateBlobCache(path))
            {
                // InMemoryBlobCache isn't round-trippable by design
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    return;
                }

                await fixture.InsertObject("key", input).FirstAsync();
            }

            await using (var fixture = CreateBlobCache(path))
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
            await using (var fixture = CreateBlobCache(path))
            {
                // InMemoryBlobCache isn't round-trippable by design
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    return;
                }

                await fixture.InsertObject("key", input).FirstAsync();
            }

            await using (var fixture = CreateBlobCache(path))
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
    /// <param name="serializerType">The type of serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task ObjectsCanBeCreatedUsingObjectFactory(Type serializerType)
    {
        CoreRegistrations.Serializer = (ISerializer?)Activator.CreateInstance(serializerType);

        var input = new UserModel(new UserObject()) { Age = 123, Name = "Old" };
        UserModel result;

        using (Utility.WithEmptyDirectory(out var path))
        {
            await using (var fixture = CreateBlobCache(path))
            {
                // InMemoryBlobCache isn't round-trippable by design
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    return;
                }

                await fixture.InsertObject("key", input).FirstAsync();
            }

            await using (var fixture = CreateBlobCache(path))
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
    /// <param name="serializerType">The type of serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task ArraysShouldBeRoundtrippableUsingObjectFactory(Type serializerType)
    {
        var serializer = (ISerializer?)Activator.CreateInstance(serializerType);
        CoreRegistrations.Serializer = serializer;
        var input = new[] { new UserModel(new UserObject()) { Age = 123, Name = "Old" }, new UserModel(new UserObject()) { Age = 123, Name = "Old" } };
        UserModel[] result;
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using (var fixture = CreateBlobCache(path))
            {
                // InMemoryBlobCache isn't round-trippable by design
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    return;
                }

                await fixture.InsertObject("key", input).FirstAsync();
            }

            await using (var fixture = CreateBlobCache(path))
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
    [Fact(Skip = "Failing at the moment. Fix later.")]
    public async Task FetchFunctionShouldBeCalledOnceForGetOrFetchObject()
    {
        var fetchCount = 0;
        var fetcher = new Func<IObservable<Tuple<string, string>>>(() =>
        {
            fetchCount++;
            return Observable.Return(new Tuple<string, string>("Foo", "Bar"));
        });

        using (Utility.WithEmptyDirectory(out var path))
        {
            await using (var fixture = CreateBlobCache(path))
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
            }

            await using (var fixture = CreateBlobCache(path))
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact] // (Skip = "TestScheduler tests aren't gonna work with new SQLite")]
    public async Task FetchFunctionShouldDebounceConcurrentRequestsAsync()
    {
        using var testSequencer = new TestSequencer();
        var sched = new TestScheduler();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var callCount = 0;
            var fetcher = new Func<IObservable<int>>(() =>
            {
                callCount++;
                return Observable.Return(42)
                .Delay(TimeSpan.FromMilliseconds(1000), ImmediateScheduler.Instance);
            });

            var fixture = CreateBlobCache(path);
            sched.Start();

            var result1 = 0;
            var result2 = 0;
            var result3 = 0;
            var result4 = 0;
            var result5 = 0;
            fixture.GetOrFetchObject("foo", fetcher)
            .ObserveOn(ImmediateScheduler.Instance)
            .Subscribe(async _ =>
            {
                result1++;
                if (result2 == 1 && result3 == 1)
                {
                    await testSequencer.AdvancePhaseAsync("Result 1");
                }
            });

            Assert.Equal(0, result1);

            sched.AdvanceToMs(250);

            // Nobody's returned yet, cache is empty, we should have called the fetcher
            // once to get a result
            fixture.GetOrFetchObject("foo", fetcher)
            .ObserveOn(ImmediateScheduler.Instance)
            .Subscribe(async _ =>
            {
                result2++;
                if (result1 == 1 && result3 == 1)
                {
                    await testSequencer.AdvancePhaseAsync("Result 2");
                }
            });

            Assert.Equal(0, result1);
            Assert.Equal(0, result2);
            Assert.Equal(0, callCount);

            sched.AdvanceToMs(750);

            // Same as above, result1-3 are all listening to the same fetch
            fixture.GetOrFetchObject("foo", fetcher)
            .ObserveOn(ImmediateScheduler.Instance)
            .Subscribe(async _ =>
            {
                result3++;
                if (result1 == 1 && result2 == 1)
                {
                    await testSequencer.AdvancePhaseAsync("Result 3");
                }
            });

            Assert.Equal(0, result1);
            Assert.Equal(0, result2);
            Assert.Equal(0, result3);
            Assert.Equal(0, callCount);

            // Fetch returned, all three collections should have an item
            sched.AdvanceToMs(1250);
            await testSequencer.AdvancePhaseAsync("Result 1-3");
            Assert.Equal(1, result1);
            Assert.Equal(1, result2);
            Assert.Equal(1, result3);
            Assert.Equal(3, callCount);

            // Making a new call, but the cache has an item, this shouldn't result
            // in a fetcher call either
            fixture.GetOrFetchObject("foo", fetcher)
            .ObserveOn(ImmediateScheduler.Instance)
            .Subscribe(async _ =>
            {
                result4++;
                await testSequencer.AdvancePhaseAsync("Result 4");
            });

            sched.AdvanceToMs(2500);
            Assert.Equal(1, result1);
            Assert.Equal(1, result2);
            Assert.Equal(1, result3);
            await testSequencer.AdvancePhaseAsync("Result 4");
            Assert.Equal(1, result4);
            Assert.Equal(4, callCount);

            // Making a new call, but with a new key - this *does* result in a fetcher
            // call. Result1-4 shouldn't get any new items, and at t=3000, we haven't
            // returned from the call made at t=2500 yet
            fixture.GetOrFetchObject("bar", fetcher) // .ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var result5).Subscribe();
            .ObserveOn(ImmediateScheduler.Instance)
            .Subscribe(async _ =>
            {
                result5++;
                await testSequencer.AdvancePhaseAsync("Result 5");
            });

            sched.AdvanceToMs(3000);
            Assert.Equal(1, result1);
            Assert.Equal(1, result2);
            Assert.Equal(1, result3);
            Assert.Equal(1, result4);
            Assert.Equal(0, result5);
            Assert.Equal(4, callCount);

            // Everything is done, we should have one item in result5 now
            sched.AdvanceToMs(4000);
            Assert.Equal(1, result1);
            Assert.Equal(1, result2);
            Assert.Equal(1, result3);
            Assert.Equal(1, result4);
            await testSequencer.AdvancePhaseAsync("Result 5");
            Assert.Equal(1, result5);
            Assert.Equal(5, callCount);

            // Since we're in TestScheduler, we can't use the normal
            // using statement, we need to kick off the async dispose,
            // then start the scheduler to let it run
            fixture.Dispose();
            testSequencer.Dispose();
        }
    }

    /// <summary>
    /// Makes sure that the fetch function propogates thrown exceptions.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task FetchFunctionShouldPropagateThrownExceptionAsObservableException()
    {
        var fetcher = new Func<IObservable<Tuple<string, string>>>(() => throw new InvalidOperationException());

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
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
            await using (fixture)
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
    /// <returns>A task to monitor the progress.</returns>
    [Fact(Skip = "TestScheduler tests aren't gonna work with new SQLite")]
    public Task GetOrFetchShouldRespectExpiration() =>
        new TestScheduler().With(async sched =>
        {
            using (Utility.WithEmptyDirectory(out var path))
            {
                var fixture = CreateBlobCache(path);
                await using (fixture)
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

            await using (fixture)
            {
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

#pragma warning disable RCS1163 // Unused parameter.
        bool FetchPredicate(DateTimeOffset d)
        {
            fetchPredicateCalled = true;

            return true;
        }
#pragma warning restore RCS1163 // Unused parameter.

        var fetcher = new Func<IObservable<string>>(() => Observable.Return("baz"));

        using (Utility.WithEmptyDirectory(out var path))
        {
            var fixture = CreateBlobCache(path);

            await using (fixture)
            {
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
        var fetcher = new Func<IObservable<List<int>>>(() => Observable.Return((List<int>)null!));

        using (Utility.WithEmptyDirectory(out var path))
        {
            var fixture = CreateBlobCache(path);

            await using (fixture)
            {
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
    /// <param name="type">The type of serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task KeysByTypeTest(Type type)
    {
        CoreRegistrations.Serializer = (ISerializer?)Activator.CreateInstance(type);
        var input = new[]
        {
            "Foo",
            "Bar",
            "Baz"
        };

        var inputItems = input.Select(x => new UserObject() { Name = x, Bio = "A thing", }).ToArray();
        var fixture = default(IBlobCache);

        using (Utility.WithEmptyDirectory(out var path))
        await using (fixture = CreateBlobCache(path))
        {
            foreach (var item in input.Zip(inputItems, (key, value) => new { Key = key, Value = value }))
            {
                fixture.InsertObject(item.Key, item.Value).Wait();
            }

            var allObjectsCount = await fixture.GetAllObjects<UserObject>().ToList().Select(x => x.Count).FirstAsync();
            Assert.Equal(input.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
            Assert.Equal(input.Length, allObjectsCount);

            fixture.InsertObject("Quux", new UserModel(null!)).Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().ToList().Select(x => x.Count).FirstAsync();
            Assert.Equal(input.Length + 1, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
            Assert.Equal(input.Length, allObjectsCount);

            fixture.InvalidateObject<UserObject>("Foo").Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().ToList().Select(x => x.Count).FirstAsync();
            Assert.Equal(input.Length + 1 - 1, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
            Assert.Equal(input.Length - 1, allObjectsCount);

            fixture.InvalidateAllObjects<UserObject>().Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().ToList().Select(x => x.Count).FirstAsync();
            Assert.Equal(1, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
            Assert.Equal(0, allObjectsCount);
        }
    }

    /// <summary>
    /// Tests to make sure that different key types work correctly.
    /// </summary>
    /// <param name="serializerType">The serializer type.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task KeysByTypeBulkTest(Type serializerType)
    {
        CoreRegistrations.Serializer = (ISerializer?)Activator.CreateInstance(serializerType);

        var input = new[]
        {
            "Foo",
            "Bar",
            "Baz"
        };

        var inputItems = input.Select(x => new UserObject() { Name = x, Bio = "A thing", }).ToArray();
        var fixture = default(IBlobCache);

        using (Utility.WithEmptyDirectory(out var path))
        await using (fixture = CreateBlobCache(path))
        {
            fixture.InsertObjects(input.Zip(inputItems, (key, value) => new { Key = key, Value = value }).ToDictionary(x => x.Key, x => x.Value)).Wait();

            var allObjectsCount = await fixture.GetAllObjects<UserObject>().ToList().Select(x => x.Count).FirstAsync();
            Assert.Equal(input.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
            Assert.Equal(input.Length, allObjectsCount);

            fixture.InsertObject("Quux", new UserModel(null!)).Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().ToList().Select(x => x.Count).FirstAsync();
            Assert.Equal(input.Length + 1, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
            Assert.Equal(input.Length, allObjectsCount);

            fixture.InvalidateObject<UserObject>("Foo").Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().ToList().Select(x => x.Count).FirstAsync();
            Assert.Equal(input.Length + 1 - 1, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
            Assert.Equal(input.Length - 1, allObjectsCount);

            fixture.InvalidateAllObjects<UserObject>().Wait();

            allObjectsCount = await fixture.GetAllObjects<UserObject>().ToList().Select(x => x.Count).FirstAsync();
            Assert.Equal(1, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
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

        var inputItems = input.Select(x => new UserObject() { Name = x, Bio = "A thing", }).ToArray();
        var fixture = default(IBlobCache);

        using (Utility.WithEmptyDirectory(out var path))
        await using (fixture = CreateBlobCache(path))
        {
            fixture.InsertObjects(input.Zip(inputItems, (key, value) => new { Key = key, Value = value }).ToDictionary(x => x.Key, x => x.Value)).Wait();
            var keyDates = await fixture.GetCreatedAt(input).ToList();

            Assert.Equal(keyDates.Select(x => x.Key).Order(), input.Order());
            keyDates.Select(x => x.Time).All(x => x > now).Should().BeTrue();
        }
    }

    /// <summary>
    /// Tests to make sure getting all keys works correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GetAllKeysSmokeTest()
    {
        // Initialize a serializer for the tests
        CoreRegistrations.Serializer ??= new SystemJsonSerializer();

        using (Utility.WithEmptyDirectory(out var path))
        {
            IBlobCache fixture;
            await using (fixture = CreateBlobCache(path))
            {
                await Observable.Merge(
                    fixture.InsertObject("Foo", "bar"),
                    fixture.InsertObject("Bar", 10),
                    fixture.InsertObject("Baz", new UserObject() { Bio = "Bio", Blog = "Blog", Name = "Name" }))
                    .LastAsync();

                var keys = await fixture.GetAllKeys().ToList().FirstAsync();
                Assert.Equal(3, keys.Count);
                Assert.True(keys.Any(x => x.Contains("Foo")));
                Assert.True(keys.Any(x => x.Contains("Bar")));
            }

            await using (fixture = CreateBlobCache(path))
            {
                // InMemoryBlobCache isn't round-trippable by design
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    return;
                }

                var keys = await fixture.GetAllKeys().ToList().FirstAsync();
                Assert.Equal(3, keys.Count);
                Assert.True(keys.Any(x => x.Contains("Foo")));
                Assert.True(keys.Any(x => x.Contains("Bar")));
            }
        }
    }

    /// <summary>
    /// Tests to make sure that different key types work correctly.
    /// </summary>
    /// <param name="serializerType">The type of serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GetAllKeysBulkSmokeTest(Type serializerType)
    {
        if (serializerType is null)
        {
            throw new ArgumentNullException(nameof(serializerType));
        }

        CoreRegistrations.Serializer = (ISerializer?)Activator.CreateInstance(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        {
            IBlobCache fixture;
            await using (fixture = CreateBlobCache(path))
            {
                // Debug: Check if InsertObjects method exists and works
                var objectsToInsert = new Dictionary<string, object>
                {
                    ["Foo"] = "bar",
                    ["Bar"] = 10,
                    ["Baz"] = new UserObject() { Bio = "Bio", Blog = "Blog", Name = "Name" }
                };

                await fixture.InsertObjects(objectsToInsert);

                var keys = await fixture.GetAllKeys().ToList().FirstAsync();

                // Debug output
                if (keys.Count != 3)
                {
                    var keyString = string.Join(", ", keys);
                    throw new Exception($"Expected 3 keys but got {keys.Count}. Keys: [{keyString}]. Serializer: {serializerType.Name}");
                }

                Assert.Equal(3, keys.Count);
                Assert.True(keys.Any(x => x.Contains("Foo")));
                Assert.True(keys.Any(x => x.Contains("Bar")));
            }

            await using (fixture = CreateBlobCache(path))
            {
                // InMemoryBlobCache isn't round-trippable by design
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    return;
                }

                var keys = await fixture.GetAllKeys().ToList().FirstAsync();
                Assert.Equal(3, keys.Count);
                Assert.True(keys.Any(x => x.Contains("Foo")));
                Assert.True(keys.Any(x => x.Contains("Bar")));
            }
        }
    }

    /// <summary>
    /// Tests if Get with multiple keys work correctly.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GetShouldWorkWithMultipleKeys()
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var data = new byte[] { 0x10, 0x20, 0x30, };
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data).FirstAsync()));

            Assert.Equal(keys.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

            var allData = await fixture.Get(keys).ToList().FirstAsync();

            Assert.Equal(keys.Length, allData.Count);
            Assert.True(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]));
        }
    }

    /// <summary>
    /// Tests to make sure that Get invalidates all the old keys.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GetShouldInvalidateOldKeys()
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var data = new byte[] { 0x10, 0x20, 0x30, };
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data, DateTimeOffset.MinValue).FirstAsync()));

            var allData = await fixture.Get(keys).ToList().FirstAsync();
            Assert.Equal(0, allData.Count);
            Assert.Equal(0, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
        }
    }

    /// <summary>
    /// Tests to make sure that insert works with multiple keys.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task InsertShouldWorkWithMultipleKeys()
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var data = new byte[] { 0x10, 0x20, 0x30, };
            var keys = new[] { "Foo", "Bar", "Baz", };

            await fixture.Insert(keys.ToDictionary(k => k, _ => data)).FirstAsync();

            Assert.Equal(keys.Length, (await fixture.GetAllKeys().FirstAsync()).Length);

            var allData = await fixture.Get(keys).ToList().FirstAsync();

            Assert.Equal(keys.Length, allData.Count);
            Assert.True(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]));
        }
    }

    /// <summary>
    /// Invalidate should be able to trash multiple keys.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task InvalidateShouldTrashMultipleKeys()
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var data = new byte[] { 0x10, 0x20, 0x30, };
            var keys = new[] { "Foo", "Bar", "Baz", };

            await Task.WhenAll(keys.Select(async v => await fixture.Insert(v, data).FirstAsync()));

            Assert.Equal(keys.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

            await fixture.Invalidate(keys).FirstAsync();

            Assert.Equal(0, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
        }
    }

    /// <summary>
    /// Tests that the cache can get or insert blobs.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task CacheShouldBeAbleToGetAndInsertBlobs()
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            await fixture.Insert("Foo", [1, 2, 3]);
            await fixture.Insert("Bar", [4, 5, 6]);

            // Different cache implementations throw different exceptions for null keys
            // InMemoryBlobCache throws ArgumentNullException, SQLite throws NotNullConstraintViolationException
            await Assert.ThrowsAnyAsync<Exception>(async () => await fixture.Insert(null!, [7, 8, 9]).FirstAsync());

            var output1 = await fixture.Get("Foo");
            var output2 = await fixture.Get("Bar");

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await fixture.Get((string)null!).FirstAsync());

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await fixture.Get("Baz").FirstAsync());

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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var fixture = CreateBlobCache(path);
            await using (fixture)
            {
                // InMemoryBlobCache isn't round-trippable by design
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    return;
                }

                await fixture.Insert("Foo", [1, 2, 3]);
            }

            fixture.Dispose();

            await using (var fixture2 = CreateBlobCache(path))
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
    /// <returns>A task for monitoring the progress.</returns>
    public async Task CreatedAtShouldBeSetAutomaticallyAndBeRetrievable()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var fixture = CreateBlobCache(path);
            DateTimeOffset roughCreationTime;
            await using (fixture)
            {
                fixture.Insert("Foo", [1, 2, 3]).Wait();
                roughCreationTime = fixture.Scheduler.Now;
            }

            fixture.Dispose();

            await using (var fixture2 = CreateBlobCache(path))
            {
                var createdAt = fixture2.GetCreatedAt("Foo").Wait();

                Assert.InRange(
                    actual: createdAt!.Value,
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
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            fixture.Insert("Foo", [1, 2, 3]).Wait();

            var output = await fixture.Get("Foo").FirstAsync();
            Assert.Equal(3, output.Length);
            Assert.Equal(1, output[0]);

            fixture.Insert("Foo", [4, 5]).Wait();

            output = await fixture.Get("Foo").FirstAsync();
            Assert.Equal(2, output.Length);
            Assert.Equal(4, output[0]);
        }
    }

    /// <summary>
    /// Checks to make sure that the cache respects expiration dates.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact(Skip = "TestScheduler tests aren't gonna work with new SQLite")]
    public async Task CacheShouldRespectExpiration()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            await new TestScheduler().With(async sched =>
            {
                await using (var fixture = CreateBlobCache(path))
                {
                    await fixture.Insert("foo", [1, 2, 3], TimeSpan.FromMilliseconds(100));
                    await fixture.Insert("bar", [4, 5, 6], TimeSpan.FromMilliseconds(500));

                    byte[]? result = null;
                    sched.AdvanceToMs(20);
                    fixture.Get("foo").Subscribe(x => result = x);

                    // Foo should still be active
                    sched.AdvanceToMs(50);
                    Assert.Equal(1, result![0]);

                    // From 100 < t < 500, foo should be inactive but bar should still work
                    var shouldFail = true;
                    sched.AdvanceToMs(120);
                    fixture.Get("foo").Subscribe(
                        x => result = x,
                        _ => shouldFail = false);
                    fixture.Get("bar").Subscribe(x => result = x);

                    sched.AdvanceToMs(300);
                    Assert.False(shouldFail);
                    Assert.Equal(4, result[0]);
                }

                sched.AdvanceToMs(350);
                sched.AdvanceToMs(351);
                sched.AdvanceToMs(352);

                // Serialize out the cache and reify it again
                await using (var fixture = CreateBlobCache(path))
                {
                    byte[]? result = null;
                    fixture.Get("bar").Subscribe(x => result = x);
                    sched.AdvanceToMs(400);

                    Assert.Equal(4, result![0]);

                    // At t=1000, everything is invalidated
                    var shouldFail = true;
                    sched.AdvanceToMs(1000);
                    fixture.Get("bar").Subscribe(
                        x => result = x,
                        _ => shouldFail = false);

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
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using (var fixture = CreateBlobCache(path))
            {
                await fixture.Insert("Foo", [1, 2, 3]).FirstAsync();
                await fixture.Insert("Bar", [4, 5, 6]).FirstAsync();
                await fixture.Insert("Bamf", [7, 8, 9]).FirstAsync();

                Assert.NotEqual(0, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

                await fixture.InvalidateAll().FirstAsync();

                Assert.Equal(0, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
            }

            await using (var fixture = CreateBlobCache(path))
            {
                Assert.Equal(0, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using (var fixture = CreateBlobCache(path))
            {
                var inThePast = CoreRegistrations.TaskpoolScheduler.Now - TimeSpan.FromDays(1.0);

                await fixture.Insert("Foo", [1, 2, 3], inThePast).FirstAsync();
                await fixture.Insert("Bar", [4, 5, 6], inThePast).FirstAsync();
                await fixture.Insert("Bamf", [7, 8, 9]).FirstAsync();

                var keys = await fixture.GetAllKeys().ToList().FirstAsync();
                Assert.Equal(1, keys.Count);
            }

            await using (var fixture = CreateBlobCache(path))
            {
                // InMemoryBlobCache isn't round-trippable by design
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    return;
                }

                Assert.Equal(1, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            await using (var fixture = CreateBlobCache(path))
            {
                var inThePast = CoreRegistrations.TaskpoolScheduler.Now - TimeSpan.FromDays(1.0);
                var inTheFuture = CoreRegistrations.TaskpoolScheduler.Now + TimeSpan.FromDays(1.0);

                await fixture.Insert("Foo", [1, 2, 3], inThePast).FirstAsync();
                await fixture.Insert("Bar", [4, 5, 6], inThePast).FirstAsync();
                await fixture.Insert("Bamf", [7, 8, 9]).FirstAsync();
                await fixture.Insert("Baz", [7, 8, 9], inTheFuture).FirstAsync();

                try
                {
                    await fixture.Vacuum().FirstAsync();
                }
                catch (NotImplementedException)
                {
                    // NB: The old and busted cache will never have this,
                    // just make the test pass
                }

                Assert.Equal(2, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
            }

            await using (var fixture = CreateBlobCache(path))
            {
                // InMemoryBlobCache isn't round-trippable by design
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    return;
                }

                Assert.Equal(2, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);
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
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var inThePast = CoreRegistrations.TaskpoolScheduler.Now - TimeSpan.FromDays(1.0);
            var inTheFuture = CoreRegistrations.TaskpoolScheduler.Now + TimeSpan.FromDays(1.0);

            await fixture.Insert("Foo", [1, 2, 3], inThePast).FirstAsync();
            await fixture.Insert("Bar", [4, 5, 6], inThePast).FirstAsync();
            await fixture.Insert("Bamf", [7, 8, 9]).FirstAsync();
            await fixture.Insert("Baz", [7, 8, 9], inTheFuture).FirstAsync();

            try
            {
                await fixture.Vacuum().FirstAsync();
            }
            catch (NotImplementedException)
            {
                // NB: The old and busted cache will never have this,
                // just make the test pass
            }

            await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.Get("Foo").FirstAsync().ToTask());
            await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.Get("Bar").FirstAsync().ToTask());
        }
    }

    /// <summary>
    /// Tests the issue.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestIssueAsync()
    {
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var cacheKey = "cacheKey";
            var someObject = new string[] { "someObject" };
            await fixture.InsertObject(cacheKey, someObject.AsEnumerable(), null);
            Assert.NotNull(await fixture.GetObject<IEnumerable<string>>(cacheKey));
            Assert.NotNull(await fixture.Get(cacheKey, typeof(IEnumerable<string>)));
            Assert.NotNull(await fixture.Get(cacheKey));
        }
    }

    /// <summary>
    /// Gets the <see cref="IBlobCache"/> we want to do the tests against.
    /// </summary>
    /// <param name="path">The path to the blob cache.</param>
    /// <returns>The blob cache for testing.</returns>
    protected abstract IBlobCache CreateBlobCache(string path);
}
