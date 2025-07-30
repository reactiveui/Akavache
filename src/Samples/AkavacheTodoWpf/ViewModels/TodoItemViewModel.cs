// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using AkavacheTodoWpf.Models;
using AkavacheTodoWpf.Services;
using ReactiveUI;

namespace AkavacheTodoWpf.ViewModels;

/// <summary>
/// View model for individual todo items with reactive behaviors for WPF.
/// </summary>
public class TodoItemViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly TodoCacheService _cacheService;
    private readonly NotificationService _notificationService;
    private readonly ObservableAsPropertyHelper<string> _dueDateDisplay;
    private readonly ObservableAsPropertyHelper<string> _priorityDisplay;
    private readonly ObservableAsPropertyHelper<bool> _isOverdue;
    private readonly ObservableAsPropertyHelper<bool> _isDueSoon;
    private readonly ObservableAsPropertyHelper<Brush> _backgroundBrush;
    private readonly ObservableAsPropertyHelper<Brush> _foregroundBrush;

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

        // Setup WPF-specific brush properties
        _backgroundBrush = this.WhenAnyValue(
                x => x.TodoItem.IsCompleted,
                x => x.IsOverdue,
                x => x.IsDueSoon)
            .Select(values => GetBackgroundBrush())
            .ToProperty(this, x => x.BackgroundBrush);

        _foregroundBrush = this.WhenAnyValue(
                x => x.TodoItem.IsCompleted,
                x => x.IsOverdue,
                x => x.IsDueSoon)
            .Select(values => GetForegroundBrush())
            .ToProperty(this, x => x.ForegroundBrush);

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
    /// Gets the background brush based on todo status.
    /// </summary>
    public Brush BackgroundBrush => _backgroundBrush.Value;

    /// <summary>
    /// Gets the foreground brush based on todo status.
    /// </summary>
    public Brush ForegroundBrush => _foregroundBrush.Value;

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
    /// Gets the priority color indicator brush.
    /// </summary>
    public Brush PriorityBrush => TodoItem.Priority switch
    {
        TodoPriority.Critical => Brushes.Red,
        TodoPriority.High => Brushes.Orange,
        TodoPriority.Medium => Brushes.Green,
        TodoPriority.Low => Brushes.Blue,
        _ => Brushes.Gray
    };

    /// <summary>
    /// Gets formatted tags as a single string.
    /// </summary>
    public string TagsDisplay => TodoItem.Tags.Count > 0 ? string.Join(", ", TodoItem.Tags) : "No tags";

    private IObservable<Unit> ExecuteToggleCompleted()
    {
        return Observable.FromAsync(async () =>
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TodoItem.IsCompleted = !TodoItem.IsCompleted;
            });
        })
        .SelectMany(_ => SaveTodoItem())
        .SelectMany(_ =>
        {
            // Invalidate cache for this specific todo to force refresh
            return TodoCacheService.InvalidateTodo(TodoItem.Id);
        });
    }

    private IObservable<Unit> ExecuteDelete()
    {
        return TodoCacheService.InvalidateTodo(TodoItem.Id)
            .SelectMany(_ =>
            {
                // In a real app, you might want to remove from parent collection
                // This is typically handled by the parent view model
                return Observable.Return(Unit.Default);
            });
    }

    private Unit ExecuteEdit()
    {
        // In a real app, this would open an edit dialog or navigate to an edit view
        // For this demo, we'll show a simple input dialog using a basic approach
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new EditTodoDialog(TodoItem.Title);
            if (dialog.ShowDialog() == true)
            {
                var newTitle = dialog.TodoTitle;
                if (!string.IsNullOrWhiteSpace(newTitle) && newTitle != TodoItem.Title)
                {
                    TodoItem.Title = newTitle;
                    SaveTodoItem().Subscribe();
                }
            }
        });

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
        return TodoCacheService.GetAllTodos()
            .Take(1)
            .SelectMany(todos =>
            {
                if (todos == null)
                {
                    todos = [];
                }

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
                return TodoCacheService.SaveTodos(todos);
            });
    }

    private Brush GetBackgroundBrush()
    {
        if (TodoItem.IsCompleted)
        {
            return new SolidColorBrush(Color.FromRgb(240, 240, 240));
        }

        if (IsOverdue)
        {
            return new SolidColorBrush(Color.FromRgb(255, 235, 238));
        }

        if (IsDueSoon)
        {
            return new SolidColorBrush(Color.FromRgb(255, 243, 224));
        }

        return Brushes.Transparent;
    }

    private Brush GetForegroundBrush()
    {
        if (TodoItem.IsCompleted)
        {
            return new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        if (IsOverdue)
        {
            return new SolidColorBrush(Color.FromRgb(211, 47, 47));
        }

        if (IsDueSoon)
        {
            return new SolidColorBrush(Color.FromRgb(245, 124, 0));
        }

        return new SolidColorBrush(Color.FromRgb(33, 33, 33));
    }
}
