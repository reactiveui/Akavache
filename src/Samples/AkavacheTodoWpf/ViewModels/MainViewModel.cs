// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Runtime.Versioning;
using System.Windows;
using AkavacheTodoWpf.Models;
using AkavacheTodoWpf.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AkavacheTodoWpf.ViewModels;

/// <summary>
/// Main view model for the WPF Todo application demonstrating ReactiveUI and Akavache integration.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public partial class MainViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly NotificationService _notificationService;
    private readonly ObservableAsPropertyHelper<bool> _isLoading;
    private readonly ObservableAsPropertyHelper<TodoStats?> _todoStats;
    private readonly ObservableAsPropertyHelper<CacheInfo?> _cacheInfo;

    [Reactive]
    private string _newTodoTitle = string.Empty;
    [Reactive]
    private string _newTodoDescription = string.Empty;
    [Reactive]
    private DateTime? _newTodoDueDate;
    [Reactive]
    private TodoPriority _newTodoPriority = TodoPriority.Medium;
    [Reactive]
    private AppSettings? _settings = new();
    [Reactive]
    private string _statusMessage = "Ready";
    [Reactive]
    private string _newTodoTime = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    public MainViewModel(NotificationService notificationService)
    {
        _notificationService = notificationService;

        // Initialize collections
        Todos = [];
        Notifications = [];
        PriorityOptions = Enum.GetValues<TodoPriority>();

        // Create commands
        AddTodoCommand = ReactiveCommand.CreateFromObservable(ExecuteAddTodo);
        RefreshCommand = ReactiveCommand.CreateFromObservable(ExecuteRefresh);
        ClearCompletedCommand = ReactiveCommand.CreateFromObservable(ExecuteClearCompleted);
        SaveSettingsCommand = ReactiveCommand.CreateFromObservable(ExecuteSaveSettings);
        CleanupCacheCommand = ReactiveCommand.CreateFromObservable(ExecuteCleanupCache);
        LoadSampleDataCommand = ReactiveCommand.CreateFromObservable(ExecuteLoadSampleData);
        ExitCommand = ReactiveCommand.CreateFromObservable(ExecuteExit);

        // Initialize observable properties in constructor
        ReactiveCommand<Unit, Unit>[] loadingCommands = [AddTodoCommand, RefreshCommand, ClearCompletedCommand, SaveSettingsCommand, CleanupCacheCommand, LoadSampleDataCommand
        ];
        _isLoading = loadingCommands
            .Select(static cmd => cmd.IsExecuting)
            .CombineLatest(static executing => executing.Any(static x => x))
            .ToProperty(this, static x => x.IsLoading);

        // Enhanced statistics calculation that responds to individual todo property changes
        _todoStats = Observable.Merge(
            this.WhenAnyValue(static x => x.Todos.Count).Select(static _ => Unit.Default),
            Todos.ObserveCollectionChanges().Select(static _ => Unit.Default),
            this.WhenAnyValue(static x => x.TodoStats).Select(static _ => Unit.Default).Skip(1))
        .Throttle(TimeSpan.FromMilliseconds(500))
        .SelectMany(static _ => TodoCacheService.GetTodoStats())
        .Catch(Observable.Return(new TodoStats()))
        .ObserveOn(RxApp.MainThreadScheduler)
        .ToProperty(this, static x => x.TodoStats);

        // Setup cache info with reduced frequency and better error handling
        _cacheInfo = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(5)) // Reduced to 5 minutes instead of 30 seconds
            .SelectMany(static _ => TodoCacheService.GetCacheInfo()) // Remove unnecessary cache_test insertion
            .Retry(3)
            .Catch(static (Exception ex) =>
            {
                System.Diagnostics.Debug.WriteLine($"Cache info failed: {ex}");
                return Observable.Return(new CacheInfo
                {
                    UserAccountKeys = 0,
                    LocalMachineKeys = 0,
                    SecureKeys = 0,
                    TotalKeys = 0,
                    LastChecked = DateTimeOffset.Now
                });
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, static x => x.CacheInfo);

        // Setup activator for proper lifecycle management
        Activator = new ViewModelActivator();

        this.WhenActivated(SetupBindings);

        // Manually activate immediately to ensure initial data loading
        Activator.Activate();
    }

    /// <summary>
    /// Gets a value indicating whether any operation is loading.
    /// </summary>
    public bool IsLoading => _isLoading.Value;

    /// <summary>
    /// Gets the current todo statistics.
    /// </summary>
    public TodoStats? TodoStats => _todoStats?.Value ?? default;

    /// <summary>
    /// Gets the current cache information.
    /// </summary>
    public CacheInfo? CacheInfo => _cacheInfo.Value;

    /// <summary>
    /// Gets the view model activator for lifecycle management.
    /// </summary>
    public ViewModelActivator Activator { get; }

    /// <summary>
    /// Gets the collection of todo items.
    /// </summary>
    public ObservableCollection<TodoItemViewModel> Todos { get; }

    /// <summary>
    /// Gets the collection of notification messages.
    /// </summary>
    public ObservableCollection<string> Notifications { get; }

    /// <summary>
    /// Gets the priority options for the ComboBox.
    /// </summary>
    public TodoPriority[] PriorityOptions { get; }

    /// <summary>
    /// Gets the command to add a new todo.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddTodoCommand { get; }

    /// <summary>
    /// Gets the command to refresh all data.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    /// <summary>
    /// Gets the command to clear completed todos.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearCompletedCommand { get; }

    /// <summary>
    /// Gets the command to save settings.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }

    /// <summary>
    /// Gets the command to cleanup cache.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CleanupCacheCommand { get; }

    /// <summary>
    /// Gets the command to load sample data.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadSampleDataCommand { get; }

    /// <summary>
    /// Gets the command to exit the application.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    /// <summary>
    /// Saves application state when shutting down.
    /// </summary>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> SaveApplicationState() => Observable.Merge(
            SaveCurrentTodos(),
            TodoCacheService.SaveSettings(Settings),
            TodoCacheService.SaveApplicationState());

    private static List<TodoItem> CreateSampleTodos()
    {
        var now = DateTimeOffset.Now;

        return
        [
            new TodoItem
            {
                Title = "Review Akavache Documentation",
                Description = "Go through the comprehensive Akavache documentation and examples",
                DueDate = now.AddHours(2),
                Priority = TodoPriority.High,
                Tags = ["documentation", "akavache"]
            },
            new TodoItem
            {
                Title = "Implement Cache Expiration",
                Description = "Add proper cache expiration for temporary data",
                DueDate = now.AddDays(1),
                Priority = TodoPriority.Medium,
                Tags = ["development", "caching"]
            },
            new TodoItem
            {
                Title = "Test Notification System",
                Description = "Verify that notifications work correctly for due dates",
                DueDate = now.AddMinutes(30),
                Priority = TodoPriority.Critical,
                Tags = ["testing", "notifications"]
            },
            new TodoItem
            {
                Title = "Write Unit Tests",
                Description = "Create comprehensive unit tests for cache service",
                DueDate = now.AddDays(3),
                Priority = TodoPriority.High,
                Tags = ["testing", "development"]
            },
            new TodoItem
            {
                Title = "Optimize Performance",
                Description = "Profile and optimize cache performance for large datasets",
                Priority = TodoPriority.Low,
                Tags = ["performance", "optimization"]
            }
        ];
    }

    private void SetupBindings(CompositeDisposable disposables)
    {
        // Dispose the property helpers when deactivated
        _isLoading.DisposeWith(disposables);
        _todoStats.DisposeWith(disposables);
        _cacheInfo.DisposeWith(disposables);

        // Subscribe to individual todo property changes for statistics updates
        Todos.ToObservableChangeSet()
            .ToCollection()
            .SelectMany(x => x)
            .Where(todoVm => todoVm.TodoItem != null)
            .Select(todoVm =>
            todoVm.TodoItem.WhenAnyValue(
                x => x.IsCompleted,
                x => x.DueDate,
                x => x.Priority,
                x => x.CreatedAt))
        .Throttle(TimeSpan.FromMilliseconds(500))
        .SelectMany(_ => TodoCacheService.GetTodoStats())
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(_ =>
            this.RaisePropertyChanged(nameof(TodoStats)))
        .DisposeWith(disposables);

        // Simple statistics refresh on todo property changes
        Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
            .Where(_ => Todos.Count > 0) // Only if we have todos
            .SelectMany(_ => TodoCacheService.GetTodoStats())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
                this.RaisePropertyChanged(nameof(TodoStats)))
            .DisposeWith(disposables);

        // Timer to refresh time-dependent properties (IsOverdue, IsDueSoon) every minute
        Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                // Trigger property notifications for all todos to refresh time-dependent UI
                foreach (var todoViewModel in Todos)
                {
                    todoViewModel.TodoItem.RaisePropertyChanged(nameof(TodoItem.IsOverdue));
                    todoViewModel.TodoItem.RaisePropertyChanged(nameof(TodoItem.IsDueSoon));
                }

                // Also refresh statistics
                this.RaisePropertyChanged(nameof(TodoStats));
            })
            .DisposeWith(disposables);

        // Load initial data
        LoadInitialData().Subscribe(
            _ => { },
            ex => StatusMessage = $"Error loading data: {ex.Message}")
            .DisposeWith(disposables);

        // Subscribe to notifications with timestamp-based deduplication
        _notificationService.ReminderNotifications
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(todo =>
            {
                var timestamp = DateTimeOffset.Now.ToString("HH:mm:ss");
                var baseMessage = $"Reminder: {todo.Title}";
                var messageWithTimestamp = $"{baseMessage} [{timestamp}]";

                // Check if a notification for this todo already exists
                var existingIndex = -1;
                for (var i = 0; i < Notifications.Count; i++)
                {
                    if (Notifications[i].Contains(baseMessage))
                    {
                        existingIndex = i;
                        break;
                    }
                }

                if (existingIndex >= 0)
                {
                    // Update existing notification with new timestamp
                    Notifications[existingIndex] = messageWithTimestamp;
                }
                else
                {
                    // Add new notification
                    Notifications.Insert(0, messageWithTimestamp);

                    // Keep only the latest 10 notifications to prevent overflow
                    while (Notifications.Count > 10)
                    {
                        Notifications.RemoveAt(Notifications.Count - 1);
                    }
                }

                StatusMessage = baseMessage;
            })
            .DisposeWith(disposables);

        // Auto-save when todos change
        this.WhenAnyValue(x => x.Todos.Count)
            .Skip(1) // Skip initial load
            .Throttle(TimeSpan.FromSeconds(2))
            .SelectMany(_ => SaveCurrentTodos())
            .Subscribe(
                _ => { },
                ex => StatusMessage = $"Auto-save failed: {ex.Message}")
            .DisposeWith(disposables);

        // Handle command errors globally
        Observable.Merge(
            AddTodoCommand.ThrownExceptions,
            RefreshCommand.ThrownExceptions,
            ClearCompletedCommand.ThrownExceptions,
            SaveSettingsCommand.ThrownExceptions,
            CleanupCacheCommand.ThrownExceptions,
            LoadSampleDataCommand.ThrownExceptions)
            .Subscribe(ex =>
            {
                StatusMessage = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Command error: {ex}");
            })
            .DisposeWith(disposables);
    }

    private IObservable<Unit> LoadInitialData()
    {
        StatusMessage = "Loading data...";

        return LoadTodos().Merge(
            LoadSettings())
        .Finally(() => StatusMessage = "Ready");
    }

    private IObservable<Unit> LoadTodos() => TodoCacheService.GetAllTodos()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(todos =>
            {
                if (todos == null || todos.Count == 0)
                {
                    StatusMessage = "No todos found. You can add some!";
                    return;
                }

                Todos.Clear();
                foreach (var todo in todos.OrderBy(GetSortKey))
                {
                    var todoViewModel = new TodoItemViewModel(todo, _notificationService, RemoveTodoFromCollection);
                    Todos.Add(todoViewModel);
                }
            })
            .Select(_ => Unit.Default);

    /// <summary>
    /// Removes a todo from the collection and updates the cache.
    /// </summary>
    /// <param name="todoViewModel">The todo view model to remove.</param>
    private void RemoveTodoFromCollection(TodoItemViewModel todoViewModel)
    {
        Todos.Remove(todoViewModel);
        StatusMessage = $"Deleted todo: {todoViewModel.TodoItem.Title}";

        // Save the updated collection
        SaveCurrentTodos().Subscribe();
    }

    private IObservable<Unit> LoadSettings() => TodoCacheService.GetSettings()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(settings => Settings = settings)
            .Select(_ => Unit.Default);

    private IObservable<Unit> SaveCurrentTodos()
    {
        var todos = Todos.Select(vm => vm.TodoItem).ToList();
        return TodoCacheService.SaveTodos(todos)
            .Do(_ => StatusMessage = $"Saved {todos.Count} todos");
    }

    private IObservable<Unit> ExecuteAddTodo()
    {
        // Parse the date and time
        DateTime? dueDateTime = null;
        if (NewTodoDueDate.HasValue)
        {
            dueDateTime = NewTodoDueDate.Value.Date;
            if (!string.IsNullOrWhiteSpace(NewTodoTime) && TimeSpan.TryParse(NewTodoTime, out var time))
            {
                dueDateTime = dueDateTime.Value.Add(time);
            }
        }

        var newTodo = new TodoItem
        {
            Title = NewTodoTitle,
            Description = NewTodoDescription,
            DueDate = dueDateTime.HasValue ? new DateTimeOffset(dueDateTime.Value) : null,
            Priority = NewTodoPriority,
            CreatedAt = DateTimeOffset.Now
        };

        var viewModel = new TodoItemViewModel(newTodo, _notificationService, RemoveTodoFromCollection);

        return Observable.Start(
            () =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                Todos.Insert(0, viewModel);

                // Clear form
                NewTodoTitle = string.Empty;
                NewTodoDescription = string.Empty;
                NewTodoDueDate = null;
                NewTodoTime = string.Empty;
                NewTodoPriority = Settings!.DefaultPriority;
            }),
            RxApp.MainThreadScheduler)
        .SelectMany(_ => _notificationService.ScheduleReminder(newTodo))
        .ObserveOn(RxApp.MainThreadScheduler)
        .Do(_ => StatusMessage = $"Added todo: {newTodo.Title}");
    }

    private IObservable<Unit> ExecuteRefresh()
    {
        StatusMessage = "Refreshing...";
        return LoadInitialData();
    }

    private IObservable<Unit> ExecuteClearCompleted() =>
        Observable.FromAsync(async () =>
        {
            var completedTodos = Todos.Where(vm => vm.TodoItem.IsCompleted).ToList();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var completedTodo in completedTodos)
                {
                    Todos.Remove(completedTodo);
                }
            });

            StatusMessage = $"Removed {completedTodos.Count} completed todos";
        });

    private IObservable<Unit> ExecuteSaveSettings() => TodoCacheService.SaveSettings(Settings)
            .SelectMany(_ => _notificationService.UpdateSettings(Settings))
            .Do(_ => StatusMessage = "Settings saved");

    private IObservable<Unit> ExecuteCleanupCache()
    {
        StatusMessage = "Cleaning up cache...";
        return TodoCacheService.CleanupCache()
            .Do(_ => StatusMessage = "Cache cleaned up");
    }

    private IObservable<Unit> ExecuteLoadSampleData()
    {
        var sampleTodos = CreateSampleTodos();

        return Observable.FromAsync(async () => await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Todos.Clear();
                foreach (var todo in sampleTodos)
                {
                    Todos.Add(new TodoItemViewModel(todo, _notificationService, RemoveTodoFromCollection));
                }
            }))
        .SelectMany(_ => SaveCurrentTodos())
        .Do(_ => StatusMessage = $"Loaded {sampleTodos.Count} sample todos");
    }

    private IObservable<Unit> ExecuteExit() => SaveApplicationState()
    .ObserveOn(RxApp.MainThreadScheduler)
    .Do(static _ => Application.Current?.Shutdown());

    private object GetSortKey(TodoItem todo) => Settings?.SortOrder switch
    {
        TodoSortOrder.CreatedDate => todo.CreatedAt,
        TodoSortOrder.DueDate => todo.DueDate ?? DateTimeOffset.MaxValue,
        TodoSortOrder.Priority => (int)todo.Priority,
        TodoSortOrder.Title => todo.Title,
        _ => todo.CreatedAt
    };
}
