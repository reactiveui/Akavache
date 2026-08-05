// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
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

/// <summary>Main view model for the WPF Todo application demonstrating ReactiveUI and Akavache integration.</summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public sealed partial class MainViewModel : ReactiveObject, IActivatableViewModel
{
    /// <summary>How long a burst of todo changes settles before the statistics are recalculated.</summary>
    private const int StatsThrottleMilliseconds = 500;

    /// <summary>How often the cache key counts shown on the dashboard are refreshed.</summary>
    private const int CacheInfoRefreshMinutes = 5;

    /// <summary>How many times a failed cache-information read is retried before giving up.</summary>
    private const int CacheInfoRetryCount = 3;

    /// <summary>How long todo changes settle before the collection is written back to the cache.</summary>
    private const int AutoSaveThrottleSeconds = 2;

    /// <summary>The maximum number of reminder messages kept in the notification list.</summary>
    private const int MaxVisibleNotifications = 10;

    /// <summary>Due-date offset used by the "review documentation" sample todo.</summary>
    private const int SampleReviewDueHours = 2;

    /// <summary>Due-date offset used by the "test notifications" sample todo.</summary>
    private const int SampleNotificationTestDueMinutes = 30;

    /// <summary>Due-date offset used by the "write unit tests" sample todo.</summary>
    private const int SampleUnitTestDueDays = 3;

    /// <summary>The notification service used to surface reminders.</summary>
    private readonly NotificationService _notificationService;

    /// <summary>Backing field for <see cref="IsLoading"/>.</summary>
    private readonly ObservableAsPropertyHelper<bool> _isLoading;

    /// <summary>Backing field for <see cref="TodoStats"/>.</summary>
    private readonly ObservableAsPropertyHelper<TodoStats?> _todoStats;

    /// <summary>Backing field for <see cref="CacheInfo"/>.</summary>
    private readonly ObservableAsPropertyHelper<CacheInfo?> _cacheInfo;

    /// <summary>Backing field for the <see cref="NewTodoTitle"/> input.</summary>
    [Reactive]
    private string _newTodoTitle = string.Empty;

    /// <summary>Backing field for the <see cref="NewTodoDescription"/> input.</summary>
    [Reactive]
    private string _newTodoDescription = string.Empty;

    /// <summary>Backing field for the <see cref="NewTodoDueDate"/> input.</summary>
    [Reactive]
    private DateTime? _newTodoDueDate;

    /// <summary>Backing field for the <see cref="NewTodoPriority"/> input.</summary>
    [Reactive]
    private TodoPriority _newTodoPriority = TodoPriority.Medium;

    /// <summary>Backing field for the cached <see cref="Settings"/>.</summary>
    [Reactive]
    private AppSettings? _settings = new();

    /// <summary>Backing field for the <see cref="StatusMessage"/> message.</summary>
    [Reactive]
    private string _statusMessage = "Ready";

    /// <summary>Backing field for the <see cref="NewTodoTime"/> input.</summary>
    [Reactive]
    private string _newTodoTime = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="MainViewModel"/> class.</summary>
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

        IObservable<bool>[] commandExecutionStates =
        [
            AddTodoCommand.IsExecuting,
            RefreshCommand.IsExecuting,
            ClearCompletedCommand.IsExecuting,
            SaveSettingsCommand.IsExecuting,
            CleanupCacheCommand.IsExecuting,
            LoadSampleDataCommand.IsExecuting
        ];

        _isLoading = commandExecutionStates
            .CombineLatest(AnyExecuting)
            .ToProperty(this, static x => x.IsLoading);

        // Enhanced statistics calculation that responds to individual todo property changes
        _todoStats = Observable.Merge(
            this.WhenAnyValue(static x => x.Todos.Count).Select(static _ => Unit.Default),
            Todos.ObserveCollectionChanges().Select(static _ => Unit.Default),
            this.WhenAnyValue(static x => x.TodoStats).Select(static _ => Unit.Default).Skip(1))
        .Throttle(TimeSpan.FromMilliseconds(StatsThrottleMilliseconds))
        .SelectMany(static _ => TodoCacheService.GetTodoStats())
        .Catch(Observable.Return(new TodoStats()))
        .ObserveOn(RxSchedulers.MainThreadScheduler)
        .ToProperty(this, static x => x.TodoStats);

        // Setup cache info with reduced frequency and better error handling
        _cacheInfo = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(CacheInfoRefreshMinutes))
            .SelectMany(static _ => TodoCacheService.GetCacheInfo()) // Remove unnecessary cache_test insertion
            .Retry(CacheInfoRetryCount)
            .Catch(static (Exception ex) =>
            {
                System.Diagnostics.Debug.WriteLine($"Cache info failed: {ex}");
                return Observable.Return(new CacheInfo { UserAccountKeys = 0, LocalMachineKeys = 0, SecureKeys = 0, TotalKeys = 0, LastChecked = TimeProvider.System.GetUtcNow() });
            })
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .ToProperty(this, static x => x.CacheInfo);

        // Setup activator for proper lifecycle management
        Activator = new();

        this.WhenActivated(SetupBindings);

        // Manually activate immediately to ensure initial data loading
        _ = Activator.Activate();
    }

    /// <summary>Gets a value indicating whether any operation is loading.</summary>
    public bool IsLoading => _isLoading.Value;

    /// <summary>Gets the current statistics.</summary>
    public TodoStats? TodoStats => _todoStats.Value;

    /// <summary>Gets the current cache information.</summary>
    public CacheInfo? CacheInfo => _cacheInfo.Value;

    /// <summary>Gets the view model activator for lifecycle management.</summary>
    public ViewModelActivator Activator { get; }

    /// <summary>Gets the collection of todo items.</summary>
    public ObservableCollection<TodoItemViewModel> Todos { get; }

    /// <summary>Gets the collection of notification messages.</summary>
    public ObservableCollection<string> Notifications { get; }

    /// <summary>Gets the priority options for the ComboBox.</summary>
    public TodoPriority[] PriorityOptions { get; }

    /// <summary>Gets the command to add a new todo.</summary>
    public ReactiveCommand<Unit, Unit> AddTodoCommand { get; }

    /// <summary>Gets the command to refresh all data.</summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    /// <summary>Gets the command to clear completed todos.</summary>
    public ReactiveCommand<Unit, Unit> ClearCompletedCommand { get; }

    /// <summary>Gets the command to save settings.</summary>
    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }

    /// <summary>Gets the command to cleanup cache.</summary>
    public ReactiveCommand<Unit, Unit> CleanupCacheCommand { get; }

    /// <summary>Gets the command to load sample data.</summary>
    public ReactiveCommand<Unit, Unit> LoadSampleDataCommand { get; }

    /// <summary>Gets the command to exit the application.</summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    /// <summary>Saves the current application state (todos, settings, and internal cache state) to the cache.</summary>
    /// <returns>An observable that signals when the save operation is complete.</returns>
    public IObservable<Unit> SaveApplicationState() => Observable.Merge(
            SaveCurrentTodos(),
            TodoCacheService.SaveSettings(Settings),
            TodoCacheService.SaveApplicationState());

    /// <summary>Creates a small set of demonstration todos.</summary>
    /// <returns>A list of sample todo items.</returns>
    private static List<TodoItem> CreateSampleTodos()
    {
        var now = TimeProvider.System.GetLocalNow();

        return
        [
            new()
            {
                Title = "Review Akavache Documentation",
                Description = "Go through the comprehensive Akavache documentation and examples",
                DueDate = now.AddHours(SampleReviewDueHours),
                Priority = TodoPriority.High,
                Tags = ["documentation", "akavache"]
            },
            new()
            {
                Title = "Implement Cache Expiration",
                Description = "Add proper cache expiration for temporary data",
                DueDate = now.AddDays(1),
                Priority = TodoPriority.Medium,
                Tags = ["development", "caching"]
            },
            new()
            {
                Title = "Test Notification System",
                Description = "Verify that notifications work correctly for due dates",
                DueDate = now.AddMinutes(SampleNotificationTestDueMinutes),
                Priority = TodoPriority.Critical,
                Tags = ["testing", "notifications"]
            },
            new()
            {
                Title = "Write Unit Tests",
                Description = "Create comprehensive unit tests for cache service",
                DueDate = now.AddDays(SampleUnitTestDueDays),
                Priority = TodoPriority.High,
                Tags = ["testing", "development"]
            },
            new() { Title = "Optimize Performance", Description = "Profile and optimize cache performance for large datasets", Priority = TodoPriority.Low, Tags = ["performance", "optimization"] }
        ];
    }

    /// <summary>Reports whether any of the tracked commands is currently executing.</summary>
    /// <param name="executionStates">The latest execution state of every tracked command.</param>
    /// <returns>True when at least one command is running; otherwise, false.</returns>
    private static bool AnyExecuting(IList<bool> executionStates)
    {
        foreach (var executing in executionStates)
        {
            if (executing)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Wires up reactive subscriptions when the view model is activated.</summary>
    /// <param name="disposables">The composite disposable to add subscriptions to.</param>
    private void SetupBindings(CompositeDisposable disposables)
    {
        // Dispose the property helpers when deactivated
        _ = _isLoading.DisposeWith(disposables);
        _ = _todoStats.DisposeWith(disposables);
        _ = _cacheInfo.DisposeWith(disposables);

        TrackStatistics(disposables);
        TrackNotifications(disposables);

        // Load initial data
        _ = LoadInitialData().Subscribe(
            static _ => { },
            ex => StatusMessage = $"Error loading data: {ex.Message}")
            .DisposeWith(disposables);

        // Auto-save when todos change
        _ = this.WhenAnyValue(x => x.Todos.Count)
            .Skip(1) // Skip initial load
            .Throttle(TimeSpan.FromSeconds(AutoSaveThrottleSeconds))
            .SelectMany(_ => SaveCurrentTodos())
            .Subscribe(
                static _ => { },
                ex => StatusMessage = $"Auto-save failed: {ex.Message}")
            .DisposeWith(disposables);

        // Handle command errors globally
        _ = Observable.Merge(
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

    /// <summary>Keeps the statistics in step with edits to individual todos and with the passage of time.</summary>
    /// <param name="disposables">The composite disposable to add subscriptions to.</param>
    private void TrackStatistics(CompositeDisposable disposables)
    {
        // Subscribe to individual todo property changes for statistics updates
        _ = Todos.ToObservableChangeSet()
            .ToCollection()
            .SelectMany(static x => x)
            .Where(static todoVm => todoVm.TodoItem is not null)
            .Select(static todoVm =>
            todoVm.TodoItem.WhenAnyValue(
                static x => x.IsCompleted,
                static x => x.DueDate,
                static x => x.Priority,
                static x => x.CreatedAt))
        .Throttle(TimeSpan.FromMilliseconds(StatsThrottleMilliseconds))
        .SelectMany(static _ => TodoCacheService.GetTodoStats())
        .ObserveOn(RxSchedulers.MainThreadScheduler)
        .Subscribe(_ =>
            this.RaisePropertyChanged())
        .DisposeWith(disposables);

        // Simple statistics refresh on todo property changes
        _ = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
            .Where(_ => Todos.Count > 0) // Only if we have todos
            .SelectMany(static _ => TodoCacheService.GetTodoStats())
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ =>
                this.RaisePropertyChanged())
            .DisposeWith(disposables);

        // Timer to refresh time-dependent properties (IsOverdue, IsDueSoon) every minute
        _ = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ =>
            {
                // Trigger property notifications for all todos to refresh time-dependent UI
                foreach (var todoViewModel in Todos)
                {
                    todoViewModel.TodoItem.RaisePropertyChanged();
                    todoViewModel.TodoItem.RaisePropertyChanged();
                }

                // Also refresh statistics
                this.RaisePropertyChanged();
            })
            .DisposeWith(disposables);
    }

    /// <summary>Mirrors reminder notifications into the list shown in the UI, de-duplicated by todo.</summary>
    /// <param name="disposables">The composite disposable to add subscriptions to.</param>
    private void TrackNotifications(CompositeDisposable disposables) =>
        _ = _notificationService.ReminderNotifications
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(todo =>
            {
                var timestamp = TimeProvider.System.GetLocalNow().ToString("HH:mm:ss");
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

                    // Keep only the most recent notifications to prevent overflow
                    while (Notifications.Count > MaxVisibleNotifications)
                    {
                        Notifications.RemoveAt(Notifications.Count - 1);
                    }
                }

                StatusMessage = baseMessage;
            })
            .DisposeWith(disposables);

    /// <summary>Loads todos and settings from the cache when the view model starts.</summary>
    /// <returns>An observable that signals when initial data loading is complete.</returns>
    private IObservable<Unit> LoadInitialData()
    {
        StatusMessage = "Loading data...";

        return LoadTodos().Merge(
            LoadSettings())
        .Finally(() => StatusMessage = "Ready");
    }

    /// <summary>Loads the todo collection from the cache.</summary>
    /// <returns>An observable that signals when the todo collection is loaded.</returns>
    private IObservable<Unit> LoadTodos() => TodoCacheService.GetAllTodos()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Do(todos =>
            {
                if (todos is null || todos.Count == 0)
                {
                    StatusMessage = "No todos found. You can add some!";
                    return;
                }

                List<TodoItem> sortedTodos = [.. todos];
                sortedTodos.Sort(CompareBySortOrder);

                Todos.Clear();
                foreach (var todo in sortedTodos)
                {
                    Todos.Add(new(todo, _notificationService, RemoveTodoFromCollection));
                }
            })
            .Select(static _ => Unit.Default);

    /// <summary>Removes a todo from the collection and updates the cache.</summary>
    /// <param name="todoViewModel">The todo view model to remove.</param>
    private void RemoveTodoFromCollection(TodoItemViewModel todoViewModel)
    {
        _ = Todos.Remove(todoViewModel);
        StatusMessage = $"Deleted todo: {todoViewModel.TodoItem.Title}";

        // Save the updated collection
        _ = SaveCurrentTodos().Subscribe();
    }

    /// <summary>Loads the application settings from the cache.</summary>
    /// <returns>An observable that signals when settings are loaded.</returns>
    private IObservable<Unit> LoadSettings() => TodoCacheService.GetSettings()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Do(settings => Settings = settings)
            .Select(static _ => Unit.Default);

    /// <summary>Persists the current todo collection to the cache.</summary>
    /// <returns>An observable that signals when the todos are saved.</returns>
    private IObservable<Unit> SaveCurrentTodos()
    {
        List<TodoItem> todos = new(Todos.Count);
        foreach (var todoViewModel in Todos)
        {
            todos.Add(todoViewModel.TodoItem);
        }

        return TodoCacheService.SaveTodos(todos)
            .Do(_ => StatusMessage = $"Saved {todos.Count} todos");
    }

    /// <summary>Executes the add-todo command.</summary>
    /// <returns>An observable that signals when the todo has been added.</returns>
    private IObservable<Unit> ExecuteAddTodo()
    {
        // Parse the date and time
        DateTime? dueDateTime = null;
        if (NewTodoDueDate.HasValue)
        {
            var dueDay = DateOnly.FromDateTime(NewTodoDueDate.Value);
            var dueTimeOfDay = !string.IsNullOrWhiteSpace(NewTodoTime) && TimeOnly.TryParse(NewTodoTime, out var time)
                ? time
                : TimeOnly.MinValue;

            dueDateTime = dueDay.ToDateTime(dueTimeOfDay);
        }

        TodoItem newTodo = new()
        {
            Title = NewTodoTitle,
            Description = NewTodoDescription,
            DueDate = dueDateTime.HasValue ? new DateTimeOffset(dueDateTime.Value) : null,
            Priority = NewTodoPriority,
            CreatedAt = TimeProvider.System.GetUtcNow()
        };

        TodoItemViewModel viewModel = new(newTodo, _notificationService, RemoveTodoFromCollection);

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
            RxSchedulers.MainThreadScheduler)
        .SelectMany(_ => _notificationService.ScheduleReminder(newTodo))
        .ObserveOn(RxSchedulers.MainThreadScheduler)
        .Do(_ => StatusMessage = $"Added todo: {newTodo.Title}");
    }

    /// <summary>Executes the refresh command.</summary>
    /// <returns>An observable that signals when the refresh is complete.</returns>
    private IObservable<Unit> ExecuteRefresh()
    {
        StatusMessage = "Refreshing...";
        return LoadInitialData();
    }

    /// <summary>Executes the clear-completed command.</summary>
    /// <returns>An observable that signals when completed todos are removed.</returns>
    private IObservable<Unit> ExecuteClearCompleted() =>
        Observable.FromAsync(async () =>
        {
            List<TodoItemViewModel> completedTodos = [];
            foreach (var todoViewModel in Todos)
            {
                if (todoViewModel.TodoItem.IsCompleted)
                {
                    completedTodos.Add(todoViewModel);
                }
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var completedTodo in completedTodos)
                {
                    _ = Todos.Remove(completedTodo);
                }
            });

            StatusMessage = $"Removed {completedTodos.Count} completed todos";
        });

    /// <summary>Executes the save-settings command.</summary>
    /// <returns>An observable that signals when settings are saved.</returns>
    private IObservable<Unit> ExecuteSaveSettings() => TodoCacheService.SaveSettings(Settings)
            .SelectMany(_ => _notificationService.UpdateSettings(Settings))
            .Do(_ => StatusMessage = "Settings saved");

    /// <summary>Executes the cleanup-cache command.</summary>
    /// <returns>An observable that signals when the cache cleanup is complete.</returns>
    private IObservable<Unit> ExecuteCleanupCache()
    {
        StatusMessage = "Cleaning up cache...";
        return TodoCacheService.CleanupCache()
            .Do(_ => StatusMessage = "Cache cleaned up");
    }

    /// <summary>Executes the load-sample-data command.</summary>
    /// <returns>An observable that signals when sample data is loaded.</returns>
    private IObservable<Unit> ExecuteLoadSampleData()
    {
        var sampleTodos = CreateSampleTodos();

        return Observable.FromAsync(async () => await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Todos.Clear();
                foreach (var todo in sampleTodos)
                {
                    Todos.Add(new(todo, _notificationService, RemoveTodoFromCollection));
                }
            }))
        .SelectMany(_ => SaveCurrentTodos())
        .Do(_ => StatusMessage = $"Loaded {sampleTodos.Count} sample todos");
    }

    /// <summary>Executes the exit command.</summary>
    /// <returns>An observable that signals when the application is exiting.</returns>
    private IObservable<Unit> ExecuteExit() => SaveApplicationState()
    .ObserveOn(RxSchedulers.MainThreadScheduler)
    .Do(static _ => Application.Current?.Shutdown());

    /// <summary>Orders two todos using the sort order currently configured in the settings.</summary>
    /// <param name="left">The todo that should come first when the result is negative.</param>
    /// <param name="right">The todo that should come first when the result is positive.</param>
    /// <returns>A negative number, zero, or a positive number describing the relative order.</returns>
    private int CompareBySortOrder(TodoItem left, TodoItem right) =>
        Comparer<object>.Default.Compare(GetSortKey(left), GetSortKey(right));

    /// <summary>Returns a comparable key used to sort todo items according to the current sort order settings.</summary>
    /// <param name="todo">The todo item to extract the sort key from.</param>
    /// <returns>The object used as a sort key.</returns>
    private object GetSortKey(TodoItem todo) => Settings?.SortOrder switch
    {
        TodoSortOrder.CreatedDate => todo.CreatedAt,
        TodoSortOrder.DueDate => todo.DueDate ?? DateTimeOffset.MaxValue,
        TodoSortOrder.Priority => (int)todo.Priority,
        TodoSortOrder.Title => todo.Title,
        _ => todo.CreatedAt
    };
}
