// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.IsolatedStorage;
using System.Reflection;
using Akavache.Core;
using Akavache.Helpers;
using Splat;
using Splat.Builder;

namespace Akavache;

/// <summary>
/// Provides extension methods for configuring Akavache cache database with Splat application builders.
/// </summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>
    /// Initializes the CacheDatabase with a custom builder configuration for the specified application.
    /// </summary>
    /// <typeparam name="T">The type of serializer to use for cache operations.</typeparam>
    /// <param name="builder">The Splat application builder to configure.</param>
    /// <param name="configure">An action to configure the Akavache builder settings.</param>
    /// <param name="applicationName">The name of the application for cache directory paths. Must not be null or whitespace.</param>
    /// <returns>The configured Splat application builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static IAppBuilder WithAkavacheCacheDatabase<T>(this IAppBuilder builder, Action<IAkavacheBuilder> configure, string applicationName)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(applicationName);

        CacheDatabase.Initialize<T>(configure, applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes the CacheDatabase with a custom serializer configuration and builder settings.
    /// </summary>
    /// <typeparam name="T">The type of serializer to use for cache operations.</typeparam>
    /// <param name="builder">The Splat application builder to configure.</param>
    /// <param name="configureSerializer">A function that creates and configures the serializer instance.</param>
    /// <param name="configure">An action to configure the Akavache builder settings.</param>
    /// <param name="applicationName">The name of the application for cache directory paths. Must not be null or whitespace.</param>
    /// <returns>The configured Splat application builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static IAppBuilder WithAkavacheCacheDatabase<T>(this IAppBuilder builder, Func<T> configureSerializer, Action<IAkavacheBuilder> configure, string applicationName)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(applicationName);

        CacheDatabase.Initialize(configureSerializer, configure, applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes the CacheDatabase with default in-memory caches and a required application name.
    /// </summary>
    /// <typeparam name="T">The type of serializer to use for cache operations.</typeparam>
    /// <param name="builder">The Splat application builder to configure.</param>
    /// <param name="applicationName">The name of the application for cache directory paths. Must not be null or whitespace.</param>
    /// <returns>The configured Splat application builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static IAppBuilder WithAkavacheCacheDatabase<T>(this IAppBuilder builder, string applicationName)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(applicationName);

        CacheDatabase.Initialize<T>(applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes the CacheDatabase with default in-memory caches, a required application name,
    /// and a custom serializer configuration.
    /// </summary>
    /// <typeparam name="T">The type of serializer to use for cache operations.</typeparam>
    /// <param name="builder">The Splat application builder to configure.</param>
    /// <param name="configureSerializer">A function that creates and configures the serializer instance.</param>
    /// <param name="applicationName">The name of the application for cache directory paths. Must not be null or whitespace.</param>
    /// <returns>The configured Splat application builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static IAppBuilder WithAkavacheCacheDatabase<T>(this IAppBuilder builder, Func<T> configureSerializer, string applicationName)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(applicationName);

        CacheDatabase.Initialize(configureSerializer, applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration and a required application name.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">The application name for cache directory paths. Must not be null or whitespace.</param>
    /// <param name="configure">An action to configure the CacheDatabase builder.</param>
    /// <param name="instance">The instance.</param>
    /// <returns>
    /// The configured builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="configure"/>, or <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static IAppBuilder WithAkavache<T>(this IAppBuilder builder, string applicationName, Action<IAkavacheBuilder> configure, Action<IAkavacheInstance> instance)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentExceptionHelper.ThrowIfNull(configure);
        ArgumentExceptionHelper.ThrowIfNull(instance);

        var akavacheBuilder = CacheDatabase.CreateBuilder(applicationName)
            .WithSerializer<T>();
        configure(akavacheBuilder);
        instance(akavacheBuilder.Build());
        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration and a required application name.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">The application name for cache directory paths. Must not be null or whitespace.</param>
    /// <param name="configure">An action to configure the CacheDatabase builder.</param>
    /// <param name="instance">The instance.</param>
    /// <returns>
    /// The configured builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="configure"/>, or <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static IAppBuilder WithAkavache<T>(this IAppBuilder builder, string applicationName, Action<IAkavacheBuilder> configure, Action<IMutableDependencyResolver, IAkavacheInstance> instance)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentExceptionHelper.ThrowIfNull(configure);
        ArgumentExceptionHelper.ThrowIfNull(instance);

        var akavacheBuilder = CacheDatabase.CreateBuilder(applicationName)
            .WithSerializer<T>();
        configure(akavacheBuilder);

        return builder.WithCustomRegistration(splat => instance(splat, akavacheBuilder.Build()));
    }

    /// <summary>
    /// Initializes CacheDatabase with a set of default in-memory caches and a required application name.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">The application name for cache directory paths. Must not be null or whitespace.</param>
    /// <param name="instance">The instance created.</param>
    /// <returns>
    /// A BlobCache builder for further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static IAppBuilder WithAkavache<T>(this IAppBuilder builder, string applicationName, Action<IAkavacheInstance> instance)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentExceptionHelper.ThrowIfNull(instance);

        instance(CacheDatabase.CreateBuilder(applicationName)
            .WithSerializer<T>()
            .WithInMemoryDefaults().Build());

        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with a set of default in-memory caches and a required application name.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">The application name for cache directory paths. Must not be null or whitespace.</param>
    /// <param name="instance">The instance created.</param>
    /// <returns>
    /// A BlobCache builder for further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="instance"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null or whitespace.</exception>
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public static IAppBuilder WithAkavache<T>(this IAppBuilder builder, string applicationName, Action<IMutableDependencyResolver, IAkavacheInstance> instance)
        where T : class, ISerializer, new()
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentExceptionHelper.ThrowIfNull(instance);

        return builder.WithCustomRegistration(splat => instance(splat, CacheDatabase.CreateBuilder(applicationName)
            .WithSerializer<T>()
            .WithInMemoryDefaults().Build()));
    }

    /// <summary>
    /// Withes the in memory.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
    public static IAkavacheBuilder WithInMemory(this IAkavacheBuilder builder)
    {
        // Ensure the builder is not null
        ArgumentExceptionHelper.ThrowIfNull(builder);

        ArgumentExceptionHelper.ThrowIfNull(builder.SerializerTypeName);

        return builder.WithInMemory(new InMemoryBlobCache(builder.SerializerTypeName));
    }

#if AKAVACHE_MOBILE || AKAVACHE_MOBILE_IOS
    /// <summary>
    /// Gets the isolated cache directory.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="cacheName">Name of the cache.
    /// Any value will use User Store For Assembly.
    /// </param>
    /// <returns>The Isolated cache path.</returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
    /// <exception cref="ArgumentException">
    /// Cache name cannot be null or empty. - cacheName
    /// or
    /// Application name cannot be null or empty. - ApplicationName.
    /// </exception>
#else
    /// <summary>
    /// Gets the isolated cache directory.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="cacheName">Name of the cache.
    /// This will determine the file store to use.
    /// LocalMachine will use Machine Store For Assembly.
    /// UserAccount will use User Store For Assembly.
    /// Secure will use User Store For Assembly.
    /// SettingsCache will use User Store For Assembly.
    /// Any other value will use Machine Store For Assembly.
    /// </param>
    /// <returns>The Isolated cache path.</returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
    /// <exception cref="ArgumentException">
    /// Cache name cannot be null or empty. - cacheName
    /// or
    /// Application name cannot be null or empty. - ApplicationName.
    /// </exception>
#endif
    [ExcludeFromCodeCoverage]
    public static string? GetIsolatedCacheDirectory(this IAkavacheInstance builder, string cacheName)
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(cacheName);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(builder.ApplicationName);

        // Validate input to prevent path traversal attacks
        var validatedCacheName = SecurityUtilities.ValidateCacheName(cacheName, nameof(cacheName));
        var validatedApplicationName = SecurityUtilities.ValidateApplicationName(builder.ApplicationName, nameof(builder.ApplicationName));

        string? cachePath = null;
        var store = validatedCacheName switch
        {
            "UserAccount" => IsolatedStorageFile.GetUserStoreForAssembly(),
            "Secure" => IsolatedStorageFile.GetUserStoreForAssembly(),
            "SettingsCache" => IsolatedStorageFile.GetUserStoreForAssembly(),
#if AKAVACHE_MOBILE || AKAVACHE_MOBILE_IOS
            _ => IsolatedStorageFile.GetUserStoreForAssembly(),
#else
            // On Unix systems (macOS/Linux), machine store may fail due to read-only /usr/share/IsolatedStorage
            // Fall back to user store to avoid permission issues in unit tests and restricted environments
            _ => Environment.OSVersion.Platform == PlatformID.Unix
                ? IsolatedStorageFile.GetUserStoreForAssembly()
                : IsolatedStorageFile.GetMachineStoreForAssembly(),
#endif
        };

        // Compute CachePath under a writable location (fix iOS bundle write attempt)
        using var isoStore = store;

        // Try to get a path within isolated storage for the settings cache using the application name
        try
        {
            if (isoStore != null)
            {
                var isoPath = Path.Combine(validatedApplicationName, validatedCacheName);

                // Ensure the directory exists
                if (!isoStore.DirectoryExists(isoPath))
                {
                    isoStore.CreateDirectory(isoPath);
                }

                if (isoStore.DirectoryExists(isoPath))
                {
                    isoStore.GetDirectoryNames(isoPath);
                    cachePath = Path.Combine(isoStore.GetType().GetProperty("RootDirectory", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(isoStore)?.ToString() ?? string.Empty, isoPath);
                }
            }
        }
        catch
        {
            // Ignore isolated storage exceptions and fall back to local app data path
        }

        return cachePath;
    }

    /// <summary>
    /// Gets the legacy cache directory.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="cacheName">Name of the cache.</param>
    /// <returns>The Legacy cache path.</returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
    /// <exception cref="ArgumentException">
    /// Cache name cannot be null or empty. - cacheName
    /// or
    /// Application name cannot be null or empty. - ApplicationName.
    /// </exception>
    [ExcludeFromCodeCoverage]
    public static string? GetLegacyCacheDirectory(this IAkavacheInstance builder, string cacheName)
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(cacheName);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(builder.ApplicationName);

#if ANDROID
        switch (cacheName)
        {
            case "LocalMachine":
                return Application.Context.CacheDir?.AbsolutePath;
            case "Secure":
                {
                    var path = Application.Context.FilesDir?.AbsolutePath;

                    if (path is null)
                    {
                        return null;
                    }

                    DirectoryInfo di = new(Path.Combine(path, "Secret"));
                    if (!di.Exists)
                    {
                        di.CreateRecursive();
                    }

                    return di.FullName;
                }

            default:
                // Use the cache directory for UserAccount and SettingsCache caches
                return Application.Context.FilesDir?.AbsolutePath;
        }
#elif IOS || MACCATALYST
        return cacheName switch
        {
            "LocalMachine" => CreateAppDirectory(NSSearchPathDirectory.CachesDirectory, builder.ApplicationName, "BlobCache"),
            "Secure" => CreateAppDirectory(NSSearchPathDirectory.ApplicationSupportDirectory, builder.ApplicationName, "SecretCache"),
            _ => CreateAppDirectory(NSSearchPathDirectory.ApplicationSupportDirectory, builder.ApplicationName, "BlobCache"),
        };
#else
        return cacheName switch
        {
            "LocalMachine" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), builder.ApplicationName, "BlobCache"),
            "Secure" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), builder.ApplicationName, "SecretCache"),
            _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), builder.ApplicationName, "BlobCache"),
        };
