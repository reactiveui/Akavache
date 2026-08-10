// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests for Akavache.Drawing Size struct functionality.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class SizeTests
{
    /// <summary>Width of the reference size that the comparison, hashing and formatting cases all start from.</summary>
    private const float ReferenceWidth = 100F;

    /// <summary>Height of the reference size that the comparison, hashing and formatting cases all start from.</summary>
    private const float ReferenceHeight = 200F;

    /// <summary>Width one step above the reference, used to isolate a width-only mismatch.</summary>
    private const float LargerWidth = 150F;

    /// <summary>Height one step above the reference, pairing with <see cref="LargerWidth"/> in the ordered collection.</summary>
    private const float LargerHeight = 300F;

    /// <summary>Width of the largest entry in the ordered collection.</summary>
    private const float LargestWidth = 200F;

    /// <summary>Height of the largest entry in the ordered collection.</summary>
    private const float LargestHeight = 400F;

    /// <summary>Height above the reference, used to isolate a height-only mismatch.</summary>
    private const float TallerHeight = 250F;

    /// <summary>The non-zero extent paired with a zero one, so only the zeroed axis reads as empty.</summary>
    private const float NonZeroExtent = 100F;

    /// <summary>Width carrying a fractional part, to prove the constructor stores it unrounded.</summary>
    private const float FractionalWidth = 100.5F;

    /// <summary>Height carrying a fractional part, to prove the constructor stores it unrounded.</summary>
    private const float FractionalHeight = 200.75F;

    /// <summary>Width whose fractional part must appear in the formatted string.</summary>
    private const float FormattedFractionalWidth = 1.5F;

    /// <summary>Height whose fractional part must appear in the formatted string.</summary>
    private const float FormattedFractionalHeight = 2.75F;

    /// <summary>Width of the size built entirely from negative extents.</summary>
    private const float NegativeWidth = -100F;

    /// <summary>Height of the size built entirely from negative extents.</summary>
    private const float NegativeHeight = -200F;

    /// <summary>The negative width of the size whose two axes disagree in sign.</summary>
    private const float MixedSignWidth = -50F;

    /// <summary>Aspect ratio of the wholly negative size, where the two signs cancel.</summary>
    private const float NegativeSizeAspectRatio = 0.5F;

    /// <summary>Aspect ratio of the mixed-sign size, where the signs do not cancel.</summary>
    private const float MixedSignAspectRatio = -0.5F;

    /// <summary>Slack allowed when comparing a computed aspect ratio, covering five decimal places.</summary>
    private const float AspectRatioTolerance = 0.00001F;

    /// <summary>Entries left once the duplicate is removed from the four-element sample.</summary>
    private const int DistinctSizeCount = 3;

    /// <summary>Keys left in the dictionary once the repeated size overwrites its earlier entry.</summary>
    private const int DistinctKeyCount = 2;

    /// <summary>The value written last for the repeated dictionary key, overwriting the original.</summary>
    private const string OverwritingValue = "Third";

    /// <summary>Tests that Size constructor sets properties correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeConstructorShouldSetPropertiesCorrectly()
    {
        // Act
        Size size = new(FractionalWidth, FractionalHeight);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(size.Width).IsEqualTo(FractionalWidth);
            await Assert.That(size.Height).IsEqualTo(FractionalHeight);
        }
    }

    /// <summary>Tests that Size with zero dimensions works correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeWithZeroDimensionsShouldWork()
    {
        // Act
        Size zeroSize = new(0, 0);
        Size zeroWidth = new(0, NonZeroExtent);
        Size zeroHeight = new(NonZeroExtent, 0);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(zeroSize.Width).IsZero();
            await Assert.That(zeroSize.Height).IsZero();
            await Assert.That(zeroWidth.Width).IsZero();
            await Assert.That(zeroWidth.Height).IsEqualTo(NonZeroExtent);
            await Assert.That(zeroHeight.Width).IsEqualTo(NonZeroExtent);
            await Assert.That(zeroHeight.Height).IsZero();
        }
    }

    /// <summary>Tests that AspectRatio calculation works correctly.</summary>
    /// <param name="width">The width to test.</param>
    /// <param name="height">The height to test.</param>
    /// <param name="expectedRatio">The expected aspect ratio.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Arguments(100F, 100F, 1.0F)] // Square
    [Arguments(200F, 100F, 2.0F)] // 2:1 landscape
    [Arguments(100F, 200F, 0.5F)] // 1:2 portrait
    [Arguments(16F, 9F, 1.777778F)] // 16:9 widescreen (approximately)
    [Arguments(4F, 3F, 1.333333F)] // 4:3 standard (approximately)
    [Arguments(100F, 0F, 0F)] // Zero height
    [Test]
    public async Task AspectRatioShouldBeCalculatedCorrectly(float width, float height, float expectedRatio)
    {
        // Arrange
        Size size = new(width, height);

        // Assert
        await Assert.That(size.AspectRatio).IsEqualTo(expectedRatio).Within(AspectRatioTolerance);
    }

    /// <summary>Tests that AspectRatio handles zero width correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task AspectRatioWithZeroWidthShouldReturnZero()
    {
        // Arrange
        Size size = new(0F, NonZeroExtent);

        // Assert
        await Assert.That(size.AspectRatio).IsZero();
    }

    /// <summary>Tests that Size equality operators work correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeEqualityOperatorsShouldWork()
    {
        // Arrange
        Size size1 = new(ReferenceWidth, ReferenceHeight);
        Size size2 = new(ReferenceWidth, ReferenceHeight);
        Size size3 = new(LargerWidth, ReferenceHeight);
        Size size4 = new(ReferenceWidth, TallerHeight);

        // Act & Assert - Equality
        await Assert.That(size1).IsEqualTo(size2);
        await Assert.That(size1).IsNotEqualTo(size3);
        await Assert.That(size1).IsNotEqualTo(size4);

        // Act & Assert - Inequality
        await Assert.That(size1).IsEqualTo(size2);
        await Assert.That(size1).IsNotEqualTo(size3);
        await Assert.That(size1).IsNotEqualTo(size4);
    }

    /// <summary>Tests that Size.Equals method works correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeEqualsShouldWork()
    {
        // Arrange
        Size size1 = new(ReferenceWidth, ReferenceHeight);
        Size size2 = new(ReferenceWidth, ReferenceHeight);
        Size size3 = new(LargerWidth, ReferenceHeight);

        using (Assert.Multiple())
        {
            // Act & Assert - Equals with Size
            await Assert.That(size1).IsEqualTo(size2);
            await Assert.That(size1).IsNotEqualTo(size3);

            // Act & Assert - Equals with object
            await Assert.That(size1).IsEqualTo(size2);
            await Assert.That(size1).IsNotEqualTo(size3);
        }
    }

    /// <summary>Tests that Size.GetHashCode works correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeGetHashCodeShouldWork()
    {
        // Arrange
        Size size1 = new(ReferenceWidth, ReferenceHeight);
        Size size2 = new(ReferenceWidth, ReferenceHeight);
        Size size3 = new(LargerWidth, ReferenceHeight);

        // Act
        var hash1 = size1.GetHashCode();
        var hash2 = size2.GetHashCode();
        var hash3 = size3.GetHashCode();

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(hash2).IsEqualTo(hash1); // Equal objects should have equal hash codes
            await Assert.That(hash3).IsNotEqualTo(hash1); // Different objects should typically have different hash codes
        }
    }

    /// <summary>Tests that Size.ToString works correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeToStringShouldWork()
    {
        // Arrange
        Size size1 = new(ReferenceWidth, ReferenceHeight);
        Size size2 = new(FormattedFractionalWidth, FormattedFractionalHeight);
        Size size3 = new(0F, 0F);

        // Act
        var str1 = size1.ToString();
        var str2 = size2.ToString();
        var str3 = size3.ToString();

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(str1).IsEqualTo("100x200");
            await Assert.That(str2).IsEqualTo("1.5x2.75");
            await Assert.That(str3).IsEqualTo("0x0");
        }
    }

    /// <summary>Tests Size with negative dimensions.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeWithNegativeDimensionsShouldWork()
    {
        // Arrange & Act
        Size negativeSize = new(NegativeWidth, NegativeHeight);
        Size mixedSize = new(MixedSignWidth, NonZeroExtent);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(negativeSize.Width).IsEqualTo(NegativeWidth);
            await Assert.That(negativeSize.Height).IsEqualTo(NegativeHeight);
            await Assert.That(negativeSize.AspectRatio).IsEqualTo(NegativeSizeAspectRatio);

            await Assert.That(mixedSize.Width).IsEqualTo(MixedSignWidth);
            await Assert.That(mixedSize.Height).IsEqualTo(NonZeroExtent);
            await Assert.That(mixedSize.AspectRatio).IsEqualTo(MixedSignAspectRatio);
        }
    }

    /// <summary>Tests Size with very large dimensions.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeWithLargeDimensionsShouldWork()
    {
        // Arrange & Act
        Size largeSize = new(float.MaxValue, float.MaxValue);
        Size veryLargeSize = new(1e30F, 1e30F);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(largeSize.Width).IsEqualTo(float.MaxValue);
            await Assert.That(largeSize.Height).IsEqualTo(float.MaxValue);
            await Assert.That(largeSize.AspectRatio).IsEqualTo(1.0F); // MaxValue / MaxValue = 1

            await Assert.That(veryLargeSize.Width).IsEqualTo(1e30F);
            await Assert.That(veryLargeSize.Height).IsEqualTo(1e30F);
            await Assert.That(veryLargeSize.AspectRatio).IsEqualTo(1.0F);
        }
    }

    /// <summary>Tests Size with very small dimensions.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeWithSmallDimensionsShouldWork()
    {
        // Arrange & Act
        Size smallSize = new(float.Epsilon, float.Epsilon);
        Size tinySize = new(1e-30F, 1e-30F);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(smallSize.Width).IsEqualTo(float.Epsilon);
            await Assert.That(smallSize.Height).IsEqualTo(float.Epsilon);
            await Assert.That(smallSize.AspectRatio).IsEqualTo(1.0F); // Epsilon / Epsilon = 1

            await Assert.That(tinySize.Width).IsEqualTo(1e-30F);
            await Assert.That(tinySize.Height).IsEqualTo(1e-30F);
            await Assert.That(tinySize.AspectRatio).IsEqualTo(1.0F);
        }
    }

    /// <summary>Tests Size with special float values.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeWithSpecialFloatValuesShouldWork()
    {
        // Arrange & Act
        Size infiniteSize = new(float.PositiveInfinity, float.PositiveInfinity);
        Size nanSize = new(float.NaN, float.NaN);
        Size mixedSpecialSize = new(float.PositiveInfinity, NonZeroExtent);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(infiniteSize.Width).IsEqualTo(float.PositiveInfinity);
            await Assert.That(infiniteSize.Height).IsEqualTo(float.PositiveInfinity);
            await Assert.That(infiniteSize.AspectRatio).IsNaN(); // Infinity / Infinity = NaN

            await Assert.That(nanSize.Width).IsNaN();
            await Assert.That(nanSize.Height).IsNaN();
            await Assert.That(nanSize.AspectRatio).IsNaN();

            await Assert.That(mixedSpecialSize.Width).IsEqualTo(float.PositiveInfinity);
            await Assert.That(mixedSpecialSize.Height).IsEqualTo(NonZeroExtent);
            await Assert.That(mixedSpecialSize.AspectRatio).IsEqualTo(float.PositiveInfinity); // Infinity / 100 = Infinity
        }
    }

    /// <summary>Tests that Size struct behaves correctly in collections.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeShouldWorkInCollections()
    {
        // Arrange
        Size[] sizes =
        [
            new(ReferenceWidth, ReferenceHeight),
            new(LargerWidth, LargerHeight),
            new(ReferenceWidth, ReferenceHeight), // Duplicate
            new(LargestWidth, LargestHeight)
        ];

        // Act
        var uniqueSizes = sizes.Distinct().ToArray();
        var sortedSizes = sizes.OrderBy(static s => s.Width).ThenBy(static s => s.Height).ToArray();

        // Assert
        await Assert.That(uniqueSizes).Count().IsEqualTo(DistinctSizeCount);
        await Assert.That(uniqueSizes).Contains(new Size(ReferenceWidth, ReferenceHeight));
        await Assert.That(uniqueSizes).Contains(new Size(LargerWidth, LargerHeight));
        using (Assert.Multiple())
        {
            await Assert.That(uniqueSizes).Contains(new Size(LargestWidth, LargestHeight));

            // Check sorting
            await Assert.That(sortedSizes[0]).IsEqualTo(new(ReferenceWidth, ReferenceHeight));
            await Assert.That(sortedSizes[1]).IsEqualTo(new(ReferenceWidth, ReferenceHeight)); // Duplicate
            await Assert.That(sortedSizes[2]).IsEqualTo(new(LargerWidth, LargerHeight));
            await Assert.That(sortedSizes[3]).IsEqualTo(new(LargestWidth, LargestHeight));
        }
    }

    /// <summary>Tests that Size can be used as dictionary key.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeShouldWorkAsDictionaryKey()
    {
        // Arrange
        Dictionary<Size, string> sizeDict = [];
        Size size1 = new(ReferenceWidth, ReferenceHeight);
        Size size2 = new(LargerWidth, LargerHeight);
        Size size3 = new(ReferenceWidth, ReferenceHeight); // Same as size1

        // Act
        sizeDict[size1] = "First";
        sizeDict[size2] = "Second";
        sizeDict[size3] = OverwritingValue; // Should overwrite "First"

        // Assert
        await Assert.That(sizeDict).Count().IsEqualTo(DistinctKeyCount);
        using (Assert.Multiple())
        {
            await Assert.That(sizeDict[size1]).IsEqualTo(OverwritingValue); // Overwritten by size3
            await Assert.That(sizeDict[size3]).IsEqualTo(OverwritingValue); // Same as size1
            await Assert.That(sizeDict[size2]).IsEqualTo("Second");
        }
    }

    /// <summary>Tests that the == and != operators on Size return the expected results.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeOperatorEqualsAndNotEqualsShouldWork()
    {
        // Arrange
        Size size1 = new(ReferenceWidth, ReferenceHeight);
        Size size2 = new(ReferenceWidth, ReferenceHeight);
        Size size3 = new(LargerWidth, ReferenceHeight);
        Size size4 = new(ReferenceWidth, TallerHeight);

        using (Assert.Multiple())
        {
            // Act & Assert - operator ==
            await Assert.That(size1 == size2).IsTrue();
            await Assert.That(size1 == size3).IsFalse();
            await Assert.That(size1 == size4).IsFalse();

            // Act & Assert - operator !=
            await Assert.That(size1 != size2).IsFalse();
            await Assert.That(size1 != size3).IsTrue();
            await Assert.That(size1 != size4).IsTrue();
        }
    }

    /// <summary>Tests that Equals(object?) handles matching Size, non-matching Size, different type and null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    [SuppressMessage("Performance", "CA1508:Avoid dead conditional code", Justification = "Test deliberately verifies Equals(null) returns false; the analyzer can't model the boxed object cast.")]
    public async Task SizeEqualsObjectShouldWork()
    {
        // Arrange
        Size size1 = new(ReferenceWidth, ReferenceHeight);
        object sameBoxed = new Size(ReferenceWidth, ReferenceHeight);
        object differentBoxed = new Size(LargerWidth, ReferenceHeight);
        object notASize = "not a size";
        object? nullObj = null;

        using (Assert.Multiple())
        {
            // Act & Assert
            await Assert.That(size1.Equals(sameBoxed)).IsTrue();
            await Assert.That(size1.Equals(differentBoxed)).IsFalse();
            await Assert.That(size1.Equals(notASize)).IsFalse();
            await Assert.That(size1.Equals(nullObj)).IsFalse();
        }
    }

    /// <summary>Tests Size with realistic image dimensions.</summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Arguments(1920F, 1080F)] // Full HD
    [Arguments(3840F, 2160F)] // 4K UHD
    [Arguments(1024F, 768F)] // XGA
    [Arguments(800F, 600F)] // SVGA
    [Arguments(640F, 480F)] // VGA
    [Arguments(320F, 240F)] // QVGA
    [Test]
    public async Task SizeWithRealisticImageDimensionsShouldWork(float width, float height)
    {
        // Arrange & Act
        Size size = new(width, height);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(size.Width).IsEqualTo(width);
            await Assert.That(size.Height).IsEqualTo(height);
            await Assert.That(size.AspectRatio).IsGreaterThan(0);
            await Assert.That(size.ToString()).Contains("x");
        }
    }
}
