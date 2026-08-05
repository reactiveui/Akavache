// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace AkavacheTodoWpf.Models;

/// <summary>Represents a Todo item with all necessary properties for demonstration.</summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public class TodoItem : ReactiveObject
{
    /// <summary>How far ahead of its due date a todo starts counting as "due soon".</summary>
    private const int DueSoonWindowHours = 24;

    /// <summary>Gets or sets the unique identifier for the todo item.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the title of the todo item.</summary>
    [JsonPropertyName("title")]
    public string Title
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
= string.Empty;

    /// <summary>Gets or sets the description of the todo item.</summary>
    [JsonPropertyName("description")]
    public string Description
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
= string.Empty;

    /// <summary>Gets or sets a value indicating whether the todo item is completed.</summary>
    [JsonPropertyName("isCompleted")]
    public bool IsCompleted
    {
        get => field;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged();
            this.RaisePropertyChanged();
        }
    }

    /// <summary>Gets or sets the creation date of the todo item.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = TimeProvider.System.GetUtcNow();

    /// <summary>Gets or sets the due date of the todo item. Used for expiration demonstration.</summary>
    [JsonPropertyName("dueDate")]
    public DateTimeOffset? DueDate
    {
        get => field;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged();
            this.RaisePropertyChanged();
        }
    }

    /// <summary>Gets or sets the priority level of the todo item.</summary>
    [JsonPropertyName("priority")]
    public TodoPriority Priority
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
= TodoPriority.Medium;

    /// <summary>Gets or sets any tags associated with the todo item.</summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>Gets a value indicating whether the todo item is overdue.</summary>
    [JsonIgnore]
    public bool IsOverdue => DueDate < TimeProvider.System.GetLocalNow() && !IsCompleted;

    /// <summary>Gets a value indicating whether the todo item is due soon (within 24 hours).</summary>
    [JsonIgnore]
    public bool IsDueSoon => DueDate > TimeProvider.System.GetLocalNow()
                             && DueDate.Value <= TimeProvider.System.GetLocalNow().AddHours(DueSoonWindowHours)
                             && !IsCompleted;
}
