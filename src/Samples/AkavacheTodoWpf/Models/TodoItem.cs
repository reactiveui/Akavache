// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using ReactiveUI;

namespace AkavacheTodoWpf.Models;

/// <summary>
/// Represents a Todo item with all necessary properties for demonstration.
/// </summary>
public class TodoItem : ReactiveObject
{
    private bool _isCompleted;
    private DateTimeOffset? _dueDate;
    private TodoPriority _priority = TodoPriority.Medium;
    private string _title = string.Empty;
    private string _description = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for the todo item.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the title of the todo item.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// Gets or sets the description of the todo item.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the todo item is completed.
    /// </summary>
    [JsonPropertyName("isCompleted")]
    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            this.RaiseAndSetIfChanged(ref _isCompleted, value);
            this.RaisePropertyChanged(nameof(IsOverdue));
            this.RaisePropertyChanged(nameof(IsDueSoon));
        }
    }

    /// <summary>
    /// Gets or sets the creation date of the todo item.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the due date of the todo item. Used for expiration demonstration.
    /// </summary>
    [JsonPropertyName("dueDate")]
    public DateTimeOffset? DueDate
    {
        get => _dueDate;
        set
        {
            this.RaiseAndSetIfChanged(ref _dueDate, value);
            this.RaisePropertyChanged(nameof(IsOverdue));
            this.RaisePropertyChanged(nameof(IsDueSoon));
        }
    }

    /// <summary>
    /// Gets or sets the priority level of the todo item.
    /// </summary>
    [JsonPropertyName("priority")]
    public TodoPriority Priority
    {
        get => _priority;
        set => this.RaiseAndSetIfChanged(ref _priority, value);
    }

    /// <summary>
    /// Gets or sets any tags associated with the todo item.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether the todo item is overdue.
    /// </summary>
    [JsonIgnore]
    public bool IsOverdue => DueDate < DateTimeOffset.Now && !IsCompleted;

    /// <summary>
    /// Gets a value indicating whether the todo item is due soon (within 24 hours).
    /// </summary>
    [JsonIgnore]
    public bool IsDueSoon => DueDate > DateTimeOffset.Now &&
                             DueDate.Value <= DateTimeOffset.Now.AddHours(24) &&
                             !IsCompleted;
}
