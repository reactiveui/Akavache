// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.SystemTextJson;
using Splat;

namespace Akavache.Settings.Tests;

/// <summary>
/// Tests for the static helper decomposition of
/// <see cref="SettingsBase.GetBlobCacheForClass(string)"/> — each strategy can be
/// exercised in isolation thanks to the internal helpers.
/// </summary>
[Category("Akavache")]
[TestExecutor<AkavacheTestExecutor>]
public class SettingsBaseHelperTests
{
    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> returns
    /// <see langword="null"/> when the registry has not been populated.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldReturnNullWhenRegistryEmpty()
    {
        AkavacheBuilder.BlobCaches = [];

        var result = SettingsBase.TryGetFromBlobCacheRegistry("MissingClass");

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> returns the
    /// matching cache when the class name is registered with a non-null entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldReturnExactMatch()
    {
        InMemoryBlobCache cache = new(new SystemJsonSerializer());
        AkavacheBuilder.BlobCaches = new()
        {
            ["KnownClass"] = cache,
        };

        var result = SettingsBase.TryGetFromBlobCacheRegistry("KnownClass");

        await Assert.That(result).IsSameReferenceAs(cache);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> falls back
    /// to the first registered non-null entry when the class name does not match.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldFallBackToFirstNonNullEntry()
    {
        InMemoryBlobCache fallback = new(new SystemJsonSerializer());
        AkavacheBuilder.BlobCaches = new()
        {
            ["RenamedDatabase"] = fallback,
        };

        var result = SettingsBase.TryGetFromBlobCacheRegistry("DifferentClassName");

        await Assert.That(result).IsSameReferenceAs(fallback);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> returns
    /// <see langword="null"/> when the registry exists but every entry is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldReturnNullWhenAllEntriesNull()
    {
        AkavacheBuilder.BlobCaches = new()
        {
            ["NullEntry"] = null,
        };

        var result = SettingsBase.TryGetFromBlobCacheRegistry("NullEntry");

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> returns
    /// <see langword="null"/> when the static registry itself is <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldReturnNullWhenRegistryIsNull()
    {
        AkavacheBuilder.BlobCaches = null;

        var result = SettingsBase.TryGetFromBlobCacheRegistry("AnyClass");

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that the default-resolver overload of
    /// <see cref="SettingsBase.TryGetFromCacheDatabase()"/> returns
    /// <see langword="null"/> when <see cref="CacheDatabase"/> has not been initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromCacheDatabaseShouldReturnNullWhenNotInitialized()
    {
        var result = SettingsBase.TryGetFromCacheDatabase();

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromCacheDatabase(Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// returns the UserAccount cache when its resolver succeeds.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromCacheDatabaseShouldReturnUserAccountWhenResolverSucceeds()
    {
        InMemoryBlobCache userAccount = new(new SystemJsonSerializer());

        var result = SettingsBase.TryGetFromCacheDatabase(
            () => userAccount,
            ThrowingResolver,
            ThrowingResolver);

        await Assert.That(result).IsSameReferenceAs(userAccount);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromCacheDatabase(Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// falls back to LocalMachine when UserAccount's resolver throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromCacheDatabaseShouldFallBackToLocalMachineWhenUserAccountThrows()
    {
        InMemoryBlobCache localMachine = new(new SystemJsonSerializer());

        var result = SettingsBase.TryGetFromCacheDatabase(
            ThrowingResolver,
            () => localMachine,
            ThrowingResolver);

        await Assert.That(result).IsSameReferenceAs(localMachine);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromCacheDatabase(Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// falls back to InMemory when UserAccount and LocalMachine both throw.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromCacheDatabaseShouldFallBackToInMemoryWhenOthersThrow()
    {
        InMemoryBlobCache inMemory = new(new SystemJsonSerializer());

        var result = SettingsBase.TryGetFromCacheDatabase(
            ThrowingResolver,
            ThrowingResolver,
            () => inMemory);

        await Assert.That(result).IsSameReferenceAs(inMemory);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromCacheDatabase(Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// returns <see langword="null"/> when every resolver throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromCacheDatabaseShouldReturnNullWhenAllResolversThrow()
    {
        var result = SettingsBase.TryGetFromCacheDatabase(
            ThrowingResolver,
            ThrowingResolver,
            ThrowingResolver);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryReadAmbientCache"/> returns the value a
    /// successful resolver produces.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadAmbientCacheShouldReturnValueFromSuccessfulResolver()
    {
        InMemoryBlobCache cache = new(new SystemJsonSerializer());

        var result = SettingsBase.TryReadAmbientCache(() => cache);

        await Assert.That(result).IsSameReferenceAs(cache);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryReadAmbientCache"/> swallows a resolver
    /// exception and returns <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadAmbientCacheShouldReturnNullWhenResolverThrows()
    {
        var result = SettingsBase.TryReadAmbientCache(ThrowingResolver);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.GetBlobCacheForClass(string, Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// uses the supplied UserAccount resolver when the explicit registry is empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetBlobCacheForClassShouldUseInjectedResolvers()
    {
        AkavacheBuilder.BlobCaches = [];
        InMemoryBlobCache userAccount = new(new SystemJsonSerializer());

        var result = SettingsBase.GetBlobCacheForClass(
            "AnyClass",
            () => userAccount,
            ThrowingResolver,
            ThrowingResolver);

        await Assert.That(result).IsSameReferenceAs(userAccount);
    }

    /// <summary>
    /// Tests that the injectable-resolver <see cref="SettingsBase"/> constructor
    /// routes the supplied delegates through
    /// <see cref="SettingsBase.GetBlobCacheForClass(string, Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// when the registry has no entry for the class.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InjectableResolverConstructorShouldUseFallbackResolvers()
    {
        AkavacheBuilder.BlobCaches = [];
        InMemoryBlobCache userAccount = new(new SystemJsonSerializer());

        await using ResolverInjectedSettings settings = new(
            nameof(ResolverInjectedSettings),
            () => userAccount,
            ThrowingResolver,
            ThrowingResolver);

        await Assert.That(settings).IsNotNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetTransientFallback"/> returns
    /// <see langword="null"/> when no <see cref="ISerializer"/> is registered with
    /// the locator.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetTransientFallbackShouldReturnNullWhenNoSerializerRegistered()
    {
        var result = SettingsBase.TryGetTransientFallback();

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetTransientFallback"/> constructs a
    /// fresh <see cref="InMemoryBlobCache"/> when an <see cref="ISerializer"/> has
    /// been registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetTransientFallbackShouldReturnInMemoryWhenSerializerRegistered()
    {
        SystemJsonSerializer serializer = new();
        AppLocator.CurrentMutable.RegisterConstant<ISerializer>(serializer);
        try
        {
            var result = SettingsBase.TryGetTransientFallback();

            await Assert.That(result).IsNotNull();
            await Assert.That(result).IsTypeOf<InMemoryBlobCache>();
        }
        finally
        {
            AppLocator.CurrentMutable.UnregisterAll<ISerializer>();
        }
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.CreateNoCacheFoundException"/> includes
    /// every registered key in the message when the registry has entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateNoCacheFoundExceptionShouldListRegisteredKeys()
    {
        AkavacheBuilder.BlobCaches = new()
        {
            ["AlphaCache"] = new InMemoryBlobCache(new SystemJsonSerializer()),
            ["BetaCache"] = new InMemoryBlobCache(new SystemJsonSerializer()),
        };

        var exception = SettingsBase.CreateNoCacheFoundException("TargetClass");

        await Assert.That(exception.Message).Contains("TargetClass");
        await Assert.That(exception.Message).Contains("AlphaCache");
        await Assert.That(exception.Message).Contains("BetaCache");
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.CreateNoCacheFoundException"/> reports
    /// <c>&lt;none&gt;</c> when the registry is empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateNoCacheFoundExceptionShouldReport_NoneWhenRegistryEmpty()
    {
        AkavacheBuilder.BlobCaches = [];

        var exception = SettingsBase.CreateNoCacheFoundException("TargetClass");

        await Assert.That(exception.Message).Contains("<none>");
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.CreateNoCacheFoundException"/> reports
    /// <c>&lt;none&gt;</c> when the registry is <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateNoCacheFoundExceptionShouldReport_NoneWhenRegistryNull()
    {
        AkavacheBuilder.BlobCaches = null;

        var exception = SettingsBase.CreateNoCacheFoundException("TargetClass");

        await Assert.That(exception.Message).Contains("<none>");
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.GetBlobCacheForClass(string)"/> throws the
    /// descriptive exception when every strategy returns <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetBlobCacheForClassShouldThrowWhenNoStrategyResolves()
    {
        AkavacheBuilder.BlobCaches = [];

        await Assert.That(static () => SettingsBase.GetBlobCacheForClass("UnresolvableClass"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.GetBlobCacheForClass(string)"/> short-circuits to
    /// the registry when an entry exists, never touching the ambient
    /// <see cref="CacheDatabase"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetBlobCacheForClassShouldShortCircuitToRegistry()
    {
        InMemoryBlobCache registered = new(new SystemJsonSerializer());
        AkavacheBuilder.BlobCaches = new()
        {
            ["MyClass"] = registered,
        };

        var result = SettingsBase.GetBlobCacheForClass("MyClass");

        await Assert.That(result).IsSameReferenceAs(registered);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.ReadAmbientUserAccount"/> throws when
    /// <see cref="CacheDatabase"/> is not initialized — this is the default resolver
    /// wired up by the parameterless <see cref="SettingsBase"/> constructor, so
    /// exercising it directly gives us coverage on that line without having to stand
    /// up a subclass and ambient state.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadAmbientUserAccountShouldThrowWhenNotInitialized() =>
        await Assert.That(static () => SettingsBase.ReadAmbientUserAccount()).Throws<InvalidOperationException>();

    /// <summary>
    /// Tests that <see cref="SettingsBase.ReadAmbientLocalMachine"/> throws when
    /// <see cref="CacheDatabase"/> is not initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadAmbientLocalMachineShouldThrowWhenNotInitialized() =>
        await Assert.That(static () => SettingsBase.ReadAmbientLocalMachine()).Throws<InvalidOperationException>();

    /// <summary>
    /// Tests that <see cref="SettingsBase.ReadAmbientInMemory"/> throws when
    /// <see cref="CacheDatabase"/> is not initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadAmbientInMemoryShouldThrowWhenNotInitialized() =>
        await Assert.That(static () => SettingsBase.ReadAmbientInMemory()).Throws<InvalidOperationException>();

    /// <summary>
    /// Resolver stub that always throws — mirrors the behaviour of
    /// <see cref="CacheDatabase"/> property getters when the requested cache kind is
    /// unconfigured.
    /// </summary>
    /// <returns>Never returns; always throws.</returns>
    private static IBlobCache ThrowingResolver() =>
        throw new InvalidOperationException("cache kind not configured");

    /// <summary>
    /// Minimal <see cref="SettingsBase"/> subclass that forwards to the injectable-
    /// resolver constructor, so tests can exercise the 3-arg overload directly.
    /// </summary>
    /// <param name="className">The class name used as the key prefix.</param>
    /// <param name="userAccountResolver">Delegate that returns the UserAccount cache.</param>
    /// <param name="localMachineResolver">Delegate that returns the LocalMachine cache.</param>
    /// <param name="inMemoryResolver">Delegate that returns the InMemory cache.</param>
    private sealed class ResolverInjectedSettings(
        string className,
        Func<IBlobCache> userAccountResolver,
        Func<IBlobCache> localMachineResolver,
        Func<IBlobCache> inMemoryResolver)
        : SettingsBase(className, userAccountResolver, localMachineResolver, inMemoryResolver);
}
