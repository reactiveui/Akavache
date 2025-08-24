// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AkavacheTodoWpf.Services;

/// <summary>
/// Represents todo statistics for dashboard display.
/// </summary>
public partial class TodoStats : ReactiveObject
{
    /// <summary>
    /// Gets or sets the total number of todos.
    /// </summary>
    [Reactive]
    public partial int TotalTodos { get; set; }

    /// <summary>
    /// Gets or sets the number of completed todos.
    /// </summary>
    [Reactive]
    public partial int CompletedTodos { get; set; }

    /// <summary>
    /// Gets or sets the number of overdue todos.
    /// </summary>
    [Reactive]
    public partial int OverdueTodos { get; set; }

    /// <summary>
    /// Gets or sets the number of todos due soon.
    /// </summary>
    [Reactive]
    public partial int DueSoonTodos { get; set; }

    /// <summary>
    /// Gets or sets the number of high priority todos.
    /// </summary>
    [Reactive]
    public partial int HighPriorityTodos { get; set; }

    /// <summary>
    /// Gets or sets when these statistics were calculated.
    /// </summary>
    [Reactive]
    public partial DateTimeOffset LastCalculated { get; set; } = DateTimeOffset.Now;
}