#endif
    }

    /// <summary>
    /// Recursively creates all directories along the full path of the supplied <see cref="DirectoryInfo"/>.
    /// </summary>
    /// <param name="directoryInfo">The directory whose full path should be created on disk.</param>
    internal static void CreateRecursive(this DirectoryInfo directoryInfo) =>
        _ = directoryInfo.SplitFullPath().Aggregate(static (parent, dir) =>
        {
            var path = Path.Combine(parent, dir);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        });

    /// <summary>
    /// Splits the full path of the supplied <see cref="DirectoryInfo"/> into its individual
    /// path components in root-down order. Uses <c>yield return</c> so the common
    /// <see cref="Enumerable.Aggregate{TSource}"/> consumer never sees a materialised list.
    /// </summary>
    /// <param name="directoryInfo">The directory whose full path will be split.</param>
    /// <returns>The ordered path components, beginning with the root.</returns>
    internal static IEnumerable<string> SplitFullPath(this DirectoryInfo directoryInfo)
    {
        var fullName = directoryInfo.FullName;
        var root = Path.GetPathRoot(fullName);

        // Walk once from leaf to root to know the depth — cheap (just string comparisons +
        // GetDirectoryName) and lets us pre-size a stack-like char-sized cursor below.
        var depth = 0;
        for (var path = fullName; path != root && path is not null; path = Path.GetDirectoryName(path))
        {
            var filename = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(filename))
            {
                depth++;
            }
        }

        return SplitFullPathIterator(fullName, root, depth);

        static IEnumerable<string> SplitFullPathIterator(string fullName, string? root, int depth)
        {
            if (root is not null)
            {
                yield return root;
            }

            if (depth == 0)
            {
                yield break;
            }

            // Second pass materialises components leaf-to-root into a local buffer so we can
            // emit them root-to-leaf without the old List allocation + Reverse step.
            var components = new string[depth];
            var index = depth - 1;
            for (var path = fullName; path != root && path is not null && index >= 0; path = Path.GetDirectoryName(path))
            {
                var filename = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(filename))
                {
                    components[index--] = filename;
                }
            }

            for (var i = 0; i < components.Length; i++)
            {
                yield return components[i];
            }
        }
    }

#if IOS || MACCATALYST
    /// <summary>
    /// Creates a per-application directory beneath a system search-path directory on Apple platforms.
    /// </summary>
    /// <param name="targetDir">The platform search-path directory to use as the parent.</param>
    /// <param name="applicationName">The application name segment to use within the path.</param>
    /// <param name="subDir">The leaf cache sub-directory name.</param>
    /// <returns>The fully qualified path of the created directory.</returns>
    internal static string CreateAppDirectory(NSSearchPathDirectory targetDir, string applicationName, string subDir = "BlobCache")
    {
        using var fm = new NSFileManager();
        var url = fm.GetUrl(targetDir, NSSearchPathDomain.All, null, true, out _) ?? throw new DirectoryNotFoundException();
        var rp = url.RelativePath ?? throw new DirectoryNotFoundException();
        var ret = Path.Combine(rp, applicationName, subDir);

        var di = new DirectoryInfo(ret);
        if (!di.Exists)
        {
            di.CreateRecursive();
        }

        return ret;
    }
#endif
}
