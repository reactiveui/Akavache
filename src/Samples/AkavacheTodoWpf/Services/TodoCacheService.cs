// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using Akavache.Core;
using AkavacheTodoWpf.Models;

namespace AkavacheTodoWpf.Services;

/// <summary>
/// Service that demonstrates comprehensive Akavache usage for the Todo application.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public static class TodoCacheService
{
    /// <summary>
    /// Gets all todos from cache.
    /// </summary>
    /// <returns>Observable list of todos.</returns>
    public static IObservable<List<TodoItem>?> GetAllTodos() => BlobCache.UserAccount
        .GetObject<List<TodoItem>>("todos")
        .Catch(Observable.Return(new List<TodoItem>()));

    /// <summary>
    /// Saves todos to cache.
    /// </summary>
    /// <param name="todos">The todos to save.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <returns>Observable unit.</returns>
    public static IObservable<Unit> SaveTodos(List<TodoItem> todos, DateTimeOffset? expiration = null) =>
        BlobCache.UserAccount.InsertObject("todos", todos, expiration);

    /// <summary>
    /// Gets application settings.
    /// </summary>
    /// <returns>Observable app settings.</returns>
    public static IObservable<AppSettings?> GetSettings() => BlobCache.UserAccount
        .GetOrCreateObject("app_settings", () => new AppSettings());

    /// <summary>
    /// Saves application settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>Observable unit.</returns>
    public static IObservable<Unit> SaveSettings(AppSettings? settings) =>
        BlobCache.UserAccount.InsertObject("app_settings", settings);

    /// <summary>
    /// Gets todo statistics.
    /// </summary>
    /// <returns>Observable todo statistics.</returns>
    public static IObservable<TodoStats?> GetTodoStats() => GetAllTodos()
        .Select(todos =>
        {
            if (todos == null || todos.Count == 0)
            {
                return new TodoStats();
            }

            var now = DateTimeOffset.Now;
            return new TodoStats
            {
                TotalTodos = todos.Count,
                CompletedTodos = todos.Count(t => t.IsCompleted),
                OverdueTodos = todos.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate < now),
                DueSoonTodos = todos.Count(t => !t.IsCompleted && t.DueDate.HasValue &&
                    t.DueDate >= now && t.DueDate <= now.AddDays(1))
            };
        });

    /// <summary>
    /// Gets cache information.
    /// </summary>
    /// <returns>Observable cache information.</returns>
    public static IObservable<CacheInfo> GetCacheInfo() => Observable.FromAsync(async () =>
    {
        try
        {
            var userKeys = await BlobCache.UserAccount.GetAllKeys();
            var localKeys = await BlobCache.LocalMachine.GetAllKeys();
            var secureKeys = await BlobCache.Secure.GetAllKeys();

            return new CacheInfo
            {
                UserAccountKeys = userKeys?.Length ?? 0,
                LocalMachineKeys = localKeys?.Length ?? 0,
                SecureKeys = secureKeys?.Length ?? 0
            };
        }
        catch (Exception)
        {
            // Return empty cache info if any cache operation fails
            return new CacheInfo
            {
                UserAccountKeys = 0,
                LocalMachineKeys = 0,
                SecureKeys = 0
            };
        }
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
    public static IObservable<Unit> SaveApplicationState() =>
        BlobCache.UserAccount.InsertObject("last_shutdown", DateTimeOffset.Now);
}
