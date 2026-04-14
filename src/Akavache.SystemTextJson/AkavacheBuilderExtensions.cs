// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using Akavache.Core;
using Akavache.Helpers;

namespace Akavache.SystemTextJson;

/// <summary>
/// Provides extension methods for configuring Akavache to use System.Text.Json serialization.
/// </summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>
    /// Configures the builder to use System.Text.Json serialization with default options.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static IAkavacheBuilder WithSerializerSystemTextJson(this IAkavacheBuilder builder)
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);

        builder.WithSerializer<SystemJsonSerializer>();
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
        return builder;
    }

    /// <summary>
    /// Configures the builder to use System.Text.Json serialization with custom options.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <param name="settings">The JSON serializer options to use for customizing serialization behavior.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="settings"/> is null.</exception>
    public static IAkavacheBuilder WithSerializerSystemTextJson(this IAkavacheBuilder builder, JsonSerializerOptions settings)
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNull(settings);

        builder.WithSerializer(() => new SystemJsonSerializer { Options = settings });
        UniversalSerializer.RegisterSerializer(() => new SystemJsonSerializer { Options = settings });
        return builder;
    }

    /// <summary>
    /// Configures the builder to use System.Text.Json serialization with options configured through a delegate.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <param name="configure">Action to configure the JSON serializer options.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    public static IAkavacheBuilder UseSystemTextJsonSerializer(this IAkavacheBuilder builder, Action<JsonSerializerOptions> configure)
    {
        ArgumentExceptionHelper.ThrowIfNull(builder);
        ArgumentExceptionHelper.ThrowIfNull(configure);

        JsonSerializerOptions settings = new();
        configure(settings);
        builder.WithSerializer(() => new SystemJsonSerializer { Options = settings });
        UniversalSerializer.RegisterSerializer(() => new SystemJsonSerializer { Options = settings });
        return builder;
    }
}
