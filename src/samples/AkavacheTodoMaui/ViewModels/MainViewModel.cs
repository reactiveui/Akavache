// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using AkavacheTodoMaui.Models;
using AkavacheTodoMaui.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AkavacheTodoMaui.ViewModels;

/// <summary>
/// Main view model for the MAUI Todo application demonstrating ReactiveUI and Akavache integration.
/// </summary>
[RequiresUnreferencedCode("ReactiveObject requires types to be preserved for reflection.")]
[RequiresDynamicCode("ReactiveObject requires types to be preserved for reflection.")]
public partial class MainViewModel : ReactiveObject, IActivatableViewModel
{
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
    private DateTime? _newTodoDueDate = DateTime.Now;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
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

        // Initialize observable properties in constructor
        ReactiveCommand<Unit, Unit>[] loadingCommands = [AddTodoCommand,
            RefreshCommand,
            ClearCompletedCommand,
            SaveSettingsCommand,
            CleanupCacheCommand,
            LoadSampleDataCommand,
            TestDateCommand
        ];
        _isLoading = loadingCommands
            .Select(static cmd => cmd.IsExecuting)
            .CombineLatest(static executing => executing.Any(static x => x))
            .ToProperty(this, static x => x.IsLoading);

        // Enhanced statistics calculation that responds to individual todo property changes
        _todoStats = Observable.Merge(
            this.WhenAnyValue(static x => x.Todos.Count).Select(static _ => Unit.Default),
            Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1)).Select(static _ => Unit.Default),
            this.WhenAnyValue(static x => x.TodoStats).Select(static _ => Unit.Default).Skip(1))
            .Throttle(TimeSpan.FromMilliseconds(300))
            .SelectMany(static _ => TodoCacheService.GetTodoStats())
            .Catch(Observable.Return(new TodoStats()))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .ToProperty(this, static x => x.TodoStats);

        // Setup cache info with reduced frequency and better error handling
        _cacheInfo = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(5))
            .SelectMany(static _ => TodoCacheService.GetCacheInfo())
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
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .ToProperty(this, static x => x.CacheInfo);

        // Setup activator for proper lifecycle management
        Activator = new();

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
    public TodoStats? TodoStats => _todoStats?.Value;

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
    /// Gets the priority options for the Picker.
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
    /// Gets the command to test date setting functionality.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestDateCommand { get; }

    /// <summary>
    /// Saves application state when shutting down.
    /// </summary>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> SaveApplicationState() => Observable.Merge(
            SaveCurrentTodos(),
            TodoCacheService.SaveSettings(Settings),
            TodoCacheService.SaveApplicationState());

    /// <summary>Creates a set of sample todo items used by the load-sample-data command.</summary>
    /// <returns>A new list of sample todos.</returns>
    private static List<TodoItem> CreateSampleTodos()
    {
        var now = DateTimeOffset.Now;

        return
        [
            new()
            {
                Title = "Review Akavache Documentation",
                Description = "Go through the comprehensive Akavache documentation and examples",
                DueDate = now.AddHours(2),
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
                DueDate = now.AddMinutes(30),
                Priority = TodoPriority.Critical,
                Tags = ["testing", "notifications"]
            },
            new()
            {
                Title = "Write Unit Tests",
                Description = "Create comprehensive unit tests for cache service",
                DueDate = now.AddDays(3),
                Priority = TodoPriority.High,
                Tags = ["testing", "development"]
            },
            new()
            {
                Title = "Optimize Performance",
                Description = "Profile and optimize cache performance for large datasets",
                Priority = TodoPriority.Low,
                Tags = ["performance", "optimization"]
            }
        ];
    }

    /// <summary>Wires up reactive subscriptions active while the view model is activated.</summary>
    /// <param name="disposables">The composite disposable used to tie subscriptions to the activation lifetime.</param>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private void SetupBindings(CompositeDisposable disposables)
    {
        // Dispose the property helpers when deactivated
        _isLoading.DisposeWith(disposables);
        _todoStats.DisposeWith(disposables);
        _cacheInfo.DisposeWith(disposables);

        // Timer to refresh time-dependent properties (IsOverdue, IsDueSoon) every minute
        Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ =>
            {
                // Trigger property notifications for all todos to refresh time-dependent UI
                foreach (var todoViewModel in Todos)
                {
                    todoViewModel.TodoItem.RaisePropertyChanged(nameof(TodoItem.IsOverdue));
                    todoViewModel.TodoItem.RaisePropertyChanged(nameof(TodoItem.IsDueSoon));

                    // Also refresh the view model colors
                    todoViewModel.RaisePropertyChanged(nameof(TodoItemViewModel.BackgroundColor));
                    todoViewModel.RaisePropertyChanged(nameof(TodoItemViewModel.TextColor));
                }

                // Force statistics refresh for time-based changes
                this.RaisePropertyChanged(nameof(TodoStats));
            })
            .DisposeWith(disposables);

        // Subscribe to collection changes to track todo completion status changes
        Observable.FromEventPattern<System.Collections.Specialized.NotifyCollectionChangedEventHandler, System.Collections.Specialized.NotifyCollectionChangedEventArgs>(
                handler => Todos.CollectionChanged += handler,
                handler => Todos.CollectionChanged -= handler)
            .Subscribe(args =>
            {
                // When todos are added, subscribe to their property changes
                if (args.EventArgs.NewItems == null)
                {
                    return;
                }

                foreach (TodoItemViewModel todoVm in args.EventArgs.NewItems)
                {
                    // Subscribe to completion status changes with immediate response
                    todoVm.WhenAnyValue(x => x.TodoItem.IsCompleted)
                        .Skip(1) // Skip initial value
                        .ObserveOn(RxSchedulers.MainThreadScheduler)
                        .Subscribe(isCompleted =>
                        {
                            // Save the updated todo to cache immediately
                            SaveCurrentTodos().Subscribe(
                                _ =>
                                {
                                    // Force statistics refresh after save completes
                                    RefreshStatistics();

                                    StatusMessage = isCompleted ?
                                        $"Completed: {todoVm.TodoItem.Title}" :
                                        $"Reopened: {todoVm.TodoItem.Title}";
                                },
                                ex => System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}"));
                        })
                        .DisposeWith(disposables);
                }
            })
            .DisposeWith(disposables);

        // Load initial data
        LoadInitialData().Subscribe(
            _ => { },
            ex => StatusMessage = $"Error loading data: {ex.Message}")
            .DisposeWith(disposables);

        // Subscribe to notifications with timestamp-based deduplication
        _notificationService.ReminderNotifications
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(todo =>
            {
                var timestamp = DateTimeOffset.Now.ToString("HH:mm:ss");
                var baseMessage = $"Reminder: {todo.Title}";
                var messageWithTimestamp = baseMessage + " [" + timestamp + "]";

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

        // Auto-save when todos change and refresh statistics
        this.WhenAnyValue(x => x.Todos.Count)
            .Skip(1) // Skip initial load
            .Throttle(TimeSpan.FromSeconds(2))
            .SelectMany(_ => SaveCurrentTodos())
            .Subscribe(
                _ =>
                {
                    // Force statistics refresh when collection changes
                    this.RaisePropertyChanged(nameof(TodoStats));
                },
                ex => StatusMessage = $"Auto-save failed: {ex.Message}")
            .DisposeWith(disposables);

        // Handle command errors globally
        Observable.Merge(
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

    /// <summary>Loads todos and settings on startup.</summary>
    /// <returns>An observable that completes when loading finishes.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<Unit> LoadInitialData()
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
    private IObservable<Unit> LoadTodos() => TodoCacheService.GetAllTodos()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
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
                    TodoItemViewModel todoViewModel = new(todo, _notificationService, RemoveTodoFromCollection);
                    Todos.Add(todoViewModel);
                }
            })
            .Select(_ => Unit.Default);

    /// <summary>
    /// Removes a todo from the collection and updates the cache.
    /// </summary>
    /// <param name="todoViewModel">The todo view model to remove.</param>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private void RemoveTodoFromCollection(TodoItemViewModel todoViewModel)
    {
        Todos.Remove(todoViewModel);
        StatusMessage = $"Deleted todo: {todoViewModel.TodoItem.Title}";

        // Save the updated collection and refresh statistics
        SaveCurrentTodos().Subscribe();
        RefreshStatistics();
    }

    /// <summary>Loads the application settings from cache.</summary>
    /// <returns>An observable that completes when settings are loaded.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<Unit> LoadSettings() => TodoCacheService.GetSettings()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Do(settings => Settings = settings)
            .Select(_ => Unit.Default);

    /// <summary>Saves the current todo collection to cache.</summary>
    /// <returns>An observable that completes when the save is done.</returns>
    [RequiresUnreferencedCode("ReactiveObject requires types to be preserved for reflection.")]
    [RequiresDynamicCode("ReactiveObject requires types to be preserved for reflection.")]
    private IObservable<Unit> SaveCurrentTodos()
    {
        var todos = Todos.Select(vm => vm.TodoItem).ToList();
        return TodoCacheService.SaveTodos(todos)
            .Do(_ => StatusMessage = $"Saved {todos.Count} todos");
    }

    /// <summary>Command handler that adds a new todo from the input form.</summary>
    /// <returns>An observable that completes when the todo has been added and saved.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<Unit> ExecuteAddTodo()
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(NewTodoTitle))
        {
            StatusMessage = "Title is required";
            return Observable.Return(Unit.Default);
        }

        // Parse the date and time more robustly
        DateTimeOffset? dueDate = null;

        // Debug what we have for date input
        System.Diagnostics.Debug.WriteLine($"NewTodoDueDate: {NewTodoDueDate}");
        System.Diagnostics.Debug.WriteLine($"NewTodoTime: '{NewTodoTime}'");

        if (NewTodoDueDate.HasValue)
        {
            try
            {
                var date = NewTodoDueDate.Value.Date;

                // Parse time if provided
                if (!string.IsNullOrWhiteSpace(NewTodoTime) && TimeSpan.TryParse(NewTodoTime, out var time))
                {
                    date = date.Add(time);
                    System.Diagnostics.Debug.WriteLine($"Added time {time} to date, result: {date}");
                }

                dueDate = new DateTimeOffset(date);

                // Debug logging to see what date we're setting
                System.Diagnostics.Debug.WriteLine($"Final due date: {dueDate.Value:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Invalid date/time format: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Date parsing error: {ex}");
                return Observable.Return(Unit.Default);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("No due date selected - NewTodoDueDate is null");
        }

        TodoItem newTodo = new()
        {
            Title = NewTodoTitle,
            Description = NewTodoDescription,
            DueDate = dueDate,
            Priority = NewTodoPriority,
            CreatedAt = DateTimeOffset.Now
        };

        // Parse tags if provided
        if (!string.IsNullOrWhiteSpace(NewTodoTags))
        {
            newTodo.Tags = [.. NewTodoTags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(tag => tag.Trim())
                                     .Where(tag => !string.IsNullOrEmpty(tag))];
        }

        // Debug the created todo
        System.Diagnostics.Debug.WriteLine($"Created todo: {newTodo.Title}, Due: {newTodo.DueDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "No due date"}");

        TodoItemViewModel viewModel = new(newTodo, _notificationService, RemoveTodoFromCollection);

        return Observable.Start(
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
                this.RaisePropertyChanged(nameof(NewTodoDueDate));
            }),
            RxSchedulers.MainThreadScheduler)
        .SelectMany(_ => SaveCurrentTodos())
        .SelectMany(_ => _notificationService.ScheduleReminder(newTodo))
        .ObserveOn(RxSchedulers.MainThreadScheduler)
        .Do(_ =>
        {
            StatusMessage = $"Added todo: {newTodo.Title}" + (dueDate.HasValue ? $" (Due: {dueDate.Value:MMM dd, yyyy HH:mm})" : " (No due date)");

            // Force statistics refresh after adding
            this.RaisePropertyChanged(nameof(TodoStats));
        });
    }

    /// <summary>Command handler that refreshes all data from cache.</summary>
    /// <returns>An observable that completes when refresh is done.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<Unit> ExecuteRefresh()
    {
        StatusMessage = "Refreshing...";
        return LoadInitialData();
    }

    /// <summary>Command handler that removes all completed todos.</summary>
    /// <returns>An observable that completes when the clear operation is done.</returns>
    private IObservable<Unit> ExecuteClearCompleted() =>
        Observable.FromAsync(async () =>
        {
            var completedTodos = Todos.Where(vm => vm.TodoItem.IsCompleted).ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var completedTodo in completedTodos)
                {
                    Todos.Remove(completedTodo);
                }
            });

            StatusMessage = $"Removed {completedTodos.Count} completed todos";
        });

    /// <summary>Command handler that saves settings and updates notification state.</summary>
    /// <returns>An observable that completes when settings have been saved.</returns>
    private IObservable<Unit> ExecuteSaveSettings() => TodoCacheService.SaveSettings(Settings)
            .SelectMany(_ => _notificationService.UpdateSettings(Settings))
            .Do(_ => StatusMessage = "Settings saved");

    /// <summary>Command handler that vacuums the cache.</summary>
    /// <returns>An observable that completes when cleanup is done.</returns>
    private IObservable<Unit> ExecuteCleanupCache()
    {
        StatusMessage = "Cleaning up cache...";
        return TodoCacheService.CleanupCache()
            .Do(_ => StatusMessage = "Cache cleaned up");
    }

    /// <summary>Command handler that populates the app with sample data.</summary>
    /// <returns>An observable that completes when the sample data is saved.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<Unit> ExecuteLoadSampleData()
    {
        var sampleTodos = CreateSampleTodos();

        return Observable.FromAsync(async () => await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Todos.Clear();
                foreach (var todo in sampleTodos)
                {
                    TodoItemViewModel todoViewModel = new(todo, _notificationService, RemoveTodoFromCollection);
                    Todos.Add(todoViewModel);
                }
            }))
        .SelectMany(_ => SaveCurrentTodos())
        .Do(_ => StatusMessage = $"Loaded {sampleTodos.Count} sample todos");
    }

    /// <summary>Command handler that pre-fills the new todo form with test data.</summary>
    /// <returns>An observable that completes immediately.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private IObservable<Unit> ExecuteTestDate()
    {
        // Create a test todo with a specific due date for verification
        var testDate = DateTime.Today.AddDays(1).AddHours(14); // Tomorrow at 2 PM
        NewTodoTitle = "Test Todo with Due Date";
        NewTodoDescription = "This is a test to verify due dates are working";
        NewTodoTags = "test, verification, demo";
        NewTodoDueDate = testDate;
        NewTodoTime = "14:00";
        NewTodoPriority = TodoPriority.High;

        // Refresh UI to show the set values
        this.RaisePropertyChanged(nameof(NewTodoTitle));
        this.RaisePropertyChanged(nameof(NewTodoDescription));
        this.RaisePropertyChanged(nameof(NewTodoTags));
        this.RaisePropertyChanged(nameof(NewTodoDueDate));
        this.RaisePropertyChanged(nameof(NewTodoTime));
        this.RaisePropertyChanged(nameof(NewTodoPriority));

        StatusMessage = $"Pre-filled form with test data - Due: {testDate:MMM dd, yyyy} at 2:00 PM";
        return Observable.Return(Unit.Default);
    }

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

    /// <summary>
    /// Forces an immediate refresh of the TodoStats.
    /// </summary>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    private void RefreshStatistics()
    {
        // Simple immediate property change notification
        this.RaisePropertyChanged(nameof(TodoStats));

        // Log for debugging
        System.Diagnostics.Debug.WriteLine("Statistics refresh triggered");
    }
}
