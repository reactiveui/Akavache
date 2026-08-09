// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

#if REACTIVE_SHIM
namespace Akavache.Reactive.V10toV11;
#else
namespace Akavache.V10toV11;
#endif

/// <summary>
/// Path resolution, cache construction, and per-kind migration steps behind the V10-to-V11
/// builder extensions. These take the builder as an ordinary argument rather than acting on a
/// receiver, so they stay helpers instead of being published as extensions. Kept internal so
/// tests can drive each branch without the full <c>MigrateFromV10</c> entry point.
/// </summary>
internal static class V10MigrationHelpers
{
    /// <summary>
    /// Wraps a single cache-kind migration in an <see cref="IObservable{RxVoid}"/> that
    /// short-circuits when the kind is disabled, the underlying cache is not a
    /// <see cref="SqliteBlobCache"/>, or no V10 database file exists for it. The
    /// returned observable emits a single <see cref="RxVoid"/> on completion regardless
    /// of which branch fired — so callers can <c>Concat</c> multiple kinds into one
    /// pipeline without tracking each one individually.
    /// </summary>
    /// <remarks>
    /// Marked <c>internal</c> so tests can drive each branch in isolation without
    /// spinning up the full <c>MigrateFromV10</c> entry point. Every
    /// observable branch returns one item then completes, which makes it trivial to
    /// assert on the result sequence in a unit test.
    /// </remarks>
    /// <param name="builder">The Akavache builder supplying path resolution and serializer context.</param>
    /// <param name="cacheName">Logical cache-kind name (<c>UserAccount</c> / <c>LocalMachine</c> / <c>Secure</c>).</param>
    /// <param name="enabled">Whether the migration is enabled for this kind in the options.</param>
    /// <param name="sqliteCache">The V11 destination cache, or <see langword="null"/> when the kind isn't a SqliteBlobCache.</param>
    /// <param name="serializer">The current serializer (used by the row-conversion path).</param>
    /// <param name="options">Migration options.</param>
    /// <returns>A one-shot observable that completes when migration for this kind finishes (or is skipped).</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("V10 migration may use reflection to re-serialize entries with their original type.")]
    internal static IObservable<RxVoid> BuildMigration(
        IAkavacheBuilder builder,
        string cacheName,
        bool enabled,
        SqliteBlobCache? sqliteCache,
        ISerializer serializer,
        V10MigrationOptions options)
    {
        if (!enabled || sqliteCache is null)
        {
            return ImmutableReturnRxVoidSignal.Instance;
        }

        var v10Path = GetV10DatabasePath(builder, cacheName);
        return v10Path is null
            ? ImmutableReturnRxVoidSignal.Instance
            : V10MigrationService.Migrate(v10Path, sqliteCache, serializer, options);
    }

    /// <summary>Creates a <see cref="SqliteBlobCache"/> rooted at the legacy V10 directory and filename for the given cache name.</summary>
    /// <param name="cacheName">The logical V11 cache name (e.g., "UserAccount").</param>
    /// <param name="builder">The Akavache builder used to resolve directories and the serializer.</param>
    /// <returns>A <see cref="SqliteBlobCache"/> bound to the legacy V10 file path.</returns>
    /// <exception cref="InvalidOperationException">The legacy cache directory cannot be resolved, or no serializer is registered for the builder's serializer type.</exception>
    internal static SqliteBlobCache CreateV10Cache(string cacheName, IAkavacheBuilder builder)
    {
        var directory = builder.GetLegacyCacheDirectory(cacheName);
        if (directory is null || string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException($"Failed to determine legacy cache directory for '{cacheName}'.");
        }

        // Ensure the cache directory exists
        if (!Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        // Use the V10 filename instead of the V11 name
        var filePath = Path.Combine(directory, V10FileNameMap.GetV10FileName(cacheName));

        var serializer = AppLocator.Current.GetService<ISerializer>(builder.SerializerTypeName)
                         ?? throw new InvalidOperationException($"No serializer of type '{builder.SerializerTypeName}' is registered in the service locator.");

        SqliteBlobCache cache = new(filePath, serializer);

        if (builder.ForcedDateTimeKind.HasValue)
        {
            cache.ForcedDateTimeKind = builder.ForcedDateTimeKind.Value;
        }

        return cache;
    }

    /// <summary>Gets the absolute path to the V10 database file for the given cache name, or <c>null</c> if no legacy directory is available.</summary>
    /// <param name="builder">The Akavache builder used to resolve directories.</param>
    /// <param name="cacheName">The logical V11 cache name.</param>
    /// <returns>The full path to the V10 database file, or <c>null</c> if it cannot be determined.</returns>
    internal static string? GetV10DatabasePath(IAkavacheBuilder builder, string cacheName)
    {
        var directory = builder.GetLegacyCacheDirectory(cacheName);
        return directory is null || string.IsNullOrWhiteSpace(directory) ? null : Path.Combine(directory, V10FileNameMap.GetV10FileName(cacheName));
    }

    /// <summary>Unwraps known secure cache wrappers to retrieve the underlying <see cref="IBlobCache"/>.</summary>
    /// <param name="secureBlobCache">The secure cache to unwrap.</param>
    /// <returns>The underlying blob cache, or <c>null</c> if none can be resolved.</returns>
    internal static IBlobCache? GetUnderlyingBlobCache(ISecureBlobCache? secureBlobCache) => secureBlobCache switch
    {
        IWrappedBlobCache wrappedBlobCache => wrappedBlobCache.InnerCache,
        IBlobCache blobCache => blobCache,
        _ => null,
    };

    /// <summary>Validates that an application name has been configured on the builder.</summary>
    /// <param name="applicationName">The application name to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when the name is null, empty, or whitespace.</exception>
    internal static void ValidateApplicationName(string? applicationName)
    {
        if (!string.IsNullOrWhiteSpace(applicationName))
        {
            return;
        }

        throw new InvalidOperationException("Application name must be set before configuring V10 file names. Call WithApplicationName() first.");
    }
}
