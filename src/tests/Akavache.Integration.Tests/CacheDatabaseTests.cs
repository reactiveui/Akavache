// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Akavache.NewtonsoftJson;
using Akavache.Settings;
using Akavache.SystemTextJson;
using Akavache.Tests;
using Akavache.Tests.Executors;

namespace Akavache.Integration.Tests;

/// <summary>
/// Tests for CacheDatabase functionality and global configuration, including
/// uninitialized states, initialization overloads, and shutdown.
/// </summary>
[Category("Akavache")]
[NotInParallel]
[TestExecutor<AkavacheTestExecutor>]
public class CacheDatabaseTests
{
    /// <summary>
    /// Tests that CacheDatabase.TaskpoolScheduler is available and functional.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task TaskpoolSchedulerShouldBeAvailable()
    {
        // Act
        var scheduler = CacheDatabase.TaskpoolScheduler;

        // Assert
        await Assert.That(scheduler).IsNotNull();

        // Test that it can schedule work
        var workExecuted = false;
        ManualResetEventSlim resetEvent = new(false);

        scheduler.Schedule(() =>
        {
            workExecuted = true;
            resetEvent.Set();
        });

        using (Assert.Multiple())
        {
            // Wait for work to complete
            await Assert.That(resetEvent.Wait(5000)).IsTrue();
            await Assert.That(workExecuted).IsTrue();
        }
    }

    /// <summary>
    /// Tests that CacheDatabase properly validates serializer functionality.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SerializerFunctionalityValidationShouldWork()
    {
        // Arrange
        object[] testCases =
        [
            "string test",
            42,
            3.14d,
            true,
            DateTime.UtcNow,
            DateTimeOffset.Now,
            Guid.NewGuid(),
            new { Name = "Test", Value = 123 },
            (int[])[1, 2, 3, 4, 5],
            new Dictionary<string, object> { ["key1"] = "value1", ["key2"] = 42 }
        ];

        ISerializer[] serializers =
        [
            new SystemJsonSerializer(),
            new SystemJsonBsonSerializer(),
            new NewtonsoftSerializer(),
            new NewtonsoftBsonSerializer()
        ];

        foreach (var serializer in serializers)
        {
            // Assert - Test each serializer with various data types
            foreach (var testCase in testCases)
            {
                try
                {
                    var serialized = serializer.Serialize(testCase);
                    await Assert.That(serialized).IsNotNull();
                    await Assert.That(serialized).IsNotEmpty();

                    // For simple types, test round-trip
                    if (testCase is string or int or double or bool)
                    {
                        var deserialized = serializer.Deserialize<object>(serialized);

                        // For basic equality comparison, convert both to string
                        await Assert.That(deserialized?.ToString()).IsEqualTo(testCase.ToString());
                    }
                }
                catch (Exception ex)
                {
                    // Some serializers might not support all types - that's acceptable
                    // Just ensure we don't get unexpected exceptions
                    await Assert.That(ex)
                        .IsTypeOf<NotSupportedException>()
                        .Or.IsTypeOf<InvalidOperationException>();
                }
            }
        }
    }

    /// <summary>
    /// Tests ApplicationName getter throws when not initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ApplicationNameShouldThrowWhenNotInitialized() =>
        await Assert.That(static () => _ = CacheDatabase.ApplicationName)
            .Throws<InvalidOperationException>();

    /// <summary>
    /// Tests ForcedDateTimeKind getter throws when not initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindShouldThrowWhenNotInitialized() =>
        await Assert.That(static () => _ = CacheDatabase.ForcedDateTimeKind)
            .Throws<InvalidOperationException>();

    /// <summary>
    /// Tests InMemory getter throws when not initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InMemoryShouldThrowWhenNotInitialized() =>
        await Assert.That(static () => _ = CacheDatabase.InMemory)
            .Throws<InvalidOperationException>();

    /// <summary>
    /// Tests LocalMachine getter throws when not initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LocalMachineShouldThrowWhenNotInitialized() =>
        await Assert.That(static () => _ = CacheDatabase.LocalMachine)
            .Throws<InvalidOperationException>();

    /// <summary>
    /// Tests Secure getter throws when not initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SecureShouldThrowWhenNotInitialized() =>
        await Assert.That(static () => _ = CacheDatabase.Secure)
            .Throws<InvalidOperationException>();

