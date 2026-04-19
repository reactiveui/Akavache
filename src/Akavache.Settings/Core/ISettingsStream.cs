// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Settings.Core;

/// <summary>
/// Non-generic handle over a <see cref="SettingsStream{T}"/> so <see cref="SettingsStorage"/>
/// can hold heterogeneous streams in a single dictionary. Lets <see cref="SettingsStorage.Initialize"/>
/// pre-warm every stream's cold load without needing to know each property's concrete
/// value type.
/// </summary>
internal interface ISettingsStream : IDisposable
{
    /// <summary>
    /// Starts the one-time cold load from disk if it hasn't already begun, and returns an
    /// observable that fires <see cref="Unit"/> when the load finishes (or immediately if
    /// it was already done). The returned observable never errors — failures during the
    /// cold load are swallowed so callers can treat it as "best-effort warm-up".
    /// </summary>
    /// <returns>A one-shot <c>Unit</c> observable that completes when the cold load is done.</returns>
    IObservable<Unit> EnsureLoaded();
}
