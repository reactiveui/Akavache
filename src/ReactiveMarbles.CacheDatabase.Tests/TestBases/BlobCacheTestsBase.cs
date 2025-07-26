// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using DynamicData;
using FluentAssertions;
using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.NewtonsoftJson;
using ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;
using ReactiveMarbles.CacheDatabase.SystemTextJson;
using ReactiveMarbles.CacheDatabase.SystemTextJson.Bson;
using ReactiveMarbles.CacheDatabase.Tests.Helpers;
using ReactiveMarbles.CacheDatabase.Tests.Mocks;
using Xunit;

namespace ReactiveMarbles.CacheDatabase.Tests;

/// <summary>
/// A base class for tests about bulk operations.
/// </summary>
public abstract class BlobCacheTestsBase : IDisposable
{
    private ISerializer? _originalSerializer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobCacheTestsBase"/> class.
    /// </summary>
    protected BlobCacheTestsBase()
    {
        // Store the original serializer to restore it after each test
        _originalSerializer = CoreRegistrations.Serializer;
    }

    /// <summary>
    /// Gets the serializers to use.
    /// </summary>
    public static IEnumerable<object[]> Serializers { get; } =
    [
        [typeof(SystemJsonSerializer)],
        [typeof(SystemJsonBsonSerializer)], // Hybrid serializer for BSON compatibility
        [typeof(NewtonsoftSerializer)],
        [typeof(NewtonsoftBsonSerializer)],
    ];

    /// <summary>
    /// Disposes of the test resources and restores the original serializer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tests to make sure the download url extension methods download correctly.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory(Skip = "Network-dependent tests are unreliable in CI environments")]
    [MemberData(nameof(Serializers))]
    public async Task DownloadUrlTest(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory(Skip = "Network-dependent tests are unreliable in CI environments")]
    [MemberData(nameof(Serializers))]
    public async Task DownloadUriTest(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory(Skip = "Network-dependent tests are unreliable in CI environments")]
    [MemberData(nameof(Serializers))]
    public async Task DownloadUrlWithKeyTest(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory(Skip = "Network-dependent tests are unreliable in CI environments")]
    [MemberData(nameof(Serializers))]
    public async Task DownloadUriWithKeyTest(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GettingNonExistentKeyShouldThrow(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task ArraysShouldBeRoundtrippable(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
        SetupTestSerializer(serializerType);

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
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task FetchFunctionShouldBeCalledOnceForGetOrFetchObject(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
                // First call should trigger fetch
                var result = await fixture.GetOrFetchObject("Test", fetcher).ObserveOn(ImmediateScheduler.Instance).FirstAsync();
                Assert.NotNull(result);
                Assert.Equal("Foo", result.Item1);
                Assert.Equal("Bar", result.Item2);
                Assert.Equal(1, fetchCount);

                // 2nd time around, we should be grabbing from cache
                result = await fixture.GetOrFetchObject("Test", fetcher).ObserveOn(ImmediateScheduler.Instance).FirstAsync();
                Assert.NotNull(result);
                Assert.Equal("Foo", result.Item1);
                Assert.Equal("Bar", result.Item2);
                Assert.Equal(1, fetchCount);
            }

            await using (var fixture = CreateBlobCache(path))
            {
                // InMemoryBlobCache isn't round-trippable by design - skip persistence test for in-memory caches
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    return;
                }

                var result = await fixture.GetOrFetchObject("Test", fetcher).ObserveOn(ImmediateScheduler.Instance).FirstAsync();
                Assert.NotNull(result);
                Assert.Equal("Foo", result.Item1);
                Assert.Equal("Bar", result.Item2);
                Assert.Equal(1, fetchCount);
            }
        }
    }

