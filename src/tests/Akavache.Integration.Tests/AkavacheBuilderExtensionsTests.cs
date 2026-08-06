// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Settings;
using Akavache.SystemTextJson;
using Akavache.Tests.Executors;
using Splat;
using Splat.Builder;

namespace Akavache.Integration.Tests;

/// <summary>Tests for AkavacheBuilderExtensions.</summary>
[Category("Akavache")]
public class AkavacheBuilderExtensionsTests
{
    /// <summary>Application name supplied to guard-clause tests, which reject their arguments before it is ever used.</summary>
    private const string TestApplicationName = "TestApp";

    /// <summary>Name of the per-user blob cache slot.</summary>
    private const string UserAccountSlot = "UserAccount";

    /// <summary>Name of the encrypted blob cache slot.</summary>
    private const string SecureCacheSlot = "Secure";

    /// <summary>Name of the machine-wide blob cache slot.</summary>
    private const string LocalMachineSlot = "LocalMachine";

    /// <summary>Folder beneath the system temp directory that holds the throwaway directories these tests create.</summary>
    private const string TempDirectoryRootName = "AkavacheTest";

    /// <summary>Components produced by splitting a path carrying three named segments beneath its root.</summary>
    private const int ThreeSegmentPathComponentCount = 4;

    /// <summary>Components produced by splitting a path carrying a single named segment beneath its root.</summary>
    private const int SingleSegmentPathComponentCount = 2;

    /// <summary>Gap between polls while waiting for a registration callback to run.</summary>
    private const int PollIntervalMilliseconds = 25;

    /// <summary>Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configure, applicationName) throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseConfigureShouldThrowOnNullBuilder() =>
        await Assert.That(static () =>
            AkavacheBuilderExtensions.WithAkavacheCacheDatabase<SystemJsonSerializer>(
                null!,
                static b => b.WithInMemoryDefaults(),
                TestApplicationName))
            .Throws<ArgumentNullException>();

    /// <summary>Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configure, applicationName) initializes the cache database.</summary>
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

    /// <summary>Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configureSerializer, configure, applicationName) throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseFactoryConfigureShouldThrowOnNullBuilder() =>
        await Assert.That(static () =>
            AkavacheBuilderExtensions.WithAkavacheCacheDatabase(
                null!,
                static () => new SystemJsonSerializer(),
                static b => b.WithInMemoryDefaults(),
                TestApplicationName))
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

    /// <summary>Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, applicationName) throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseDefaultShouldThrowOnNullBuilder() =>
        await Assert.That(static () =>
            AkavacheBuilderExtensions.WithAkavacheCacheDatabase<SystemJsonSerializer>(null!, TestApplicationName))
            .Throws<ArgumentNullException>();

    /// <summary>Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, applicationName) initializes the cache database.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseDefaultShouldInitialize()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        var result = appBuilder.WithAkavacheCacheDatabase<SystemJsonSerializer>("TestApp_DefaultInit");

