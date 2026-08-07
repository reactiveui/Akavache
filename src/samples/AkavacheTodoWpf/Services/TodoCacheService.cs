// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using Akavache;
using AkavacheTodoWpf.Models;

namespace AkavacheTodoWpf.Services;

/// <summary>Service that demonstrates comprehensive Akavache usage for the Todo application.</summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public static class TodoCacheService
{
    /// <summary>How long a single cache key enumeration may run before it is abandoned.</summary>
    private const int CacheQueryTimeoutSeconds = 5;

    /// <summary>How long the combined cache inspection may run before it is abandoned.</summary>
    private const int CacheInfoTimeoutSeconds = 15;

    /// <summary>Sentinel key count reported for a cache that could not be queried.</summary>
    private const int UnknownKeyCount = -1;

    /// <summary>The caches summarized by <see cref="GetCacheInfo"/>: user account, local machine and secure.</summary>
    private const int TrackedCacheCount = 3;

    /// <summary>Gets all todos from cache.</summary>
    /// <returns>Observable list of todos.</returns>
    public static IObservable<List<TodoItem>?> GetAllTodos() => CacheDatabase.UserAccount
        .GetObject<List<TodoItem>>("todos")
        .Catch<List<TodoItem>?, Exception>(static _ => Signal.Return<List<TodoItem>?>([]));

    /// <summary>Saves todos to cache so that they never expire.</summary>
    /// <param name="todos">The todos to save.</param>
    /// <returns>Observable unit.</returns>
    public static IObservable<RxVoid> SaveTodos(List<TodoItem> todos) => SaveTodos(todos, null);

    /// <summary>Saves todos to cache.</summary>
    /// <param name="todos">The todos to save.</param>
    /// <param name="expiration">The absolute expiration time, or null to keep the entry indefinitely.</param>
    /// <returns>Observable unit.</returns>
    public static IObservable<RxVoid> SaveTodos(List<TodoItem> todos, DateTimeOffset? expiration) =>
        CacheDatabase.UserAccount.InsertObject("todos", todos, expiration);

    /// <summary>Gets application settings.</summary>
    /// <returns>Observable app settings.</returns>
    public static IObservable<AppSettings?> GetSettings() => CacheDatabase.UserAccount
        .GetOrCreateObject("app_settings", static () => new AppSettings());

    /// <summary>Saves application settings.</summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>Observable unit.</returns>
    public static IObservable<RxVoid> SaveSettings(AppSettings? settings) =>
        CacheDatabase.UserAccount.InsertObject("app_settings", settings);

    /// <summary>Gets todo statistics.</summary>
    /// <returns>Observable todo statistics.</returns>
    public static IObservable<TodoStats?> GetTodoStats() => GetAllTodos().Select(Summarize);

    /// <summary>Gets cache information with enhanced debugging and error handling.</summary>
    /// <returns>Observable cache information.</returns>
    public static IObservable<CacheInfo> GetCacheInfo() =>
        Signal.Defer(static () =>
        {
            System.Diagnostics.Debug.WriteLine("Getting cache info...");

            // Use timeout and better error handling for each cache operation
            var userKeysObs = CacheDatabase.UserAccount.GetAllKeys()
                .ToArray()
                .Timeout(TimeSpan.FromSeconds(CacheQueryTimeoutSeconds))
                .Catch(static (Exception ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"UserAccount cache error: {ex.Message}");
                    return Signal.Return(Array.Empty<string>());
                });

            var localKeysObs = CacheDatabase.LocalMachine.GetAllKeys()
                .ToArray()
                .Timeout(TimeSpan.FromSeconds(CacheQueryTimeoutSeconds))
                .Catch(static (Exception ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"LocalMachine cache error: {ex.Message}");
                    return Signal.Return(Array.Empty<string>());
                });

            var secureKeysObs = CacheDatabase.Secure.GetAllKeys()
                .ToArray()
                .Timeout(TimeSpan.FromSeconds(CacheQueryTimeoutSeconds))
                .Catch(static (Exception ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Secure cache error: {ex.Message}");
                    return Signal.Return(Array.Empty<string>());
                });

            return userKeysObs.CombineLatest(
                localKeysObs,
                secureKeysObs,
                static (userKeys, localKeys, secureKeys) =>
                {
                    CacheInfo result = new()
                    {
                        UserAccountKeys = userKeys?.Length ?? 0,
                        LocalMachineKeys = localKeys?.Length ?? 0,
                        SecureKeys = secureKeys?.Length ?? 0,
                        TotalKeys = (userKeys?.Length ?? 0) + (localKeys?.Length ?? 0) + (secureKeys?.Length ?? 0),
                        LastChecked = TimeProvider.System.GetLocalNow()
                    };

                    System.Diagnostics.Debug.WriteLine($"Cache keys found: User={result.UserAccountKeys}, Local={result.LocalMachineKeys}, Secure={result.SecureKeys}");
                    return result;
                })
                .Timeout(TimeSpan.FromSeconds(CacheInfoTimeoutSeconds)) // Overall timeout
                .Catch(static (Exception ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Cache info error: {ex}");
                    return Signal.Return(UnavailableCacheInfo());
                });
        });

    /// <summary>Invalidates a todo by ID.</summary>
    /// <param name="todoId">The todo ID to invalidate.</param>
    /// <returns>Observable unit.</returns>
    public static IObservable<RxVoid> InvalidateTodo(string todoId) =>
        CacheDatabase.UserAccount.Invalidate($"todo_{todoId}");

    /// <summary>Cleans up the cache.</summary>
    /// <returns>Observable unit.</returns>
    public static IObservable<RxVoid> CleanupCache() => CacheDatabase.UserAccount.Vacuum();

    /// <summary>Saves application state.</summary>
    /// <returns>Observable unit.</returns>
    public static IObservable<RxVoid> SaveApplicationState() =>
        CacheDatabase.UserAccount.InsertObject("last_shutdown", TimeProvider.System.GetLocalNow());

    /// <summary>Counts the completed, overdue and due-soon todos in a single pass.</summary>
    /// <param name="todos">The todos to summarize, or null when the cache holds nothing.</param>
    /// <returns>Statistics describing the supplied todos.</returns>
    private static TodoStats Summarize(List<TodoItem>? todos)
    {
        TodoStats stats = new();

        if (todos is null)
        {
            return stats;
        }

        foreach (var todo in todos)
        {
            if (todo.IsCompleted)
            {
                stats.CompletedTodos++;
            }

            if (!todo.IsOverdue)
            {
                stats.OverdueTodos++;
            }

            if (!todo.IsDueSoon)
            {
                stats.DueSoonTodos++;
            }
        }

        stats.TotalTodos = todos.Count;
        return stats;
    }

    /// <summary>Builds the placeholder reported when the caches cannot be inspected.</summary>
    /// <returns>Cache information whose counts are all <see cref="UnknownKeyCount"/>.</returns>
    private static CacheInfo UnavailableCacheInfo() => new()
    {
        UserAccountKeys = UnknownKeyCount,
        LocalMachineKeys = UnknownKeyCount,
        SecureKeys = UnknownKeyCount,
        TotalKeys = UnknownKeyCount * TrackedCacheCount,
        LastChecked = TimeProvider.System.GetLocalNow()
    };
}
