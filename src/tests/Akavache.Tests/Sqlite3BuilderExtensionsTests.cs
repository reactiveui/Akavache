// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Core;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.Sqlite3.AkavacheBuilderExtensions (non-encrypted).
/// </summary>
[Category("Akavache")]
[NotInParallel(["CacheDatabaseState", "NativeSqlite"])]
public class Sqlite3BuilderExtensionsTests
{
    /// <summary>
    /// Tests WithSqliteProvider() throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteProviderShouldThrowOnNullBuilder() =>
        await Assert.That(static () => Sqlite3.AkavacheBuilderExtensions.WithSqliteProvider(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithSqliteProvider() initializes the SQLite provider.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteProviderShouldInitialize()
    {
        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CreateBuilder("WithSqliteProviderInit");
        var result = builder.WithSqliteProvider();
        await Assert.That(result).IsSameReferenceAs(builder);
    }

    /// <summary>
    /// Tests WithSqliteProvider() is idempotent (second call is a no-op).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteProviderShouldBeIdempotent()
    {
        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CreateBuilder("WithSqliteProviderIdempotent");
        builder.WithSqliteProvider();
        var result = builder.WithSqliteProvider();
        await Assert.That(result).IsSameReferenceAs(builder);
    }

    /// <summary>
    /// Tests WithSqliteDefaults() throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteDefaultsShouldThrowOnNullBuilder() =>
        await Assert.That(static () => Sqlite3.AkavacheBuilderExtensions.WithSqliteDefaults(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithSqliteDefaults() throws when no serializer is registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteDefaultsShouldThrowWhenNoSerializer()
    {
        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CreateBuilder("WithSqliteDefaultsNoSerializer");
        await Assert.That(() => builder.WithSqliteDefaults())
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests WithSqliteDefaults() throws when application name is empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteDefaultsShouldThrowWhenApplicationNameEmpty()
    {
        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        SystemJsonSerializer serializer = new();
        FakeAkavacheBuilder builder = new()
        {
            ApplicationName = string.Empty,
            Serializer = serializer,
            SerializerTypeName = typeof(SystemJsonSerializer).AssemblyQualifiedName,
        };

        await Assert.That(() => builder.WithSqliteDefaults())
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests WithSqliteDefaults() creates all SQLite-backed caches on the happy path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteDefaultsShouldCreateCaches()
    {
        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        using (Utility.WithEmptyDirectory(out _))
        {
            var builder = CacheDatabase.CreateBuilder()
                .WithApplicationName($"WithSqliteDefaultsTest_{Guid.NewGuid():N}")
                .WithSerializer<SystemJsonSerializer>();

            var result = builder.WithSqliteDefaults();

            try
            {
                await Assert.That(result).IsSameReferenceAs(builder);
                await Assert.That(builder.UserAccount).IsNotNull();
                await Assert.That(builder.LocalMachine).IsNotNull();
                await Assert.That(builder.Secure).IsNotNull();
                await Assert.That(builder.InMemory).IsNotNull();
            }
            finally
            {
                if (builder.UserAccount is IAsyncDisposable uaDisposable)
                {
                    await uaDisposable.DisposeAsync();
                }

                if (builder.LocalMachine is IAsyncDisposable lmDisposable)
                {
                    await lmDisposable.DisposeAsync();
                }

                if (builder.Secure is IAsyncDisposable sDisposable)
                {
                    await sDisposable.DisposeAsync();
                }

                if (builder.InMemory is IAsyncDisposable imDisposable)
                {
                    await imDisposable.DisposeAsync();
                }
            }
        }
    }

    /// <summary>
    /// Tests CreateSqliteCache throws on empty cache name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateSqliteCacheShouldThrowOnEmptyName()
    {
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName("CreateSqliteCacheEmptyName")
            .WithSerializer<SystemJsonSerializer>();

        await Assert.That(() => Sqlite3.AkavacheBuilderExtensions.CreateSqliteCache(string.Empty, builder))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests CreateSqliteCache throws when no serializer is registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateSqliteCacheShouldThrowWhenNoSerializer()
    {
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName("CreateSqliteCacheNoSerializer");

        await Assert.That(() => Sqlite3.AkavacheBuilderExtensions.CreateSqliteCache("UserAccount", builder))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests CreateSqliteCache throws when application name is empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateSqliteCacheShouldThrowWhenApplicationNameEmpty()
    {
        SystemJsonSerializer serializer = new();
        FakeAkavacheBuilder builder = new()
        {
            ApplicationName = string.Empty,
            Serializer = serializer,
            SerializerTypeName = typeof(SystemJsonSerializer).AssemblyQualifiedName,
        };

        await Assert.That(() => Sqlite3.AkavacheBuilderExtensions.CreateSqliteCache("UserAccount", builder))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests CreateSqliteCache creates a cache on the happy path with legacy file location
    /// and a forced DateTimeKind set.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateSqliteCacheShouldCreateCacheWithLegacyAndForcedDateTimeKind()
    {
        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName($"CreateSqliteCacheHappy_{Guid.NewGuid():N}")
            .WithSerializer<SystemJsonSerializer>()
            .WithLegacyFileLocation()
            .UseForcedDateTimeKind(DateTimeKind.Utc);

        builder.WithSqliteProvider();

        var cache = Sqlite3.AkavacheBuilderExtensions.CreateSqliteCache("UserAccount", builder);
        try
        {
            await Assert.That(cache).IsNotNull();
            await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests ResetSqliteProviderForTests is callable and resets internal state.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResetSqliteProviderForTestsShouldBeCallable()
    {
        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CreateBuilder("ResetSqliteProviderForTestsCall");
        builder.WithSqliteProvider();
        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var result = builder.WithSqliteProvider();
        await Assert.That(result).IsSameReferenceAs(builder);
    }

    /// <summary>
    /// Tests SecureBlobCacheWrapper.Serializer throws when inner cache serializer is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SecureBlobCacheWrapperSerializerShouldThrowWhenNull()
    {
        FakeNullSerializerBlobCache fakeInner = new();
        var wrapper = new Akavache.Sqlite3.AkavacheBuilderExtensions.SecureBlobCacheWrapper(fakeInner);

        await Assert.That(() => _ = wrapper.Serializer)
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests SecureBlobCacheWrapper.DisposeAsyncCore disposes the inner cache via IAsyncDisposable.
    /// Both InMemoryBlobCache and SqliteBlobCache implement IAsyncDisposable through IBlobCache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SecureBlobCacheWrapperDisposeAsyncCoreShouldDisposeInner()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            SystemJsonSerializer serializer = new();
            SqliteBlobCache inner = new(
                Path.Combine(path, "async-dispose-test.db"), serializer);
            var wrapper = new Akavache.Sqlite3.AkavacheBuilderExtensions.SecureBlobCacheWrapper(inner);

            await Assert.That(async () => await wrapper.DisposeAsyncCore()).ThrowsNothing();
        }
    }

    /// <summary>
    /// Tests SecureBlobCacheWrapper full DisposeAsync calls both DisposeAsyncCore and Dispose(false).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Second-call Dispose intentionally synchronous to exercise the idempotent sync path.")]
    public async Task SecureBlobCacheWrapperDisposeAsyncShouldWork()
    {
        InMemoryBlobCache inner = new(new SystemJsonSerializer());
        var wrapper = new Akavache.Sqlite3.AkavacheBuilderExtensions.SecureBlobCacheWrapper(inner);

        await Assert.That(async () => await wrapper.DisposeAsync()).ThrowsNothing();

        // Double dispose should not throw
        wrapper.Dispose();
    }

    /// <summary>
    /// Tests SecureBlobCacheWrapper.Dispose with disposing=true disposes the inner cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Test deliberately exercises the synchronous Dispose path.")]
    public async Task SecureBlobCacheWrapperDisposeShouldDisposeInner()
    {
        InMemoryBlobCache inner = new(new SystemJsonSerializer());
        var wrapper = new Akavache.Sqlite3.AkavacheBuilderExtensions.SecureBlobCacheWrapper(inner);

        wrapper.Dispose();

        // Second Dispose should not throw (idempotent due to _disposed flag)
        await Assert.That(() => wrapper.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// Creates a new <see cref="IAkavacheBuilder"/> with the given application name.
    /// </summary>
    /// <param name="applicationName">The application name to configure on the builder.</param>
    /// <returns>A new <see cref="IAkavacheBuilder"/>.</returns>
    private static IAkavacheBuilder CreateBuilder(string applicationName) =>
        CacheDatabase.CreateBuilder().WithApplicationName(applicationName);

    /// <summary>
    /// Fake IBlobCache with null Serializer to test the null guard in SecureBlobCacheWrapper.
    /// </summary>
    private sealed class FakeNullSerializerBlobCache : IBlobCache
    {
        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IScheduler Scheduler => ImmediateScheduler.Instance;

        /// <inheritdoc/>
        public ISerializer Serializer => null!;

        /// <inheritdoc/>
        public IHttpService HttpService { get; set; } = null!;

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) => Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) => Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) => Observable.Return<byte[]?>(null);

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) => Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => Observable.Empty<KeyValuePair<string, byte[]>>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => Observable.Empty<string>();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) => Observable.Empty<(string, DateTimeOffset?)>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) => Observable.Empty<(string, DateTimeOffset?)>();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => Observable.Return<DateTimeOffset?>(null);

        /// <inheritdoc/>
        public IObservable<Unit> Flush() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Flush(Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key, Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll(Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> Vacuum() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration) => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// Minimal fake IAkavacheBuilder used to force empty-ApplicationName scenarios that
    /// cannot be produced through the real builder (which coerces empty names to no-ops).
    /// </summary>
    private sealed class FakeAkavacheBuilder : IAkavacheBuilder
    {
        /// <inheritdoc/>
        public Assembly ExecutingAssembly { get; } = typeof(FakeAkavacheBuilder).Assembly;

        /// <inheritdoc/>
        public string ApplicationName { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string? ApplicationRootPath { get; set; }

        /// <inheritdoc/>
        public string? SettingsCachePath { get; set; }

        /// <inheritdoc/>
        public string? ExecutingAssemblyName => ExecutingAssembly.GetName().Name;

        /// <inheritdoc/>
        public Version? Version => ExecutingAssembly.GetName().Version;

        /// <inheritdoc/>
        public IBlobCache? InMemory { get; set; }

        /// <inheritdoc/>
        public IBlobCache? LocalMachine { get; set; }

        /// <inheritdoc/>
        public ISecureBlobCache? Secure { get; set; }

        /// <inheritdoc/>
        public IBlobCache? UserAccount { get; set; }

        /// <inheritdoc/>
        public IHttpService? HttpService { get; set; }

        /// <inheritdoc/>
        public ISerializer? Serializer { get; set; }

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public string? SerializerTypeName { get; set; }

        /// <inheritdoc/>
        public FileLocationOption FileLocationOption { get; set; } = FileLocationOption.Default;

        /// <inheritdoc/>
        public IAkavacheInstance Build() => this;

        /// <inheritdoc/>
        public IAkavacheBuilder UseForcedDateTimeKind(DateTimeKind kind)
        {
            ForcedDateTimeKind = kind;
            return this;
        }

        /// <inheritdoc/>
        public IAkavacheBuilder WithApplicationName(string? applicationName)
        {
            ApplicationName = applicationName ?? string.Empty;
            return this;
        }

        /// <inheritdoc/>
        public IAkavacheBuilder WithExecutingAssembly(Assembly assembly) => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithInMemory(IBlobCache cache)
        {
            InMemory = cache;
            return this;
        }

        /// <inheritdoc/>
        public IAkavacheBuilder WithInMemoryDefaults() => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithLegacyFileLocation()
        {
            FileLocationOption = FileLocationOption.Legacy;
            return this;
        }

        /// <inheritdoc/>
        public IAkavacheBuilder WithLocalMachine(IBlobCache cache)
        {
            LocalMachine = cache;
            return this;
        }

        /// <inheritdoc/>
        public IAkavacheBuilder WithSecure(ISecureBlobCache cache)
        {
            Secure = cache;
            return this;
        }

        /// <inheritdoc/>
        public IAkavacheBuilder WithSerializer<T>()
            where T : class, ISerializer, new()
        {
            Serializer = new T();
            SerializerTypeName = typeof(T).AssemblyQualifiedName;
            return this;
        }

        /// <inheritdoc/>
        public IAkavacheBuilder WithSerializer<T>(Func<T> configure)
            where T : class, ISerializer
        {
            Serializer = configure();
            SerializerTypeName = typeof(T).AssemblyQualifiedName;
            return this;
        }

        /// <inheritdoc/>
        public IAkavacheBuilder WithUserAccount(IBlobCache cache)
        {
            UserAccount = cache;
            return this;
        }
    }
}
