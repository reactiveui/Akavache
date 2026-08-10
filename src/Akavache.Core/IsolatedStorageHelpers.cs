// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.IsolatedStorage;
using System.Reflection;

#if REACTIVE_SHIM
namespace Akavache.Reactive;
#else
namespace Akavache;
#endif

/// <summary>
/// Isolated storage lookups used while resolving a cache directory. Kept outside
/// <c>AkavacheBuilderExtensions</c> so the reflection-based path probe stays an internal helper
/// rather than an extension published on <see cref="IsolatedStorageFile"/>.
/// </summary>
internal static class IsolatedStorageHelpers
{
    /// <summary>
    /// Resolves the physical directory backing <paramref name="cacheName"/> inside an isolated
    /// storage store, creating it when absent. Returns <c>null</c> when the store cannot be read
    /// or written, which is the caller's signal to fall back to the local application data path.
    /// </summary>
    /// <param name="isoStore">The opened isolated storage store.</param>
    /// <param name="applicationName">The already-validated application name segment.</param>
    /// <param name="cacheName">The already-validated cache name segment.</param>
    /// <returns>The physical cache directory, or <c>null</c> when isolated storage is unusable.</returns>
    [ExcludeFromCodeCoverage]
    [SuppressMessage(
        "Serialization",
        "SES1406:Reflection must not reach non-public members to bypass their declared accessibility",
        Justification = "IsolatedStorageFile exposes no public member for the store's physical path; the null-tolerant lookup returns null on failure and the caller falls back.")]
    internal static string? ResolveIsolatedCachePath(IsolatedStorageFile isoStore, string applicationName, string cacheName)
    {
        try
        {
            var isoPath = Path.Combine(applicationName, cacheName);

            // Ensure the directory exists
            if (!isoStore.DirectoryExists(isoPath))
            {
                isoStore.CreateDirectory(isoPath);
            }

            if (!isoStore.DirectoryExists(isoPath))
            {
                return null;
            }

            _ = isoStore.GetDirectoryNames(isoPath);

            // 'RootDirectory' is the only route to the store's on-disk location: it is private on
            // .NET and internal on .NET Framework, and neither IsolatedStorageFile nor its base
            // type surfaces the path through any public member
            var rootDirectory = isoStore.GetType()
                .GetProperty("RootDirectory", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(isoStore)
                ?.ToString();

            return Path.Combine(rootDirectory ?? string.Empty, isoPath);
        }
        catch (Exception ex) when (ex is IsolatedStorageException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // The store can be unavailable, read-only, or already disposed on this platform, in
            // which case the caller falls back to the local application data path
            return null;
        }
    }
}
