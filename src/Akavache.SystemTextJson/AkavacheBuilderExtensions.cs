// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Akavache.SystemTextJson
{
    /// <summary>
    /// AkavacheBuilderExtensions.
    /// </summary>
    public static class AkavacheBuilderExtensions
    {
        /// <summary>
        /// Uses the system text json.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="System.ArgumentNullException">builder.</exception>
        public static IAkavacheBuilder UseSystemTextJsonSerializer(this IAkavacheBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.WithSerializer(new SystemJsonSerializer());
            return builder;
        }

        /// <summary>
        /// Uses the system text json.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// builder
        /// or
        /// settings.
        /// </exception>
        public static IAkavacheBuilder UseSystemTextJsonSerializer(this IAkavacheBuilder builder, JsonSerializerOptions settings)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            builder.WithSerializer(new SystemJsonSerializer { Options = settings });
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
        public static IAkavacheBuilder UseSystemTextJsonSerializer(this IAkavacheBuilder builder, Action<JsonSerializerOptions> configure)
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
            builder.WithSerializer(new SystemJsonSerializer { Options = settings });
            return builder;
        }

        /// <summary>
        /// Uses the newtonsoft bson.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="System.ArgumentNullException">builder.</exception>
        public static IAkavacheBuilder UseSystemJsonBsonSerializer(this IAkavacheBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.WithSerializer(new SystemJsonBsonSerializer());
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

            builder.WithSerializer(new SystemJsonBsonSerializer { Options = settings, });
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
            builder.WithSerializer(new SystemJsonBsonSerializer { Options = settings, });
            return builder;
        }
    }
}
