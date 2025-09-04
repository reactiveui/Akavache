// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Drawing;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.Drawing Size struct functionality.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class SizeTests
{
    /// <summary>
    /// Tests that Size constructor sets properties correctly.
    /// </summary>
    [Test]
    public void SizeConstructorShouldSetPropertiesCorrectly()
    {
        // Act
        var size = new Size(100.5f, 200.75f);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(size.Width, Is.EqualTo(100.5f));
            Assert.That(size.Height, Is.EqualTo(200.75f));
        }
    }

    /// <summary>
    /// Tests that Size with zero dimensions works correctly.
    /// </summary>
    [Test]
    public void SizeWithZeroDimensionsShouldWork()
    {
        // Act
        var zeroSize = new Size(0, 0);
        var zeroWidth = new Size(0, 100);
        var zeroHeight = new Size(100, 0);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(zeroSize.Width, Is.Zero);
            Assert.That(zeroSize.Height, Is.Zero);
            Assert.That(zeroWidth.Width, Is.Zero);
            Assert.That(zeroWidth.Height, Is.EqualTo(100f));
            Assert.That(zeroHeight.Width, Is.EqualTo(100f));
            Assert.That(zeroHeight.Height, Is.Zero);
        }
    }

    /// <summary>
    /// Tests that AspectRatio calculation works correctly.
    /// </summary>
    /// <param name="width">The width to test.</param>
    /// <param name="height">The height to test.</param>
    /// <param name="expectedRatio">The expected aspect ratio.</param>
    [TestCase(100f, 100f, 1.0f)] // Square
    [TestCase(200f, 100f, 2.0f)] // 2:1 landscape
    [TestCase(100f, 200f, 0.5f)] // 1:2 portrait
    [TestCase(16f, 9f, 1.777778f)] // 16:9 widescreen (approximately)
    [TestCase(4f, 3f, 1.333333f)] // 4:3 standard (approximately)
    [TestCase(100f, 0f, 0f)] // Zero height
    [Test]
    public void AspectRatioShouldBeCalculatedCorrectly(float width, float height, float expectedRatio)
    {
        // Arrange
        var size = new Size(width, height);

        // Act
        var actualRatio = size.AspectRatio;

        // Assert
        // Check for equality within a small tolerance (5 decimal places)
        Assert.That(actualRatio, Is.EqualTo(expectedRatio).Within(0.00001f));
    }

    /// <summary>
    /// Tests that AspectRatio handles zero width correctly.
    /// </summary>
    [Test]
    public void AspectRatioWithZeroWidthShouldReturnZero()
    {
        // Arrange
        var size = new Size(0f, 100f);

        // Act
        var ratio = size.AspectRatio;

        // Assert
        Assert.That(ratio, Is.Zero);
    }

    /// <summary>
    /// Tests that Size equality operators work correctly.
    /// </summary>
    [Test]
    public void SizeEqualityOperatorsShouldWork()
    {
        // Arrange
        var size1 = new Size(100f, 200f);
        var size2 = new Size(100f, 200f);
        var size3 = new Size(150f, 200f);
        var size4 = new Size(100f, 250f);

        // Act & Assert - Equality
        Assert.That(size1, Is.EqualTo(size2));
        Assert.That(size1, Is.Not.EqualTo(size3));
        Assert.That(size1, Is.Not.EqualTo(size4));

        // Act & Assert - Inequality
        Assert.That(size1, Is.EqualTo(size2));
        Assert.That(size1, Is.Not.EqualTo(size3));
        Assert.That(size1, Is.Not.EqualTo(size4));
    }

    /// <summary>
    /// Tests that Size.Equals method works correctly.
    /// </summary>
    [Test]
    public void SizeEqualsShouldWork()
    {
        // Arrange
        var size1 = new Size(100f, 200f);
        var size2 = new Size(100f, 200f);
        var size3 = new Size(150f, 200f);

        using (Assert.EnterMultipleScope())
        {
            // Act & Assert - Equals with Size
            Assert.That(size1, Is.EqualTo(size2));
            Assert.That(size1, Is.Not.EqualTo(size3));

            // Act & Assert - Equals with object
            Assert.That(size1, Is.EqualTo((object)size2));
            Assert.That(size1, Is.Not.EqualTo((object)size3));
        }
    }

    /// <summary>
    /// Tests that Size.GetHashCode works correctly.
    /// </summary>
    [Test]
    public void SizeGetHashCodeShouldWork()
    {
        // Arrange
        var size1 = new Size(100f, 200f);
        var size2 = new Size(100f, 200f);
        var size3 = new Size(150f, 200f);

        // Act
        var hash1 = size1.GetHashCode();
        var hash2 = size2.GetHashCode();
        var hash3 = size3.GetHashCode();

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(hash2, Is.EqualTo(hash1)); // Equal objects should have equal hash codes
            Assert.That(hash3, Is.Not.EqualTo(hash1)); // Different objects should typically have different hash codes
        }
    }

    /// <summary>
    /// Tests that Size.ToString works correctly.
    /// </summary>
    [Test]
    public void SizeToStringShouldWork()
    {
        // Arrange
        var size1 = new Size(100f, 200f);
        var size2 = new Size(1.5f, 2.75f);
        var size3 = new Size(0f, 0f);

        // Act
        var str1 = size1.ToString();
        var str2 = size2.ToString();
        var str3 = size3.ToString();

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(str1, Is.EqualTo("100x200"));
            Assert.That(str2, Is.EqualTo("1.5x2.75"));
            Assert.That(str3, Is.EqualTo("0x0"));
        }
    }

    /// <summary>
    /// Tests Size with negative dimensions.
    /// </summary>
    [Test]
    public void SizeWithNegativeDimensionsShouldWork()
    {
        // Arrange & Act
        var negativeSize = new Size(-100f, -200f);
        var mixedSize = new Size(-50f, 100f);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(negativeSize.Width, Is.EqualTo(-100f));
            Assert.That(negativeSize.Height, Is.EqualTo(-200f));
            Assert.That(negativeSize.AspectRatio, Is.EqualTo(0.5f)); // -100 / -200 = 0.5

            Assert.That(mixedSize.Width, Is.EqualTo(-50f));
            Assert.That(mixedSize.Height, Is.EqualTo(100f));
            Assert.That(mixedSize.AspectRatio, Is.EqualTo(-0.5f)); // -50 / 100 = -0.5
        }
    }

    /// <summary>
    /// Tests Size with very large dimensions.
    /// </summary>
    [Test]
    public void SizeWithLargeDimensionsShouldWork()
    {
        // Arrange & Act
        var largeSize = new Size(float.MaxValue, float.MaxValue);
        var veryLargeSize = new Size(1e30f, 1e30f);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(largeSize.Width, Is.EqualTo(float.MaxValue));
            Assert.That(largeSize.Height, Is.EqualTo(float.MaxValue));
            Assert.That(largeSize.AspectRatio, Is.EqualTo(1.0f)); // MaxValue / MaxValue = 1

            Assert.That(veryLargeSize.Width, Is.EqualTo(1e30f));
            Assert.That(veryLargeSize.Height, Is.EqualTo(1e30f));
            Assert.That(veryLargeSize.AspectRatio, Is.EqualTo(1.0f));
        }
    }

    /// <summary>
    /// Tests Size with very small dimensions.
    /// </summary>
    [Test]
    public void SizeWithSmallDimensionsShouldWork()
    {
        // Arrange & Act
        var smallSize = new Size(float.Epsilon, float.Epsilon);
        var tinySize = new Size(1e-30f, 1e-30f);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(smallSize.Width, Is.EqualTo(float.Epsilon));
            Assert.That(smallSize.Height, Is.EqualTo(float.Epsilon));
            Assert.That(smallSize.AspectRatio, Is.EqualTo(1.0f)); // Epsilon / Epsilon = 1

            Assert.That(tinySize.Width, Is.EqualTo(1e-30f));
            Assert.That(tinySize.Height, Is.EqualTo(1e-30f));
            Assert.That(tinySize.AspectRatio, Is.EqualTo(1.0f));
        }
    }

    /// <summary>
    /// Tests Size with special float values.
    /// </summary>
    [Test]
    public void SizeWithSpecialFloatValuesShouldWork()
    {
        // Arrange & Act
        var infiniteSize = new Size(float.PositiveInfinity, float.PositiveInfinity);
        var nanSize = new Size(float.NaN, float.NaN);
        var mixedSpecialSize = new Size(float.PositiveInfinity, 100f);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(infiniteSize.Width, Is.EqualTo(float.PositiveInfinity));
            Assert.That(infiniteSize.Height, Is.EqualTo(float.PositiveInfinity));
            Assert.That(infiniteSize.AspectRatio, Is.NaN); // Infinity / Infinity = NaN

            Assert.That(nanSize.Width, Is.NaN);
            Assert.That(nanSize.Height, Is.NaN);
            Assert.That(nanSize.AspectRatio, Is.NaN);

            Assert.That(mixedSpecialSize.Width, Is.EqualTo(float.PositiveInfinity));
            Assert.That(mixedSpecialSize.Height, Is.EqualTo(100f));
            Assert.That(mixedSpecialSize.AspectRatio, Is.EqualTo(float.PositiveInfinity)); // Infinity / 100 = Infinity
        }
    }

    /// <summary>
    /// Tests that Size struct behaves correctly in collections.
    /// </summary>
    [Test]
    public void SizeShouldWorkInCollections()
    {
        // Arrange
        Size[] sizes =
        [
            new Size(100f, 200f),
            new Size(150f, 300f),
            new Size(100f, 200f), // Duplicate
            new Size(200f, 400f)
        ];

        // Act
        var uniqueSizes = sizes.Distinct().ToArray();
        var sortedSizes = sizes.OrderBy(static s => s.Width).ThenBy(static s => s.Height).ToArray();

        // Assert
        Assert.That(uniqueSizes, Has.Length.EqualTo(3)); // Should remove one duplicate
        Assert.That(uniqueSizes, Does.Contain(new Size(100f, 200f)));
        Assert.That(uniqueSizes, Does.Contain(new Size(150f, 300f)));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(uniqueSizes, Does.Contain(new Size(200f, 400f)));

            // Check sorting
            Assert.That(sortedSizes[0], Is.EqualTo(new Size(100f, 200f)));
            Assert.That(sortedSizes[1], Is.EqualTo(new Size(100f, 200f))); // Duplicate
            Assert.That(sortedSizes[2], Is.EqualTo(new Size(150f, 300f)));
            Assert.That(sortedSizes[3], Is.EqualTo(new Size(200f, 400f)));
        }
    }

    /// <summary>
    /// Tests that Size can be used as dictionary key.
    /// </summary>
    [Test]
    public void SizeShouldWorkAsDictionaryKey()
    {
        // Arrange
        var sizeDict = new Dictionary<Size, string>();
        var size1 = new Size(100f, 200f);
        var size2 = new Size(150f, 300f);
        var size3 = new Size(100f, 200f); // Same as size1

        // Act
        sizeDict[size1] = "First";
        sizeDict[size2] = "Second";
        sizeDict[size3] = "Third"; // Should overwrite "First"

        // Assert
        Assert.That(sizeDict, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sizeDict[size1], Is.EqualTo("Third")); // Overwritten by size3
            Assert.That(sizeDict[size3], Is.EqualTo("Third")); // Same as size1
            Assert.That(sizeDict[size2], Is.EqualTo("Second"));
        }
    }

    /// <summary>
    /// Tests Size with realistic image dimensions.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    [TestCase(1920f, 1080f)] // Full HD
    [TestCase(3840f, 2160f)] // 4K UHD
    [TestCase(1024f, 768f)] // XGA
    [TestCase(800f, 600f)] // SVGA
    [TestCase(640f, 480f)] // VGA
    [TestCase(320f, 240f)] // QVGA
    [Test]
    public void SizeWithRealisticImageDimensionsShouldWork(float width, float height)
    {
        // Arrange & Act
        var size = new Size(width, height);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(size.Width, Is.EqualTo(width));
            Assert.That(size.Height, Is.EqualTo(height));
            Assert.That(size.AspectRatio, Is.GreaterThan(0));
            Assert.That(size.ToString(), Does.Contain("x"));
        }
    }
}
