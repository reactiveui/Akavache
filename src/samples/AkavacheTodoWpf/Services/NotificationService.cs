// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Threading;
using AkavacheTodoWpf.Models;
using ReactiveUI;

namespace AkavacheTodoWpf.Services;

/// <summary>
/// Service for handling todo notifications and reminders in WPF.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public class NotificationService : ReactiveObject, IDisposable
{
    /// <summary>Backing field for <see cref="CacheInfo"/>.</summary>
    private readonly ObservableAsPropertyHelper<CacheInfo> _cacheInfo;

    /// <summary>Subject used to publish reminder notifications.</summary>
    private readonly Subject<TodoItem> _reminderSubject = new();

    /// <summary>Timer that periodically checks for due reminders.</summary>
    private readonly Timer? _reminderTimer;

    /// <summary>The currently loaded application settings.</summary>
    private AppSettings _currentSettings = new();

    /// <summary>Tracks whether <see cref="Dispose()"/> has been called.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    public NotificationService()
    {
        // Subscribe to settings changes
        TodoCacheService.GetSettings()
            .Subscribe(settings => _currentSettings = settings ?? new AppSettings());

        // Start reminder timer
        _reminderTimer = new(CheckForReminders, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        // Setup cache info
        _cacheInfo = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(30))
            .SelectMany(_ => TodoCacheService.GetCacheInfo())
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .ToProperty(this, x => x.CacheInfo, new CacheInfo());
    }

    /// <summary>
    /// Gets the cache information.
    /// </summary>
    /// <value>
    /// The cache information.
    /// </value>
    public CacheInfo CacheInfo => _cacheInfo.Value;

    /// <summary>
    /// Gets an observable that emits todos that need reminders.
    /// </summary>
    public IObservable<TodoItem> ReminderNotifications => _reminderSubject.AsObservable();

    /// <summary>
    /// Shows a system tray notification (if available).
    /// </summary>
    /// <param name="todo">The todo item.</param>
    /// <param name="message">The notification message.</param>
    /// <returns>Observable unit.</returns>
    public static IObservable<Unit> ShowTrayNotification(TodoItem todo, string message) =>
        Observable.FromAsync(async () =>
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Create a simple notification window or use system tray
                Window notificationWindow = new()
                {
                    Title = "Todo Reminder",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowStyle = WindowStyle.ToolWindow,
                    ResizeMode = ResizeMode.NoResize,
                    Topmost = true,
                    Content = new System.Windows.Controls.TextBlock
                    {
                        Text = message,
                        Margin = new(10),
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                };

                // Auto-close after 5 seconds
                DispatcherTimer timer = new()
                {
                    Interval = TimeSpan.FromSeconds(5)
                };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    notificationWindow.Close();
                };
                timer.Start();

                notificationWindow.Show();
            }));

    /// <summary>
    /// Schedules a reminder for a specific todo item.
    /// </summary>
    /// <param name="todo">The todo item to schedule.</param>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> ScheduleReminder(TodoItem todo)
    {
        if (todo?.DueDate.HasValue == false || !_currentSettings.NotificationsEnabled)
        {
            return Observable.Return(Unit.Default);
        }

        var reminderTime = todo?.DueDate!.Value.AddMinutes(-_currentSettings.NotificationMinutes);

        if (reminderTime <= DateTimeOffset.Now)
        {
            // Immediate notification for overdue items
            _reminderSubject.OnNext(todo!);
            return Observable.Return(Unit.Default);
        }

        // Schedule future notification
        var delay = reminderTime - DateTimeOffset.Now;
        if (delay == null || delay.Value < TimeSpan.Zero)
        {
            // If the delay is negative, it means the todo is overdue
            _reminderSubject.OnNext(todo!);
            return Observable.Return(Unit.Default);
        }

        return Observable.Timer(delay.Value)
            .Select(_ =>
            {
                if (todo?.IsCompleted == false)
                {
                    _reminderSubject.OnNext(todo);
                }

                return Unit.Default;
            });
    }

    /// <summary>
    /// Gets all todos that are due soon and need reminders.
    /// </summary>
    /// <returns>Observable list of todos needing reminders.</returns>
    public IObservable<List<TodoItem>?> GetTodosNeedingReminders() => TodoCacheService.GetAllTodos()
            .Select(todos => todos?.Where(todo =>
                todo is { IsCompleted: false, DueDate: not null } &&
                _currentSettings.NotificationsEnabled &&
                ShouldNotify(todo)).ToList());

    /// <summary>
    /// Sends a WPF notification for a specific todo item.
    /// </summary>
    /// <param name="todo">The todo item.</param>
    /// <param name="message">The notification message.</param>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> SendNotification(TodoItem todo, string message) =>
        Observable.FromAsync(async () => await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // Show WPF MessageBox or system notification
            MessageBox.Show(
                message,
                "Todo Reminder",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Also emit through the subject for reactive handling
            _reminderSubject.OnNext(todo);
        }));

    /// <summary>
    /// Checks for todos that need immediate reminders.
    /// </summary>
    /// <returns>An observable unit that signals when the check is complete.</returns>
    public IObservable<Unit> CheckImmediateReminders() => GetTodosNeedingReminders()
            .SelectMany(todos =>
            {
                if (todos == null || todos.Count == 0)
                {
                    return Observable.Return(Unit.Default);
                }

                return todos.ToObservable()
                    .SelectMany(todo => SendNotification(todo, GetNotificationMessage(todo)))
                    .DefaultIfEmpty(Unit.Default)
                    .Take(1);
            });

    /// <summary>
    /// Updates notification settings and reschedules reminders.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> UpdateSettings(AppSettings? settings)
    {
        if (settings == null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(settings)));
        }

        _currentSettings = settings;

        if (!settings.NotificationsEnabled)
        {
            return Observable.Return(Unit.Default);
        }

        // Reschedule all reminders with new settings
        return TodoCacheService.GetAllTodos()
            .SelectMany(todos =>
            {
                if (todos == null || todos.Count == 0)
                {
                    return Observable.Return(Unit.Default);
                }

                return todos.ToObservable()
                    .Where(todo => todo is { IsCompleted: false, DueDate: not null })
                    .SelectMany(ScheduleReminder)
                    .DefaultIfEmpty(Unit.Default)
                    .Take(1);
            });
    }

    /// <summary>
    /// Disposes the notification service and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the notification service and cleans up resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
        {
            return;
        }

        _reminderTimer?.Dispose();
        _reminderSubject?.Dispose();
        _cacheInfo?.Dispose();
        _disposed = true;
    }

    /// <summary>Builds a human-readable reminder message for a todo.</summary>
    /// <param name="todo">The todo item to build the message for.</param>
    /// <returns>A string representing the reminder message.</returns>
    private static string GetNotificationMessage(TodoItem todo)
    {
        if (todo.IsOverdue)
        {
            return $"Overdue: {todo.Title}";
        }

        if (todo.IsDueSoon)
        {
            var timeUntilDue = todo.DueDate!.Value - DateTimeOffset.Now;
            return $"Due soon: {todo.Title} (in {timeUntilDue.Hours}h {timeUntilDue.Minutes}m)";
        }

        return $"Reminder: {todo.Title}";
    }

    /// <summary>Timer callback that triggers reminder checks on a background thread.</summary>
    /// <param name="state">The timer state (unused).</param>
    private void CheckForReminders(object? state)
    {
        if (!_currentSettings.NotificationsEnabled || _disposed)
        {
            return;
        }

        // Check for reminders in background
        CheckImmediateReminders()
            .Subscribe(
                static _ => { /* Success */ },
                static ex => System.Diagnostics.Debug.WriteLine($"Reminder check failed: {ex}"));
    }

    /// <summary>Determines whether the supplied todo currently warrants a notification.</summary>
    /// <param name="todo">The todo item to check.</param>
    /// <returns>True if a notification should be sent; otherwise, false.</returns>
    private bool ShouldNotify(TodoItem todo)
    {
        if (!todo.DueDate.HasValue || todo.IsCompleted)
        {
            return false;
        }

        var now = DateTimeOffset.Now;
        var notificationTime = todo.DueDate.Value.AddMinutes(-_currentSettings.NotificationMinutes);

        return now >= notificationTime && now <= todo.DueDate.Value;
    }
}
