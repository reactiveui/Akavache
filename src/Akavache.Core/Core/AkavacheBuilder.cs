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

    /// <summary>Cached settings cache directory path, computed lazily.</summary>
    private string? _settingsCachePath;

    /// <summary>The file location strategy chosen by the builder.</summary>
    private FileLocationOption _fileLocationOption;

    /// <summary>Caller-supplied executing assembly, or <see langword="null"/> if not set.</summary>
    private Assembly? _explicitExecutingAssembly;

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
        _fileLocationOption = fileLocationOption;
        ApplicationRootPath = Path.Combine(AppContext.BaseDirectory, "..");
    }

    /// <inheritdoc />
    public Assembly ExecutingAssembly => _explicitExecutingAssembly ?? typeof(AkavacheBuilder).Assembly;

    /// <inheritdoc />
    public string ApplicationName { get; private set; } = "Akavache";

    /// <inheritdoc />
    public string? ApplicationRootPath { get; }

    /// <inheritdoc />
    public string? SettingsCachePath
    {
        get
        {
            // Lazy computation to ensure ApplicationName is properly set via WithApplicationName()
            _settingsCachePath ??= _fileLocationOption switch
                {
                    FileLocationOption.Legacy => this.GetLegacyCacheDirectory("SettingsCache"),
                    _ => this.GetIsolatedCacheDirectory("SettingsCache"),
                };

            return _settingsCachePath;
        }
        set => _settingsCachePath = value;
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
    public IHttpService? HttpService { get; set; } = new HttpService();

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
    public FileLocationOption FileLocationOption => _fileLocationOption;

    /// <summary>
    /// Gets or sets the registry of named blob caches created by builders.
    /// </summary>
    internal static Dictionary<string, IBlobCache?>? BlobCaches { get; set; } = [];

    /// <summary>
    /// Gets or sets the registry of named settings stores created by builders.
    /// </summary>
    internal static Dictionary<string, ISettingsStorage?>? SettingsStores { get; set; } = [];

    /// <inheritdoc />
    public IAkavacheBuilder WithLegacyFileLocation()
    {
        _fileLocationOption = FileLocationOption.Legacy;
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

        _explicitExecutingAssembly = assembly;
        ExecutingAssemblyName = assembly.GetName().Name;
        Version = ReadFileVersion(assembly);
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithInMemory(IBlobCache cache)
    {
        InMemory = cache ?? throw new ArgumentNullException(nameof(cache));
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
        LocalMachine = cache ?? throw new ArgumentNullException(nameof(cache));
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithSecure(ISecureBlobCache cache)
    {
        Secure = cache ?? throw new ArgumentNullException(nameof(cache));
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithUserAccount(IBlobCache cache)
    {
        UserAccount = cache ?? throw new ArgumentNullException(nameof(cache));
        return this;
    }

    /// <inheritdoc />
    public IAkavacheBuilder WithSerializer<T>()
        where T : ISerializer, new()
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
        where T : ISerializer
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
    internal static Version? ReadFileVersion(Assembly assembly)
    {
        var versionAttr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        if (versionAttr is null)
        {
            return null;
        }

        return Version.TryParse(versionAttr.Version, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Applies the configured <see cref="ForcedDateTimeKind"/> (if any) to the supplied cache.
    /// </summary>
    /// <param name="cache">The cache to configure.</param>
    internal void ApplyForcedDateTimeKind(IBlobCache cache)
    {
        if (ForcedDateTimeKind.HasValue)
        {
            cache.ForcedDateTimeKind = ForcedDateTimeKind.Value;
        }
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
        var cache = new InMemoryBlobCache(Serializer);
        ApplyForcedDateTimeKind(cache);
        return cache;
    }

    /// <summary>
    /// A wrapper that implements ISecureBlobCache by delegating to an IBlobCache.
    /// </summary>
    /// <param name="inner">The underlying blob cache to delegate to.</param>
    private class SecureBlobCacheWrapper(IBlobCache inner) : ISecureBlobCache
    {
        /// <inheritdoc />
        public DateTimeKind? ForcedDateTimeKind
        {
            get => inner.ForcedDateTimeKind;
            set => inner.ForcedDateTimeKind = value;
        }

        /// <inheritdoc />
        public IScheduler Scheduler => inner.Scheduler;

        /// <inheritdoc />
        public ISerializer Serializer => inner.Serializer;

        /// <inheritdoc />
        public IHttpService HttpService
        {
            get => inner.HttpService;
            set => inner.HttpService = value;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (inner is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        }

        /// <inheritdoc />
        public IObservable<Unit> Flush() => inner.Flush();

        /// <inheritdoc />
        public IObservable<Unit> Flush(Type type) => inner.Flush(type);

        /// <inheritdoc />
        public IObservable<byte[]?> Get(string key) => inner.Get(key);

        /// <inheritdoc />
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => inner.Get(keys);

        /// <inheritdoc />
        public IObservable<byte[]?> Get(string key, Type type) => inner.Get(key, type);

        /// <inheritdoc />
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => inner.Get(keys, type);

        /// <inheritdoc />
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => inner.GetAll(type);

        /// <inheritdoc />
        public IObservable<string> GetAllKeys() => inner.GetAllKeys();

        /// <inheritdoc />
        public IObservable<string> GetAllKeys(Type type) => inner.GetAllKeys(type);

        /// <inheritdoc />
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => inner.GetCreatedAt(keys);

        /// <inheritdoc />
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => inner.GetCreatedAt(key);

        /// <inheritdoc />
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => inner.GetCreatedAt(keys, type);

        /// <inheritdoc />
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => inner.GetCreatedAt(key, type);

        /// <inheritdoc />
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) =>
                                                                                                                                    inner.Insert(keyValuePairs, absoluteExpiration);

        /// <inheritdoc />
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            inner.Insert(key, data, absoluteExpiration);

        /// <inheritdoc />
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) =>
            inner.Insert(keyValuePairs, type, absoluteExpiration);

        /// <inheritdoc />
        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
            inner.Insert(key, data, type, absoluteExpiration);

        /// <inheritdoc />
        public IObservable<Unit> Invalidate(string key) => inner.Invalidate(key);

        /// <inheritdoc />
        public IObservable<Unit> Invalidate(string key, Type type) => inner.Invalidate(key, type);

        /// <inheritdoc />
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => inner.Invalidate(keys);

        /// <inheritdoc />
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => inner.Invalidate(keys, type);

        /// <inheritdoc />
        public IObservable<Unit> InvalidateAll(Type type) => inner.InvalidateAll(type);

        /// <inheritdoc />
        public IObservable<Unit> InvalidateAll() => inner.InvalidateAll();

        /// <inheritdoc />
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) => inner.UpdateExpiration(key, absoluteExpiration);

        /// <inheritdoc />
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) => inner.UpdateExpiration(key, type, absoluteExpiration);

        /// <inheritdoc />
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) => inner.UpdateExpiration(keys, absoluteExpiration);

        /// <inheritdoc />
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) => inner.UpdateExpiration(keys, type, absoluteExpiration);

        /// <inheritdoc />
        public IObservable<Unit> Vacuum() => inner.Vacuum();
    }
}
