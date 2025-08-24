// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Akavache.NewtonsoftJson
{
    /// <summary>
    /// AkavacheBuilderExtensions.
    /// </summary>
    public static class AkavacheBuilderExtensions
    {
        /// <summary>
        /// Uses the newtonsoft json.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="System.ArgumentNullException">builder.</exception>
        public static IAkavacheBuilder WithSerializerNewtonsoftJson(this IAkavacheBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.WithSerializer<NewtonsoftSerializer>();
            return builder;
        }

        /// <summary>
        /// Uses the newtonsoft json.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// builder
        /// or
        /// settings.
        /// </exception>
        public static IAkavacheBuilder WithSerializerNewtonsoftJson(this IAkavacheBuilder builder, JsonSerializerSettings settings)
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
        /// Uses the newtonsoft json.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configure">The configure.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// builder
        /// or
        /// configure.
        /// </exception>
        public static IAkavacheBuilder WithSerializerNewtonsoftJson(this IAkavacheBuilder builder, Action<JsonSerializerSettings> configure)
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
        /// Uses the newtonsoft bson.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="System.ArgumentNullException">builder.</exception>
        public static IAkavacheBuilder WithSerializerNewtonsoftBson(this IAkavacheBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.WithSerializer<NewtonsoftBsonSerializer>();
            return builder;
        }

        /// <summary>
        /// Uses the newtonsoft bson.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// builder
        /// or
        /// settings.
        /// </exception>
        public static IAkavacheBuilder WithSerializerNewtonsoftBson(this IAkavacheBuilder builder, JsonSerializerSettings settings)
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
        /// Uses the newtonsoft bson.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configure">The configure.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// builder
        /// or
        /// configure.
        /// </exception>
        public static IAkavacheBuilder WithSerializerNewtonsoftBson(this IAkavacheBuilder builder, Action<JsonSerializerSettings> configure)
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
