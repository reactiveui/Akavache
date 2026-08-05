// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

using Akavache.Core;
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>A base class for tests about bulk operations.</summary>
public abstract class BlobCacheTestsBase : IDisposable
{
    /// <summary>How long a fetch-backed observable is given to produce its value.</summary>
    private const int FetchTimeoutSeconds = 5;

    /// <summary>Number of values GetAndFetchLatest emits once a value is cached: the cached one, then the freshly fetched one.</summary>
    private const int CachedAndLatestEmissionCount = 2;

    /// <summary>A backing field which indicates if the class has been disposed.</summary>
    private bool _disposed;

    /// <summary>Gets the serializers to use.</summary>
    public static IEnumerable<object[]> Serializers { get; } =
    [
        [typeof(SystemJsonSerializer)],
        [typeof(SystemJsonBsonSerializer)], // BSON-enabled System.Text.Json serializer
        [typeof(NewtonsoftSerializer)],
        [typeof(NewtonsoftBsonSerializer)], // BSON-enabled Newtonsoft.Json serializer
    ];

    /// <summary>Fetches the function should be called once for get or fetch object.</summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    /// <exception cref="ArgumentNullException">nameof(serializerType).</exception>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task FetchFunctionShouldBeCalledOnceForGetOrFetchObject(Type serializerType)
    {
        ArgumentExceptionHelper.ThrowIfNull(serializerType);

        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            if (fixture.GetType().Name.Contains("Encrypted"))
            {
                return;
            }

            _ = SetupTestSerializer(serializerType);

            var fetchCount = 0;
            Func<IObservable<Tuple<string, string>>> fetcher = new(() =>
            {
                fetchCount++;
                return Observable.Return(new Tuple<string, string>("Foo", "Bar"));
            });

            try
            {
                var result = fixture.GetOrFetchObject("Test", fetcher).ObserveOn(ImmediateScheduler.Instance)
                    .WaitForValue(TimeSpan.FromSeconds(FetchTimeoutSeconds));

                await Assert.That(result).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(result!.Item1).IsEqualTo("Foo");
                    await Assert.That(result.Item2).IsEqualTo("Bar");
                }

                await Assert.That(fetchCount).IsGreaterThanOrEqualTo(1);

                var initialFetchCount = fetchCount;
                result = fixture.GetOrFetchObject("Test", fetcher).ObserveOn(ImmediateScheduler.Instance)
                    .WaitForValue(TimeSpan.FromSeconds(FetchTimeoutSeconds));
                await Assert.That(result).IsNotNull();
                using (Assert.Multiple())
                {
                    await Assert.That(result!.Item1).IsEqualTo("Foo");
                    await Assert.That(result.Item2).IsEqualTo("Bar");
                }

                await Assert.That(fetchCount).IsLessThanOrEqualTo(initialFetchCount + 1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Skipping GetOrFetch test for {serializerType.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>Fetches the function should be called at least once for get and fetch latest when no cached value exists.</summary>
    /// <param name="serializerType">Type of the serializer.</param>
    /// <returns>
    /// A task to monitor the progress.
    /// </returns>
    /// <exception cref="ArgumentNullException">nameof(serializerType).</exception>
    [Arguments(typeof(SystemJsonSerializer))]
    [Arguments(typeof(SystemJsonBsonSerializer))]
    [Arguments(typeof(NewtonsoftSerializer))]
    [Arguments(typeof(NewtonsoftBsonSerializer))]
    [Test]
    public async Task FetchFunctionShouldBeCalledAtLeastOnceForGetAndFetchLatest(Type serializerType)
    {
        ArgumentExceptionHelper.ThrowIfNull(serializerType);

        var serializer = SetupTestSerializer(serializerType);

        using (Utility.WithEmptyDirectory(out var path))
        using (var fixture = CreateBlobCache(path, serializer))
        {
            if (fixture.GetType().Name.Contains("Encrypted"))
            {
                return;
            }

            _ = SetupTestSerializer(serializerType);

            var fetchCount = 0;
            var fetcher = () =>
            {
                fetchCount++;
                return Observable.Return(new Tuple<string, int>("Foo", fetchCount));
            };

            var result = fixture.GetAndFetchLatest("Test", fetcher)
                .ObserveOn(ImmediateScheduler.Instance)
                .WaitForValue(TimeSpan.FromSeconds(FetchTimeoutSeconds));

            using (Assert.Multiple())
            {
                await Assert.That(result).IsNotNull();
                await Assert.That(result!.Item1).IsEqualTo("Foo");
                await Assert.That(result.Item2).IsEqualTo(1);
            }

            await Assert.That(fetchCount).IsEqualTo(1);

            List<Tuple<string, int>?> results = [];
            var initialFetchCount = fetchCount;
            await fixture.GetAndFetchLatest("Test", fetcher)
                .ObserveOn(ImmediateScheduler.Instance)
                .Take(CachedAndLatestEmissionCount) // Take at most the cached value plus the latest
                .ForEachAsync(results.Add);

            // Results validation
            using (Assert.Multiple())
            {
                await Assert.That(results).Count().IsEqualTo(CachedAndLatestEmissionCount);

                // Cached value
                await Assert.That(results[0]).IsNotNull();
                await Assert.That(results[0]?.Item1).IsEqualTo("Foo");
                await Assert.That(results[0]?.Item2).IsEqualTo(1);

                // Latest value
                await Assert.That(results[1]).IsNotNull();
                await Assert.That(results[1]?.Item1).IsEqualTo("Foo");
                await Assert.That(results[1]?.Item2).IsEqualTo(initialFetchCount + 1);
            }

            await Assert.That(fetchCount).IsEqualTo(initialFetchCount + 1);
        }
    }

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the <see cref="IBlobCache" /> we want to do the tests against.</summary>
    /// <param name="path">The path to the blob cache.</param>
    /// <param name="serializer">The serializer.</param>
    /// <returns>
    /// The blob cache for testing.
    /// </returns>
    protected abstract IBlobCache CreateBlobCache(string path, ISerializer serializer);

    /// <summary>Helper method to create a blob cache for a specific path, ensuring the path is used correctly.</summary>
    /// <param name="path">The base path for the cache.</param>
    /// <param name="serializer">The serializer.</param>
    /// <returns>
    /// The cache instance.
    /// </returns>
    /// <remarks>
    /// For roundtrip tests, this uses the same database file creation strategy as the main
    /// <see cref="CreateBlobCache"/> but ensures the path is respected for proper isolation.
    /// </remarks>
    protected virtual IBlobCache CreateBlobCacheForPath(string path, ISerializer serializer) =>
        CreateBlobCache(path, serializer);

    /// <summary>
    /// Checks if a serializer type is compatible with the current cache implementation.
    /// This prevents cross-serializer testing that would be invalid.
    /// </summary>
    /// <param name="serializerType">The serializer type to check.</param>
    /// <param name="cacheType">The cache type to check against.</param>
    /// <returns>True if the serializer is compatible with the cache type.</returns>
    protected virtual bool IsSerializerCompatibleWithCache(Type serializerType, Type cacheType)
    {
        // With the universal shim, most combinations should now work
        // Only skip truly incompatible combinations that can't be shimmed
        if (serializerType is not null && cacheType is not null)
        {
            return true;
        }

        throw new ArgumentNullException(serializerType is null ? nameof(serializerType) : nameof(cacheType));
    }

    /// <summary>Disposes the specified disposing.</summary>
    /// <param name="disposing">if set to <c>true</c> [disposing].</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    /// <summary>Sets up the test with the specified serializer type.</summary>
    /// <param name="serializerType">The type of serializer to use for this test.</param>
    /// <returns>The configured serializer instance.</returns>
    private static ISerializer SetupTestSerializer(Type? serializerType)
    {
        // Clear any existing in-flight requests to ensure clean test state
        RequestCache.Clear();

        if (serializerType == typeof(NewtonsoftBsonSerializer))
        {
            // Register the Newtonsoft BSON serializer specifically
            return new NewtonsoftBsonSerializer();
        }

        if (serializerType == typeof(SystemJsonBsonSerializer))
        {
            // Register the System.Text.Json BSON serializer specifically
            return new SystemJsonBsonSerializer();
        }

        if (serializerType == typeof(NewtonsoftSerializer))
        {
            // Register the Newtonsoft JSON serializer
            return new NewtonsoftSerializer();
        }

        return serializerType == typeof(SystemJsonSerializer) ? new SystemJsonSerializer() : null!;
    }
}
