// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using AkavacheTodoMaui.Models;

namespace AkavacheTodoMaui.Services;

/// <summary>
/// Service for handling todo notifications and reminders.
/// </summary>
public class NotificationService : IDisposable
{
    private readonly Subject<TodoItem> _reminderSubject = new();
    private readonly Timer? _reminderTimer;
    private AppSettings _currentSettings = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public NotificationService()
    {
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
    /// Gets the notification message for a todo item.
    /// </summary>
    /// <param name="todo">The todo item.</param>
    /// <returns>The notification message.</returns>
    public static string GetNotificationMessage(TodoItem todo)
    {
        if (todo?.IsOverdue == true)
        {
            return $"Overdue: {todo.Title}";
        }

        if (todo?.IsDueSoon == true)
        {
            var timeUntilDue = todo.DueDate!.Value - DateTimeOffset.Now;
            return $"Due soon: {todo.Title} (in {timeUntilDue.Hours}h {timeUntilDue.Minutes}m)";
        }

        return $"Reminder: {todo?.Title}";
    }

    /// <summary>
    /// Schedules a reminder for a specific todo item.
    /// </summary>
    /// <param name="todo">The todo item to schedule.</param>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> ScheduleReminder(TodoItem todo)
    {
        if (todo == null)
        {
            throw new ArgumentNullException(nameof(todo));
        }

        if (!todo.DueDate.HasValue || !_currentSettings.NotificationsEnabled)
        {
            return Observable.Return(Unit.Default);
        }

        var reminderTime = todo.DueDate.Value.AddMinutes(-_currentSettings.NotificationMinutes);

        if (reminderTime <= DateTimeOffset.Now)
        {
            // Immediate notification for overdue items
            _reminderSubject.OnNext(todo);
            return Observable.Return(Unit.Default);
        }

        // Schedule future notification (in a real app, you'd use platform-specific scheduling)
        var delay = reminderTime - DateTimeOffset.Now;
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
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public IObservable<List<TodoItem>> GetTodosNeedingReminders() =>
        TodoCacheService.GetAllTodos()
            .Select(todos => todos.Where(todo =>
                !todo.IsCompleted &&
                todo.DueDate.HasValue &&
                _currentSettings.NotificationsEnabled &&
                ShouldNotify(todo)).ToList());

    /// <summary>
    /// Sends a notification for a specific todo item.
    /// </summary>
    /// <param name="todo">The todo item.</param>
    /// <param name="message">The notification message.</param>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> SendNotification(TodoItem todo, string message) =>

        // In a real app, this would trigger platform-specific notifications
        // For demo purposes, we'll just emit through the subject
        Observable.FromAsync(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // This would be replaced with actual notification display
                _reminderSubject.OnNext(todo);
            });
        });

    /// <summary>
    /// Checks for todos that need immediate reminders.
    /// </summary>
    /// <returns>Observable unit.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public IObservable<Unit> CheckImmediateReminders() => GetTodosNeedingReminders()
            .SelectMany(todos => todos.ToObservable())
            .SelectMany(todo => SendNotification(todo, GetNotificationMessage(todo)))
            .Aggregate(Unit.Default, (_, __) => Unit.Default);

    /// <summary>
    /// Updates notification settings and reschedules reminders.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    /// <returns>Observable unit.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public IObservable<Unit> UpdateSettings(AppSettings? settings)
    {
        _currentSettings = settings ?? new AppSettings();

        if (settings?.NotificationsEnabled == false)
        {
            return Observable.Return(Unit.Default);
        }

        // Reschedule all reminders with new settings
        return TodoCacheService.GetAllTodos()
            .SelectMany(static todos => todos.ToObservable())
            .Where(static todo => !todo.IsCompleted && todo.DueDate.HasValue)
            .SelectMany(ScheduleReminder)
            .Aggregate(Unit.Default, static (_, __) => Unit.Default);
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
    /// <param name="disposing">True to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _reminderTimer?.Dispose();
            _reminderSubject?.Dispose();
            _disposed = true;
        }
    }

    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private void CheckForReminders(object? state)
    {
        if (!_currentSettings.NotificationsEnabled)
        {
            return;
        }

        // Check for reminders in background
        CheckImmediateReminders()
            .Subscribe(
                static _ => { /* Success */ },
                static ex => System.Diagnostics.Debug.WriteLine($"Reminder check failed: {ex}"));
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
