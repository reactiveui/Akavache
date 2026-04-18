// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Core;
using Akavache.Settings;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.Sqlite3.AkavacheBuilderExtensions (non-encrypted).
/// </summary>
[Category("Akavache")]
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
                if (builder.UserAccount is IDisposable uaDisposable)
                {
                    uaDisposable.Dispose();
                }

                if (builder.LocalMachine is IDisposable lmDisposable)
                {
                    lmDisposable.Dispose();
                }

                if (builder.Secure is IDisposable sDisposable)
                {
                    sDisposable.Dispose();
                }

                if (builder.InMemory is IDisposable imDisposable)
                {
                    imDisposable.Dispose();
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
            cache.Dispose();
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
    /// Creates a new <see cref="IAkavacheBuilder"/> with the given application name.
    /// </summary>
    /// <param name="applicationName">The application name to configure on the builder.</param>
    /// <returns>A new <see cref="IAkavacheBuilder"/>.</returns>
    private static IAkavacheBuilder CreateBuilder(string applicationName) =>
        CacheDatabase.CreateBuilder().WithApplicationName(applicationName);

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
        public ISerializer? Serializer { get; set; }

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public string? SerializerTypeName { get; set; }

        /// <inheritdoc/>
        public FileLocationOption FileLocationOption { get; set; } = FileLocationOption.Default;

        /// <inheritdoc/>
        public IDictionary<string, IBlobCache> BlobCaches { get; } = new Dictionary<string, IBlobCache>();

        /// <inheritdoc/>
        public IDictionary<string, ISettingsStorage> SettingsStores { get; } = new Dictionary<string, ISettingsStorage>();

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
