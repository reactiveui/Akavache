// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;
using System.Reactive;
using ReactiveUI;
using System.Reactive.Concurrency;
using Akavache.Core;

namespace Akavache.Mobile
{
    /// <summary>
    /// Registrations for the mobile platform. Will register all our instances with splat.
    /// </summary>
    [Preserve(AllMembers = true)]
    public class Registrations : IWantsToRegisterStuff
    {
        /// <inheritdoc />
        public void Register(IMutableDependencyResolver resolver)
        {
            resolver.Register(() => new JsonSerializerSettings() 
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            }, typeof(JsonSerializerSettings), null);

            var akavacheDriver = new AkavacheDriver();
            resolver.Register(() => akavacheDriver, typeof(ISuspensionDriver), null);

            // NB: These correspond to the hacks in Akavache.Http's registrations
            resolver.Register(() => resolver.GetService<ISuspensionHost>().ShouldPersistState,
                typeof(IObservable<IDisposable>), "ShouldPersistState");

            resolver.Register(() => resolver.GetService<ISuspensionHost>().IsUnpausing,
                typeof(IObservable<Unit>), "IsUnpausing");

            resolver.Register(() => RxApp.TaskpoolScheduler, typeof(IScheduler), "Taskpool");
        }
    }
}
