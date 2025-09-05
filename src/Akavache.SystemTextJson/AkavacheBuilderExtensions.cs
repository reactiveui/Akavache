// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Akavache.SystemTextJson
{
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
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder WithSerializerSystemTextJson(this IAkavacheBuilder builder)
#else
        public static IAkavacheBuilder WithSerializerSystemTextJson(this IAkavacheBuilder builder)
#endif
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.WithSerializer<SystemJsonSerializer>();
            return builder;
        }

        /// <summary>
        /// Configures the builder to use System.Text.Json serialization with custom options.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <param name="settings">The JSON serializer options to use for customizing serialization behavior.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="settings"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder WithSerializerSystemTextJson(this IAkavacheBuilder builder, JsonSerializerOptions settings)
#else
        public static IAkavacheBuilder WithSerializerSystemTextJson(this IAkavacheBuilder builder, JsonSerializerOptions settings)
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

            builder.WithSerializer(() => new SystemJsonSerializer { Options = settings });
            return builder;
        }

        /// <summary>
        /// Configures the builder to use System.Text.Json serialization with options configured through a delegate.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <param name="configure">Action to configure the JSON serializer options.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder UseSystemTextJsonSerializer(this IAkavacheBuilder builder, Action<JsonSerializerOptions> configure)
#else
        public static IAkavacheBuilder UseSystemTextJsonSerializer(this IAkavacheBuilder builder, Action<JsonSerializerOptions> configure)
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

            var settings = new JsonSerializerOptions();
            configure(settings);
            builder.WithSerializer(() => new SystemJsonSerializer { Options = settings });
            return builder;
        }

        /// <summary>
        /// Configures the builder to use System.Text.Json BSON serialization with default options.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder UseSystemJsonBsonSerializer(this IAkavacheBuilder builder)
#else
        public static IAkavacheBuilder UseSystemJsonBsonSerializer(this IAkavacheBuilder builder)
#endif
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.WithSerializer<SystemJsonBsonSerializer>();
            return builder;
        }

        /// <summary>
        /// Configures the builder to use System.Text.Json BSON serialization with custom options.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <param name="settings">The JSON serializer options to use for customizing BSON serialization behavior.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="settings"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder UseSystemJsonBsonSerializer(this IAkavacheBuilder builder, JsonSerializerOptions settings)
#else
        public static IAkavacheBuilder UseSystemJsonBsonSerializer(this IAkavacheBuilder builder, JsonSerializerOptions settings)
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

            builder.WithSerializer(() => new SystemJsonBsonSerializer { Options = settings, });
            return builder;
        }

        /// <summary>
        /// Configures the builder to use System.Text.Json BSON serialization with options configured through a delegate.
        /// </summary>
        /// <param name="builder">The Akavache builder to configure.</param>
        /// <param name="configure">Action to configure the JSON serializer options for BSON serialization.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
#if NET6_0_OR_GREATER

        [RequiresUnreferencedCode("Serializers require types to be preserved for serialization.")]
        public static IAkavacheBuilder UseSystemJsonBsonSerializer(this IAkavacheBuilder builder, Action<JsonSerializerOptions> configure)
#else
        public static IAkavacheBuilder UseSystemJsonBsonSerializer(this IAkavacheBuilder builder, Action<JsonSerializerOptions> configure)
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

            var settings = new JsonSerializerOptions();
            configure(settings);
            builder.WithSerializer(() => new SystemJsonBsonSerializer { Options = settings, });
            return builder;
        }
    }
}
