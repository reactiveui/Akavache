// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace AkavacheTodoWpf.Models;

/// <summary>
/// Represents todo sort orders.
/// </summary>
public enum TodoSortOrder
{
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
