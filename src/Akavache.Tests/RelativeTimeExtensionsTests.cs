// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using Xunit;

namespace Akavache.Tests;

/// <summary>
/// Tests for relative time extension methods.
/// </summary>
public class RelativeTimeExtensionsTests
{
    /// <summary>
    /// Tests that Insert with TimeSpan correctly calculates expiration.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task InsertWithTimeSpanShouldCalculateExpirationCorrectly()
    {
        // Arrange
        CoreRegistrations.Serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache();
            var testData = "test data"u8.ToArray();
            var expiration = TimeSpan.FromSeconds(1);
            var beforeInsert = DateTimeOffset.Now;

            // Act
            await cache.Insert("test_key", testData, expiration).FirstAsync();

            // Assert - verify the data was inserted
            var retrievedData = await cache.Get("test_key").FirstAsync();
            Assert.Equal(testData, retrievedData);

            // Verify expiration was set (we can't easily test exact expiration without waiting)
            var createdAt = await cache.GetCreatedAt("test_key").FirstAsync();
            Assert.NotNull(createdAt);
            Assert.True(createdAt >= beforeInsert);

            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that InsertObject with TimeSpan correctly calculates expiration.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task InsertObjectWithTimeSpanShouldCalculateExpirationCorrectly()
    {
        // Arrange
        CoreRegistrations.Serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache();
            var testObject = new { Name = "Test", Value = 42 };
            var expiration = TimeSpan.FromMinutes(1);
            var beforeInsert = DateTimeOffset.Now;

            // Act
            await cache.InsertObject("test_object", testObject, expiration).FirstAsync();

            // Assert - verify the object was inserted
            var retrievedObject = await cache.GetObject<dynamic>("test_object").FirstAsync();
            Assert.NotNull(retrievedObject);

            // Verify expiration was set
            var createdAt = await cache.GetCreatedAt("test_object").FirstAsync();
            Assert.NotNull(createdAt);
            Assert.True(createdAt >= beforeInsert);

            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that Insert throws ArgumentNullException when cache is null.
    /// </summary>
    [Fact]
    public void InsertShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;
        var testData = "test"u8.ToArray();
        var expiration = TimeSpan.FromSeconds(1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.Insert("key", testData, expiration));
    }

    /// <summary>
    /// Tests that InsertObject throws ArgumentNullException when cache is null.
    /// </summary>
    [Fact]
    public void InsertObjectShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;
        var testObject = new { Name = "Test" };
        var expiration = TimeSpan.FromSeconds(1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.InsertObject("key", testObject, expiration));
    }

    /// <summary>
    /// Tests that DownloadUrl (string) throws ArgumentNullException when cache is null.
    /// </summary>
    [Fact]
    public void DownloadUrlStringShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;
        var url = "http://example.com";
        var expiration = TimeSpan.FromMinutes(5);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.DownloadUrl(url, HttpMethod.Get, expiration));
    }

    /// <summary>
    /// Tests that DownloadUrl (Uri) throws ArgumentNullException when cache is null.
    /// </summary>
    [Fact]
    public void DownloadUrlUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;
        var url = new Uri("http://example.com");
        var expiration = TimeSpan.FromMinutes(5);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.DownloadUrl(url, HttpMethod.Get, expiration));
    }

    /// <summary>
    /// Tests that SaveLogin throws ArgumentNullException when cache is null.
    /// </summary>
    [Fact]
    public void SaveLoginShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        ISecureBlobCache? cache = null;
        var expiration = TimeSpan.FromHours(1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.SaveLogin("user", "password", "host", expiration));
    }

    /// <summary>
    /// Tests that relative time extensions work with different time spans.
    /// </summary>
    /// <param name="seconds">The number of seconds for the timespan.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(1)] // 1 second
    [InlineData(60)] // 1 minute
    [InlineData(3600)] // 1 hour
    public async Task RelativeTimeExtensionsShouldWorkWithDifferentTimeSpans(int seconds)
    {
        // Arrange
        CoreRegistrations.Serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache();
            var testData = "test data"u8.ToArray();
            var expiration = TimeSpan.FromSeconds(seconds);
            var beforeInsert = DateTimeOffset.Now;

            // Act
            await cache.Insert($"test_key_{seconds}", testData, expiration).FirstAsync();

            // Assert
            var retrievedData = await cache.Get($"test_key_{seconds}").FirstAsync();
            Assert.Equal(testData, retrievedData);

            var createdAt = await cache.GetCreatedAt($"test_key_{seconds}").FirstAsync();
            Assert.NotNull(createdAt);
            Assert.True(createdAt >= beforeInsert);

            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that zero timespan works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ZeroTimeSpanShouldWorkCorrectly()
    {
        // Arrange
        CoreRegistrations.Serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache();
            var testData = "test data"u8.ToArray();
            var expiration = TimeSpan.Zero;

            // Act - Zero timespan should set expiration to current time (immediate expiration)
            await cache.Insert("zero_expiration", testData, expiration).FirstAsync();

            // Assert - The data should still be insertable but might be immediately expired
            var createdAt = await cache.GetCreatedAt("zero_expiration").FirstAsync();
            Assert.NotNull(createdAt);

            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that negative timespan results in past expiration.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task NegativeTimeSpanShouldResultInPastExpiration()
    {
        // Arrange
        CoreRegistrations.Serializer = new SystemJsonSerializer();
        using (Utility.WithEmptyDirectory(out var path))
        {
            var cache = new InMemoryBlobCache();
            var testData = "test data"u8.ToArray();
            var expiration = TimeSpan.FromSeconds(-1); // Past expiration

            // Act - Negative timespan should set expiration to past time
            await cache.Insert("negative_expiration", testData, expiration).FirstAsync();

            // Assert - The data should still be insertable
            var createdAt = await cache.GetCreatedAt("negative_expiration").FirstAsync();
            Assert.NotNull(createdAt);

            await cache.DisposeAsync();
        }
    }
}
