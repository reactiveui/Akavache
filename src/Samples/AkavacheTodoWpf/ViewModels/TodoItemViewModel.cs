// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Media;
using AkavacheTodoWpf.Models;
using AkavacheTodoWpf.Services;
using ReactiveUI;

namespace AkavacheTodoWpf.ViewModels;

/// <summary>
/// View model for individual todo items with reactive behaviors for WPF.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public class TodoItemViewModel : ReactiveObject, IActivatableViewModel
{
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
    /// <param name="notificationService">The notification service.</param>
    /// <param name="deleteAction">Action to call when deleting this item.</param>
    public TodoItemViewModel(TodoItem todoItem, NotificationService notificationService, Action<TodoItemViewModel>? deleteAction = null)
    {
        TodoItem = todoItem;
        _notificationService = notificationService;
        DeleteAction = deleteAction;

        // Create commands
        ToggleCompletedCommand = ReactiveCommand.CreateFromObservable(ExecuteToggleCompleted);
        DeleteCommand = ReactiveCommand.CreateFromObservable(ExecuteDelete);
        EditCommand = ReactiveCommand.CreateFromObservable(ExecuteEdit);
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

            // Auto-save when properties change
            this.WhenAnyValue(x => x.TodoItem.IsCompleted)
                .Skip(1) // Skip initial value
                .Throttle(TimeSpan.FromMilliseconds(500))
                .SelectMany(_ => SaveTodoItem())
                .Subscribe(
                    _ => { },
                    ex => System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex}"))
                .DisposeWith(disposables));
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
    /// Gets or sets the delete action to call when deleting this item.
    /// </summary>
    public Action<TodoItemViewModel>? DeleteAction { get; set; }

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
    public string TagsDisplay => TodoItem.Tags.Count > 0 ? string.Join(", ", TodoItem.Tags) : string.Empty;

    /// <summary>
    /// Gets the CSS class for the priority badge.
    /// </summary>
    public string PriorityBadgeClass => GetPriorityBadgeClass();

    /// <summary>
    /// Gets the CSS class for the completion icon.
    /// </summary>
    public string CompletionIconClass => TodoItem.IsCompleted ? "bi bi-check-circle-fill text-success" : "bi bi-circle text-muted";

    private IObservable<Unit> ExecuteToggleCompleted() =>
        Observable.FromAsync(async () => await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TodoItem.IsCompleted = !TodoItem.IsCompleted;

                // Trigger property notifications for UI updates
                this.RaisePropertyChanged(nameof(TodoItem));
                this.RaisePropertyChanged(nameof(TodoItem.IsCompleted));
                this.RaisePropertyChanged(nameof(IsOverdue));
                this.RaisePropertyChanged(nameof(IsDueSoon));
                this.RaisePropertyChanged(nameof(BackgroundBrush));
                this.RaisePropertyChanged(nameof(ForegroundBrush));
            }))
        .SelectMany(_ => SaveTodoItem())
        .SelectMany(_ =>

            // Invalidate cache for this specific todo to force refresh
            TodoCacheService.InvalidateTodo(TodoItem.Id));

    private IObservable<Unit> ExecuteDelete() =>
        Observable.FromAsync(async () =>
        {
            // Remove from parent collection first
            await Application.Current.Dispatcher.InvokeAsync(() => DeleteAction?.Invoke(this));

            // Then invalidate cache
            await TodoCacheService.InvalidateTodo(TodoItem.Id);
        });

    private IObservable<Unit> ExecuteEdit() =>
        Observable.FromAsync(async () => await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new Views.EditTodoDialog(TodoItem);
            if (dialog.ShowDialog() == true)
            {
                var updatedTodo = dialog.UpdatedTodo;
                if (updatedTodo != null)
                {
                    TodoItem.Title = updatedTodo.Title;
                    TodoItem.Description = updatedTodo.Description;
                    TodoItem.DueDate = updatedTodo.DueDate;
                    TodoItem.Priority = updatedTodo.Priority;

                    // Trigger property notifications for ALL relevant properties
                    this.RaisePropertyChanged(nameof(TodoItem));
                    this.RaisePropertyChanged(nameof(TodoItem.Title));
                    this.RaisePropertyChanged(nameof(TodoItem.Description));
                    this.RaisePropertyChanged(nameof(TodoItem.DueDate));
                    this.RaisePropertyChanged(nameof(TodoItem.Priority));
                    this.RaisePropertyChanged(nameof(TodoItem.IsCompleted));

                    SaveTodoItem().Subscribe();
                }
            }
        }));

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
                todos ??= [];

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

    private string GetRelativeTimeDisplay()
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

    private string GetPriorityBadgeClass() => TodoItem.Priority switch
    {
        TodoPriority.Critical => "badge bg-danger",
        TodoPriority.High => "badge bg-warning",
        TodoPriority.Medium => "badge bg-success",
        TodoPriority.Low => "badge bg-info",
        _ => "badge bg-secondary"
    };
}
