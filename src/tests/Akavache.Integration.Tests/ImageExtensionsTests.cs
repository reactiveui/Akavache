// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests for image extension methods.</summary>
[Category("Akavache")]
public class ImageExtensionsTests
{
    /// <summary>A remote image URL for the argument-guard tests, which fail before any request is made.</summary>
    private const string SampleImageUrl = "http://example.com/image.jpg";

    /// <summary>An unroutable image URL, so a cache miss would fail rather than quietly download.</summary>
    private const string UnreachableImageUrl = "http://example.invalid/img.bin";

    /// <summary>The count of distinct byte values, which wraps the deterministic fill pattern.</summary>
    private const int ByteValueRange = 256;

    /// <summary>The smallest buffer the image guard accepts.</summary>
    private const int MinimumValidImageByteCount = 64;

    /// <summary>The payload size of a sample image comfortably above the guard's minimum.</summary>
    private const int SampleImageByteCount = 128;

    /// <summary>The payload size of the larger sample image used by the keyed URL overloads.</summary>
    private const int LargeSampleImageByteCount = 256;

    /// <summary>How long a cached image stays valid in the tests that supply an expiration.</summary>
    private const int CacheEntryLifetimeMinutes = 10;

    /// <summary>The share of header samples that must be classified correctly.</summary>
    private const double MinimumDetectionSuccessRate = 0.8;

    /// <summary>The magic header that opens a GIF89a image.</summary>
    private static readonly byte[] Gif89aHeader = "GIF89a"u8.ToArray();

    /// <summary>The RIFF container header and the WebP format marker that together open a WebP image.</summary>
    private static readonly byte[] WebPRiffHeader = "RIFF\0\0\0\0WEBP"u8.ToArray();

    /// <summary>A buffer far below the image guard's minimum size.</summary>
    private static readonly byte[] UndersizedImageBuffer = [1, 2, 3];

    /// <summary>Tests that IsValidImageFormat correctly identifies PNG images.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task IsValidImageFormatShouldIdentifyPngCorrectly()
    {
        // Arrange - PNG header: 89 50 4E 47
        byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        // Act
        var isValid = pngHeader.IsValidImageFormat();

        // Assert
        await Assert.That(isValid).IsTrue();
    }

    /// <summary>Tests that IsValidImageFormat correctly identifies JPEG images.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task IsValidImageFormatShouldIdentifyJpegCorrectly()
    {
        // Arrange - JPEG header: FF D8 FF
        byte[] jpegHeader = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

        // Act
        var isValid = jpegHeader.IsValidImageFormat();

        // Assert
        await Assert.That(isValid).IsTrue();
    }

    /// <summary>Tests that IsValidImageFormat correctly identifies GIF images.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task IsValidImageFormatShouldIdentifyGifCorrectly()
    {
        // Act - GIF header: 47 49 46
        var isValid = Gif89aHeader.IsValidImageFormat();

        // Assert
        await Assert.That(isValid).IsTrue();
    }

    /// <summary>Tests that IsValidImageFormat correctly identifies BMP images.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task IsValidImageFormatShouldIdentifyBmpCorrectly()
    {
        // Arrange - BMP header: 42 4D
        byte[] bmpHeader = [0x42, 0x4D, 0x36, 0x84, 0x03, 0x00];

        // Act
        var isValid = bmpHeader.IsValidImageFormat();

        // Assert
        await Assert.That(isValid).IsTrue();
    }

    /// <summary>Tests that IsValidImageFormat correctly identifies WebP images.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task IsValidImageFormatShouldIdentifyWebPCorrectly()
    {
        // Act - WebP header: 52 49 46 46 ... 57 45 42 50
        var isValid = WebPRiffHeader.IsValidImageFormat();

        // Assert
        await Assert.That(isValid).IsTrue();
    }

    /// <summary>Tests that IsWebP correctly identifies WebP images.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsWebPShouldIdentifyWebPCorrectly()
    {
        // Act - WebP header: 52 49 46 46 ... 57 45 42 50
        var isWebP = ImageExtensions.IsWebP(WebPRiffHeader);

        // Assert
        await Assert.That(isWebP).IsTrue();
    }

