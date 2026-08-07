// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

#if REACTIVE_SHIM
namespace Akavache.Reactive.SystemTextJson.Bson;
#else
namespace Akavache.SystemTextJson.Bson;
#endif

/// <summary>Provides extension methods for configuring Akavache to use System.Text.Json BSON serialization.</summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>Extension members for <c>IAkavacheBuilder</c>.</summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    extension(IAkavacheBuilder builder)
    {
        /// <summary>Configures the builder to use System.Text.Json BSON serialization with default options.</summary>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public IAkavacheBuilder UseSystemJsonBsonSerializer()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            _ = builder.WithSerializer<SystemJsonBsonSerializer>();
            UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
            UniversalSerializer.RegisterSerializer(static () => new SystemJsonBsonSerializer());
            return builder;
        }

        /// <summary>Configures the builder to use System.Text.Json BSON serialization with custom options.</summary>
        /// <param name="settings">The JSON serializer options to use for customizing BSON serialization behavior.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="settings"/> is null.</exception>
        public IAkavacheBuilder UseSystemJsonBsonSerializer(JsonSerializerOptions settings)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);
            ArgumentExceptionHelper.ThrowIfNull(settings);

            _ = builder.WithSerializer(() => new SystemJsonBsonSerializer { Options = settings, });
            UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
            UniversalSerializer.RegisterSerializer(() => new SystemJsonBsonSerializer { Options = settings, });
            return builder;
        }

        /// <summary>Configures the builder to use System.Text.Json BSON serialization with options configured through a delegate.</summary>
        /// <param name="configure">Action to configure the JSON serializer options for BSON serialization.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
        [SuppressMessage(
            "Performance",
            "PSH1416:Cache the constructed options in a static readonly field",
            Justification = "These options exist to be mutated by the caller's configure delegate, so each "
                + "call needs its own instance. Sharing one static instance would leak one caller's "
                + "configuration into every other caller.")]
        public IAkavacheBuilder UseSystemJsonBsonSerializer(Action<JsonSerializerOptions> configure)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);
            ArgumentExceptionHelper.ThrowIfNull(configure);

            JsonSerializerOptions settings = new();
            configure(settings);
            _ = builder.WithSerializer(() => new SystemJsonBsonSerializer { Options = settings, });
            UniversalSerializer.RegisterSerializer(static () => new SystemJsonSerializer());
            UniversalSerializer.RegisterSerializer(() => new SystemJsonBsonSerializer { Options = settings, });
            return builder;
        }
    }
}
