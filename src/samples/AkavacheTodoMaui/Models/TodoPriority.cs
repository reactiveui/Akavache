// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace AkavacheTodoMaui.Models;

/// <summary>
/// Represents the priority levels for todo items.
/// </summary>
public enum TodoPriority
{
    /// <summary>
    /// There is no priority.
    /// </summary>
    None = 0,

    /// <summary>
    /// Low priority.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium priority.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High priority.
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical priority.
    /// </summary>
    Critical = 4
}
