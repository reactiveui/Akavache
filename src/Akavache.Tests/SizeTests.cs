// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Drawing;
using Xunit;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.Drawing Size struct functionality.
/// </summary>
public class SizeTests
{
    /// <summary>
    /// Tests that Size constructor sets properties correctly.
    /// </summary>
    [Fact]
    public void SizeConstructorShouldSetPropertiesCorrectly()
    {
        // Act
        var size = new Size(100.5f, 200.75f);

        // Assert
        Assert.Equal(100.5f, size.Width);
        Assert.Equal(200.75f, size.Height);
    }

    /// <summary>
    /// Tests that Size with zero dimensions works correctly.
    /// </summary>
    [Fact]
    public void SizeWithZeroDimensionsShouldWork()
    {
        // Act
        var zeroSize = new Size(0, 0);
        var zeroWidth = new Size(0, 100);
        var zeroHeight = new Size(100, 0);

        // Assert
        Assert.Equal(0f, zeroSize.Width);
        Assert.Equal(0f, zeroSize.Height);
        Assert.Equal(0f, zeroWidth.Width);
        Assert.Equal(100f, zeroWidth.Height);
        Assert.Equal(100f, zeroHeight.Width);
        Assert.Equal(0f, zeroHeight.Height);
    }

    /// <summary>
    /// Tests that AspectRatio calculation works correctly.
    /// </summary>
    /// <param name="width">The width to test.</param>
    /// <param name="height">The height to test.</param>
    /// <param name="expectedRatio">The expected aspect ratio.</param>
    [Theory]
    [InlineData(100f, 100f, 1.0f)] // Square
    [InlineData(200f, 100f, 2.0f)] // 2:1 landscape
    [InlineData(100f, 200f, 0.5f)] // 1:2 portrait
    [InlineData(16f, 9f, 1.777778f)] // 16:9 widescreen (approximately)
    [InlineData(4f, 3f, 1.333333f)] // 4:3 standard (approximately)
    [InlineData(100f, 0f, 0f)] // Zero height
    public void AspectRatioShouldBeCalculatedCorrectly(float width, float height, float expectedRatio)
    {
        // Arrange
        var size = new Size(width, height);

        // Act
        var actualRatio = size.AspectRatio;

        // Assert
        Assert.Equal(expectedRatio, actualRatio, 5); // 5 decimal places precision
    }

    /// <summary>
    /// Tests that AspectRatio handles zero width correctly.
    /// </summary>
    [Fact]
    public void AspectRatioWithZeroWidthShouldReturnZero()
    {
        // Arrange
        var size = new Size(0f, 100f);

        // Act
        var ratio = size.AspectRatio;

        // Assert
        Assert.Equal(0f, ratio);
    }

    /// <summary>
    /// Tests that Size equality operators work correctly.
    /// </summary>
    [Fact]
    public void SizeEqualityOperatorsShouldWork()
    {
        // Arrange
        var size1 = new Size(100f, 200f);
        var size2 = new Size(100f, 200f);
        var size3 = new Size(150f, 200f);
        var size4 = new Size(100f, 250f);

        // Act & Assert - Equality
        Assert.True(size1 == size2);
        Assert.False(size1 == size3);
        Assert.False(size1 == size4);

        // Act & Assert - Inequality
        Assert.False(size1 != size2);
        Assert.True(size1 != size3);
        Assert.True(size1 != size4);
    }

    /// <summary>
    /// Tests that Size.Equals method works correctly.
    /// </summary>
    [Fact]
    public void SizeEqualsShouldWork()
    {
        // Arrange
        var size1 = new Size(100f, 200f);
        var size2 = new Size(100f, 200f);
        var size3 = new Size(150f, 200f);

        // Act & Assert - Equals with Size
        Assert.True(size1.Equals(size2));
        Assert.False(size1.Equals(size3));

        // Act & Assert - Equals with object
        Assert.True(size1.Equals((object)size2));
        Assert.False(size1.Equals((object)size3));
        Assert.False(size1.Equals(null));
        Assert.False(size1.Equals("not a size"));
        Assert.False(size1.Equals(42));
    }

    /// <summary>
    /// Tests that Size.GetHashCode works correctly.
    /// </summary>
    [Fact]
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

