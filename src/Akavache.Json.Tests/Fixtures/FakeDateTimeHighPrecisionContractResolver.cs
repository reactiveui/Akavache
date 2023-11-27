﻿// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json.Serialization;

namespace Akavache.Tests;

/// <summary>
/// A fake DateTime using high precision times.
/// </summary>
public class FakeDateTimeHighPrecisionContractResolver : DefaultContractResolver
{
    /// <inheritdoc />
    protected override JsonContract CreateContract(Type objectType)
    {
        var contract = base.CreateContract(objectType);
        if (objectType == typeof(DateTime) || objectType == typeof(DateTime?))
        {
            contract.Converter = new FakeDateTimeHighPrecisionJsonConverter();
        }

        return contract;
    }
}