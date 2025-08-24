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
/// Default implementation of IAkavacheBuilder.
/// </summary>
internal class AkavacheBuilder : IAkavacheBuilder
{
    private string _applicationName = "Akavache";

    [SuppressMessage("ExecutingAssembly.Location", "IL3000:String may be null", Justification = "Handled.")]
    public AkavacheBuilder()
    {
        var fileLocation = string.Empty;
        try
        {
            fileLocation = ExecutingAssembly.Location;
        }
        catch (Exception)
        {
            throw;
        }

        if (string.IsNullOrWhiteSpace(fileLocation))
        {
            fileLocation = AppContext.BaseDirectory;
        }

        ExecutingAssemblyName = ExecutingAssembly.FullName!.Split(',')[0];
        ApplicationRootPath = Path.Combine(Path.GetDirectoryName(fileLocation)!, "..");
        SettingsCachePath = Path.Combine(ApplicationRootPath, "SettingsCache");
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(fileLocation);
        Version = new(fileVersionInfo.ProductMajorPart, fileVersionInfo.ProductMinorPart, fileVersionInfo.ProductBuildPart, fileVersionInfo.ProductPrivatePart);

        // Ensure the settings cache directory exists
        Directory.CreateDirectory(SettingsCachePath);
    }

    /// <inheritdoc />
    public Assembly ExecutingAssembly => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

    /// <inheritdoc />
    public string ApplicationName => _applicationName;

    /// <inheritdoc />
    public string? ApplicationRootPath { get; }

    /// <inheritdoc />
    public string? SettingsCachePath { get; set; }

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

    internal static Dictionary<string, IBlobCache?>? BlobCaches { get; set; } = [];

    internal static Dictionary<string, ISettingsStorage?>? SettingsStores { get; set; } = [];

    /// <inheritdoc />
    public IAkavacheBuilder WithApplicationName(string? applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            return this;
        }

        _applicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
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
            throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Serializer = new [SerializerType]() before using InMemory defaults.");
        }

        UserAccount ??= CreateInMemoryCache();
        LocalMachine ??= CreateInMemoryCache();
        Secure ??= new SecureBlobCacheWrapper(CreateInMemoryCache(), Serializer);
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
    public IAkavacheBuilder WithSerializer(ISerializer serializer)
    {
        if (serializer == null)
        {
            throw new ArgumentNullException(nameof(serializer));
        }

        var serializerType = serializer.GetType();
        SerializerTypeName = serializerType.AssemblyQualifiedName;

        // Register the serializer if not already registered, we only want one instance of each serializer type
        if (!AppLocator.CurrentMutable.HasRegistration(serializerType, contract: SerializerTypeName))
        {
            AppLocator.CurrentMutable.RegisterLazySingleton(() => serializer, contract: SerializerTypeName);
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
            throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Serializer = new [SerializerType]() before using InMemory defaults.");
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

    private IBlobCache CreateInMemoryCache()
    {
        if (Serializer == null)
        {
            throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Serializer = new [SerializerType]() before using BlobCache.");
        }

        var serializer = Serializer ?? throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Serializer = new [SerializerType]() before using BlobCache.");
        var serializerType = serializer.GetType();

        // Try to create the appropriate InMemoryBlobCache based on serializer
        if (serializerType.Namespace?.Contains("SystemTextJson") == true)
        {
            var type = Type.GetType("Akavache.InMemoryBlobCache, Akavache.SystemTextJson");
            if (type != null)
            {
                var cache = (IBlobCache)Activator.CreateInstance(type)!;
                ApplyForcedDateTimeKind(cache);
                return cache;
            }
        }
        else if (serializerType.Namespace?.Contains("NewtonsoftJson") == true)
        {
            var type = Type.GetType("Akavache.InMemoryBlobCache, Akavache.NewtonsoftJson");
            if (type != null)
            {
                var cache = (IBlobCache)Activator.CreateInstance(type)!;
                ApplyForcedDateTimeKind(cache);
                return cache;
            }
        }

        throw new InvalidOperationException(
            "No suitable InMemoryBlobCache implementation found. " +
            "Install one of: Akavache.SystemTextJson or Akavache.NewtonsoftJson packages and ensure a serializer is registered.");
    }

    /// <summary>
    /// A wrapper that implements ISecureBlobCache by delegating to an IBlobCache.
    /// </summary>
    private class SecureBlobCacheWrapper(IBlobCache inner, ISerializer serializer) : ISecureBlobCache
    {
        public DateTimeKind? ForcedDateTimeKind
        {
            get => inner.ForcedDateTimeKind;
            set => inner.ForcedDateTimeKind = value;
        }

        public IScheduler Scheduler => inner.Scheduler;

        public ISerializer Serializer => serializer;

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

        public IObservable<Unit> Vacuum() => inner.Vacuum();
    }
}
