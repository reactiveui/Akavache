// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Drawing;

/// <summary>
/// Represents the size of an image.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Size"/> struct.
/// </remarks>
/// <param name="width">The width.</param>
/// <param name="height">The height.</param>
public readonly struct Size(float width, float height) : IEquatable<Size>
{
    /// <summary>
    /// Gets the width.
    /// </summary>
    public float Width { get; } = width;

    /// <summary>
    /// Gets the height.
    /// </summary>
    public float Height { get; } = height;

    /// <summary>
    /// Gets the aspect ratio (width / height).
    /// </summary>
    public float AspectRatio => Height != 0 ? Width / Height : 0;

    /// <summary>
    /// Implements the operator op_Equality.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator ==(in Size left, in Size right) => left.Width.Equals(right.Width) && left.Height.Equals(right.Height);

    /// <summary>
    /// Implements the operator op_Inequality.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
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
