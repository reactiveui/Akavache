// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Akavache.Internal
{
    /// <summary>
    /// Gets a set of flags about the file access mode.
    /// </summary>
    [SuppressMessage("FxCop.Style", "CA1714: Flags should use plural names", Justification = "Legacy reasons.")]
    [Flags]
    public enum FileAccess
    {
        /// <summary>
        /// The file access is used for reading.
        /// </summary>
        Read = 1,

        /// <summary>
        /// The file access is used for writing.
        /// </summary>
        Write = 2,

        /// <summary>
        /// The file access is used for reading and writing.
        /// </summary>
        ReadWrite = 3,
    }
}
