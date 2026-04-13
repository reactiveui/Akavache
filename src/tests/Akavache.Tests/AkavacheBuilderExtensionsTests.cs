// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.SystemTextJson;
using Akavache.Tests.Executors;
using Splat;
using Splat.Builder;

namespace Akavache.Tests;

/// <summary>
/// Tests for AkavacheBuilderExtensions.
/// </summary>
[Category("Akavache")]
[NotInParallel("CacheDatabaseState")]
public class AkavacheBuilderExtensionsTests
{
    /// <summary>
    /// Reset CacheDatabase between tests since it has global state.
    /// </summary>
    /// <returns>A task.</returns>
    [Before(Test)]
    public async Task ResetCacheDatabase() => await CacheDatabase.ResetForTestsAsync();

    /// <summary>
    /// Cleanup CacheDatabase after each test.
    /// </summary>
    /// <returns>A task.</returns>
    [After(Test)]
    public async Task CleanupCacheDatabase() => await CacheDatabase.ResetForTestsAsync();

    /// <summary>
    /// Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configure, applicationName) throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseConfigureShouldThrowOnNullBuilder() =>
        await Assert.That(static () =>
            AkavacheBuilderExtensions.WithAkavacheCacheDatabase<SystemJsonSerializer>(
                null!, static b => b.WithInMemoryDefaults(), "TestApp"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configure, applicationName) initializes the cache database.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseConfigureShouldInitialize()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        var result = appBuilder.WithAkavacheCacheDatabase<SystemJsonSerializer>(
            static b => b.WithInMemoryDefaults(),
            "TestApp_ConfigureInit");

        await Assert.That(result).IsNotNull();
        await Assert.That(CacheDatabase.IsInitialized).IsTrue();
    }

    /// <summary>
    /// Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configureSerializer, configure, applicationName) throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseFactoryConfigureShouldThrowOnNullBuilder() =>
        await Assert.That(static () =>
            AkavacheBuilderExtensions.WithAkavacheCacheDatabase(
                null!, static () => new SystemJsonSerializer(), static b => b.WithInMemoryDefaults(), "TestApp"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configureSerializer, configure, applicationName) initializes the cache database.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseFactoryConfigureShouldInitialize()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        var result = appBuilder.WithAkavacheCacheDatabase(
            static () => new SystemJsonSerializer(),
            static b => b.WithInMemoryDefaults(),
            "TestApp_FactoryConfigureInit");

        await Assert.That(result).IsNotNull();
        await Assert.That(CacheDatabase.IsInitialized).IsTrue();
    }

    /// <summary>
    /// Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, applicationName) throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseDefaultShouldThrowOnNullBuilder() =>
        await Assert.That(static () =>
            AkavacheBuilderExtensions.WithAkavacheCacheDatabase<SystemJsonSerializer>(null!, "TestApp"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, applicationName) initializes the cache database.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseDefaultShouldInitialize()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        var result = appBuilder.WithAkavacheCacheDatabase<SystemJsonSerializer>("TestApp_DefaultInit");

        await Assert.That(result).IsNotNull();
        await Assert.That(CacheDatabase.IsInitialized).IsTrue();
    }

    /// <summary>
    /// Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configureSerializer, applicationName) throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseFactoryShouldThrowOnNullBuilder() =>
        await Assert.That(static () =>
            AkavacheBuilderExtensions.WithAkavacheCacheDatabase(
                null!, static () => new SystemJsonSerializer(), "TestApp"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configureSerializer, applicationName) initializes the cache database.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseFactoryShouldInitialize()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        var result = appBuilder.WithAkavacheCacheDatabase(
            static () => new SystemJsonSerializer(),
            "TestApp_FactoryInit");

        await Assert.That(result).IsNotNull();
        await Assert.That(CacheDatabase.IsInitialized).IsTrue();
    }

