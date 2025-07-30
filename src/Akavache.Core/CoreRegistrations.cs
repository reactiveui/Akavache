// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// Registrations needed for running the application.
/// </summary>
public static class CoreRegistrations
{
    private static IScheduler? _taskPoolOverride;

    /// <summary>
    /// Gets or sets the serializer.
    /// </summary>
    public static ISerializer? Serializer { get; set; }

    /// <summary>
    /// Gets or sets the http service.
    /// </summary>
    public static IHttpService? HttpService { get; set; } = new HttpService();

    /// <summary>
    /// Gets or sets the Scheduler used for task pools.
    /// </summary>
    public static IScheduler TaskpoolScheduler
    {
        get => _taskPoolOverride ?? TaskPoolScheduler.Default;
        set => _taskPoolOverride = value;
    }
}
