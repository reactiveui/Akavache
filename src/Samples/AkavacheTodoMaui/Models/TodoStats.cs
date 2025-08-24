// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace AkavacheTodoMaui.Models;

/// <summary>
/// Represents todo statistics for dashboard display.
/// </summary>
public class TodoStats
{
    /// <summary>
    /// Gets or sets the total number of todos.
    /// </summary>
    public int TotalTodos { get; set; }

    /// <summary>
    /// Gets or sets the number of completed todos.
    /// </summary>
    public int CompletedTodos { get; set; }

    /// <summary>
    /// Gets or sets the number of overdue todos.
    /// </summary>
    public int OverdueTodos { get; set; }

    /// <summary>
    /// Gets or sets the number of todos due soon.
    /// </summary>
    public int DueSoonTodos { get; set; }

    /// <summary>
    /// Gets or sets the number of high priority todos.
    /// </summary>
    public int HighPriorityTodos { get; set; }

    /// <summary>
    /// Gets or sets when these statistics were calculated.
    /// </summary>
    public DateTimeOffset LastCalculated { get; set; }
}
