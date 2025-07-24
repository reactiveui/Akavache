// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveMarbles.CacheDatabase.Core
{
    /// <summary>
    /// Determines how to serialize to and from a byte.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Deserializes from bytes.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <returns>The type.</returns>
        T? Deserialize<T>(byte[] bytes);

        /// <summary>
        /// Serializes to an bytes.
        /// </summary>
        /// <typeparam name="T">The type of serialize.</typeparam>
        /// <param name="item">The item to serialize.</param>
        /// <returns>The bytes.</returns>
        byte[] Serialize<T>(T item);
    }
}
