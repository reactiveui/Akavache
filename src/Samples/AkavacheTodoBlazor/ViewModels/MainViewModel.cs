// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AkavacheTodoBlazor.Models;
using AkavacheTodoBlazor.Services;
using ReactiveUI;

namespace AkavacheTodoBlazor.ViewModels;

/// <summary>
/// Main view model for the Blazor Todo application demonstrating ReactiveUI and Akavache integration.
/// </summary>
public class MainViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly TodoCacheService _cacheService;
    private readonly ObservableAsPropertyHelper<bool> _isLoading;
    private readonly ObservableAsPropertyHelper<TodoStats> _todoStats;
    private readonly ObservableAsPropertyHelper<CacheInfo> _cacheInfo;
    private string _newTodoTitle = string.Empty;
    private string _newTodoDescription = string.Empty;
    private DateTime? _newTodoDueDate;
    private TodoPriority _newTodoPriority = TodoPriority.Medium;
    private AppSettings _settings = new();
    private string _statusMessage = "Ready";
    private int _currentPage = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="cacheService">The cache service.</param>
    public MainViewModel(TodoCacheService cacheService)
    {
        _cacheService = cacheService;

        // Initialize collections
        Todos = new ObservableCollection<TodoItemViewModel>();
        AllTodos = new ObservableCollection<TodoItemViewModel>();
        Notifications = new ObservableCollection<string>();
        PriorityOptions = Enum.GetValues<TodoPriority>();

        // Create commands
        AddTodoCommand = ReactiveCommand.CreateFromObservable(ExecuteAddTodo);
        RefreshCommand = ReactiveCommand.CreateFromObservable(ExecuteRefresh);
        ClearCompletedCommand = ReactiveCommand.CreateFromObservable(ExecuteClearCompleted);
        SaveSettingsCommand = ReactiveCommand.CreateFromObservable(ExecuteSaveSettings);
        CleanupCacheCommand = ReactiveCommand.CreateFromObservable(ExecuteCleanupCache);
        LoadSampleDataCommand = ReactiveCommand.CreateFromObservable(ExecuteLoadSampleData);
        NextPageCommand = ReactiveCommand.Create(ExecuteNextPage);
        PreviousPageCommand = ReactiveCommand.Create(ExecutePreviousPage);
        ToggleThemeCommand = ReactiveCommand.Create(ExecuteToggleTheme);

        // Setup loading indicator
        var loadingCommands = new[] { AddTodoCommand, RefreshCommand, ClearCompletedCommand, SaveSettingsCommand };
        _isLoading = loadingCommands
            .Select(cmd => cmd.IsExecuting)
            .CombineLatest(executing => executing.Any(x => x))
            .ToProperty(this, x => x.IsLoading);

        // Setup todo statistics
        _todoStats = this.WhenAnyValue(x => x.AllTodos.Count)
            .Where(_ => AllTodos.Count > 0)
            .SelectMany(_ => _cacheService.GetTodoStats())
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.TodoStats, new TodoStats());

        // Setup cache info
        _cacheInfo = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(30))
            .SelectMany(_ => _cacheService.GetCacheInfo())
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.CacheInfo, new CacheInfo());

        // Setup activator for proper lifecycle management
        Activator = new ViewModelActivator();

        this.WhenActivated(disposables =>
        {
            // Load initial data
            LoadInitialData().Subscribe().DisposeWith(disposables);

            // Auto-save when todos change
            this.WhenAnyValue(x => x.AllTodos.Count)
                .Skip(1) // Skip initial load
                .Throttle(TimeSpan.FromSeconds(2))
                .SelectMany(_ => SaveCurrentTodos())
                .Subscribe()
                .DisposeWith(disposables);

            // Update pagination when settings change
            this.WhenAnyValue(x => x.Settings.ItemsPerPage, x => x.CurrentPage)
                .Subscribe(_ => UpdatePagination())
                .DisposeWith(disposables);

            // Auto-refresh if enabled
            this.WhenAnyValue(x => x.Settings.AutoRefresh)
                .Where(autoRefresh => autoRefresh)
                .SelectMany(_ => Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(5)))
                .SelectMany(_ => ExecuteRefresh())
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
    /// Gets the collection of todo items for current page.
    /// </summary>
    public ObservableCollection<TodoItemViewModel> Todos { get; }

    /// <summary>
    /// Gets the collection of all todo items.
    /// </summary>
    public ObservableCollection<TodoItemViewModel> AllTodos { get; }

    /// <summary>
    /// Gets the collection of notification messages.
    /// </summary>
    public ObservableCollection<string> Notifications { get; }

    /// <summary>
    /// Gets the priority options for the dropdown.
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
    public AppSettings Settings
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
    /// Gets or sets the current page number.
    /// </summary>
    public int CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    /// <summary>
    /// Gets a value indicating whether any operation is loading.
    /// </summary>
    public bool IsLoading => _isLoading.Value;

    /// <summary>
    /// Gets the current todo statistics.
    /// </summary>
    public TodoStats TodoStats => _todoStats.Value;

    /// <summary>
    /// Gets the current cache information.
    /// </summary>
    public CacheInfo CacheInfo => _cacheInfo.Value;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)AllTodos.Count / Settings.ItemsPerPage);

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

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
    /// Gets the command to go to next page.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NextPageCommand { get; }

    /// <summary>
    /// Gets the command to go to previous page.
    /// </summary>
    public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }

    /// <summary>
    /// Gets the command to toggle theme.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleThemeCommand { get; }

    /// <summary>
    /// Saves application state when shutting down.
    /// </summary>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> SaveApplicationState()
    {
        return Observable.Merge(
            SaveCurrentTodos(),
            _cacheService.SaveSettings(Settings),
            _cacheService.SaveApplicationState());
    }

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
            },
            new TodoItem
            {
                Title = "Build Blazor Dashboard",
                Description = "Create interactive dashboard showing cache statistics",
                DueDate = now.AddDays(2),
                Priority = TodoPriority.Medium,
                Tags = ["blazor", "dashboard", "ui"]
            },
            new TodoItem
            {
                Title = "API Integration",
                Description = "Demonstrate GetAndFetchLatest with simulated API calls",
                DueDate = now.AddHours(6),
                Priority = TodoPriority.High,
                Tags = ["api", "integration", "caching"]
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

    private IObservable<Unit> LoadTodos()
    {
        return _cacheService.GetAllTodos()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(todos =>
            {
                AllTodos.Clear();
                foreach (var todo in todos.OrderBy(GetSortKey))
                {
                    AllTodos.Add(new TodoItemViewModel(todo, _cacheService));
                }

                UpdatePagination();
            })
            .Select(_ => Unit.Default);
    }

    private IObservable<Unit> LoadSettings()
    {
        return _cacheService.GetSettings()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(settings => Settings = settings)
            .Select(_ => Unit.Default);
    }

    private IObservable<Unit> SaveCurrentTodos()
    {
        var todos = AllTodos.Select(vm => vm.TodoItem).ToList();
        return _cacheService.SaveTodos(todos)
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

        var viewModel = new TodoItemViewModel(newTodo, _cacheService);

        return Observable.FromAsync(async () =>
        {
            await Task.Run(() =>
            {
                AllTodos.Insert(0, viewModel);

                // Clear form
                NewTodoTitle = string.Empty;
                NewTodoDescription = string.Empty;
                NewTodoDueDate = null;
                NewTodoPriority = Settings.DefaultPriority;

                UpdatePagination();
            });
        })
        .Do(_ => StatusMessage = $"Added todo: {newTodo.Title}");
    }

    private IObservable<Unit> ExecuteRefresh()
    {
        StatusMessage = "Refreshing...";
        return LoadInitialData();
    }

    private IObservable<Unit> ExecuteClearCompleted()
    {
        return Observable.FromAsync(async () =>
        {
            var completedTodos = AllTodos.Where(vm => vm.TodoItem.IsCompleted).ToList();

            await Task.Run(() =>
            {
                foreach (var completedTodo in completedTodos)
                {
                    AllTodos.Remove(completedTodo);
                }

                UpdatePagination();
            });

            StatusMessage = $"Removed {completedTodos.Count} completed todos";
        });
    }

    private IObservable<Unit> ExecuteSaveSettings()
    {
        return _cacheService.SaveSettings(Settings)
            .Do(_ => StatusMessage = "Settings saved");
    }

    private IObservable<Unit> ExecuteCleanupCache()
    {
        StatusMessage = "Cleaning up cache...";
        return _cacheService.CleanupCache()
            .Do(_ => StatusMessage = "Cache cleaned up");
    }

    private IObservable<Unit> ExecuteLoadSampleData()
    {
        var sampleTodos = CreateSampleTodos();

        return Observable.FromAsync(async () =>
        {
            await Task.Run(() =>
            {
                AllTodos.Clear();
                foreach (var todo in sampleTodos)
                {
                    AllTodos.Add(new TodoItemViewModel(todo, _cacheService));
                }

                UpdatePagination();
            });
        })
        .SelectMany(_ => SaveCurrentTodos())
        .Do(_ => StatusMessage = $"Loaded {sampleTodos.Count} sample todos");
    }

    private Unit ExecuteNextPage()
    {
        if (HasNextPage)
        {
            CurrentPage++;
        }

        return Unit.Default;
    }

    private Unit ExecutePreviousPage()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
        }

        return Unit.Default;
    }

    private Unit ExecuteToggleTheme()
    {
        Settings.Theme = Settings.Theme switch
        {
            AppTheme.Light => AppTheme.Dark,
            AppTheme.Dark => AppTheme.System,
            AppTheme.System => AppTheme.Light,
            _ => AppTheme.Light
        };

        ExecuteSaveSettings().Subscribe();
        return Unit.Default;
    }

    private void UpdatePagination()
    {
        var filteredTodos = Settings.ShowCompleted
            ? AllTodos
            : AllTodos.Where(vm => !vm.TodoItem.IsCompleted);

        var skip = (CurrentPage - 1) * Settings.ItemsPerPage;
        var pageTodos = filteredTodos.Skip(skip).Take(Settings.ItemsPerPage).ToList();

        Todos.Clear();
        foreach (var todo in pageTodos)
        {
            Todos.Add(todo);
        }

        // Ensure CurrentPage is valid
        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
        }
        else if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }

        this.RaisePropertyChanged(nameof(TotalPages));
        this.RaisePropertyChanged(nameof(HasNextPage));
        this.RaisePropertyChanged(nameof(HasPreviousPage));
    }

    private object GetSortKey(TodoItem todo) => Settings.SortOrder switch
    {
        TodoSortOrder.CreatedDate => todo.CreatedAt,
        TodoSortOrder.DueDate => todo.DueDate ?? DateTimeOffset.MaxValue,
        TodoSortOrder.Priority => (int)todo.Priority,
        TodoSortOrder.Title => todo.Title,
        _ => todo.CreatedAt
    };
}
