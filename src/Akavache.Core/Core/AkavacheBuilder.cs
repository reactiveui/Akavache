// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Akavache.Settings;
using Splat;

namespace Akavache.Core;

/// <summary>
/// Provides the default implementation of the Akavache builder interface for configuring cache instances.
/// </summary>
internal class AkavacheBuilder : IAkavacheBuilder
{
    private static readonly object _lock = new();
    private string? _settingsCachePath;
    private FileLocationOption _fileLocationOption;

    [SuppressMessage("ExecutingAssembly.Location", "IL3000:String may be null", Justification = "Handled.")]
    public AkavacheBuilder(FileLocationOption fileLocationOption = FileLocationOption.Default)
    {
        _fileLocationOption = fileLocationOption;
        try
        {
            ExecutingAssemblyName = ExecutingAssembly.FullName!.Split(',')[0];
            string? fileLocation = null;
            try
            {
                fileLocation = ExecutingAssembly.Location;
            }
            catch
            {
            }

            if (string.IsNullOrWhiteSpace(fileLocation))
            {
                fileLocation = AppContext.BaseDirectory;
            }

            ApplicationRootPath = Path.Combine(Path.GetDirectoryName(fileLocation)!, "..");

            // Additional validation before calling FileVersionInfo.GetVersionInfo to prevent Android crashes
            if (!string.IsNullOrWhiteSpace(fileLocation) && File.Exists(fileLocation))
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(fileLocation);
                Version = new(fileVersionInfo.ProductMajorPart, fileVersionInfo.ProductMinorPart, fileVersionInfo.ProductBuildPart, fileVersionInfo.ProductPrivatePart);
            }
        }
        catch
        {
            // Ignore exceptions and leave Version and ApplicationRootPath as null
        }

        // SettingsCachePath will be computed lazily when first accessed to ensure ApplicationName is properly set
    }

    /// <inheritdoc />
    public Assembly ExecutingAssembly => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

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
    public string? ExecutingAssemblyName { get; }

    /// <inheritdoc />
    public Version? Version { get; }

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

    internal static Dictionary<string, IBlobCache?>? BlobCaches { get; set; } = [];

    internal static Dictionary<string, ISettingsStorage?>? SettingsStores { get; set; } = [];

    /// <inheritdoc />
    public IAkavacheBuilder WithApplicationName(string? applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            return this;
        }

        ApplicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
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
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public IAkavacheBuilder WithSerializer<T>()
#else
    public IAkavacheBuilder WithSerializer<T>()
#endif
        where T : ISerializer, new()
    {
        var serializerType = typeof(T);
        SerializerTypeName = serializerType.AssemblyQualifiedName;
        lock (_lock)
        {
            // Register the serializer if not already registered, we only want one instance of each serializer type
            if (!AppLocator.CurrentMutable.HasRegistration(typeof(ISerializer), contract: SerializerTypeName))
            {
                AppLocator.CurrentMutable.RegisterLazySingleton<ISerializer>(static () => new T(), contract: SerializerTypeName);
            }
        }

        return this;
    }

    /// <inheritdoc />
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
    public IAkavacheBuilder WithSerializer<T>(Func<T> configure)
#else
    public IAkavacheBuilder WithSerializer<T>(Func<T> configure)
#endif
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

    private void ApplyForcedDateTimeKind(IBlobCache cache)
    {
        if (ForcedDateTimeKind.HasValue)
        {
            cache.ForcedDateTimeKind = ForcedDateTimeKind.Value;
        }
    }

    private InMemoryBlobCache CreateInMemoryCache()
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
    private class SecureBlobCacheWrapper(IBlobCache inner) : ISecureBlobCache
    {
        public DateTimeKind? ForcedDateTimeKind
        {
            get => inner.ForcedDateTimeKind;
            set => inner.ForcedDateTimeKind = value;
        }

        public IScheduler Scheduler => inner.Scheduler;

        public ISerializer Serializer => inner.Serializer;

        public IHttpService HttpService
        {
            get => inner.HttpService;
            set => inner.HttpService = value;
        }

        public void Dispose()
        {
            if (inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (inner is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public IObservable<Unit> Flush() => inner.Flush();

        public IObservable<Unit> Flush(Type type) => inner.Flush(type);

        public IObservable<byte[]?> Get(string key) => inner.Get(key);

        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => inner.Get(keys);

        public IObservable<byte[]?> Get(string key, Type type) => inner.Get(key, type);

        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => inner.Get(keys, type);

        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => inner.GetAll(type);

        public IObservable<string> GetAllKeys() => inner.GetAllKeys();

        public IObservable<string> GetAllKeys(Type type) => inner.GetAllKeys(type);

        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => inner.GetCreatedAt(keys);

        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => inner.GetCreatedAt(key);

        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => inner.GetCreatedAt(keys, type);

        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => inner.GetCreatedAt(key, type);

        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) =>
                                                                                                                                    inner.Insert(keyValuePairs, absoluteExpiration);

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            inner.Insert(key, data, absoluteExpiration);

        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) =>
            inner.Insert(keyValuePairs, type, absoluteExpiration);

        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
            inner.Insert(key, data, type, absoluteExpiration);

        public IObservable<Unit> Invalidate(string key) => inner.Invalidate(key);

        public IObservable<Unit> Invalidate(string key, Type type) => inner.Invalidate(key, type);

        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => inner.Invalidate(keys);

        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => inner.Invalidate(keys, type);

        public IObservable<Unit> InvalidateAll(Type type) => inner.InvalidateAll(type);

        public IObservable<Unit> InvalidateAll() => inner.InvalidateAll();

        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) => inner.UpdateExpiration(key, absoluteExpiration);

        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) => inner.UpdateExpiration(key, type, absoluteExpiration);

        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) => inner.UpdateExpiration(keys, absoluteExpiration);

        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) => inner.UpdateExpiration(keys, type, absoluteExpiration);

        public IObservable<Unit> Vacuum() => inner.Vacuum();
    }
}
