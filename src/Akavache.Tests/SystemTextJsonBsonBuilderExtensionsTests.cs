// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using Akavache.Core;
using Akavache.SystemTextJson.Bson;
using Akavache.Tests.Executors;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.SystemTextJson.Bson.AkavacheBuilderExtensions.
/// </summary>
[Category("Akavache")]
[NotInParallel("CacheDatabaseState")]
public class SystemTextJsonBsonBuilderExtensionsTests
{
    /// <summary>
    /// Tests UseSystemJsonBsonSerializer() throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemJsonBsonSerializerShouldThrowOnNullBuilder() =>
        await Assert.That(static () => Akavache.SystemTextJson.Bson.AkavacheBuilderExtensions.UseSystemJsonBsonSerializer(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests UseSystemJsonBsonSerializer() registers serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemJsonBsonSerializerShouldRegisterSerializer()
    {
        var builder = CreateBuilder("UseSystemJsonBsonSerializerDefault");
        var result = builder.UseSystemJsonBsonSerializer();

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.SerializerTypeName).IsNotNull();
    }

    /// <summary>
    /// Tests UseSystemJsonBsonSerializer(settings) throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemJsonBsonSerializerSettingsShouldThrowOnNullBuilder() =>
        await Assert.That(static () => Akavache.SystemTextJson.Bson.AkavacheBuilderExtensions.UseSystemJsonBsonSerializer(null!, new JsonSerializerOptions()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests UseSystemJsonBsonSerializer(settings) throws on null settings.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemJsonBsonSerializerSettingsShouldThrowOnNullSettings()
    {
        var builder = CreateBuilder("UseSystemJsonBsonSerializerNullSettings");
        await Assert.That(() => builder.UseSystemJsonBsonSerializer((JsonSerializerOptions)null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests UseSystemJsonBsonSerializer(settings) registers serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemJsonBsonSerializerSettingsShouldRegisterSerializer()
    {
        var builder = CreateBuilder("UseSystemJsonBsonSerializerSettings");
        var settings = new JsonSerializerOptions { WriteIndented = true };
        var result = builder.UseSystemJsonBsonSerializer(settings);

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.SerializerTypeName).IsNotNull();
    }

    /// <summary>
    /// Tests UseSystemJsonBsonSerializer(configure) throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemJsonBsonSerializerConfigureShouldThrowOnNullBuilder()
    {
        Action<JsonSerializerOptions> configure = _ => { };
        await Assert.That(() => Akavache.SystemTextJson.Bson.AkavacheBuilderExtensions.UseSystemJsonBsonSerializer(null!, configure))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests UseSystemJsonBsonSerializer(configure) throws on null configure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemJsonBsonSerializerConfigureShouldThrowOnNullConfigure()
    {
        var builder = CreateBuilder("UseSystemJsonBsonSerializerNullConfigure");
        await Assert.That(() => builder.UseSystemJsonBsonSerializer((Action<JsonSerializerOptions>)null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests UseSystemJsonBsonSerializer(configure) invokes configure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemJsonBsonSerializerConfigureShouldInvokeAction()
    {
        var builder = CreateBuilder("UseSystemJsonBsonSerializerConfigure");
        var configureInvoked = false;
        var result = builder.UseSystemJsonBsonSerializer(o =>
        {
            configureInvoked = true;
            o.WriteIndented = true;
        });

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(configureInvoked).IsTrue();
    }

    /// <summary>
    /// Exercises every <c>UseSystemJsonBsonSerializer</c> overload and then enumerates
    /// alternative serializers so the registered factory lambdas execute and their
    /// compiler-generated closure classes get coverage.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task UseSystemJsonBsonSerializerVariantsShouldExecuteRegisteredFactoryLambdas()
    {
        UniversalSerializer.ResetCaches();

        var builder = CreateBuilder("UseSystemJsonBsonSerializerVariantsFactoryExec");
        builder.UseSystemJsonBsonSerializer();
        builder.UseSystemJsonBsonSerializer(new JsonSerializerOptions { WriteIndented = true });
        builder.UseSystemJsonBsonSerializer(o => o.WriteIndented = true);

        var alternatives = UniversalSerializer.GetAvailableAlternativeSerializers(new Akavache.NewtonsoftJson.NewtonsoftSerializer());

        await Assert.That(alternatives.Count).IsGreaterThanOrEqualTo(1);

        UniversalSerializer.ResetCaches();
    }

    private static IAkavacheBuilder CreateBuilder(string applicationName) =>
        CacheDatabase.CreateBuilder().WithApplicationName(applicationName);
}
