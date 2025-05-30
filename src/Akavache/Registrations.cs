﻿// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Core;
using Splat;

namespace Akavache;

/// <summary>
/// Setup registrations for the application.
/// </summary>
[Preserve(AllMembers = true)]
#if NET8_0_OR_GREATER
[RequiresUnreferencedCode("Registrations for Akavache")]
[RequiresDynamicCode("Registrations for Akavache")]
#endif
public class Registrations : IWantsToRegisterStuff
{
    /// <summary>
    /// Registers the application name. This will create storage location for our storage.
    /// </summary>
    /// <param name="applicationName">The name of the application that is running.</param>
    public static void Start(string applicationName) => Sqlite3.Registrations.Start(applicationName, SQLitePCL.Batteries_V2.Init);

    /// <inheritdoc />
    public void Register(IMutableDependencyResolver resolver, IReadonlyDependencyResolver readonlyDependencyResolver) => SQLitePCL.Batteries_V2.Init();
}