        // Assert
        Assert.Equal(hash1, hash2); // Equal objects should have equal hash codes
        Assert.NotEqual(hash1, hash3); // Different objects should typically have different hash codes
    }

    /// <summary>
    /// Tests that Size.ToString works correctly.
    /// </summary>
    [Fact]
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

        // Assert
        Assert.Equal("100x200", str1);
        Assert.Equal("1.5x2.75", str2);
        Assert.Equal("0x0", str3);
    }

    /// <summary>
    /// Tests Size with negative dimensions.
    /// </summary>
    [Fact]
    public void SizeWithNegativeDimensionsShouldWork()
    {
        // Arrange & Act
        var negativeSize = new Size(-100f, -200f);
        var mixedSize = new Size(-50f, 100f);

        // Assert
        Assert.Equal(-100f, negativeSize.Width);
        Assert.Equal(-200f, negativeSize.Height);
        Assert.Equal(0.5f, negativeSize.AspectRatio); // -100 / -200 = 0.5

        Assert.Equal(-50f, mixedSize.Width);
        Assert.Equal(100f, mixedSize.Height);
        Assert.Equal(-0.5f, mixedSize.AspectRatio); // -50 / 100 = -0.5
    }

    /// <summary>
    /// Tests Size with very large dimensions.
    /// </summary>
    [Fact]
    public void SizeWithLargeDimensionsShouldWork()
    {
        // Arrange & Act
        var largeSize = new Size(float.MaxValue, float.MaxValue);
        var veryLargeSize = new Size(1e30f, 1e30f);

        // Assert
        Assert.Equal(float.MaxValue, largeSize.Width);
        Assert.Equal(float.MaxValue, largeSize.Height);
        Assert.Equal(1.0f, largeSize.AspectRatio); // MaxValue / MaxValue = 1

        Assert.Equal(1e30f, veryLargeSize.Width);
        Assert.Equal(1e30f, veryLargeSize.Height);
        Assert.Equal(1.0f, veryLargeSize.AspectRatio);
    }

    /// <summary>
    /// Tests Size with very small dimensions.
    /// </summary>
    [Fact]
    public void SizeWithSmallDimensionsShouldWork()
    {
        // Arrange & Act
        var smallSize = new Size(float.Epsilon, float.Epsilon);
        var tinySize = new Size(1e-30f, 1e-30f);

        // Assert
        Assert.Equal(float.Epsilon, smallSize.Width);
        Assert.Equal(float.Epsilon, smallSize.Height);
        Assert.Equal(1.0f, smallSize.AspectRatio); // Epsilon / Epsilon = 1

        Assert.Equal(1e-30f, tinySize.Width);
        Assert.Equal(1e-30f, tinySize.Height);
        Assert.Equal(1.0f, tinySize.AspectRatio);
    }

    /// <summary>
    /// Tests Size with special float values.
    /// </summary>
    [Fact]
    public void SizeWithSpecialFloatValuesShouldWork()
    {
        // Arrange & Act
        var infiniteSize = new Size(float.PositiveInfinity, float.PositiveInfinity);
        var nanSize = new Size(float.NaN, float.NaN);
        var mixedSpecialSize = new Size(float.PositiveInfinity, 100f);

        // Assert
        Assert.Equal(float.PositiveInfinity, infiniteSize.Width);
        Assert.Equal(float.PositiveInfinity, infiniteSize.Height);
        Assert.True(float.IsNaN(infiniteSize.AspectRatio)); // Infinity / Infinity = NaN

        Assert.True(float.IsNaN(nanSize.Width));
        Assert.True(float.IsNaN(nanSize.Height));
        Assert.True(float.IsNaN(nanSize.AspectRatio));

        Assert.Equal(float.PositiveInfinity, mixedSpecialSize.Width);
        Assert.Equal(100f, mixedSpecialSize.Height);
        Assert.Equal(float.PositiveInfinity, mixedSpecialSize.AspectRatio); // Infinity / 100 = Infinity
    }

    /// <summary>
    /// Tests that Size struct behaves correctly in collections.
    /// </summary>
    [Fact]
    public void SizeShouldWorkInCollections()
    {
        // Arrange
        var sizes = new[]
        {
            new Size(100f, 200f),
            new Size(150f, 300f),
            new Size(100f, 200f), // Duplicate
            new Size(200f, 400f)
        };

        // Act
        var uniqueSizes = sizes.Distinct().ToArray();
        var sortedSizes = sizes.OrderBy(s => s.Width).ThenBy(s => s.Height).ToArray();

        // Assert
        Assert.Equal(3, uniqueSizes.Length); // Should remove one duplicate
        Assert.Contains(new Size(100f, 200f), uniqueSizes);
        Assert.Contains(new Size(150f, 300f), uniqueSizes);
        Assert.Contains(new Size(200f, 400f), uniqueSizes);

        // Check sorting
        Assert.Equal(new Size(100f, 200f), sortedSizes[0]);
        Assert.Equal(new Size(100f, 200f), sortedSizes[1]); // Duplicate
        Assert.Equal(new Size(150f, 300f), sortedSizes[2]);
        Assert.Equal(new Size(200f, 400f), sortedSizes[3]);
    }

    /// <summary>
    /// Tests that Size can be used as dictionary key.
    /// </summary>
    [Fact]
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
        Assert.Equal(2, sizeDict.Count);
        Assert.Equal("Third", sizeDict[size1]); // Overwritten by size3
        Assert.Equal("Third", sizeDict[size3]); // Same as size1
        Assert.Equal("Second", sizeDict[size2]);
    }

    /// <summary>
    /// Tests Size with realistic image dimensions.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    [Theory]
    [InlineData(1920f, 1080f)] // Full HD
    [InlineData(3840f, 2160f)] // 4K UHD
    [InlineData(1024f, 768f)] // XGA
    [InlineData(800f, 600f)] // SVGA
    [InlineData(640f, 480f)] // VGA
    [InlineData(320f, 240f)] // QVGA
    public void SizeWithRealisticImageDimensionsShouldWork(float width, float height)
    {
        // Arrange & Act
        var size = new Size(width, height);

        // Assert
        Assert.Equal(width, size.Width);
        Assert.Equal(height, size.Height);
        Assert.True(size.AspectRatio > 0);
        Assert.Contains("x", size.ToString());
    }
}
