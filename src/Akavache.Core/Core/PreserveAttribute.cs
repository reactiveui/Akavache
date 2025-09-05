// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// Specifies that a class, method, or other code element should be preserved during trimming and ahead-of-time compilation.
/// This attribute helps ensure that reflection-dependent code remains available at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
internal sealed class PreserveAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreserveAttribute"/> class with the specified preservation options.
    /// </summary>
    /// <param name="allMembers">A value indicating whether all members of the target should be preserved.</param>
    /// <param name="conditional">A value indicating whether preservation is conditional.</param>
    public PreserveAttribute(bool allMembers, bool conditional)
    {
        AllMembers = allMembers;
        Conditional = conditional;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PreserveAttribute"/> class.
    /// </summary>
    public PreserveAttribute()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether all members of the target should be preserved.
    /// </summary>
    public bool AllMembers
    {
        get;
        set;
    }

    /// <summary>
    /// Gets a value indicating whether preservation is conditional.
    /// </summary>
    public bool Conditional { get; }
}
