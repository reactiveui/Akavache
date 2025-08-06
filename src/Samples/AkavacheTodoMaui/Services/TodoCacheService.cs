// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Core;
using AkavacheTodoMaui.Models;

namespace AkavacheTodoMaui.Services;

/// <summary>
/// Service that demonstrates comprehensive Akavache usage for the Todo application.
/// </summary>
public static class TodoCacheService
{
    /// <summary>
    /// Gets all todos from cache.
    /// </summary>
    /// <returns>Observable list of todos.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public static IObservable<List<TodoItem>> GetAllTodos() => BlobCache.UserAccount
        .GetObject<List<TodoItem>>("todos")
        .Catch(Observable.Return(new List<TodoItem>()))
        .Select(todos => todos ?? new List<TodoItem>());

    /// <summary>
    /// Saves todos to cache.
    /// </summary>
    /// <param name="todos">The todos to save.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <returns>Observable unit.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public static IObservable<Unit> SaveTodos(List<TodoItem> todos, DateTimeOffset? expiration = null) =>
        BlobCache.UserAccount.InsertObject("todos", todos, expiration);

    /// <summary>
    /// Gets application settings.
    /// </summary>
    /// <returns>Observable app settings.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public static IObservable<AppSettings> GetSettings() => BlobCache.UserAccount
        .GetOrCreateObject("app_settings", () => new AppSettings())
        .Select(settings => settings ?? new AppSettings());

    /// <summary>
    /// Saves application settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>Observable unit.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public static IObservable<Unit> SaveSettings(AppSettings? settings) =>
        BlobCache.UserAccount.InsertObject("app_settings", settings);

    /// <summary>
    /// Gets todo statistics.
    /// </summary>
    /// <returns>Observable todo statistics.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public static IObservable<TodoStats> GetTodoStats() => GetAllTodos()
        .Select(todos =>
        {
            if (todos == null || todos.Count == 0)
            {
                return new TodoStats();
            }

            return new TodoStats
            {
                TotalTodos = todos.Count,
                CompletedTodos = todos.Count(t => t.IsCompleted),
                OverdueTodos = todos.Count(t => t.IsOverdue),
                DueSoonTodos = todos.Count(t => t.IsDueSoon)
            };
        });

    /// <summary>
    /// Gets cache information with enhanced debugging and error handling.
    /// </summary>
    /// <returns>Observable cache information.</returns>
    public static IObservable<CacheInfo> GetCacheInfo() =>
        Observable.Defer(() =>
        {
            System.Diagnostics.Debug.WriteLine("Getting cache info...");

            // Use timeout and better error handling for each cache operation
            var userKeysObs = BlobCache.UserAccount.GetAllKeys()
                .ToArray()
                .Timeout(TimeSpan.FromSeconds(5))
                .Catch((Exception ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"UserAccount cache error: {ex.Message}");
                    return Observable.Return(Array.Empty<string>());
                });

            var localKeysObs = BlobCache.LocalMachine.GetAllKeys()
                .ToArray()
                .Timeout(TimeSpan.FromSeconds(5))
                .Catch((Exception ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"LocalMachine cache error: {ex.Message}");
                    return Observable.Return(Array.Empty<string>());
                });

            var secureKeysObs = BlobCache.Secure.GetAllKeys()
                .ToArray()
                .Timeout(TimeSpan.FromSeconds(5))
                .Catch((Exception ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Secure cache error: {ex.Message}");
                    return Observable.Return(Array.Empty<string>());
                });

            return userKeysObs.CombineLatest(
                localKeysObs,
                secureKeysObs,
                (userKeys, localKeys, secureKeys) =>
                {
                    var result = new CacheInfo
                    {
                        UserAccountKeys = userKeys?.Length ?? 0,
                        LocalMachineKeys = localKeys?.Length ?? 0,
                        SecureKeys = secureKeys?.Length ?? 0,
                        TotalKeys = (userKeys?.Length ?? 0) + (localKeys?.Length ?? 0) + (secureKeys?.Length ?? 0),
                        LastChecked = DateTimeOffset.Now
                    };

                    System.Diagnostics.Debug.WriteLine($"Cache keys found: User={result.UserAccountKeys}, Local={result.LocalMachineKeys}, Secure={result.SecureKeys}");
                    return result;
                })
                .Timeout(TimeSpan.FromSeconds(15)) // Overall timeout
                .Catch((Exception ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Cache info error: {ex}");
                    return Observable.Return(new CacheInfo
                    {
                        UserAccountKeys = -1,
                        LocalMachineKeys = -1,
                        SecureKeys = -1,
                        TotalKeys = -3,
                        LastChecked = DateTimeOffset.Now
                    });
                });
        });

    /// <summary>
    /// Invalidates a todo by ID.
    /// </summary>
    /// <param name="todoId">The todo ID to invalidate.</param>
    /// <returns>Observable unit.</returns>
    public static IObservable<Unit> InvalidateTodo(string todoId) =>
        BlobCache.UserAccount.Invalidate($"todo_{todoId}");

    /// <summary>
    /// Cleans up the cache.
    /// </summary>
    /// <returns>Observable unit.</returns>
    public static IObservable<Unit> CleanupCache() => BlobCache.UserAccount.Vacuum();

    /// <summary>
    /// Saves application state.
    /// </summary>
    /// <returns>Observable unit.</returns>
    [RequiresUnreferencedCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    [RequiresDynamicCode("This method uses reactive extensions which may not be preserved in trimming scenarios.")]
    public static IObservable<Unit> SaveApplicationState() =>
        BlobCache.UserAccount.InsertObject("last_shutdown", DateTimeOffset.Now);
}
