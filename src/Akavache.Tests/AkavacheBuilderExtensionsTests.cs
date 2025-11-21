// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
using Akavache.Core;
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using NUnit.Framework;
using Splat;

namespace Akavache.Tests;

/// <summary>
/// Tests for AkavacheBuilderExtensions covering initialization overloads and path resolution.
/// </summary>
[TestFixture]
[Category("Builder")]
public class AkavacheBuilderExtensionsTests
{
    /// <summary>
    /// Cleanup registrations before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        if (AppLocator.CurrentMutable.HasRegistration(typeof(ISerializer)))
        {
            AppLocator.CurrentMutable.UnregisterAll(typeof(ISerializer));
        }
    }

    /// <summary>
    /// Validates null builder causes ArgumentNullException for generic overload.
    /// </summary>
    [Test]
    public void WithAkavacheCacheDatabase_Generic_ThrowsOnNullBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => ((Splat.Builder.IAppBuilder)null!).WithAkavacheCacheDatabase<SystemJsonSerializer>("TestApp"));
    }

    /// <summary>
    /// Ensures action configure overload registers serializer.
    /// </summary>
    [Test]
    public void WithAkavacheCacheDatabase_ActionConfigure_RegistersServices()
    {
        var serializerType = typeof(SystemJsonSerializer);
        var serializerTypeName = serializerType.AssemblyQualifiedName;
        var builder = Splat.Builder.AppBuilder.CreateSplatBuilder();
        var app = builder.WithAkavacheCacheDatabase<SystemJsonSerializer>(b => b.WithInMemoryDefaults(), "TestApp").Build();
        var serializer = app.Current?.GetService<Akavache.ISerializer>(serializerTypeName);
        Assert.That(serializer, Is.Not.Null);
    }

    /// <summary>
    /// Ensures InMemory defaults create instance with application name.
    /// </summary>
    [Test]
    public void WithAkavache_InMemory_Defaults_CreateInstance()
    {
        var builder = Splat.Builder.AppBuilder.CreateSplatBuilder();
        IAkavacheInstance? instanceCaptured = null;
        builder.WithAkavache<SystemJsonSerializer>("TestApp", (Action<IAkavacheInstance>)(inst => instanceCaptured = inst));
        Assert.That(instanceCaptured, Is.Not.Null);
        Assert.That(instanceCaptured!.ApplicationName, Is.EqualTo("TestApp"));
    }

    /// <summary>
    /// Valid legacy path contains application name.
    /// </summary>
    [Test]
    public void Builder_GetLegacyCacheDirectory_ReturnsValidPath()
    {
        var builder = Splat.Builder.AppBuilder.CreateSplatBuilder();
        IAkavacheInstance? instanceCaptured = null;
        builder.WithAkavache<SystemJsonSerializer>("LegacyTestApp", (Action<IAkavacheInstance>)(inst => instanceCaptured = inst));
        Assert.That(instanceCaptured, Is.Not.Null);
        var localMachinePath = instanceCaptured!.GetLegacyCacheDirectory("LocalMachine");
        Assert.That(localMachinePath, Is.Not.Null);
        Assert.That(localMachinePath, Does.Contain("LegacyTestApp"));
    }

    /// <summary>
    /// Isolated cache directory returns non-null for user account.
    /// </summary>
    [Test]
    public void Builder_GetIsolatedCacheDirectory_ReturnsNonNullForUserAccount()
    {
        var builder = Splat.Builder.AppBuilder.CreateSplatBuilder();
        IAkavacheInstance? instanceCaptured = null;
        builder.WithAkavache<SystemJsonSerializer>("IsoTestApp", (Action<IAkavacheInstance>)(inst => instanceCaptured = inst));
        Assert.That(instanceCaptured, Is.Not.Null);
        var isolatedPath = instanceCaptured!.GetIsolatedCacheDirectory("UserAccount");
        Assert.That(isolatedPath, Is.Not.Null);
    }

    /// <summary>
    /// Null configure throws.
    /// </summary>
    [Test]
    public void Builder_WithAkavache_ThrowsOnNullConfigure()
    {
        var builder = Splat.Builder.AppBuilder.CreateSplatBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.WithAkavache<SystemJsonSerializer>("TestApp", (Action<IAkavacheBuilder>)null!, (Action<IAkavacheInstance>)(inst => { })));
    }

    /// <summary>
    /// Null instance callback throws.
    /// </summary>
    [Test]
    public void Builder_WithAkavache_ThrowsOnNullInstanceCallback()
    {
        var builder = Splat.Builder.AppBuilder.CreateSplatBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.WithAkavache<SystemJsonSerializer>("TestApp", b => { }, (Action<IAkavacheInstance>)null!));
    }

    /// <summary>
    /// Custom serializer factory is used for registration.
    /// </summary>
    [Test]
    public void WithAkavacheCacheDatabase_ConfiguredSerializerFactory_Used()
    {
        var serializerType = typeof(NewtonsoftSerializer);
        var serializerTypeName = serializerType.AssemblyQualifiedName;
        var builder = Splat.Builder.AppBuilder.CreateSplatBuilder();
        var customSerializer = new NewtonsoftSerializer();
        builder.WithAkavacheCacheDatabase(() => customSerializer, b => b.WithInMemoryDefaults(), "FactoryApp");
        var registered = Locator.Current.GetService<ISerializer>(serializerTypeName);
        Assert.That(registered, Is.SameAs(customSerializer));
    }
}
