// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Tests;

using TUnit.Core;
using TUnit.Core.Interfaces;

[assembly: ParallelLimiter<ParallelLimit2>]

namespace Akavache.Tests;

/// <summary>
/// Limits parallel test execution to 2 concurrent tests.
/// </summary>
#pragma warning disable SA1649
public sealed class ParallelLimit2 : IParallelLimit
#pragma warning restore SA1649
{
    /// <inheritdoc/>
    public int Limit => 2;
}
