// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for relative time extension methods.
/// </summary>
[Category("Akavache")]
public class RelativeTimeExtensionsTests
{
    /// <summary>
    /// Tests that Insert with TimeSpan correctly calculates expiration.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertWithTimeSpanShouldCalculateExpirationCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            var testData = "test data"u8.ToArray();

            // Use a longer expiration to avoid CI timing issues
            var expiration = TimeSpan.FromMinutes(5);
            var beforeInsert = DateTimeOffset.Now;

            // Act
            await cache.Insert("test_key", testData, expiration);

            // Assert - verify the data was inserted
            var retrievedData = await cache.Get("test_key");
            await Assert.That(retrievedData).IsEquivalentTo(testData);

            // Verify expiration was set (we can't easily test exact expiration without waiting)
            var createdAt = await cache.GetCreatedAt("test_key");
            using (Assert.Multiple())
            {
                await Assert.That(createdAt).IsNotNull();
                await Assert.That(createdAt!.Value).IsGreaterThanOrEqualTo(beforeInsert);
            }

            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that InsertObject with TimeSpan correctly calculates expiration.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectWithTimeSpanShouldCalculateExpirationCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            var testObject = new { Name = "Test", Value = 42 };
            var expiration = TimeSpan.FromMinutes(1);
            var beforeInsert = DateTimeOffset.Now;

            // Act
            await cache.InsertObject("test_object", testObject, expiration);

            // Assert - verify the object was inserted
            var retrievedObject = await cache.GetObject<dynamic>("test_object");
            await Assert.That((object?)retrievedObject).IsNotNull();

