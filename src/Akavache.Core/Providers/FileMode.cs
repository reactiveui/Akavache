// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Internal;

/// <summary>
/// Gets the file mode.
/// </summary>
public enum FileMode
{
    /// <summary>
    /// Creates the contents of the file.
    /// </summary>
    CreateNew = 1,

    /// <summary>
    /// Creates a new file.
    /// </summary>
    Create = 2,

    /// <summary>
    /// Opens the file.
    /// </summary>
    Open = 3,

    /// <summary>
    /// Opens or creates the file.
    /// </summary>
    OpenOrCreate = 4,

    /// <summary>
    /// Truncates the file.
    /// </summary>
    Truncate = 5,

    /// <summary>
    /// Appends to the end of the file.
    /// </summary>
    Append = 6,
}