// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// System first
using Akavache.Drawing;

namespace Akavache.Tests;

/// <summary>
/// Skeleton tests for Size struct operations.
/// </summary>
[Category("Drawing")]
public class SizeStructTests
{
    /// <summary>
    /// Default struct should have zero width and height.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Size_Default_IsZero()
    {
        var s = default(Size);
        using (Assert.Multiple())
        {
            await Assert.That(s.Width).IsZero();
            await Assert.That(s.Height).IsZero();
        }
    }

    /// <summary>
    /// Equality operators and Equals behave correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Size_Equals_Works()
    {
        var a = new Size(10, 20);
        var b = new Size(10, 20);
        using (Assert.Multiple())
        {
            await Assert.That(a).IsEqualTo(b);
            await Assert.That(a).IsEqualTo(b);
            await Assert.That(a).IsEqualTo(b);
        }
    }

    /// <summary>
    /// Hash codes match for identical values.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Size_GetHashCode_Consistent()
    {
        var a = new Size(5, 7);
        var b = new Size(5, 7);
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    /// <summary>
    /// Inequality operators behave correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Size_Inequality_Works()
    {
        var a = new Size(1, 2);
        var b = new Size(2, 1);
        using (Assert.Multiple())
        {
            await Assert.That(a).IsNotEqualTo(b);
            await Assert.That(a).IsNotEqualTo(b);
        }
    }
}