    /// <summary>
    /// Tests UserAccount getter throws when not initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UserAccountShouldThrowWhenNotInitialized() =>
        await Assert.That(static () => _ = CacheDatabase.UserAccount)
            .Throws<InvalidOperationException>();

    /// <summary>
    /// Tests Shutdown is a no-op when not initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownShouldNoopWhenNotInitialized() =>
        await Assert.That(static () => CacheDatabase.Shutdown().SubscribeAndComplete()).ThrowsNothing();

    /// <summary>
    /// Tests Initialize with serializer factory.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeWithSerializerFactoryShouldSucceed()
    {
        CacheDatabase.Initialize(static () => new SystemJsonSerializer(), "TestApp_FactoryInit");
        await Assert.That(CacheDatabase.IsInitialized).IsTrue();
        await Assert.That(CacheDatabase.ApplicationName).IsEqualTo("TestApp_FactoryInit");
    }

    /// <summary>
    /// Tests Initialize with configure action throws on null configure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeWithConfigureShouldThrowOnNullConfigure() =>
        await Assert.That(static () =>
                CacheDatabase.Initialize<SystemJsonSerializer>((Action<IAkavacheBuilder>)null!, "TestApp"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests Initialize with configure action and serializer factory throws on null configure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeWithSerializerAndConfigureShouldThrowOnNullConfigure() =>
        await Assert.That(static () =>
                CacheDatabase.Initialize(static () => new SystemJsonSerializer(), null!, "TestApp"))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests Initialize with configure action invokes the action.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeWithConfigureShouldInvokeAction()
    {
        var configureCalled = false;

        CacheDatabase.Initialize<SystemJsonSerializer>(Configure, "TestApp_ConfigureCalled");

        await Assert.That(configureCalled).IsTrue();
        await Assert.That(CacheDatabase.IsInitialized).IsTrue();
        return;

        void Configure(IAkavacheBuilder b)
        {
            configureCalled = true;
            b.WithInMemoryDefaults();
        }
    }

    /// <summary>
    /// Tests Initialize with serializer factory and configure action invokes both.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeWithSerializerAndConfigureShouldInvokeAction()
    {
        var configureCalled = false;
        Action<IAkavacheBuilder> configure = b =>
        {
            configureCalled = true;
            b.WithInMemoryDefaults();
        };
        CacheDatabase.Initialize(
            static () => new SystemJsonSerializer(),
            configure,
            "TestApp_SerializerConfigureCalled");

        await Assert.That(configureCalled).IsTrue();
        await Assert.That(CacheDatabase.IsInitialized).IsTrue();
    }

    /// <summary>
    /// Tests Shutdown after initialization completes.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownAfterInitializeShouldComplete()
    {
        CacheDatabase.Initialize<SystemJsonSerializer>("TestApp_ShutdownTest");
        await CacheDatabase.Shutdown().FirstAsync();

        // After shutdown the data is flushed but state remains
        await Assert.That(CacheDatabase.IsInitialized).IsTrue();
    }

    /// <summary>
    /// Tests TaskpoolScheduler returns default when not overridden.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TaskpoolSchedulerShouldReturnDefault()
    {
        var scheduler = CacheDatabase.TaskpoolScheduler;
        await Assert.That(scheduler).IsNotNull();
    }

    /// <summary>
    /// Tests TaskpoolScheduler can be overridden.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TaskpoolSchedulerShouldBeOverridable()
    {
        var customScheduler = ImmediateScheduler.Instance;
        CacheDatabase.TaskpoolScheduler = customScheduler;
        await Assert.That(CacheDatabase.TaskpoolScheduler).IsSameReferenceAs(customScheduler);
    }

    /// <summary>
    /// Tests Shutdown disposes every entry in the current instance's BlobCaches and
    /// SettingsStores registries. The instance-scoped dicts are non-nullable, so the
    /// "skip nulls" branch that used to exist for the static-dict-with-null-values
    /// model is gone — this test validates the dispose-all-entries path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownShouldDisposeBlobCachesAndSettingsStores()
    {
        CacheDatabase.Initialize<SystemJsonSerializer>("TestApp_ShutdownDisposes");

        InMemoryBlobCache liveBlob = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        FakeSettingsStorage liveStore = new();

        var instance = CacheDatabase.CurrentInstance!;
        instance.BlobCaches["live"] = liveBlob;
        instance.SettingsStores["live"] = liveStore;

        await CacheDatabase.Shutdown().FirstAsync();

        using (Assert.Multiple())
        {
            await Assert.That(liveStore.Disposed).IsTrue();
            await Assert.That(CacheDatabase.IsInitialized).IsTrue();
        }
    }

    /// <summary>
    /// Tests Shutdown handles an empty BlobCaches/SettingsStores registry without
    /// throwing. This covers the instance-dict equivalent of the old "null-dict"
    /// safety path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownShouldSkipBlobCachesAndSettingsStoresWhenEmpty()
    {
        CacheDatabase.Initialize<SystemJsonSerializer>("TestApp_ShutdownEmptyDicts");

        await Assert.That(static async () => await CacheDatabase.Shutdown().FirstAsync()).ThrowsNothing();
    }

    /// <summary>
    /// Tests Shutdown catches exceptions thrown synchronously by a cache Flush call
    /// and rethrows them through the observable, which then triggers the catch
    /// in ResetForTests.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownShouldCatchFlushExceptionsAndResetForTestsShouldSwallowThem()
    {
        FakeAkavacheInstance fakeInstance = new() { UserAccount = new ThrowingFlushBlobCache(), };
        CacheDatabase.SetBuilder(fakeInstance);

        await Assert.That(static async () => await CacheDatabase.Shutdown().FirstAsync())
            .Throws<InvalidOperationException>();

        // ResetForTests should swallow the exception from Shutdown.
        await Assert.That(static async () => await CacheDatabase.ResetForTests()).ThrowsNothing();
        await Assert.That(CacheDatabase.IsInitialized).IsFalse();
    }

    /// <summary>
    /// Tests that accessing all CacheDatabase properties succeeds after initialization.
    /// Covers the non-null return branches of ApplicationName, ForcedDateTimeKind,
    /// InMemory, LocalMachine, Secure, and UserAccount.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task AllPropertiesShouldReturnValuesWhenInitialized()
    {
        CacheDatabase.Initialize<SystemJsonSerializer>("TestApp_AllProps");

        using (Assert.Multiple())
        {
            await Assert.That(CacheDatabase.ApplicationName).IsEqualTo("TestApp_AllProps");
            await Assert.That(CacheDatabase.InMemory).IsNotNull();
            await Assert.That(CacheDatabase.LocalMachine).IsNotNull();
            await Assert.That(CacheDatabase.Secure).IsNotNull();
            await Assert.That(CacheDatabase.UserAccount).IsNotNull();
        }
    }

    /// <summary>
    /// Tests that Shutdown handles null caches on the builder by substituting
    /// Observable.Return(Unit.Default) for each null flush, covering lines 124-127.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownShouldHandleNullCachesOnBuilder()
    {
        FakeAkavacheInstance fakeInstance = new()
        {
            UserAccount = null, LocalMachine = null, Secure = null, InMemory = null,
        };
        CacheDatabase.SetBuilder(fakeInstance);

        await Assert.That(static async () => await CacheDatabase.Shutdown().FirstAsync()).ThrowsNothing();
    }

    /// <summary>
    /// Tests that ResetForTests swallows exceptions from Shutdown when the
    /// underlying cache flush throws an observable error (not a synchronous exception).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResetForTestsShouldSwallowObservableShutdownErrors()
    {
        FakeAkavacheInstance fakeInstance = new() { UserAccount = new ObservableErrorFlushBlobCache(), };
        CacheDatabase.SetBuilder(fakeInstance);

        // ResetForTests should swallow the error from Shutdown.
        await Assert.That(static async () => await CacheDatabase.ResetForTests()).ThrowsNothing();
        await Assert.That(CacheDatabase.IsInitialized).IsFalse();
    }

    /// <summary>
    /// Tests that Shutdown returns Unit.Default when the task list is non-empty
    /// (covering the Merge/TakeLast/Select branch at line 134-136).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownShouldMergeAndReturnUnitWhenTasksExist()
    {
        CacheDatabase.Initialize<SystemJsonSerializer>("TestApp_ShutdownMerge");

        var result = await CacheDatabase.Shutdown().FirstAsync();
        await Assert.That(result).IsEqualTo(Unit.Default);
    }

    /// <summary>
    /// Tests that ForcedDateTimeKind returns the value when set on a builder with non-null ForcedDateTimeKind,
    /// covering the non-null branch at line 46.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindShouldReturnValueWhenSet()
    {
        FakeAkavacheInstance fakeInstance = new() { ForcedDateTimeKind = DateTimeKind.Utc, };
        CacheDatabase.SetBuilder(fakeInstance);

        await Assert.That(CacheDatabase.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests CreateBuilder throws on null/whitespace application name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateBuilderShouldThrowOnNullOrWhitespaceAppName()
    {
        await Assert.That(static () => CacheDatabase.CreateBuilder(null!))
            .Throws<ArgumentException>();
        await Assert.That(static () => CacheDatabase.CreateBuilder("   "))
            .Throws<ArgumentException>();
        await Assert.That(static () => CacheDatabase.CreateBuilder(string.Empty))
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Verifies that <see cref="CacheDatabase.Shutdown"/> returns immediately and yields
    /// <see cref="Unit.Default"/> when CacheDatabase has not been initialized — covers the
    /// early-return branch at the top of <c>Shutdown</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownShouldNoOpWhenNotInitialized()
    {
        await CacheDatabase.ResetForTests();

        var result = await CacheDatabase.Shutdown().FirstAsync();
        await Assert.That(result).IsEqualTo(Unit.Default);
        await Assert.That(CacheDatabase.IsInitialized).IsFalse();
    }

    /// <summary>
    /// Verifies that <see cref="CacheDatabase.ApplicationName"/> throws
    /// <see cref="InvalidOperationException"/> when the configured instance reports a null
    /// application name — covers the right-hand side of the <c>?? throw</c> in the property getter.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ApplicationNameGetterShouldThrowWhenInstanceApplicationNameIsNull()
    {
        CacheDatabase.SetBuilder(new FakeAkavacheInstance { ApplicationName = null! });
        await Assert.That(static () => CacheDatabase.ApplicationName).Throws<InvalidOperationException>();
        await CacheDatabase.ResetForTests();
    }

    /// <summary>
    /// Verifies that the cache instance property getters
    /// (<see cref="CacheDatabase.InMemory"/>, <see cref="CacheDatabase.LocalMachine"/>,
    /// <see cref="CacheDatabase.Secure"/>, <see cref="CacheDatabase.UserAccount"/>) throw
    /// <see cref="InvalidOperationException"/> when the configured instance returns null
    /// for those caches — closes the <c>?? throw</c> right-hand branch on each getter.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SubCacheGettersShouldThrowWhenInstanceReturnsNull()
    {
        CacheDatabase.SetBuilder(new FakeAkavacheInstance
        {
            InMemory = null, LocalMachine = null, Secure = null, UserAccount = null,
        });

        await Assert.That(static () => CacheDatabase.InMemory).Throws<InvalidOperationException>();
        await Assert.That(static () => CacheDatabase.LocalMachine).Throws<InvalidOperationException>();
        await Assert.That(static () => CacheDatabase.Secure).Throws<InvalidOperationException>();
        await Assert.That(static () => CacheDatabase.UserAccount).Throws<InvalidOperationException>();

        await CacheDatabase.ResetForTests();
    }

    /// <summary>
    /// Verifies that <see cref="CacheDatabase.ForcedDateTimeKind"/> throws
    /// <see cref="InvalidOperationException"/> when the configured instance reports a null
    /// kind — closes the right-hand branch of the <c>?? throw</c> in the getter.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ForcedDateTimeKindGetterShouldThrowWhenInstanceKindIsNull()
    {
        CacheDatabase.SetBuilder(new FakeAkavacheInstance { ForcedDateTimeKind = null });
        await Assert.That(static () => CacheDatabase.ForcedDateTimeKind).Throws<InvalidOperationException>();
        await CacheDatabase.ResetForTests();
    }

    /// <summary>
    /// A minimal in-memory fake of <see cref="ISettingsStorage"/> that tracks disposal.
    /// </summary>
    private sealed class FakeSettingsStorage : ISettingsStorage
    {
        /// <inheritdoc/>
        event System.ComponentModel.PropertyChangedEventHandler? System.ComponentModel.INotifyPropertyChanged.
            PropertyChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has been disposed.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Initializes the storage.
        /// </summary>
        /// <returns>A completed observable.</returns>
        public IObservable<Unit> Initialize() => Observable.Return(Unit.Default);

        /// <inheritdoc/>
        public void Dispose() => Disposed = true;
    }

    /// <summary>
    /// A minimal stub <see cref="IAkavacheInstance"/> used to drive the Shutdown path
    /// with a caller-supplied UserAccount cache.
    /// </summary>
    private sealed class FakeAkavacheInstance : IAkavacheInstance
    {
        /// <inheritdoc/>
        public System.Reflection.Assembly ExecutingAssembly { get; } = typeof(FakeAkavacheInstance).Assembly;

        /// <inheritdoc/>
        public string ApplicationName { get; set; } = "FakeAkavacheInstance";

        /// <inheritdoc/>
        public string? ApplicationRootPath { get; }

        /// <inheritdoc/>
        public string? SettingsCachePath { get; set; }

        /// <inheritdoc/>
        public string? ExecutingAssemblyName { get; }

        /// <inheritdoc/>
        public Version? Version { get; }

        /// <inheritdoc/>
        public IBlobCache? InMemory { get; set; }

        /// <inheritdoc/>
        public IBlobCache? LocalMachine { get; set; }

        /// <inheritdoc/>
        public ISecureBlobCache? Secure { get; set; }

        /// <inheritdoc/>
        public IBlobCache? UserAccount { get; set; }

        /// <inheritdoc/>
        public ISerializer? Serializer { get; } = new SystemJsonSerializer();

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public string? SerializerTypeName { get; }

        /// <inheritdoc/>
        public IDictionary<string, IBlobCache> BlobCaches { get; } = new Dictionary<string, IBlobCache>();

        /// <inheritdoc/>
        public IDictionary<string, ISettingsStorage> SettingsStores { get; } =
            new Dictionary<string, ISettingsStorage>();
    }

    /// <summary>
    /// An <see cref="IBlobCache"/> test double whose <see cref="Flush()"/> method throws
    /// synchronously, forcing <see cref="CacheDatabase.Shutdown"/> into its catch block.
    /// Only the members actually used by Shutdown are implemented.
    /// </summary>
    private sealed class ThrowingFlushBlobCache : IBlobCache
    {
        /// <inheritdoc/>
        public ISerializer Serializer { get; } = new SystemJsonSerializer();

        /// <inheritdoc/>
        public IScheduler Scheduler { get; } = ImmediateScheduler.Instance;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IObservable<Unit> Flush() => throw new InvalidOperationException("Simulated flush failure.");

        /// <inheritdoc/>
        public IObservable<Unit> Flush(Type type) => throw new InvalidOperationException("Simulated flush failure.");

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            DateTimeOffset? absoluteExpiration = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            Type type,
            DateTimeOffset? absoluteExpiration = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit>
            Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Vacuum() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(
            IEnumerable<string> keys,
            Type type,
            DateTimeOffset? absoluteExpiration) => throw new NotImplementedException();
    }

    /// <summary>
    /// An <see cref="IBlobCache"/> test double whose <see cref="Flush()"/> method returns
    /// an observable error (rather than throwing synchronously), to exercise the ResetForTests catch block
    /// when the error propagates through the observable pipeline.
    /// </summary>
    private sealed class ObservableErrorFlushBlobCache : IBlobCache
    {
        /// <inheritdoc/>
        public ISerializer Serializer { get; } = new SystemJsonSerializer();

        /// <inheritdoc/>
        public IScheduler Scheduler { get; } = ImmediateScheduler.Instance;

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public IObservable<Unit> Flush() =>
            Observable.Throw<Unit>(new InvalidOperationException("Simulated observable flush failure."));

        /// <inheritdoc/>
        public IObservable<Unit> Flush(Type type) =>
            Observable.Throw<Unit>(new InvalidOperationException("Simulated observable flush failure."));

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            DateTimeOffset? absoluteExpiration = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Insert(
            IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
            Type type,
            DateTimeOffset? absoluteExpiration = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit>
            Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<byte[]?> Get(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<string> GetAllKeys(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(string key, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll(Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> InvalidateAll() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> Vacuum() => throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IObservable<Unit> UpdateExpiration(
            IEnumerable<string> keys,
            Type type,
            DateTimeOffset? absoluteExpiration) => throw new NotImplementedException();
    }
}
