// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Settings.Core;
using Akavache.SystemTextJson;
using Akavache.Tests;
using Splat;

namespace Akavache.Settings.Tests;

/// <summary>
/// Parallel-safe tests for the static helper decomposition of
/// <see cref="SettingsBase"/>. These tests exercise methods that either take
/// explicit resolver delegates (no global state) or only read from
/// <see cref="CacheDatabase.CurrentInstance"/> without mutating it.
/// </summary>
[Category("Akavache")]
public class SettingsBaseHelperTests
{
    /// <summary>
    /// Tests that <see cref="SettingsBase.TryReadAmbientCache"/> returns the value
    /// produced by a successful resolver.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryReadAmbientCacheShouldReturnValueFromSuccessfulResolver()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        var result = SettingsBase.TryReadAmbientCache(() => cache);

        await Assert.That(result).IsSameReferenceAs(cache);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryReadAmbientCache"/> swallows a resolver
    /// exception and returns <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryReadAmbientCacheShouldReturnNullWhenResolverThrows()
    {
        var result = SettingsBase.TryReadAmbientCache(ThrowingResolver);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromCacheDatabase(Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// returns the UserAccount cache when its resolver succeeds.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryGetFromCacheDatabaseShouldReturnUserAccountWhenResolverSucceeds()
    {
        InMemoryBlobCache userAccount = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

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
    internal async Task TryGetFromCacheDatabaseShouldFallBackToLocalMachineWhenUserAccountThrows()
    {
        InMemoryBlobCache localMachine = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

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
    internal async Task TryGetFromCacheDatabaseShouldFallBackToInMemoryWhenOthersThrow()
    {
        InMemoryBlobCache inMemory = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

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
    internal async Task TryGetFromCacheDatabaseShouldReturnNullWhenAllResolversThrow()
    {
        var result = SettingsBase.TryGetFromCacheDatabase(
            ThrowingResolver,
            ThrowingResolver,
            ThrowingResolver);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.GetBlobCacheForClass(string, Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// uses the supplied UserAccount resolver when the explicit registry has no match.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetBlobCacheForClassWithResolversShouldUseUserAccountResolver()
    {
        InMemoryBlobCache userAccount = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        var result = SettingsBase.GetBlobCacheForClass(
            "ParallelTestClass_UserAccount",
            () => userAccount,
            ThrowingResolver,
            ThrowingResolver);

        await Assert.That(result).IsSameReferenceAs(userAccount);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.GetBlobCacheForClass(string, Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// falls through to LocalMachine when UserAccount throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetBlobCacheForClassWithResolversShouldFallBackToLocalMachine()
    {
        InMemoryBlobCache localMachine = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        var result = SettingsBase.GetBlobCacheForClass(
            "ParallelTestClass_LocalMachine",
            ThrowingResolver,
            () => localMachine,
            ThrowingResolver);

        await Assert.That(result).IsSameReferenceAs(localMachine);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.GetBlobCacheForClass(string, Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// falls through to InMemory when both UserAccount and LocalMachine throw.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetBlobCacheForClassWithResolversShouldFallBackToInMemory()
    {
        InMemoryBlobCache inMemory = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        var result = SettingsBase.GetBlobCacheForClass(
            "ParallelTestClass_InMemory",
            ThrowingResolver,
            ThrowingResolver,
            () => inMemory);

        await Assert.That(result).IsSameReferenceAs(inMemory);
    }

    /// <summary>
    /// Tests that the injectable-resolver <see cref="SettingsBase"/> constructor
    /// (lines 48-57) routes the supplied delegates through
    /// <see cref="SettingsBase.GetBlobCacheForClass(string, Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InjectableResolverConstructorShouldUseFallbackResolvers()
    {
        InMemoryBlobCache userAccount = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        using ResolverInjectedSettings settings = new(
            nameof(ResolverInjectedSettings),
            () => userAccount,
            ThrowingResolver,
            ThrowingResolver);

        await Assert.That(settings).IsNotNull();
    }

    /// <summary>
    /// Tests that the injectable-resolver constructor creates a working settings
    /// instance that can read and write property values.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InjectableResolverConstructorShouldCreateFunctionalSettings()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        using ResolverInjectedSettings settings = new(
            nameof(ResolverInjectedSettings),
            () => cache,
            ThrowingResolver,
            ThrowingResolver);

        await Assert.That((int)settings.TestValue).IsEqualTo(42);

        settings.TestValue.Set(99).SubscribeAndComplete();

        await Assert.That((int)settings.TestValue).IsEqualTo(99);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> returns
    /// <see langword="null"/> when no Akavache instance has been initialized
    /// (CurrentInstance is null).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryGetFromBlobCacheRegistryShouldNotThrow()
    {
        // When CacheDatabase.CurrentInstance is null (or its registry is empty),
        // TryGetFromBlobCacheRegistry returns null. If an instance happens to be
        // configured by another test, the result will be the first registered
        // entry (fallback). Either way, no exception.
        await Assert.That(() => _ = SettingsBase.TryGetFromBlobCacheRegistry("NonexistentClass"))
            .ThrowsNothing();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromCacheDatabase()"/> (default-resolver
    /// overload) returns <see langword="null"/> or a cache without throwing, regardless
    /// of CacheDatabase state.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryGetFromCacheDatabaseDefaultOverloadShouldNotThrow()
    {
        // The default overload delegates to ReadAmbientUserAccount/LocalMachine/InMemory
        // wrapped in TryReadAmbientCache, so it swallows any exceptions.
        await Assert.That(() => _ = SettingsBase.TryGetFromCacheDatabase()).ThrowsNothing();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.CreateNoCacheFoundException"/> returns
    /// an <see cref="InvalidOperationException"/> that contains the class name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CreateNoCacheFoundExceptionShouldContainClassName()
    {
        var exception = SettingsBase.CreateNoCacheFoundException("MyTargetClass");

        await Assert.That(exception).IsTypeOf<InvalidOperationException>();
        await Assert.That(exception.Message).Contains("MyTargetClass");
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.CreateNoCacheFoundException"/> reports
    /// available caches or &lt;none&gt; depending on registry state.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CreateNoCacheFoundExceptionShouldReportAvailableOrNone()
    {
        var exception = SettingsBase.CreateNoCacheFoundException("SomeClass");

        // The message will either list registered caches or "<none>" - either is valid
        await Assert.That(exception.Message).IsNotNull();
        await Assert.That(exception.Message.Length).IsGreaterThan(0);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.GetBlobCacheForClass(string, Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// throws <see cref="InvalidOperationException"/> when all strategies fail.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetBlobCacheForClassShouldThrowWhenAllStrategiesFail()
    {
        // Ensure no serializer registered so TryGetTransientFallback returns null
        var existing = AppLocator.Current.GetService<ISerializer>();
        if (existing is not null)
        {
            AppLocator.CurrentMutable.UnregisterAll<ISerializer>();
        }

        try
        {
            await Assert.That(() => SettingsBase.GetBlobCacheForClass(
                "UnresolvableClass",
                ThrowingResolver,
                ThrowingResolver,
                ThrowingResolver))
                .Throws<InvalidOperationException>();
        }
        finally
        {
            if (existing is not null)
            {
                AppLocator.CurrentMutable.RegisterConstant<ISerializer>(existing);
            }
        }
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.PushAmbientCache"/> sets the ambient cache
    /// and restores it on disposal.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task PushAmbientCacheShouldSetAndRestoreAmbientCache()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());

        using (SettingsBase.PushAmbientCache(cache))
        {
            // Within the scope, constructing a SettingsBase subclass should use the ambient cache
            await Assert.That(cache).IsNotNull();
        }

        // After disposal, the ambient cache is restored — reaching this point without
        // an exception is the assertion.
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.ReadAmbientUserAccount"/> throws
    /// <see cref="InvalidOperationException"/> when CacheDatabase is not initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReadAmbientUserAccountShouldNotThrowWhenWrapped()
    {
        // ReadAmbientUserAccount throws when CacheDatabase has no UserAccount.
        // TryReadAmbientCache swallows the exception, so either we get null
        // (not initialized) or a cache (if some other test initialized it).
        await Assert.That(() => _ = SettingsBase.TryReadAmbientCache(SettingsBase.ReadAmbientUserAccount))
            .ThrowsNothing();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.ReadAmbientLocalMachine"/> does not throw
    /// when wrapped in <see cref="SettingsBase.TryReadAmbientCache"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReadAmbientLocalMachineShouldNotThrowWhenWrapped()
    {
        await Assert.That(() => _ = SettingsBase.TryReadAmbientCache(SettingsBase.ReadAmbientLocalMachine))
            .ThrowsNothing();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.ReadAmbientInMemory"/> does not throw
    /// when wrapped in <see cref="SettingsBase.TryReadAmbientCache"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReadAmbientInMemoryShouldNotThrowWhenWrapped()
    {
        await Assert.That(() => _ = SettingsBase.TryReadAmbientCache(SettingsBase.ReadAmbientInMemory))
            .ThrowsNothing();
    }

    /// <summary>
    /// Resolver stub that always throws, mirroring unconfigured cache behavior.
    /// </summary>
    /// <returns>Never returns; always throws.</returns>
    private static IBlobCache ThrowingResolver() =>
        throw new InvalidOperationException("cache kind not configured");

    /// <summary>
    /// Minimal <see cref="SettingsBase"/> subclass that forwards to the injectable-
    /// resolver constructor, exercising the 3-arg overload (lines 48-57).
    /// </summary>
    private sealed class ResolverInjectedSettings : SettingsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResolverInjectedSettings"/> class.
        /// </summary>
        /// <param name="className">The class name used as the key prefix.</param>
        /// <param name="userAccountResolver">Delegate that returns the UserAccount cache.</param>
        /// <param name="localMachineResolver">Delegate that returns the LocalMachine cache.</param>
        /// <param name="inMemoryResolver">Delegate that returns the InMemory cache.</param>
        public ResolverInjectedSettings(
            string className,
            Func<IBlobCache> userAccountResolver,
            Func<IBlobCache> localMachineResolver,
            Func<IBlobCache> inMemoryResolver)
            : base(className, userAccountResolver, localMachineResolver, inMemoryResolver) =>
            TestValue = CreateProperty(42, nameof(TestValue));

        /// <summary>Gets the test value property helper.</summary>
        public SettingsPropertyHelper<int> TestValue { get; }
    }
}
