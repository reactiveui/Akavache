// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

#if REACTIVE_SHIM
namespace Akavache.Reactive.V10toV11;
#else
namespace Akavache.V10toV11;
#endif

/// <summary>Internal helpers that create, unwrap and validate caches for the V10 compatibility and migration paths.</summary>
internal static class V10CacheExtensions
{
    /// <summary>Extension members for <c>ISecureBlobCache?</c>.</summary>
    /// <param name="secureBlobCache">The secure cache, possibly a wrapper, to unwrap.</param>
    extension(ISecureBlobCache? secureBlobCache)
    {
        /// <summary>Unwraps known secure cache wrappers to retrieve the underlying <see cref="IBlobCache"/>.</summary>
        /// <returns>The underlying blob cache, or <c>null</c> if none can be resolved.</returns>
        internal IBlobCache? GetUnderlyingBlobCache() => secureBlobCache switch
        {
            IWrappedBlobCache wrappedBlobCache => wrappedBlobCache.InnerCache,
            IBlobCache blobCache => blobCache,
            _ => null,
        };
    }

    /// <summary>Extension members for <c>string</c>.</summary>
    /// <param name="cacheName">The V11 cache slot name to map onto its V10 file.</param>
    extension(string cacheName)
    {
        /// <summary>Creates a <see cref="SqliteBlobCache"/> rooted at the legacy V10 directory and filename for the given cache name.</summary>
        /// <param name="builder">The Akavache builder used to resolve directories and the serializer.</param>
        /// <returns>A <see cref="SqliteBlobCache"/> bound to the legacy V10 file path.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <c>directory is null || string.IsNullOrWhiteSpace(directory)</c>.</exception>
        internal SqliteBlobCache CreateV10Cache(IAkavacheBuilder builder)
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
    }

    /// <summary>Extension members for <c>string?</c>.</summary>
    /// <param name="applicationName">The application name configured on the builder.</param>
    extension(string? applicationName)
    {
        /// <summary>Validates that an application name has been configured on the builder.</summary>
        /// <exception cref="InvalidOperationException">Thrown when the name is null, empty, or whitespace.</exception>
        internal void ValidateApplicationName()
        {
            if (!string.IsNullOrWhiteSpace(applicationName))
            {
                return;
            }

            throw new InvalidOperationException("Application name must be set before configuring V10 file names. Call WithApplicationName() first.");
        }
    }
}
