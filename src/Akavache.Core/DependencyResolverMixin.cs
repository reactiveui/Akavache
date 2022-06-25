﻿// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Splat;

namespace Akavache;

/// <summary>
/// A set of mix-in associated with the <see cref="IDependencyResolver"/> interface.
/// </summary>
[SuppressMessage("FxCop.Analyzer", "CA1307: The behavior of 'string.Replace(string, string)' could vary based on the current user's locale settings", Justification = "Not all platforms allow locale.")]
public static class DependencyResolverMixin
{
    /// <summary>
    /// Initializes a ReactiveUI dependency resolver with classes that
    /// Akavache uses internally.
    /// </summary>
    /// <param name="resolver">The resolver to register Akavache based dependencies against.</param>
    /// <param name="readonlyDependencyResolver">The readonly dependency resolver.</param>
    public static void InitializeAkavache(this IMutableDependencyResolver resolver, IReadonlyDependencyResolver readonlyDependencyResolver)
    {
        var namespaces = new[]
        {
            "Akavache",
            "Akavache.Core",
            "Akavache.Mac",
            "Akavache.Deprecated",
            "Akavache.Mobile",
            "Akavache.Sqlite3",
            "Akavache.Drawing"
        };

        var fdr = typeof(DependencyResolverMixin);

        if (fdr?.AssemblyQualifiedName is null)
        {
            throw new($"Cannot find valid assembly name for the {nameof(DependencyResolverMixin)} class.");
        }

        var assemblyName = new AssemblyName(
            fdr.AssemblyQualifiedName.Replace(fdr.FullName + ", ", string.Empty));

        if (assemblyName.Name is null)
        {
            throw new InvalidOperationException("Could not find a valid name for assembly");
        }

        foreach (var ns in namespaces)
        {
            var targetType = ns + ".Registrations";
            var fullName = targetType + ", " + assemblyName.FullName.Replace(assemblyName.Name, ns);

            var registerTypeClass = Type.GetType(fullName, false);
            if (registerTypeClass is null)
            {
                continue;
            }

            var registerer = (IWantsToRegisterStuff?)Activator.CreateInstance(registerTypeClass);

            if (registerer is null)
            {
                continue;
            }

            registerer.Register(resolver, readonlyDependencyResolver);
        }
    }
}
