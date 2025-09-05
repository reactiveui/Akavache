// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Drawing;

/// <summary>
/// Represents the size dimensions of an image with width and height values.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Size"/> struct with the specified dimensions.
/// </remarks>
/// <param name="width">The width dimension in pixels.</param>
/// <param name="height">The height dimension in pixels.</param>
public readonly struct Size(float width, float height) : IEquatable<Size>
{
    /// <summary>
    /// Gets the width dimension in pixels.
    /// </summary>
    public float Width { get; } = width;

    /// <summary>
    /// Gets the height dimension in pixels.
    /// </summary>
    public float Height { get; } = height;

    /// <summary>
    /// Gets the aspect ratio calculated as width divided by height.
    /// </summary>
    public float AspectRatio => Height != 0 ? Width / Height : 0;

    /// <summary>
    /// Determines whether two <see cref="Size"/> instances are equal.
    /// </summary>
    /// <param name="left">The first size to compare.</param>
    /// <param name="right">The second size to compare.</param>
    /// <returns><c>true</c> if the sizes are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(in Size left, in Size right) => left.Width.Equals(right.Width) && left.Height.Equals(right.Height);

    /// <summary>
    /// Determines whether two <see cref="Size"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first size to compare.</param>
    /// <param name="right">The second size to compare.</param>
    /// <returns><c>true</c> if the sizes are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(in Size left, in Size right) => !(left == right);

    /// <inheritdoc/>
    public override string ToString() => $"{Width}x{Height}";

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Size other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(Size other) => Width.Equals(other.Width) && Height.Equals(other.Height);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
#if NETSTANDARD2_0
        // .NET Standard 2.0 compatible hash code generation
        unchecked
        {
            var hash = 17;
            hash = (hash * 23) + Width.GetHashCode();
            hash = (hash * 23) + Height.GetHashCode();
            return hash;
        }
#else
        return HashCode.Combine(Width, Height);
#endif
    }
}
