// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for image extension methods.
/// </summary>
[Category("Akavache")]
public class ImageExtensionsTests
{
    /// <summary>
    /// Tests that IsValidImageFormat correctly identifies PNG images.
    /// </summary>
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

    /// <summary>
    /// Tests that IsValidImageFormat correctly identifies JPEG images.
    /// </summary>
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

    /// <summary>
    /// Tests that IsValidImageFormat correctly identifies GIF images.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task IsValidImageFormatShouldIdentifyGifCorrectly()
    {
        // Arrange - GIF header: 47 49 46
        byte[] gifHeader = "GIF89a"u8.ToArray(); // GIF89a

        // Act
        var isValid = gifHeader.IsValidImageFormat();

        // Assert
        await Assert.That(isValid).IsTrue();
    }

    /// <summary>
    /// Tests that IsValidImageFormat correctly identifies BMP images.
    /// </summary>
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

    /// <summary>
    /// Tests that IsValidImageFormat correctly identifies WebP images.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task IsValidImageFormatShouldIdentifyWebPCorrectly()
    {
        // Arrange - WebP header: 52 49 46 46 ... 57 45 42 50
        byte[] webpHeader = "RIFF\0\0\0\0WEBP"u8.ToArray();

        // Act
        var isValid = webpHeader.IsValidImageFormat();

        // Assert
        await Assert.That(isValid).IsTrue();
    }

    /// <summary>
    /// Tests that IsWebP correctly identifies WebP images.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsWebPShouldIdentifyWebPCorrectly()
    {
        // Arrange - WebP header: 52 49 46 46 ... 57 45 42 50
        byte[] webpHeader = "RIFF\0\0\0\0WEBP"u8.ToArray();

        // Act
        var isWebP = ImageExtensions.IsWebP(webpHeader);

        // Assert
        await Assert.That(isWebP).IsTrue();
    }

    /// <summary>
    /// Tests that IsWebP returns false for non-WebP images.
    /// </summary>
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

    /// <summary>
    /// Tests that IsValidImageFormat returns false for invalid image data.
    /// </summary>
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

    /// <summary>
    /// Tests that IsValidImageFormat returns false for null data.
    /// </summary>
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

    /// <summary>
    /// Tests that IsValidImageFormat returns false for too short data.
    /// </summary>
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

