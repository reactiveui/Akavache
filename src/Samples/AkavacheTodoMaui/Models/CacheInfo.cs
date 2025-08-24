// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace AkavacheTodoMaui.Models;

/// <summary>
/// Represents information about cache usage.
/// </summary>
public class CacheInfo
{
    /// <summary>
    /// Gets or sets the number of keys in UserAccount cache.
    /// </summary>
    public int UserAccountKeys { get; set; }

    /// <summary>
    /// Gets or sets the number of keys in LocalMachine cache.
    /// </summary>
    public int LocalMachineKeys { get; set; }

    /// <summary>
    /// Gets or sets the number of keys in Secure cache.
    /// </summary>
    public int SecureKeys { get; set; }

    /// <summary>
    /// Gets or sets the total number of keys across all caches.
    /// </summary>
    public int TotalKeys { get; set; }

    /// <summary>
    /// Gets or sets when this information was last checked.
    /// </summary>
    public DateTimeOffset LastChecked { get; set; }
}
