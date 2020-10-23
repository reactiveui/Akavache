// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Sqlite3
{
    /// <summary>
    /// A object that has been wrapped. Used often for allowing serialization.
    /// </summary>
    /// <typeparam name="T">The type of object wrapped.</typeparam>
    internal class ObjectWrapper<T> : IObjectWrapper
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public T Value { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
