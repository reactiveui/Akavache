// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace OrderCache;

/// <summary>Stores a dispatcher's notification preference in the supplied cache.</summary>
/// <param name="cache">The cache that these settings own and dispose.</param>
[RequiresUnreferencedCode("SettingsStorage serializes settings using the cache's runtime serializer.")]
[RequiresDynamicCode("SettingsStorage uses runtime serialization for its observable settings.")]
internal sealed class DeliveryPreferences(IBlobCache cache) : SettingsStorage(nameof(DeliveryPreferences), cache)
{
    /// <summary>Gets a stream of the latest notification preference and subsequent changes.</summary>
    internal IObservable<bool> Notifications => GetOrCreateObservable(true);

    /// <summary>Persists a notification choice and publishes it to subscribers.</summary>
    /// <param name="enabled">Whether the dispatcher wants delivery notifications.</param>
    /// <param name="token">Cancels waiting for the write.</param>
    /// <returns>Completion when the cache accepts the preference.</returns>
    internal async Task SetNotificationsAsync(bool enabled, CancellationToken token) =>
        _ = await SetObservable(enabled, nameof(Notifications)).ToTask(token);
}
