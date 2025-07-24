// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Core;
using Splat;

namespace Akavache.Drawing;

/// <summary>
/// Setup registrations for the application.
/// </summary>
[Preserve(AllMembers = true)]
#if NET8_0_OR_GREATER
[RequiresUnreferencedCode("Registrations for Akavache.Drawing")]
[RequiresDynamicCode("Registrations for Akavache.Drawing")]
#endif
public class Registrations : IWantsToRegisterStuff
{
    /// <inheritdoc />
    public void Register(IMutableDependencyResolver resolver, IReadonlyDependencyResolver readonlyDependencyResolver)
    {
        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

#if !NETSTANDARD
        Locator.CurrentMutable.RegisterPlatformBitmapLoader();
#endif
    }
}
