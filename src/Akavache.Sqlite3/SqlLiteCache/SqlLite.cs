﻿// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Sqlite3;

/// <summary>
/// Main class for registering items with the system.
/// </summary>
public static class SqlLite
{
    /// <summary>
    /// Starts the process and bundle registration.
    /// </summary>
    /// <param name="bundleRegistration">Performs the registration via the mention.</param>
    public static void Start(Action bundleRegistration)
    {
        _ = bundleRegistration ?? throw new ArgumentNullException(nameof(bundleRegistration));
        bundleRegistration();
    }
}