    /// <summary>
    /// Tests WithAkavache&lt;T&gt;(builder, applicationName, configure, instance) throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheConfigureInstanceShouldThrowOnNullBuilder()
    {
        Action<IAkavacheInstance> instance = _ => { };
        await Assert.That(() =>
            AkavacheBuilderExtensions.WithAkavache<SystemJsonSerializer>(
                null!, "TestApp", b => b.WithInMemoryDefaults(), instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithAkavache configure-instance overload throws on null configure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheConfigureInstanceShouldThrowOnNullConfigure()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IAkavacheInstance> instance = _ => { };
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>("TestApp", null!, instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithAkavache configure-instance overload throws on null instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheConfigureInstanceShouldThrowOnNullInstance()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IAkavacheInstance>? instance = null;
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>(
                "TestApp", b => b.WithInMemoryDefaults(), instance!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithAkavache configure-instance overload invokes configure and instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheConfigureInstanceShouldInvokeCallbacks()
    {
        var configureInvoked = false;
        var instanceInvoked = false;
        var appBuilder = AppBuilder.CreateSplatBuilder();

        appBuilder.WithAkavache<SystemJsonSerializer>(
            "TestApp_ConfigInst",
            b =>
            {
                configureInvoked = true;
                b.WithInMemoryDefaults();
            },
            i => instanceInvoked = i is not null);

        await Assert.That(configureInvoked).IsTrue();
        await Assert.That(instanceInvoked).IsTrue();
    }

    /// <summary>
    /// Tests WithAkavache resolver-instance overload throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheResolverInstanceShouldThrowOnNullBuilder()
    {
        Action<IMutableDependencyResolver, IAkavacheInstance> instance = (_, _) => { };
        await Assert.That(() =>
            AkavacheBuilderExtensions.WithAkavache<SystemJsonSerializer>(
                null!, "TestApp", b => b.WithInMemoryDefaults(), instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithAkavache resolver-instance overload throws on null configure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheResolverInstanceShouldThrowOnNullConfigure()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IMutableDependencyResolver, IAkavacheInstance> instance = (_, _) => { };
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>("TestApp", null!, instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithAkavache resolver-instance overload throws on null instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheResolverInstanceShouldThrowOnNullInstance()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IMutableDependencyResolver, IAkavacheInstance>? instance = null;
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>("TestApp", b => b.WithInMemoryDefaults(), instance!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithAkavache simple instance overload throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheSimpleInstanceShouldThrowOnNullBuilder()
    {
        Action<IAkavacheInstance> instance = _ => { };
        await Assert.That(() =>
            AkavacheBuilderExtensions.WithAkavache<SystemJsonSerializer>(null!, "TestApp", instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithAkavache simple instance overload throws on null instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheSimpleInstanceShouldThrowOnNullInstance()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IAkavacheInstance>? instance = null;
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>("TestApp", instance!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithAkavache simple instance overload invokes the instance callback.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheSimpleInstanceShouldInvokeCallback()
    {
        var instanceInvoked = false;
        var appBuilder = AppBuilder.CreateSplatBuilder();

        appBuilder.WithAkavache<SystemJsonSerializer>(
            "TestApp_SimpleInst",
            i => instanceInvoked = i is not null);

        await Assert.That(instanceInvoked).IsTrue();
    }

    /// <summary>
    /// Tests WithAkavache simple resolver-instance overload throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheSimpleResolverInstanceShouldThrowOnNullBuilder()
    {
        Action<IMutableDependencyResolver, IAkavacheInstance> instance = (_, _) => { };
        await Assert.That(() =>
            AkavacheBuilderExtensions.WithAkavache<SystemJsonSerializer>(null!, "TestApp", instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithAkavache simple resolver-instance overload throws on null instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheSimpleResolverInstanceShouldThrowOnNullInstance()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IMutableDependencyResolver, IAkavacheInstance>? instance = null;
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>("TestApp", instance!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithInMemory throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryShouldThrowOnNullBuilder() =>
        await Assert.That(static () => AkavacheBuilderExtensions.WithInMemory(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithInMemory throws when no serializer is configured.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryShouldThrowWhenNoSerializerConfigured()
    {
        var builder = CacheDatabase.CreateBuilder();
        await Assert.That(() => builder.WithInMemory()).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithInMemory works when serializer is configured.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryShouldWorkWithSerializer()
    {
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName("TestApp_WithInMemory")
            .WithSerializer<SystemJsonSerializer>()
            .WithInMemory();

        await Assert.That(builder).IsNotNull();
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnNullBuilder() =>
        await Assert.That(static () => AkavacheBuilderExtensions.GetIsolatedCacheDirectory(null!, "TestCache"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests GetIsolatedCacheDirectory throws on null cache name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnNullCacheName()
    {
        var instance = CreateInstance("TestApp_NullCache");
        await Assert.That(() => instance.GetIsolatedCacheDirectory(null!))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory throws on empty cache name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnEmptyCacheName()
    {
        var instance = CreateInstance("TestApp_EmptyCache");
        await Assert.That(() => instance.GetIsolatedCacheDirectory(string.Empty))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory returns a valid path for UserAccount.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldReturnPathForUserAccount()
    {
        var instance = CreateInstance("TestApp_UserAccountIso");
        var path = instance.GetIsolatedCacheDirectory("UserAccount");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory returns a valid path for Secure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldReturnPathForSecure()
    {
        var instance = CreateInstance("TestApp_SecureIso");
        var path = instance.GetIsolatedCacheDirectory("Secure");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory returns a valid path for SettingsCache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldReturnPathForSettingsCache()
    {
        var instance = CreateInstance("TestApp_SettingsIso");
        var path = instance.GetIsolatedCacheDirectory("SettingsCache");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory returns a path for unknown cache name (default branch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldHandleUnknownCacheName()
    {
        var instance = CreateInstance("TestApp_UnknownIso");
        var path = instance.GetIsolatedCacheDirectory("LocalMachine");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>
    /// Tests GetLegacyCacheDirectory throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnNullBuilder() =>
        await Assert.That(static () => AkavacheBuilderExtensions.GetLegacyCacheDirectory(null!, "TestCache"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests GetLegacyCacheDirectory throws on null cache name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnNullCacheName()
    {
        var instance = CreateInstance("TestApp_LegacyNullCache");
        await Assert.That(() => instance.GetLegacyCacheDirectory(null!))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests GetLegacyCacheDirectory throws on empty cache name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnEmptyCacheName()
    {
        var instance = CreateInstance("TestApp_LegacyEmptyCache");
        await Assert.That(() => instance.GetLegacyCacheDirectory(string.Empty))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests GetLegacyCacheDirectory returns a path for LocalMachine.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldReturnPathForLocalMachine()
    {
        var instance = CreateInstance("TestApp_LegacyLM");
        var path = instance.GetLegacyCacheDirectory("LocalMachine");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>
    /// Tests GetLegacyCacheDirectory returns a path for Secure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldReturnPathForSecure()
    {
        var instance = CreateInstance("TestApp_LegacySecure");
        var path = instance.GetLegacyCacheDirectory("Secure");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>
    /// Tests GetLegacyCacheDirectory returns a path for UserAccount (default branch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldReturnPathForUserAccount()
    {
        var instance = CreateInstance("TestApp_LegacyUA");
        var path = instance.GetLegacyCacheDirectory("UserAccount");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>
    /// Tests CreateRecursive creates nested directories.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateRecursiveShouldCreateNestedDirectories()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "AkavacheTest", Guid.NewGuid().ToString("N"), "level1", "level2", "level3");
        var dirInfo = new DirectoryInfo(tempPath);

        try
        {
            dirInfo.CreateRecursive();
            await Assert.That(Directory.Exists(tempPath)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(Path.Combine(Path.GetTempPath(), "AkavacheTest")))
            {
                try
                {
                    Directory.Delete(Path.Combine(Path.GetTempPath(), "AkavacheTest"), true);
                }
                catch
                {
                    // best effort
                }
            }
        }
    }

    /// <summary>
    /// Tests SplitFullPath returns path components.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SplitFullPathShouldReturnComponents()
    {
        var dirInfo = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "foo", "bar"));
        var components = dirInfo.SplitFullPath().ToList();
        await Assert.That(components.Count).IsGreaterThan(0);
    }

    /// <summary>
    /// Tests CreateRecursive is a no-op when the target directory already exists.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateRecursiveShouldBeNoOpWhenDirectoryExists()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "AkavacheTestExtra", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);
        var dirInfo = new DirectoryInfo(tempPath);

        try
        {
            dirInfo.CreateRecursive();
            await Assert.That(Directory.Exists(tempPath)).IsTrue();

            // Second call should also succeed as a no-op path.
            dirInfo.CreateRecursive();
            await Assert.That(Directory.Exists(tempPath)).IsTrue();
        }
        finally
        {
            try
            {
                Directory.Delete(tempPath, true);
            }
            catch
            {
                // best effort
            }
        }
    }

    /// <summary>
    /// Tests CreateRecursive creates only the missing leaf when parents already exist.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateRecursiveShouldCreateOnlyMissingLeaf()
    {
        var root = Path.Combine(Path.GetTempPath(), "AkavacheTestExtra", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var leaf = Path.Combine(root, "newLeaf");
        var dirInfo = new DirectoryInfo(leaf);

        try
        {
            dirInfo.CreateRecursive();
            await Assert.That(Directory.Exists(leaf)).IsTrue();
        }
        finally
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch
            {
                // best effort
            }
        }
    }

    /// <summary>
    /// Tests SplitFullPath includes the root as the first element.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SplitFullPathShouldIncludeRoot()
    {
        var full = Path.Combine(Path.GetTempPath(), "alpha", "beta", "gamma");
        var dirInfo = new DirectoryInfo(full);
        var components = dirInfo.SplitFullPath().ToList();

        await Assert.That(components.Count).IsGreaterThanOrEqualTo(4);

        var expectedRoot = Path.GetPathRoot(dirInfo.FullName);
        await Assert.That(components[0]).IsEqualTo(expectedRoot);
        await Assert.That(components).Contains("alpha");
        await Assert.That(components).Contains("beta");
        await Assert.That(components).Contains("gamma");
    }

    /// <summary>
    /// Tests SplitFullPath handles a path whose last segment is the root itself.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SplitFullPathShouldHandleRootOnlyPath()
    {
        var root = Path.GetPathRoot(Path.GetTempPath());
        if (string.IsNullOrEmpty(root))
        {
            return;
        }

        var dirInfo = new DirectoryInfo(root);
        var components = dirInfo.SplitFullPath().ToList();

        // Root-only path should yield at least the root.
        await Assert.That(components.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(components[0]).IsEqualTo(root);
    }

    /// <summary>
    /// Tests SplitFullPath handles a single-component path beneath the root.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SplitFullPathShouldHandleSingleComponentPath()
    {
        var root = Path.GetPathRoot(Path.GetTempPath());
        if (string.IsNullOrEmpty(root))
        {
            return;
        }

        var single = Path.Combine(root, "single_" + Guid.NewGuid().ToString("N"));
        var dirInfo = new DirectoryInfo(single);
        var components = dirInfo.SplitFullPath().ToList();

        await Assert.That(components.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(components[0]).IsEqualTo(root);
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory with a LocalMachine cache name triggers the machine/user store fallback path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldHandleLocalMachineBranch()
    {
        var instance = CreateInstance("TestApp_LMIsoExtra");
        var path = instance.GetIsolatedCacheDirectory("LocalMachine");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory can be called multiple times for the same cache without error (exercises DirectoryExists branch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldBeIdempotent()
    {
        var instance = CreateInstance("TestApp_IsoIdem");
        var first = instance.GetIsolatedCacheDirectory("UserAccount");
        var second = instance.GetIsolatedCacheDirectory("UserAccount");

        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNotNull();
        await Assert.That(second).IsEqualTo(first);
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory throws ArgumentException when ApplicationName is whitespace.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnWhitespaceCacheName()
    {
        var instance = CreateInstance("TestApp_WsCache");
        await Assert.That(() => instance.GetIsolatedCacheDirectory("   "))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests GetLegacyCacheDirectory throws ArgumentException on whitespace cache name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnWhitespaceCacheName()
    {
        var instance = CreateInstance("TestApp_LegacyWs");
        await Assert.That(() => instance.GetLegacyCacheDirectory("   "))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests GetLegacyCacheDirectory returns a path for SettingsCache (hits the default switch branch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldReturnPathForSettingsCache()
    {
        var instance = CreateInstance("TestApp_LegacySettings");
        var path = instance.GetLegacyCacheDirectory("SettingsCache");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>
    /// Tests GetLegacyCacheDirectory returns paths that contain the application name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryPathShouldContainApplicationName()
    {
        const string appName = "TestApp_LegacyContainsName";
        var instance = CreateInstance(appName);
        var localMachine = instance.GetLegacyCacheDirectory("LocalMachine");
        var secure = instance.GetLegacyCacheDirectory("Secure");
        var userAccount = instance.GetLegacyCacheDirectory("UserAccount");

        await Assert.That(localMachine).IsNotNull();
        await Assert.That(localMachine!).Contains(appName);
        await Assert.That(secure).IsNotNull();
        await Assert.That(secure!).Contains(appName);
        await Assert.That(userAccount).IsNotNull();
        await Assert.That(userAccount!).Contains(appName);
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory uses a path constructed from the application name and cache name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryPathShouldReferenceCacheName()
    {
        const string appName = "TestApp_IsoContains";
        var instance = CreateInstance(appName);
        var path = instance.GetIsolatedCacheDirectory("UserAccount");

        await Assert.That(path).IsNotNull();
        await Assert.That(path!).Contains("UserAccount");
    }

    /// <summary>
    /// Tests WithInMemory returns the same builder for chaining.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryShouldReturnSameBuilder()
    {
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName("TestApp_WithInMemoryChain")
            .WithSerializer<SystemJsonSerializer>();

        var result = builder.WithInMemory();
        await Assert.That(result).IsSameReferenceAs(builder);
    }

    /// <summary>
    /// Tests WithAkavache resolver-instance overload runs its configure/instance bodies
    /// when the underlying SplatBuilder is built, covering the WithCustomRegistration lambda.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task WithAkavacheConfigureResolverInstanceShouldRegisterCallbacks()
    {
        var configureInvoked = false;
        var instanceInvoked = false;
        var appBuilder = AppBuilder.CreateSplatBuilder();

        Action<IAkavacheBuilder> configure = b =>
        {
            configureInvoked = true;
            b.WithInMemoryDefaults();
        };
        Action<IMutableDependencyResolver, IAkavacheInstance> instance = (resolver, i) =>
            instanceInvoked = resolver is not null && i is not null;

        appBuilder
            .WithAkavache<SystemJsonSerializer>("TestApp_ConfigResolverBuild", configure, instance)
            .Build();

        await WaitUntilAsync(() => instanceInvoked).ConfigureAwait(false);

        await Assert.That(configureInvoked).IsTrue();
        await Assert.That(instanceInvoked).IsTrue();
    }

    /// <summary>
    /// Tests WithAkavache simple resolver-instance overload runs its instance body when
    /// the underlying SplatBuilder is built, covering the WithCustomRegistration lambda.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task WithAkavacheSimpleResolverInstanceShouldRegisterCallback()
    {
        var instanceInvoked = false;
        var appBuilder = AppBuilder.CreateSplatBuilder();

        Action<IMutableDependencyResolver, IAkavacheInstance> instance = (resolver, i) =>
            instanceInvoked = resolver is not null && i is not null;

        appBuilder
            .WithAkavache<SystemJsonSerializer>("TestApp_SimpleResolverBuild", instance)
            .Build();

        await WaitUntilAsync(() => instanceInvoked).ConfigureAwait(false);

        await Assert.That(instanceInvoked).IsTrue();
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory throws ArgumentException when the instance's
    /// ApplicationName is null, exercising the ApplicationName null-check branch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnNullApplicationName()
    {
        var stub = new StubAkavacheInstance(null);
        await Assert.That(() => stub.GetIsolatedCacheDirectory("UserAccount"))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests GetIsolatedCacheDirectory throws ArgumentException when the instance's
    /// ApplicationName is whitespace only.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnWhitespaceApplicationName()
    {
        var stub = new StubAkavacheInstance("   ");
        await Assert.That(() => stub.GetIsolatedCacheDirectory("UserAccount"))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests GetLegacyCacheDirectory throws ArgumentException when the instance's
    /// ApplicationName is null, exercising the ApplicationName null-check branch.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnNullApplicationName()
    {
        var stub = new StubAkavacheInstance(null);
        await Assert.That(() => stub.GetLegacyCacheDirectory("LocalMachine"))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests GetLegacyCacheDirectory throws ArgumentException when the instance's
    /// ApplicationName is whitespace only.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnWhitespaceApplicationName()
    {
        var stub = new StubAkavacheInstance("\t");
        await Assert.That(() => stub.GetLegacyCacheDirectory("LocalMachine"))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Tests SplitFullPath skips empty filename components (covers the continue branch)
    /// by passing in a path with a trailing directory separator.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SplitFullPathShouldSkipEmptyFilenameComponents()
    {
        var pathWithTrailing = Path.Combine(Path.GetTempPath(), "foo", "bar") + Path.DirectorySeparatorChar;
        var dirInfo = new DirectoryInfo(pathWithTrailing);

        var components = dirInfo.SplitFullPath().ToList();

        await Assert.That(components).IsNotEmpty();
        await Assert.That(components.Any(string.IsNullOrEmpty)).IsFalse();
        await Assert.That(components).Contains("foo");
        await Assert.That(components).Contains("bar");
    }

    /// <summary>Creates an in-memory Akavache instance for test use.</summary>
    /// <param name="applicationName">The application name to configure on the instance.</param>
    /// <returns>A freshly built <see cref="IAkavacheInstance"/>.</returns>
    private static IAkavacheInstance CreateInstance(string applicationName) =>
        CacheDatabase.CreateBuilder()
            .WithApplicationName(applicationName)
            .WithSerializer<SystemJsonSerializer>()
            .WithInMemoryDefaults()
            .Build();

    /// <summary>Polls a predicate until it becomes true or the timeout elapses.</summary>
    /// <param name="condition">Predicate to wait on.</param>
    /// <param name="timeoutMs">Maximum wait in milliseconds.</param>
    /// <returns>A task that completes when the condition is observed or the timeout elapses.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Minimal stub used to exercise argument validation paths that depend on the
    /// instance's ApplicationName (which cannot be set to null via the real builder).
    /// </summary>
    private sealed class StubAkavacheInstance : IAkavacheInstance
    {
        /// <summary>Initializes a new instance of the <see cref="StubAkavacheInstance"/> class.</summary>
        /// <param name="applicationName">The application name to expose.</param>
        public StubAkavacheInstance(string? applicationName) => ApplicationName = applicationName!;

        /// <inheritdoc/>
        public Assembly ExecutingAssembly => typeof(StubAkavacheInstance).Assembly;

        /// <inheritdoc/>
        public string ApplicationName { get; }

        /// <inheritdoc/>
        public string? ApplicationRootPath => null;

        /// <inheritdoc/>
        public string? SettingsCachePath { get; set; }

        /// <inheritdoc/>
        public string? ExecutingAssemblyName => ExecutingAssembly.GetName().Name;

        /// <inheritdoc/>
        public Version? Version => ExecutingAssembly.GetName().Version;

        /// <inheritdoc/>
        public IBlobCache? InMemory => null;

        /// <inheritdoc/>
        public IBlobCache? LocalMachine => null;

        /// <inheritdoc/>
        public ISecureBlobCache? Secure => null;

        /// <inheritdoc/>
        public IBlobCache? UserAccount => null;

        /// <inheritdoc/>
        public IHttpService? HttpService { get; set; }

        /// <inheritdoc/>
        public ISerializer? Serializer => null;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public string? SerializerTypeName => null;
    }
}
