// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Core;
using Akavache.EncryptedSqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.
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
        await Assert.That(static () => Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.WithEncryptedSqliteProvider(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithEncryptedSqliteProvider() initializes the SQLite provider.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithEncryptedSqliteProviderShouldInitialize()
    {
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
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
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
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
        await Assert.That(static () => Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.WithSqliteDefaults(null!, "password"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithSqliteDefaults(password) throws when no serializer is registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSqliteDefaultsShouldThrowWhenNoSerializer()
    {
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
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
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        using (Utility.WithEmptyDirectory(out var path))
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

        await Assert.That(() => Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache(string.Empty, builder, "password"))
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

        await Assert.That(() => Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("UserAccount", builder, "password"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests CreateEncryptedSqliteCache throws on whitespace cache name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateEncryptedSqliteCacheShouldThrowOnWhitespaceName()
    {
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName("CreateEncryptedSqliteCacheWhitespaceName")
            .WithSerializer<SystemJsonSerializer>();

        await Assert.That(() => Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("   ", builder, "password"))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests CreateEncryptedSqliteCache happy path returns a valid encrypted cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateEncryptedSqliteCacheShouldReturnValidCache()
    {
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName($"CreateEncryptedSqliteCacheHappy_{Guid.NewGuid():N}")
            .WithSerializer<SystemJsonSerializer>()
            .WithEncryptedSqliteProvider();

        var cache = Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("UserAccount", builder, "test_password");

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
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName($"CreateEncryptedSqliteCacheForcedDtk_{Guid.NewGuid():N}")
            .WithSerializer<SystemJsonSerializer>()
            .WithEncryptedSqliteProvider()
            .UseForcedDateTimeKind(DateTimeKind.Utc);

        var cache = Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("UserAccount", builder, "test_password");

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
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName($"CreateEncryptedSqliteCacheLegacy_{Guid.NewGuid():N}")
            .WithSerializer<SystemJsonSerializer>()
            .WithEncryptedSqliteProvider()
            .WithLegacyFileLocation();

        var cache = Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("UserAccount", builder, "test_password");

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
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
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
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        var serializer = new SystemJsonSerializer();
        var builder = new FakeBuilder
        {
            ApplicationName = string.Empty,
            Serializer = serializer,
            SerializerTypeName = typeof(SystemJsonSerializer).AssemblyQualifiedName,
        };

        await Assert.That(() => Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.WithSqliteDefaults(builder, "test123"))
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
        var serializer = new SystemJsonSerializer();
        var builder = new FakeBuilder
        {
            ApplicationName = string.Empty,
            Serializer = serializer,
            SerializerTypeName = typeof(SystemJsonSerializer).AssemblyQualifiedName,
        };

        await Assert.That(() => Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.CreateEncryptedSqliteCache("UserAccount", builder, "test123"))
            .Throws<ArgumentException>();
    }

    private static IAkavacheBuilder CreateBuilder(string applicationName) =>
        CacheDatabase.CreateBuilder().WithApplicationName(applicationName);

    /// <summary>
    /// Minimal <see cref="IAkavacheBuilder"/> stub used to drive validation paths that
    /// cannot be reached through the real builder (which coerces empty application names
    /// to no-ops). Mirrors the helper in <c>Sqlite3BuilderExtensionsTests</c>.
    /// </summary>
    private sealed class FakeBuilder : IAkavacheBuilder
    {
        public Assembly ExecutingAssembly { get; } = typeof(FakeBuilder).Assembly;

        public string ApplicationName { get; set; } = string.Empty;

        public string? ApplicationRootPath { get; set; }

        public string? SettingsCachePath { get; set; }

        public string? ExecutingAssemblyName => ExecutingAssembly.GetName().Name;

        public Version? Version => ExecutingAssembly.GetName().Version;

        public IBlobCache? InMemory { get; set; }

        public IBlobCache? LocalMachine { get; set; }

        public ISecureBlobCache? Secure { get; set; }

        public IBlobCache? UserAccount { get; set; }

        public IHttpService? HttpService { get; set; }

        public ISerializer? Serializer { get; set; }

        public DateTimeKind? ForcedDateTimeKind { get; set; }

        public string? SerializerTypeName { get; set; }

        public FileLocationOption FileLocationOption { get; set; } = FileLocationOption.Default;

        public IAkavacheInstance Build() => this;

        public IAkavacheBuilder UseForcedDateTimeKind(DateTimeKind kind)
        {
            ForcedDateTimeKind = kind;
            return this;
        }

        public IAkavacheBuilder WithApplicationName(string? applicationName)
        {
            ApplicationName = applicationName ?? string.Empty;
            return this;
        }

        public IAkavacheBuilder WithExecutingAssembly(Assembly assembly) => this;

        public IAkavacheBuilder WithInMemory(IBlobCache cache)
        {
            InMemory = cache;
            return this;
        }

        public IAkavacheBuilder WithInMemoryDefaults() => this;

        public IAkavacheBuilder WithLegacyFileLocation()
        {
            FileLocationOption = FileLocationOption.Legacy;
            return this;
        }

        public IAkavacheBuilder WithLocalMachine(IBlobCache cache)
        {
            LocalMachine = cache;
            return this;
        }

        public IAkavacheBuilder WithSecure(ISecureBlobCache cache)
        {
            Secure = cache;
            return this;
        }

        public IAkavacheBuilder WithSerializer<T>()
            where T : ISerializer, new()
        {
            Serializer = new T();
            SerializerTypeName = typeof(T).AssemblyQualifiedName;
            return this;
        }

        public IAkavacheBuilder WithSerializer<T>(Func<T> configure)
            where T : ISerializer
        {
            Serializer = configure();
            SerializerTypeName = typeof(T).AssemblyQualifiedName;
            return this;
        }

        public IAkavacheBuilder WithUserAccount(IBlobCache cache)
        {
            UserAccount = cache;
            return this;
        }
    }
}
