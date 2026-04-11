// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;

namespace Akavache.Tests;

/// <summary>
/// Tests for the internal <see cref="PreserveAttribute"/> class,
/// verifying both constructors and all settable properties.
/// </summary>
[Category("Akavache")]
public class PreserveAttributeTests
{
    /// <summary>
    /// Tests that the parameterless constructor creates an instance with default property values.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ParameterlessConstructorShouldCreateInstanceWithDefaults()
    {
        var attribute = new PreserveAttribute();

        await Assert.That(attribute.AllMembers).IsFalse();
        await Assert.That(attribute.Conditional).IsFalse();
    }

    /// <summary>
    /// Tests that the two-parameter constructor sets AllMembers and Conditional correctly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ParameterizedConstructorShouldSetProperties()
    {
        var attribute = new PreserveAttribute(allMembers: true, conditional: true);

        await Assert.That(attribute.AllMembers).IsTrue();
        await Assert.That(attribute.Conditional).IsTrue();
    }

    /// <summary>
    /// Tests that the two-parameter constructor handles false values correctly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ParameterizedConstructorShouldHandleFalseValues()
    {
        var attribute = new PreserveAttribute(allMembers: false, conditional: false);

        await Assert.That(attribute.AllMembers).IsFalse();
        await Assert.That(attribute.Conditional).IsFalse();
    }

    /// <summary>
    /// Tests that the AllMembers property setter works after construction.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AllMembersSetterShouldUpdateValue()
    {
        var attribute = new PreserveAttribute();

        attribute.AllMembers = true;

        await Assert.That(attribute.AllMembers).IsTrue();

        attribute.AllMembers = false;

        await Assert.That(attribute.AllMembers).IsFalse();
    }

    /// <summary>
    /// Tests that the attribute can be applied to any target (as declared by <see cref="AttributeTargets.All"/>).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AttributeShouldTargetAll()
    {
        var usageAttribute = typeof(PreserveAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        await Assert.That(usageAttribute.ValidOn).IsEqualTo(AttributeTargets.All);
    }
}
