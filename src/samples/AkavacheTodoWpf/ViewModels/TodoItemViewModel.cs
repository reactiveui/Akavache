// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Media;
using AkavacheTodoWpf.Models;
using AkavacheTodoWpf.Services;
using AkavacheTodoWpf.Views;
using ReactiveUI;

namespace AkavacheTodoWpf.ViewModels;

/// <summary>View model for individual todo items with reactive behaviors for WPF.</summary>
[System.Diagnostics.DebuggerDisplay("{Activator}")]
[SupportedOSPlatform("windows10.0.19041.0")]
public sealed class TodoItemViewModel : ReactiveObject, IActivatableViewModel
{
    /// <summary>How long editing settles before the todo is written back to the cache.</summary>
    private const int AutoSaveThrottleMilliseconds = 500;

    /// <summary>Row background shown for a completed todo.</summary>
    private static readonly SolidColorBrush CompletedBackground = HexBrush("#F0F0F0");

    /// <summary>Row background shown for an overdue todo.</summary>
    private static readonly SolidColorBrush OverdueBackground = HexBrush("#FFEBEE");

    /// <summary>Row background shown for a todo that is due soon.</summary>
    private static readonly SolidColorBrush DueSoonBackground = HexBrush("#FFF3E0");

    /// <summary>Row text color shown for a completed todo.</summary>
    private static readonly SolidColorBrush CompletedForeground = HexBrush("#666666");

    /// <summary>Row text color shown for an overdue todo.</summary>
    private static readonly SolidColorBrush OverdueForeground = HexBrush("#D32F2F");

    /// <summary>Row text color shown for a todo that is due soon.</summary>
    private static readonly SolidColorBrush DueSoonForeground = HexBrush("#F57C00");

    /// <summary>Row text color shown for a todo that needs no attention yet.</summary>
    private static readonly SolidColorBrush DefaultForeground = HexBrush("#212121");

    /// <summary>The notification service used to schedule reminders.</summary>
    private readonly NotificationService _notificationService;

    /// <summary>Backing field for <see cref="DueDateDisplay"/>.</summary>
    private readonly ObservableAsPropertyHelper<string> _dueDateDisplay;

    /// <summary>Backing field for <see cref="PriorityDisplay"/>.</summary>
    private readonly ObservableAsPropertyHelper<string> _priorityDisplay;

    /// <summary>Backing field for <see cref="IsOverdue"/>.</summary>
    private readonly ObservableAsPropertyHelper<bool> _isOverdue;

    /// <summary>Backing field for <see cref="IsDueSoon"/>.</summary>
    private readonly ObservableAsPropertyHelper<bool> _isDueSoon;

    /// <summary>Backing field for <see cref="BackgroundBrush"/>.</summary>
    private readonly ObservableAsPropertyHelper<Brush> _backgroundBrush;

    /// <summary>Backing field for <see cref="ForegroundBrush"/>.</summary>
    private readonly ObservableAsPropertyHelper<Brush> _foregroundBrush;

    /// <summary>Initializes a new instance of the <see cref="TodoItemViewModel"/> class.</summary>
    /// <param name="todoItem">The todo item model.</param>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="deleteAction">Action to call when deleting this item, or null when the item cannot be deleted.</param>
    public TodoItemViewModel(TodoItem todoItem, NotificationService notificationService, Action<TodoItemViewModel>? deleteAction)
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

        // Setup WPF-specific brush properties
        _backgroundBrush = this.WhenAnyValue(
                x => x.TodoItem.IsCompleted,
                x => x.IsOverdue,
                x => x.IsDueSoon)
            .Select(_ => GetBackgroundBrush())
            .ToProperty(this, x => x.BackgroundBrush);

        _foregroundBrush = this.WhenAnyValue(
                x => x.TodoItem.IsCompleted,
                x => x.IsOverdue,
                x => x.IsDueSoon)
            .Select(_ => GetForegroundBrush())
            .ToProperty(this, x => x.ForegroundBrush);

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

    /// <summary>Gets the formatted due date display string.</summary>
    public string DueDateDisplay => _dueDateDisplay.Value;

    /// <summary>Gets the priority display string.</summary>
    public string PriorityDisplay => _priorityDisplay.Value;

    /// <summary>Gets a value indicating whether the todo is overdue.</summary>
    public bool IsOverdue => _isOverdue.Value;

    /// <summary>Gets a value indicating whether the todo is due soon.</summary>
    public bool IsDueSoon => _isDueSoon.Value;

    /// <summary>Gets the background brush based on todo status.</summary>
    public Brush BackgroundBrush => _backgroundBrush.Value;

    /// <summary>Gets the foreground brush based on todo status.</summary>
    public Brush ForegroundBrush => _foregroundBrush.Value;

    /// <summary>Gets the command to toggle completion status.</summary>
    public ReactiveCommand<RxVoid, RxVoid> ToggleCompletedCommand { get; }

    /// <summary>Gets the command to delete the todo.</summary>
    public ReactiveCommand<RxVoid, RxVoid> DeleteCommand { get; }

    /// <summary>Gets the command to edit the todo.</summary>
    public ReactiveCommand<RxVoid, RxVoid> EditCommand { get; }

    /// <summary>Gets the command to schedule a reminder.</summary>
    public ReactiveCommand<RxVoid, RxVoid> ScheduleReminderCommand { get; }

