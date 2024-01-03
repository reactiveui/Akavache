// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Newtonsoft.Json;
using Splat;

namespace Akavache.NewtonsoftJson;

/// <summary>
/// Registrations for the mobile platform. Will register all our instances with splat.
/// </summary>
[Preserve(AllMembers = true)]
public class Registrations : IWantsToRegisterStuff
{
    /// <inheritdoc />
    public void Register(IMutableDependencyResolver resolver, IReadonlyDependencyResolver readonlyDependencyResolver)
    {
        resolver.ThrowArgumentNullExceptionIfNull(nameof(resolver));

        resolver?.Register(
            () => new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            },
            typeof(JsonSerializerSettings),
            null);

        resolver?.Register(() => new NewtonsoftSerializer(readonlyDependencyResolver.GetService<JsonSerializerSettings>()!), typeof(ISerializer), null);

        resolver?.Register(() => new JsonDateTimeContractResolver(), typeof(IDateTimeContractResolver), null);
    }
}
