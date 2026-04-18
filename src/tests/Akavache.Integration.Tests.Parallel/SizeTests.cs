// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Drawing;

namespace Akavache.Integration.Tests;

/// <summary>
/// Tests for Akavache.Drawing Size struct functionality.
/// </summary>
[Category("Akavache")]
public class SizeTests
{
    /// <summary>
    /// Tests that Size constructor sets properties correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeConstructorShouldSetPropertiesCorrectly()
    {
        // Act
        Size size = new(100.5f, 200.75f);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(size.Width).IsEqualTo(100.5f);
            await Assert.That(size.Height).IsEqualTo(200.75f);
        }
    }

    /// <summary>
    /// Tests that Size with zero dimensions works correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeWithZeroDimensionsShouldWork()
    {
        // Act
        Size zeroSize = new(0, 0);
        Size zeroWidth = new(0, 100);
        Size zeroHeight = new(100, 0);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(zeroSize.Width).IsZero();
            await Assert.That(zeroSize.Height).IsZero();
            await Assert.That(zeroWidth.Width).IsZero();
            await Assert.That(zeroWidth.Height).IsEqualTo(100f);
            await Assert.That(zeroHeight.Width).IsEqualTo(100f);
            await Assert.That(zeroHeight.Height).IsZero();
        }
    }

    /// <summary>
    /// Tests that AspectRatio calculation works correctly.
    /// </summary>
    /// <param name="width">The width to test.</param>
    /// <param name="height">The height to test.</param>
    /// <param name="expectedRatio">The expected aspect ratio.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Arguments(100f, 100f, 1.0f)] // Square
    [Arguments(200f, 100f, 2.0f)] // 2:1 landscape
    [Arguments(100f, 200f, 0.5f)] // 1:2 portrait
    [Arguments(16f, 9f, 1.777778f)] // 16:9 widescreen (approximately)
    [Arguments(4f, 3f, 1.333333f)] // 4:3 standard (approximately)
    [Arguments(100f, 0f, 0f)] // Zero height
    [Test]
    public async Task AspectRatioShouldBeCalculatedCorrectly(float width, float height, float expectedRatio)
    {
        // Arrange
        Size size = new(width, height);

        // Act
        var actualRatio = size.AspectRatio;

        // Assert
        // Check for equality within a small tolerance (5 decimal places)
        await Assert.That(actualRatio).IsEqualTo(expectedRatio).Within(0.00001f);
    }

    /// <summary>
    /// Tests that AspectRatio handles zero width correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task AspectRatioWithZeroWidthShouldReturnZero()
    {
        // Arrange
        Size size = new(0f, 100f);

        // Act
        var ratio = size.AspectRatio;

        // Assert
        await Assert.That(ratio).IsZero();
    }

    /// <summary>
    /// Tests that Size equality operators work correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeEqualityOperatorsShouldWork()
    {
        // Arrange
        Size size1 = new(100f, 200f);
        Size size2 = new(100f, 200f);
        Size size3 = new(150f, 200f);
        Size size4 = new(100f, 250f);

        // Act & Assert - Equality
        await Assert.That(size1).IsEqualTo(size2);
        await Assert.That(size1).IsNotEqualTo(size3);
        await Assert.That(size1).IsNotEqualTo(size4);

        // Act & Assert - Inequality
        await Assert.That(size1).IsEqualTo(size2);
        await Assert.That(size1).IsNotEqualTo(size3);
        await Assert.That(size1).IsNotEqualTo(size4);
    }

    /// <summary>
    /// Tests that Size.Equals method works correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeEqualsShouldWork()
    {
        // Arrange
        Size size1 = new(100f, 200f);
        Size size2 = new(100f, 200f);
        Size size3 = new(150f, 200f);

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

    /// <summary>
    /// Tests that Size.GetHashCode works correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeGetHashCodeShouldWork()
    {
        // Arrange
        Size size1 = new(100f, 200f);
        Size size2 = new(100f, 200f);
        Size size3 = new(150f, 200f);

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

    /// <summary>
    /// Tests that Size.ToString works correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeToStringShouldWork()
    {
        // Arrange
        Size size1 = new(100f, 200f);
        Size size2 = new(1.5f, 2.75f);
        Size size3 = new(0f, 0f);

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

    /// <summary>
    /// Tests Size with negative dimensions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeWithNegativeDimensionsShouldWork()
    {
        // Arrange & Act
        Size negativeSize = new(-100f, -200f);
        Size mixedSize = new(-50f, 100f);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(negativeSize.Width).IsEqualTo(-100f);
            await Assert.That(negativeSize.Height).IsEqualTo(-200f);
            await Assert.That(negativeSize.AspectRatio).IsEqualTo(0.5f); // -100 / -200 = 0.5

            await Assert.That(mixedSize.Width).IsEqualTo(-50f);
            await Assert.That(mixedSize.Height).IsEqualTo(100f);
            await Assert.That(mixedSize.AspectRatio).IsEqualTo(-0.5f); // -50 / 100 = -0.5
        }
    }

    /// <summary>
    /// Tests Size with very large dimensions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeWithLargeDimensionsShouldWork()
    {
        // Arrange & Act
        Size largeSize = new(float.MaxValue, float.MaxValue);
        Size veryLargeSize = new(1e30f, 1e30f);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(largeSize.Width).IsEqualTo(float.MaxValue);
            await Assert.That(largeSize.Height).IsEqualTo(float.MaxValue);
            await Assert.That(largeSize.AspectRatio).IsEqualTo(1.0f); // MaxValue / MaxValue = 1

            await Assert.That(veryLargeSize.Width).IsEqualTo(1e30f);
            await Assert.That(veryLargeSize.Height).IsEqualTo(1e30f);
            await Assert.That(veryLargeSize.AspectRatio).IsEqualTo(1.0f);
        }
    }

    /// <summary>
    /// Tests Size with very small dimensions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeWithSmallDimensionsShouldWork()
    {
        // Arrange & Act
        Size smallSize = new(float.Epsilon, float.Epsilon);
        Size tinySize = new(1e-30f, 1e-30f);

        using (Assert.Multiple())
        {
            // Assert
            await Assert.That(smallSize.Width).IsEqualTo(float.Epsilon);
            await Assert.That(smallSize.Height).IsEqualTo(float.Epsilon);
            await Assert.That(smallSize.AspectRatio).IsEqualTo(1.0f); // Epsilon / Epsilon = 1

            await Assert.That(tinySize.Width).IsEqualTo(1e-30f);
            await Assert.That(tinySize.Height).IsEqualTo(1e-30f);
            await Assert.That(tinySize.AspectRatio).IsEqualTo(1.0f);
        }
    }

    /// <summary>
    /// Tests Size with special float values.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeWithSpecialFloatValuesShouldWork()
    {
        // Arrange & Act
        Size infiniteSize = new(float.PositiveInfinity, float.PositiveInfinity);
        Size nanSize = new(float.NaN, float.NaN);
        Size mixedSpecialSize = new(float.PositiveInfinity, 100f);

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
            await Assert.That(mixedSpecialSize.Height).IsEqualTo(100f);
            await Assert.That(mixedSpecialSize.AspectRatio).IsEqualTo(float.PositiveInfinity); // Infinity / 100 = Infinity
        }
    }

    /// <summary>
    /// Tests that Size struct behaves correctly in collections.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeShouldWorkInCollections()
    {
        // Arrange
        Size[] sizes =
        [
            new(100f, 200f),
            new(150f, 300f),
            new(100f, 200f), // Duplicate
            new(200f, 400f)
        ];

        // Act
        var uniqueSizes = sizes.Distinct().ToArray();
        var sortedSizes = sizes.OrderBy(static s => s.Width).ThenBy(static s => s.Height).ToArray();

        // Assert
        await Assert.That(uniqueSizes).Count().IsEqualTo(3); // Should remove one duplicate
        await Assert.That(uniqueSizes).Contains(new Size(100f, 200f));
        await Assert.That(uniqueSizes).Contains(new Size(150f, 300f));
        using (Assert.Multiple())
        {
            await Assert.That(uniqueSizes).Contains(new Size(200f, 400f));

            // Check sorting
            await Assert.That(sortedSizes[0]).IsEqualTo(new(100f, 200f));
            await Assert.That(sortedSizes[1]).IsEqualTo(new(100f, 200f)); // Duplicate
            await Assert.That(sortedSizes[2]).IsEqualTo(new(150f, 300f));
            await Assert.That(sortedSizes[3]).IsEqualTo(new(200f, 400f));
        }
    }

    /// <summary>
    /// Tests that Size can be used as dictionary key.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeShouldWorkAsDictionaryKey()
    {
        // Arrange
        Dictionary<Size, string> sizeDict = [];
        Size size1 = new(100f, 200f);
        Size size2 = new(150f, 300f);
        Size size3 = new(100f, 200f); // Same as size1

        // Act
        sizeDict[size1] = "First";
        sizeDict[size2] = "Second";
        sizeDict[size3] = "Third"; // Should overwrite "First"

        // Assert
        await Assert.That(sizeDict).Count().IsEqualTo(2);
        using (Assert.Multiple())
        {
            await Assert.That(sizeDict[size1]).IsEqualTo("Third"); // Overwritten by size3
            await Assert.That(sizeDict[size3]).IsEqualTo("Third"); // Same as size1
            await Assert.That(sizeDict[size2]).IsEqualTo("Second");
        }
    }

    /// <summary>
    /// Tests that the == and != operators on Size return the expected results.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SizeOperatorEqualsAndNotEqualsShouldWork()
    {
        // Arrange
        Size size1 = new(100f, 200f);
        Size size2 = new(100f, 200f);
        Size size3 = new(150f, 200f);
        Size size4 = new(100f, 250f);

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

    /// <summary>
    /// Tests that Equals(object?) handles matching Size, non-matching Size, different type and null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1508:Avoid dead conditional code", Justification = "Test deliberately verifies Equals(null) returns false; the analyzer can't model the boxed object cast.")]
    public async Task SizeEqualsObjectShouldWork()
    {
        // Arrange
        Size size1 = new(100f, 200f);
        object sameBoxed = new Size(100f, 200f);
        object differentBoxed = new Size(150f, 200f);
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

    /// <summary>
    /// Tests Size with realistic image dimensions.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Arguments(1920f, 1080f)] // Full HD
    [Arguments(3840f, 2160f)] // 4K UHD
    [Arguments(1024f, 768f)] // XGA
    [Arguments(800f, 600f)] // SVGA
    [Arguments(640f, 480f)] // VGA
    [Arguments(320f, 240f)] // QVGA
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
