// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using AkavacheTodoMaui.Models;
using ReactiveUI;

namespace AkavacheTodoMaui.ViewModels;

/// <summary>
/// View model for editing existing todo items.
/// </summary>
public class EditTodoViewModel : ReactiveObject
{
    private readonly TodoItem _originalTodo;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _tagsString = string.Empty;
    private DateTime _dueDate = DateTime.Today;
    private string _dueTime = string.Empty;
    private TodoPriority _priority = TodoPriority.Medium;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditTodoViewModel"/> class.
    /// </summary>
    /// <param name="todoItem">The todo item to edit.</param>
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
        CancelCommand = ReactiveCommand.Create(ExecuteCancel);

        // Initialize priority options
        PriorityOptions = Enum.GetValues<TodoPriority>();
    }

    /// <summary>
    /// Gets or sets the todo title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// Gets or sets the todo description.
    /// </summary>
    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    /// <summary>
    /// Gets or sets the tags as a comma-separated string.
    /// </summary>
    public string TagsString
    {
        get => _tagsString;
        set => this.RaiseAndSetIfChanged(ref _tagsString, value);
    }

    /// <summary>
    /// Gets or sets the due date.
    /// </summary>
    public DateTime DueDate
    {
        get => _dueDate;
        set => this.RaiseAndSetIfChanged(ref _dueDate, value);
    }

    /// <summary>
    /// Gets or sets the due time.
    /// </summary>
    public string DueTime
    {
        get => _dueTime;
        set => this.RaiseAndSetIfChanged(ref _dueTime, value);
    }

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    public TodoPriority Priority
    {
        get => _priority;
        set => this.RaiseAndSetIfChanged(ref _priority, value);
    }

    /// <summary>
    /// Gets the priority options.
    /// </summary>
    public TodoPriority[] PriorityOptions { get; }

    /// <summary>
    /// Gets the save command.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>
    /// Gets the cancel command.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>
    /// Gets a value indicating whether the todo was saved.
    /// </summary>
    public bool WasSaved { get; private set; }

    /// <summary>
    /// Gets the updated todo item if saved.
    /// </summary>
    public TodoItem? UpdatedTodo { get; private set; }

    private async Task ExecuteSave()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            await Application.Current!.MainPage!.DisplayAlert("Validation Error", "Title is required.", "OK");
            return;
        }

        try
        {
            // Parse due date and time
            DateTimeOffset? dueDate = null;
            var date = DueDate.Date;

            if (!string.IsNullOrWhiteSpace(DueTime) && TimeSpan.TryParse(DueTime, out var time))
            {
                date = date.Add(time);
            }

            dueDate = new DateTimeOffset(date);

            // Parse tags
            var tags = new List<string>();
            if (!string.IsNullOrWhiteSpace(TagsString))
            {
                tags = TagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(tag => tag.Trim())
                                .Where(tag => !string.IsNullOrEmpty(tag))
                                .ToList();
            }

            // Create updated todo
            UpdatedTodo = new TodoItem
            {
                Id = _originalTodo.Id,
                Title = Title,
                Description = Description,
                DueDate = dueDate,
                Priority = Priority,
                CreatedAt = _originalTodo.CreatedAt,
                IsCompleted = _originalTodo.IsCompleted,
                Tags = tags
            };

            WasSaved = true;
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", $"Failed to save todo: {ex.Message}", "OK");
        }
    }

    private async void ExecuteCancel()
    {
        WasSaved = false;
        await Shell.Current.GoToAsync("..");
    }
}
