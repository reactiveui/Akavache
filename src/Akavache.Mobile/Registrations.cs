// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;

using Newtonsoft.Json;

using ReactiveUI;

using Splat;

namespace Akavache.Mobile;

/// <summary>
/// Registrations for the mobile platform. Will register all our instances with splat.
/// </summary>
[Preserve(AllMembers = true)]
public class Registrations : IWantsToRegisterStuff
{
    /// <inheritdoc />
    public void Register(IMutableDependencyResolver resolver, IReadonlyDependencyResolver readonlyDependencyResolver)
    {
        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        resolver.Register(
            () => new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            },
            typeof(JsonSerializerSettings),
            null);

        var akavacheDriver = new AkavacheDriver();
        resolver.Register(() => akavacheDriver, typeof(ISuspensionDriver), null);

        // NB: These correspond to the hacks in Akavache.Http's registrations
        resolver.Register(
            () => readonlyDependencyResolver.GetService<ISuspensionHost>()?.ShouldPersistState ?? throw new InvalidOperationException("Unable to resolve ISuspensionHost, probably ReactiveUI is not initialized."),
            typeof(IObservable<IDisposable>),
            "ShouldPersistState");

        resolver.Register(
            () => readonlyDependencyResolver.GetService<ISuspensionHost>()?.IsUnpausing ?? throw new InvalidOperationException("Unable to resolve ISuspensionHost, probably ReactiveUI is not initialized."),
            typeof(IObservable<Unit>),
            "IsUnpausing");

        resolver.Register(() => RxApp.TaskpoolScheduler, typeof(IScheduler), "Taskpool");
    }
}