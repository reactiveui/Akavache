// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace AkavacheTodoMaui.Models;

/// <summary>
/// Represents a Todo item with all necessary properties for demonstration.
/// </summary>
[RequiresUnreferencedCode("ReactiveObject requires types to be preserved for reflection.")]
[RequiresDynamicCode("ReactiveObject requires types to be preserved for reflection.")]
public class TodoItem : ReactiveObject
{
    /// <summary>Backing field for <see cref="IsCompleted"/>.</summary>
    private bool _isCompleted;

    /// <summary>Backing field for <see cref="DueDate"/>.</summary>
    private DateTimeOffset? _dueDate;

    /// <summary>Backing field for <see cref="Title"/>.</summary>
    private string _title = string.Empty;

    /// <summary>Backing field for <see cref="Description"/>.</summary>
    private string _description = string.Empty;

    /// <summary>Backing field for <see cref="Priority"/>.</summary>
    private TodoPriority _priority = TodoPriority.Medium;

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

    /// <summary>
    /// Raises property changed for time-dependent properties.
    /// </summary>
    public void RefreshTimeBasedProperties()
    {
        this.RaisePropertyChanged(nameof(IsOverdue));
        this.RaisePropertyChanged(nameof(IsDueSoon));
    }
}
