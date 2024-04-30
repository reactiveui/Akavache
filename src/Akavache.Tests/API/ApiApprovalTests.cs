// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

using Akavache.Sqlite3;

namespace Akavache.APITests;

/// <summary>
/// Tests for handling API approval.
/// </summary>
[ExcludeFromCodeCoverage]
public class ApiApprovalTests
{
    /// <summary>
    /// Tests to make sure the akavache project is approved.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task AkavacheProject() => typeof(SQLitePersistentBlobCache).Assembly.CheckApproval(["Akavache"]);

    /// <summary>
    /// Tests to make sure the akavache core project is approved.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task AkavacheCore() => typeof(BlobCache).Assembly.CheckApproval(["Akavache"]);

#if !NETSTANDARD
    /// <summary>
    /// Tests to make sure the akavache drawing project is approved.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public Task AkavacheDrawing() => typeof(Akavache.Drawing.Registrations).Assembly.CheckApproval(["Akavache"]);
#endif
}
