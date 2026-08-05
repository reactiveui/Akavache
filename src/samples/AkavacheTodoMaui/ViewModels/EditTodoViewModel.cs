// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using AkavacheTodoMaui.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AkavacheTodoMaui.ViewModels;

/// <summary>View model for editing existing todo items.</summary>
[RequiresUnreferencedCode("ReactiveObject requires types to be preserved for reflection.")]
[RequiresDynamicCode("ReactiveObject requires types to be preserved for reflection.")]
public partial class EditTodoViewModel : ReactiveObject
{
    /// <summary>The original todo item being edited.</summary>
    private readonly TodoItem _originalTodo;

    /// <summary>Backing field for the reactive Title property.</summary>
    [Reactive]
    private string _title;

    /// <summary>Backing field for the reactive Description property.</summary>
    [Reactive]
    private string _description;

    /// <summary>Backing field for the reactive TagsString property.</summary>
    [Reactive]
    private string _tagsString;

    /// <summary>Backing field for the reactive DueDate property.</summary>
    [Reactive]
    private DateTime _dueDate = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime).ToDateTime(TimeOnly.MinValue);

    /// <summary>Backing field for the reactive DueTime property.</summary>
    [Reactive]
    private string _dueTime = string.Empty;

    /// <summary>Backing field for the reactive Priority property.</summary>
    [Reactive]
    private TodoPriority _priority;

    /// <summary>Initializes a new instance of the <see cref="EditTodoViewModel"/> class.</summary>
    /// <param name="todoItem">The todo item to edit.</param>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public EditTodoViewModel(TodoItem todoItem)
    {
        _originalTodo = todoItem ?? throw new ArgumentNullException(nameof(todoItem));

        // Initialize with current values
        _title = todoItem.Title;
        _description = todoItem.Description;
        _tagsString = string.Join(", ", todoItem.Tags);
        _priority = todoItem.Priority;

        if (todoItem.DueDate.HasValue)
        {
            _dueDate = todoItem.DueDate.Value.DateTime;
            _dueTime = todoItem.DueDate.Value.ToString("HH:mm");
        }

        // Initialize commands
        SaveCommand = ReactiveCommand.CreateFromTask(ExecuteSave);
        CancelCommand = ReactiveCommand.CreateFromTask(ExecuteCancel);

        // Initialize priority options
        PriorityOptions = Enum.GetValues<TodoPriority>();
    }

    /// <summary>Gets the priority options.</summary>
    public TodoPriority[] PriorityOptions { get; }

    /// <summary>Gets the save command.</summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>Gets the cancel command.</summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>Gets a value indicating whether the todo was saved.</summary>
    public bool WasSaved { get; private set; }

    /// <summary>Gets the updated todo item if saved.</summary>
    public TodoItem? UpdatedTodo { get; private set; }

    /// <summary>Splits a comma-separated tag list into trimmed, non-empty tags.</summary>
    /// <param name="tagsString">The raw comma-separated text typed by the user.</param>
    /// <returns>The parsed tags, which is empty when nothing usable was entered.</returns>
    private static List<string> ParseTags(string tagsString)
    {
        List<string> tags = [];

        if (string.IsNullOrWhiteSpace(tagsString))
        {
            return tags;
        }

        foreach (var rawTag in tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var tag = rawTag.Trim();
            if (tag.Length > 0)
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    /// <summary>Validates input, constructs the updated todo and navigates back.</summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    private async Task ExecuteSave()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Validation Error", "Title is required.", "OK");
            return;
        }

        try
        {
            // Parse due date and time
            var dueDay = DateOnly.FromDateTime(DueDate);
            var dueTimeOfDay = !string.IsNullOrWhiteSpace(DueTime) && TimeOnly.TryParse(DueTime, out var time)
                ? time
                : TimeOnly.MinValue;

            DateTimeOffset? dueDate = new DateTimeOffset(dueDay.ToDateTime(dueTimeOfDay));

            // Create updated todo. The edited values are copied into locals first so the object
            // initializer never reads like it is assigning a property to itself.
            var editedTitle = Title;
            var editedDescription = Description;
            var editedPriority = Priority;

            UpdatedTodo = new()
            {
                Id = _originalTodo.Id,
                Title = editedTitle,
                Description = editedDescription,
                DueDate = dueDate,
                Priority = editedPriority,
                CreatedAt = _originalTodo.CreatedAt,
                IsCompleted = _originalTodo.IsCompleted,
                Tags = ParseTags(TagsString)
            };

            WasSaved = true;
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Error", $"Failed to save todo: {ex.Message}", "OK");
        }
    }

    /// <summary>Cancels editing and navigates back without saving.</summary>
    /// <returns>A task to monitor.</returns>
    private async Task ExecuteCancel()
    {
        WasSaved = false;
        await Shell.Current.GoToAsync("..");
    }
}
