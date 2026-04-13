// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Core;
using Akavache.EncryptedSqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for EncryptedSqlite3.AkavacheBuilderExtensions.
/// </summary>
[Category("Akavache")]
[NotInParallel(["CacheDatabaseState", "NativeSqlite"])]
public class EncryptedSqlite3BuilderExtensionsTests
{
    /// <summary>
    /// Tests WithEncryptedSqliteProvider() throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithEncryptedSqliteProviderShouldThrowOnNullBuilder() =>
        await Assert.That(static () => EncryptedSqlite3.AkavacheBuilderExtensions.WithEncryptedSqliteProvider(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithEncryptedSqliteProvider() initializes the SQLite provider.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithEncryptedSqliteProviderShouldInitialize()
    {
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CreateBuilder("WithEncryptedSqliteProviderInit");
        var result = builder.WithEncryptedSqliteProvider();
        await Assert.That(result).IsSameReferenceAs(builder);
    }

    /// <summary>
    /// Tests WithEncryptedSqliteProvider() is idempotent (second call is a no-op).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithEncryptedSqliteProviderShouldBeIdempotent()
    {
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CreateBuilder("WithEncryptedSqliteProviderIdempotent");
        builder.WithEncryptedSqliteProvider();
        var result = builder.WithEncryptedSqliteProvider();
        await Assert.That(result).IsSameReferenceAs(builder);
    }

    /// <summary>
    /// Tests WithSqliteDefaults(password) throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteDefaultsShouldThrowOnNullBuilder() =>
        await Assert.That(static () => EncryptedSqlite3.AkavacheBuilderExtensions.WithSqliteDefaults(null!, "password"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithSqliteDefaults(password) throws when no serializer is registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteDefaultsShouldThrowWhenNoSerializer()
    {
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CreateBuilder("WithSqliteDefaultsNoSerializer");
        await Assert.That(() => builder.WithSqliteDefaults("password"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests WithSqliteDefaults(password) creates encrypted caches.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteDefaultsShouldCreateEncryptedCaches()
    {
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        using (Utility.WithEmptyDirectory(out _))
        {
            var builder = CacheDatabase.CreateBuilder()
                .WithApplicationName($"WithSqliteDefaultsEncryptedTest_{Guid.NewGuid():N}")
                .WithSerializer<SystemJsonSerializer>();

            var result = builder.WithSqliteDefaults("test_password");

            await Assert.That(result).IsSameReferenceAs(builder);
            await Assert.That(builder.UserAccount).IsNotNull();
            await Assert.That(builder.LocalMachine).IsNotNull();
            await Assert.That(builder.Secure).IsNotNull();

            // Cleanup the caches
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
        }
    }

    /// <summary>
    /// Tests CreateEncryptedSqliteCache throws on empty cache name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateEncryptedSqliteCacheShouldThrowOnEmptyName()
    {
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName("CreateEncryptedSqliteCacheEmptyName")
            .WithSerializer<SystemJsonSerializer>();

        await Assert.That(() => EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache(string.Empty, builder, "password"))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests CreateEncryptedSqliteCache throws when no serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateEncryptedSqliteCacheShouldThrowWhenNoSerializer()
    {
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName("CreateEncryptedSqliteCacheNoSerializer");

        await Assert.That(() => EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("UserAccount", builder, "password"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests CreateEncryptedSqliteCache throws on whitespace cache name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateEncryptedSqliteCacheShouldThrowOnWhitespaceName()
    {
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName("CreateEncryptedSqliteCacheWhitespaceName")
            .WithSerializer<SystemJsonSerializer>();

        await Assert.That(() => EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("   ", builder, "password"))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests CreateEncryptedSqliteCache happy path returns a valid encrypted cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateEncryptedSqliteCacheShouldReturnValidCache()
    {
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName($"CreateEncryptedSqliteCacheHappy_{Guid.NewGuid():N}")
            .WithSerializer<SystemJsonSerializer>()
            .WithEncryptedSqliteProvider();

        var cache = EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("UserAccount", builder, "test_password");

        try
        {
            await Assert.That(cache).IsNotNull();
            await Assert.That(cache.ForcedDateTimeKind).IsNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests CreateEncryptedSqliteCache propagates ForcedDateTimeKind from the builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateEncryptedSqliteCacheShouldPropagateForcedDateTimeKind()
    {
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName($"CreateEncryptedSqliteCacheForcedDtk_{Guid.NewGuid():N}")
            .WithSerializer<SystemJsonSerializer>()
            .WithEncryptedSqliteProvider()
            .UseForcedDateTimeKind(DateTimeKind.Utc);

        var cache = EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("UserAccount", builder, "test_password");

        try
        {
            await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests CreateEncryptedSqliteCache with the Legacy file location option.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateEncryptedSqliteCacheShouldSupportLegacyFileLocation()
    {
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName($"CreateEncryptedSqliteCacheLegacy_{Guid.NewGuid():N}")
            .WithSerializer<SystemJsonSerializer>()
            .WithEncryptedSqliteProvider()
            .WithLegacyFileLocation();

        var cache = EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("UserAccount", builder, "test_password");

        try
        {
            await Assert.That(cache).IsNotNull();
        }
        finally
        {
            await cache.DisposeAsync();
        }
    }

    /// <summary>
    /// Tests WithSqliteDefaults(password) propagates ForcedDateTimeKind to every cache it creates.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteDefaultsShouldPropagateForcedDateTimeKindToAllCaches()
    {
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName($"WithSqliteDefaultsEncryptedDtk_{Guid.NewGuid():N}")
            .WithSerializer<SystemJsonSerializer>()
            .UseForcedDateTimeKind(DateTimeKind.Utc);

        builder.WithSqliteDefaults("test_password");

        try
        {
            await Assert.That(builder.UserAccount!.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
            await Assert.That(builder.LocalMachine!.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
            await Assert.That(builder.Secure!.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
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
        }
    }

    /// <summary>
    /// Verifies that <c>WithSqliteDefaults(IAkavacheBuilder, string)</c> on the encrypted
    /// builder extensions throws <see cref="InvalidOperationException"/> when the builder
    /// reports an empty application name. Closes the right-hand branch of the
    /// application-name guard in the encrypted assembly's compilation of
    /// <c>AkavacheBuilderExtensions</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteDefaultsShouldThrowWhenApplicationNameEmpty()
    {
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        SystemJsonSerializer serializer = new();
        FakeBuilder builder = new()
        {
            ApplicationName = string.Empty,
            Serializer = serializer,
            SerializerTypeName = typeof(SystemJsonSerializer).AssemblyQualifiedName,
        };

        await Assert.That(() => builder.WithSqliteDefaults("test123"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that <c>CreateEncryptedSqliteCache</c> throws <see cref="ArgumentException"/>
    /// when the builder reports an empty application name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateEncryptedSqliteCacheShouldThrowWhenApplicationNameEmpty()
    {
        SystemJsonSerializer serializer = new();
        FakeBuilder builder = new()
        {
            ApplicationName = string.Empty,
            Serializer = serializer,
            SerializerTypeName = typeof(SystemJsonSerializer).AssemblyQualifiedName,
        };

        await Assert.That(() => EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("UserAccount", builder, "test123"))
            .Throws<ArgumentException>();
    }

    /// <summary>Creates a real <see cref="IAkavacheBuilder"/> with the given application name.</summary>
    /// <param name="applicationName">The application name to assign to the builder.</param>
    /// <returns>A configured <see cref="IAkavacheBuilder"/>.</returns>
    private static IAkavacheBuilder CreateBuilder(string applicationName) =>
        CacheDatabase.CreateBuilder().WithApplicationName(applicationName);

    /// <summary>
    /// Minimal <see cref="IAkavacheBuilder"/> stub used to drive validation paths that
    /// cannot be reached through the real builder (which coerces empty application names
    /// to no-ops). Mirrors the helper in <c>Sqlite3BuilderExtensionsTests</c>.
    /// </summary>
    private sealed class FakeBuilder : IAkavacheBuilder
    {
        /// <inheritdoc/>
        public Assembly ExecutingAssembly { get; } = typeof(FakeBuilder).Assembly;

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
