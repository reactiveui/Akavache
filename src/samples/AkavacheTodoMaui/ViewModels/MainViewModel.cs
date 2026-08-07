// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AkavacheTodoMaui.Models;
using AkavacheTodoMaui.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AkavacheTodoMaui.ViewModels;

/// <summary>Main view model for the MAUI Todo application demonstrating ReactiveUI and Akavache integration.</summary>
[RequiresUnreferencedCode("ReactiveObject requires types to be preserved for reflection.")]
[RequiresDynamicCode("ReactiveObject requires types to be preserved for reflection.")]
public sealed partial class MainViewModel : ReactiveObject, IActivatableViewModel
{
    /// <summary>How long a burst of todo changes settles before the statistics are recalculated.</summary>
    private const int StatsThrottleMilliseconds = 300;

    /// <summary>How often the cache key counts shown on the dashboard are refreshed.</summary>
    private const int CacheInfoRefreshMinutes = 5;

    /// <summary>How many times a failed cache-information read is retried before giving up.</summary>
    private const int CacheInfoRetryCount = 3;

    /// <summary>How long todo changes settle before the collection is written back to the cache.</summary>
    private const int AutoSaveThrottleSeconds = 2;

    /// <summary>Due-date offset used by the "review documentation" sample todo.</summary>
    private const int SampleReviewDueHours = 2;

    /// <summary>Due-date offset used by the "test notifications" sample todo.</summary>
    private const int SampleNotificationTestDueMinutes = 30;

    /// <summary>Due-date offset used by the "write unit tests" sample todo.</summary>
    private const int SampleUnitTestDueDays = 3;

    /// <summary>The hour of the day the test-data command uses for its due date.</summary>
    private const int TestTodoDueHour = 14;

    /// <summary>The notification service used to schedule and deliver reminders.</summary>
    private readonly NotificationService _notificationService;

    /// <summary>OAPH exposing the aggregated loading state of all commands.</summary>
    private readonly ObservableAsPropertyHelper<bool> _isLoading;

    /// <summary>OAPH exposing the current todo statistics.</summary>
    private readonly ObservableAsPropertyHelper<TodoStats?> _todoStats;

    /// <summary>OAPH exposing the current cache information.</summary>
    private readonly ObservableAsPropertyHelper<CacheInfo?> _cacheInfo;

    /// <summary>Backing field for the reactive NewTodoTitle property.</summary>
    [Reactive]
    private string _newTodoTitle = string.Empty;

    /// <summary>Backing field for the reactive NewTodoDescription property.</summary>
    [Reactive]
    private string _newTodoDescription = string.Empty;

    /// <summary>Backing field for the reactive NewTodoTags property.</summary>
    [Reactive]
    private string _newTodoTags = string.Empty;

    /// <summary>Backing field for the reactive NewTodoDueDate property.</summary>
    [Reactive]
    private DateTime? _newTodoDueDate = TimeProvider.System.GetUtcNow().UtcDateTime;

    /// <summary>Backing field for the reactive NewTodoPriority property.</summary>
    [Reactive]
    private TodoPriority _newTodoPriority = TodoPriority.Medium;

    /// <summary>Backing field for the reactive Settings property.</summary>
    [Reactive]
    private AppSettings? _settings = new();

    /// <summary>Backing field for the reactive StatusMessage property.</summary>
    [Reactive]
    private string _statusMessage = "Ready";

    /// <summary>Backing field for the reactive NewTodoTime property.</summary>
    [Reactive]
    private string _newTodoTime = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="MainViewModel"/> class.</summary>
    /// <param name="notificationService">The notification service.</param>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
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
        TestDateCommand = ReactiveCommand.CreateFromObservable(ExecuteTestDate);

        IObservable<bool>[] commandExecutionStates =
        [
            AddTodoCommand.IsExecuting,
            RefreshCommand.IsExecuting,
            ClearCompletedCommand.IsExecuting,
            SaveSettingsCommand.IsExecuting,
            CleanupCacheCommand.IsExecuting,
            LoadSampleDataCommand.IsExecuting,
            TestDateCommand.IsExecuting
        ];

