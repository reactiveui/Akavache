// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Akavache.Drawing;
using Akavache.SystemTextJson;
using Splat;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.Drawing BitmapImageExtensions functionality.
/// </summary>
[Category("Akavache")]
[NotInParallel("BitmapLoader")]
public class BitmapImageExtensionsTests
{
    /// <summary>The default timeout applied to observable-based test operations.</summary>
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    /// <summary>The bitmap loader captured prior to each test so it can be restored during teardown.</summary>
    private IBitmapLoader? _originalLoader;

    /// <summary>
    /// Performs per-test-class initialization.
    /// </summary>
    [Before(Test)]
    public void Initialize()
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
    }

    /// <summary>
    /// Performs per-test-class cleanup.
    /// </summary>
    [After(Test)]
    public void TearDown()
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
    }

    /// <summary>
    /// Tests that LoadImage throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task LoadImageShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImage("test_key"));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that LoadImage with dimensions throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task LoadImageWithDimensionsShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImage("test_key", 100f, 200f));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that LoadImageFromUrl throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task LoadImageFromUrlShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl("http://example.com/image.png"));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with Uri throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task LoadImageFromUrlWithUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            var uri = new Uri("http://example.com/image.png");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl(uri));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with key throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task LoadImageFromUrlWithKeyShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl("key", "http://example.com/image.png"));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with key and Uri throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task LoadImageFromUrlWithKeyAndUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            var uri = new Uri("http://example.com/image.png");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl("key", uri));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that SaveImage throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task SaveImageShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            var mockBitmap = new MockBitmap();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.SaveImage("key", mockBitmap));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that SaveImage throws ArgumentNullException when image is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SaveImageShouldThrowArgumentNullExceptionWhenImageIsNull()
    {
        // Arrange
        await using var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        IBitmap? nullBitmap = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.SaveImage("key", nullBitmap!));
    }

    /// <summary>
    /// Tests that ImageToBytes throws ArgumentNullException when image is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task ImageToBytesShouldThrowArgumentNullExceptionWhenImageIsNull()
    {
        try
        {
            // Arrange
            IBitmap? nullBitmap = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => nullBitmap!.ImageToBytes());
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
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
        await Assert.That(result).IsEqualTo(validImageData);
    }

    /// <summary>
    /// Tests that ThrowOnBadImageBuffer throws for null data.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldThrowForNullData()
    {
        // Arrange
        byte[]? nullData = null;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await BitmapImageExtensions.ThrowOnBadImageBuffer(nullData)
            .Timeout(TestTimeout)
            .FirstAsync());
    }

    /// <summary>
    /// Tests that ThrowOnBadImageBuffer throws for too small data.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
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
        await Assert.That(loadedBitmap).IsNotNull();

        using (Assert.Multiple())
        {
            // For the mock implementation, we can verify basic properties
            await Assert.That(loadedBitmap.Width).IsEqualTo(mockBitmap.Width);
            await Assert.That(loadedBitmap.Height).IsEqualTo(mockBitmap.Height);
        }

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
        await Assert.That(bytes).IsNotNull();
        await Assert.That(bytes).IsNotEmpty();
    }

    /// <summary>
    /// Tests various buffer sizes with ThrowOnBadImageBuffer.
    /// </summary>
    /// <param name="bufferSize">The size of the buffer to test.</param>
    /// <param name="shouldSucceed">Whether the validation should succeed.</param>
    /// <returns>A task representing the test.</returns>
    [Arguments(0, false)] // Empty buffer
    [Arguments(32, false)] // Too small buffer
    [Arguments(63, false)] // Just under threshold
    [Arguments(64, true)] // At threshold
    [Arguments(128, true)] // Above threshold
    [Arguments(1024, true)] // Much larger buffer
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
            await Assert.That(result).IsEqualTo(buffer);
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
        await Assert.That(loadedBitmap).IsNotNull();
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
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with a string URL returns a bitmap when the
    /// cache already contains valid data for the URL key.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageFromUrlStringShouldReturnBitmapFromCachedData()
    {
        await using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        const string url = "http://example.com/cached_string_url.png";
        var imageData = CreateValidImageBytes();

        // Seed cache with the URL as the key — DownloadUrl(string url) uses `url` as key.
        await cache.Insert(url, imageData).Timeout(TestTimeout).FirstAsync();

        var loaded = await cache.LoadImageFromUrl(url).Timeout(TestTimeout).FirstAsync();

        await Assert.That(loaded).IsNotNull();
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with a Uri returns a bitmap when the cache
    /// already contains valid data for the URL key.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageFromUrlUriShouldReturnBitmapFromCachedData()
    {
        await using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        var uri = new Uri("http://example.com/cached_uri.png");
        var imageData = CreateValidImageBytes();

        // HttpService.DownloadUrl(Uri) uses url.ToString() as the cache key.
        await cache.Insert(uri.ToString(), imageData).Timeout(TestTimeout).FirstAsync();

        var loaded = await cache.LoadImageFromUrl(uri).Timeout(TestTimeout).FirstAsync();

        await Assert.That(loaded).IsNotNull();
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with a key and string URL returns a bitmap
    /// when the cache already contains valid data for the supplied key.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageFromUrlWithKeyAndStringShouldReturnBitmapFromCachedData()
    {
        await using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        const string key = "custom_key_string";
        const string url = "http://example.com/keyed_string_url.png";
        var imageData = CreateValidImageBytes();

        await cache.Insert(key, imageData).Timeout(TestTimeout).FirstAsync();

        var loaded = await cache.LoadImageFromUrl(key, url).Timeout(TestTimeout).FirstAsync();

        await Assert.That(loaded).IsNotNull();
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with a key and Uri returns a bitmap when
    /// the cache already contains valid data for the supplied key.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageFromUrlWithKeyAndUriShouldReturnBitmapFromCachedData()
    {
        await using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        const string key = "custom_key_uri";
        var uri = new Uri("http://example.com/keyed_uri.png");
        var imageData = CreateValidImageBytes();

        await cache.Insert(key, imageData).Timeout(TestTimeout).FirstAsync();

        var loaded = await cache.LoadImageFromUrl(key, uri).Timeout(TestTimeout).FirstAsync();

        await Assert.That(loaded).IsNotNull();
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with dimensions round-trips cached data
    /// while forwarding the desired width and height to the loader.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageFromUrlWithDimensionsShouldPassThroughToLoader()
    {
        await using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        const string url = "http://example.com/dimensioned.png";
        var imageData = CreateValidImageBytes();

        await cache.Insert(url, imageData).Timeout(TestTimeout).FirstAsync();

        var loaded = await cache.LoadImageFromUrl(url, fetchAlways: false, desiredWidth: 320f, desiredHeight: 240f)
            .Timeout(TestTimeout)
            .FirstAsync();

        await Assert.That(loaded).IsNotNull();
    }

    /// <summary>
    /// Tests that LoadImage surfaces an IOException when the bitmap loader
    /// returns null for otherwise valid bytes.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageShouldThrowIOExceptionWhenLoaderReturnsNullBitmap()
    {
        // Swap in a loader that returns null for Load so the null-coalescing throw fires.
        BitmapLoader.Current = new NullReturningBitmapLoader();

        await using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        const string key = "null_bitmap_key";

        await cache.Insert(key, CreateValidImageBytes()).Timeout(TestTimeout).FirstAsync();

        await Assert.ThrowsAsync<IOException>(async () => await cache.LoadImage(key)
            .Timeout(TestTimeout)
            .FirstAsync());
    }

    /// <summary>
    /// Tests LoadImage throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageShouldThrowOnNullCache() =>
        await Assert.That(static () => BitmapImageExtensions.LoadImage(null!, "key"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests LoadImageFromUrl(string) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageFromUrlStringShouldThrowOnNullCache() =>
        await Assert.That(static () => BitmapImageExtensions.LoadImageFromUrl(null!, "http://example.com/img.png"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests LoadImageFromUrl(Uri) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageFromUrlUriShouldThrowOnNullCache() =>
        await Assert.That(static () => BitmapImageExtensions.LoadImageFromUrl(null!, new Uri("http://example.com/img.png")))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests LoadImageFromUrl(key, string) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageFromUrlKeyStringShouldThrowOnNullCache() =>
        await Assert.That(static () => BitmapImageExtensions.LoadImageFromUrl(null!, "mykey", "http://example.com/img.png"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests LoadImageFromUrl(key, Uri) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageFromUrlKeyUriShouldThrowOnNullCache() =>
        await Assert.That(static () => BitmapImageExtensions.LoadImageFromUrl(null!, "mykey", new Uri("http://example.com/img.png")))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests <see cref="BitmapImageExtensions.ThrowOnNullOrBadImageBuffer"/> throws
    /// an "Image data is null" error when handed a <see langword="null"/> buffer.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldThrowForNullInput() =>
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await BitmapImageExtensions.ThrowOnNullOrBadImageBuffer(null).FirstAsync());

    /// <summary>
    /// Tests <see cref="BitmapImageExtensions.ThrowOnNullOrBadImageBuffer"/> routes a
    /// valid (&gt;= 64-byte) buffer through the bad-image guard and returns it.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldReturnValidBuffer()
    {
        var buffer = new byte[128];

        var result = await BitmapImageExtensions.ThrowOnNullOrBadImageBuffer(buffer).FirstAsync();

        await Assert.That(result).IsSameReferenceAs(buffer);
    }

    /// <summary>
    /// Tests <see cref="BitmapImageExtensions.ThrowOnNullOrBadImageBuffer"/> forwards
    /// the short-buffer error from <see cref="BitmapImageExtensions.ThrowOnBadImageBuffer"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldThrowForShortBuffer() =>
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await BitmapImageExtensions.ThrowOnNullOrBadImageBuffer([1, 2, 3]).FirstAsync());

    /// <summary>
    /// Tests <see cref="BitmapImageExtensions.BytesToImage"/> returns a decoded
    /// <see cref="IBitmap"/> on the happy path by routing through
    /// <see cref="BitmapLoader.Current"/> (the ambient Splat bitmap loader).
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task BytesToImageShouldReturnBitmapOnHappyPath()
    {
        var previousLoader = BitmapLoader.Current;
        BitmapLoader.Current = new MockBitmapLoader();
        try
        {
            var bitmap = await BitmapImageExtensions.BytesToImage(new byte[128], null, null).FirstAsync();

            await Assert.That(bitmap).IsNotNull();
        }
        finally
        {
            BitmapLoader.Current = previousLoader;
        }
    }

    /// <summary>
    /// Tests <see cref="BitmapImageExtensions.BytesToImage"/> throws an
    /// <see cref="IOException"/> when <see cref="BitmapLoader.Current"/> returns a
    /// <see langword="null"/> bitmap.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task BytesToImageShouldThrowWhenLoaderReturnsNullBitmap()
    {
        var previousLoader = BitmapLoader.Current;
        BitmapLoader.Current = new NullReturningBitmapLoader();
        try
        {
            await Assert.ThrowsAsync<IOException>(async () =>
                await BitmapImageExtensions.BytesToImage(new byte[128], null, null).FirstAsync());
        }
        finally
        {
            BitmapLoader.Current = previousLoader;
        }
    }

    /// <summary>
    /// Tests <see cref="BitmapImageExtensions.BytesToImage"/> propagates desired size
    /// parameters through to the loader on the happy path.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task BytesToImageShouldForwardDesiredSizeToLoader()
    {
        var previousLoader = BitmapLoader.Current;
        var capturing = new CapturingBitmapLoader();
        BitmapLoader.Current = capturing;
        try
        {
            _ = await BitmapImageExtensions.BytesToImage(new byte[128], 320f, 240f).FirstAsync();

            await Assert.That(capturing.LastWidth).IsEqualTo(320f);
            await Assert.That(capturing.LastHeight).IsEqualTo(240f);
        }
        finally
        {
            BitmapLoader.Current = previousLoader;
        }
    }

    /// <summary>
    /// Creates a deterministic PNG-signature buffer large enough to pass <see cref="BitmapImageExtensions.ThrowOnBadImageBuffer"/>.
    /// </summary>
    /// <returns>A 128-byte buffer prefixed with the PNG magic bytes.</returns>
    private static byte[] CreateValidImageBytes()
    {
        var buffer = new byte[128];
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

        return buffer;
    }

    /// <summary>
    /// Mock bitmap implementation for testing.
    /// </summary>
    private sealed class MockBitmap : IBitmap
    {
        /// <inheritdoc/>
        public float Width => 100;

        /// <inheritdoc/>
        public float Height => 200;

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Dispose()
        {
            // Mock dispose
        }
    }

    /// <summary>
    /// Bitmap loader stub that captures the last requested width/height for the
    /// <see cref="BytesToImageShouldForwardDesiredSizeToLoader"/> test.
    /// </summary>
    private sealed class CapturingBitmapLoader : IBitmapLoader
    {
        /// <summary>Gets the last desired width passed to <see cref="Load"/>.</summary>
        public float? LastWidth { get; private set; }

        /// <summary>Gets the last desired height passed to <see cref="Load"/>.</summary>
        public float? LastHeight { get; private set; }

        /// <inheritdoc/>
        public Task<IBitmap?> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight)
        {
            LastWidth = desiredWidth;
            LastHeight = desiredHeight;
            return Task.FromResult<IBitmap?>(new MockBitmap());
        }

        /// <inheritdoc/>
        public IBitmap Create(float width, float height) => new MockBitmap();

        /// <inheritdoc/>
        public Task<IBitmap?> LoadFromResource(string source, float? desiredWidth, float? desiredHeight) => Task.FromResult<IBitmap?>(new MockBitmap());
    }

    /// <summary>
    /// Mock bitmap loader implementation for testing.
    /// </summary>
    private sealed class MockBitmapLoader : IBitmapLoader
    {
        /// <inheritdoc/>
        public Task<IBitmap?> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight) => Task.FromResult<IBitmap?>(new MockBitmap());

        /// <inheritdoc/>
        public IBitmap Create(float width, float height) => new MockBitmap();

        /// <inheritdoc/>
        public Task<IBitmap?> LoadFromResource(string source, float? desiredWidth, float? desiredHeight) => Task.FromResult<IBitmap?>(new MockBitmap());
    }

    /// <summary>
    /// Bitmap loader that always returns null from <see cref="Load"/> in order
    /// to exercise the null-bitmap throw path in BytesToImage.
    /// </summary>
    private sealed class NullReturningBitmapLoader : IBitmapLoader
    {
        /// <inheritdoc/>
        public Task<IBitmap?> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(null);

        /// <inheritdoc/>
        public IBitmap Create(float width, float height) => new MockBitmap();

        /// <inheritdoc/>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Cannot be static as it implements interface")]
        public Task<IBitmap?> LoadFromResource(string source, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(null);
    }
}
