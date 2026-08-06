// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Threading.Tasks;

namespace Akavache.Settings;

/// <summary>
/// Task-based extension methods for <see cref="IAkavacheInstance"/> to manage settings stores.
/// These methods provide an async/await friendly alternative to the observable-based APIs.
/// </summary>
public static class AkavacheBuilderAsyncExtensions
{
    /// <summary>Extension members for <c>IAkavacheInstance</c>.</summary>
    /// <param name="builder">The Akavache builder instance.</param>
    extension(IAkavacheInstance builder)
    {
        /// <summary>Asynchronously deletes the settings store for the specified type.</summary>
        /// <typeparam name="T">The settings type whose store should be deleted.</typeparam>
        /// <returns>A task that completes when deletion is done.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public Task DeleteSettingsStoreAsync<T>() =>
            builder.DeleteSettingsStoreAsync<T>((string?)null);

        /// <summary>Asynchronously deletes the settings store for the specified type.</summary>
        /// <typeparam name="T">The settings type whose store should be deleted.</typeparam>
        /// <param name="overrideDatabaseName">Optional override database name.</param>
        /// <returns>A task that completes when deletion is done.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public Task DeleteSettingsStoreAsync<T>(string? overrideDatabaseName) =>
                builder.DeleteSettingsStore<T>(overrideDatabaseName).ToTask();

        /// <summary>Asynchronously disposes of the settings store for the specified type.</summary>
        /// <typeparam name="T">The settings type whose store should be disposed.</typeparam>
        /// <returns>A task that completes when disposal is done.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public Task DisposeSettingsStoreAsync<T>() =>
            builder.DisposeSettingsStoreAsync<T>((string?)null);

        /// <summary>Asynchronously disposes of the settings store for the specified type.</summary>
        /// <typeparam name="T">The settings type whose store should be disposed.</typeparam>
        /// <param name="overrideDatabaseName">Optional override database name.</param>
        /// <returns>A task that completes when disposal is done.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Type parameter appears in no parameter",
            Justification = "The type parameter names the cached or serialized type; there is no argument to infer it from.")]
        public Task DisposeSettingsStoreAsync<T>(string? overrideDatabaseName) =>
                builder.DisposeSettingsStore<T>(overrideDatabaseName).ToTask();
    }
}
