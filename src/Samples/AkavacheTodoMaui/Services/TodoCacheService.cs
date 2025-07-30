// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using Akavache.Core;
using AkavacheTodoMaui.Extensions;
using AkavacheTodoMaui.Models;

namespace AkavacheTodoMaui.Services;

/// <summary>
/// Service that demonstrates comprehensive Akavache usage for the Todo application.
/// </summary>
public class TodoCacheService
{
    private const string TodosKey = "todos";
    private const string SettingsKey = "app_settings";
    private const string LastSyncKey = "last_sync";
    private const string UserPreferencesKey = "user_preferences";
    private const string CacheStatsKey = "cache_stats";

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoCacheService"/> class.
    /// </summary>
    public TodoCacheService()
    {
    }

    /// <summary>
    /// Gets all todos from cache, demonstrating GetObject with error handling.
    /// </summary>
    /// <returns>Observable list of todos.</returns>
    public IObservable<List<TodoItem>> GetAllTodos()
    {
        return BlobCache.UserAccount
            .GetObject<List<TodoItem>>(TodosKey)
            .Catch(Observable.Return(new List<TodoItem>()));
    }

    /// <summary>
    /// Saves todos to cache with expiration, demonstrating InsertObject with DateTime expiration.
    /// </summary>
    /// <param name="todos">The todos to save.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> SaveTodos(List<TodoItem> todos, DateTimeOffset? expiration = null)
    {
        var exp = expiration ?? DateTimeOffset.Now.AddHours(24);
        return BlobCache.UserAccount.InsertObject(TodosKey, todos, exp);
    }

    /// <summary>
    /// Gets a specific todo by ID, demonstrating GetOrFetchObject pattern.
    /// </summary>
    /// <param name="todoId">The todo ID.</param>
    /// <returns>Observable todo item.</returns>
    public IObservable<TodoItem?> GetTodo(string todoId)
    {
        var key = $"todo_{todoId}";
        return BlobCache.UserAccount
            .GetOrFetchObject(key, async () =>
            {
                // Simulate fetching from a remote source
                var todos = await GetAllTodos().FirstAsync();
                return todos.FirstOrDefault(t => t.Id == todoId);
            }, DateTimeOffset.Now.AddMinutes(30));
    }

    /// <summary>
    /// Saves application settings, demonstrating different cache locations.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> SaveSettings(AppSettings settings)
    {
        settings.LastUsed = DateTimeOffset.Now;

        // Save to UserAccount for settings that should persist and potentially sync
        return BlobCache.UserAccount.InsertObject(SettingsKey, settings);
    }

    /// <summary>
    /// Gets application settings with default fallback.
    /// </summary>
    /// <returns>Observable app settings.</returns>
    public IObservable<AppSettings> GetSettings()
    {
        return BlobCache.UserAccount
            .GetOrCreateObject(SettingsKey, () => new AppSettings());
    }

    /// <summary>
    /// Demonstrates GetAndFetchLatest pattern for getting fresh data while serving cached.
    /// </summary>
    /// <returns>Observable todo statistics.</returns>
    public IObservable<TodoStats> GetTodoStats()
    {
        return BlobCache.LocalMachine.GetAndFetchLatest(
            CacheStatsKey,
            () => Observable.FromAsync(() => CalculateTodoStats()),
            createdAt => DateTimeOffset.Now - createdAt > TimeSpan.FromMinutes(5), // Refresh if older than 5 minutes
            DateTimeOffset.Now.AddHours(1));
    }

    /// <summary>
    /// Demonstrates bulk operations and GetCreatedAt functionality.
    /// </summary>
    /// <param name="todoIds">The todo IDs to get metadata for.</param>
    /// <returns>Observable dictionary of creation dates.</returns>
    public IObservable<Dictionary<string, DateTimeOffset?>> GetTodoMetadata(IEnumerable<string> todoIds)
    {
        var keys = todoIds.Select(id => $"todo_{id}").ToList();

        return BlobCache.UserAccount
            .GetCreatedAt(keys)
            .Select(results => results.ToDictionary(
                kvp => kvp.Key.Replace("todo_", string.Empty),
                kvp => kvp.Value));
    }

    /// <summary>
    /// Demonstrates InvalidateObject for specific cache invalidation.
    /// </summary>
    /// <param name="todoId">The todo ID to invalidate.</param>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> InvalidateTodo(string todoId)
    {
        var key = $"todo_{todoId}";
        return BlobCache.UserAccount.InvalidateObject<TodoItem>(key);
    }

