// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Akavache.NewtonsoftJson
{
    /// <summary>
    /// Provides extension methods for configuring Akavache to use Newtonsoft.Json serialization.
    /// </summary>
    public static class AkavacheBuilderExtensions
    {
        /// <summary>
        /// Configures the builder to use Newtonsoft.Json serialization with default settings.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder WithSerializerNewtonsoftJson(this IAkavacheBuilder builder)
#else
        public static IAkavacheBuilder WithSerializerNewtonsoftJson(this IAkavacheBuilder builder)
#endif
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.WithSerializer<NewtonsoftSerializer>();
            return builder;
        }

        /// <summary>
        /// Configures the builder to use Newtonsoft.Json serialization with custom settings.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <param name="settings">The JSON serializer settings to use for customizing serialization behavior.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="settings"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder WithSerializerNewtonsoftJson(this IAkavacheBuilder builder, JsonSerializerSettings settings)
#else
        public static IAkavacheBuilder WithSerializerNewtonsoftJson(this IAkavacheBuilder builder, JsonSerializerSettings settings)
#endif
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            builder.WithSerializer(() => new NewtonsoftSerializer { Options = settings });
            return builder;
        }

        /// <summary>
        /// Configures the builder to use Newtonsoft.Json serialization with settings configured through a delegate.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <param name="configure">Action to configure the JSON serializer settings.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder WithSerializerNewtonsoftJson(this IAkavacheBuilder builder, Action<JsonSerializerSettings> configure)
#else
        public static IAkavacheBuilder WithSerializerNewtonsoftJson(this IAkavacheBuilder builder, Action<JsonSerializerSettings> configure)
#endif
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var settings = new JsonSerializerSettings();
            configure(settings);
            builder.WithSerializer(() => new NewtonsoftSerializer { Options = settings });
            return builder;
        }

        /// <summary>
        /// Configures the builder to use Newtonsoft.Json BSON serialization with default settings.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder WithSerializerNewtonsoftBson(this IAkavacheBuilder builder)
#else
        public static IAkavacheBuilder WithSerializerNewtonsoftBson(this IAkavacheBuilder builder)
#endif
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.WithSerializer<NewtonsoftBsonSerializer>();
            return builder;
        }

        /// <summary>
        /// Configures the builder to use Newtonsoft.Json BSON serialization with custom settings.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <param name="settings">The JSON serializer settings to use for customizing BSON serialization behavior.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="settings"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder WithSerializerNewtonsoftBson(this IAkavacheBuilder builder, JsonSerializerSettings settings)
#else
        public static IAkavacheBuilder WithSerializerNewtonsoftBson(this IAkavacheBuilder builder, JsonSerializerSettings settings)
#endif
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var serializer = new NewtonsoftBsonSerializer
            {
                Options = settings,
            };
            builder.WithSerializer(() => serializer);
            return builder;
        }

        /// <summary>
        /// Configures the builder to use Newtonsoft.Json BSON serialization with settings configured through a delegate.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <param name="configure">Action to configure the JSON serializer settings for BSON serialization.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder WithSerializerNewtonsoftBson(this IAkavacheBuilder builder, Action<JsonSerializerSettings> configure)
#else
        public static IAkavacheBuilder WithSerializerNewtonsoftBson(this IAkavacheBuilder builder, Action<JsonSerializerSettings> configure)
#endif
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var settings = new JsonSerializerSettings();
            configure(settings);
            var serializer = new NewtonsoftBsonSerializer
            {
                Options = settings,
            };
            builder.WithSerializer(() => serializer);
            return builder;
        }
    }
}
