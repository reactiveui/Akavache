// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AkavacheTodoWpf.Services;

/// <summary>
/// Represents information about cache usage.
/// </summary>
public partial class CacheInfo : ReactiveObject
{
    /// <summary>
    /// Gets or sets the number of keys in UserAccount cache.
    /// </summary>
    [Reactive]
    public partial int UserAccountKeys { get; set; }

    /// <summary>
    /// Gets or sets the number of keys in LocalMachine cache.
    /// </summary>
    [Reactive]
    public partial int LocalMachineKeys { get; set; }

    /// <summary>
    /// Gets or sets the number of keys in Secure cache.
    /// </summary>
    [Reactive]
    public partial int SecureKeys { get; set; }

    /// <summary>
    /// Gets or sets the total number of keys across all caches.
    /// </summary>
    [Reactive]
    public partial int TotalKeys { get; set; }

    /// <summary>
    /// Gets or sets when this information was last checked.
    /// </summary>
    [Reactive]
    public partial DateTimeOffset LastChecked { get; set; } = DateTimeOffset.Now;
}
