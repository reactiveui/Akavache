// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AkavacheTodoBlazor.Models;
using AkavacheTodoBlazor.Services;
using ReactiveUI;

namespace AkavacheTodoBlazor.ViewModels;

/// <summary>
/// View model for individual todo items with reactive behaviors for Blazor.
/// </summary>
public class TodoItemViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly TodoCacheService _cacheService;
    private readonly ObservableAsPropertyHelper<string> _dueDateDisplay;
    private readonly ObservableAsPropertyHelper<string> _priorityDisplay;
    private readonly ObservableAsPropertyHelper<bool> _isOverdue;
    private readonly ObservableAsPropertyHelper<bool> _isDueSoon;
    private readonly ObservableAsPropertyHelper<string> _statusCssClass;
    private readonly ObservableAsPropertyHelper<string> _priorityCssClass;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoItemViewModel"/> class.
    /// </summary>
    /// <param name="todoItem">The todo item model.</param>
    /// <param name="cacheService">The cache service.</param>
    public TodoItemViewModel(TodoItem todoItem, TodoCacheService cacheService)
    {
        TodoItem = todoItem;
        _cacheService = cacheService;

        // Create commands
        ToggleCompletedCommand = ReactiveCommand.CreateFromObservable(ExecuteToggleCompleted);
        DeleteCommand = ReactiveCommand.CreateFromObservable(ExecuteDelete);
        EditCommand = ReactiveCommand.Create(ExecuteEdit);

        // Setup computed properties
        _dueDateDisplay = this.WhenAnyValue(x => x.TodoItem.DueDate)
            .Select(dueDate => dueDate?.ToString("MMM dd, yyyy HH:mm") ?? "No due date")
            .ToProperty(this, x => x.DueDateDisplay);

        _priorityDisplay = this.WhenAnyValue(x => x.TodoItem.Priority)
            .Select(priority => priority.ToString())
            .ToProperty(this, x => x.PriorityDisplay);

        _isOverdue = this.WhenAnyValue(x => x.TodoItem.DueDate, x => x.TodoItem.IsCompleted)
            .Select(values => TodoItem.IsOverdue)
            .ToProperty(this, x => x.IsOverdue);

        _isDueSoon = this.WhenAnyValue(x => x.TodoItem.DueDate, x => x.TodoItem.IsCompleted)
            .Select(values => TodoItem.IsDueSoon)
            .ToProperty(this, x => x.IsDueSoon);

        // Setup CSS class properties for Blazor styling
        _statusCssClass = this.WhenAnyValue(
                x => x.TodoItem.IsCompleted,
                x => x.IsOverdue,
                x => x.IsDueSoon)
            .Select(values => GetStatusCssClass())
            .ToProperty(this, x => x.StatusCssClass);

        _priorityCssClass = this.WhenAnyValue(x => x.TodoItem.Priority)
            .Select(priority => GetPriorityCssClass(priority))
            .ToProperty(this, x => x.PriorityCssClass);

        // Setup activator
        Activator = new ViewModelActivator();

        this.WhenActivated(disposables =>
        {
            // Auto-save when properties change
            this.WhenAnyValue(x => x.TodoItem.IsCompleted)
                .Skip(1) // Skip initial value
                .Throttle(TimeSpan.FromMilliseconds(500))
                .SelectMany(_ => SaveTodoItem())
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    /// <summary>
    /// Gets the view model activator.
    /// </summary>
    public ViewModelActivator Activator { get; }

    /// <summary>
    /// Gets the todo item model.
    /// </summary>
    public TodoItem TodoItem { get; }

    /// <summary>
    /// Gets the formatted due date display.
    /// </summary>
    public string DueDateDisplay => _dueDateDisplay.Value;

    /// <summary>
    /// Gets the priority display string.
    /// </summary>
    public string PriorityDisplay => _priorityDisplay.Value;

    /// <summary>
    /// Gets a value indicating whether the todo is overdue.
    /// </summary>
    public bool IsOverdue => _isOverdue.Value;

    /// <summary>
    /// Gets a value indicating whether the todo is due soon.
    /// </summary>
    public bool IsDueSoon => _isDueSoon.Value;

    /// <summary>
    /// Gets the CSS class for status styling.
    /// </summary>
    public string StatusCssClass => _statusCssClass.Value;

    /// <summary>
    /// Gets the CSS class for priority styling.
    /// </summary>
    public string PriorityCssClass => _priorityCssClass.Value;

    /// <summary>
    /// Gets the command to toggle completion status.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleCompletedCommand { get; }

    /// <summary>
    /// Gets the command to delete the todo.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

    /// <summary>
    /// Gets the command to edit the todo.
    /// </summary>
    public ReactiveCommand<Unit, Unit> EditCommand { get; }

    /// <summary>
    /// Gets formatted tags as a single string.
    /// </summary>
    public string TagsDisplay => TodoItem.Tags.Count > 0 ? string.Join(", ", TodoItem.Tags) : "No tags";

    /// <summary>
    /// Gets the Bootstrap badge class for priority.
    /// </summary>
    public string PriorityBadgeClass => TodoItem.Priority switch
    {
        TodoPriority.Critical => "badge bg-danger",
        TodoPriority.High => "badge bg-warning",
        TodoPriority.Medium => "badge bg-success",
        TodoPriority.Low => "badge bg-info",
        _ => "badge bg-secondary"
    };

    /// <summary>
    /// Gets the Bootstrap icon class for completion status.
    /// </summary>
    public string CompletionIconClass => TodoItem.IsCompleted ? "bi bi-check-circle-fill text-success" : "bi bi-circle text-muted";

    /// <summary>
    /// Gets the completion percentage as a string for progress bars.
    /// </summary>
    public string CompletionPercentage => TodoItem.IsCompleted ? "100" : "0";

    /// <summary>
    /// Gets the relative time display for due date.
    /// </summary>
    public string RelativeTimeDisplay
    {
        get
        {
            if (!TodoItem.DueDate.HasValue)
            {
                return "No due date";
            }

            var now = DateTimeOffset.Now;
            var dueDate = TodoItem.DueDate.Value;
            var timeSpan = dueDate - now;

            if (timeSpan.TotalDays > 1)
            {
                return $"Due in {(int)timeSpan.TotalDays} days";
            }

            if (timeSpan.TotalHours > 1)
            {
                return $"Due in {(int)timeSpan.TotalHours} hours";
            }

            if (timeSpan.TotalMinutes > 1)
            {
                return $"Due in {(int)timeSpan.TotalMinutes} minutes";
            }

            if (timeSpan.TotalMinutes > 0)
            {
                return "Due soon";
            }

            var overdue = now - dueDate;
            if (overdue.TotalDays > 1)
            {
                return $"Overdue by {(int)overdue.TotalDays} days";
            }

            if (overdue.TotalHours > 1)
            {
                return $"Overdue by {(int)overdue.TotalHours} hours";
            }

            return "Overdue";
        }
    }

    private static string GetPriorityCssClass(TodoPriority priority) => priority switch
    {
        TodoPriority.Critical => "priority-critical",
        TodoPriority.High => "priority-high",
        TodoPriority.Medium => "priority-medium",
        TodoPriority.Low => "priority-low",
        _ => "priority-normal"
    };

    private IObservable<Unit> ExecuteToggleCompleted()
    {
        return Observable.FromAsync(async () =>
        {
            await Task.Run(() =>
            {
                TodoItem.IsCompleted = !TodoItem.IsCompleted;
            });
        })
        .SelectMany(_ => SaveTodoItem())
        .SelectMany(_ =>
        {
            // Invalidate cache for this specific todo to force refresh
            return _cacheService.InvalidateTodo(TodoItem.Id);
        });
    }

    private IObservable<Unit> ExecuteDelete()
    {
        return _cacheService.InvalidateTodo(TodoItem.Id)
            .SelectMany(_ =>
            {
                // In a real app, you might want to remove from parent collection
                // This is typically handled by the parent view model
                return Observable.Return(Unit.Default);
            });
    }

    private Unit ExecuteEdit()
    {
        // In a Blazor app, this might trigger a modal or navigate to an edit component
        // For this demo, we'll just demonstrate the pattern
        return Unit.Default;
    }

    private IObservable<Unit> SaveTodoItem()
    {
        // Use individual cache key for this todo
        var key = $"todo_{TodoItem.Id}";
        return _cacheService.GetAllTodos()
            .Take(1)
            .SelectMany(todos =>
            {
                // Update the todo in the list
                var existingTodo = todos.FirstOrDefault(t => t.Id == TodoItem.Id);
                if (existingTodo != null)
                {
                    var index = todos.IndexOf(existingTodo);
                    todos[index] = TodoItem;
                }
                else
                {
                    todos.Add(TodoItem);
                }

                // Save the updated list
                return _cacheService.SaveTodos(todos);
            });
    }

    private string GetStatusCssClass()
    {
        if (TodoItem.IsCompleted)
        {
            return "todo-completed";
        }

        if (IsOverdue)
        {
            return "todo-overdue";
        }

        if (IsDueSoon)
        {
            return "todo-due-soon";
        }

        return "todo-normal";
    }
}
