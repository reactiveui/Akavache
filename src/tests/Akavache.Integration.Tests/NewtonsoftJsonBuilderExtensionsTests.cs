// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Newtonsoft.Json;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests for NewtonsoftJson.AkavacheBuilderExtensions.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class NewtonsoftJsonBuilderExtensionsTests
{
    /// <summary>The number of Newtonsoft serializers (JSON and BSON) this fixture registers as alternatives.</summary>
    private const int ExpectedAlternativeSerializerCount = 2;

    /// <summary>Tests WithSerializerNewtonsoftJson() throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftJsonShouldThrowOnNullBuilder() =>
        await Assert.That(static () => NewtonsoftJson.AkavacheBuilderExtensions.WithSerializerNewtonsoftJson(null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests WithSerializerNewtonsoftJson() registers serializer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftJsonShouldRegisterSerializer()
    {
        var builder = CreateBuilder("WithSerializerNewtonsoftJsonDefault");
        var result = builder.WithSerializerNewtonsoftJson();

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.SerializerTypeName).IsNotNull();
    }

    /// <summary>Tests WithSerializerNewtonsoftJson(settings) throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftJsonSettingsShouldThrowOnNullBuilder() =>
        await Assert.That(static () => NewtonsoftJson.AkavacheBuilderExtensions.WithSerializerNewtonsoftJson(null!, new JsonSerializerSettings()))
            .Throws<ArgumentNullException>();

    /// <summary>Tests WithSerializerNewtonsoftJson(settings) throws on null settings.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftJsonSettingsShouldThrowOnNullSettings()
    {
        var builder = CreateBuilder("WithSerializerNewtonsoftJsonNullSettings");
        await Assert.That(() => builder.WithSerializerNewtonsoftJson((JsonSerializerSettings)null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithSerializerNewtonsoftJson(settings) registers serializer with custom settings.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftJsonSettingsShouldRegisterSerializer()
    {
        var builder = CreateBuilder("WithSerializerNewtonsoftJsonSettings");
        JsonSerializerSettings settings = new() { Formatting = Formatting.Indented };
        var result = builder.WithSerializerNewtonsoftJson(settings);

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.SerializerTypeName).IsNotNull();
    }

    /// <summary>Tests WithSerializerNewtonsoftJson(configure) throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftJsonConfigureShouldThrowOnNullBuilder()
    {
        Action<JsonSerializerSettings> configure = static _ => { };
        await Assert.That(() => NewtonsoftJson.AkavacheBuilderExtensions.WithSerializerNewtonsoftJson(null!, configure))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithSerializerNewtonsoftJson(configure) throws on null configure.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftJsonConfigureShouldThrowOnNullConfigure()
    {
        var builder = CreateBuilder("WithSerializerNewtonsoftJsonNullConfigure");
        await Assert.That(() => builder.WithSerializerNewtonsoftJson((Action<JsonSerializerSettings>)null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithSerializerNewtonsoftJson(configure) invokes configure and registers serializer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftJsonConfigureShouldInvokeAction()
    {
        var builder = CreateBuilder("WithSerializerNewtonsoftJsonConfigure");
        var configureInvoked = false;
        var result = builder.WithSerializerNewtonsoftJson(s =>
        {
            configureInvoked = true;
            s.Formatting = Formatting.Indented;
        });

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(configureInvoked).IsTrue();
    }

    /// <summary>Tests WithSerializerNewtonsoftBson() throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftBsonShouldThrowOnNullBuilder() =>
        await Assert.That(static () => NewtonsoftJson.AkavacheBuilderExtensions.WithSerializerNewtonsoftBson(null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests WithSerializerNewtonsoftBson() registers serializer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftBsonShouldRegisterSerializer()
    {
        var builder = CreateBuilder("WithSerializerNewtonsoftBsonDefault");
        var result = builder.WithSerializerNewtonsoftBson();

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.SerializerTypeName).IsNotNull();
    }

    /// <summary>Tests WithSerializerNewtonsoftBson(settings) throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftBsonSettingsShouldThrowOnNullBuilder() =>
        await Assert.That(static () => NewtonsoftJson.AkavacheBuilderExtensions.WithSerializerNewtonsoftBson(null!, new JsonSerializerSettings()))
            .Throws<ArgumentNullException>();

    /// <summary>Tests WithSerializerNewtonsoftBson(settings) throws on null settings.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftBsonSettingsShouldThrowOnNullSettings()
    {
        var builder = CreateBuilder("WithSerializerNewtonsoftBsonNullSettings");
        await Assert.That(() => builder.WithSerializerNewtonsoftBson((JsonSerializerSettings)null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithSerializerNewtonsoftBson(settings) registers serializer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftBsonSettingsShouldRegisterSerializer()
    {
        var builder = CreateBuilder("WithSerializerNewtonsoftBsonSettings");
        var result = builder.WithSerializerNewtonsoftBson(new JsonSerializerSettings { Formatting = Formatting.Indented });

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.SerializerTypeName).IsNotNull();
    }

    /// <summary>Tests WithSerializerNewtonsoftBson(configure) throws on null builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftBsonConfigureShouldThrowOnNullBuilder()
    {
        Action<JsonSerializerSettings> configure = static _ => { };
        await Assert.That(() => NewtonsoftJson.AkavacheBuilderExtensions.WithSerializerNewtonsoftBson(null!, configure))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithSerializerNewtonsoftBson(configure) throws on null configure.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftBsonConfigureShouldThrowOnNullConfigure()
    {
        var builder = CreateBuilder("WithSerializerNewtonsoftBsonNullConfigure");
        await Assert.That(() => builder.WithSerializerNewtonsoftBson((Action<JsonSerializerSettings>)null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests WithSerializerNewtonsoftBson(configure) invokes configure and registers serializer.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerNewtonsoftBsonConfigureShouldInvokeAction()
    {
        var builder = CreateBuilder("WithSerializerNewtonsoftBsonConfigure");
        var configureInvoked = false;
        var result = builder.WithSerializerNewtonsoftBson(s =>
        {
            configureInvoked = true;
            s.Formatting = Formatting.Indented;
        });

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(configureInvoked).IsTrue();
    }

    /// <summary>
    /// Tests that the <c>static () =&gt;</c> factory lambdas passed to
    /// <see cref="UniversalSerializer.RegisterSerializer"/> by each
    /// <c>WithSerializerNewtonsoft*</c> overload actually execute when
    /// <see cref="UniversalSerializer.GetAvailableAlternativeSerializers"/> enumerates
    /// them. Without this test those lambda bodies live in the compiler-generated
    /// <c>&lt;&gt;c</c> closure class with zero coverage because the builder tests above
    /// only verify the registration happens — never that the factories can run.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [TestExecutor<AkavacheTestExecutor>]
    public async Task WithSerializerNewtonsoftVariantsShouldExecuteRegisteredFactoryLambdas()
    {
        UniversalSerializer.ResetCaches();

        var builder = CreateBuilder("WithSerializerNewtonsoftVariantsFactoryExec");
        _ = builder.WithSerializerNewtonsoftJson();
        _ = builder.WithSerializerNewtonsoftJson(new JsonSerializerSettings { Formatting = Formatting.Indented });
        _ = builder.WithSerializerNewtonsoftJson(static s => s.Formatting = Formatting.Indented);
        _ = builder.WithSerializerNewtonsoftBson();
        _ = builder.WithSerializerNewtonsoftBson(new JsonSerializerSettings { Formatting = Formatting.Indented });
        _ = builder.WithSerializerNewtonsoftBson(static s => s.Formatting = Formatting.Indented);

        // Force every registered factory lambda (including the static () => new ... ones)
        // to actually execute by enumerating alternatives against an unrelated primary.
        var alternatives = UniversalSerializer.GetAvailableAlternativeSerializers(new SystemJsonSerializer());

        await Assert.That(alternatives.Count).IsGreaterThanOrEqualTo(ExpectedAlternativeSerializerCount);

        UniversalSerializer.ResetCaches();
    }

    /// <summary>Creates a real <see cref="IAkavacheBuilder"/> with the given application name.</summary>
    /// <param name="applicationName">The application name to assign to the builder.</param>
    /// <returns>A configured <see cref="IAkavacheBuilder"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IAkavacheBuilder CreateBuilder(string applicationName) =>
        CacheDatabase.CreateBuilder().WithApplicationName(applicationName);
}
