// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace AkavacheTodoMaui.Models;

/// <summary>Represents information about cache usage.</summary>
[System.Diagnostics.DebuggerDisplay("{UserAccountKeys}")]
public class CacheInfo
{
    /// <summary>Gets or sets the number of keys in UserAccount cache.</summary>
    public int UserAccountKeys { get; set; }

    /// <summary>Gets or sets the number of keys in LocalMachine cache.</summary>
    public int LocalMachineKeys { get; set; }

    /// <summary>Gets or sets the number of keys in Secure cache.</summary>
    public int SecureKeys { get; set; }

    /// <summary>Gets or sets the total number of keys across all caches.</summary>
    public int TotalKeys { get; set; }

    /// <summary>Gets or sets when this information was last checked.</summary>
    public DateTimeOffset LastChecked { get; set; }
}
