// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

[AttributeUsage(AttributeTargets.All)]
internal sealed class PreserveAttribute : Attribute
{
    public PreserveAttribute(bool allMembers, bool conditional)
    {
        AllMembers = allMembers;
        Conditional = conditional;
    }

    public PreserveAttribute()
    {
    }

    public bool AllMembers
    {
        get;
        set;
    }

    public bool Conditional { get; }
}