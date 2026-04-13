// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Tests.Executors;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.SystemTextJson.AkavacheBuilderExtensions.
/// </summary>
[Category("Akavache")]
[NotInParallel("CacheDatabaseState")]
public class SystemTextJsonBuilderExtensionsTests
{
    /// <summary>
    /// Tests WithSerializerSystemTextJson() throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerSystemTextJsonShouldThrowOnNullBuilder() =>
        await Assert.That(static () => Akavache.SystemTextJson.AkavacheBuilderExtensions.WithSerializerSystemTextJson(null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithSerializerSystemTextJson() registers serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerSystemTextJsonShouldRegisterSerializer()
    {
        var builder = CreateBuilder("WithSerializerSystemTextJsonDefault");
        var result = builder.WithSerializerSystemTextJson();

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.SerializerTypeName).IsNotNull();
    }

    /// <summary>
    /// Tests WithSerializerSystemTextJson(settings) throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerSystemTextJsonSettingsShouldThrowOnNullBuilder() =>
        await Assert.That(static () => Akavache.SystemTextJson.AkavacheBuilderExtensions.WithSerializerSystemTextJson(null!, new JsonSerializerOptions()))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests WithSerializerSystemTextJson(settings) throws on null settings.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerSystemTextJsonSettingsShouldThrowOnNullSettings()
    {
        var builder = CreateBuilder("WithSerializerSystemTextJsonNullSettings");
        await Assert.That(() => builder.WithSerializerSystemTextJson((JsonSerializerOptions)null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests WithSerializerSystemTextJson(settings) registers serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerSystemTextJsonSettingsShouldRegisterSerializer()
    {
        var builder = CreateBuilder("WithSerializerSystemTextJsonSettings");
        var settings = new JsonSerializerOptions { WriteIndented = true };
        var result = builder.WithSerializerSystemTextJson(settings);

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.SerializerTypeName).IsNotNull();
    }

    /// <summary>
    /// Tests UseSystemTextJsonSerializer(configure) throws on null builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemTextJsonSerializerConfigureShouldThrowOnNullBuilder()
    {
        Action<JsonSerializerOptions> configure = _ => { };
        await Assert.That(() => Akavache.SystemTextJson.AkavacheBuilderExtensions.UseSystemTextJsonSerializer(null!, configure))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests UseSystemTextJsonSerializer(configure) throws on null configure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemTextJsonSerializerConfigureShouldThrowOnNullConfigure()
    {
        var builder = CreateBuilder("UseSystemTextJsonSerializerNullConfigure");
        await Assert.That(() => builder.UseSystemTextJsonSerializer(null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests UseSystemTextJsonSerializer(configure) invokes configure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseSystemTextJsonSerializerConfigureShouldInvokeAction()
    {
        var builder = CreateBuilder("UseSystemTextJsonSerializerConfigure");
        var configureInvoked = false;
        var result = builder.UseSystemTextJsonSerializer(o =>
        {
            configureInvoked = true;
            o.WriteIndented = true;
        });

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(configureInvoked).IsTrue();
    }

    /// <summary>
    /// Tests that the <c>static () =&gt;</c> factory lambdas registered by each
    /// <c>WithSerializerSystemTextJson</c>/<c>UseSystemTextJsonSerializer</c> overload
    /// actually execute when <see cref="UniversalSerializer.GetAvailableAlternativeSerializers"/>
    /// enumerates them.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task WithSerializerSystemTextJsonVariantsShouldExecuteRegisteredFactoryLambdas()
    {
        UniversalSerializer.ResetCaches();

        var builder = CreateBuilder("WithSerializerSystemTextJsonVariantsFactoryExec");
        builder.WithSerializerSystemTextJson();
        builder.WithSerializerSystemTextJson(new JsonSerializerOptions { WriteIndented = true });
        builder.UseSystemTextJsonSerializer(o => o.WriteIndented = true);

        // Pass a primary that isn't SystemJsonSerializer so the registered factories
        // are kept in the alternatives list and therefore invoked.
        var alternatives = UniversalSerializer.GetAvailableAlternativeSerializers(new NewtonsoftJson.NewtonsoftSerializer());

        await Assert.That(alternatives.Count).IsGreaterThanOrEqualTo(1);

        UniversalSerializer.ResetCaches();
    }

    /// <summary>
    /// Creates a fresh <see cref="IAkavacheBuilder"/> with a unique application name.
    /// </summary>
    /// <param name="applicationName">The application name used for builder isolation.</param>
    /// <returns>A new builder instance.</returns>
    private static IAkavacheBuilder CreateBuilder(string applicationName) =>
        CacheDatabase.CreateBuilder().WithApplicationName(applicationName);
}
