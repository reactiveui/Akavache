// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AkavacheTodoMaui.Models;
using AkavacheTodoMaui.Services;
using ReactiveUI;

namespace AkavacheTodoMaui.ViewModels;

/// <summary>
/// View model for individual todo items with reactive behaviors.
/// </summary>
public class TodoItemViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly TodoCacheService _cacheService;
    private readonly NotificationService _notificationService;
    private readonly ObservableAsPropertyHelper<string> _dueDateDisplay;
    private readonly ObservableAsPropertyHelper<string> _priorityDisplay;
    private readonly ObservableAsPropertyHelper<bool> _isOverdue;
    private readonly ObservableAsPropertyHelper<bool> _isDueSoon;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoItemViewModel"/> class.
    /// </summary>
    /// <param name="todoItem">The todo item model.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="notificationService">The notification service.</param>
    public TodoItemViewModel(TodoItem todoItem, TodoCacheService cacheService, NotificationService notificationService)
    {
        TodoItem = todoItem;
        _cacheService = cacheService;
        _notificationService = notificationService;

        // Create commands
        ToggleCompletedCommand = ReactiveCommand.CreateFromObservable(ExecuteToggleCompleted);
        DeleteCommand = ReactiveCommand.CreateFromObservable(ExecuteDelete);
        EditCommand = ReactiveCommand.Create(ExecuteEdit);
        ScheduleReminderCommand = ReactiveCommand.CreateFromObservable(ExecuteScheduleReminder);

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
    /// Gets the command to schedule a reminder.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ScheduleReminderCommand { get; }

    /// <summary>
    /// Gets the background color based on todo status.
    /// </summary>
    public string BackgroundColor
    {
        get
        {
            if (TodoItem.IsCompleted)
            {
                return "#F0F0F0";
            }

            if (IsOverdue)
            {
                return "#FFEBEE";
            }

            if (IsDueSoon)
            {
                return "#FFF3E0";
            }

            return "Transparent";
        }
    }

    /// <summary>
    /// Gets the text color based on todo status.
    /// </summary>
    public string TextColor
    {
        get
        {
            if (TodoItem.IsCompleted)
            {
                return "#666666";
            }

            if (IsOverdue)
            {
                return "#D32F2F";
            }

            if (IsDueSoon)
            {
                return "#F57C00";
            }

            return "#212121";
        }
    }

    /// <summary>
    /// Gets the priority color indicator.
    /// </summary>
    public string PriorityColor => TodoItem.Priority switch
    {
        TodoPriority.Critical => "#D32F2F",
        TodoPriority.High => "#F57C00",
        TodoPriority.Medium => "#388E3C",
        TodoPriority.Low => "#1976D2",
        _ => "#666666"
    };

    /// <summary>
    /// Gets formatted tags as a single string.
    /// </summary>
    public string TagsDisplay => TodoItem.Tags.Count > 0 ? string.Join(", ", TodoItem.Tags) : "No tags";

    private IObservable<Unit> ExecuteToggleCompleted()
    {
        return Observable.FromAsync(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
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
        // In a real app, this would navigate to an edit page or open a modal
        // For this demo, we'll just demonstrate the pattern
        return Unit.Default;
    }

    private IObservable<Unit> ExecuteScheduleReminder()
    {
        if (!TodoItem.DueDate.HasValue)
        {
            return Observable.Return(Unit.Default);
        }

        return _notificationService.ScheduleReminder(TodoItem);
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
}
