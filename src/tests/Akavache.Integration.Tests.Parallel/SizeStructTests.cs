// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// System first

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Skeleton tests for Size struct operations.</summary>
[Category("Drawing")]
public class SizeStructTests
{
    /// <summary>Width shared by the two sizes built for the equality checks.</summary>
    private const float EqualPairWidth = 10;

    /// <summary>Height shared by the two sizes built for the equality checks.</summary>
    private const float EqualPairHeight = 20;

    /// <summary>Width shared by the two sizes whose hash codes must agree.</summary>
    private const float HashedPairWidth = 5;

    /// <summary>Height shared by the two sizes whose hash codes must agree.</summary>
    private const float HashedPairHeight = 7;

    /// <summary>The smaller of the two extents swapped between the sizes compared for inequality.</summary>
    private const float NarrowExtent = 1;

    /// <summary>The larger of the two extents swapped between the sizes compared for inequality.</summary>
    private const float WideExtent = 2;

    /// <summary>Default struct should have zero width and height.</summary>
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

    /// <summary>Equality operators and Equals behave correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Size_Equals_Works()
    {
        Size a = new(EqualPairWidth, EqualPairHeight);
        Size b = new(EqualPairWidth, EqualPairHeight);
        using (Assert.Multiple())
        {
            await Assert.That(a).IsEqualTo(b);
            await Assert.That(a).IsEqualTo(b);
            await Assert.That(a).IsEqualTo(b);
        }
    }

    /// <summary>Hash codes match for identical values.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Size_GetHashCode_Consistent()
    {
        Size a = new(HashedPairWidth, HashedPairHeight);
        Size b = new(HashedPairWidth, HashedPairHeight);
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    /// <summary>Inequality operators behave correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Size_Inequality_Works()
    {
        Size a = new(NarrowExtent, WideExtent);
        Size b = new(WideExtent, NarrowExtent);
        using (Assert.Multiple())
        {
            await Assert.That(a).IsNotEqualTo(b);
            await Assert.That(a).IsNotEqualTo(b);
        }
    }
}