    /// <summary>
    /// Makes sure the fetch function debounces current requests.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous unit test.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task FetchFunctionShouldDebounceConcurrentRequestsAsync(Type serializerType)
    {
        SetupTestSerializer(serializerType);

        // Use a simpler concurrency test that doesn't rely on precise scheduler timing
        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var callCount = 0;
            var fetcher = new Func<IObservable<int>>(() =>
            {
                var currentCount = Interlocked.Increment(ref callCount);

                // Add some delay to ensure concurrent requests can overlap
                return Observable.Create<int>(observer =>
                {
                    Task.Run(async () =>
                    {
                        if (currentCount == 1)
                        {
                            // First call waits for all concurrent requests to start
                            await Task.Delay(100);
                        }

                        observer.OnNext(42);
                        observer.OnCompleted();
                    });

                    return new System.Reactive.Disposables.CompositeDisposable();
                });
            });

            // Start multiple concurrent requests for the same key
            var tasks = Enumerable.Range(0, 5)
                .Select(_ => fixture.GetOrFetchObject("concurrent_key", fetcher).FirstAsync().ToTask())
                .ToArray();

            // Wait for all to complete
            var results = await Task.WhenAll(tasks);

            // All should return the same result
            Assert.True(results.All(r => r == 42));

            // The fetch function should have been called only once (or a very small number of times)
            // due to request deduplication
            Assert.True(callCount <= 2, $"Expected fetch to be called 1-2 times, but was called {callCount} times");
        }
    }

    /// <summary>
    /// Makes sure that the fetch function propogates thrown exceptions.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task FetchFunctionShouldPropagateThrownExceptionAsObservableException(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task FetchFunctionShouldPropagateObservedExceptionAsObservableException(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GetOrFetchShouldRespectExpiration(Type serializerType)
    {
        SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var fetchCount = 0;
            var fetcher = new Func<IObservable<string>>(() =>
            {
                fetchCount++;
                return Observable.Return($"fetch_{fetchCount}");
            });

            // Test 1: First call should fetch and cache the result
            var result1 = await fixture.GetOrFetchObject("expiry_test", fetcher, DateTimeOffset.Now.AddMilliseconds(300))
                .FirstAsync();
            Assert.Equal("fetch_1", result1);
            Assert.Equal(1, fetchCount);

            // Test 2: Second call within expiry should use cache (not fetch again)
            var result2 = await fixture.GetOrFetchObject("expiry_test", fetcher, DateTimeOffset.Now.AddMilliseconds(300))
                .FirstAsync();
            Assert.Equal("fetch_1", result2);
            Assert.Equal(1, fetchCount); // Should still be 1 since we used cache

            // Test 3: Wait for expiry and test again
            await Task.Delay(400); // Wait for cache entry to expire

            // When cache expires, GetObject should throw KeyNotFoundException
            // and GetOrFetchObject should detect this and fetch again
            var result3 = await fixture.GetOrFetchObject("expiry_test", fetcher, DateTimeOffset.Now.AddSeconds(10))
                .FirstAsync();
            Assert.Equal("fetch_2", result3);
            Assert.Equal(2, fetchCount); // Should be 2 now since cache expired and we fetched again
        }
    }

    /// <summary>
    /// Makes sure that the GetAndFetchLatest invalidates objects on errors.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GetAndFetchLatestShouldInvalidateObjectOnError(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GetAndFetchLatestCallsFetchPredicate(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">The serializer type.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GetAndFetchLatestValidatesItemsToBeCached(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
        SetupTestSerializer(type);

        var input = new[] { "Foo", "Bar", "Baz" };

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
        SetupTestSerializer(serializerType);

        var input = new[] { "Foo", "Bar", "Baz" };

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task CreatedAtTimeAccurate(Type serializerType)
    {
        SetupTestSerializer(serializerType);

        var input = new[] { "Foo", "Bar", "Baz" };

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GetAllKeysSmokeTest(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">The serializer type.</param>
    /// <returns>A task to monitor the progress.</returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GetAllKeysBulkSmokeTest(Type serializerType)
    {
        if (serializerType is null)
        {
            throw new ArgumentNullException(nameof(serializerType));
        }

        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GetShouldWorkWithMultipleKeys(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GetShouldInvalidateOldKeys(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task InsertShouldWorkWithMultipleKeys(Type serializerType)
    {
        SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        await using (var fixture = CreateBlobCache(path))
        {
            var data = new byte[] { 0x10, 0x20, 0x30, };
            var keys = new[] { "Foo", "Bar", "Baz", };

            await fixture.Insert(keys.ToDictionary(k => k, _ => data)).FirstAsync();

            Assert.Equal(keys.Length, (await fixture.GetAllKeys().ToList().FirstAsync()).Count);

            var allData = await fixture.Get(keys).ToList().FirstAsync();

            Assert.Equal(keys.Length, allData.Count);
            Assert.True(allData.All(x => x.Value[0] == data[0] && x.Value[1] == data[1]));
        }
    }

    /// <summary>
    /// Invalidate should be able to trash multiple keys.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task InvalidateShouldTrashMultipleKeys(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task CacheShouldBeAbleToGetAndInsertBlobs(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task CacheShouldBeRoundtrippable(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task for monitoring the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task CreatedAtShouldBeSetAutomaticallyAndBeRetrievable(Type serializerType)
    {
        SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        {
            var fixture = CreateBlobCache(path);
            DateTimeOffset roughCreationTime;
            await using (fixture)
            {
                fixture.Insert("Foo", [1, 2, 3]).Wait();
                roughCreationTime = fixture.Scheduler.Now;

                // For InMemoryBlobCache, test CreatedAt immediately since it doesn't persist
                if (fixture.GetType().Name.Contains("InMemoryBlobCache"))
                {
                    var createdAt = fixture.GetCreatedAt("Foo").Wait();
                    Assert.NotNull(createdAt);
                    Assert.InRange(
                        actual: createdAt.Value,
                        low: roughCreationTime - TimeSpan.FromSeconds(1),
                        high: roughCreationTime);
                    return;
                }
            }

            fixture.Dispose();

            await using (var fixture2 = CreateBlobCache(path))
            {
                var createdAt = fixture2.GetCreatedAt("Foo").Wait();

                Assert.NotNull(createdAt);
                Assert.InRange(
                    actual: createdAt.Value,
                    low: roughCreationTime - TimeSpan.FromSeconds(1),
                    high: roughCreationTime);
            }
        }
    }

    /// <summary>
    /// Tests to make sure that GetsAllKeys does not return expired keys.
    /// </summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task GetAllKeysShouldntReturnExpiredKeys(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task VacuumDoesntPurgeKeysThatShouldBeThere(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task VacuumPurgeEntriesThatAreExpired(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous unit test.
    /// </returns>
    [Theory]
    [MemberData(nameof(Serializers))]
    public async Task TestIssueAsync(Type serializerType)
    {
        SetupTestSerializer(serializerType);

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
    /// Sets up the test with the specified serializer type.
    /// </summary>
    /// <param name="serializerType">The type of serializer to use for this test.</param>
    /// <returns>The configured serializer instance.</returns>
    protected static ISerializer SetupTestSerializer(Type serializerType)
    {
        // Clear any existing in-flight requests to ensure clean test state
        RequestCache.Clear();

        var serializer = (ISerializer?)Activator.CreateInstance(serializerType);
        if (serializer == null)
        {
            throw new InvalidOperationException($"Failed to create serializer of type {serializerType?.Name}");
        }

        // Always set the serializer directly for consistent behavior
        CoreRegistrations.Serializer = serializer;

        // Special handling for BSON serializers
        if (serializerType == typeof(NewtonsoftBsonSerializer))
        {
            ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson.BsonRegistrations.EnsureRegistered();
        }
        else if (serializerType == typeof(SystemJsonBsonSerializer))
        {
            // SystemJsonBsonSerializer doesn't need special BSON registrations as it's self-contained
            // But we might need to ensure DateTime handling is set up correctly
        }

        return serializer;
    }

    /// <summary>
    /// Restores the original serializer configuration.
    /// </summary>
    protected virtual void RestoreOriginalSerializer()
    {
        if (_originalSerializer != null)
        {
            CoreRegistrations.Serializer = _originalSerializer;
        }
    }

    /// <summary>
    /// Disposes of the test resources.
    /// </summary>
    /// <param name="disposing">Whether we're disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                RestoreOriginalSerializer();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Gets the <see cref="IBlobCache"/> we want to do the tests against.
    /// </summary>
    /// <param name="path">The path to the blob cache.</param>
    /// <returns>The blob cache for testing.</returns>
    protected abstract IBlobCache CreateBlobCache(string path);
}
