// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Akavache;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
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

    /// <summary>The app's SQLite stores and their generated serialization contract.</summary>
    private static readonly IAkavacheInstance Stores = CacheDatabase.CreateBuilder("AkavacheTodoWpf")
        .WithSerializerSystemTextJson(TodoJsonContext.Default.Options)
        .UseForcedDateTimeKind(DateTimeKind.Utc)
        .WithSqliteDefaults()
        .Build();

    /// <summary>Gets the user store supplied by the SQLite defaults.</summary>
    private static IBlobCache UserAccount => Stores.UserAccount!;

    /// <summary>Gets all todos from cache.</summary>
    /// <returns>Observable list of todos.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<List<TodoItem>?> GetAllTodos() => UserAccount
        .Get("todos", typeof(List<TodoItem>))
        .Select(static bytes => bytes is null ? null : SystemJsonSerializer.DeserializeAot(bytes, TodoJsonContext.Default.ListTodoItem))
        .Catch<List<TodoItem>?, Exception>(static _ => Signal.Return<List<TodoItem>?>([]));

    /// <summary>Saves todos to cache so that they never expire.</summary>
    /// <param name="todos">The todos to save.</param>
    /// <returns>Observable unit.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<RxVoid> SaveTodos(List<TodoItem> todos) => SaveTodos(todos, null);

    /// <summary>Saves todos to cache.</summary>
    /// <param name="todos">The todos to save.</param>
    /// <param name="expiration">The absolute expiration time, or null to keep the entry indefinitely.</param>
    /// <returns>Observable unit.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<RxVoid> SaveTodos(List<TodoItem> todos, DateTimeOffset? expiration) =>
        UserAccount.Insert("todos", SystemJsonSerializer.SerializeAot(todos, TodoJsonContext.Default.ListTodoItem), typeof(List<TodoItem>), expiration);

    /// <summary>Gets application settings.</summary>
    /// <returns>Observable app settings.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<AppSettings?> GetSettings() => UserAccount
        .Get("app_settings", typeof(AppSettings))
        .Select(static bytes => bytes is null ? null : SystemJsonSerializer.DeserializeAot(bytes, TodoJsonContext.Default.AppSettings))
        .Catch<AppSettings?, Exception>(static _ =>
        {
            AppSettings settings = new();
            return SaveSettings(settings).Select(_ => (AppSettings?)settings);
        });

    /// <summary>Saves application settings.</summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>Observable unit.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<RxVoid> SaveSettings(AppSettings? settings) =>
        UserAccount.Insert("app_settings", settings is null ? [] : SystemJsonSerializer.SerializeAot(settings, TodoJsonContext.Default.AppSettings), typeof(AppSettings));

    /// <summary>Gets todo statistics.</summary>
    /// <returns>Observable todo statistics.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<TodoStats?> GetTodoStats() => GetAllTodos().Select(Summarize);

    /// <summary>Gets cache information with enhanced debugging and error handling.</summary>
    /// <returns>Observable cache information.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<CacheInfo> GetCacheInfo() =>
        Signal.Defer(static () =>
        {
            System.Diagnostics.Debug.WriteLine("Getting cache info...");

            // Use timeout and better error handling for each cache operation
            var userKeysObs = UserAccount.GetAllKeys()
                .ToArray()
                .Timeout(TimeSpan.FromSeconds(CacheQueryTimeoutSeconds))
                .Catch(static (Exception ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"UserAccount cache error: {ex.Message}");
                    return Signal.Return(Array.Empty<string>());
                });

            var localKeysObs = Stores.LocalMachine!.GetAllKeys()
                .ToArray()
                .Timeout(TimeSpan.FromSeconds(CacheQueryTimeoutSeconds))
                .Catch(static (Exception ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"LocalMachine cache error: {ex.Message}");
                    return Signal.Return(Array.Empty<string>());
                });

            var secureKeysObs = Stores.Secure!.GetAllKeys()
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
                        LastChecked = TimeProvider.System.GetLocalNow(),
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<RxVoid> InvalidateTodo(string todoId) =>
        UserAccount.Invalidate($"todo_{todoId}");

    /// <summary>Cleans up the cache.</summary>
    /// <returns>Observable unit.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<RxVoid> CleanupCache() => UserAccount.Vacuum();

    /// <summary>Saves application state.</summary>
    /// <returns>Observable unit.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<RxVoid> SaveApplicationState() =>
        UserAccount.Insert(
            "last_shutdown",
            SystemJsonSerializer.SerializeAot(TimeProvider.System.GetLocalNow(), TodoJsonContext.Default.DateTimeOffset),
            typeof(DateTimeOffset));

    /// <summary>Flushes and disposes the four caches opened by the SQLite defaults.</summary>
    /// <param name="token">Cancels the wait for pending writes.</param>
    /// <returns>Completion after closing the stores.</returns>
    internal static async Task FlushAndCloseAsync(CancellationToken token)
    {
        using var account = UserAccount;
        using var local = Stores.LocalMachine!;
        using var secure = Stores.Secure!;
        using var memory = Stores.InMemory!;
        await Task.WhenAll(account.Flush().ToTask(token), local.Flush().ToTask(token), secure.Flush().ToTask(token), memory.Flush().ToTask(token));
    }

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
        LastChecked = TimeProvider.System.GetLocalNow(),
    };
}
