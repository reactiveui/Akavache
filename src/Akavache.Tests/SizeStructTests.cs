// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
using Akavache.Drawing;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Skeleton tests for Size struct operations.
/// </summary>
[TestFixture]
[Category("Drawing")]
public class SizeStructTests
{
    /// <summary>
    /// Default struct should have zero width and height.
    /// </summary>
    [Test]
    public void Size_Default_IsZero()
    {
        var s = default(Size);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(s.Width, Is.Zero);
            Assert.That(s.Height, Is.Zero);
        }
    }

    /// <summary>
    /// Equality operators and Equals behave correctly.
    /// </summary>
    [Test]
    public void Size_Equals_Works()
    {
        var a = new Size(10, 20);
        var b = new Size(10, 20);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.EqualTo(b));
        }
    }

    /// <summary>
    /// Hash codes match for identical values.
    /// </summary>
    [Test]
    public void Size_GetHashCode_Consistent()
    {
        var a = new Size(5, 7);
        var b = new Size(5, 7);
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    /// <summary>
    /// Inequality operators behave correctly.
    /// </summary>
    [Test]
    public void Size_Inequality_Works()
    {
        var a = new Size(1, 2);
        var b = new Size(2, 1);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}
