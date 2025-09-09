// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.IO;

#endif
using System.IO.IsolatedStorage;
using System.Reflection;
using Akavache.Core;
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
    /// <param name="applicationName">The name of the application for cache directory paths.</param>
    /// <returns>The configured Splat application builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static IAppBuilder WithAkavacheCacheDatabase<T>(this IAppBuilder builder, Action<IAkavacheBuilder> configure, string? applicationName = null)
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

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
    /// <param name="applicationName">The name of the application for cache directory paths.</param>
    /// <returns>The configured Splat application builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static IAppBuilder WithAkavacheCacheDatabase<T>(this IAppBuilder builder, Func<T> configureSerializer, Action<IAkavacheBuilder> configure, string? applicationName = null)
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        CacheDatabase.Initialize(configureSerializer, configure, applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes the CacheDatabase with default in-memory caches.
    /// This is the safest default configuration as it does not require any additional packages.
    /// </summary>
    /// <typeparam name="T">The type of serializer to use for cache operations.</typeparam>
    /// <param name="builder">The Splat application builder to configure.</param>
    /// <param name="applicationName">The name of the application for cache directory paths. If null, uses the current ApplicationName.</param>
    /// <returns>The configured Splat application builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static IAppBuilder WithAkavacheCacheDatabase<T>(this IAppBuilder builder, string? applicationName = null)
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        CacheDatabase.Initialize<T>(applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes the CacheDatabase with default in-memory caches and a custom serializer configuration.
    /// This is a safe default configuration that does not require any additional packages.
    /// </summary>
    /// <typeparam name="T">The type of serializer to use for cache operations.</typeparam>
    /// <param name="builder">The Splat application builder to configure.</param>
    /// <param name="configureSerializer">A function that creates and configures the serializer instance.</param>
    /// <param name="applicationName">The name of the application for cache directory paths. If null, uses the current ApplicationName.</param>
    /// <returns>The configured Splat application builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static IAppBuilder WithAkavacheCacheDatabase<T>(this IAppBuilder builder, Func<T> configureSerializer, string? applicationName = null)
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        CacheDatabase.Initialize(configureSerializer, applicationName);

        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="configure">An action to configure the CacheDatabase builder.</param>
    /// <param name="instance">The instance.</param>
    /// <returns>
    /// The configured builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder
    /// or
    /// configure
    /// or
    /// instance.</exception>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static IAppBuilder WithAkavache<T>(this IAppBuilder builder, string? applicationName, Action<IAkavacheBuilder> configure, Action<IAkavacheInstance> instance)

        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var akavacheBuilder = CacheDatabase.CreateBuilder()
            .WithApplicationName(applicationName)
            .WithSerializer<T>();
        configure(akavacheBuilder);
        instance(akavacheBuilder.Build());
        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with a custom builder configuration.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="configure">An action to configure the CacheDatabase builder.</param>
    /// <param name="instance">The instance.</param>
    /// <returns>
    /// The configured builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder
    /// or
    /// configure
    /// or
    /// instance.</exception>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static IAppBuilder WithAkavache<T>(this IAppBuilder builder, string? applicationName, Action<IAkavacheBuilder> configure, Action<IMutableDependencyResolver, IAkavacheInstance> instance)
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var akavacheBuilder = CacheDatabase.CreateBuilder()
            .WithApplicationName(applicationName)
            .WithSerializer<T>();
        configure(akavacheBuilder);

        return builder.WithCustomRegistration(splat => instance(splat, akavacheBuilder.Build()));
    }

    /// <summary>
    /// Initializes CacheDatabase with a set of default in-memory caches.
    /// This is the safest default as it doesn't require any additional packages.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">The application name for cache directories. If null, uses the current ApplicationName.</param>
    /// <param name="instance">The instance created.</param>
    /// <returns>
    /// A BlobCache builder for further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static IAppBuilder WithAkavache<T>(this IAppBuilder builder, string? applicationName, Action<IAkavacheInstance> instance)
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        instance(CacheDatabase.CreateBuilder()
            .WithApplicationName(applicationName)
            .WithSerializer<T>()
            .WithInMemoryDefaults().Build());

        return builder;
    }

    /// <summary>
    /// Initializes CacheDatabase with a set of default in-memory caches.
    /// This is the safest default as it doesn't require any additional packages.
    /// </summary>
    /// <typeparam name="T">The type of Serializer.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="applicationName">The application name for cache directories. If null, uses the current ApplicationName.</param>
    /// <param name="instance">The instance created.</param>
    /// <returns>
    /// A BlobCache builder for further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
