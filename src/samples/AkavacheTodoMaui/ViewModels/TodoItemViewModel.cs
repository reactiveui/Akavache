// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using AkavacheTodoMaui.Models;
using AkavacheTodoMaui.Services;
using AkavacheTodoMaui.Views;
using ReactiveUI;

namespace AkavacheTodoMaui.ViewModels;

/// <summary>View model for individual todo items with reactive behaviors.</summary>
[RequiresUnreferencedCode("ReactiveObject requires types to be preserved for reflection.")]
[RequiresDynamicCode("ReactiveObject requires types to be preserved for reflection.")]
public sealed class TodoItemViewModel : ReactiveObject, IActivatableViewModel
{
    /// <summary>How long editing settles before the todo is written back to the cache.</summary>
    private const int AutoSaveThrottleMilliseconds = 500;

    /// <summary>The notification service used for scheduling reminders.</summary>
    private readonly NotificationService _notificationService;

    /// <summary>OAPH exposing the formatted due-date text.</summary>
    private readonly ObservableAsPropertyHelper<string> _dueDateDisplay;

    /// <summary>OAPH exposing the priority display text.</summary>
    private readonly ObservableAsPropertyHelper<string> _priorityDisplay;

    /// <summary>OAPH exposing whether the todo is overdue.</summary>
    private readonly ObservableAsPropertyHelper<bool> _isOverdue;

    /// <summary>OAPH exposing whether the todo is due soon.</summary>
    private readonly ObservableAsPropertyHelper<bool> _isDueSoon;

    /// <summary>Initializes a new instance of the <see cref="TodoItemViewModel"/> class.</summary>
    /// <param name="todoItem">The todo item model.</param>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="deleteAction">Action to call when deleting this item, or null when the item cannot be deleted.</param>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public TodoItemViewModel(TodoItem todoItem, NotificationService notificationService, Action<TodoItemViewModel>? deleteAction)
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
            .Select(static dueDate => dueDate?.ToString("MMM dd, yyyy HH:mm") ?? "No due date")
            .ToProperty(this, x => x.DueDateDisplay);

        _priorityDisplay = this.WhenAnyValue(x => x.TodoItem.Priority)
            .Select(static priority => priority.ToString())
            .ToProperty(this, x => x.PriorityDisplay);

        _isOverdue = this.WhenAnyValue(x => x.TodoItem.DueDate, x => x.TodoItem.IsCompleted)
            .Select(_ => TodoItem.IsOverdue)
            .ToProperty(this, x => x.IsOverdue);

        _isDueSoon = this.WhenAnyValue(x => x.TodoItem.DueDate, x => x.TodoItem.IsCompleted)
            .Select(_ => TodoItem.IsDueSoon)
            .ToProperty(this, x => x.IsDueSoon);

        // Setup activator
        Activator = new();

