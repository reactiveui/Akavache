// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

#if REACTIVE_SHIM
namespace Akavache.Reactive.NewtonsoftJson;
#else
namespace Akavache.NewtonsoftJson;
#endif

/// <summary>Provides extension methods for configuring Akavache to use Newtonsoft.Json serialization.</summary>
public static class AkavacheBuilderExtensions
{
    /// <summary>Extension members for <c>IAkavacheBuilder</c>.</summary>
    /// <param name="builder">The Akavache builder to configure.</param>
    extension(IAkavacheBuilder builder)
    {
        /// <summary>Configures the builder to use Newtonsoft.Json serialization with default settings.</summary>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public IAkavacheBuilder WithSerializerNewtonsoftJson()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            _ = builder.WithSerializer<NewtonsoftSerializer>();
            UniversalSerializer.RegisterSerializer(static () => new NewtonsoftSerializer());
            UniversalSerializer.RegisterSerializer(static () => new NewtonsoftBsonSerializer());
            return builder;
        }

        /// <summary>Configures the builder to use Newtonsoft.Json serialization with custom settings.</summary>
        /// <param name="settings">The JSON serializer settings to use for customizing serialization behavior.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="settings"/> is null.</exception>
        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public IAkavacheBuilder WithSerializerNewtonsoftJson(JsonSerializerSettings settings)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);
            ArgumentExceptionHelper.ThrowIfNull(settings);

            _ = builder.WithSerializer(() => new NewtonsoftSerializer { Options = settings });
            UniversalSerializer.RegisterSerializer(() => new NewtonsoftSerializer { Options = settings });
            UniversalSerializer.RegisterSerializer(static () => new NewtonsoftBsonSerializer());
            return builder;
        }

        /// <summary>Configures the builder to use Newtonsoft.Json serialization with settings configured through a delegate.</summary>
        /// <param name="configure">Action to configure the JSON serializer settings.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public IAkavacheBuilder WithSerializerNewtonsoftJson(Action<JsonSerializerSettings> configure)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);
            ArgumentExceptionHelper.ThrowIfNull(configure);

            JsonSerializerSettings settings = new();
            configure(settings);
            _ = builder.WithSerializer(() => new NewtonsoftSerializer { Options = settings });
            UniversalSerializer.RegisterSerializer(() => new NewtonsoftSerializer { Options = settings });
            UniversalSerializer.RegisterSerializer(static () => new NewtonsoftBsonSerializer());
            return builder;
        }

        /// <summary>Configures the builder to use Newtonsoft.Json BSON serialization with default settings.</summary>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public IAkavacheBuilder WithSerializerNewtonsoftBson()
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            _ = builder.WithSerializer<NewtonsoftBsonSerializer>();
            UniversalSerializer.RegisterSerializer(static () => new NewtonsoftSerializer());
            UniversalSerializer.RegisterSerializer(static () => new NewtonsoftBsonSerializer());
            return builder;
        }

        /// <summary>Configures the builder to use Newtonsoft.Json BSON serialization with custom settings.</summary>
        /// <param name="settings">The JSON serializer settings to use for customizing BSON serialization behavior.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="settings"/> is null.</exception>
        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public IAkavacheBuilder WithSerializerNewtonsoftBson(JsonSerializerSettings settings)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);
            ArgumentExceptionHelper.ThrowIfNull(settings);

            NewtonsoftBsonSerializer serializer = new() { Options = settings, };
            _ = builder.WithSerializer(() => serializer);
            UniversalSerializer.RegisterSerializer(static () => new NewtonsoftSerializer());
            UniversalSerializer.RegisterSerializer(() => new NewtonsoftBsonSerializer { Options = settings, });
            return builder;
        }

        /// <summary>Configures the builder to use Newtonsoft.Json BSON serialization with settings configured through a delegate.</summary>
        /// <param name="configure">Action to configure the JSON serializer settings for BSON serialization.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public IAkavacheBuilder WithSerializerNewtonsoftBson(Action<JsonSerializerSettings> configure)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);
            ArgumentExceptionHelper.ThrowIfNull(configure);

            JsonSerializerSettings settings = new();
            configure(settings);
            NewtonsoftBsonSerializer serializer = new() { Options = settings, };
            _ = builder.WithSerializer(() => serializer);
            UniversalSerializer.RegisterSerializer(static () => new NewtonsoftSerializer());
            UniversalSerializer.RegisterSerializer(() => new NewtonsoftBsonSerializer { Options = settings, });
            return builder;
        }
    }
}
