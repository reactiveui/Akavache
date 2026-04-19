// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

namespace Akavache.Settings.Tests;

/// <summary>
/// Tests the <c>settings?.Invoke(settingsDb)</c> null branch in
/// <see cref="AkavacheBuilderExtensions.WithSettingsStore{T}(IAkavacheBuilder, IBlobCache, Action{T}, string)"/>
/// by passing <see langword="null"/> for the <c>settings</c> action.
/// </summary>
[Category("Akavache")]
public class SettingsBuilderExtensionsNullSettingsTests
{
    /// <summary>
    /// Passing <see langword="null"/> for the <c>settings</c> action skips the
    /// <c>settings?.Invoke</c> call without throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task WithSettingsStore_NullSettingsAction_DoesNotThrow()
    {
        using InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        FakeAkavacheBuilder builder = new() { Serializer = new SystemJsonSerializer() };

        var result = builder.WithSettingsStore<ViewSettings>(cache, null!);

        await Assert.That(result).IsNotNull();

        // Clean up: dispose the settings store that was created.
        if (!builder.SettingsStores.TryGetValue(nameof(ViewSettings), out var store))
        {
            return;
        }

        store.Dispose();
    }

    /// <summary>
    /// Passing <see langword="null"/> for the <c>settings</c> action on the secure
    /// overload (<c>WithSecureSettingsStore</c>) skips <c>settings?.Invoke</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task WithSecureSettingsStore_NullSettingsAction_DoesNotThrow()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        FakeAkavacheBuilder builder = new()
        {
            Serializer = new SystemJsonSerializer(),
            SettingsCachePath = path,
        };

        var result = builder.WithSecureSettingsStore<ViewSettings>("password", null!);

        await Assert.That(result).IsNotNull();

        if (!builder.SettingsStores.TryGetValue(nameof(ViewSettings), out var store))
        {
            return;
        }

        store.Dispose();
    }

    /// <summary>
    /// Minimal <see cref="IAkavacheBuilder"/> stub for testing extension methods
    /// that only need <see cref="IAkavacheInstance.BlobCaches"/>,
    /// <see cref="IAkavacheInstance.SettingsStores"/>, and <see cref="IAkavacheInstance.Serializer"/>.
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
        public IAkavacheBuilder UseForcedDateTimeKind(DateTimeKind kind) => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithApplicationName(string? applicationName) => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithExecutingAssembly(Assembly assembly) => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithInMemory(IBlobCache cache) => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithInMemoryDefaults() => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithLegacyFileLocation() => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithLocalMachine(IBlobCache cache) => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithSecure(ISecureBlobCache cache) => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithUserAccount(IBlobCache cache) => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithSerializer<T>()
            where T : class, ISerializer, new() => this;

        /// <inheritdoc/>
        public IAkavacheBuilder WithSerializer<T>(Func<T> configure)
            where T : class, ISerializer => this;
    }
}
