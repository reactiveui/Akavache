// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

using Akavache.Core;

namespace Akavache.Sqlite3;

/// <summary>
/// Providers a override for the linker.
/// This will use bait and switch to provide different versions.
/// </summary>
[Preserve(AllMembers = true)]
public static class LinkerPreserve
{
    /// <summary>
    /// Initializes static members of the <see cref="LinkerPreserve"/> class.
    /// This will be different in derived classes
    /// and will use bait and switch.
    /// </summary>
    /// <exception cref="Exception">A exception due to this being in the non-derived assembly.</exception>
    [SuppressMessage("FxCop.Design", "CA1065: Don't throw in constructors", Justification = "Shim class, should not happen.")]
    static LinkerPreserve() => throw new InvalidOperationException(typeof(SQLitePersistentBlobCache).FullName);
}
