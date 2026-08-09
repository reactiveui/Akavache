// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AkavacheTodoWpf.Services;

/// <summary>Represents information about cache usage.</summary>
[System.Diagnostics.DebuggerDisplay("{UserAccountKeys}")]
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
    public partial DateTimeOffset LastChecked { get; set; } = TimeProvider.System.GetLocalNow();
}