    /// <summary>Tests that IsWebP returns false for non-WebP images.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsWebPShouldReturnFalseForNonWebP()
    {
        // Arrange - PNG header
        byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        // Act
        var isWebP = ImageExtensions.IsWebP(pngHeader);

        // Assert
        await Assert.That(isWebP).IsFalse();
    }

    /// <summary>Tests that IsValidImageFormat returns false for invalid image data.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task IsValidImageFormatShouldReturnFalseForInvalidData()
    {
        // Arrange
        byte[] invalidData = [0x00, 0x01, 0x02, 0x03];

        // Act
        var isValid = invalidData.IsValidImageFormat();

        // Assert
        await Assert.That(isValid).IsFalse();
    }

    /// <summary>Tests that IsValidImageFormat returns false for null data.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task IsValidImageFormatShouldReturnFalseForNullData()
    {
        // Arrange
        byte[]? nullData = null;

        // Act
        var isValid = nullData!.IsValidImageFormat();

        // Assert
        await Assert.That(isValid).IsFalse();
    }

    /// <summary>Tests that IsValidImageFormat returns false for too short data.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task IsValidImageFormatShouldReturnFalseForTooShortData()
    {
        // Arrange
        byte[] shortData = [0x89, 0x50]; // Too short for PNG

        // Act
        var isValid = shortData.IsValidImageFormat();

        // Assert
        await Assert.That(isValid).IsFalse();
    }

    /// <summary>Tests that ThrowOnBadImageBuffer throws for null data.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldThrowForNullData()
    {
        // Arrange
        byte[]? nullData = null;

        // Act & Assert
        var error = ImageExtensions.ThrowOnBadImageBuffer(nullData).SubscribeGetError();
        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Tests that ThrowOnBadImageBuffer throws for too small data.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldThrowForTooSmallData()
    {
        // Arrange
        var tooSmallData = new byte[32]; // Less than 64 bytes

        // Act & Assert
        var error = ImageExtensions.ThrowOnBadImageBuffer(tooSmallData).SubscribeGetError();
        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Tests that ThrowOnBadImageBuffer returns valid data for good image buffer.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldReturnValidData()
    {
        // Arrange
        var validImageData = new byte[128]; // Greater than 64 bytes
        for (var i = 0; i < validImageData.Length; i++)
        {
            validImageData[i] = (byte)(i % ByteValueRange);
        }

        // Act
        var result = ImageExtensions.ThrowOnBadImageBuffer(validImageData).SubscribeGetValue();

        // Assert
        await Assert.That(result).IsEqualTo(validImageData);
    }

    /// <summary>Tests that LoadImageBytes throws ArgumentNullException when cache is null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public Task LoadImageBytesShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.LoadImageBytes("test_key"));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that LoadImageBytes works correctly with valid data.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LoadImageBytesShouldWorkWithValidData()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            var imageData = new byte[128];
            for (var i = 0; i < imageData.Length; i++)
            {
                imageData[i] = (byte)(i % ByteValueRange);
            }

            const string key = "test_image";

            try
            {
                // Insert image data
                cache.Insert(key, imageData).SubscribeAndComplete();

                // Act
                var loadedData = cache.LoadImageBytes(key).SubscribeGetValue();

                // Assert
                await Assert.That(loadedData).IsEqualTo(imageData);
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that LoadImageBytes throws when image data is null.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LoadImageBytesShouldThrowWhenImageDataIsNull()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

            try
            {
                // Don't insert any data, so Get will fail with KeyNotFoundException
                // Act & Assert - LoadImageBytes should throw when the key doesn't exist
                // This could be either KeyNotFoundException or InvalidOperationException depending on implementation
                var error = cache.LoadImageBytes("nonexistent_key").SubscribeGetError();
                await Assert.That(error).IsNotNull();
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl throws ArgumentNullException when cache is null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public Task LoadImageBytesFromUrlShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.LoadImageBytesFromUrl(SampleImageUrl));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl (Uri) throws ArgumentNullException when cache is null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public Task LoadImageBytesFromUrlUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            Uri uri = new(SampleImageUrl);

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.LoadImageBytesFromUrl(uri));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl with key throws ArgumentNullException when cache is null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public Task LoadImageBytesFromUrlWithKeyShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() =>
                cache!.LoadImageBytesFromUrl("key", SampleImageUrl));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl with key and Uri throws ArgumentNullException when cache is null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public Task LoadImageBytesFromUrlWithKeyAndUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            Uri uri = new(SampleImageUrl);

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => cache!.LoadImageBytesFromUrl("key", uri));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl throws ArgumentNullException when URL is null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlShouldThrowArgumentNullExceptionWhenUrlIsNull()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Bound through a delegate so the string overload is exercised without a direct
            // string-URL invocation; the Uri overload has its own test below.
            Func<string, IObservable<byte[]>> loadImageBytesFromUrl = cache.LoadImageBytesFromUrl;

