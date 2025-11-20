// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using AkavacheTodoMaui.Models;
using AkavacheTodoMaui.Services;
using ReactiveUI;

namespace AkavacheTodoMaui.ViewModels;

/// <summary>
/// View model for individual todo items with reactive behaviors.
/// </summary>
[RequiresUnreferencedCode("ReactiveObject requires types to be preserved for reflection.")]
[RequiresDynamicCode("ReactiveObject requires types to be preserved for reflection.")]
public partial class TodoItemViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly NotificationService _notificationService;
    private readonly ObservableAsPropertyHelper<string> _dueDateDisplay;
    private readonly ObservableAsPropertyHelper<string> _priorityDisplay;
    private readonly ObservableAsPropertyHelper<bool> _isOverdue;
    private readonly ObservableAsPropertyHelper<bool> _isDueSoon;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoItemViewModel"/> class.
    /// </summary>
    /// <param name="todoItem">The todo item model.</param>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="deleteAction">Action to call when deleting this item.</param>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public TodoItemViewModel(TodoItem todoItem, NotificationService notificationService, Action<TodoItemViewModel>? deleteAction = null)
    {
        TodoItem = todoItem;
        _notificationService = notificationService;
        DeleteAction = deleteAction;

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
                .Subscribe(
                    _ => { },
                    ex => System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex}"))
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
                return "#E8F5E8"; // Light green background for completed
            }

            if (IsOverdue)
            {
                return "#FFEBEE"; // Light red background for overdue
            }

            if (IsDueSoon)
            {
                return "#FFF3E0"; // Light orange background for due soon
            }

            return "White"; // Clean white background for normal todos
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
                return "#2E7D32"; // Dark green text for completed
            }

            if (IsOverdue)
            {
                return "#C62828"; // Dark red text for overdue
            }

            if (IsDueSoon)
            {
                return "#E65100"; // Dark orange text for due soon
            }

            return "#212121"; // Dark text for normal todos
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
    public string TagsDisplay => TodoItem.Tags.Count > 0 ? string.Join(", ", TodoItem.Tags) : string.Empty;

    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<Unit> ExecuteToggleCompleted() =>
        Observable.FromAsync(async () => await MainThread.InvokeOnMainThreadAsync(() =>
            {
                TodoItem.IsCompleted = !TodoItem.IsCompleted;

                // Trigger property notifications for UI updates
                this.RaisePropertyChanged(nameof(TodoItem));
                this.RaisePropertyChanged(nameof(TodoItem.IsCompleted));
                this.RaisePropertyChanged(nameof(IsOverdue));
                this.RaisePropertyChanged(nameof(IsDueSoon));
                this.RaisePropertyChanged(nameof(BackgroundColor));
                this.RaisePropertyChanged(nameof(TextColor));

                // Refresh time-based properties
                TodoItem.RefreshTimeBasedProperties();
            }))
        .SelectMany(_ => SaveTodoItem())
        .SelectMany(_ => TodoCacheService.InvalidateTodo(TodoItem.Id))
        .Do(_ =>
        {
            // Ensure the TodoItem property change is properly propagated
            this.RaisePropertyChanged(nameof(TodoItem));
            this.RaisePropertyChanged(nameof(TodoItem.IsCompleted));
        });

    private IObservable<Unit> ExecuteDelete() =>
        Observable.FromAsync(async () =>
        {
            // Remove from parent collection first
            await MainThread.InvokeOnMainThreadAsync(() => DeleteAction?.Invoke(this));

            // Then invalidate cache
            await TodoCacheService.InvalidateTodo(TodoItem.Id);
        });

    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private Unit ExecuteEdit()
    {
        // Navigate to edit page for MAUI
        var editViewModel = new EditTodoViewModel(TodoItem);
        var editPage = new Views.EditTodoPage(editViewModel);

        // Subscribe to the page disappearing to check if changes were made
        editPage.Disappearing += async (sender, e) =>
        {
            if (editViewModel.WasSaved && editViewModel.UpdatedTodo != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Update the current todo with the edited values
                    TodoItem.Title = editViewModel.UpdatedTodo.Title;
                    TodoItem.Description = editViewModel.UpdatedTodo.Description;
                    TodoItem.DueDate = editViewModel.UpdatedTodo.DueDate;
                    TodoItem.Priority = editViewModel.UpdatedTodo.Priority;
                    TodoItem.Tags = editViewModel.UpdatedTodo.Tags;

                    // Trigger property notifications
                    this.RaisePropertyChanged(nameof(TodoItem));
                    this.RaisePropertyChanged(nameof(DueDateDisplay));
                    this.RaisePropertyChanged(nameof(PriorityDisplay));
                    this.RaisePropertyChanged(nameof(TagsDisplay));
                    this.RaisePropertyChanged(nameof(IsOverdue));
                    this.RaisePropertyChanged(nameof(IsDueSoon));
                    this.RaisePropertyChanged(nameof(BackgroundColor));
                    this.RaisePropertyChanged(nameof(TextColor));

                    // Save the updated todo
                    SaveTodoItem().Subscribe();
                });
            }
        };

        // Navigate to the edit page
        Application.Current!.Windows[0].Page?.Navigation.PushAsync(editPage);
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
}