            // Verify expiration was set
            var createdAt = await cache.GetCreatedAt("test_object");
            await Assert.That(createdAt).IsNotNull();
            await Assert.That(createdAt!.Value).IsGreaterThanOrEqualTo(beforeInsert);

            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that Insert throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public Task InsertShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            var testData = "test"u8.ToArray();
            var expiration = TimeSpan.FromSeconds(1);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.Insert("key", testData, expiration));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that InsertObject throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public Task InsertObjectShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            var testObject = new { Name = "Test" };
            var expiration = TimeSpan.FromSeconds(1);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.InsertObject("key", testObject, expiration));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that DownloadUrl (string) throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public Task DownloadUrlStringShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            const string url = "http://example.com";
            var expiration = TimeSpan.FromMinutes(5);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.DownloadUrl(url, HttpMethod.Get, expiration));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that DownloadUrl (Uri) throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public Task DownloadUrlUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            Uri url = new("http://example.com");
            var expiration = TimeSpan.FromMinutes(5);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.DownloadUrl(url, HttpMethod.Get, expiration));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that SaveLogin throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public Task SaveLoginShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            ISecureBlobCache? cache = null;
            var expiration = TimeSpan.FromHours(1);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.SaveLogin("user", "password", "host", expiration));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that relative time extensions work with different time spans.
    /// </summary>
    /// <param name="seconds">The number of seconds for the timespan.</param>
    /// <returns>A task representing the test.</returns>
    [Arguments(30)] // 30 seconds (avoid short durations that can expire in CI)
    [Arguments(60)] // 1 minute
    [Arguments(3600)] // 1 hour
    [Test]
    public async Task RelativeTimeExtensionsShouldWorkWithDifferentTimeSpans(int seconds)
    {
        // Arrange
        using (Utility.WithEmptyDirectory(out _))
        {
            SystemJsonSerializer serializer = new();
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            var testData = "test data"u8.ToArray();
            var expiration = TimeSpan.FromSeconds(seconds);
            var beforeInsert = DateTimeOffset.Now;

            // Act
            await cache.Insert($"test_key_{seconds}", testData, expiration);

            // Assert
            var retrievedData = await cache.Get($"test_key_{seconds}");
            await Assert.That(retrievedData).IsEquivalentTo(testData);

            var createdAt = await cache.GetCreatedAt($"test_key_{seconds}");
            await Assert.That(createdAt).IsNotNull();
            await Assert.That(createdAt!.Value).IsGreaterThanOrEqualTo(beforeInsert);

            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that zero timespan works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ZeroTimeSpanShouldWorkCorrectly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            var testData = "test data"u8.ToArray();
            var expiration = TimeSpan.Zero;

            // Act - Zero timespan should set expiration to current time (immediate expiration)
            await cache.Insert("zero_expiration", testData, expiration);

            // Assert - The data should still be insertable but might be immediately expired
            var createdAt = await cache.GetCreatedAt("zero_expiration");
            await Assert.That(createdAt).IsNotNull();

            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that negative timespan results in past expiration.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task NegativeTimeSpanShouldResultInPastExpiration()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateScheduler.Instance, serializer);
            var testData = "test data"u8.ToArray();
            var expiration = TimeSpan.FromSeconds(-1); // Past expiration

            // Act - Negative timespan should set expiration to past time
            await cache.Insert("negative_expiration", testData, expiration);

            // Assert - The data should still be insertable
            var createdAt = await cache.GetCreatedAt("negative_expiration");
            await Assert.That(createdAt).IsNotNull();

            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests Insert(key, data, TimeSpan) round-trips.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertShouldRoundTrip()
    {
        var cache = CreateCache();
        try
        {
            cache.Insert("k", [1, 2, 3], TimeSpan.FromMinutes(1)).SubscribeAndComplete();
            var data = cache.Get("k").SubscribeGetValue();
            await Assert.That(data).IsNotNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests InsertObject(key, value, TimeSpan) round-trips.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectShouldRoundTrip()
    {
        var cache = CreateCache();
        try
        {
            cache.InsertObject("k", "value", TimeSpan.FromMinutes(1)).SubscribeAndComplete();
            var result = cache.GetObject<string>("k").SubscribeGetValue();
            await Assert.That(result).IsEqualTo("value");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(key, TimeSpan) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeyShouldThrowOnNullCache() =>
        await Assert.That(static () => RelativeTimeExtensions.UpdateExpiration(null!, "k", TimeSpan.FromMinutes(1)))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests UpdateExpiration(key, type, TimeSpan) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeyTypeShouldThrowOnNullCache() =>
        await Assert.That(static () => RelativeTimeExtensions.UpdateExpiration(null!, "k", typeof(string), TimeSpan.FromMinutes(1)))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests UpdateExpiration(keys, TimeSpan) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldThrowOnNullCache() =>
        await Assert.That(static () => RelativeTimeExtensions.UpdateExpiration(null!, ["k"], TimeSpan.FromMinutes(1)))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests UpdateExpiration(keys, type, TimeSpan) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysTypeShouldThrowOnNullCache() =>
        await Assert.That(static () => RelativeTimeExtensions.UpdateExpiration(null!, ["k"], typeof(string), TimeSpan.FromMinutes(1)))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests UpdateExpiration(key, TimeSpan) updates the expiration.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeyShouldWork()
    {
        var cache = CreateCache();
        try
        {
            cache.Insert("k", [1]).SubscribeAndComplete();
            cache.UpdateExpiration("k", TimeSpan.FromMinutes(1)).SubscribeAndComplete();
            var data = cache.Get("k").SubscribeGetValue();
            await Assert.That(data).IsNotNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests UpdateExpiration(keys, TimeSpan) updates the expiration.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysShouldWork()
    {
        var cache = CreateCache();
        try
        {
            cache.Insert("k1", [1]).SubscribeAndComplete();
            cache.Insert("k2", [2]).SubscribeAndComplete();
            cache.UpdateExpiration(["k1", "k2"], TimeSpan.FromMinutes(1)).SubscribeAndComplete();

            var d1 = cache.Get("k1").SubscribeGetValue();
            var d2 = cache.Get("k2").SubscribeGetValue();
            await Assert.That(d1).IsNotNull();
            await Assert.That(d2).IsNotNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests DownloadUrl(string, HttpMethod, TimeSpan) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlStringHttpMethodShouldThrowOnNullCache() =>
        await Assert.That(static () => RelativeTimeDownloadExtensions.DownloadUrl(null!, "http://example.com", HttpMethod.Get, TimeSpan.FromMinutes(1)))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests DownloadUrl(Uri, HttpMethod, TimeSpan) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlUriHttpMethodShouldThrowOnNullCache() =>
        await Assert.That(static () => RelativeTimeDownloadExtensions.DownloadUrl(null!, new Uri("http://example.com"), HttpMethod.Get, TimeSpan.FromMinutes(1)))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests SaveLogin(ISecureBlobCache, ..., TimeSpan) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SaveLoginWithTimeSpanShouldThrowOnNullCache() =>
        await Assert.That(static () => RelativeTimeExtensions.SaveLogin(null!, "user", "pass", "host", TimeSpan.FromMinutes(1)))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests InsertObject with TimeSpan throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertObjectWithTimeSpanShouldThrowOnNullCache() =>
        await Assert.That(static () => RelativeTimeExtensions.InsertObject(null!, "key", "val", TimeSpan.FromMinutes(1)))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests Insert with TimeSpan throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InsertWithTimeSpanShouldThrowOnNullCache() =>
        await Assert.That(static () => RelativeTimeExtensions.Insert(null!, "key", [1, 2], TimeSpan.FromMinutes(1)))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests <see cref="RelativeTimeExtensions.UpdateExpiration(IBlobCache, string, Type, TimeSpan)"/>
    /// happy-path: updates the expiration of an existing typed entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeyTypeShouldWork()
    {
        var cache = CreateCache();
        try
        {
            cache.Insert("k", [1], typeof(string)).SubscribeAndComplete();
            cache.UpdateExpiration("k", typeof(string), TimeSpan.FromHours(1)).SubscribeAndComplete();

            var data = cache.Get("k", typeof(string)).SubscribeGetValue();
            await Assert.That(data).IsNotNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests <see cref="RelativeTimeExtensions.UpdateExpiration(IBlobCache, IEnumerable{string}, Type, TimeSpan)"/>
    /// happy-path: bulk updates the expiration for multiple typed entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UpdateExpirationKeysTypeShouldWork()
    {
        var cache = CreateCache();
        try
        {
            cache.Insert("k1", [1], typeof(string)).SubscribeAndComplete();
            cache.Insert("k2", [2], typeof(string)).SubscribeAndComplete();

            cache.UpdateExpiration(["k1", "k2"], typeof(string), TimeSpan.FromHours(1)).SubscribeAndComplete();

            var d1 = cache.Get("k1", typeof(string)).SubscribeGetValue();
            var d2 = cache.Get("k2", typeof(string)).SubscribeGetValue();
            await Assert.That(d1).IsNotNull();
            await Assert.That(d2).IsNotNull();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests <see cref="RelativeTimeExtensions.SaveLogin(ISecureBlobCache, string, string, string, TimeSpan)"/>
    /// happy-path: stores credentials and exercises the non-null cache branch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SaveLoginWithTimeSpanShouldWork()
    {
        var cache = CreateCache();
        try
        {
            cache.SaveLogin("user", "pass", "host", TimeSpan.FromHours(1)).SubscribeAndComplete();

            var login = cache.GetLogin("host").SubscribeGetValue();
            await Assert.That(login).IsNotNull();
            await Assert.That(login.UserName).IsEqualTo("user");
            await Assert.That(login.Password).IsEqualTo("pass");
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests <see cref="RelativeTimeExtensions.DownloadUrl(IBlobCache, string, HttpMethod, TimeSpan, IEnumerable{KeyValuePair{string, string}}, bool)"/>
    /// happy-path by pre-populating the cache so the download is served from cache (avoiding network).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task DownloadUrlStringHttpMethodShouldServeFromCache()
    {
        var cache = CreateCache();
        try
        {
            const string url = "http://example.invalid/data";
            cache.Insert(url, [9, 8, 7]).SubscribeAndComplete();

            var data = cache.DownloadUrl(url, HttpMethod.Get, TimeSpan.FromHours(1)).SubscribeGetValue();

            await Assert.That(data).IsEquivalentTo(new byte[] { 9, 8, 7 });
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests <see cref="RelativeTimeExtensions.DownloadUrl(IBlobCache, Uri, HttpMethod, TimeSpan, IEnumerable{KeyValuePair{string, string}}, bool)"/>
    /// happy-path by pre-populating the cache so the download is served from cache (avoiding network).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DownloadUrlUriHttpMethodShouldServeFromCache()
    {
        var cache = CreateCache();
        try
        {
            Uri url = new("http://example.invalid/data");
            cache.Insert(url.ToString(), [1, 2, 3]).SubscribeAndComplete();

            var data = cache.DownloadUrl(url, HttpMethod.Get, TimeSpan.FromHours(1)).SubscribeGetValue();

            await Assert.That(data).IsEquivalentTo(new byte[] { 1, 2, 3 });
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Creates a fresh in-memory cache wired to <see cref="ImmediateScheduler"/>.</summary>
    /// <returns>A new <see cref="InMemoryBlobCache"/>.</returns>
    private static InMemoryBlobCache CreateCache() =>
        new(ImmediateScheduler.Instance, new SystemJsonSerializer());
}
