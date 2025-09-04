// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Drawing;
using Akavache.SystemTextJson;
using Splat;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.Drawing BitmapImageExtensions functionality.
/// </summary>
[NonParallelizable]
[TestFixture]
[Category("Akavache")]
public class BitmapImageExtensionsTests : IAsyncLifetime
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
    private IBitmapLoader? _originalLoader;

    /// <summary>
    /// Performs per-test-class initialization.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task InitializeAsync()
    {
        // Ensure a fast, deterministic bitmap loader for all tests in this class
        try
        {
            _originalLoader = BitmapLoader.Current;
        }
        catch
        {
            _originalLoader = null;
        }

        BitmapLoader.Current = new MockBitmapLoader();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs per-test-class cleanup.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task DisposeAsync()
    {
        try
        {
            if (_originalLoader is not null)
            {
                BitmapLoader.Current = _originalLoader;
            }
        }
        catch
        {
            // Ignore restore failures
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests that LoadImage throws ArgumentNullException when cache is null.
    /// </summary>
    [Test]
    public void LoadImageShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.LoadImage("test_key"));
    }

    /// <summary>
    /// Tests that LoadImage with dimensions throws ArgumentNullException when cache is null.
    /// </summary>
    [Test]
    public void LoadImageWithDimensionsShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.LoadImage("test_key", 100f, 200f));
    }

    /// <summary>
    /// Tests that LoadImageFromUrl throws ArgumentNullException when cache is null.
    /// </summary>
    [Test]
    public void LoadImageFromUrlShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl("http://example.com/image.png"));
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with Uri throws ArgumentNullException when cache is null.
    /// </summary>
    [Test]
    public void LoadImageFromUrlWithUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;
        var uri = new Uri("http://example.com/image.png");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl(uri));
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with key throws ArgumentNullException when cache is null.
    /// </summary>
    [Test]
    public void LoadImageFromUrlWithKeyShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl("key", "http://example.com/image.png"));
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with key and Uri throws ArgumentNullException when cache is null.
    /// </summary>
    [Test]
    public void LoadImageFromUrlWithKeyAndUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;
        var uri = new Uri("http://example.com/image.png");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl("key", uri));
    }

    /// <summary>
    /// Tests that SaveImage throws ArgumentNullException when cache is null.
    /// </summary>
    [Test]
    public void SaveImageShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;
        var mockBitmap = new MockBitmap();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache!.SaveImage("key", mockBitmap));
    }

    /// <summary>
    /// Tests that SaveImage throws ArgumentNullException when image is null.
    /// </summary>
    [Test]
    public void SaveImageShouldThrowArgumentNullExceptionWhenImageIsNull()
    {
        // Arrange
        using var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        IBitmap? nullBitmap = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.SaveImage("key", nullBitmap!));
    }

    /// <summary>
    /// Tests that ImageToBytes throws ArgumentNullException when image is null.
    /// </summary>
    [Test]
    public void ImageToBytesShouldThrowArgumentNullExceptionWhenImageIsNull()
    {
        // Arrange
        IBitmap? nullBitmap = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullBitmap!.ImageToBytes());
    }

    /// <summary>
    /// Tests that ThrowOnBadImageBuffer works correctly with valid data.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldReturnValidDataForGoodBuffer()
    {
        // Arrange
        var validImageData = new byte[128]; // Greater than 64 bytes
        for (var i = 0; i < validImageData.Length; i++)
        {
            validImageData[i] = (byte)(i % 256);
        }

        // Act
        var result = await BitmapImageExtensions.ThrowOnBadImageBuffer(validImageData)
            .Timeout(TestTimeout)
            .FirstAsync();

        // Assert
        Assert.That(result, Is.EqualTo(validImageData));
    }

    /// <summary>
    /// Tests that ThrowOnBadImageBuffer throws for null data.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldThrowForNullData()
    {
        // Arrange
        byte[]? nullData = null;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await BitmapImageExtensions.ThrowOnBadImageBuffer(nullData!)
            .Timeout(TestTimeout)
            .FirstAsync());
    }

    /// <summary>
    /// Tests that ThrowOnBadImageBuffer throws for too small data.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldThrowForTooSmallData()
    {
        // Arrange
        var tooSmallData = new byte[32]; // Less than 64 bytes

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await BitmapImageExtensions.ThrowOnBadImageBuffer(tooSmallData)
            .Timeout(TestTimeout)
            .FirstAsync());
    }

    /// <summary>
    /// Tests that LoadImage handles missing keys correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LoadImageShouldHandleMissingKeysCorrectly()
    {
        // Arrange
        await using var cache = new InMemoryBlobCache(new SystemJsonSerializer());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.LoadImage("nonexistent_key")
            .Timeout(TestTimeout)
            .FirstAsync());
    }

    /// <summary>
    /// Tests that SaveImage and LoadImage work together for basic functionality.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task SaveImageAndLoadImageShouldWorkTogether()
    {
        // Arrange
        await using var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        var mockBitmap = new MockBitmap();
        const string key = "test_image";

        // Act - Save image (should serialize the bitmap data)
        await cache.SaveImage(key, mockBitmap)
            .Timeout(TestTimeout)
            .FirstAsync();

        // Act - Load image (should deserialize and recreate bitmap)
        var loadedBitmap = await cache.LoadImage(key)
            .Timeout(TestTimeout)
            .FirstAsync();

        // Assert
        Assert.That(loadedBitmap, Is.Not.Null);

        // For the mock implementation, we can verify basic properties
        Assert.That(loadedBitmap.Width, Is.EqualTo(mockBitmap.Width));
        Assert.That(loadedBitmap.Height, Is.EqualTo(mockBitmap.Height));

        await cache.DisposeAsync();
    }

    /// <summary>
    /// Tests that ImageToBytes works correctly with mock bitmap.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ImageToBytesShouldWorkWithMockBitmap()
    {
        // Arrange
        var mockBitmap = new MockBitmap();

        // Act
        var bytes = await mockBitmap.ImageToBytes()
            .Timeout(TestTimeout)
            .FirstAsync();

        // Assert
        Assert.That(bytes, Is.Not.Null);
        Assert.That(bytes.Length > 0, Is.True);
    }

    /// <summary>
    /// Tests various buffer sizes with ThrowOnBadImageBuffer.
    /// </summary>
    /// <param name="bufferSize">The size of the buffer to test.</param>
    /// <param name="shouldSucceed">Whether the validation should succeed.</param>
    /// <returns>A task representing the test.</returns>
    [TestCase(0, false)] // Empty buffer
    [TestCase(32, false)] // Too small buffer
    [TestCase(63, false)] // Just under threshold
    [TestCase(64, true)] // At threshold
    [TestCase(128, true)] // Above threshold
    [TestCase(1024, true)] // Much larger buffer
    [Test]
    public async Task ThrowOnBadImageBufferShouldHandleVariousBufferSizes(int bufferSize, bool shouldSucceed)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)(i % 256);
        }

        if (shouldSucceed)
        {
            // Act
            var result = await BitmapImageExtensions.ThrowOnBadImageBuffer(buffer)
                .Timeout(TestTimeout)
                .FirstAsync();

            // Assert
            Assert.That(result, Is.EqualTo(buffer));
        }
        else
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await BitmapImageExtensions.ThrowOnBadImageBuffer(buffer)
                .Timeout(TestTimeout)
                .FirstAsync());
        }
    }

    /// <summary>
    /// Tests that LoadImage with dimensions parameters work correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LoadImageWithDimensionsShouldAcceptParameters()
    {
        // Arrange
        await using var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        var validImageData = new byte[128];
        for (var i = 0; i < validImageData.Length; i++)
        {
            validImageData[i] = (byte)(i % 256);
        }

        const string key = "dimension_test_image";

        // Insert valid image data
        await cache.Insert(key, validImageData)
            .Timeout(TestTimeout)
            .FirstAsync();

        // Act - Load with dimensions
        var loadedBitmap = await cache.LoadImage(key, 100f, 200f)
            .Timeout(TestTimeout)
            .FirstAsync();

        // Assert
        Assert.That(loadedBitmap, Is.Not.Null);
    }

    /// <summary>
    /// Tests that SaveImage with expiration works correctly.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task SaveImageWithExpirationShouldWork()
    {
        // Arrange
        await using var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        var mockBitmap = new MockBitmap();
        const string key = "expiring_image";
        var expiration = DateTimeOffset.Now.AddMinutes(10);

        // Act
        await cache.SaveImage(key, mockBitmap, expiration)
            .Timeout(TestTimeout)
            .FirstAsync();

        // Verify it was saved (just check that no exception was thrown)
        Assert.That(true, Is.True);
    }

    /// <summary>
    /// Mock bitmap implementation for testing.
    /// </summary>
    private class MockBitmap : IBitmap
    {
        public float Width => 100;

        public float Height => 200;

        public Task Save(CompressedBitmapFormat format, float quality, Stream target)
        {
            // Produce a deterministic buffer >=64 bytes to satisfy ThrowOnBadImageBuffer
            var buffer = new byte[128];

            // PNG signature
            buffer[0] = 0x89;
            buffer[1] = 0x50;
            buffer[2] = 0x4E;
            buffer[3] = 0x47;
            buffer[4] = 0x0D;
            buffer[5] = 0x0A;
            buffer[6] = 0x1A;
            buffer[7] = 0x0A;

            for (var i = 8; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(i % 256);
            }

            return target.WriteAsync(buffer, 0, buffer.Length);
        }

        public void Dispose()
        {
            // Mock dispose
        }
    }

    /// <summary>
    /// Mock bitmap loader implementation for testing.
    /// </summary>
    private class MockBitmapLoader : IBitmapLoader
    {
        public Task<IBitmap?> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight) => Task.FromResult<IBitmap?>(new MockBitmap());

        public IBitmap Create(float width, float height) => new MockBitmap();

#pragma warning disable CA1822 // Mark members as static - Cannot be static as it implements interface
        public Task<IBitmap> LoadFromResource(string source, Assembly? assembly) => Task.FromResult<IBitmap>(new MockBitmap());

        public Task<IBitmap?> LoadFromResource(string source, float? desiredWidth, float? desiredHeight) => Task.FromResult<IBitmap?>(new MockBitmap());
#pragma warning restore CA1822 // Mark members as static
    }
}