        _isLoading = commandExecutionStates
            .CombineLatestValuesAreAllFalse()
            .Select(static allIdle => !allIdle)
            .ToProperty(this, static x => x.IsLoading);

        // Enhanced statistics calculation that responds to individual todo property changes
        _todoStats = Signal.Merge(
            this.WhenAnyValue(static x => x.Todos.Count).Select(static _ => RxVoid.Default),
            Signal.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1)).Select(static _ => RxVoid.Default),
            this.WhenAnyValue(static x => x.TodoStats).Select(static _ => RxVoid.Default).Skip(1))
            .Throttle(TimeSpan.FromMilliseconds(StatsThrottleMilliseconds))
            .SelectMany(static _ => TodoCacheService.GetTodoStats())
            .Catch<TodoStats?, Exception>(static _ => Signal.Return<TodoStats?>(new()))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .ToProperty(this, static x => x.TodoStats);

        // Setup cache info with reduced frequency and better error handling
        _cacheInfo = Signal.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(CacheInfoRefreshMinutes))
            .SelectMany(static _ => TodoCacheService.GetCacheInfo())
            .Retry(CacheInfoRetryCount)
            .Catch(static (Exception ex) =>
            {
                System.Diagnostics.Debug.WriteLine($"Cache info failed: {ex}");
                return Signal.Return(new CacheInfo { UserAccountKeys = 0, LocalMachineKeys = 0, SecureKeys = 0, TotalKeys = 0, LastChecked = TimeProvider.System.GetUtcNow() });
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

    /// <summary>Gets the current todo statistics.</summary>
    public TodoStats? TodoStats => _todoStats.Value;

    /// <summary>Gets the current cache information.</summary>
    public CacheInfo? CacheInfo => _cacheInfo.Value;

    /// <summary>Gets the view model activator for lifecycle management.</summary>
    public ViewModelActivator Activator { get; }

    /// <summary>Gets the collection of todo items.</summary>
    public ObservableCollection<TodoItemViewModel> Todos { get; }

    /// <summary>Gets the collection of notification messages.</summary>
    public ObservableCollection<string> Notifications { get; }

    /// <summary>Gets the priority options for the Picker.</summary>
    public TodoPriority[] PriorityOptions { get; }

    /// <summary>Gets the command to add a new todo.</summary>
    public ReactiveCommand<RxVoid, RxVoid> AddTodoCommand { get; }

    /// <summary>Gets the command to refresh all data.</summary>
    public ReactiveCommand<RxVoid, RxVoid> RefreshCommand { get; }

    /// <summary>Gets the command to clear completed todos.</summary>
    public ReactiveCommand<RxVoid, RxVoid> ClearCompletedCommand { get; }

    /// <summary>Gets the command to save settings.</summary>
    public ReactiveCommand<RxVoid, RxVoid> SaveSettingsCommand { get; }

    /// <summary>Gets the command to cleanup cache.</summary>
    public ReactiveCommand<RxVoid, RxVoid> CleanupCacheCommand { get; }

    /// <summary>Gets the command to load sample data.</summary>
    public ReactiveCommand<RxVoid, RxVoid> LoadSampleDataCommand { get; }

    /// <summary>Gets the command to test date setting functionality.</summary>
    public ReactiveCommand<RxVoid, RxVoid> TestDateCommand { get; }

    /// <summary>Saves application state when shutting down.</summary>
    /// <returns>Observable unit.</returns>
    public IObservable<RxVoid> SaveApplicationState() => Signal.Merge(
            SaveCurrentTodos(),
            TodoCacheService.SaveSettings(Settings),
            TodoCacheService.SaveApplicationState());

    /// <summary>Creates a set of sample todo items used by the load-sample-data command.</summary>
    /// <returns>A new list of sample todos.</returns>
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

    /// <summary>Splits a comma-separated tag list into trimmed, non-empty tags.</summary>
    /// <param name="tagsString">The raw comma-separated text typed by the user.</param>
    /// <returns>The parsed tags, which is empty when nothing usable was entered.</returns>
    private static List<string> ParseTags(string tagsString)
    {
        List<string> tags = [];

        if (string.IsNullOrWhiteSpace(tagsString))
        {
            return tags;
        }

        foreach (var rawTag in tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var tag = rawTag.Trim();
            if (tag.Length > 0)
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    /// <summary>Wires up reactive subscriptions active while the view model is activated.</summary>
    /// <param name="disposables">The composite disposable used to tie subscriptions to the activation lifetime.</param>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private void SetupBindings(MultipleDisposable disposables)
    {
        // Dispose the property helpers when deactivated
        _ = _isLoading.DisposeWith(disposables);
        _ = _todoStats.DisposeWith(disposables);
        _ = _cacheInfo.DisposeWith(disposables);

        TrackTimeDependentProperties(disposables);
        TrackCompletionChanges(disposables);
        TrackNotifications(disposables);

        // Load initial data
        _ = LoadInitialData().Subscribe(
            static _ => { },
            ex => StatusMessage = $"Error loading data: {ex.Message}")
            .DisposeWith(disposables);

        // Auto-save when todos change and refresh statistics
        _ = this.WhenAnyValue(x => x.Todos.Count)
            .Skip(1) // Skip initial load
            .Throttle(TimeSpan.FromSeconds(AutoSaveThrottleSeconds))
            .SelectMany(_ => SaveCurrentTodos())
            .Subscribe(
                _ =>
                {
                    // Force statistics refresh when collection changes
                    this.RaisePropertyChanged();
                },
                ex => StatusMessage = $"Auto-save failed: {ex.Message}")
            .DisposeWith(disposables);

        // Handle command errors globally
        _ = Signal.Merge(
            AddTodoCommand.ThrownExceptions,
            RefreshCommand.ThrownExceptions,
            ClearCompletedCommand.ThrownExceptions,
            SaveSettingsCommand.ThrownExceptions,
            CleanupCacheCommand.ThrownExceptions,
            LoadSampleDataCommand.ThrownExceptions,
            TestDateCommand.ThrownExceptions)
            .Subscribe(ex =>
            {
                StatusMessage = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Command error: {ex}");
            })
            .DisposeWith(disposables);
    }

    /// <summary>Refreshes the time-dependent todo properties (IsOverdue, IsDueSoon) once a minute.</summary>
    /// <param name="disposables">The composite disposable used to tie subscriptions to the activation lifetime.</param>
    private void TrackTimeDependentProperties(MultipleDisposable disposables) =>
        _ = Signal.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ =>
            {
                // Trigger property notifications for all todos to refresh time-dependent UI
                foreach (var todoViewModel in Todos)
                {
                    todoViewModel.TodoItem.RaisePropertyChanged();
                    todoViewModel.TodoItem.RaisePropertyChanged();

                    // Also refresh the view model colors
                    todoViewModel.RaisePropertyChanged();
                    todoViewModel.RaisePropertyChanged();
                }

                // Force statistics refresh for time-based changes
                this.RaisePropertyChanged();
            })
            .DisposeWith(disposables);

    /// <summary>Saves a todo as soon as its completion status changes.</summary>
    /// <param name="disposables">The composite disposable used to tie subscriptions to the activation lifetime.</param>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private void TrackCompletionChanges(MultipleDisposable disposables) =>
        _ = Signal.FromEventPattern<System.Collections.Specialized.NotifyCollectionChangedEventHandler, System.Collections.Specialized.NotifyCollectionChangedEventArgs>(
                handler => Todos.CollectionChanged += handler,
                handler => Todos.CollectionChanged -= handler)
            .Subscribe(args =>
            {
                // When todos are added, subscribe to their property changes
                if (args.EventArgs.NewItems is null)
                {
                    return;
                }

                foreach (TodoItemViewModel todoVm in args.EventArgs.NewItems)
                {
                    // Subscribe to completion status changes with immediate response
                    _ = todoVm.WhenAnyValue(x => x.TodoItem.IsCompleted)
                        .Skip(1) // Skip initial value
                        .ObserveOn(RxSchedulers.MainThreadScheduler)
                        .Subscribe(isCompleted => SaveCompletionChange(todoVm, isCompleted))
                        .DisposeWith(disposables);
                }
            })
            .DisposeWith(disposables);

    /// <summary>Persists a completion toggle and reports the outcome in the status bar.</summary>
    /// <param name="todoVm">The todo whose completion status changed.</param>
    /// <param name="isCompleted">The new completion status.</param>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private void SaveCompletionChange(TodoItemViewModel todoVm, bool isCompleted) =>
        _ = SaveCurrentTodos().Subscribe(
            _ =>
            {
                // Force statistics refresh after save completes
                RefreshStatistics();

                StatusMessage = isCompleted
                    ? $"Completed: {todoVm.TodoItem.Title}"
                    : $"Reopened: {todoVm.TodoItem.Title}";
            },
            static ex => System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}"));

    /// <summary>Mirrors reminder notifications into the list shown in the UI, de-duplicated by todo.</summary>
    /// <param name="disposables">The composite disposable used to tie subscriptions to the activation lifetime.</param>
    private void TrackNotifications(MultipleDisposable disposables) =>
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

                    // Keep only the latest 10 notifications to prevent overflow
                    while (Notifications.Count > 10)
                    {
                        Notifications.RemoveAt(Notifications.Count - 1);
                    }
                }

                StatusMessage = baseMessage;
            })
            .DisposeWith(disposables);

    /// <summary>Loads todos and settings on startup.</summary>
    /// <returns>An observable that completes when loading finishes.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<RxVoid> LoadInitialData()
    {
        StatusMessage = "Loading data...";

        return LoadTodos().Merge(
            LoadSettings())
        .Finally(() => StatusMessage = "Ready");
    }

    /// <summary>Loads the list of todos from cache and populates the observable collection.</summary>
    /// <returns>An observable that completes when the load is done.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<RxVoid> LoadTodos() => TodoCacheService.GetAllTodos()
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
            .Select(static _ => RxVoid.Default);

    /// <summary>Removes a todo from the collection and updates the cache.</summary>
    /// <param name="todoViewModel">The todo view model to remove.</param>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private void RemoveTodoFromCollection(TodoItemViewModel todoViewModel)
    {
        _ = Todos.Remove(todoViewModel);
        StatusMessage = $"Deleted todo: {todoViewModel.TodoItem.Title}";

        // Save the updated collection and refresh statistics
        _ = SaveCurrentTodos().Subscribe();
        RefreshStatistics();
    }

    /// <summary>Loads the application settings from cache.</summary>
    /// <returns>An observable that completes when settings are loaded.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<RxVoid> LoadSettings() => TodoCacheService.GetSettings()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Do(settings => Settings = settings)
            .Select(static _ => RxVoid.Default);

    /// <summary>Saves the current todo collection to cache.</summary>
    /// <returns>An observable that completes when the save is done.</returns>
    [RequiresUnreferencedCode("ReactiveObject requires types to be preserved for reflection.")]
    [RequiresDynamicCode("ReactiveObject requires types to be preserved for reflection.")]
    private IObservable<RxVoid> SaveCurrentTodos()
    {
        List<TodoItem> todos = new(Todos.Count);
        foreach (var todoViewModel in Todos)
        {
            todos.Add(todoViewModel.TodoItem);
        }

        return TodoCacheService.SaveTodos(todos)
            .Do(_ => StatusMessage = $"Saved {todos.Count} todos");
    }

    /// <summary>Command handler that adds a new todo from the input form.</summary>
    /// <returns>An observable that completes when the todo has been added and saved.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<RxVoid> ExecuteAddTodo()
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(NewTodoTitle))
        {
            StatusMessage = "Title is required";
            return Signal.Return(RxVoid.Default);
        }

        // Debug what we have for date input
        System.Diagnostics.Debug.WriteLine($"NewTodoDueDate: {NewTodoDueDate}");
        System.Diagnostics.Debug.WriteLine($"NewTodoTime: '{NewTodoTime}'");

        DateTimeOffset? dueDate;
        try
        {
            dueDate = ParseNewTodoDueDate();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Invalid date/time format: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Date parsing error: {ex}");
            return Signal.Return(RxVoid.Default);
        }

        TodoItem newTodo = new()
        {
            Title = NewTodoTitle,
            Description = NewTodoDescription,
            DueDate = dueDate,
            Priority = NewTodoPriority,
            CreatedAt = TimeProvider.System.GetUtcNow(),
            Tags = ParseTags(NewTodoTags)
        };

        // Debug the created todo
        System.Diagnostics.Debug.WriteLine($"Created todo: {newTodo.Title}, Due: {newTodo.DueDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "No due date"}");

        TodoItemViewModel viewModel = new(newTodo, _notificationService, RemoveTodoFromCollection);

        return Signal.Start(
            () =>
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                Todos.Insert(0, viewModel);

                // Clear form
                NewTodoTitle = string.Empty;
                NewTodoDescription = string.Empty;
                NewTodoTags = string.Empty;
                NewTodoDueDate = null;
                NewTodoTime = string.Empty;
                NewTodoPriority = Settings?.DefaultPriority ?? TodoPriority.Medium;

                // Notify that DatePicker should reset
                this.RaisePropertyChanged();
            }),
            RxSchedulers.MainThreadScheduler)
        .SelectMany(_ => SaveCurrentTodos())
        .SelectMany(_ => _notificationService.ScheduleReminder(newTodo))
        .ObserveOn(RxSchedulers.MainThreadScheduler)
        .Do(_ =>
        {
            var dueSuffix = dueDate.HasValue
                ? $" (Due: {dueDate.Value.ToString("MMM dd, yyyy HH:mm", CultureInfo.InvariantCulture)})"
                : " (No due date)";
            StatusMessage = $"Added todo: {newTodo.Title}{dueSuffix}";

            // Force statistics refresh after adding
            this.RaisePropertyChanged();
        });
    }

    /// <summary>Builds the due date from the date and time typed into the new-todo form.</summary>
    /// <returns>The parsed due date, or null when no date was selected.</returns>
    private DateTimeOffset? ParseNewTodoDueDate()
    {
        if (!NewTodoDueDate.HasValue)
        {
            System.Diagnostics.Debug.WriteLine("No due date selected - NewTodoDueDate is null");
            return null;
        }

        var dueDay = DateOnly.FromDateTime(NewTodoDueDate.Value);
        var dueTimeOfDay = TimeOnly.MinValue;

        // Parse time if provided
        if (!string.IsNullOrWhiteSpace(NewTodoTime) && TimeOnly.TryParse(NewTodoTime, out var time))
        {
            dueTimeOfDay = time;
            System.Diagnostics.Debug.WriteLine($"Added time {time} to date");
        }

        var dueDate = new DateTimeOffset(dueDay.ToDateTime(dueTimeOfDay));

        // Debug logging to see what date we're setting
        System.Diagnostics.Debug.WriteLine($"Final due date: {dueDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
        return dueDate;
    }

    /// <summary>Command handler that refreshes all data from cache.</summary>
    /// <returns>An observable that completes when refresh is done.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<RxVoid> ExecuteRefresh()
    {
        StatusMessage = "Refreshing...";
        return LoadInitialData();
    }

    /// <summary>Command handler that removes all completed todos.</summary>
    /// <returns>An observable that completes when the clear operation is done.</returns>
    private IObservable<RxVoid> ExecuteClearCompleted() =>
        Signal.FromAsync(async () =>
        {
            List<TodoItemViewModel> completedTodos = [];
            foreach (var todoViewModel in Todos)
            {
                if (todoViewModel.TodoItem.IsCompleted)
                {
                    completedTodos.Add(todoViewModel);
                }
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var completedTodo in completedTodos)
                {
                    _ = Todos.Remove(completedTodo);
                }
            });

            StatusMessage = $"Removed {completedTodos.Count} completed todos";
            return RxVoid.Default;
        });

    /// <summary>Command handler that saves settings and updates notification state.</summary>
    /// <returns>An observable that completes when settings have been saved.</returns>
    private IObservable<RxVoid> ExecuteSaveSettings() => TodoCacheService.SaveSettings(Settings)
            .SelectMany(_ => _notificationService.UpdateSettings(Settings))
            .Do(_ => StatusMessage = "Settings saved");

    /// <summary>Command handler that vacuums the cache.</summary>
    /// <returns>An observable that completes when cleanup is done.</returns>
    private IObservable<RxVoid> ExecuteCleanupCache()
    {
        StatusMessage = "Cleaning up cache...";
        return TodoCacheService.CleanupCache()
            .Do(_ => StatusMessage = "Cache cleaned up");
    }

    /// <summary>Command handler that populates the app with sample data.</summary>
    /// <returns>An observable that completes when the sample data is saved.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<RxVoid> ExecuteLoadSampleData()
    {
        var sampleTodos = CreateSampleTodos();

        return Signal.FromAsync(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Todos.Clear();
                foreach (var todo in sampleTodos)
                {
                    Todos.Add(new(todo, _notificationService, RemoveTodoFromCollection));
                }
            });
            return RxVoid.Default;
        })
        .SelectMany(_ => SaveCurrentTodos())
        .Do(_ => StatusMessage = $"Loaded {sampleTodos.Count} sample todos");
    }

    /// <summary>Command handler that pre-fills the new todo form with test data.</summary>
    /// <returns>An observable that completes immediately.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<RxVoid> ExecuteTestDate()
    {
        // Create a test todo with a specific due date for verification
        var testDate = DateTime.Today.AddDays(1).AddHours(TestTodoDueHour);
        NewTodoTitle = "Test Todo with Due Date";
        NewTodoDescription = "This is a test to verify due dates are working";
        NewTodoTags = "test, verification, demo";
        NewTodoDueDate = testDate;
        NewTodoTime = "14:00";
        NewTodoPriority = TodoPriority.High;

        // Refresh UI to show the set values
        this.RaisePropertyChanged();
        this.RaisePropertyChanged();
        this.RaisePropertyChanged();
        this.RaisePropertyChanged();
        this.RaisePropertyChanged();
        this.RaisePropertyChanged();

        StatusMessage = $"Pre-filled form with test data - Due: {testDate:MMM dd, yyyy} at 2:00 PM";
        return Signal.Return(RxVoid.Default);
    }

    /// <summary>Orders two todos using the sort order currently configured in the settings.</summary>
    /// <param name="left">The todo that should come first when the result is negative.</param>
    /// <param name="right">The todo that should come first when the result is positive.</param>
    /// <returns>A negative number, zero, or a positive number describing the relative order.</returns>
    private int CompareBySortOrder(TodoItem left, TodoItem right) =>
        Comparer<object>.Default.Compare(GetSortKey(left), GetSortKey(right));

    /// <summary>Returns the sort key for a given todo based on the current sort order setting.</summary>
    /// <param name="todo">The todo to inspect.</param>
    /// <returns>The value used to order the todo.</returns>
    private object GetSortKey(TodoItem todo) => Settings?.SortOrder switch
    {
        TodoSortOrder.CreatedDate => todo.CreatedAt,
        TodoSortOrder.DueDate => todo.DueDate ?? DateTimeOffset.MaxValue,
        TodoSortOrder.Priority => (int)todo.Priority,
        TodoSortOrder.Title => todo.Title,
        _ => todo.CreatedAt
    };

    /// <summary>Forces an immediate refresh of the TodoStats.</summary>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private void RefreshStatistics()
    {
        // Simple immediate property change notification
        this.RaisePropertyChanged();

        // Log for debugging
        System.Diagnostics.Debug.WriteLine("Statistics refresh triggered");
    }
}
