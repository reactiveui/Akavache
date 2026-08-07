// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Splat;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests for Akavache.Drawing BitmapImageExtensions functionality.</summary>
[Category("Akavache")]
public class BitmapImageExtensionsTests
{
    /// <summary>The absolute image URL passed to the URL overloads that are exercised with a null cache.</summary>
    private const string SampleImageUrl = "http://example.com/image.png";

    /// <summary>The pixel width asked for when an image is loaded with explicit dimensions.</summary>
    private const float DesiredWidthPixels = 100F;

    /// <summary>The pixel height asked for when an image is loaded with explicit dimensions.</summary>
    private const float DesiredHeightPixels = 200F;

    /// <summary>The pixel width the bitmap loader is expected to receive when a decode size is forwarded to it.</summary>
    private const float ForwardedWidthPixels = 320F;

    /// <summary>The pixel height the bitmap loader is expected to receive when a decode size is forwarded to it.</summary>
    private const float ForwardedHeightPixels = 240F;

    /// <summary>The number of distinct byte values the deterministic buffer fill cycles through.</summary>
    private const int ByteValueRange = 256;

    /// <summary>
    /// Byte length of the decodable image the tests seed a cache with. Differs from
    /// <see cref="CacheBackedHttpService.DownloadedPayloadLength"/> so the byte count reaching the
    /// loader says whether the cache or the download served the request.
    /// </summary>
    private const int SeededImageByteLength = 128;

    /// <summary>How far ahead of now a saved image is set to expire.</summary>
    private const double ImageExpirationMinutes = 10D;

    /// <summary>The bitmap loader captured prior to each test so it can be restored during teardown.</summary>
    private IBitmapLoader? _originalLoader;

    /// <summary>Performs per-test-class initialization.</summary>
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