        this.WhenActivated(disposables =>
            _ = this.WhenAnyValue(x => x.TodoItem.IsCompleted)
                .Skip(1) // Skip initial value
                .Throttle(TimeSpan.FromMilliseconds(AutoSaveThrottleMilliseconds))
                .SelectMany(_ => SaveTodoItem())
                .Subscribe(
                    static _ => { },
                    static ex => System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex}"))
                .DisposeWith(disposables));
    }

    /// <summary>Gets the view model activator.</summary>
    public ViewModelActivator Activator { get; }

    /// <summary>Gets the todo item model.</summary>
    public TodoItem TodoItem { get; }

    /// <summary>Gets or sets the delete action to call when deleting this item.</summary>
    public Action<TodoItemViewModel>? DeleteAction { get; set; }

    /// <summary>Gets the formatted due date display.</summary>
    public string DueDateDisplay => _dueDateDisplay.Value;

    /// <summary>Gets the priority display string.</summary>
    public string PriorityDisplay => _priorityDisplay.Value;

    /// <summary>Gets a value indicating whether the todo is overdue.</summary>
    public bool IsOverdue => _isOverdue.Value;

    /// <summary>Gets a value indicating whether the todo is due soon.</summary>
    public bool IsDueSoon => _isDueSoon.Value;

    /// <summary>Gets the command to toggle completion status.</summary>
    public ReactiveCommand<RxVoid, RxVoid> ToggleCompletedCommand { get; }

    /// <summary>Gets the command to delete the todo.</summary>
    public ReactiveCommand<RxVoid, RxVoid> DeleteCommand { get; }

    /// <summary>Gets the command to edit the todo.</summary>
    public ReactiveCommand<RxVoid, RxVoid> EditCommand { get; }

    /// <summary>Gets the command to schedule a reminder.</summary>
    public ReactiveCommand<RxVoid, RxVoid> ScheduleReminderCommand { get; }

    /// <summary>Gets the background color based on todo status.</summary>
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

            return IsDueSoon ? "#FFF3E0" : "White";
        }
    }

    /// <summary>Gets the text color based on todo status.</summary>
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

            return IsDueSoon ? "#E65100" : "#212121";
        }
    }

    /// <summary>Gets the priority color indicator.</summary>
    public string PriorityColor => TodoItem.Priority switch
    {
        TodoPriority.Critical => "#D32F2F",
        TodoPriority.High => "#F57C00",
        TodoPriority.Medium => "#388E3C",
        TodoPriority.Low => "#1976D2",
        _ => "#666666"
    };

    /// <summary>Gets formatted tags as a single string.</summary>
    public string TagsDisplay => TodoItem.Tags.Count > 0 ? string.Join(", ", TodoItem.Tags) : string.Empty;

    /// <summary>Command handler that toggles the todo's completion status.</summary>
    /// <returns>An observable that completes when the toggle has been persisted.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<RxVoid> ExecuteToggleCompleted() =>
        Signal.FromAsync(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                TodoItem.IsCompleted = !TodoItem.IsCompleted;

                // Trigger property notifications for UI updates
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();

                // Refresh time-based properties
                TodoItem.RefreshTimeBasedProperties();
            });
            return RxVoid.Default;
        })
        .SelectMany(_ => SaveTodoItem())
        .SelectMany(_ => TodoCacheService.InvalidateTodo(TodoItem.Id))
        .Do(_ =>
        {
            // Ensure the TodoItem property change is properly propagated
            this.RaisePropertyChanged();
            this.RaisePropertyChanged();
        });

    /// <summary>Command handler that deletes the todo and invalidates its cache entry.</summary>
    /// <returns>An observable that completes when the delete is done.</returns>
    private IObservable<RxVoid> ExecuteDelete() =>
        Signal.FromAsync(async () =>
        {
            // Remove from parent collection first
            await MainThread.InvokeOnMainThreadAsync(() => DeleteAction?.Invoke(this));

            // Then invalidate cache
            await TodoCacheService.InvalidateTodo(TodoItem.Id);
            return RxVoid.Default;
        });

    /// <summary>Command handler that navigates to the edit page for this todo.</summary>
    /// <returns>Unit default value.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private RxVoid ExecuteEdit()
    {
        // Navigate to edit page for MAUI
        EditTodoViewModel editViewModel = new(TodoItem);
        EditTodoPage editPage = new(editViewModel);

        // Subscribe to the page disappearing to check if changes were made
        editPage.Disappearing += async (_, _) =>
        {
            if (!editViewModel.WasSaved || editViewModel.UpdatedTodo is null)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Update the current todo with the edited values
                TodoItem.Title = editViewModel.UpdatedTodo.Title;
                TodoItem.Description = editViewModel.UpdatedTodo.Description;
                TodoItem.DueDate = editViewModel.UpdatedTodo.DueDate;
                TodoItem.Priority = editViewModel.UpdatedTodo.Priority;
                TodoItem.Tags = editViewModel.UpdatedTodo.Tags;

                // Trigger property notifications
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();

                // Save the updated todo
                _ = SaveTodoItem().Subscribe();
            });
        };

        // Navigate to the edit page
        Application.Current!.Windows[0].Page?.Navigation.PushAsync(editPage);
        return RxVoid.Default;
    }

    /// <summary>Command handler that schedules a reminder for this todo.</summary>
    /// <returns>An observable that completes when scheduling is done.</returns>
    private IObservable<RxVoid> ExecuteScheduleReminder() => !TodoItem.DueDate.HasValue
        ? Signal.Return(RxVoid.Default)
        : _notificationService.ScheduleReminder(TodoItem);

    /// <summary>Persists the current todo item by merging it into the cached todo list.</summary>
    /// <returns>An observable that completes when the save is done.</returns>
    private IObservable<RxVoid> SaveTodoItem() =>
        TodoCacheService.GetAllTodos()
            .Take(1)
            .SelectMany(todos =>
            {
                todos ??= [];

                // Replace the matching todo in the cached list, or append it when it is new
                var index = -1;
                for (var i = 0; i < todos.Count; i++)
                {
                    if (todos[i].Id == TodoItem.Id)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
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