    /// <summary>
    /// Demonstrates cache expiration by saving temporary data.
    /// </summary>
    /// <param name="data">The data to cache temporarily.</param>
    /// <param name="expirationMinutes">Minutes until expiration.</param>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> SaveTemporaryData(object data, int expirationMinutes = 5)
    {
        var key = $"temp_data_{DateTime.Now.Ticks}";
        var expiration = DateTimeOffset.Now.AddMinutes(expirationMinutes);

        return BlobCache.LocalMachine.InsertObject(key, data, expiration);
    }

    /// <summary>
    /// Demonstrates Vacuum operation for cache cleanup.
    /// </summary>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> CleanupCache()
    {
        return Observable.Merge(
            BlobCache.LocalMachine.Vacuum(),
            BlobCache.UserAccount.Vacuum());
    }

    /// <summary>
    /// Demonstrates GetAllKeys for debugging purposes.
    /// </summary>
    /// <returns>Observable list of all cache keys.</returns>
    public IObservable<List<string>> GetAllCacheKeys()
    {
        return BlobCache.UserAccount
            .GetAllKeys()
            .Select(keys => keys.ToList());
    }

    /// <summary>
    /// Demonstrates secure storage using BlobCache.Secure.
    /// </summary>
    /// <param name="userCredentials">The credentials to store securely.</param>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> SaveSecureData(Dictionary<string, string> userCredentials)
    {
        return BlobCache.Secure.InsertObject("user_credentials", userCredentials);
    }

    /// <summary>
    /// Gets secure data from encrypted cache.
    /// </summary>
    /// <returns>Observable dictionary of user credentials.</returns>
    public IObservable<Dictionary<string, string>> GetSecureData()
    {
        return BlobCache.Secure
            .GetObject<Dictionary<string, string>>("user_credentials")
            .Catch(Observable.Return(new Dictionary<string, string>()));
    }

    /// <summary>
    /// Demonstrates cache size and performance monitoring.
    /// </summary>
    /// <returns>Observable cache information.</returns>
    public IObservable<CacheInfo> GetCacheInfo()
    {
        return Observable.CombineLatest(
            BlobCache.UserAccount.GetAllKeys(),
            BlobCache.LocalMachine.GetAllKeys(),
            BlobCache.Secure.GetAllKeys(),
            (userKeys, localKeys, secureKeys) => new CacheInfo
            {
                UserAccountKeys = userKeys.Length,
                LocalMachineKeys = localKeys.Length,
                SecureKeys = secureKeys.Length,
                TotalKeys = userKeys.Length + localKeys.Length + secureKeys.Length,
                LastChecked = DateTimeOffset.Now
            });
    }

    /// <summary>
    /// Saves application state on shutdown.
    /// </summary>
    /// <returns>Observable unit.</returns>
    public IObservable<Unit> SaveApplicationState()
    {
        var appState = new
        {
            LastShutdown = DateTimeOffset.Now,
            Version = "1.0.0",
            Platform = DeviceInfo.Platform.ToString()
        };

        return BlobCache.UserAccount.InsertObject("app_state", appState)
            .SelectMany(_ => BlobCache.UserAccount.Flush());
    }

    /// <summary>
    /// Loads application state on startup.
    /// </summary>
    /// <returns>Observable app state.</returns>
    public IObservable<AppState> LoadApplicationState()
    {
        return BlobCache.UserAccount
            .GetObject<AppState>("app_state")
            .Catch(Observable.Return(new AppState
            {
                LastShutdown = DateTimeOffset.Now,
                Version = "1.0.0",
                Platform = DeviceInfo.Platform.ToString()
            }));
    }

    private async Task<TodoStats> CalculateTodoStats()
    {
        var todos = await GetAllTodos().FirstAsync();

        return new TodoStats
        {
            TotalTodos = todos.Count,
            CompletedTodos = todos.Count(t => t.IsCompleted),
            OverdueTodos = todos.Count(t => t.IsOverdue),
            DueSoonTodos = todos.Count(t => t.IsDueSoon),
            HighPriorityTodos = todos.Count(t => t.Priority >= TodoPriority.High),
            LastCalculated = DateTimeOffset.Now
        };
    }
}
