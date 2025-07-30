// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using AkavacheTodoWpf.Models;
using AkavacheTodoWpf.Services;
using ReactiveUI;

namespace AkavacheTodoWpf.ViewModels;

/// <summary>
/// Main view model for the WPF Todo application demonstrating ReactiveUI and Akavache integration.
/// </summary>
public class MainViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly TodoCacheService _cacheService;
    private readonly NotificationService _notificationService;
    private readonly ObservableAsPropertyHelper<bool> _isLoading;
    private readonly ObservableAsPropertyHelper<TodoStats?> _todoStats;
    private readonly ObservableAsPropertyHelper<CacheInfo> _cacheInfo;
    private string _newTodoTitle = string.Empty;
    private string _newTodoDescription = string.Empty;
    private DateTime? _newTodoDueDate;
    private TodoPriority _newTodoPriority = TodoPriority.Medium;
    private AppSettings? _settings = new();
    private string _statusMessage = "Ready";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="notificationService">The notification service.</param>
    public MainViewModel(TodoCacheService cacheService, NotificationService notificationService)
    {
        _cacheService = cacheService;
        _notificationService = notificationService;

        // Initialize collections
        Todos = new ObservableCollection<TodoItemViewModel>();
        Notifications = new ObservableCollection<string>();
        PriorityOptions = Enum.GetValues<TodoPriority>();

        // Create commands
        AddTodoCommand = ReactiveCommand.CreateFromObservable(ExecuteAddTodo);
        RefreshCommand = ReactiveCommand.CreateFromObservable(ExecuteRefresh);
        ClearCompletedCommand = ReactiveCommand.CreateFromObservable(ExecuteClearCompleted);
        SaveSettingsCommand = ReactiveCommand.CreateFromObservable(ExecuteSaveSettings);
        CleanupCacheCommand = ReactiveCommand.CreateFromObservable(ExecuteCleanupCache);
        LoadSampleDataCommand = ReactiveCommand.CreateFromObservable(ExecuteLoadSampleData);
        ExitCommand = ReactiveCommand.CreateFromObservable(ExecuteExit);

        // Setup loading indicator
        var loadingCommands = new[] { AddTodoCommand, RefreshCommand, ClearCompletedCommand, SaveSettingsCommand };
        _isLoading = loadingCommands
            .Select(cmd => cmd.IsExecuting)
            .CombineLatest(executing => executing.Any(x => x))
            .ToProperty(this, x => x.IsLoading);

        // Setup todo statistics
        _todoStats = this.WhenAnyValue(x => x.Todos.Count)
            .Where(_ => Todos.Count > 0)
            .SelectMany(_ => TodoCacheService.GetTodoStats())
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.TodoStats, new TodoStats());

        // Setup cache info
        _cacheInfo = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(30))
            .SelectMany(_ => TodoCacheService.GetCacheInfo())
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.CacheInfo, new CacheInfo());

        // Setup activator for proper lifecycle management
        Activator = new ViewModelActivator();

        this.WhenActivated(disposables =>
        {
            // Load initial data
            LoadInitialData().Subscribe().DisposeWith(disposables);

            // Subscribe to notifications
            _notificationService.ReminderNotifications
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(todo =>
                {
                    var message = $"Reminder: {todo.Title}";
                    Notifications.Insert(0, message);
                    StatusMessage = message;
                })
                .DisposeWith(disposables);

            // Auto-save when todos change
            this.WhenAnyValue(x => x.Todos.Count)
                .Skip(1) // Skip initial load
                .Throttle(TimeSpan.FromSeconds(2))
                .SelectMany(_ => SaveCurrentTodos())
                .Subscribe()
                .DisposeWith(disposables);

            // Validation for add command
            var canAddTodo = this.WhenAnyValue(
                x => x.NewTodoTitle,
                title => !string.IsNullOrWhiteSpace(title));

            AddTodoCommand.CanExecute
                .CombineLatest(canAddTodo, (cmdCanExecute, validTitle) => cmdCanExecute && validTitle)
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

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
    /// Gets or sets the new todo title.
    /// </summary>
    public string NewTodoTitle
    {
        get => _newTodoTitle;
        set => this.RaiseAndSetIfChanged(ref _newTodoTitle, value);
    }

    /// <summary>
    /// Gets or sets the new todo description.
    /// </summary>
    public string NewTodoDescription
    {
        get => _newTodoDescription;
        set => this.RaiseAndSetIfChanged(ref _newTodoDescription, value);
    }

    /// <summary>
    /// Gets or sets the new todo due date.
    /// </summary>
    public DateTime? NewTodoDueDate
    {
        get => _newTodoDueDate;
        set => this.RaiseAndSetIfChanged(ref _newTodoDueDate, value);
    }

    /// <summary>
    /// Gets or sets the new todo priority.
    /// </summary>
    public TodoPriority NewTodoPriority
    {
        get => _newTodoPriority;
        set => this.RaiseAndSetIfChanged(ref _newTodoPriority, value);
    }

    /// <summary>
    /// Gets or sets the application settings.
    /// </summary>
    public AppSettings? Settings
    {
        get => _settings;
        set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets a value indicating whether any operation is loading.
    /// </summary>
    public bool IsLoading => _isLoading.Value;

    /// <summary>
    /// Gets the current todo statistics.
    /// </summary>
    public TodoStats? TodoStats => _todoStats.Value;

    /// <summary>
    /// Gets the current cache information.
    /// </summary>
    public CacheInfo CacheInfo => _cacheInfo.Value;

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

    private IObservable<Unit> LoadInitialData()
    {
        StatusMessage = "Loading data...";

        return Observable.Merge(
            LoadTodos(),
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
                    Todos.Add(new TodoItemViewModel(todo, _cacheService, _notificationService));
                }
            })
            .Select(_ => Unit.Default);

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
        var newTodo = new TodoItem
        {
            Title = NewTodoTitle,
            Description = NewTodoDescription,
            DueDate = NewTodoDueDate?.Date,
            Priority = NewTodoPriority,
            CreatedAt = DateTimeOffset.Now
        };

        var viewModel = new TodoItemViewModel(newTodo, _cacheService, _notificationService);

        return Observable.FromAsync(async () =>
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Todos.Insert(0, viewModel);

                // Clear form
                NewTodoTitle = string.Empty;
                NewTodoDescription = string.Empty;
                NewTodoDueDate = null;
                NewTodoPriority = Settings!.DefaultPriority;
            });
        })
        .SelectMany(_ => _notificationService.ScheduleReminder(newTodo))
        .Do(_ => StatusMessage = $"Added todo: {newTodo.Title}");
    }

    private IObservable<Unit> ExecuteRefresh()
    {
        StatusMessage = "Refreshing...";
        return LoadInitialData();
    }

    private IObservable<Unit> ExecuteClearCompleted() => Observable.FromAsync(async () =>
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

        return Observable.FromAsync(async () =>
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Todos.Clear();
                foreach (var todo in sampleTodos)
                {
                    Todos.Add(new TodoItemViewModel(todo, _cacheService, _notificationService));
                }
            });
        })
        .SelectMany(_ => SaveCurrentTodos())
        .Do(_ => StatusMessage = $"Loaded {sampleTodos.Count} sample todos");
    }

    private IObservable<Unit> ExecuteExit() => SaveApplicationState()
            .Do(_ => Application.Current.Shutdown());

    private object GetSortKey(TodoItem todo) => Settings?.SortOrder switch
    {
        TodoSortOrder.CreatedDate => todo.CreatedAt,
        TodoSortOrder.DueDate => todo.DueDate ?? DateTimeOffset.MaxValue,
        TodoSortOrder.Priority => (int)todo.Priority,
        TodoSortOrder.Title => todo.Title,
        _ => todo.CreatedAt
    };
}