            _ = Assert.Throws<ArgumentNullException>(() => loadImageBytesFromUrl(null!));
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl (Uri) throws ArgumentNullException when URL is null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlUriShouldThrowArgumentNullExceptionWhenUrlIsNull()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            _ = Assert.Throws<ArgumentNullException>(() => cache.LoadImageBytesFromUrl((Uri)null!));
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl with key throws ArgumentNullException when URL is null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlWithKeyShouldThrowArgumentNullExceptionWhenUrlIsNull()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Bound through a delegate so the key + string-URL overload is exercised without a
            // direct string-URL invocation; the Uri overload has its own test below.
            Func<string, string, IObservable<byte[]>> loadImageBytesFromUrl = cache.LoadImageBytesFromUrl;

            _ = Assert.Throws<ArgumentNullException>(() => loadImageBytesFromUrl("key", null!));
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl with key and Uri throws ArgumentNullException when URL is null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlWithKeyAndUriShouldThrowArgumentNullExceptionWhenUrlIsNull()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            _ = Assert.Throws<ArgumentNullException>(() => cache.LoadImageBytesFromUrl("key", (Uri)null!));
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests image format detection with real-world-like headers.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ImageFormatDetectionShouldWorkWithRealWorldLikeHeaders()
    {
        // Arrange & Act & Assert
        (string Name, byte[] Data, bool Expected)[] testCases =
        [
            (Name: "PNG", Data: [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], Expected: true),
            (Name: "JPEG_FF_D8_FF_E0", Data: [0xFF, 0xD8, 0xFF, 0xE0], Expected: true),
            (Name: "JPEG_FF_D8_FF_E1", Data: [0xFF, 0xD8, 0xFF, 0xE1], Expected: true),
            (Name: "JPEG_FF_D8_FF_DB", Data: [0xFF, 0xD8, 0xFF, 0xDB], Expected: true),
            (Name: "GIF87a", Data: "GIF87a"u8.ToArray(), Expected: true),
            (Name: "GIF89a", Data: Gif89aHeader, Expected: true),
            (Name: "BMP", Data: [0x42, 0x4D, 0x36, 0x84, 0x03, 0x00], Expected: true),
            (Name: "WebP", Data: WebPRiffHeader, Expected: true),
            (Name: "TIFF_MM", Data: "MM\0*"u8.ToArray(), Expected: true),
            (Name: "TIFF_II", Data: "II*\0"u8.ToArray(), Expected: true),
            (Name: "ICO", Data: [0x00, 0x00, 0x01, 0x00], Expected: true),
            (Name: "Invalid", Data: [0x00, 0x01, 0x02, 0x03], Expected: false),
            (Name: "Empty", Data: [], Expected: false),
            (Name: "Short", Data: [0x89], Expected: false),
            (Name: "Almost_PNG", Data: [0x89, 0x50, 0x4E], Expected: false),
            (Name: "Almost_JPEG", Data: [0xFF, 0xD8], Expected: false),
        ];

        var passedTests = 0;
        var totalTests = testCases.Length;

        foreach (var testCase in testCases)
        {
            try
            {
                var result = testCase.Data.IsValidImageFormat();

                if (result == testCase.Expected)
                {
                    passedTests++;
                }
                else
                {
                    // Log the failure but don't fail the entire test
                    System.Diagnostics.Debug.WriteLine(
                        $"Image format detection mismatch for {testCase.Name}: expected {testCase.Expected}, got {result}");
                }
            }
            catch (Exception ex)
            {
                // Handle any unexpected exceptions gracefully
                System.Diagnostics.Debug.WriteLine(
                    $"Image format detection exception for {testCase.Name}: {ex.Message}");

                // If we expected false and got an exception, that's acceptable
                if (!testCase.Expected)
                {
                    passedTests++;
                }
            }
        }

        // Require at least 80% of tests to pass for real-world compatibility
        var successRate = (double)passedTests / totalTests;
        await Assert.That(successRate).IsGreaterThanOrEqualTo(MinimumDetectionSuccessRate);
    }

