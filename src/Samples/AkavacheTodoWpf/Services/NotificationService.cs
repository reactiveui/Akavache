// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using AkavacheTodoWpf.Models;

namespace AkavacheTodoWpf.Services;

/// <summary>
/// Service for handling todo notifications and reminders in WPF.
/// </summary>
public class NotificationService : IDisposable
{
    private readonly TodoCacheService _cacheService;
    private readonly Subject<TodoItem> _reminderSubject = new();
    private readonly Timer? _reminderTimer;
    private AppSettings _currentSettings = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="cacheService">The cache service.</param>
    public NotificationService(TodoCacheService cacheService)
    {
        _cacheService = cacheService;

        // Subscribe to settings changes
        TodoCacheService.GetSettings()
            .Subscribe(settings => _currentSettings = settings);

        // Start reminder timer
        _reminderTimer = new Timer(CheckForReminders, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

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
        {
            // In a real application, you might use NotifyIcon or Toast notifications
            // For this demo, we'll use a simple approach
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Create a simple notification window or use system tray
                var notificationWindow = new Window
                {
                    Title = "Todo Reminder",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowStyle = WindowStyle.ToolWindow,
                    ResizeMode = ResizeMode.NoResize,
                    Topmost = true
                };

                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = message,
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                notificationWindow.Content = textBlock;

                // Auto-close after 5 seconds
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    notificationWindow.Close();
                };
                timer.Start();

                notificationWindow.Show();
            });
        });

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
        if (delay == null || delay < TimeSpan.Zero)
        {
            // If the delay is negative, it means the todo is overdue
            _reminderSubject.OnNext(todo!);
            return Observable.Return(Unit.Default);
        }

        return Observable.Timer(delay)
            .Select(_ =>
            {
                if (!todo.IsCompleted)
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
                !todo.IsCompleted &&
                todo.DueDate.HasValue &&
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
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> CheckImmediateReminders() => GetTodosNeedingReminders()
            .SelectMany(todos => Observable.FromIterable(todos))
            .SelectMany(todo => SendNotification(todo, GetNotificationMessage(todo)))
            .Aggregate(Unit.Default, (_, __) => Unit.Default);

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
            .SelectMany(todos => Observable.FromIterable(todos))
            .Where(todo => !todo.IsCompleted && todo.DueDate.HasValue)
            .SelectMany(ScheduleReminder)
            .Aggregate(Unit.Default, (_, __) => Unit.Default);
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
        if (!_disposed && disposing)
        {
            _reminderTimer?.Dispose();
            _reminderSubject?.Dispose();
            _disposed = true;
        }
    }

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

    private void CheckForReminders(object? state)
    {
        if (!_currentSettings.NotificationsEnabled || _disposed)
        {
            return;
        }

        // Check for reminders in background
        CheckImmediateReminders()
            .Subscribe(
                _ => { /* Success */ },
                ex => System.Diagnostics.Debug.WriteLine($"Reminder check failed: {ex}"));
    }

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
