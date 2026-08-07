// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings;
#else
namespace Akavache.Settings;
#endif

/// <summary>
/// Task-based compatibility wrappers around the <see cref="ISettingsStorage"/> surface.
/// These extension methods are provided for callers that prefer async/await over observables.
/// </summary>
public static class SettingsStorageAsyncExtensions
{
    /// <summary>Extension members for <c>ISettingsStorage</c>.</summary>
    /// <param name="storage">The settings storage to initialize.</param>
    extension(ISettingsStorage storage)
    {
        /// <summary>Initializes the settings storage asynchronously.</summary>
        /// <returns>A task that completes once every property's cold load from disk has finished.</returns>
        [RequiresUnreferencedCode("Settings initialization requires types to be preserved for reflection.")]
        [RequiresDynamicCode("Settings initialization requires types to be preserved for reflection.")]
        public Task InitializeAsync()
        {
            ArgumentExceptionHelper.ThrowIfNull(storage);
            return storage.Initialize().ToTask();
        }
    }
}
