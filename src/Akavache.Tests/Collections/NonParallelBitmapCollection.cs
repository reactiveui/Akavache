// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Ensures all tests that interact with global bitmap state (Splat.BitmapLoader)
/// do not run in parallel with any other tests in the suite.
/// </summary>
[CollectionDefinition("Non-Parallel Bitmap Tests", DisableParallelization = true)]
public sealed class NonParallelBitmapCollection
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and disable parallelization.
}
