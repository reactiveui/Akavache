// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Helpers;
using Akavache.Settings;
using Splat;

namespace Akavache.Core;

/// <summary>
/// Provides the default implementation of the Akavache builder interface for configuring cache instances.
/// </summary>
internal class AkavacheBuilder : IAkavacheBuilder
{
#if NET9_0_OR_GREATER
    /// <summary>Synchronization primitive guarding serializer registration.</summary>
    private static readonly Lock _lock = new();
#else
    /// <summary>Synchronization primitive guarding serializer registration.</summary>
    private static readonly object _lock = new();
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="AkavacheBuilder"/> class.
    /// </summary>
    /// <remarks>
    /// Sets <see cref="ApplicationRootPath"/> to the parent of
    /// <see cref="AppContext.BaseDirectory"/> and leaves
    /// <see cref="IAkavacheInstance.ExecutingAssembly"/>,
    /// <see cref="IAkavacheInstance.ExecutingAssemblyName"/>, and
    /// <see cref="IAkavacheInstance.Version"/> at their sentinel defaults. Callers
    /// that need assembly metadata must call
    /// <see cref="WithExecutingAssembly(Assembly)"/> with a caller-owned
    /// <see cref="Assembly"/> reference — the AOT-safe path.
    /// </remarks>
    /// <param name="fileLocationOption">The file location strategy.</param>
    public AkavacheBuilder(FileLocationOption fileLocationOption = FileLocationOption.Default)
    {
        FileLocationOption = fileLocationOption;
        ApplicationRootPath = Path.Combine(AppContext.BaseDirectory, "..");
    }

    /// <inheritdoc />
    public Assembly ExecutingAssembly
    {
        get => field ?? typeof(AkavacheBuilder).Assembly;
        private set;
    }

    /// <inheritdoc />
    public string ApplicationName { get; private set; } = "Akavache";

    /// <inheritdoc />
    public string? ApplicationRootPath { get; }

    /// <inheritdoc />
    public string? SettingsCachePath
    {
        // Lazy computation to ensure ApplicationName is properly set via WithApplicationName()
        get => field ??= FileLocationOption switch
        {
            FileLocationOption.Legacy => this.GetLegacyCacheDirectory("SettingsCache"),
            _ => this.GetIsolatedCacheDirectory("SettingsCache"),
        };
        set;
    }

    /// <inheritdoc />
    public string? ExecutingAssemblyName { get; private set; }

    /// <inheritdoc />
    public Version? Version { get; private set; }

    /// <inheritdoc />
    public IBlobCache? InMemory { get; private set; }

    /// <inheritdoc />
    public IBlobCache? LocalMachine { get; private set; }

    /// <inheritdoc />
    public ISecureBlobCache? Secure { get; private set; }

    /// <inheritdoc />
    public IBlobCache? UserAccount { get; private set; }

    /// <inheritdoc />
    public ISerializer? Serializer => AppLocator.Current.GetService<ISerializer>(contract: SerializerTypeName);

    /// <summary>
    /// Gets or sets the forced DateTime kind for DateTime serialization.
    /// When set, all DateTime values will be converted to this kind during cache operations.
    /// </summary>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <summary>
    /// Gets or sets the name of the serializer type.
    /// </summary>
    /// <value>
    /// The name of the serializer type.
    /// </value>
    public string? SerializerTypeName { get; internal set; }

    /// <summary>
    /// Gets the file location option.
    /// </summary>
    /// <value>
    /// The file location option.
    /// </value>
    public FileLocationOption FileLocationOption { get; private set; }

    /// <inheritdoc />
    public IDictionary<string, IBlobCache> BlobCaches { get; } = new Dictionary<string, IBlobCache>();

    /// <inheritdoc />
    public IDictionary<string, ISettingsStorage> SettingsStores { get; } = new Dictionary<string, ISettingsStorage>();

