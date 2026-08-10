// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace AkavacheV11Reader;

/// <summary>The complex value the V10 writer stored, read back here to check object deserialization.</summary>
[System.Diagnostics.DebuggerDisplay("{Name}")]
public class Person
{
    /// <summary>Gets or sets the person's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the person's age.</summary>
    public int Age { get; set; }

    /// <summary>Gets or sets the person's email address.</summary>
    public string Email { get; set; } = string.Empty;
}
