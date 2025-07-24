// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ReactiveMarbles.CacheDatabase.Core;
using Splat;

namespace ReactiveMarbles.CacheDatabase.Drawing;

/// <summary>
/// Setup registrations for the drawing application.
/// </summary>
[Preserve(AllMembers = true)]
#if NET8_0_OR_GREATER
[RequiresUnreferencedCode("Registrations for ReactiveMarbles.CacheDatabase.Drawing")]
[RequiresDynamicCode("Registrations for ReactiveMarbles.CacheDatabase.Drawing")]
#endif
public static class Registrations
{
    /// <summary>
    /// Registers the platform bitmap loader for drawing support.
    /// </summary>
    public static void RegisterBitmapLoader()
    {
#if !NETSTANDARD
        Locator.CurrentMutable.RegisterPlatformBitmapLoader();
#endif
    }

    /// <summary>
    /// Initializes drawing support for ReactiveMarbles.CacheDatabase.
    /// </summary>
    public static void Initialize() => RegisterBitmapLoader();
}
