// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Threading.Tasks;
using Akavache.Helpers;

namespace Akavache.Settings;

/// <summary>
/// Task-based compatibility wrappers around the <see cref="ISettingsStorage"/> surface.
/// These extension methods are provided for callers that prefer async/await over observables.
/// </summary>
public static class SettingsStorageAsyncExtensions
{
    /// <summary>
    /// Initializes the settings storage asynchronously.
    /// </summary>
    /// <param name="storage">The settings storage to initialize.</param>
    /// <returns>A task that completes once every property's cold load from disk has finished.</returns>
    [RequiresUnreferencedCode("Settings initialization requires types to be preserved for reflection.")]
    [RequiresDynamicCode("Settings initialization requires types to be preserved for reflection.")]
    public static Task InitializeAsync(this ISettingsStorage storage)
    {
        ArgumentExceptionHelper.ThrowIfNull(storage);
        return storage.Initialize().ToTask();
    }
}