    /// <inheritdoc />
    public IAkavacheBuilder WithLegacyFileLocation()
    {
        FileLocationOption = FileLocationOption.Legacy;
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithApplicationName(string? applicationName)
    {
        // Null or whitespace is treated as a no-op so the default "Akavache" value
        // (set by the constructor) stays in place. The strict path — require a real
        // application name — lives on CacheDatabase.CreateBuilder(string) /
        // CacheDatabase.Initialize<T>(string), which throw on null/whitespace.
        // The null check is split out from the whitespace check so the compiler
        // flow-tracks non-nullness on every TFM (string.IsNullOrWhiteSpace only
        // carries [NotNullWhen(false)] on net6+).
        if (applicationName is null || string.IsNullOrWhiteSpace(applicationName))
        {
            return this;
        }

        ApplicationName = applicationName;
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithExecutingAssembly(Assembly assembly)
    {
        ArgumentExceptionHelper.ThrowIfNull(assembly);

        ExecutingAssembly = assembly;
        ExecutingAssemblyName = assembly.GetName().Name;
        Version = ReadFileVersion(assembly);
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithInMemory(IBlobCache cache)
    {
        ArgumentExceptionHelper.ThrowIfNull(cache);
        InMemory = cache;
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithInMemoryDefaults()
    {
        if (Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using InMemory defaults.");
        }

        UserAccount ??= CreateInMemoryCache();
        LocalMachine ??= CreateInMemoryCache();
        Secure ??= new SecureBlobCacheWrapper(CreateInMemoryCache());
        InMemory ??= CreateInMemoryCache();

        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithLocalMachine(IBlobCache cache)
    {
        ArgumentExceptionHelper.ThrowIfNull(cache);
        LocalMachine = cache;
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithSecure(ISecureBlobCache cache)
    {
        ArgumentExceptionHelper.ThrowIfNull(cache);
        Secure = cache;
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithUserAccount(IBlobCache cache)
    {
        ArgumentExceptionHelper.ThrowIfNull(cache);
        UserAccount = cache;
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithSerializer<T>()
        where T : class, ISerializer, new()
    {
        var serializerType = typeof(T);
        SerializerTypeName = serializerType.AssemblyQualifiedName;
        lock (_lock)
        {
            // Register the serializer if not already registered, we only want one instance of each serializer type
            if (!AppLocator.CurrentMutable.HasRegistration<ISerializer>(contract: SerializerTypeName))
            {
                AppLocator.CurrentMutable.RegisterLazySingleton<ISerializer>(static () => new T(), contract: SerializerTypeName);
            }
        }

        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithSerializer<T>(Func<T> configure)
        where T : class, ISerializer
    {
        var serializerType = typeof(T);
        SerializerTypeName = serializerType.AssemblyQualifiedName;

        lock (_lock)
        {
            // Register the serializer if not already registered, we only want one instance of each serializer type
            if (!AppLocator.CurrentMutable.HasRegistration(serializerType, contract: SerializerTypeName))
            {
                var serializer = configure();
                AppLocator.CurrentMutable.RegisterLazySingleton<ISerializer>(() => serializer, contract: SerializerTypeName);
            }
        }

        return this;
    }

    /// <summary>
    /// Uses the kind of the forced date time.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    public IAkavacheBuilder UseForcedDateTimeKind(DateTimeKind kind)
    {
        ForcedDateTimeKind = kind;
        return this;
    }

    /// <inheritdoc />
    public IAkavacheInstance Build()
    {
        if (Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using InMemory defaults.");
        }

        return this;
    }

    /// <summary>
    /// Reads and parses the <see cref="AssemblyFileVersionAttribute"/> from
    /// <paramref name="assembly"/> into a <see cref="System.Version"/>.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> if the attribute is missing or its value
    /// cannot be parsed. The assembly reference is caller-owned so there is no
    /// reflection-based discovery involved.
    /// </remarks>
    /// <param name="assembly">The caller-supplied assembly.</param>
    /// <returns>The parsed version, or <see langword="null"/>.</returns>
    internal static Version? ReadFileVersion(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyFileVersionAttribute>() is { Version: var version } &&
            Version.TryParse(version, out var parsed)
                ? parsed
                : null;

    /// <summary>
    /// Applies the configured <see cref="ForcedDateTimeKind"/> (if any) to the supplied cache.
    /// </summary>
    /// <param name="cache">The cache to configure.</param>
    internal void ApplyForcedDateTimeKind(IBlobCache cache)
    {
        if (!ForcedDateTimeKind.HasValue)
        {
            return;
        }

        cache.ForcedDateTimeKind = ForcedDateTimeKind.Value;
    }

    /// <summary>
    /// Creates a new <see cref="InMemoryBlobCache"/> using the registered serializer.
    /// </summary>
    /// <returns>The newly created in-memory cache instance.</returns>
    internal InMemoryBlobCache CreateInMemoryCache()
    {
        if (Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using this BlobCache.");
        }

        // Always use Akavache.InMemoryBlobCache from Akavache.Core and pass the serializer
        InMemoryBlobCache cache = new(Serializer);
        ApplyForcedDateTimeKind(cache);
        return cache;
    }
}
