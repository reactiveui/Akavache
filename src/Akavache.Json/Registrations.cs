// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using Akavache.Core;
using Splat;

namespace Akavache.Json;

/// <summary>
/// Registrations for the mobile platform. Will register all our instances with splat.
/// </summary>
[Preserve(AllMembers = true)]
public class Registrations : IWantsToRegisterStuff
{
    /// <inheritdoc />
    public void Register(IMutableDependencyResolver resolver, IReadonlyDependencyResolver readonlyDependencyResolver)
    {
#if NETSTANDARD || XAMARINIOS || XAMARINMAC || XAMARINTVOS || TIZEN || MONOANDROID13_0
        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }
#else
        ArgumentNullException.ThrowIfNull(resolver);
#endif

        resolver.Register(
            () => new JsonSerializerOptions(),
            typeof(JsonSerializerOptions),
            null);

        resolver.Register(() => new SystemJsonSerializer(readonlyDependencyResolver.GetService<JsonSerializerOptions>()!), typeof(ISerializer), null);

        ////resolver.Register(() => new JsonDateTimeContractResolver(), typeof(IDateTimeContractResolver), null);
    }
}