        await Assert.That(result).IsNotNull();
        await Assert.That(CacheDatabase.IsInitialized).IsTrue();
    }

    /// <summary>Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configureSerializer, applicationName) throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheCacheDatabaseFactoryShouldThrowOnNullBuilder() =>
        await Assert.That(static () =>
            AkavacheBuilderExtensions.WithAkavacheCacheDatabase(
                null!,
                static () => new SystemJsonSerializer(),
                TestApplicationName))
            .Throws<ArgumentNullException>();

    /// <summary>Tests WithAkavacheCacheDatabase&lt;T&gt;(builder, configureSerializer, applicationName) initializes the cache database.</summary>
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

    /// <summary>Tests WithAkavache&lt;T&gt;(builder, applicationName, configure, instance) throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheConfigureInstanceShouldThrowOnNullBuilder()
    {
        Action<IAkavacheInstance> instance = static _ => { };
        await Assert.That(() =>
            AkavacheBuilderExtensions.WithAkavache<SystemJsonSerializer>(
                null!,
                TestApplicationName,
                static b => b.WithInMemoryDefaults(),
                instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithAkavache configure-instance overload throws on null configure.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheConfigureInstanceShouldThrowOnNullConfigure()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IAkavacheInstance> instance = static _ => { };
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>(TestApplicationName, null!, instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithAkavache configure-instance overload throws on null instance.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheConfigureInstanceShouldThrowOnNullInstance()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IAkavacheInstance>? instance = null;
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>(
                TestApplicationName,
                static b => b.WithInMemoryDefaults(),
                instance!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithAkavache configure-instance overload invokes configure and instance.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheConfigureInstanceShouldInvokeCallbacks()
    {
        var configureInvoked = false;
        var instanceInvoked = false;
        var appBuilder = AppBuilder.CreateSplatBuilder();

        _ = appBuilder.WithAkavache<SystemJsonSerializer>(
            "TestApp_ConfigInst",
            b =>
            {
                configureInvoked = true;
                _ = b.WithInMemoryDefaults();
            },
            i => instanceInvoked = i is not null);

        await Assert.That(configureInvoked).IsTrue();
        await Assert.That(instanceInvoked).IsTrue();
    }

    /// <summary>Tests WithAkavache resolver-instance overload throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheResolverInstanceShouldThrowOnNullBuilder()
    {
        Action<IMutableDependencyResolver, IAkavacheInstance> instance = static (_, _) => { };
        await Assert.That(() =>
            AkavacheBuilderExtensions.WithAkavache<SystemJsonSerializer>(
                null!,
                TestApplicationName,
                static b => b.WithInMemoryDefaults(),
                instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithAkavache resolver-instance overload throws on null configure.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheResolverInstanceShouldThrowOnNullConfigure()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IMutableDependencyResolver, IAkavacheInstance> instance = static (_, _) => { };
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>(TestApplicationName, null!, instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithAkavache resolver-instance overload throws on null instance.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheResolverInstanceShouldThrowOnNullInstance()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IMutableDependencyResolver, IAkavacheInstance>? instance = null;
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>(TestApplicationName, static b => b.WithInMemoryDefaults(), instance!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithAkavache simple instance overload throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheSimpleInstanceShouldThrowOnNullBuilder()
    {
        Action<IAkavacheInstance> instance = static _ => { };
        await Assert.That(() =>
            AkavacheBuilderExtensions.WithAkavache<SystemJsonSerializer>(null!, TestApplicationName, instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithAkavache simple instance overload throws on null instance.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheSimpleInstanceShouldThrowOnNullInstance()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IAkavacheInstance>? instance = null;
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>(TestApplicationName, instance!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithAkavache simple instance overload invokes the instance callback.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheSimpleInstanceShouldInvokeCallback()
    {
        var instanceInvoked = false;
        var appBuilder = AppBuilder.CreateSplatBuilder();

        _ = appBuilder.WithAkavache<SystemJsonSerializer>(
            "TestApp_SimpleInst",
            i => instanceInvoked = i is not null);

        await Assert.That(instanceInvoked).IsTrue();
    }

    /// <summary>Tests WithAkavache simple resolver-instance overload throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheSimpleResolverInstanceShouldThrowOnNullBuilder()
    {
        Action<IMutableDependencyResolver, IAkavacheInstance> instance = static (_, _) => { };
        await Assert.That(() =>
            AkavacheBuilderExtensions.WithAkavache<SystemJsonSerializer>(null!, TestApplicationName, instance))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithAkavache simple resolver-instance overload throws on null instance.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithAkavacheSimpleResolverInstanceShouldThrowOnNullInstance()
    {
        var appBuilder = AppBuilder.CreateSplatBuilder();
        Action<IMutableDependencyResolver, IAkavacheInstance>? instance = null;
        await Assert.That(() =>
            appBuilder.WithAkavache<SystemJsonSerializer>(TestApplicationName, instance!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithInMemory throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryShouldThrowOnNullBuilder() =>
        await Assert.That(static () => AkavacheBuilderExtensions.WithInMemory(null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests WithInMemory throws when no serializer is configured.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryShouldThrowWhenNoSerializerConfigured()
    {
        var builder = CacheDatabase.CreateBuilder();
        await Assert.That(() => builder.WithInMemory()).Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithInMemory works when serializer is configured.</summary>
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

    /// <summary>Tests GetIsolatedCacheDirectory throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnNullBuilder() =>
        await Assert.That(static () => AkavacheBuilderExtensions.GetIsolatedCacheDirectory(null!, "TestCache"))
            .Throws<ArgumentNullException>();

    /// <summary>Tests GetIsolatedCacheDirectory throws on null cache name.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnNullCacheName()
    {
        var instance = CreateInstance("TestApp_NullCache");
        await Assert.That(() => instance.GetIsolatedCacheDirectory(null!))
            .Throws<ArgumentException>();
    }

    /// <summary>Tests GetIsolatedCacheDirectory throws on empty cache name.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnEmptyCacheName()
    {
        var instance = CreateInstance("TestApp_EmptyCache");
        await Assert.That(() => instance.GetIsolatedCacheDirectory(string.Empty))
            .Throws<ArgumentException>();
    }

    /// <summary>Tests GetIsolatedCacheDirectory returns a valid path for UserAccount.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldReturnPathForUserAccount()
    {
        var instance = CreateInstance("TestApp_UserAccountIso");
        var path = instance.GetIsolatedCacheDirectory(UserAccountSlot);
        await Assert.That(path).IsNotNull();
    }

    /// <summary>Tests GetIsolatedCacheDirectory returns a valid path for Secure.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldReturnPathForSecure()
    {
        var instance = CreateInstance("TestApp_SecureIso");
        var path = instance.GetIsolatedCacheDirectory(SecureCacheSlot);
        await Assert.That(path).IsNotNull();
    }

    /// <summary>Tests GetIsolatedCacheDirectory returns a valid path for SettingsCache.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldReturnPathForSettingsCache()
    {
        var instance = CreateInstance("TestApp_SettingsIso");
        var path = instance.GetIsolatedCacheDirectory("SettingsCache");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>Tests GetIsolatedCacheDirectory returns a path for unknown cache name (default branch).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldHandleUnknownCacheName()
    {
        var instance = CreateInstance("TestApp_UnknownIso");
        var path = instance.GetIsolatedCacheDirectory(LocalMachineSlot);
        await Assert.That(path).IsNotNull();
    }

    /// <summary>Tests GetLegacyCacheDirectory throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnNullBuilder() =>
        await Assert.That(static () => AkavacheBuilderExtensions.GetLegacyCacheDirectory(null!, "TestCache"))
            .Throws<ArgumentNullException>();

    /// <summary>Tests GetLegacyCacheDirectory throws on null cache name.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnNullCacheName()
    {
        var instance = CreateInstance("TestApp_LegacyNullCache");
        await Assert.That(() => instance.GetLegacyCacheDirectory(null!))
            .Throws<ArgumentException>();
    }

    /// <summary>Tests GetLegacyCacheDirectory throws on empty cache name.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnEmptyCacheName()
    {
        var instance = CreateInstance("TestApp_LegacyEmptyCache");
        await Assert.That(() => instance.GetLegacyCacheDirectory(string.Empty))
            .Throws<ArgumentException>();
    }

    /// <summary>Tests GetLegacyCacheDirectory returns a path for LocalMachine.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldReturnPathForLocalMachine()
    {
        var instance = CreateInstance("TestApp_LegacyLM");
        var path = instance.GetLegacyCacheDirectory(LocalMachineSlot);
        await Assert.That(path).IsNotNull();
    }

    /// <summary>Tests GetLegacyCacheDirectory returns a path for Secure.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldReturnPathForSecure()
    {
        var instance = CreateInstance("TestApp_LegacySecure");
        var path = instance.GetLegacyCacheDirectory(SecureCacheSlot);
        await Assert.That(path).IsNotNull();
    }

    /// <summary>Tests GetLegacyCacheDirectory returns a path for UserAccount (default branch).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldReturnPathForUserAccount()
    {
        var instance = CreateInstance("TestApp_LegacyUA");
        var path = instance.GetLegacyCacheDirectory(UserAccountSlot);
        await Assert.That(path).IsNotNull();
    }

    /// <summary>Tests CreateRecursive creates nested directories.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateRecursiveShouldCreateNestedDirectories()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), TempDirectoryRootName, Guid.NewGuid().ToString("N"), "level1", "level2", "level3");
        DirectoryInfo dirInfo = new(tempPath);

        try
        {
            dirInfo.CreateRecursive();
            await Assert.That(Directory.Exists(tempPath)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(Path.Combine(Path.GetTempPath(), TempDirectoryRootName)))
            {
                try
                {
                    Directory.Delete(Path.Combine(Path.GetTempPath(), TempDirectoryRootName), true);
                }
                catch (IOException)
                {
                    // Teardown only, and the assertion above has already run. A concurrently
                    // running test still holding a handle under the shared temp root must not
                    // turn a passing test red; the OS reclaims the directory later.
                }
                catch (UnauthorizedAccessException)
                {
                    // Same: a leftover read-only entry is a cleanup problem, not a test failure.
                }
            }
        }
    }

    /// <summary>Tests SplitFullPath returns path components.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SplitFullPathShouldReturnComponents()
    {
        DirectoryInfo dirInfo = new(Path.Combine(Path.GetTempPath(), "foo", "bar"));
        var components = dirInfo.SplitFullPath().ToList();
        await Assert.That(components.Count).IsGreaterThan(0);
    }

    /// <summary>Tests CreateRecursive is a no-op when the target directory already exists.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateRecursiveShouldBeNoOpWhenDirectoryExists()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "AkavacheTestExtra", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempPath);
        DirectoryInfo dirInfo = new(tempPath);

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
            catch (IOException)
            {
                // Teardown only, and the assertions above have already run. A lingering handle
                // on the throwaway directory must not turn a passing test red.
            }
            catch (UnauthorizedAccessException)
            {
                // Same: a leftover read-only entry is a cleanup problem, not a test failure.
            }
        }
    }

    /// <summary>Tests CreateRecursive creates only the missing leaf when parents already exist.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateRecursiveShouldCreateOnlyMissingLeaf()
    {
        var root = Path.Combine(Path.GetTempPath(), "AkavacheTestExtra", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(root);
        var leaf = Path.Combine(root, "newLeaf");
        DirectoryInfo dirInfo = new(leaf);

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
            catch (IOException)
            {
                // Teardown only, and the assertion above has already run. A lingering handle
                // on the throwaway directory must not turn a passing test red.
            }
            catch (UnauthorizedAccessException)
            {
                // Same: a leftover read-only entry is a cleanup problem, not a test failure.
            }
        }
    }

    /// <summary>Tests SplitFullPath includes the root as the first element.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SplitFullPathShouldIncludeRoot()
    {
        var full = Path.Combine(Path.GetTempPath(), "alpha", "beta", "gamma");
        DirectoryInfo dirInfo = new(full);
        var components = dirInfo.SplitFullPath().ToList();

        await Assert.That(components.Count).IsGreaterThanOrEqualTo(ThreeSegmentPathComponentCount);

        var expectedRoot = Path.GetPathRoot(dirInfo.FullName);
        await Assert.That(components[0]).IsEqualTo(expectedRoot);
        await Assert.That(components).Contains("alpha");
        await Assert.That(components).Contains("beta");
        await Assert.That(components).Contains("gamma");
    }

    /// <summary>Tests SplitFullPath handles a path whose last segment is the root itself.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SplitFullPathShouldHandleRootOnlyPath()
    {
        var root = Path.GetPathRoot(Path.GetTempPath());
        if (string.IsNullOrEmpty(root))
        {
            return;
        }

        DirectoryInfo dirInfo = new(root);
        var components = dirInfo.SplitFullPath().ToList();

        // Root-only path should yield at least the root.
        await Assert.That(components.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(components[0]).IsEqualTo(root);
    }

    /// <summary>Tests SplitFullPath handles a single-component path beneath the root.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SplitFullPathShouldHandleSingleComponentPath()
    {
        var root = Path.GetPathRoot(Path.GetTempPath());
        if (string.IsNullOrEmpty(root))
        {
            return;
        }

        var single = Path.Combine(root, $"single_{Guid.NewGuid():N}");
        DirectoryInfo dirInfo = new(single);
        var components = dirInfo.SplitFullPath().ToList();

        await Assert.That(components.Count).IsGreaterThanOrEqualTo(SingleSegmentPathComponentCount);
        await Assert.That(components[0]).IsEqualTo(root);
    }

    /// <summary>Tests GetIsolatedCacheDirectory with a LocalMachine cache name triggers the machine/user store fallback path.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldHandleLocalMachineBranch()
    {
        var instance = CreateInstance("TestApp_LMIsoExtra");
        var path = instance.GetIsolatedCacheDirectory(LocalMachineSlot);
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
        var first = instance.GetIsolatedCacheDirectory(UserAccountSlot);
        var second = instance.GetIsolatedCacheDirectory(UserAccountSlot);

        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNotNull();
        await Assert.That(second).IsEqualTo(first);
    }

    /// <summary>Tests GetIsolatedCacheDirectory throws ArgumentException when ApplicationName is whitespace.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnWhitespaceCacheName()
    {
        var instance = CreateInstance("TestApp_WsCache");
        await Assert.That(() => instance.GetIsolatedCacheDirectory("   "))
            .Throws<ArgumentException>();
    }

    /// <summary>Tests GetLegacyCacheDirectory throws ArgumentException on whitespace cache name.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnWhitespaceCacheName()
    {
        var instance = CreateInstance("TestApp_LegacyWs");
        await Assert.That(() => instance.GetLegacyCacheDirectory("   "))
            .Throws<ArgumentException>();
    }

    /// <summary>Tests GetLegacyCacheDirectory returns a path for SettingsCache (hits the default switch branch).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldReturnPathForSettingsCache()
    {
        var instance = CreateInstance("TestApp_LegacySettings");
        var path = instance.GetLegacyCacheDirectory("SettingsCache");
        await Assert.That(path).IsNotNull();
    }

    /// <summary>Tests GetLegacyCacheDirectory returns paths that contain the application name.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryPathShouldContainApplicationName()
    {
        const string appName = "TestApp_LegacyContainsName";
        var instance = CreateInstance(appName);
        var localMachine = instance.GetLegacyCacheDirectory(LocalMachineSlot);
        var secure = instance.GetLegacyCacheDirectory(SecureCacheSlot);
        var userAccount = instance.GetLegacyCacheDirectory(UserAccountSlot);

        await Assert.That(localMachine).IsNotNull();
        await Assert.That(localMachine!).Contains(appName);
        await Assert.That(secure).IsNotNull();
        await Assert.That(secure!).Contains(appName);
        await Assert.That(userAccount).IsNotNull();
        await Assert.That(userAccount!).Contains(appName);
    }

    /// <summary>Tests GetIsolatedCacheDirectory uses a path constructed from the application name and cache name.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryPathShouldReferenceCacheName()
    {
        const string appName = "TestApp_IsoContains";
        var instance = CreateInstance(appName);
        var path = instance.GetIsolatedCacheDirectory(UserAccountSlot);

        await Assert.That(path).IsNotNull();
        await Assert.That(path!).Contains(UserAccountSlot);
    }

    /// <summary>Tests WithInMemory returns the same builder for chaining.</summary>
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
            _ = b.WithInMemoryDefaults();
        };
        Action<IMutableDependencyResolver, IAkavacheInstance> instance = (resolver, i) =>
            instanceInvoked = resolver is not null && i is not null;

        _ = appBuilder
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

        _ = appBuilder
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
        StubAkavacheInstance stub = new(null);
        await Assert.That(() => stub.GetIsolatedCacheDirectory(UserAccountSlot))
            .Throws<ArgumentException>();
    }

    /// <summary>Tests GetIsolatedCacheDirectory throws ArgumentException when the instance's ApplicationName is whitespace only.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetIsolatedCacheDirectoryShouldThrowOnWhitespaceApplicationName()
    {
        StubAkavacheInstance stub = new("   ");
        await Assert.That(() => stub.GetIsolatedCacheDirectory(UserAccountSlot))
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
        StubAkavacheInstance stub = new(null);
        await Assert.That(() => stub.GetLegacyCacheDirectory(LocalMachineSlot))
            .Throws<ArgumentException>();
    }

    /// <summary>Tests GetLegacyCacheDirectory throws ArgumentException when the instance's ApplicationName is whitespace only.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetLegacyCacheDirectoryShouldThrowOnWhitespaceApplicationName()
    {
        StubAkavacheInstance stub = new("\t");
        await Assert.That(() => stub.GetLegacyCacheDirectory(LocalMachineSlot))
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
        DirectoryInfo dirInfo = new(pathWithTrailing);

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
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        while (System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(PollIntervalMilliseconds).ConfigureAwait(false);
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
        public ISerializer? Serializer => null;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public string? SerializerTypeName => null;

        /// <inheritdoc/>
        public IDictionary<string, IBlobCache> BlobCaches { get; } = new Dictionary<string, IBlobCache>();

        /// <inheritdoc/>
        public IDictionary<string, ISettingsStorage> SettingsStores { get; } = new Dictionary<string, ISettingsStorage>();
    }
}
