﻿// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Sqlite3;

/// <summary>
/// A object that has been wrapped. Used often for allowing serialization.
/// </summary>
/// <typeparam name="T">The type of object wrapped.</typeparam>
internal class ObjectWrapper<T> : IObjectWrapper
{
    public ObjectWrapper()
    {
    }

    public ObjectWrapper(T value)
    {
        Value = value;
    }

    public T? Value { get; set; }
}
