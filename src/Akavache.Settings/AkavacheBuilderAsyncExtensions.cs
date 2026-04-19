// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Threading.Tasks;

namespace Akavache.Settings;

/// <summary>
/// Task-based extension methods for <see cref="IAkavacheInstance"/> to manage settings stores.
/// These methods provide an async/await friendly alternative to the observable-based APIs.
/// </summary>
public static class AkavacheBuilderAsyncExtensions
{
    /// <summary>
    /// Asynchronously deletes the settings store for the specified type.
    /// </summary>
    /// <typeparam name="T">The settings type whose store should be deleted.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="overrideDatabaseName">Optional override database name.</param>
    /// <returns>A task that completes when deletion is done.</returns>
    public static Task DeleteSettingsStoreAsync<T>(this IAkavacheInstance builder, string? overrideDatabaseName = null) =>
        builder.DeleteSettingsStore<T>(overrideDatabaseName).ToTask();

    /// <summary>
    /// Asynchronously disposes of the settings store for the specified type.
    /// </summary>
    /// <typeparam name="T">The settings type whose store should be disposed.</typeparam>
    /// <param name="builder">The Akavache builder instance.</param>
    /// <param name="overrideDatabaseName">Optional override database name.</param>
    /// <returns>A task that completes when disposal is done.</returns>
    public static Task DisposeSettingsStoreAsync<T>(this IAkavacheInstance builder, string? overrideDatabaseName = null) =>
        builder.DisposeSettingsStore<T>(overrideDatabaseName).ToTask();
}
