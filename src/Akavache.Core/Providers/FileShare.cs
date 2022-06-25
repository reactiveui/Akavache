// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Internal;

/// <summary>
/// Flags about how the file should be shared on the file system.
/// </summary>
[Flags]
public enum FileShare
{
    /// <summary>
    /// There is no sharing.
    /// </summary>
    None = 0,

    /// <summary>
    /// The file is shared for reading only.
    /// </summary>
    Read = 1,

    /// <summary>
    /// The file is for writing.
    /// </summary>
    Write = 2,

    /// <summary>
    /// The file sharing is for both reading and writing.
    /// </summary>
    ReadWrite = 3,

    /// <summary>
    /// The file sharing is for deleting.
    /// </summary>
    Delete = 4,

    /// <summary>
    /// The file sharing is inheritable.
    /// </summary>
    Inheritable = 16,
}