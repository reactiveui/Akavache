// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Splat;
using Akavache.Sqlite3;
using Akavache.Core;

namespace Akavache
{
    /// <summary>
    /// Setup registrations for the application.
    /// </summary>
    [Preserve(AllMembers = true)]
    public class Registrations : IWantsToRegisterStuff
    {
        /// <inheritdoc />
        public void Register(IMutableDependencyResolver resolverToUse)
        {
            SQLitePCL.Batteries_V2.Init();
        }

        /// <summary>
        /// Registers the application name. This will create storage location for our storage.
        /// </summary>
        /// <param name="applicationName">The name of the application that is running.</param>
        public static void Start(string applicationName)
        {
            Akavache.Sqlite3.Registrations.Start(applicationName, () => SQLitePCL.Batteries_V2.Init());
        }
    }
}
