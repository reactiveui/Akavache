﻿// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Akavache;

/// <summary>
/// The default http client factory. Can be replaced with for example the Microsoft Http Client factory.
/// </summary>
public class DefaultAkavacheHttpClientFactory : IAkavacheHttpClientFactory
{
    private static ConcurrentDictionary<string, HttpClient> _instances = new();

    /// <inheritdoc/>
    public HttpClient CreateClient(string name) => _instances.GetOrAdd(name, _ => new HttpClient());
}