    /// <summary>Gets the priority color indicator brush.</summary>
    public Brush PriorityBrush => TodoItem.Priority switch
    {
        TodoPriority.Critical => Brushes.Red,
        TodoPriority.High => Brushes.Orange,
        TodoPriority.Medium => Brushes.Green,
        TodoPriority.Low => Brushes.Blue,
        _ => Brushes.Gray
    };

    /// <summary>Gets formatted tags as a single string.</summary>
    public string TagsDisplay => TodoItem.Tags.Count > 0 ? string.Join(", ", TodoItem.Tags) : string.Empty;

    /// <summary>Gets the CSS class for the priority badge.</summary>
    public string PriorityBadgeClass => GetPriorityBadgeClass();

    /// <summary>Gets the CSS class for the completion icon.</summary>
    public string CompletionIconClass => TodoItem.IsCompleted ? "bi bi-check-circle-fill text-success" : "bi bi-circle text-muted";

    /// <summary>Creates a brush from a hexadecimal <c>#RRGGBB</c> color string.</summary>
    /// <param name="hex">The color in <c>#RRGGBB</c> form.</param>
    /// <returns>A brush painting the requested color.</returns>
    private static SolidColorBrush HexBrush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    /// <summary>Toggles the todo's completion state and persists the change.</summary>
    /// <returns>An observable that signals when the toggle and save operation is complete.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IObservable<RxVoid> ExecuteToggleCompleted() =>
        Signal.FromAsync(async () =>
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TodoItem.IsCompleted = !TodoItem.IsCompleted;

                // Trigger property notifications for UI updates
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
                this.RaisePropertyChanged();
            });
            return RxVoid.Default;
        })
        .SelectMany(_ => SaveTodoItem())
        .SelectMany(_ =>

            // Invalidate cache for this specific todo to force refresh
            TodoCacheService.InvalidateTodo(TodoItem.Id));

    /// <summary>Deletes this todo from its parent collection and the cache.</summary>
    /// <returns>An observable that signals when the deletion operation is complete.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IObservable<RxVoid> ExecuteDelete() =>
        Signal.FromAsync(async () =>
        {
            // Remove from parent collection first
            await Application.Current.Dispatcher.InvokeAsync(() => DeleteAction?.Invoke(this));

            // Then invalidate cache
            await TodoCacheService.InvalidateTodo(TodoItem.Id);
            return RxVoid.Default;
        });

    /// <summary>Opens the edit dialog and applies the resulting changes.</summary>
    /// <returns>An observable that signals when the edit operation is complete.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IObservable<RxVoid> ExecuteEdit() =>
        Signal.FromAsync(async () =>
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
{
EditTodoDialog dialog = new(TodoItem);
if (dialog.ShowDialog() != true)
{
return;
}

var updatedTodo = dialog.UpdatedTodo;
if (updatedTodo is null)
{
return;
}

TodoItem.Title = updatedTodo.Title;
TodoItem.Description = updatedTodo.Description;
TodoItem.DueDate = updatedTodo.DueDate;
TodoItem.Priority = updatedTodo.Priority;

    // Trigger property notifications for ALL relevant properties
this.RaisePropertyChanged();
this.RaisePropertyChanged();
this.RaisePropertyChanged();
this.RaisePropertyChanged();
this.RaisePropertyChanged();
this.RaisePropertyChanged();

_ = SaveTodoItem().Subscribe();
});
            return RxVoid.Default;
        });

    /// <summary>Schedules a reminder for this todo if it has a due date.</summary>
    /// <returns>An observable that signals when the reminder has been scheduled.</returns>
    private IObservable<RxVoid> ExecuteScheduleReminder() => !TodoItem.DueDate.HasValue ? Signal.Return(RxVoid.Default) : _notificationService.ScheduleReminder(TodoItem);

    /// <summary>Persists this todo back to the cached collection.</summary>
    /// <returns>An observable that signals when the save operation is complete.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    if (todos[i].Id != TodoItem.Id)
                    {
                        continue;
                    }

                    index = i;
                    break;
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

    /// <summary>Computes the row background brush for the current todo state.</summary>
    /// <returns>The background brush for the todo item.</returns>
    private SolidColorBrush GetBackgroundBrush()
    {
        if (TodoItem.IsCompleted)
        {
            return CompletedBackground;
        }

        if (IsOverdue)
        {
            return OverdueBackground;
        }

        return IsDueSoon ? DueSoonBackground : Brushes.Transparent;
    }

    /// <summary>Computes the row foreground brush for the current todo state.</summary>
    /// <returns>The foreground brush for the todo item.</returns>
    private SolidColorBrush GetForegroundBrush()
    {
        if (TodoItem.IsCompleted)
        {
            return CompletedForeground;
        }

        if (IsOverdue)
        {
            return OverdueForeground;
        }

        return IsDueSoon ? DueSoonForeground : DefaultForeground;
    }

    /// <summary>Returns the Bootstrap badge class for the current priority.</summary>
    /// <returns>A string representing the CSS class for the priority badge.</returns>
    private string GetPriorityBadgeClass() => TodoItem.Priority switch
    {
        TodoPriority.Critical => "badge bg-danger",
        TodoPriority.High => "badge bg-warning",
        TodoPriority.Medium => "badge bg-success",
        TodoPriority.Low => "badge bg-info",
        _ => "badge bg-secondary"
    };
}
