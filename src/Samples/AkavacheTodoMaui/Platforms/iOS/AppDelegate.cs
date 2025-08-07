// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Foundation;

namespace AkavacheTodoMaui;

/// <summary>
/// iOS App Delegate.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <summary>
    /// Creates the MAUI app.
    /// </summary>
    /// <returns>The configured MAUI app.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