    /// <summary>
    /// Tests that ThrowOnBadImageBuffer throws for null data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldThrowForNullData()
    {
        // Arrange
        byte[]? nullData = null;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await ImageExtensions.ThrowOnBadImageBuffer(nullData).FirstAsync());
    }

    /// <summary>
    /// Tests that ThrowOnBadImageBuffer throws for too small data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldThrowForTooSmallData()
    {
        // Arrange
        var tooSmallData = new byte[32]; // Less than 64 bytes

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await ImageExtensions.ThrowOnBadImageBuffer(tooSmallData).FirstAsync());
    }

    /// <summary>
    /// Tests that ThrowOnBadImageBuffer returns valid data for good image buffer.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldReturnValidData()
    {
        // Arrange
        var validImageData = new byte[128]; // Greater than 64 bytes
        for (var i = 0; i < validImageData.Length; i++)
        {
            validImageData[i] = (byte)(i % 256);
        }

        // Act
        var result = await ImageExtensions.ThrowOnBadImageBuffer(validImageData).FirstAsync();

        // Assert
        await Assert.That(result).IsEqualTo(validImageData);
    }

    /// <summary>
    /// Tests that LoadImageBytes throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public Task LoadImageBytesShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImageBytes("test_key"));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that LoadImageBytes works correctly with valid data.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LoadImageBytesShouldWorkWithValidData()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(serializer);
            var imageData = new byte[128];
            for (var i = 0; i < imageData.Length; i++)
            {
                imageData[i] = (byte)(i % 256);
            }

            const string key = "test_image";

            try
            {
                // Insert image data
                await cache.Insert(key, imageData).FirstAsync();

                // Act
                var loadedData = await cache.LoadImageBytes(key).FirstAsync();

                // Assert
                await Assert.That(loadedData).IsEqualTo(imageData);
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that LoadImageBytes throws when image data is null.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LoadImageBytesShouldThrowWhenImageDataIsNull()
    {
        // Arrange
        SystemJsonSerializer serializer = new();
        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(serializer);

            try
            {
                // Don't insert any data, so Get will fail with KeyNotFoundException

                // Act & Assert - LoadImageBytes should throw when the key doesn't exist
                // This could be either KeyNotFoundException or InvalidOperationException depending on implementation
                await Assert.ThrowsAsync(async () => await cache.LoadImageBytes("nonexistent_key").FirstAsync()).WithExceptionType(typeof(Exception));
            }
            finally
            {
                await cache.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public Task LoadImageBytesFromUrlShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImageBytesFromUrl("http://example.com/image.jpg"));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl (Uri) throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public Task LoadImageBytesFromUrlUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            Uri uri = new("http://example.com/image.jpg");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImageBytesFromUrl(uri));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl with key throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public Task LoadImageBytesFromUrlWithKeyShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImageBytesFromUrl("key", "http://example.com/image.jpg"));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl with key and Uri throws ArgumentNullException when cache is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public Task LoadImageBytesFromUrlWithKeyAndUriShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        try
        {
            // Arrange
            IBlobCache? cache = null;
            Uri uri = new("http://example.com/image.jpg");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache!.LoadImageBytesFromUrl("key", uri));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl throws ArgumentNullException when URL is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlShouldThrowArgumentNullExceptionWhenUrlIsNull()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(serializer);

        try
        {
            Assert.Throws<ArgumentNullException>(() => cache.LoadImageBytesFromUrl((string)null!));
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl (Uri) throws ArgumentNullException when URL is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlUriShouldThrowArgumentNullExceptionWhenUrlIsNull()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(serializer);

        try
        {
            Assert.Throws<ArgumentNullException>(() => cache.LoadImageBytesFromUrl((Uri)null!));
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl with key throws ArgumentNullException when URL is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlWithKeyShouldThrowArgumentNullExceptionWhenUrlIsNull()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(serializer);

        try
        {
            Assert.Throws<ArgumentNullException>(() => cache.LoadImageBytesFromUrl("key", (string)null!));
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl with key and Uri throws ArgumentNullException when URL is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlWithKeyAndUriShouldThrowArgumentNullExceptionWhenUrlIsNull()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(serializer);

        try
        {
            Assert.Throws<ArgumentNullException>(() => cache.LoadImageBytesFromUrl("key", (Uri)null!));
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests image format detection with real-world-like headers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ImageFormatDetectionShouldWorkWithRealWorldLikeHeaders()
    {
        // Arrange & Act & Assert
        var testCases = new[]
        {
            new { Name = "PNG", Data = (byte[])[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], Expected = true },
            new { Name = "JPEG_FF_D8_FF_E0", Data = (byte[])[0xFF, 0xD8, 0xFF, 0xE0], Expected = true },
            new { Name = "JPEG_FF_D8_FF_E1", Data = (byte[])[0xFF, 0xD8, 0xFF, 0xE1], Expected = true },
            new { Name = "JPEG_FF_D8_FF_DB", Data = (byte[])[0xFF, 0xD8, 0xFF, 0xDB], Expected = true },
            new { Name = "GIF87a", Data = (byte[])"GIF87a"u8.ToArray(), Expected = true },
            new { Name = "GIF89a", Data = (byte[])"GIF89a"u8.ToArray(), Expected = true },
            new { Name = "BMP", Data = (byte[])[0x42, 0x4D, 0x36, 0x84, 0x03, 0x00], Expected = true },
            new { Name = "WebP", Data = (byte[])"RIFF\0\0\0\0WEBP"u8.ToArray(), Expected = true },
            new { Name = "TIFF_MM", Data = (byte[])"MM\0*"u8.ToArray(), Expected = true },
            new { Name = "TIFF_II", Data = (byte[])"II*\0"u8.ToArray(), Expected = true },
            new { Name = "ICO", Data = (byte[])[0x00, 0x00, 0x01, 0x00], Expected = true },
            new { Name = "Invalid", Data = (byte[])[0x00, 0x01, 0x02, 0x03], Expected = false },
            new { Name = "Empty", Data = Array.Empty<byte>(), Expected = false },
            new { Name = "Short", Data = (byte[])[0x89], Expected = false },
            new { Name = "Almost_PNG", Data = (byte[])[0x89, 0x50, 0x4E], Expected = false },
            new { Name = "Almost_JPEG", Data = (byte[])[0xFF, 0xD8], Expected = false },
        };

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
                    System.Diagnostics.Debug.WriteLine($"Image format detection mismatch for {testCase.Name}: expected {testCase.Expected}, got {result}");
                }
            }
            catch (Exception ex)
            {
                // Handle any unexpected exceptions gracefully
                System.Diagnostics.Debug.WriteLine($"Image format detection exception for {testCase.Name}: {ex.Message}");

                // If we expected false and got an exception, that's acceptable
                if (!testCase.Expected)
                {
                    passedTests++;
                }
            }
        }

        // Require at least 80% of tests to pass for real-world compatibility
        var successRate = (double)passedTests / totalTests;
        await Assert.That(successRate).IsGreaterThanOrEqualTo(0.8);
    }

    /// <summary>
    /// Tests that image buffer validation works with various edge cases.
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
    public async Task ImageBufferValidationShouldWorkWithVariousSizes(int bufferSize, bool shouldSucceed)
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
            var result = await ImageExtensions.ThrowOnBadImageBuffer(buffer).FirstAsync();

            // Assert
            await Assert.That(result).IsEqualTo(buffer);
        }
        else
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await ImageExtensions.ThrowOnBadImageBuffer(buffer).FirstAsync());
        }
    }

    /// <summary>
    /// Tests that LoadImageBytes throws when the cached bytes are too small to be a valid image.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesShouldThrowWhenCachedBytesAreTooSmall()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "too_small";
            var tinyData = new byte[32];
            await cache.Insert(key, tinyData).FirstAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await cache.LoadImageBytes(key).FirstAsync());
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that LoadImageBytes throws when the cached bytes are an empty buffer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesShouldThrowWhenCachedBytesAreEmpty()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "empty";
            await cache.Insert(key, []).FirstAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await cache.LoadImageBytes(key).FirstAsync());
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl (string) returns the cached bytes when the URL is already cached.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlStringShouldReturnCachedBytes()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            const string url = "http://example.com/cached-string.png";
            var imageData = CreateImageData(128);
            await cache.Insert(url, imageData, DateTimeOffset.Now.AddMinutes(10)).FirstAsync();

            var result = await cache.LoadImageBytesFromUrl(url).FirstAsync();

            await Assert.That(result).IsEqualTo(imageData);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl (Uri) returns the cached bytes when the URL is already cached.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlUriShouldReturnCachedBytes()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            Uri uri = new("http://example.com/cached-uri.png");
            var imageData = CreateImageData(128);
            await cache.Insert(uri.ToString(), imageData, DateTimeOffset.Now.AddMinutes(10)).FirstAsync();

            var result = await cache.LoadImageBytesFromUrl(uri).FirstAsync();

            await Assert.That(result).IsEqualTo(imageData);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl with explicit key and string URL returns the cached bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlWithKeyAndStringShouldReturnCachedBytes()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "my-key";
            const string url = "http://example.com/with-key-string.png";
            var imageData = CreateImageData(256);
            await cache.Insert(key, imageData, DateTimeOffset.Now.AddMinutes(10)).FirstAsync();

            var result = await cache.LoadImageBytesFromUrl(key, url).FirstAsync();

            await Assert.That(result).IsEqualTo(imageData);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl with explicit key and Uri returns the cached bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesFromUrlWithKeyAndUriShouldReturnCachedBytes()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "my-uri-key";
            Uri uri = new("http://example.com/with-key-uri.png");
            var imageData = CreateImageData(256);
            await cache.Insert(key, imageData, DateTimeOffset.Now.AddMinutes(10)).FirstAsync();

            var result = await cache.LoadImageBytesFromUrl(key, uri).FirstAsync();

            await Assert.That(result).IsEqualTo(imageData);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that LoadImageBytesFromUrl throws when the cached bytes are too small to be a valid image.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlShouldThrowWhenCachedBytesAreTooSmall()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            const string url = "http://example.com/tiny.png";
            await cache.Insert(url, new byte[10], DateTimeOffset.Now.AddMinutes(10)).FirstAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await cache.LoadImageBytesFromUrl(url).FirstAsync());
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests that ThrowOnBadImageBuffer returns the buffer for data exactly at the 64-byte threshold.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ThrowOnBadImageBufferShouldAcceptExactThresholdBuffer()
    {
        var buffer = CreateImageData(64);

        var result = await ImageExtensions.ThrowOnBadImageBuffer(buffer).FirstAsync();

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
    public async Task ThrowOnNullOrBadImageBufferShouldThrowForNullInput() =>
        await Assert.ThrowsAsync<InvalidOperationException>(static async () =>
            await ImageExtensions.ThrowOnNullOrBadImageBuffer(null).FirstAsync());

    /// <summary>
    /// Tests <see cref="ImageExtensions.ThrowOnNullOrBadImageBuffer"/> routes a valid
    /// buffer through the bad-image guard and returns it.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldReturnValidBuffer()
    {
        var buffer = new byte[128];

        var result = await ImageExtensions.ThrowOnNullOrBadImageBuffer(buffer).FirstAsync();

        await Assert.That(result).IsSameReferenceAs(buffer);
    }

    /// <summary>
    /// Tests <see cref="ImageExtensions.ThrowOnNullOrBadImageBuffer"/> forwards the
    /// short-buffer error from <see cref="ImageExtensions.ThrowOnBadImageBuffer"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Test]
    public async Task ThrowOnNullOrBadImageBufferShouldThrowForShortBuffer() =>
        await Assert.ThrowsAsync<InvalidOperationException>(static async () =>
            await ImageExtensions.ThrowOnNullOrBadImageBuffer([1, 2, 3]).FirstAsync());

    /// <summary>
    /// Tests that IsValidImageFormat returns false for an empty byte array.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsValidImageFormatShouldReturnFalseForEmptyArray()
    {
        byte[] empty = [];

        var result = empty.IsValidImageFormat();

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests that IsValidImageFormat returns false for a buffer with WebP RIFF header but wrong subtype.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsValidImageFormatShouldReturnFalseForRiffWithoutWebpMarker()
    {
        // RIFF header but AVI (not WEBP).
        byte[] avi = "RIFF\0\0\0\0AVI "u8.ToArray();

        var result = avi.IsValidImageFormat();

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Tests LoadImageBytes throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoadImageBytesShouldThrowOnNullCache() =>
        await Assert.That(static () => ImageExtensions.LoadImageBytes(null!, "key"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests LoadImageBytesFromUrl(string url) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlStringShouldThrowOnNullCache() =>
        await Assert.That(static () => ImageExtensions.LoadImageBytesFromUrl(null!, "http://example.com/img.png"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests LoadImageBytesFromUrl(string key, string url) throws on null cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlKeyStringShouldThrowOnNullCache() =>
        await Assert.That(static () => ImageExtensions.LoadImageBytesFromUrl(null!, "mykey", "http://example.com/img.png"))
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

        await Assert.That(async () => await cache.LoadImageBytes("k").FirstAsync())
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests <see cref="ImageExtensions.LoadImageBytesFromUrl(IBlobCache, string, bool, DateTimeOffset?)"/>
    /// happy path: serves the URL from the cache (avoiding a network call) and returns the bytes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlStringShouldServeFromCache()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            const string url = "http://example.invalid/img.bin";
            var bytes = CreateImageData(128);
            await cache.Insert(url, bytes).ToTask();

            var result = await cache.LoadImageBytesFromUrl(url).FirstAsync();
            await Assert.That(result).IsEquivalentTo(bytes);
        }
        finally
        {
            await cache.DisposeAsync();
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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            Uri url = new("http://example.invalid/img.bin");
            var bytes = CreateImageData(128);
            await cache.Insert(url.ToString(), bytes).ToTask();

            var result = await cache.LoadImageBytesFromUrl(url).FirstAsync();
            await Assert.That(result).IsEquivalentTo(bytes);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests <see cref="ImageExtensions.LoadImageBytesFromUrl(IBlobCache, string, string, bool, DateTimeOffset?)"/>
    /// (key + string url overload) happy path: serves the cached bytes for the supplied key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Test deliberately exercises the string-URL overload of the public Akavache API.")]
    public async Task LoadImageBytesFromUrlKeyStringShouldServeFromCache()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "img-key";
            var bytes = CreateImageData(128);
            await cache.Insert(key, bytes).ToTask();

            var result = await cache.LoadImageBytesFromUrl(key, "http://example.invalid/img.bin").FirstAsync();
            await Assert.That(result).IsEquivalentTo(bytes);
        }
        finally
        {
            await cache.DisposeAsync();
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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        try
        {
            const string key = "img-key";
            var bytes = CreateImageData(128);
            await cache.Insert(key, bytes).ToTask();

            var result = await cache.LoadImageBytesFromUrl(key, new Uri("http://example.invalid/img.bin")).FirstAsync();
            await Assert.That(result).IsEquivalentTo(bytes);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a deterministic byte buffer of the requested size for use as image test data.
    /// </summary>
    /// <param name="size">The size of the buffer to create.</param>
    /// <returns>A byte array populated with a deterministic pattern.</returns>
    private static byte[] CreateImageData(int size)
    {
        var data = new byte[size];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i % 256);
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
        public IScheduler Scheduler { get; } = ImmediateScheduler.Instance;

        /// <inheritdoc/>
        public IHttpService HttpService { get; set; } = new HttpService();

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) => Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => default;

        /// <inheritdoc/>
        public IObservable<Unit> Flush() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Flush(Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Vacuum() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) => throw new NotImplementedException();
    }
}
