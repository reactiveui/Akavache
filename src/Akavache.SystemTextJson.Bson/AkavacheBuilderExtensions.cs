// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using Akavache.Core;

namespace Akavache.SystemTextJson.Bson;

/// <summary>
/// Provides extension methods for configuring Akavache to use System.Text.Json BSON serialization.
/// </summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>
    /// Configures the builder to use System.Text.Json BSON serialization with default options.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static IAkavacheBuilder UseSystemJsonBsonSerializer(this IAkavacheBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.WithSerializer<SystemJsonBsonSerializer>();
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonBsonSerializer());
        return builder;
    }

    /// <summary>
    /// Configures the builder to use System.Text.Json BSON serialization with custom options.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <param name="settings">The JSON serializer options to use for customizing BSON serialization behavior.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="settings"/> is null.</exception>
    public static IAkavacheBuilder UseSystemJsonBsonSerializer(this IAkavacheBuilder builder, JsonSerializerOptions settings)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        builder.WithSerializer(() => new SystemJsonBsonSerializer { Options = settings, });
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
        UniversalSerializer.RegisterSerializer(() => new SystemJsonBsonSerializer { Options = settings, });
        return builder;
    }

    /// <summary>
    /// Configures the builder to use System.Text.Json BSON serialization with options configured through a delegate.
    /// </summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    /// <param name="configure">Action to configure the JSON serializer options for BSON serialization.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    public static IAkavacheBuilder UseSystemJsonBsonSerializer(this IAkavacheBuilder builder, Action<JsonSerializerOptions> configure)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var settings = new JsonSerializerOptions();
        configure(settings);
        builder.WithSerializer(() => new SystemJsonBsonSerializer { Options = settings, });
        UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
        UniversalSerializer.RegisterSerializer(() => new SystemJsonBsonSerializer { Options = settings, });
        return builder;
    }
}