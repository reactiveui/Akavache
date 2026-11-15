// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings;
#else
namespace Akavache.Settings;
#endif

/// <summary>Constructs settings against a supplied cache and registers them on the owning instance.</summary>
internal static class SettingsStoreFactory
{
    /// <summary>Registers the cache before construction and publishes the store only after construction succeeds.</summary>
    /// <typeparam name="T">The settings type to construct.</typeparam>
    /// <param name="instance">The instance owning both registries.</param>
    /// <param name="key">The validated registry key.</param>
    /// <param name="cache">The cache supplied to the settings constructor.</param>
    /// <returns>The registered settings instance.</returns>
    internal static T Create<T>(IAkavacheInstance instance, string key, IBlobCache cache)
        where T : class, ISettingsStorage, new()
    {
        instance.BlobCaches[key] = cache;

        T settings;
        using (SettingsBase.PushAmbientCache(cache))
        {
            settings = new();
        }

        instance.SettingsStores[key] = settings;
        return settings;
    }
}