#endif
    public static IAppBuilder WithAkavache<T>(this IAppBuilder builder, string? applicationName, Action<IMutableDependencyResolver, IAkavacheInstance> instance)
        where T : ISerializer, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        return builder.WithCustomRegistration(splat => instance(splat, CacheDatabase.CreateBuilder()
            .WithApplicationName(applicationName)
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
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.SerializerTypeName == null)
        {
            throw new InvalidOperationException("A serializer must be configured before using in-memory cache.");
        }

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
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    /// <exception cref="System.ArgumentException">
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
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    /// <exception cref="System.ArgumentException">
    /// Cache name cannot be null or empty. - cacheName
    /// or
    /// Application name cannot be null or empty. - ApplicationName.
    /// </exception>
#endif
    public static string? GetIsolatedCacheDirectory(this IAkavacheInstance builder, string cacheName)
    {
        // Ensure the builder is not null
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrWhiteSpace(cacheName))
        {
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(cacheName));
        }

        if (string.IsNullOrWhiteSpace(builder.ApplicationName))
        {
            throw new ArgumentException("Application name cannot be null or empty.", nameof(builder.ApplicationName));
        }

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
        using (var isoStore = store)
        {
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
                        var dirNames = isoStore.GetDirectoryNames(isoPath);
                        cachePath = Path.Combine(isoStore.GetType().GetProperty("RootDirectory", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(isoStore)?.ToString() ?? string.Empty, isoPath);
                    }
                }
            }
            catch
            {
                // Ignore isolated storage exceptions and fall back to local app data path
            }
        }

        return cachePath;
    }

    /// <summary>
    /// Gets the legacy cache directory.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="cacheName">Name of the cache.</param>
    /// <returns>The Legacy cache path.</returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    /// <exception cref="System.ArgumentException">
    /// Cache name cannot be null or empty. - cacheName
    /// or
    /// Application name cannot be null or empty. - ApplicationName.
    /// </exception>
    public static string? GetLegacyCacheDirectory(this IAkavacheInstance builder, string cacheName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrWhiteSpace(cacheName))
        {
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(cacheName));
        }

        if (string.IsNullOrWhiteSpace(builder.ApplicationName))
        {
            throw new ArgumentException("Application name cannot be null or empty.", nameof(builder.ApplicationName));
        }

#if ANDROID
        switch (cacheName)
        {
            case "LocalMachine":
                return Application.Context.CacheDir?.AbsolutePath;
            case "Secure":
                var path = Application.Context.FilesDir?.AbsolutePath;

                if (path is null)
                {
                    return null;
                }

                var di = new DirectoryInfo(Path.Combine(path, "Secret"));
                if (!di.Exists)
                {
                    di.CreateRecursive();
                }

                return di.FullName;
            default:
                // Use the cache directory for UserAccount and SettingsCache caches
                return Application.Context.FilesDir?.AbsolutePath;
        }
#elif IOS || MACCATALYST
        return cacheName switch
        {
            "LocalMachine" => (string)CreateAppDirectory(NSSearchPathDirectory.CachesDirectory, builder.ApplicationName, "BlobCache"),
            "Secure" => (string)CreateAppDirectory(NSSearchPathDirectory.ApplicationSupportDirectory, builder.ApplicationName, "SecretCache"),
            _ => (string)CreateAppDirectory(NSSearchPathDirectory.ApplicationSupportDirectory, builder.ApplicationName, "BlobCache"),
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

    internal static void CreateRecursive(this DirectoryInfo directoryInfo) =>
        _ = directoryInfo.SplitFullPath().Aggregate((parent, dir) =>
        {
            var path = Path.Combine(parent, dir);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        });

    internal static IEnumerable<string> SplitFullPath(this DirectoryInfo directoryInfo)
    {
        var root = Path.GetPathRoot(directoryInfo.FullName);
        var components = new List<string>();
        for (var path = directoryInfo.FullName; path != root && path is not null; path = Path.GetDirectoryName(path))
        {
            var filename = Path.GetFileName(path);
            if (string.IsNullOrEmpty(filename))
            {
                continue;
            }

            components.Add(filename);
        }

        if (root is not null)
        {
            components.Add(root);
        }

        components.Reverse();
        return components;
    }

#if IOS || MACCATALYST
    private static string CreateAppDirectory(NSSearchPathDirectory targetDir, string applicationName, string subDir = "BlobCache")
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
