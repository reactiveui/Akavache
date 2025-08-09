// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core
{
    /// <summary>
    /// BlobCacheBuilderExtensions.
    /// </summary>
    public static class BlobCacheBuilderExtensions
    {
        /// <summary>
        /// Withes the serializser.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The builder instance for fluent configuration.</returns>
        /// <exception cref="System.ArgumentNullException">serializer.</exception>
        public static IBlobCacheBuilder WithSerializer(this IBlobCacheBuilder builder, ISerializer serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            // Ensure the builder is not null
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.WithSerializer(serializer);
        }
    }
}
