// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace AkavacheTodoMaui.Models;

/// <summary>
/// Represents todo sort orders.
/// </summary>
public enum TodoSortOrder
{
    /// <summary>
    /// There is no sort order.
    /// </summary>
    None = 0,

    /// <summary>
    /// Sort by creation date.
    /// </summary>
    CreatedDate = 1,

    /// <summary>
    /// Sort by due date.
    /// </summary>
    DueDate = 2,

    /// <summary>
    /// Sort by priority.
    /// </summary>
    Priority = 3,

    /// <summary>
    /// Sort alphabetically by title.
    /// </summary>
    Title = 4
}