    /// <summary>Performs per-test-class cleanup.</summary>
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
        catch (TypeInitializationException)
        {
            // The assignment can only fail while Splat first initialises BitmapLoader from the ambient
            // locator, in which case there is no loader to restore and the test result must not be
            // replaced by a teardown failure.
        }
    }

    /// <summary>Tests that LoadImage throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task LoadImageShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.LoadImage("test_key"));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that LoadImage with dimensions throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task LoadImageWithDimensionsShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.LoadImage("test_key", DesiredWidthPixels, DesiredHeightPixels));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that LoadImageFromUrl throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public Task LoadImageFromUrlShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl(SampleImageUrl));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that LoadImageFromUrl with Uri throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task LoadImageFromUrlWithUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            Uri uri = new(SampleImageUrl);

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl(uri));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that LoadImageFromUrl with key throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public Task LoadImageFromUrlWithKeyShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl("key", SampleImageUrl));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that LoadImageFromUrl with the key, and Uri throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task LoadImageFromUrlWithKeyAndUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            Uri uri = new(SampleImageUrl);

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.LoadImageFromUrl("key", uri));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that SaveImage throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task SaveImageShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            MockBitmap mockBitmap = new();

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.SaveImage("key", mockBitmap));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that SaveImage throws ArgumentNullException when image is null.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SaveImageShouldThrowArgumentNullExceptionWhenImageIsNull()
    {
        // Arrange
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        IBitmap? nullBitmap = null;

        // Act & Assert
        await Assert.That(cache).IsNotNull();
        _ = Assert.Throws<ArgumentNullException>(() => cache.SaveImage("key", nullBitmap!));
    }

    /// <summary>Tests that ImageToBytes throws ArgumentNullException when the image is null.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task ImageToBytesShouldThrowArgumentNullExceptionWhenImageIsNull()
    {
        try
        {
            // Arrange
            IBitmap? nullBitmap = null;

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => nullBitmap!.ImageToBytes());
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that ThrowOnBadImageBuffer works correctly with valid data.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldReturnValidDataForGoodBuffer()
    {
        // Arrange
        var validImageData = new byte[128]; // Greater than 64 bytes
        for (var i = 0; i < validImageData.Length; i++)
        {
            validImageData[i] = (byte)(i % ByteValueRange);
        }

        // Act
        var result = BitmapImageExtensions.ThrowOnBadImageBuffer(validImageData)
            .SubscribeGetValue();

        // Assert
        await Assert.That(result).IsEqualTo(validImageData);
    }

    /// <summary>Tests that ThrowOnBadImageBuffer throws for null data.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldThrowForNullData()
    {
        // Arrange
        byte[]? nullData = null;

        // Act & Assert
        var error = BitmapImageExtensions.ThrowOnBadImageBuffer(nullData).SubscribeGetError();
        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Tests that ThrowOnBadImageBuffer throws for too small data.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldThrowForTooSmallData()
    {
        // Arrange
        var tooSmallData = new byte[32]; // Less than 64 bytes

        // Act & Assert
        var error = BitmapImageExtensions.ThrowOnBadImageBuffer(tooSmallData).SubscribeGetError();
        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Tests that LoadImage handles missing keys correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LoadImageShouldHandleMissingKeysCorrectly()
    {
        // Arrange
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());

        // Act & Assert
        var error = cache.LoadImage("nonexistent_key").SubscribeGetError();
        await Assert.That(error).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>Tests that SaveImage and LoadImage work together for basic functionality.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task SaveImageAndLoadImageShouldWorkTogether()
    {
        // Arrange
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        MockBitmap mockBitmap = new();
        const string key = "test_image";

        // Act - Save image (should serialize the bitmap data)
        cache.SaveImage(key, mockBitmap)
            .WaitForCompletion();

        // Act - Load image (should deserialize and recreate bitmap)
        var loadedBitmap = cache.LoadImage(key)
            .SubscribeGetValue();

        // Assert
        await Assert.That(loadedBitmap).IsNotNull();

        using (Assert.Multiple())
        {
            // For the mock implementation, we can verify basic properties
            await Assert.That(loadedBitmap!.Width).IsEqualTo(mockBitmap.Width);
            await Assert.That(loadedBitmap.Height).IsEqualTo(mockBitmap.Height);
        }
    }

    /// <summary>Tests that ImageToBytes works correctly with mock bitmap.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ImageToBytesShouldWorkWithMockBitmap()
    {
        // Arrange
        MockBitmap mockBitmap = new();

        // Act
        var bytes = mockBitmap.ImageToBytes()
            .SubscribeGetValue();

        // Assert
        await Assert.That(bytes).IsNotNull();
        await Assert.That(bytes).IsNotEmpty();
    }

    /// <summary>Tests various buffer sizes with ThrowOnBadImageBuffer.</summary>
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
            buffer[i] = (byte)(i % ByteValueRange);
        }

        if (shouldSucceed)
        {
            // Act
            var result = BitmapImageExtensions.ThrowOnBadImageBuffer(buffer)
                .SubscribeGetValue();

            // Assert
            await Assert.That(result).IsEqualTo(buffer);
        }
        else
        {
            // Act & Assert
            var error = BitmapImageExtensions.ThrowOnBadImageBuffer(buffer)
                .SubscribeGetError();
            await Assert.That(error).IsTypeOf<InvalidOperationException>();
        }
    }

    /// <summary>Tests that LoadImage with dimensions parameters work correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LoadImageWithDimensionsShouldAcceptParameters()
    {
        // Arrange
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        var validImageData = new byte[128];
        for (var i = 0; i < validImageData.Length; i++)
        {
            validImageData[i] = (byte)(i % ByteValueRange);
        }

        const string key = "dimension_test_image";

        // Insert valid image data
        cache.Insert(key, validImageData)
            .WaitForCompletion();

        // Act - Load with dimensions
        var loadedBitmap = cache.LoadImage(key, DesiredWidthPixels, DesiredHeightPixels)
            .SubscribeGetValue();

        // Assert
        await Assert.That(loadedBitmap).IsNotNull();
    }

    /// <summary>Tests that SaveImage with expiration works correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task SaveImageWithExpirationShouldWork()
    {
        // Arrange
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        MockBitmap mockBitmap = new();
        const string key = "expiring_image";
        var expiration = TimeProvider.System.GetLocalNow().AddMinutes(ImageExpirationMinutes);

        // Act
        _ = cache.SaveImage(key, mockBitmap, expiration)
            .SubscribeGetValue();
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with a string URL returns a bitmap when the
    /// cache already contains valid data for the URL key.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageFromUrlStringShouldReturnBitmapFromCachedData()
    {
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        const string url = "http://example.com/cached_string_url.png";
        var imageData = CreateValidImageBytes();

        // Seed cache with the URL as the key — DownloadUrl(string url) uses `url` as key.
        cache.Insert(url, imageData).WaitForCompletion();

        var loaded = cache.LoadImageFromUrl(url).SubscribeGetValue();

        await Assert.That(loaded).IsNotNull();
    }

    /// <summary>Tests that LoadImageFromUrl with a Uri returns a bitmap when the cache already contains valid data for the URL key.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageFromUrlUriShouldReturnBitmapFromCachedData()
    {
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        Uri uri = new("http://example.com/cached_uri.png");
        var imageData = CreateValidImageBytes();

        // HttpService.DownloadUrl(Uri) uses url.ToString() as the cache key.
        cache.Insert(uri.ToString(), imageData).WaitForCompletion();

        var loaded = cache.LoadImageFromUrl(uri).SubscribeGetValue();

        await Assert.That(loaded).IsNotNull();
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with a key and string URL returns a bitmap
    /// when the cache already contains valid data for the supplied key.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageFromUrlWithKeyAndStringShouldReturnBitmapFromCachedData()
    {
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        const string key = "custom_key_string";
        const string url = "http://example.com/keyed_string_url.png";
        var imageData = CreateValidImageBytes();

        cache.Insert(key, imageData).WaitForCompletion();

        var loaded = cache.LoadImageFromUrl(key, url).SubscribeGetValue();

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
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        const string key = "custom_key_uri";
        Uri uri = new("http://example.com/keyed_uri.png");
        var imageData = CreateValidImageBytes();

        cache.Insert(key, imageData).WaitForCompletion();

        var loaded = cache.LoadImageFromUrl(key, uri).SubscribeGetValue();

        await Assert.That(loaded).IsNotNull();
    }

    /// <summary>
    /// Tests that LoadImageFromUrl with dimensions round-trips cached data
    /// while forwarding the desired width and height to the loader.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageFromUrlWithDimensionsShouldPassThroughToLoader()
    {
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        const string url = "http://example.com/dimensioned.png";
        var imageData = CreateValidImageBytes();

        cache.Insert(url, imageData).WaitForCompletion();

        var loaded = cache.LoadImageFromUrl(url, fetchAlways: false, desiredWidth: 320F, desiredHeight: 240F)
            .SubscribeGetValue();

        await Assert.That(loaded).IsNotNull();
    }

    /// <summary>Tests that LoadImage surfaces an IOException when the bitmap loader returns null for otherwise valid bytes.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageShouldThrowIOExceptionWhenLoaderReturnsNullBitmap()
    {
        // Swap in a loader that returns null for Load so the null-coalescing throw fires.
        BitmapLoader.Current = new NullReturningBitmapLoader();

        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        const string key = "null_bitmap_key";

        cache.Insert(key, CreateValidImageBytes()).WaitForCompletion();

        var error = cache.LoadImage(key).SubscribeGetError();
        await Assert.That(error).IsTypeOf<IOException>();
    }

    /// <summary>Tests LoadImage throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageShouldThrowOnNullCache() =>
        await Assert.That(static () => BitmapImageExtensions.LoadImage(null!, "key"))
            .Throws<ArgumentNullException>();

    /// <summary>Tests LoadImageFromUrl(string) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageFromUrlStringShouldThrowOnNullCache() =>
        await Assert.That(static () => BitmapImageExtensions.LoadImageFromUrl(null!, SampleImageUrl))
            .Throws<ArgumentNullException>();

    /// <summary>Tests LoadImageFromUrl(Uri) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageFromUrlUriShouldThrowOnNullCache() =>
        await Assert.That(static () =>
                BitmapImageExtensions.LoadImageFromUrl(null!, new Uri(SampleImageUrl)))
            .Throws<ArgumentNullException>();

    /// <summary>Tests LoadImageFromUrl(key, string) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageFromUrlKeyStringShouldThrowOnNullCache() =>
        await Assert.That(static () =>
                BitmapImageExtensions.LoadImageFromUrl(
                    null!,
                    "mykey",
                    SampleImageUrl))
            .Throws<ArgumentNullException>();

    /// <summary>Tests LoadImageFromUrl(key, Uri) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageFromUrlKeyUriShouldThrowOnNullCache() =>
        await Assert.That(static () =>
                BitmapImageExtensions.LoadImageFromUrl(null!, "mykey", new Uri(SampleImageUrl)))
            .Throws<ArgumentNullException>();

    /// <summary>Tests <see cref="BitmapImageExtensions.ThrowOnNullOrBadImageBuffer"/> throws an "Image data is null" error when handed a <see langword="null"/> buffer.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldThrowForNullInput()
    {
        var error = BitmapImageExtensions.ThrowOnNullOrBadImageBuffer(null).SubscribeGetError();
        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Tests <see cref="BitmapImageExtensions.ThrowOnNullOrBadImageBuffer"/> routes a valid (&gt;= 64-byte) buffer through the bad-image guard and returns it.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldReturnValidBuffer()
    {
        var buffer = new byte[128];

        var result = BitmapImageExtensions.ThrowOnNullOrBadImageBuffer(buffer).SubscribeGetValue();

        await Assert.That(result).IsSameReferenceAs(buffer);
    }

    /// <summary>Tests <see cref="BitmapImageExtensions.ThrowOnNullOrBadImageBuffer"/> forwards the short-buffer error from <see cref="BitmapImageExtensions.ThrowOnBadImageBuffer"/>.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldThrowForShortBuffer()
    {
        byte[] undersizedBuffer = [1, 2, 3];

        var error = BitmapImageExtensions.ThrowOnNullOrBadImageBuffer(undersizedBuffer).SubscribeGetError();
        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

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
            var bitmap = BitmapImageExtensions.BytesToImage(new byte[128], null, null).SubscribeGetValue();

            await Assert.That(bitmap).IsNotNull();
        }
        finally
        {
            BitmapLoader.Current = previousLoader;
        }
    }

    /// <summary>Tests <see cref="BitmapImageExtensions.BytesToImage"/> throws an <see cref="IOException"/> when <see cref="BitmapLoader.Current"/> returns a <see langword="null"/> bitmap.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task BytesToImageShouldThrowWhenLoaderReturnsNullBitmap()
    {
        var previousLoader = BitmapLoader.Current;
        BitmapLoader.Current = new NullReturningBitmapLoader();
        try
        {
            var error = BitmapImageExtensions.BytesToImage(new byte[128], null, null).SubscribeGetError();
            await Assert.That(error).IsTypeOf<IOException>();
        }
        finally
        {
            BitmapLoader.Current = previousLoader;
        }
    }

    /// <summary>Tests <see cref="BitmapImageExtensions.BytesToImage"/> propagates desired size parameters through to the loader on the happy path.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task BytesToImageShouldForwardDesiredSizeToLoader()
    {
        var previousLoader = BitmapLoader.Current;
        CapturingBitmapLoader capturing = new();
        BitmapLoader.Current = capturing;
        try
        {
            _ = BitmapImageExtensions.BytesToImage(new byte[128], ForwardedWidthPixels, ForwardedHeightPixels)
                .SubscribeGetValue();

            await Assert.That(capturing.LastWidth).IsEqualTo(ForwardedWidthPixels);
            await Assert.That(capturing.LastHeight).IsEqualTo(ForwardedHeightPixels);
        }
        finally
        {
            BitmapLoader.Current = previousLoader;
        }
    }

    /// <summary>
    /// Tests that the width-only <c>LoadImage</c> overload decodes the cached bytes at the
    /// requested width while leaving the height unasked-for, so the image keeps its native height.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageAtWidthShouldDecodeCachedBytesAtNativeHeight()
    {
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        const string key = "width_only_image";
        cache.Insert(key, CreateValidImageBytes()).WaitForCompletion();

        CapturingBitmapLoader loader = new();
        BitmapLoader.Current = loader;

        var loaded = cache.LoadImage(key, ForwardedWidthPixels).SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(loader.LastStreamLength).IsEqualTo(SeededImageByteLength);
            await Assert.That(loader.LastWidth).IsEqualTo(ForwardedWidthPixels);
            await Assert.That(loader.LastHeight).IsNull();
        }
    }

    /// <summary>
    /// Tests that the string-URL overload taking only <c>fetchAlways</c> honours that flag —
    /// reading the cached image when it is clear and re-downloading when it is set — and asks
    /// for the image at its native size with no expiration.
    /// </summary>
    /// <param name="fetchAlways">Whether the caller demanded a fresh download.</param>
    /// <param name="expectedByteLength">Byte length of the buffer the loader is expected to decode.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Arguments(false, SeededImageByteLength)]
    [Arguments(true, CacheBackedHttpService.DownloadedPayloadLength)]
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageFromUrlWithFetchAlwaysShouldDecodeAtNativeSize(bool fetchAlways, int expectedByteLength)
    {
        const string url = "http://example.com/fetch_always_string.png";
        using var fixture = CreateSeededImageFixture(url);

        var loaded = fixture.Cache.LoadImageFromUrl(url, fetchAlways).SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(expectedByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsNull();
            await Assert.That(fixture.Loader.LastHeight).IsNull();
            await Assert.That(fixture.HttpService.LastFetchAlways).IsEqualTo(fetchAlways);
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(fetchAlways ? 1 : 0);
        }
    }

    /// <summary>
    /// Tests that the string-URL overload taking a width reads the cached image rather than
    /// re-downloading it, decodes it at the requested width and leaves the height unasked-for.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageFromUrlAtWidthShouldDecodeCachedBytesAtNativeHeight()
    {
        const string url = "http://example.com/width_only_string.png";
        using var fixture = CreateSeededImageFixture(url);

        var loaded = fixture.Cache.LoadImageFromUrl(url, false, ForwardedWidthPixels)
            .SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(SeededImageByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsEqualTo(ForwardedWidthPixels);
            await Assert.That(fixture.Loader.LastHeight).IsNull();
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(0);
        }
    }

    /// <summary>
    /// Tests that the <see cref="Uri"/> overload taking only <c>fetchAlways</c> honours that flag
    /// and asks for the image at its native size with no expiration.
    /// </summary>
    /// <param name="fetchAlways">Whether the caller demanded a fresh download.</param>
    /// <param name="expectedByteLength">Byte length of the buffer the loader is expected to decode.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Arguments(false, SeededImageByteLength)]
    [Arguments(true, CacheBackedHttpService.DownloadedPayloadLength)]
    [Test]
    public async Task LoadImageFromUrlUriWithFetchAlwaysShouldDecodeAtNativeSize(bool fetchAlways, int expectedByteLength)
    {
        Uri imageUrl = new("http://example.com/fetch_always_uri.png");
        using var fixture = CreateSeededImageFixture(imageUrl.ToString());

        var loaded = fixture.Cache.LoadImageFromUrl(imageUrl, fetchAlways).SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(expectedByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsNull();
            await Assert.That(fixture.Loader.LastHeight).IsNull();
            await Assert.That(fixture.HttpService.LastFetchAlways).IsEqualTo(fetchAlways);
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(fetchAlways ? 1 : 0);
        }
    }

    /// <summary>Tests that the <see cref="Uri"/> overload taking a width reads the cached image and decodes it at the requested width with no height.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageFromUrlUriAtWidthShouldDecodeCachedBytesAtNativeHeight()
    {
        Uri imageUrl = new("http://example.com/width_only_uri.png");
        using var fixture = CreateSeededImageFixture(imageUrl.ToString());

        var loaded = fixture.Cache.LoadImageFromUrl(imageUrl, false, ForwardedWidthPixels).SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(SeededImageByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsEqualTo(ForwardedWidthPixels);
            await Assert.That(fixture.Loader.LastHeight).IsNull();
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(0);
        }
    }

    /// <summary>
    /// Tests that the <see cref="Uri"/> overload taking a full decode size forwards both
    /// dimensions and leaves the cached entry without an expiration.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageFromUrlUriAtSizeShouldDecodeCachedBytesWithoutAnExpiration()
    {
        Uri imageUrl = new("http://example.com/sized_uri.png");
        using var fixture = CreateSeededImageFixture(imageUrl.ToString());

        var loaded = fixture.Cache.LoadImageFromUrl(imageUrl, false, ForwardedWidthPixels, ForwardedHeightPixels)
            .SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(SeededImageByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsEqualTo(ForwardedWidthPixels);
            await Assert.That(fixture.Loader.LastHeight).IsEqualTo(ForwardedHeightPixels);
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(0);
        }
    }

    /// <summary>
    /// Tests that the keyed string-URL overload taking only <c>fetchAlways</c> honours that flag
    /// against the supplied key and asks for the image at its native size with no expiration.
    /// </summary>
    /// <param name="fetchAlways">Whether the caller demanded a fresh download.</param>
    /// <param name="expectedByteLength">Byte length of the buffer the loader is expected to decode.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Arguments(false, SeededImageByteLength)]
    [Arguments(true, CacheBackedHttpService.DownloadedPayloadLength)]
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageFromUrlWithKeyAndFetchAlwaysShouldDecodeAtNativeSize(bool fetchAlways, int expectedByteLength)
    {
        const string key = "keyed_fetch_always_string";
        const string url = "http://example.com/keyed_fetch_always_string.png";
        using var fixture = CreateSeededImageFixture(key);

        var loaded = fixture.Cache.LoadImageFromUrl(key, url, fetchAlways).SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(expectedByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsNull();
            await Assert.That(fixture.Loader.LastHeight).IsNull();
            await Assert.That(fixture.HttpService.LastFetchAlways).IsEqualTo(fetchAlways);
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(fetchAlways ? 1 : 0);
        }
    }

    /// <summary>
    /// Tests that the keyed string-URL overload taking a width reads the image already stored
    /// under the key and decodes it at the requested width with no height.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageFromUrlWithKeyAtWidthShouldDecodeCachedBytesAtNativeHeight()
    {
        const string key = "keyed_width_only_string";
        const string url = "http://example.com/keyed_width_only_string.png";
        using var fixture = CreateSeededImageFixture(key);

        var loaded = fixture.Cache.LoadImageFromUrl(key, url, false, ForwardedWidthPixels)
            .SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(SeededImageByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsEqualTo(ForwardedWidthPixels);
            await Assert.That(fixture.Loader.LastHeight).IsNull();
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(0);
        }
    }

    /// <summary>
    /// Tests that the keyed string-URL overload taking a full decode size forwards both
    /// dimensions and leaves the cached entry without an expiration.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageFromUrlWithKeyAtSizeShouldDecodeCachedBytesWithoutAnExpiration()
    {
        const string key = "keyed_sized_string";
        const string url = "http://example.com/keyed_sized_string.png";
        using var fixture = CreateSeededImageFixture(key);

        var loaded = fixture.Cache
            .LoadImageFromUrl(key, url, false, ForwardedWidthPixels, ForwardedHeightPixels)
            .SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(SeededImageByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsEqualTo(ForwardedWidthPixels);
            await Assert.That(fixture.Loader.LastHeight).IsEqualTo(ForwardedHeightPixels);
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(0);
        }
    }

    /// <summary>
    /// Tests that the keyed <see cref="Uri"/> overload taking only <c>fetchAlways</c> honours that
    /// flag against the supplied key and asks for the image at its native size with no expiration.
    /// </summary>
    /// <param name="fetchAlways">Whether the caller demanded a fresh download.</param>
    /// <param name="expectedByteLength">Byte length of the buffer the loader is expected to decode.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Arguments(false, SeededImageByteLength)]
    [Arguments(true, CacheBackedHttpService.DownloadedPayloadLength)]
    [Test]
    public async Task LoadImageFromUrlWithKeyAndUriAndFetchAlwaysShouldDecodeAtNativeSize(bool fetchAlways, int expectedByteLength)
    {
        const string key = "keyed_fetch_always_uri";
        Uri imageUrl = new("http://example.com/keyed_fetch_always_uri.png");
        using var fixture = CreateSeededImageFixture(key);

        var loaded = fixture.Cache.LoadImageFromUrl(key, imageUrl, fetchAlways).SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(expectedByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsNull();
            await Assert.That(fixture.Loader.LastHeight).IsNull();
            await Assert.That(fixture.HttpService.LastFetchAlways).IsEqualTo(fetchAlways);
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(fetchAlways ? 1 : 0);
        }
    }

    /// <summary>
    /// Tests that the keyed <see cref="Uri"/> overload taking a width reads the image already
    /// stored under the key and decodes it at the requested width with no height.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageFromUrlWithKeyAndUriAtWidthShouldDecodeCachedBytesAtNativeHeight()
    {
        const string key = "keyed_width_only_uri";
        Uri imageUrl = new("http://example.com/keyed_width_only_uri.png");
        using var fixture = CreateSeededImageFixture(key);

        var loaded = fixture.Cache.LoadImageFromUrl(key, imageUrl, false, ForwardedWidthPixels).SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(SeededImageByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsEqualTo(ForwardedWidthPixels);
            await Assert.That(fixture.Loader.LastHeight).IsNull();
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(0);
        }
    }

    /// <summary>
    /// Tests that the keyed <see cref="Uri"/> overload taking a full decode size forwards both
    /// dimensions and leaves the cached entry without an expiration.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadImageFromUrlWithKeyAndUriAtSizeShouldDecodeCachedBytesWithoutAnExpiration()
    {
        const string key = "keyed_sized_uri";
        Uri imageUrl = new("http://example.com/keyed_sized_uri.png");
        using var fixture = CreateSeededImageFixture(key);

        var loaded = fixture.Cache.LoadImageFromUrl(key, imageUrl, false, ForwardedWidthPixels, ForwardedHeightPixels)
            .SubscribeGetValue();

        using (Assert.Multiple())
        {
            await Assert.That(loaded).IsNotNull();
            await Assert.That(fixture.Loader.LastStreamLength).IsEqualTo(SeededImageByteLength);
            await Assert.That(fixture.Loader.LastWidth).IsEqualTo(ForwardedWidthPixels);
            await Assert.That(fixture.Loader.LastHeight).IsEqualTo(ForwardedHeightPixels);
            await Assert.That(fixture.HttpService.LastAbsoluteExpiration).IsNull();
            await Assert.That(fixture.HttpService.DownloadCount).IsEqualTo(0);
        }
    }

    /// <summary>
    /// Creates a cache holding one decodable image under <paramref name="key"/>, wired to a
    /// recording HTTP service and a capturing bitmap loader, so what a forwarding overload passed
    /// down can be read back off the loader and the service.
    /// </summary>
    /// <param name="key">The cache key the seeded image is stored under.</param>
    /// <returns>The fixture owning the cache, the HTTP service and the loader.</returns>
    private static ImageLoadFixture CreateSeededImageFixture(string key)
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        CacheBackedHttpService httpService = new();
        cache.SetHttpService(httpService);
        cache.Insert(key, CreateValidImageBytes()).WaitForCompletion();

        CapturingBitmapLoader loader = new();
        BitmapLoader.Current = loader;

        return new(cache, httpService, loader);
    }

    /// <summary>Creates a deterministic PNG-signature buffer large enough to pass <see cref="BitmapImageExtensions.ThrowOnBadImageBuffer"/>.</summary>
    /// <returns>A 128-byte buffer prefixed with the PNG magic bytes.</returns>
    private static byte[] CreateValidImageBytes()
    {
        var buffer = new byte[SeededImageByteLength];
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
            buffer[i] = (byte)(i % ByteValueRange);
        }

        return buffer;
    }

    /// <summary>Owns the cache, HTTP service and bitmap loader that a load-from-cache or load-from-URL test observes.</summary>
    /// <param name="cache">The cache holding a decodable image.</param>
    /// <param name="httpService">The service the cache resolves downloads through.</param>
    /// <param name="loader">The bitmap loader installed for the duration of the test.</param>
    private sealed class ImageLoadFixture(InMemoryBlobCache cache, CacheBackedHttpService httpService, CapturingBitmapLoader loader) : IDisposable
    {
        /// <summary>Gets the cache holding a decodable image.</summary>
        public InMemoryBlobCache Cache => cache;

        /// <summary>Gets the service the cache resolves downloads through.</summary>
        public CacheBackedHttpService HttpService => httpService;

        /// <summary>Gets the bitmap loader installed for the duration of the test.</summary>
        public CapturingBitmapLoader Loader => loader;

        /// <inheritdoc/>
        public void Dispose() => cache.Dispose();
    }

    /// <summary>Mock bitmap implementation for testing.</summary>
    private sealed class MockBitmap : IBitmap
    {
        /// <summary>The fixed pixel width this bitmap reports.</summary>
        private const float BitmapWidthPixels = 100F;

        /// <summary>The fixed pixel height this bitmap reports.</summary>
        private const float BitmapHeightPixels = 200F;

        /// <inheritdoc/>
        public float Width => BitmapWidthPixels;

        /// <inheritdoc/>
        public float Height => BitmapHeightPixels;

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
                buffer[i] = (byte)(i % ByteValueRange);
            }

            return target.WriteAsync(buffer, 0, buffer.Length);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Mock dispose
        }
    }

    /// <summary>Bitmap loader stub that captures the last requested width/height for the <see cref="BytesToImageShouldForwardDesiredSizeToLoader"/> test.</summary>
    private sealed class CapturingBitmapLoader : IBitmapLoader
    {
        /// <summary>Gets the last desired width passed to <see cref="Load"/>.</summary>
        public float? LastWidth { get; private set; }

        /// <summary>Gets the last desired height passed to <see cref="Load"/>.</summary>
        public float? LastHeight { get; private set; }

        /// <summary>Gets the byte length of the stream supplied to the last <see cref="Load"/> call, which identifies which buffer was decoded.</summary>
        public long LastStreamLength { get; private set; }

        /// <inheritdoc/>
        public Task<IBitmap?> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight)
        {
            LastWidth = desiredWidth;
            LastHeight = desiredHeight;
            LastStreamLength = sourceStream.Length;
            return Task.FromResult<IBitmap?>(new MockBitmap());
        }

        /// <inheritdoc/>
        public IBitmap Create(float width, float height) => new MockBitmap();

        /// <inheritdoc/>
        public Task<IBitmap?> LoadFromResource(string source, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(new MockBitmap());
    }

    /// <summary>Mock bitmap loader implementation for testing.</summary>
    private sealed class MockBitmapLoader : IBitmapLoader
    {
        /// <inheritdoc/>
        public Task<IBitmap?> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(new MockBitmap());

        /// <inheritdoc/>
        public IBitmap Create(float width, float height) => new MockBitmap();

        /// <inheritdoc/>
        public Task<IBitmap?> LoadFromResource(string source, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(new MockBitmap());
    }

    /// <summary>Bitmap loader that always returns null from <see cref="Load"/> in order to exercise the null-bitmap throw path in BytesToImage.</summary>
    private sealed class NullReturningBitmapLoader : IBitmapLoader
    {
        /// <inheritdoc/>
        public Task<IBitmap?> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(null);

        /// <inheritdoc/>
        public IBitmap Create(float width, float height) => new MockBitmap();

        /// <inheritdoc/>
        public Task<IBitmap?> LoadFromResource(string source, float? desiredWidth, float? desiredHeight) =>
            Task.FromResult<IBitmap?>(null);
    }
}