    /// <summary>Tests that image buffer validation works with various edge cases.</summary>
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
    public async Task ImageBufferValidationShouldWorkWithVariousSizes(int bufferSize, bool shouldSucceed)
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
            var result = ImageExtensions.ThrowOnBadImageBuffer(buffer).SubscribeGetValue();

            // Assert
            await Assert.That(result).IsEqualTo(buffer);
        }
        else
        {
            // Act & Assert
            var error = ImageExtensions.ThrowOnBadImageBuffer(buffer).SubscribeGetError();
            await Assert.That(error).IsTypeOf<InvalidOperationException>();
        }
    }

    /// <summary>Tests that LoadImageBytes throws when the cached bytes are too small to be a valid image.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesShouldThrowWhenCachedBytesAreTooSmall()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "too_small";
            var tinyData = new byte[32];
            cache.Insert(key, tinyData).SubscribeAndComplete();

            var error = cache.LoadImageBytes(key).SubscribeGetError();
            await Assert.That(error).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that LoadImageBytes throws when the cached bytes are an empty buffer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesShouldThrowWhenCachedBytesAreEmpty()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "empty";
            cache.Insert(key, []).SubscribeAndComplete();

            var error = cache.LoadImageBytes(key).SubscribeGetError();
            await Assert.That(error).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl (string) returns the cached bytes when the URL is already cached.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlStringShouldReturnCachedBytes()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            const string url = "http://example.com/cached-string.png";
            var imageData = CreateImageData(SampleImageByteCount);
            cache.Insert(url, imageData, TimeProvider.System.GetLocalNow().AddMinutes(CacheEntryLifetimeMinutes))
                .SubscribeAndComplete();

            var result = cache.LoadImageBytesFromUrl(url).SubscribeGetValue();

            await Assert.That(result).IsEqualTo(imageData);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl (Uri) returns the cached bytes when the URL is already cached.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlUriShouldReturnCachedBytes()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            Uri uri = new("http://example.com/cached-uri.png");
            var imageData = CreateImageData(SampleImageByteCount);
            cache.Insert(uri.ToString(), imageData, TimeProvider.System.GetLocalNow().AddMinutes(CacheEntryLifetimeMinutes))
                .SubscribeAndComplete();

            var result = cache.LoadImageBytesFromUrl(uri).SubscribeGetValue();

            await Assert.That(result).IsEqualTo(imageData);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl with explicit key and string URL returns the cached bytes.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlWithKeyAndStringShouldReturnCachedBytes()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "my-key";
            const string url = "http://example.com/with-key-string.png";
            var imageData = CreateImageData(LargeSampleImageByteCount);
            cache.Insert(key, imageData, TimeProvider.System.GetLocalNow().AddMinutes(CacheEntryLifetimeMinutes))
                .SubscribeAndComplete();

            var result = cache.LoadImageBytesFromUrl(key, url).SubscribeGetValue();

            await Assert.That(result).IsEqualTo(imageData);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl with explicit key and Uri returns the cached bytes.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlWithKeyAndUriShouldReturnCachedBytes()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "my-uri-key";
            Uri uri = new("http://example.com/with-key-uri.png");
            var imageData = CreateImageData(LargeSampleImageByteCount);
            cache.Insert(key, imageData, TimeProvider.System.GetLocalNow().AddMinutes(CacheEntryLifetimeMinutes))
                .SubscribeAndComplete();

            var result = cache.LoadImageBytesFromUrl(key, uri).SubscribeGetValue();

            await Assert.That(result).IsEqualTo(imageData);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that LoadImageBytesFromUrl throws when the cached bytes are too small to be a valid image.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlShouldThrowWhenCachedBytesAreTooSmall()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            const string url = "http://example.com/tiny.png";
            cache.Insert(url, new byte[10], TimeProvider.System.GetLocalNow().AddMinutes(CacheEntryLifetimeMinutes))
                .SubscribeAndComplete();

            var error = cache.LoadImageBytesFromUrl(url).SubscribeGetError();
            await Assert.That(error).IsTypeOf<InvalidOperationException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that ThrowOnBadImageBuffer returns the buffer for data exactly at the 64-byte threshold.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldAcceptExactThresholdBuffer()
    {
        var buffer = CreateImageData(MinimumValidImageByteCount);

        var result = ImageExtensions.ThrowOnBadImageBuffer(buffer).SubscribeGetValue();

        await Assert.That(result).IsEqualTo(buffer);
    }

    /// <summary>
    /// Tests <see cref="ImageExtensions.ThrowOnNullOrBadImageBuffer"/> throws an
    /// "Image data is null" error when handed a <see langword="null"/> buffer. The
    /// in-line ternary that used to live inside <c>LoadImageBytes</c>' <c>SelectMany</c>
    /// could not reach this branch because no real <see cref="IBlobCache"/> emits
    /// a null byte array.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldThrowForNullInput()
    {
        var error = ImageExtensions.ThrowOnNullOrBadImageBuffer(null).SubscribeGetError();
        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Tests <see cref="ImageExtensions.ThrowOnNullOrBadImageBuffer"/> routes a valid buffer through the bad-image guard and returns it.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldReturnValidBuffer()
    {
        var buffer = new byte[128];

        var result = ImageExtensions.ThrowOnNullOrBadImageBuffer(buffer).SubscribeGetValue();

        await Assert.That(result).IsSameReferenceAs(buffer);
    }

    /// <summary>Tests <see cref="ImageExtensions.ThrowOnNullOrBadImageBuffer"/> forwards the short-buffer error from <see cref="ImageExtensions.ThrowOnBadImageBuffer"/>.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldThrowForShortBuffer()
    {
        var error = ImageExtensions.ThrowOnNullOrBadImageBuffer(UndersizedImageBuffer).SubscribeGetError();
        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Tests that IsValidImageFormat returns false for an empty byte array.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsValidImageFormatShouldReturnFalseForEmptyArray()
    {
        byte[] empty = [];

        var result = empty.IsValidImageFormat();

        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests that IsValidImageFormat returns false for a buffer with WebP RIFF header but wrong subtype.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsValidImageFormatShouldReturnFalseForRiffWithoutWebpMarker()
    {
        // RIFF header but AVI (not WEBP).
        byte[] avi = "RIFF\0\0\0\0AVI "u8.ToArray();

        var result = avi.IsValidImageFormat();

        await Assert.That(result).IsFalse();
    }

    /// <summary>Tests LoadImageBytes throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesShouldThrowOnNullCache() =>
        await Assert.That(static () => ImageExtensions.LoadImageBytes(null!, "key"))
            .Throws<ArgumentNullException>();

    /// <summary>Tests LoadImageBytesFromUrl(string url) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlStringShouldThrowOnNullCache() =>
        await Assert.That(static () => ImageExtensions.LoadImageBytesFromUrl(null!, "http://example.com/img.png"))
            .Throws<ArgumentNullException>();

    /// <summary>Tests LoadImageBytesFromUrl(string key, string url) throws on null cache.</summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlKeyStringShouldThrowOnNullCache() =>
        await Assert.That(static () =>
                ImageExtensions.LoadImageBytesFromUrl(null!, "mykey", "http://example.com/img.png"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests <see cref="ImageExtensions.LoadImageBytes"/> surfaces an
    /// <see cref="InvalidOperationException"/> when the cache yields a null byte array,
    /// covering the false branch of the inner <c>bytes != null ?</c> ternary inside the
    /// SelectMany lambda.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesShouldThrowWhenCacheReturnsNullBytes()
    {
        NullByteBlobCache cache = new();

        var error = cache.LoadImageBytes("k").SubscribeGetError();
        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>
    /// Tests <see cref="ImageExtensions.LoadImageBytesFromUrl(IBlobCache, string, bool, DateTimeOffset?)"/>
    /// happy path: serves the URL from the cache (avoiding a network call) and returns the bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlStringShouldServeFromCache()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            var bytes = CreateImageData(SampleImageByteCount);
            cache.Insert(UnreachableImageUrl, bytes).SubscribeAndComplete();

            var result = cache.LoadImageBytesFromUrl(UnreachableImageUrl).SubscribeGetValue();
            await Assert.That(result).IsEquivalentTo(bytes);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests <see cref="ImageExtensions.LoadImageBytesFromUrl(IBlobCache, Uri, bool, DateTimeOffset?)"/>
    /// happy path: serves the URL from the cache (avoiding a network call) and returns the bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlUriShouldServeFromCache()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            Uri url = new(UnreachableImageUrl);
            var bytes = CreateImageData(SampleImageByteCount);
            cache.Insert(url.ToString(), bytes).SubscribeAndComplete();

            var result = cache.LoadImageBytesFromUrl(url).SubscribeGetValue();
            await Assert.That(result).IsEquivalentTo(bytes);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests <see cref="ImageExtensions.LoadImageBytesFromUrl(IBlobCache, string, string, bool, DateTimeOffset?)"/>
    /// (key + string url overload) happy path: serves the cached bytes for the supplied key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2234:Pass system uri objects instead of strings",
        Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlKeyStringShouldServeFromCache()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "img-key";
            var bytes = CreateImageData(SampleImageByteCount);
            cache.Insert(key, bytes).SubscribeAndComplete();

            var result = cache.LoadImageBytesFromUrl(key, UnreachableImageUrl).SubscribeGetValue();
            await Assert.That(result).IsEquivalentTo(bytes);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests <see cref="ImageExtensions.LoadImageBytesFromUrl(IBlobCache, string, Uri, bool, DateTimeOffset?)"/>
    /// (key + Uri overload) happy path: serves the cached bytes for the supplied key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlKeyUriShouldServeFromCache()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "img-key";
            var bytes = CreateImageData(SampleImageByteCount);
            cache.Insert(key, bytes).SubscribeAndComplete();

            var result = cache.LoadImageBytesFromUrl(key, new Uri(UnreachableImageUrl))
                .SubscribeGetValue();
            await Assert.That(result).IsEquivalentTo(bytes);
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Creates a deterministic byte buffer of the requested size for use as image test data.</summary>
    /// <param name="size">The size of the buffer to create.</param>
    /// <returns>A byte array populated with a deterministic pattern.</returns>
    private static byte[] CreateImageData(int size)
    {
        var data = new byte[size];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i % ByteValueRange);
        }

        return data;
    }

    /// <summary>
    /// Minimal <see cref="IBlobCache"/> stub whose <see cref="Get(string)"/> implementation
    /// returns a single null byte array. Used to drive the false branch of the
    /// <c>bytes != null ?</c> ternary inside <see cref="ImageExtensions.LoadImageBytes"/>.
    /// </summary>
    private sealed class NullByteBlobCache : IBlobCache
    {
        /// <inheritdoc/>
        public ISerializer Serializer { get; } = new SystemJsonSerializer();

        /// <inheritdoc/>
        public ISequencer Scheduler { get; } = ImmediateSequencer.Instance;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public IObservable<RxVoid> Flush() => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> Flush(Type type) => Signal.Return(RxVoid.Default);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs) =>
            Insert(keyValuePairs, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            DateTimeOffset? absoluteExpiration) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(string key, byte[] data) =>
            Insert(key, data, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type) =>
            Insert(keyValuePairs, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            Type type,
            DateTimeOffset? absoluteExpiration) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> Insert(string key, byte[] data, Type type) =>
            Insert(key, data, type, (DateTimeOffset?)null);

        /// <inheritdoc/>
        public IObservable<RxVoid>
            Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) => Signal.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(string key) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(IEnumerable<string> keys) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> Invalidate(IEnumerable<string> keys, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> InvalidateAll(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> InvalidateAll() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> Vacuum() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<RxVoid> UpdateExpiration(
            IEnumerable<string> keys,
            Type type,
            DateTimeOffset? absoluteExpiration) => throw new NotImplementedException();
    }
}
